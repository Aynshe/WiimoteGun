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

            // [V55v] EN: Start the crash/hang watchdog (monitors the registered client,
            // restarts WiimoteGun as the interactive user on crash or UI freeze).
            // FR: Démarrer le chien de garde de crash/gel (surveille le client enregistré,
            // relance WiimoteGun en utilisateur interactif en cas de crash ou de gel UI).
            CrashWatchdog.Start();
        }

        public void Stop()
        {
            // [V57d] EN: Stop the UMDF2/HIDMaestro host with the service (no orphan devices)
            //     FR: Stopper l'hôte UMDF2/HIDMaestro avec le service (pas de périphériques orphelins)
            try { HmHostSupervisor.EnsureStopped("service stop"); } catch { }

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

                        // [V57d] EN: Read with leaveOpen so a response line can be written
                        //     back on the same connection (HM_* lifecycle commands).
                        //     FR: Lecture avec leaveOpen pour pouvoir écrire une réponse sur
                        //     la même connexion (commandes de cycle de vie HM_*).
                        string response = null;
                        using (StreamReader sr = new StreamReader(pipeServer, System.Text.Encoding.UTF8, false, 1024, true))
                        {
                            string command = sr.ReadLine();
                            if (!string.IsNullOrEmpty(command))
                            {
                                response = ProcessCommand(command.Trim());
                            }
                        }

                        if (response != null)
                        {
                            try
                            {
                                byte[] payload = System.Text.Encoding.UTF8.GetBytes(response + "\r\n");
                                pipeServer.Write(payload, 0, payload.Length);
                                pipeServer.Flush();
                            }
                            catch { }
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

                            // [V57d] EN: Stop the UMDF2/HIDMaestro host too (crash cleanup, no orphan devices)
                            //     FR: Stopper aussi l'hôte UMDF2/HIDMaestro (nettoyage crash, pas d'orphelins)
                            HmHostSupervisor.EnsureStopped("client crash");
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

        // [V57d] EN: Returns a one-line response for commands that need one
        //     (HM_ACTIVATE / HM_DEACTIVATE / HM_STATUS), or null for the legacy
        //     one-way commands (backward compatible with the existing app).
        //     FR: Retourne une réponse en une ligne pour les commandes qui en
        //     ont besoin (HM_*), ou null pour les commandes historiques à sens
        //     unique (rétrocompatible avec l'app existante).
        private string ProcessCommand(string command)
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

                        // [V55v] EN: Arm the crash/hang watchdog for this client
                        // FR: [V55v] Armer le chien de garde de crash/gel pour ce client
                        CrashWatchdog.OnClientRegistered(pid);

                        DriverController.Log($"[ClientWatcher] Successfully registered client PID: {pid}");
                    }
                    else
                    {
                        DriverController.Log($"[ClientWatcher] ERROR: Failed to parse PID from command: '{command}'");
                    }
                    return null;
                }
                
                // EN: Handle UNREGISTER_CLIENT command (clean shutdown)
                // FR: Gérer la commande UNREGISTER_CLIENT (arrêt propre)
                if (command.StartsWith("UNREGISTER_CLIENT", StringComparison.OrdinalIgnoreCase))
                {
                    bool isRestart = command.Contains(":RESTART");
                    int previousPid;
                    lock (_clientLock)
                    {
                        previousPid = _registeredClientPid;
                        _registeredClientPid = 0;
                    }

                    // [V55v] EN: Deliberate exit — disarm the crash watchdog (never
                    // restart a clean shutdown or a controlled update restart).
                    // FR: [V55v] Sortie volontaire — désarmer le chien de garde de crash
                    // (ne jamais relancer un arrêt propre ou un redémarrage contrôlé).
                    CrashWatchdog.OnClientUnregistered(isRestart);

                    DriverController.Log($"[ClientWatcher] Unregistered client PID: {previousPid} (clean shutdown requested, restart={isRestart})");
                    
                    // EN: If it's a real exit (not a restart), trigger immediate cleanup
                    // FR: S'il s'agit d'une sortie réelle (pas d'un redémarrage), déclencher le nettoyage immédiat
                    if (!isRestart)
                    {
                        DriverController.Log("[ClientWatcher] Real exit detected. Cleaning up all virtual devices...");
                        DriverController.RemoveMouseForAllPlayers();
                        DriverController.RemoveGamepadForAllPlayers();

                        // [V57d] EN: Stop the UMDF2/HIDMaestro host too so no virtual devices linger
                        //     FR: Stopper aussi l'hôte UMDF2/HIDMaestro pour éviter les périphériques orphelins
                        HmHostSupervisor.EnsureStopped("client exit");
                    }
                    
                    return null;
                }
                
                switch (command.ToUpper())
                {
                    case "ENABLE_P1": RoutePlayerEnable(1); break;
                    case "DISABLE_P1": RoutePlayerDisable(1); break;
                    case "ENABLE_P2": RoutePlayerEnable(2); break;
                    case "DISABLE_P2": RoutePlayerDisable(2); break;
                    case "ENABLE_P3": RoutePlayerEnable(3); break;
                    case "DISABLE_P3": RoutePlayerDisable(3); break;
                    case "ENABLE_P4": RoutePlayerEnable(4); break;
                    case "DISABLE_P4": RoutePlayerDisable(4); break;
                    // EN: Cleanup unwanted VMulti collections (COL01, COL02, COL04, COL05, COL06)
                    // FR: Nettoyer les collections VMulti non désirées
                    case "CLEANUP_VMULTI": DriverController.CleanupUnwantedCollections(); break;

                    // [V57k] EN: The app consumed the ShowSetupWizardPending flag written by
                    //     UpdateService.ps1 (pre-3.0.0.24 -> 3.0.0.24+ update): the SYSTEM
                    //     service clears it here (the non-admin app cannot write HKLM).
                    //     FR: L'app a consommé le flag ShowSetupWizardPending écrit par
                    //     UpdateService.ps1 (update pré-3.0.0.24 -> 3.0.0.24+) : le service
                    //     SYSTEM l'efface ici (l'app non-admin ne peut pas écrire HKLM).
                    case "WIZARD_ACK": HmHostSupervisor.ClearSetupWizardPending(); break;

                    // [V55] EN: Reset Bluetooth adapter (disable then re-enable) to unstick the BT stack.
                    // FR: Réinitialiser l'adaptateur Bluetooth (désactiver puis réactiver) pour débloquer la pile BT.
                    case "BT_RESET":
                        bool resetOk = DriverController.ResetBluetoothAdapter();
                        DriverController.Log($"[BT-Reset] Result via pipe command: {(resetOk ? "DONE" : "SKIPPED (cooldown)")}");
                        break;

                    // EN: Remove (hide) COL03 mouse for specific players or all
                    //     [V57h] When the UMDF2 host is active, the per-player REMOVE routes
                    //     to HIDE_MOUSE_P (device swapped to keyboard-only, vmulti COL03
                    //     parity: the mouse disappears, the keyboard stays) instead of the
                    //     vmulti devcon path.
                    //     FR: Supprimer (masquer) la souris COL03 pour joueurs spécifiques
                    //     ou tous. [V57h] Quand l'hôte UMDF2 est actif, le REMOVE par
                    //     joueur route vers HIDE_MOUSE_P (device échangé en clavier-seul,
                    //     parité COL03 vmulti : la souris disparaît, le clavier reste) au
                    //     lieu du chemin vmulti devcon.
                    case "REMOVE_MOUSE_ALL": RouteRemoveAll(); break;
                    case "REMOVE_MOUSE_P1": RouteMouseRemove(1); break;
                    case "REMOVE_MOUSE_P2": RouteMouseRemove(2); break;
                    case "REMOVE_MOUSE_P3": RouteMouseRemove(3); break;
                    case "REMOVE_MOUSE_P4": RouteMouseRemove(4); break;

                    // EN: Enable/Remove Col06 gamepad for specific players.
                    //     [V57g] When the UMDF2 host is active these create/dispose the
                    //     player's HIDMaestro gamepad device instead (api from the
                    //     ":DINPUT|:XINPUT" suffix; the unsuffixed legacy form defaults
                    //     to DINPUT). The vmulti devcon path only runs when HmHost is
                    //     not active.
                    //     FR: Activer/Supprimer le gamepad Col06 pour joueurs spécifiques.
                    //     [V57g] Quand l'hôte UMDF2 est actif, ces commandes créent/
                    //     suppriment le device gamepad HIDMaestro du joueur à la place
                    //     (api depuis le suffixe « :DINPUT|:XINPUT » ; la forme legacy
                    //     sans suffixe vaut DINPUT). Le chemin vmulti devcon ne s'applique
                    //     que si HmHost n'est pas actif.
                    case "ENABLE_GAMEPAD_P1": RouteGamepadEnable(1, "DINPUT"); break;
                    case "ENABLE_GAMEPAD_P2": RouteGamepadEnable(2, "DINPUT"); break;
                    case "ENABLE_GAMEPAD_P3": RouteGamepadEnable(3, "DINPUT"); break;
                    case "ENABLE_GAMEPAD_P4": RouteGamepadEnable(4, "DINPUT"); break;
                    case "REMOVE_GAMEPAD_P1": RouteGamepadDisable(1); break;
                    case "REMOVE_GAMEPAD_P2": RouteGamepadDisable(2); break;
                    case "REMOVE_GAMEPAD_P3": RouteGamepadDisable(3); break;
                    case "REMOVE_GAMEPAD_P4": RouteGamepadDisable(4); break;

                    // [V57d] EN: UMDF2/HIDMaestro virtual input lifecycle. Delegated to the
                    //     HmHost supervisor, which spawns/stops the .NET 10 host process
                    //     (SYSTEM child, no UAC ever). These are the only pipe commands
                    //     that get a response line.
                    //     FR: Cycle de vie de l'entrée virtuelle UMDF2/HIDMaestro. Délégué
                    //     au superviseur HmHost, qui lance/arrête le processus hôte .NET 10
                    //     (enfant SYSTEM, jamais d'UAC). Seules commandes du pipe qui
                    //     reçoivent une ligne de réponse.
                    case "HM_ACTIVATE": return HmHostSupervisor.Activate();
                    case "HM_DEACTIVATE": return HmHostSupervisor.Deactivate();
                    case "HM_STATUS": return HmHostSupervisor.Status();
                    default:
                        // EN: Handle REMOVE_MOUSE_EXCEPT:1,2 format
                        // FR: Gérer le format REMOVE_MOUSE_EXCEPT:1,2
                        if (command.ToUpper().StartsWith("REMOVE_MOUSE_EXCEPT:"))
                        {
                            string players = command.Substring("REMOVE_MOUSE_EXCEPT:".Length);
                            RouteRemoveExcept(players);
                        }
                        // [V57g] EN: Suffixed gamepad commands from the UMDF2-capable app:
                        //     ENABLE_GAMEPAD_P<n>:DINPUT|XINPUT (the api travels with the
                        //     command because only the app knows the player's profile
                        //     checkbox / the modal swap state).
                        //     FR: Commandes gamepad avec suffixe depuis l'app compatible
                        //     UMDF2 : ENABLE_GAMEPAD_P<n>:DINPUT|XINPUT (l'api voyage avec
                        //     la commande car seule l'app connaît la case profil du
                        //     joueur / l'état de la bascule de la modale).
                        else if (command.ToUpper().StartsWith("ENABLE_GAMEPAD_P"))
                        {
                            int player;
                            string api;
                            if (TryParseGamepadCommand(command, "ENABLE_GAMEPAD_P", out player, out api))
                                RouteGamepadEnable(player, api);
                        }
                        break;
                }

                // [V57d] EN/FR: legacy commands stay one-way (no response)
                return null;
            }
            catch (Exception ex)
            {
                DriverController.Log("Error processing command: " + ex.Message);
                return "ERR " + ex.Message;
            }
        }

        // ------------------------------------------------------------ [V57e] routing

        // [V57e] EN: When the UMDF2/HIDMaestro host is active it owns the virtual devices:
        //     player enable/disable/remove commands are routed to HmHost, which CREATES
        //     or DISPOSES the player's device on demand (stable identity, devices exist
        //     ONLY for connected wiimotes - exactly like the vmulti enable/disable flow).
        //     Otherwise the legacy vmulti devcon path applies unchanged.
        //     FR: Quand l'hôte UMDF2/HIDMaestro est actif il possède les périphériques
        //     virtuels : les commandes joueur sont routées vers HmHost, qui CRÉE ou
        //     SUPPRIME le device du joueur à la demande (identité stable, les devices
        //     n'existent QUE pour les wiimotes connectées - exactement comme le flux
        //     enable/disable vmulti). Sinon le chemin historique vmulti devcon s'applique.
        private static void RoutePlayerEnable(int player)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.EnablePlayer(player);
            else DriverController.EnablePlayer(player);
        }

        private static void RoutePlayerDisable(int player)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.DisablePlayer(player);
            else DriverController.DisablePlayer(player);
        }

        private static void RouteRemoveAll()
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.DisableAllPlayers();
            else DriverController.RemoveMouseForAllPlayers();
        }

        private static void RouteRemoveExcept(string players)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.DisableExceptPlayers(players);
            else DriverController.RemoveMouseExceptPlayers(players);
        }

        // [V57h] EN: Per-player mouse removal. UMDF2: HIDE_MOUSE_P (kb-only swap - the
        //     mouse disappears from the system, the keyboard keeps working, exactly like
        //     vmulti hiding COL03 while keeping COL02). VMulti: the legacy devcon path.
        //     FR: Suppression souris par joueur. UMDF2 : HIDE_MOUSE_P (échange kb-seul -
        //     la souris disparaît du système, le clavier continue de fonctionner,
        //     exactement comme vmulti masquant COL03 en gardant COL02). VMulti : le
        //     chemin devcon legacy.
        private static void RouteMouseRemove(int player)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.HideMouse(player);
            else DriverController.RemoveMouseForPlayer(player);
        }

        // [V57g] EN: Gamepad enable/disable follows the same routing rule as the player
        //     devices: HmHost owns them while active (created only while the wiimote is
        //     in GamePad mode, swapped DInput<->XInput at a stable identity), otherwise
        //     the vmulti Col06 devcon path applies. The api string is normalized by the
        //     supervisor; the vmulti branch ignores it.
        //     FR: L'activation/désactivation gamepad suit la même règle de routage que
        //     les devices joueur : HmHost les possède quand il est actif (créés
        //     seulement tant que la wiimote est en mode GamePad, swap DInput<->XInput
        //     à identité stable), sinon le chemin vmulti Col06 devcon s'applique. La
        //     chaîne api est normalisée par le superviseur ; la branche vmulti
        //     l'ignore.
        private static void RouteGamepadEnable(int player, string api)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.EnableGamepad(player, api);
            else DriverController.EnableGamepadForPlayer(player);
        }

        private static void RouteGamepadDisable(int player)
        {
            if (HmHostSupervisor.IsActive()) HmHostSupervisor.DisableGamepad(player);
            else DriverController.RemoveGamepadForPlayer(player);
        }

        // [V57g] EN: Parse "ENABLE_GAMEPAD_P<n>[:DINPUT|XINPUT]" (api optional,
        //     DINPUT default - the unsuffixed form keeps the legacy vmulti flow
        //     meaning). Returns false on a malformed player number.
        //     FR: Analyse « ENABLE_GAMEPAD_P<n>[:DINPUT|XINPUT] » (api optionnelle,
        //     DINPUT par défaut - la forme sans suffixe conserve le sens du flux
        //     legacy vmulti). Renvoie faux si le numéro de joueur est mal formé.
        private static bool TryParseGamepadCommand(string command, string prefix, out int player, out string api)
        {
            player = 0;
            api = "DINPUT";
            try
            {
                string rest = command.Substring(prefix.Length);
                int colon = rest.IndexOf(':');
                string playerPart = colon >= 0 ? rest.Substring(0, colon) : rest;
                string apiPart = colon >= 0 ? rest.Substring(colon + 1) : "";
                if (!int.TryParse(playerPart, out player) || player < 1 || player > 4)
                    return false;
                if (apiPart.Trim().Equals("XINPUT", StringComparison.OrdinalIgnoreCase))
                    api = "XINPUT";
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
