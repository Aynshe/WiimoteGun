using System;
using System.Collections.Generic;
using System.Linq;
using WiimoteLib;
using WiimoteLib.Events;

namespace WiimoteGun
{
    class WiimoteControllerManager : IDisposable
    {
        private List<WiiMoteController> _controllers;
        private int MaxWiimotes => Options.Instance.Enable4Players ? 4 : 2;
        private EmulatorProcessMonitor _emulatorMonitor;

        public int ConnectedWiimotesCount => _controllers.Count;
        public IEnumerable<WiiMoteController> Controllers => _controllers.AsReadOnly();

        public WiimoteControllerManager()
        {
            _controllers = new List<WiiMoteController>();

            // Initialize emulator monitor (EN/FR: Initialiser moniteur émulateur)
            _emulatorMonitor = new EmulatorProcessMonitor();

            // Check if emulator is running at startup (EN/FR: Vérifier si émulateur actif au démarrage)
            if (_emulatorMonitor.IsEmulatorRunning())
            {
                SimpleLogger.Instance.Info("Dolphin/Cemu detected at startup - Skipping Wiimote connection");
                Program.Notify("Dolphin/Cemu detected\nWiimote control disabled");
                
                // Start monitoring for emulator shutdown (EN/FR: Surveiller arrêt émulateur)
                _emulatorMonitor.EmulatorStopped += OnEmulatorStopped;
                _emulatorMonitor.StartMonitoring();
                return; // Skip Wiimote initialization
            }

            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;
            WiimoteManager.AutoConnect = true;
            WiimoteManager.AutoDiscoveryCount = MaxWiimotes;

            WiimoteManager.Connected += OnWiimoteConnected;
            WiimoteManager.Disconnected += OnWiimoteDisconnected;
            WiimoteManager.WiimoteException += OnWiimoteException;

            WiimoteManager.StartDiscovery();

            // Start monitoring for emulator startup during runtime (EN/FR: Surveiller démarrage émulateur)
            _emulatorMonitor.EmulatorStarted += OnEmulatorStarted;
            _emulatorMonitor.StartMonitoring();
        }

        public void Dispose()
        {
            WiimoteManager.Connected -= OnWiimoteConnected;
            WiimoteManager.Disconnected -= OnWiimoteDisconnected;
            WiimoteManager.WiimoteException -= OnWiimoteException;

            foreach (var controller in _controllers)
            {
                controller.Dispose();
            }
            _controllers.Clear();

            // Stop emulator monitoring (EN/FR: Arrêter surveillance émulateur)
            if (_emulatorMonitor != null)
            {
                _emulatorMonitor.EmulatorStarted -= OnEmulatorStarted;
                _emulatorMonitor.EmulatorStopped -= OnEmulatorStopped;
                _emulatorMonitor.Dispose();
                _emulatorMonitor = null;
            }
        }

        private void OnWiimoteException(object sender, WiimoteExceptionEventArgs e)
        {
            SimpleLogger.Instance.Error("Wiimote Exception from Manager: " + e.ToString());
            
            // Critical fix: Disconnect Wiimote on fatal errors (IO/Timeout) to ensure cleanup and Service disable command
            // (EN/FR: Fix critique : Déconnecter Wiimote sur erreur fatale pour assurer cleanup et commande service)
            if (e.Wiimote != null)
            {
                System.Threading.Tasks.Task.Run(() => 
                {
                    try 
                    {
                        // Use static Disconnect method to force cleanup even if IsConnected is false (partially disposed)
                        // (EN/FR: Utiliser méthode Disconnect statique pour forcer cleanup même si déjà disposé)
                        SimpleLogger.Instance.Warning($"Force disconnecting Wiimote {e.Wiimote.Address} due to exception.");
                        WiimoteManager.Disconnect(e.Wiimote);
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error($"Error disconnecting Wiimote after exception: {ex.Message}");
                        // If Disconnect failed (already removed from manager list), manually trigger cleanup
                        // (EN/FR: Si Disconnect échoue (déjà retiré de la liste), déclencher cleanup manuellement)
                        var controller = _controllers.FirstOrDefault(c => c.Wiimote == e.Wiimote);
                        if (controller != null)
                        {
                            SimpleLogger.Instance.Warning($"Forcing manual cleanup for P{controller.PlayerIndex}");
                            _controllers.Remove(controller);
                            controller.Dispose(); // This will call ServiceClient.DisablePlayer
                            SimpleLogger.Instance.Info($"Wiimote P{controller.PlayerIndex} disconnected (manual cleanup).");
                            if (_controllers.Count == 0)
                            {
                                Program.SetConnectedState(false);
                            }
                        }
                    }
                });
            }
        }

        private void OnWiimoteConnected(object sender, WiimoteEventArgs e)
        {
            if (_controllers.Count >= MaxWiimotes)
            {
                SimpleLogger.Instance.Warning("Max number of Wiimotes reached. Ignoring new connection.");
                // Maybe provide some feedback to the user, like a short rumble.
                try
                {
                    e.Wiimote.SetRumble(true);
                    System.Threading.Thread.Sleep(200);
                    e.Wiimote.SetRumble(false);
                }
                catch { }
                return;
            }

            try
            {
                string mac = e.Wiimote.Address.ToString();
                int playerIndex = -1;

                // 1. Check if this MAC is already assigned to a preferred slot
                if (Options.Instance.PreferredMacP1 == mac) playerIndex = 1;
                else if (Options.Instance.PreferredMacP2 == mac) playerIndex = 2;
                else if (Options.Instance.PreferredMacP3 == mac) playerIndex = 3;
                else if (Options.Instance.PreferredMacP4 == mac) playerIndex = 4;

                // 2. If found, check if available
                if (playerIndex != -1)
                {
                    if (_controllers.Any(c => c.PlayerIndex == playerIndex))
                    {
                        SimpleLogger.Instance.Warning($"Wiimote {mac} is preferred for P{playerIndex} but slot is busy. Finding next available.");
                        playerIndex = -1;
                    }
                }

                // 3. If not found or busy, find first available slot
                if (playerIndex == -1)
                {
                    for (int i = 1; i <= MaxWiimotes; i++)
                    {
                        if (!_controllers.Any(c => c.PlayerIndex == i))
                        {
                            playerIndex = i;
                            break;
                        }
                    }
                }

                if (playerIndex == -1)
                {
                    SimpleLogger.Instance.Error("No available player slots for Wiimote " + mac);
                    return;
                }

                // CRITICAL: In SendInput mode, reject any Wiimote beyond Player 1
                // (EN/FR: CRITIQUE : En mode SendInput, rejeter toute Wiimote au-delà du Joueur 1)
                if (Options.Instance.DefaultMouseMode == MouseMode.SendInput && playerIndex > 1)
                {
                    SimpleLogger.Instance.Warning($"SendInput mode only supports Player 1. Rejecting Wiimote {mac} assigned to Player {playerIndex}.");
                    Program.Notify($"SendInput mode: Only 1 Wiimote allowed. Please disconnect or switch to RawInput mode.");
                    
                    // Disconnect this Wiimote
                    try
                    {
                        e.Wiimote.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error($"Failed to disconnect Wiimote {mac}: {ex.Message}");
                    }
                    return;
                }

                // 4. Auto-save preference removed. 
                // "None (Auto)" should mean dynamic assignment, not "Auto-Learn and Fix".
                // If user wants to fix a Wiimote to a player, they must do it manually via the menu.
                SimpleLogger.Instance.Info($"Assigned Wiimote {mac} to Player {playerIndex} (Dynamic)");

                var controller = new WiiMoteController(e.Wiimote, playerIndex);
                _controllers.Add(controller);

                Program.SetConnectedState(true);

                // Schedule VMulti collection cleanup after Wiimote connection
                // (EN/FR: Planifier le nettoyage des collections VMulti après connexion Wiimote)
                Core.VMultiDeviceCleanup.ScheduleCleanupAfterWiimoteConnect();

                // Remove COL03 mice for unconnected players to hide ghost lightgun icons in ES
                // (EN/FR: Supprimer les souris COL03 des joueurs non connectés pour masquer icônes fantômes)
                ScheduleRemoveUnconnectedMice();

                // CRITICAL: For DolphinBar, enable periodic GetStatus polling for disconnect detection
                // (EN/FR: CRITIQUE : Pour DolphinBar, activer polling GetStatus périodique pour détection déconnexion)
                // DolphinBar doesn't generate Windows disconnect events when Wiimote turns off
                if (!e.Wiimote.Device.IsBluetooth)
                {
                    SimpleLogger.Instance.Info("Enabling periodic disconnect detection for DolphinBar");
                }
            }
            catch (BadImageFormatException)
            {
                System.Windows.Forms.MessageBox.Show("A fatal error occurred while connecting to the virtual driver components.\n\n" +
                                                      "This is likely caused by a 32-bit/64-bit architecture mismatch.\n\n" +
                                                      "Please ensure that all DLLs (vmulti, interception) are the correct versions for your system.",
                                                      "Architecture Mismatch", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                // Optionally, shut down the application
                Program.PostToUIThread(() => System.Windows.Forms.Application.Exit());
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("Failed to create WiiMoteController: " + ex.ToString());
            }
        }

        private void OnWiimoteDisconnected(object sender, WiimoteDisconnectedEventArgs e)
        {
            var controller = _controllers.FirstOrDefault(c => c.Wiimote == e.Wiimote);
            if (controller != null)
            {
                int playerIndex = controller.PlayerIndex;
                _controllers.Remove(controller);
                controller.Dispose();
                SimpleLogger.Instance.Info($"Wiimote P{playerIndex} disconnected.");

                // Remove COL03 mouse for this disconnected player to hide ghost lightgun icon
                // (EN/FR: Supprimer la souris COL03 du joueur déconnecté pour masquer icône fantôme)
                Core.VMultiDeviceCleanup.RemoveMouseForDisconnectedPlayer(playerIndex);
            }

            if (_controllers.Count == 0)
            {
                Program.SetConnectedState(false);
            }
        }
        public IEnumerable<WiiMoteController> GetControllers()
        {
            return _controllers.ToList();
        }

        /// <summary>
        /// EN: Schedule removal of COL03 mice for unconnected players (with delay).
        /// FR: Planifier la suppression des souris COL03 pour les joueurs non connectés (avec délai).
        /// </summary>
        private void ScheduleRemoveUnconnectedMice()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    // EN: Wait for device enumeration after Wiimote connect
                    // FR: Attendre l'énumération des périphériques après connexion Wiimote
                    await System.Threading.Tasks.Task.Delay(3500).ConfigureAwait(false);

                    // EN: Get list of currently connected player indexes
                    // FR: Obtenir la liste des index des joueurs actuellement connectés
                    var connectedIndexes = _controllers.Select(c => c.PlayerIndex).ToArray();
                    Core.VMultiDeviceCleanup.RemoveMouseForUnconnectedPlayers(connectedIndexes);
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[WiimoteControllerManager] Error in ScheduleRemoveUnconnectedMice: {ex.Message}");
                }
            });
        }
        public WiiMoteController GetController(Guid id)
        {
            return _controllers.FirstOrDefault(c => c.Wiimote.ID == id);
        }
        public void UpdateIRSensitivity()
        {
            foreach (var controller in _controllers)
            {
                controller.UpdateIRSensitivity();
            }
        }

        /// <summary>
        /// Called when emulator starts during runtime (EN/FR: Appelé quand émulateur démarre pendant exécution)
        /// </summary>
        private void OnEmulatorStarted(object sender, EventArgs e)
        {
            SimpleLogger.Instance.Info("Emulator started - Triggering WiimoteGun restart");
            Program.Notify("Dolphin/Cemu started\nRestarting WiimoteGun...");

            // Delay to allow notification to be read (EN/FR: Délai pour laisser lire la notification)
            System.Threading.Thread.Sleep(2500);

            // Trigger restart with -refresh command (EN/FR: Déclencher redémarrage avec commande -refresh)
            RestartWithRefresh();
        }

        /// <summary>
        /// Called when emulator stops (EN/FR: Appelé quand émulateur s'arrête)
        /// </summary>
        private void OnEmulatorStopped(object sender, EventArgs e)
        {
            SimpleLogger.Instance.Info("Emulator stopped - Triggering WiimoteGun restart");
            Program.Notify("Dolphin/Cemu closed\nRestarting WiimoteGun...");

            // Delay to allow notification to be read (EN/FR: Délai pour laisser lire la notification)
            System.Threading.Thread.Sleep(2500);

            // Trigger restart with -refresh command (EN/FR: Déclencher redémarrage avec commande -refresh)
            RestartWithRefresh();
        }

        /// <summary>
        /// Trigger WiimoteGun restart via -refresh IPC (EN/FR: Déclencher redémarrage via -refresh IPC)
        /// </summary>
        private void RestartWithRefresh()
        {
            try
            {
                // Use MainModule.FileName to get the actual EXE path (EN/FR: Utiliser MainModule.FileName pour le chemin EXE)
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                SimpleLogger.Instance.Info($"Triggering restart via -refresh: {exePath}");
                
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = "-refresh"; // Send IPC message to running instance (EN/FR: Envoyer message IPC à l'instance)
                process.StartInfo.UseShellExecute = false;
                process.Start();

                SimpleLogger.Instance.Info("Refresh command sent - instance will reload automatically");
                
                // DO NOT exit - the -refresh command will trigger OnRefreshRequested which restarts
                // (EN/FR: NE PAS quitter - la commande -refresh déclenchera OnRefreshRequested qui redémarre)
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to send refresh command: {ex.Message}");
                Program.Notify("Restart failed\nPlease restart manually");
            }
        }
    }
}
