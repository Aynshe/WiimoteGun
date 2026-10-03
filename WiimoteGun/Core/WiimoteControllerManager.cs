using System;
using System.Collections.Generic;
using System.Linq;
using WiimoteLib;
using WiimoteLib.Events;
using WiimoteGun.Core;

namespace WiimoteGun
{
    public class WiimoteControllerManager : IDisposable
    {
        private List<WiiMoteController> _controllers;
        private int MaxWiimotes { get { return Options.Instance.Enable4Players ? 4 : 2; } }
        private EmulatorProcessMonitor _emulatorMonitor;
        private System.Threading.Timer _refreshTimer; // Debounce timer for DInput refresh (EN/FR: Timer d'anti-rebond)

        // [V55] BT Watchdog: timer for auto BT reset when no Wiimote connects
        // (EN/FR: Watchdog BT : timer pour reset BT auto si aucune Wiimote ne se connecte)
        private System.Threading.Timer _btWatchdogTimer;
        private readonly object _btWatchdogLock = new object();

        public int ConnectedWiimotesCount { get { return _controllers.Count; } }
        public IEnumerable<WiiMoteController> Controllers { get { return _controllers.AsReadOnly(); } }

        /// <summary>
        /// Check if any connected controller is currently in GamePad or GamePad43 mode.
        /// (EN/FR: Vérifier si au moins un contrôleur est en mode GamePad ou GamePad43)
        /// </summary>
        public bool IsAnyGamePadActive
        {
            get
            {
                return _controllers.Any(c => c.Mode == WiiMoteMode.GamePad || c.Mode == WiiMoteMode.GamePad43 || c.Mode == WiiMoteMode.GamePadFPS);
            }
        }

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
            // [V55v] Record real failed connection attempts as BT-reset evidence
            // (EN/FR: Enregistrer les véritables tentatives de connexion échouées comme preuve de reset BT)
            WiimoteManager.ConnectionFailed += OnWiimoteConnectionFailed;

            WiimoteManager.StartDiscovery();

            // Start monitoring for emulator startup during runtime (EN/FR: Surveiller démarrage émulateur)
            _emulatorMonitor.EmulatorStarted += OnEmulatorStarted;
            _emulatorMonitor.StartMonitoring();

            // [V55] Arm BT watchdog after startup grace period (2s), in case no Wiimote connects at all
            // (EN/FR: Armer le watchdog BT après délai de démarrage (2s), si aucune Wiimote ne se connecte)
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
            {
                if (_controllers.Count == 0)
                    ArmBtWatchdog();
            });
        }

        public void Dispose()
        {
            WiimoteManager.Connected -= OnWiimoteConnected;
            WiimoteManager.Disconnected -= OnWiimoteDisconnected;
            WiimoteManager.WiimoteException -= OnWiimoteException;
            WiimoteManager.ConnectionFailed -= OnWiimoteConnectionFailed; // [V55v]

            // [V55] Cancel BT watchdog on dispose (EN/FR: Annuler le watchdog BT à la destruction)
            DisarmBtWatchdog();

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
                // [V55] Disarm BT watchdog: a Wiimote connected, no reset needed
                // (EN/FR: Désarmer le watchdog BT : une Wiimote s'est connectée, pas besoin de reset)
                DisarmBtWatchdog();

                // [V56e] Schedule the "update available" tile notification once per
                // session: 20 seconds after the FIRST Wiimote connects. Never shown
                // while an ES game-start is in progress (re-checked every 30s while a
                // game runs). Displays for 6 seconds.
                // (EN/FR: Planifier la notification tuile « mise à jour disponible »
                // une fois par session : 20 secondes après la PREMIÈRE connexion
                // Wiimote. Jamais pendant un game-start ES en cours (revérifié toutes
                // les 30s tant qu'un jeu tourne). Affichée pendant 6 secondes.)
                ScheduleUpdateNotificationOnce();

                string mac = e.Wiimote.Address.ToString();
                int playerIndex = -1;

                // 1. Check if this MAC is already assigned to a preferred slot
                if (Options.Instance.PreferredMacP1 == mac) playerIndex = 1;
                else if (Options.Instance.PreferredMacP2 == mac) playerIndex = 2;
                else if (Options.Instance.PreferredMacP3 == mac) playerIndex = 3;
                else if (Options.Instance.PreferredMacP4 == mac) playerIndex = 4;

                // 2. If found, check if available and not locked
                // (EN/FR: Si trouvé, vérifier disponibilité et non verrouillé)
                if (playerIndex != -1)
                {
                    if (Options.Instance.GetLockedSlot(playerIndex))
                    {
                        SimpleLogger.Instance.Warning($"Wiimote {mac} preferred for P{playerIndex} but slot is locked. Finding next available.");
                        playerIndex = -1;
                    }
                    else if (_controllers.Any(c => c.PlayerIndex == playerIndex))
                    {
                        SimpleLogger.Instance.Warning($"Wiimote {mac} is preferred for P{playerIndex} but slot is busy. Finding next available.");
                        playerIndex = -1;
                    }
                }

                // 3. If not found or busy, find first available unlocked slot
                // (EN/FR: Si non trouvé, trouver le premier slot disponible et non verrouillé)
                if (playerIndex == -1)
                {
                    for (int i = 1; i <= MaxWiimotes; i++)
                    {
                        // Skip locked slots (EN/FR: Ignorer slots verrouillés)
                        if (Options.Instance.GetLockedSlot(i)) continue;
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

                // [V55] Arm BT watchdog on last disconnect
                // (EN/FR: Armer le watchdog BT à la déconnexion de la dernière Wiimote)
                ArmBtWatchdog();
            }
        }

        // ====================================================================
        // [V55/V55r] BT WATCHDOG (EN/FR: WATCHDOG BT)
        // ====================================================================

        // EN/FR: [V55r] Consecutive reset attempts without any Wiimote connecting.
        // After this many failed resets the watchdog STOPS: repeatedly resetting a
        // wedged/degraded dongle never heals it (V53/V53b) and used to leave the
        // BT stack in a worse state. The counter resets when a Wiimote connects.
        private const int BtResetMaxAttempts = 3;
        private int _btResetAttempts = 0;

        // EN/FR: [V55v] Evidence of a REAL failed connection attempt since the watchdog
        // was armed. The BT reset must NEVER fire when the user is simply not trying
        // to connect (Wiimote off / out of range): it is requested ONLY when a real
        // attempt failed or the radio stack is proven wedged.
        private bool _btAttemptFailedSinceArm = false;
        private DateTime _btArmUtc = DateTime.MinValue;

        // [V56e] Update notification scheduling state (EN/FR: État de planification de la notification de mise à jour)
        private bool _updateNotificationScheduled = false;

        // ====================================================================
        // [V56e] UPDATE AVAILABLE NOTIFICATION (EN/FR: NOTIFICATION MISE À JOUR)
        // ====================================================================

        /// <summary>
        /// EN: [V56e] Once per session, 20 seconds after the FIRST Wiimote connects, show
        /// the "update available" tile notification for 6 seconds — but NEVER while an ES
        /// game-start is in progress (re-checked every 30s while a game runs, up to 10
        /// minutes). The tile stacks below any notification already on screen.
        /// FR: [V56e] Une fois par session, 20 secondes après la PREMIÈRE connexion
        /// Wiimote, afficher la notification tuile « mise à jour disponible » pendant
        /// 6 secondes — mais JAMAIS pendant un game-start ES en cours (revérifié toutes
        /// les 30s tant qu'un jeu tourne, jusqu'à 10 minutes). La tuile s'empile sous
        /// toute notification déjà à l'écran.
        /// </summary>
        private void ScheduleUpdateNotificationOnce()
        {
            if (_updateNotificationScheduled) return; // Once per session (EN/FR: Une fois par session)
            _updateNotificationScheduled = true;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    System.Threading.Tasks.Task.Delay(20000).Wait(); // 20s after the first Wiimote connects

                    DateTime giveUp = DateTime.UtcNow.AddMinutes(10);
                    while (EsScriptIntegration.HasCurrentGame && DateTime.UtcNow < giveUp)
                    {
                        // EN/FR: A game is running: do not disturb — re-check in 30s
                        System.Threading.Tasks.Task.Delay(30000).Wait();
                    }

                    if (EsScriptIntegration.HasCurrentGame) return; // Still in game after the grace window (EN/FR: Toujours en jeu après la fenêtre de grâce)

                    if (!Core.AppUpdateChecker.HasChecked)
                    {
                        // EN/FR: Check still running: wait a bit for it (max 10s)
                        DateTime limit = DateTime.UtcNow.AddSeconds(10);
                        while (!Core.AppUpdateChecker.HasChecked && DateTime.UtcNow < limit)
                        {
                            System.Threading.Tasks.Task.Delay(500).Wait();
                        }
                    }

                    if (Core.AppUpdateChecker.UpdateAvailable)
                    {
                        string latest = Core.AppUpdateChecker.LatestVersion;
                        Program.Notify(string.Format("Update available!\r\nv{0} is out (running v{1})",
                            latest,
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version), 6000);
                        SimpleLogger.Instance.Info("[AppUpdate] Update notification shown (6s tile).");
                    }
                }
                catch { }
            });
        }

        /// <summary>
        /// EN: [V55/V55r] Arm the BT watchdog timer. If no Wiimote connects within
        /// AutoBtResetDelaySeconds (+ extraDelaySec grace), the service is asked to
        /// reset the BT adapter. Does nothing if the option is disabled.
        /// FR: [V55/V55r] Armer le timer watchdog BT. Si aucune Wiimote ne se
        /// connecte dans AutoBtResetDelaySeconds (+ grâce extraDelaySec), le service
        /// est invité à resetter l'adaptateur BT. Ne fait rien si l'option est désactivée.
        /// </summary>
        private void ArmBtWatchdog(int extraDelaySec = 0)
        {
            if (!Options.Instance.AutoBtResetOnFail) return;

            int delaySec = Math.Max(10, Options.Instance.AutoBtResetDelaySeconds) + extraDelaySec;
            SimpleLogger.Instance.Info($"[BT-Watchdog] Armed: will reset BT in {delaySec}s if no Wiimote connects.");

            // EN/FR: [V55v] Fresh arming: clear the failed-attempt evidence window
            _btAttemptFailedSinceArm = false;
            _btArmUtc = DateTime.UtcNow;

            lock (_btWatchdogLock)
            {
                _btWatchdogTimer?.Dispose();
                _btWatchdogTimer = new System.Threading.Timer(OnBtWatchdogFired, null,
                    delaySec * 1000, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// EN: [V55v] True when there is EVIDENCE of a real failed connection attempt
        /// (or a proven broken radio) since the watchdog was armed. The BT reset must
        /// never fire just because "no Wiimote is connected" — the user may simply
        /// have the Wiimote turned off. Valid evidence:
        /// 1. A Wiimote connection attempt FAILED since arming (ConnectionFailed event);
        /// 2. A REAL Bluetooth inquiry (not an instant/cached enumeration) found a
        ///    discoverable device since arming = a Wiimote was actively in pairing mode;
        /// 3. The radio stack is WEDGED (V53: 8+ consecutive instant empty inquiries) —
        ///    the BT reset automates the official manual fix (disable/enable).
        /// FR: [V55v] True s'il y a PREUVE d'une véritable tentative de connexion échouée
        /// (ou d'une radio cassée prouvée) depuis l'armement du watchdog. Le reset BT ne
        /// doit JAMAIS se déclencher sous prétexte qu'« aucune Wiimote n'est connectée » —
        /// l'utilisateur peut simplement avoir éteint sa Wiimote. Preuves valides :
        /// 1. Une tentative de connexion Wiimote a ÉCHOUÉ depuis l'armement (événement ConnectionFailed) ;
        /// 2. Un inquiry Bluetooth RÉEL (pas une énumération instantanée/cache) a trouvé un
        ///    périphérique discoverable depuis l'armement = une Wiimote était en mode appairage ;
        /// 3. La radio est COINCÉE (V53 : 8+ inquiry instantanés-vides consécutifs) —
        ///    le reset BT automatise la correction manuelle officielle (désactiver/activer).
        /// </summary>
        private bool HasBtConnectionAttemptEvidence()
        {
            // Evidence 1: a connection attempt failed since arming
            // (EN/FR: Preuve 1 : une tentative de connexion a échoué depuis l'armement)
            if (_btAttemptFailedSinceArm) return true;

            // Evidence 2: a REAL inquiry found a discoverable device since arming.
            // An instant enumeration (<300ms, V53 wedge heuristic / remembered-device
            // cache) is NOT proof of a live Wiimote in pairing mode.
            // (EN/FR: Preuve 2 : un inquiry RÉEL a trouvé un périphérique discoverable
            // depuis l'armement. Une énumération instantanée (<300ms, heuristique V53 /
            // cache des périphériques mémorisés) n'est PAS la preuve d'une Wiimote vivante.)
            try
            {
                if (WiimoteLib.Devices.BluetoothDeviceInfo.LastInquiryStartedUtc >= _btArmUtc &&
                    WiimoteLib.Devices.BluetoothDeviceInfo.LastInquiryDurationMs >= 300 &&
                    WiimoteLib.Devices.BluetoothDeviceInfo.LastInquiryDeviceCount > 0)
                {
                    return true;
                }
            }
            catch { }

            // Evidence 3: radio stack wedged (V53) — disable/enable is the official fix
            // (EN/FR: Preuve 3 : pile radio coincée (V53) — désactiver/activer est le correctif officiel)
            if (WiimoteManager.BtWedgeSuspicions >= 8) return true;

            return false;
        }

        /// <summary>
        /// EN: [V55v] ConnectionFailed handler: records a real failed connection attempt.
        /// FR: [V55v] Gestionnaire ConnectionFailed : enregistre une véritable tentative
        /// de connexion échouée.
        /// </summary>
        private void OnWiimoteConnectionFailed(object sender, WiimoteConnectionFailedEventArgs e)
        {
            _btAttemptFailedSinceArm = true;
            SimpleLogger.Instance.Warning("[BT-Watchdog] Connection attempt failed (" + (e.Exception != null ? e.Exception.Message : "unknown error") + ") — recorded as reset evidence.");
        }

        /// <summary>
        /// EN: [V55] Disarm the BT watchdog timer (called when a Wiimote connects).
        /// FR: [V55] Désarmer le timer watchdog BT (appelé quand une Wiimote se connecte).
        /// </summary>
        private void DisarmBtWatchdog()
        {
            // EN/FR: [V55r] A Wiimote connected: the reset cycle succeeded — reset the attempts counter
            _btResetAttempts = 0;

            lock (_btWatchdogLock)
            {
                if (_btWatchdogTimer != null)
                {
                    _btWatchdogTimer.Dispose();
                    _btWatchdogTimer = null;
                    SimpleLogger.Instance.Info("[BT-Watchdog] Disarmed (Wiimote connected).");
                }
            }
        }

        /// <summary>
        /// EN: [V55/V55r] Fired when the watchdog timer expires — triggers BT reset via service.
        /// V55r: growing grace period before re-arming (the BT stack needs time to
        /// re-initialize after a reset: radio + children re-enumeration + pairing
        /// loop) and a hard cap of BtResetMaxAttempts consecutive resets.
        /// FR: [V55/V55r] Déclenché à l'expiration du watchdog — reset BT via service.
        /// V55r : période de grâce croissante avant réarmement (la pile BT a besoin
        /// de temps pour se réinitialiser après un reset : radio + ré-énumération des
        /// enfants + boucle d'appairage) et plafond de BtResetMaxAttempts resets consécutifs.
        /// </summary>
        private void OnBtWatchdogFired(object state)
        {
            lock (_btWatchdogLock) { _btWatchdogTimer = null; }

            // EN: Only reset if still no Wiimote connected and option still enabled
            // FR: Reset seulement si toujours aucune Wiimote et l'option toujours activée
            if (_controllers.Count == 0 && Options.Instance.AutoBtResetOnFail)
            {
                // [V55v] EVIDENCE GATE: never reset when the user is simply not trying
                // to connect (Wiimote off / out of range). The reset is requested ONLY
                // when a real failed connection attempt (or a proven wedged radio) was
                // observed since the watchdog was armed.
                // (EN/FR: PORTE DE PREUVES : jamais de reset si l'utilisateur n'essaie
                // simplement pas de se connecter (Wiimote éteinte / hors de portée). Le
                // reset est demandé SEULEMENT si une véritable tentative de connexion
                // échouée (ou une radio coincée prouvée) a été observée depuis l'armement.)
                if (!HasBtConnectionAttemptEvidence())
                {
                    SimpleLogger.Instance.Info("[BT-Watchdog] No connection attempt detected since arming (Wiimote off or out of range?) — BT reset SKIPPED.");
                    ArmBtWatchdog();
                    return;
                }

                _btResetAttempts++;

                // EN/FR: [V55r] Give up: repeated resets never healed a wedged dongle (V53/V53b)
                if (_btResetAttempts > BtResetMaxAttempts)
                {
                    SimpleLogger.Instance.Error($"[BT-Watchdog] {BtResetMaxAttempts} BT resets without any Wiimote connection — automatic resets STOPPED. " +
                        "Unplug/replug the Bluetooth dongle (see [BT Health] messages) or check the Wiimote batteries, then reconnect.");
                    return;
                }

                SimpleLogger.Instance.Info($"[BT-Watchdog] No Wiimote after delay — requesting BT reset via service (attempt {_btResetAttempts}/{BtResetMaxAttempts}).");
                ServiceClient.RequestBtReset();

                // EN/FR: [V55r] Re-arm with a growing grace period so the BT stack has
                // time to re-initialize after the reset before the next attempt
                ArmBtWatchdog(30 * _btResetAttempts);
            }
            else
            {
                SimpleLogger.Instance.Info("[BT-Watchdog] Fired but Wiimote already connected — no reset needed.");
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
        /// <summary>
        /// EN: Trigger a debounced refresh of all DirectInput indices for active GamePads.
        /// FR: Déclencher un rafraîchissement (anti-rebond) de tous les indices DInput pour les GamePads actifs.
        /// </summary>
        public void RefreshAllDInputIndices()
        {
            // EN: Many hardware events can fire at once. Wait 1s after the last event before scanning.
            // FR: Beaucoup d'événements matériels peuvent arriver d'un coup. Attendre 1s après le dernier avant de scanner.
            if (_refreshTimer == null)
            {
                _refreshTimer = new System.Threading.Timer(_ => 
                {
                    SimpleLogger.Instance.Info("[DInput] Hardware change detected. Refreshing indices...");
                    var controllers = _controllers.ToList(); // Snapshot of active controllers
                    foreach (var controller in controllers)
                    {
                        controller.RefreshDInputIndex(silent: true);
                    }

                    // EN: Automatically update emulator profiles if indices changed
                    // FR: Mettre à jour automatiquement les profils d'émulateur si les index ont changé
                    Core.EmulatorProfileAutomator.UpdateProfiles(controllers);
                }, null, 1000, System.Threading.Timeout.Infinite);
            }
            else
            {
                _refreshTimer.Change(1000, System.Threading.Timeout.Infinite);
            }
        }
        /// <summary>
        /// EN: Swap a connected Wiimote from one player slot to another.
        /// FR: Déplacer une Wiimote connectée d'un slot joueur vers un autre.
        /// If the target slot is occupied, the two Wiimotes are swapped.
        /// Runs on a background thread due to blocking VMulti initialization.
        /// </summary>
        /// <param name="fromPlayer">Source player index (1-4)</param>
        /// <param name="toPlayer">Target player index (1-4)</param>
        /// <param name="onComplete">Callback on UI thread when swap is complete (true=success)</param>
        public void SwapPlayerSlot(int fromPlayer, int toPlayer, Action<bool> onComplete = null)
        {
            if (fromPlayer == toPlayer)
            {
                onComplete?.Invoke(false);
                return;
            }

            var fromCtrl = _controllers.FirstOrDefault(c => c.PlayerIndex == fromPlayer);
            if (fromCtrl == null)
            {
                SimpleLogger.Instance.Warning($"[Swap] No controller at P{fromPlayer}, nothing to swap.");
                onComplete?.Invoke(false);
                return;
            }

            // Block swap if target slot is locked (EN/FR: Bloquer swap si slot cible verrouillé)
            if (Options.Instance.GetLockedSlot(toPlayer))
            {
                SimpleLogger.Instance.Warning($"[Swap] Target slot P{toPlayer} is locked. Swap rejected.");
                Program.PostToUIThread(() => onComplete?.Invoke(false));
                return;
            }

            var toCtrl = _controllers.FirstOrDefault(c => c.PlayerIndex == toPlayer);

            SimpleLogger.Instance.Info($"[Swap] Starting swap P{fromPlayer} → P{toPlayer}" + 
                (toCtrl != null ? $" (P{toPlayer} occupied, will swap)" : ""));

            // Save Wiimote references before dispose (EN/FR: Sauvegarder les références Wiimote avant dispose)
            var fromWiimote = fromCtrl.Wiimote;
            var toWiimote = toCtrl?.Wiimote;

            // 1. Disable VMulti drivers for both slots (EN/FR: Désactiver pilotes VMulti pour les deux slots)
            ServiceClient.DisablePlayer(fromPlayer);
            if (toCtrl != null) ServiceClient.DisablePlayer(toPlayer);

            // 2. Dispose old controllers — cleans up virtual devices but keeps Wiimote connected
            // (EN/FR: Disposer les vieux contrôleurs — nettoie les périphériques virtuels mais garde la Wiimote connectée)
            _controllers.Remove(fromCtrl);
            fromCtrl.Dispose();
            if (toCtrl != null)
            {
                _controllers.Remove(toCtrl);
                toCtrl.Dispose();
            }

            // 3. Recreate on background thread (constructor blocks for VMulti init)
            // (EN/FR: Recréer sur thread arrière-plan car le constructeur bloque pour l'init VMulti)
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Small delay to let Service process the DISABLE commands
                    // (EN/FR: Petit délai pour laisser le Service traiter les commandes DISABLE)
                    System.Threading.Thread.Sleep(500);

                    // Create new controllers with swapped PlayerIndex
                    // (EN/FR: Créer les nouveaux contrôleurs avec PlayerIndex inversés)
                    var newFromCtrl = new WiiMoteController(fromWiimote, toPlayer);
                    _controllers.Add(newFromCtrl);

                    if (toWiimote != null)
                    {
                        var newToCtrl = new WiiMoteController(toWiimote, fromPlayer);
                        _controllers.Add(newToCtrl);
                    }

                    // Cleanup ghost mice (EN/FR: Nettoyer les souris fantômes)
                    ScheduleRemoveUnconnectedMice();

                    SimpleLogger.Instance.Info($"[Swap] Swap complete: P{fromPlayer} ↔ P{toPlayer}");
                    Program.Notify($"Wiimote swap: P{fromPlayer} ↔ P{toPlayer}");

                    // Callback on UI thread (EN/FR: Callback sur thread UI)
                    if (onComplete != null) Program.PostToUIThread(() => onComplete(true));
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[Swap] Swap failed: {ex.Message}");
                    Program.Notify($"Swap failed: {ex.Message}");
                    if (onComplete != null) Program.PostToUIThread(() => onComplete(false));
                }
            });
        }
    }
}
