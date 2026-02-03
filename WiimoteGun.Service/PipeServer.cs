using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace WiimoteGun.Service
{
    public class PipeServer
    {
        private Thread _serverThread;
        private Thread _clientWatcherThread;
        private bool _isRunning;
        private const string PIPE_NAME = "WiimoteGunService";
        
        // EN: Registered client (WiimoteGun) process ID for monitoring
        // FR: ID de processus client (WiimoteGun) enregistré pour surveillance
        private int _registeredClientPid = 0;
        private readonly object _clientLock = new object();

        public void Start()
        {
            _isRunning = true;
            _serverThread = new Thread(ServerLoop);
            _serverThread.IsBackground = true;
            _serverThread.Start();
            
            // EN: Start client watcher thread / FR: Démarrer le thread de surveillance client
            _clientWatcherThread = new Thread(ClientWatcherLoop);
            _clientWatcherThread.IsBackground = true;
            _clientWatcherThread.Start();
            DriverController.Log("[ClientWatcher] Thread started.");
        }

        public void Stop()
        {
            _isRunning = false;
            // Connect dummy client to unblock WaitConnection if needed, or just Abort if stuck (Service stop needs to be fast)
            try 
            {
                // Force abort for immediate stop during service shutdown
                if (_serverThread != null && _serverThread.IsAlive)
                    _serverThread.Abort();
                if (_clientWatcherThread != null && _clientWatcherThread.IsAlive)
                    _clientWatcherThread.Abort();
            } 
            catch {}
        }

        private void ServerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // Create pipe with security allowing Authenticated Users to connect
                    PipeSecurity ps = new PipeSecurity();
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    ps.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 1024, 1024, ps))
                    {
                        pipeServer.WaitForConnection();

                        using (StreamReader sr = new StreamReader(pipeServer))
                        {
                            string command = sr.ReadLine();
                            if (!string.IsNullOrEmpty(command))
                            {
                                ProcessCommand(command.Trim());
                            }
                        }
                    }
                }
                catch (ThreadAbortException) { return; }
                catch (Exception ex)
                {
                    // Log error but continue loop (backoff to prevent tight loop spin on error)
                    DriverController.Log("Pipe Error: " + ex.Message);
                    Thread.Sleep(2000); 
                }
            }
        }

        /// <summary>
        /// EN: Watch for client process exit and trigger cleanup.
        /// FR: Surveiller la sortie du processus client et déclencher le nettoyage.
        /// </summary>
        private void ClientWatcherLoop()
        {
            while (_isRunning)
            {
                try
                {
                    int pid;
                    lock (_clientLock)
                    {
                        pid = _registeredClientPid;
                    }

                    if (pid > 0)
                    {
                        // EN: Check if process is still running / FR: Vérifier si le processus tourne encore
                        try
                        {
                            Process clientProcess = Process.GetProcessById(pid);
                            // Process exists, continue monitoring
                        }
                        catch (ArgumentException)
                        {
                            // EN: Process no longer exists - WiimoteGun closed/crashed
                            // FR: Processus n'existe plus - WiimoteGun fermé/crashé
                            DriverController.Log($"[ClientWatcher] Client process {pid} exited! Triggering COL03 cleanup...");
                            
                            // EN: Clear registered client / FR: Effacer le client enregistré
                            lock (_clientLock)
                            {
                                _registeredClientPid = 0;
                            }
                            
                            // EN: Trigger cleanup (respects IsDeviceEnabled check already in DriverController)
                            // FR: Déclencher le nettoyage (respecte déjà la vérification IsDeviceEnabled dans DriverController)
                            DriverController.RemoveMouseForAllPlayers();
                            DriverController.RemoveGamepadForAllPlayers();
                        }
                    }

                    // EN: Check every 2 seconds / FR: Vérifier toutes les 2 secondes
                    Thread.Sleep(2000);
                }
                catch (ThreadAbortException) { return; }
                catch (Exception ex)
                {
                    DriverController.Log("ClientWatcher Error: " + ex.Message);
                    Thread.Sleep(5000);
                }
            }
        }

        private void ProcessCommand(string command)
        {
            try
            {
                DriverController.Log("Service received command: " + command);
                
                // EN: Handle REGISTER_CLIENT:PID command to register client for monitoring
                // FR: Gérer la commande REGISTER_CLIENT:PID pour enregistrer le client à surveiller
                if (command.StartsWith("REGISTER_CLIENT:", StringComparison.OrdinalIgnoreCase))
                {
                    string pidStr = command.Substring("REGISTER_CLIENT:".Length).Trim();
                    if (int.TryParse(pidStr, out int pid))
                    {
                        lock (_clientLock)
                        {
                            _registeredClientPid = pid;
                        }
                        
                        // EN: Reset enabled players list when new client connects (fresh session)
                        // FR: Réinitialiser la liste des joueurs activés quand un nouveau client se connecte (nouvelle session)
                        DriverController.ResetEnabledPlayers();
                        
                        DriverController.Log($"[ClientWatcher] Successfully registered client PID: {pid}");
                    }
                    else
                    {
                        DriverController.Log($"[ClientWatcher] ERROR: Failed to parse PID from command: '{command}'");
                    }
                    return;
                }
                
                // EN: Handle UNREGISTER_CLIENT command (clean shutdown)
                // FR: Gérer la commande UNREGISTER_CLIENT (arrêt propre)
                if (string.Equals(command, "UNREGISTER_CLIENT", StringComparison.OrdinalIgnoreCase))
                {
                    int previousPid;
                    lock (_clientLock)
                    {
                        previousPid = _registeredClientPid;
                        _registeredClientPid = 0;
                    }
                    DriverController.Log($"[ClientWatcher] Unregistered client PID: {previousPid} (clean shutdown requested)");
                    return;
                }
                
                switch (command.ToUpper())
                {
                    case "ENABLE_P1": DriverController.EnablePlayer(1); break;
                    case "DISABLE_P1": DriverController.DisablePlayer(1); break;
                    case "ENABLE_P2": DriverController.EnablePlayer(2); break;
                    case "DISABLE_P2": DriverController.DisablePlayer(2); break;
                    case "ENABLE_P3": DriverController.EnablePlayer(3); break;
                    case "DISABLE_P3": DriverController.DisablePlayer(3); break;
                    case "ENABLE_P4": DriverController.EnablePlayer(4); break;
                    case "DISABLE_P4": DriverController.DisablePlayer(4); break;
                    // EN: Cleanup unwanted VMulti collections (COL01, COL02, COL04, COL05, COL06)
                    // FR: Nettoyer les collections VMulti non désirées
                    case "CLEANUP_VMULTI": DriverController.CleanupUnwantedCollections(); break;

                    // EN: Remove (hide) COL03 mouse for specific players or all
                    // FR: Supprimer (masquer) COL03 souris pour joueurs spécifiques ou tous
                    case "REMOVE_MOUSE_ALL": DriverController.RemoveMouseForAllPlayers(); break;
                    case "REMOVE_MOUSE_P1": DriverController.RemoveMouseForPlayer(1); break;
                    case "REMOVE_MOUSE_P2": DriverController.RemoveMouseForPlayer(2); break;
                    case "REMOVE_MOUSE_P3": DriverController.RemoveMouseForPlayer(3); break;
                    case "REMOVE_MOUSE_P4": DriverController.RemoveMouseForPlayer(4); break;

                    // EN: Enable/Remove Col06 gamepad for specific players
                    // FR: Activer/Supprimer gamepad Col06 pour joueurs spécifiques
                    case "ENABLE_GAMEPAD_P1": DriverController.EnableGamepadForPlayer(1); break;
                    case "ENABLE_GAMEPAD_P2": DriverController.EnableGamepadForPlayer(2); break;
                    case "ENABLE_GAMEPAD_P3": DriverController.EnableGamepadForPlayer(3); break;
                    case "ENABLE_GAMEPAD_P4": DriverController.EnableGamepadForPlayer(4); break;
                    case "REMOVE_GAMEPAD_P1": DriverController.RemoveGamepadForPlayer(1); break;
                    case "REMOVE_GAMEPAD_P2": DriverController.RemoveGamepadForPlayer(2); break;
                    case "REMOVE_GAMEPAD_P3": DriverController.RemoveGamepadForPlayer(3); break;
                    case "REMOVE_GAMEPAD_P4": DriverController.RemoveGamepadForPlayer(4); break;
                    default:
                        // EN: Handle REMOVE_MOUSE_EXCEPT:1,2 format
                        // FR: Gérer le format REMOVE_MOUSE_EXCEPT:1,2
                        if (command.ToUpper().StartsWith("REMOVE_MOUSE_EXCEPT:"))
                        {
                            string players = command.Substring("REMOVE_MOUSE_EXCEPT:".Length);
                            DriverController.RemoveMouseExceptPlayers(players);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                DriverController.Log("Error processing command: " + ex.Message);
            }
        }
    }
}
