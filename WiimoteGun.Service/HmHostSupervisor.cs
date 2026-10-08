using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace WiimoteGun.Service
{
    /// <summary>
    /// EN: [V57d] Supervises the HmHost.exe process (UMDF2/HIDMaestro virtual input host).
    ///     The service (SYSTEM) spawns it so no UAC prompt is ever required. Lifecycle
    ///     commands (HM_ACTIVATE / HM_DEACTIVATE / HM_STATUS) arrive on the main service
    ///     pipe and are delegated here. A monitor thread restarts the host if it crashes
    ///     while the input mode wants it active.
    /// FR: [V57d] Supervise le processus HmHost.exe (hôte d'entrée virtuelle UMDF2/HIDMaestro).
    ///     Le service (SYSTEM) le lance : aucun UAC requis. Les commandes de cycle de vie
    ///     (HM_ACTIVATE / HM_DEACTIVATE / HM_STATUS) arrivent sur le pipe principal du service
    ///     et sont déléguées ici. Un thread surveillant relance l'hôte s'il crashe tant que
    ///     le mode d'input le veut actif.
    /// </summary>
    static class HmHostSupervisor
    {
        private const string HostPipeName = "WiimoteGunHmHost";

        private static readonly object _gate = new object();
        private static Process _process;
        private static bool _desiredActive; // EN: what the input mode wants / FR: ce que le mode veut
        private static Thread _monitorThread;
        private static bool _monitorStarted;

        // ------------------------------------------------------------ public API

        /// <summary>
        /// EN: Spawn HmHost if needed and activate the 4 virtual devices (P1..P4).
        ///     Idempotent. First ever run installs driver + self-signed certificate.
        /// FR: Lance HmHost si besoin et active les 4 périphériques virtuels (P1..P4).
        ///     Idempotent. Le tout premier lancement installe le pilote + le certificat auto-signé.
        /// </summary>
        public static string Activate()
        {
            lock (_gate)
            {
                try
                {
                    _desiredActive = true;
                    EnsureMonitorStarted();

                    string status = AskHost("STATUS", 5000);
                    if (status != null && status.StartsWith("OK ACTIVE"))
                        return "OK ALREADY_ACTIVE";

                    if (!IsHostAlive())
                    {
                        string err = StartHost();
                        if (err != null)
                        {
                            Log("Activate: " + err);
                            return "ERR " + err;
                        }
                    }

                    // EN: Generous timeout: first run = cert creation + signing + INF install
                    // FR: Timeout généreux : premier lancement = création cert + signature + install INF
                    string resp = AskHost("ACTIVATE", 90000);
                    if (resp == null)
                    {
                        // [V57d] EN/FR: The host usually died mid-activation - its own log has the stack
                        Log("ACTIVATE: no reply from HmHost - check HmHost\\HmHost.log.");
                        return "ERR HmHost did not reply to ACTIVATE";
                    }
                    if (!resp.StartsWith("OK"))
                        Log("ACTIVATE refused: " + resp);
                    if (resp.StartsWith("OK")) SetPersistedUmdf2Active(true); // [V57h]
                    return resp.StartsWith("OK") ? resp : "ERR " + resp;
                }
                catch (Exception ex)
                {
                    Log("Activate error: " + ex.Message);
                    return "ERR " + ex.Message;
                }
            }
        }

        /// <summary>
        /// EN: Deactivate the virtual devices and let the host process exit cleanly.
        /// FR: Désactive les périphériques virtuels et laisse l'hôte s'arrêter proprement.
        /// </summary>
        public static string Deactivate()
        {
            lock (_gate)
            {
                try
                {
                    _desiredActive = false;
                    SetPersistedUmdf2Active(false); // [V57h]
                    if (!IsHostAlive())
                        return "OK ALREADY_INACTIVE";

                    AskHost("DEACTIVATE", 15000);
                    AskHost("QUIT", 5000);
                    WaitForExit(3000);
                    return "OK DEACTIVATED";
                }
                catch (Exception ex)
                {
                    Log("Deactivate error: " + ex.Message);
                    return "ERR " + ex.Message;
                }
            }
        }

        public static string Status()
        {
            lock (_gate)
            {
                string s = AskHost("STATUS", 5000);
                if (s != null && s.StartsWith("OK"))
                    return s;
                return "OK INACTIVE";
            }
        }

        // ------------------------------------------------------------ [V57e] per-player

        /// <summary>
        /// EN: True when the UMDF2 host owns the virtual devices (input mode active).
        ///     FR: Vrai quand l'hôte UMDF2 possède les périphériques virtuels (mode actif).
        /// </summary>
        public static bool IsActive()
        {
            lock (_gate) { return _desiredActive; }
        }

        /// <summary>
        /// EN: Create the player's virtual device on demand (wiimote connected).
        ///     FR: Crée le périphérique virtuel du joueur à la demande (wiimote connectée).
        /// </summary>
        public static string EnablePlayer(int player)
        {
            string resp = AskHost("ACTIVATE_P:" + player, 30000);
            Log("EnablePlayer(" + player + ") -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// EN: Dispose the player's virtual device (identity preserved).
        ///     FR: Supprime le périphérique virtuel du joueur (identité conservée).
        /// </summary>
        public static string DisablePlayer(int player)
        {
            string resp = AskHost("DEACTIVATE_P:" + player, 15000);
            Log("DisablePlayer(" + player + ") -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// EN: Dispose every player's virtual device (REMOVE_MOUSE_ALL parity).
        ///     FR: Supprime les périphériques virtuels de tous les joueurs (parité REMOVE_MOUSE_ALL).
        /// </summary>
        public static string DisableAllPlayers()
        {
            string resp = AskHost("DEACTIVATE_P_ALL", 15000);
            Log("DisableAllPlayers() -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// EN: Dispose every player's device EXCEPT the listed ones (REMOVE_MOUSE_EXCEPT parity).
        /// FR: Supprime les périphériques de tous les joueurs SAUF ceux listés (parité REMOVE_MOUSE_EXCEPT).
        /// </summary>
        public static void DisableExceptPlayers(string playersCsv)
        {
            string resp = AskHost("DEACTIVATE_P_EXCEPT:" + (playersCsv ?? ""), 15000);
            Log("DisableExceptPlayers(" + playersCsv + ") -> " + (resp ?? "no reply"));
        }

        /// <summary>
        /// [V57h] EN: Hide the player's MOUSE while keeping the keyboard alive (vmulti
        ///     COL03 parity for UMDF2). The host swaps the player's device from the
        ///     mouse+keyboard descriptor to the keyboard-only descriptor at the SAME
        ///     identity key. EnablePlayer (ENABLE_Pn) restores the full device.
        ///     FR: Masque la SOURIS du joueur en gardant le clavier vivant (parité COL03
        ///     vmulti pour UMDF2). L'hôte échange le device du joueur du descripteur
        ///     souris+clavier vers le descripteur clavier-seul à la MÊME clé d'identité.
        ///     EnablePlayer (ENABLE_Pn) restaure le device complet.
        /// </summary>
        public static string HideMouse(int player)
        {
            string resp = AskHost("HIDE_MOUSE_P:" + player, 30000);
            Log("HideMouse(" + player + ") -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// [V57g] EN: Create (or swap) the player's gamepad device on demand - the wiimote
        ///     entered GamePad mode, or the user swapped DInput<->XInput. The api string
        ///     ("DINPUT" or "XINPUT") travels from the app's pipe command; anything other
        ///     than XINPUT normalizes to DINPUT. Generous timeout: device creation is
        ///     ~0.7-1 s and the first XInput activation may bind the XUSB companion.
        ///     FR: Crée (ou échange) le device gamepad du joueur à la demande - la wiimote
        ///     est entrée en mode GamePad, ou l'utilisateur a basculé DInput<->XInput.
        ///     La chaîne api (« DINPUT » ou « XINPUT ») vient de la commande pipe de
        ///     l'app ; toute valeur autre que XINPUT se normalise en DINPUT. Timeout
        ///     généreux : la création prend ~0,7-1 s et la première activation XInput
        ///     peut lier le companion XUSB.
        /// </summary>
        public static string EnableGamepad(int player, string api)
        {
            string a = (api != null && api.Trim().Equals("XINPUT", StringComparison.OrdinalIgnoreCase))
                ? "XINPUT" : "DINPUT";
            string resp = AskHost("ACTIVATE_GP:" + player + ":" + a, 30000);
            Log("EnableGamepad(" + player + ", " + a + ") -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// [V57g] EN: Dispose the player's gamepad device (identity preserved). Used when
        ///     the wiimote goes back to Mouse mode or leaves GamePad flows.
        ///     FR: Supprime le device gamepad du joueur (identité conservée). Utilisé
        ///     quand la wiimote repasse en mode Mouse ou quitte les flux GamePad.
        /// </summary>
        public static string DisableGamepad(int player)
        {
            string resp = AskHost("DEACTIVATE_GP:" + player, 15000);
            Log("DisableGamepad(" + player + ") -> " + (resp ?? "no reply"));
            return resp;
        }

        /// <summary>
        /// [V57k] EN: Clear the "ShowSetupWizardPending" flag written by UpdateService.ps1
        ///     when it replaced a pre-3.0.0.24 service. Called via the WIZARD_ACK pipe
        ///     command once the (non-admin) app has consumed the flag: only the SYSTEM
        ///     service can write HKLM.
        ///     FR: Efface le flag « ShowSetupWizardPending » écrit par UpdateService.ps1
        ///     quand il a remplacé un service antérieur à 3.0.0.24. Appelé via la commande
        ///     pipe WIZARD_ACK une fois que l'app (non-admin) a consommé le flag : seul le
        ///     service SYSTEM peut écrire dans HKLM.
        /// </summary>
        public static void ClearSetupWizardPending()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.CreateSubKey(PersistKeyPath))
                {
                    key.DeleteValue("ShowSetupWizardPending", false);
                }
                Log("ShowSetupWizardPending flag cleared (WIZARD_ACK).");
            }
            catch (Exception ex)
            {
                Log("ClearSetupWizardPending failed: " + ex.Message);
            }
        }

        /// <summary>
        /// EN: Stop the host unconditionally (client exit/crash cleanup, service stop).
        /// FR: Arrêter l'hôte sans condition (nettoyage sortie/crash client, arrêt service).
        /// </summary>
        public static void EnsureStopped(string reason)
        {
            lock (_gate)
            {
                _desiredActive = false;
                SetPersistedUmdf2Active(false); // [V57h]
                if (!IsHostAlive())
                    return;
                Log("EnsureStopped (" + reason + ")");
                try { AskHost("QUIT", 3000); } catch { }
                WaitForExit(3000);
            }
        }

        // ------------------------------------------------------------ [V57h] boot persistence

        // [V57h] EN: The RawInput (UMDF2) mode must survive a PC reboot: when the machine
        //     restarts with UMDF2 selected, the service starts BEFORE the app - without
        //     persistence, ENABLE_P commands arriving before the app's background
        //     HM_ACTIVATE completed fell to the legacy vmulti devcon path (vmulti drivers
        //     activated at wiimote connect - the bug the user reported). The desired state
        //     is persisted in HKLM and re-applied at service start (host spawn only: the
        //     devices are still created on demand per connected wiimote).
        //     FR: Le mode RawInput (UMDF2) doit survivre à un reboot PC : quand la machine
        //     redémarre avec UMDF2 sélectionné, le service démarre AVANT l'app - sans
        //     persistance, les commandes ENABLE_P arrivées avant la fin du HM_ACTIVATE en
        //     arrière-plan de l'app tombaient sur le chemin devcon vmulti legacy (pilotes
        //     vmulti activés à la connexion wiimote - le bug rapporté). L'état désiré est
        //     persisté dans HKLM et réappliqué au démarrage du service (lancement de
        //     l'hôte seulement : les devices restent créés à la demande par wiimote
        //     connectée).
        private const string PersistKeyPath = @"SOFTWARE\WiimoteGun";
        private const string PersistValueName = "Umdf2Active";

        private static void SetPersistedUmdf2Active(bool active)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.CreateSubKey(PersistKeyPath))
                {
                    key.SetValue(PersistValueName, active ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                Log("SetPersistedUmdf2Active(" + active + ") failed: " + ex.Message);
            }
        }

        /// <summary>
        /// [V57h] EN: True when UMDF2 was active at the previous shutdown (HKLM flag).
        ///     Called at service start to re-apply the desired state (background thread).
        ///     FR: Vrai quand UMDF2 était actif à l'arrêt précédent (flag HKLM). Appelé au
        ///     démarrage du service pour réappliquer l'état désiré (thread de fond).
        /// </summary>
        public static bool IsUmdf2PersistedActive()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(PersistKeyPath))
                {
                    if (key == null) return false;
                    object v = key.GetValue(PersistValueName);
                    return v is int i && i == 1;
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// [V57h] EN: Re-apply the persisted UMDF2 state in the background (service start).
        ///     Host spawn + ACTIVATE only - NO devices are created here: they stay created
        ///     on demand per connected wiimote (V57e design).
        ///     FR: Réapplique l'état UMDF2 persisté en arrière-plan (démarrage service).
        ///     Lancement de l'hôte + ACTIVATE seulement - AUCUN device créé ici : ils
        ///     restent créés à la demande par wiimote connectée (design V57e).
        /// </summary>
        public static void ReapplyPersistedState()
        {
            if (!IsUmdf2PersistedActive())
                return;

            new Thread(() =>
            {
                try
                {
                    // EN: Give the machine a moment after boot (BT stack, driver store)
                    //     FR: Laisser un instant à la machine après le boot (pile BT, store
                    //     de pilotes)
                    Thread.Sleep(3000);
                    Log("Boot persistence: UMDF2 was active at shutdown - re-activating host in background.");
                    string resp = Activate();
                    Log("Boot persistence: re-activation -> " + (resp ?? "no reply"));
                }
                catch (Exception ex)
                {
                    Log("Boot persistence error: " + ex.Message);
                }
            })
            { IsBackground = true }.Start();
        }

        // ------------------------------------------------------------ internals

        private static void EnsureMonitorStarted()
        {
            if (_monitorStarted) return;
            _monitorStarted = true;
            _monitorThread = new Thread(MonitorLoop);
            _monitorThread.IsBackground = true;
            _monitorThread.Start();
            Log("Monitor thread started.");
        }

        private static void MonitorLoop()
        {
            // EN: If the host crashes while the input mode wants it active, restart it
            // FR: Si l'hôte crashe alors que le mode le veut actif, le relancer
            while (true)
            {
                Thread.Sleep(5000);
                try
                {
                    lock (_gate)
                    {
                        if (!_desiredActive || IsHostAlive())
                            continue;

                        // [V57d] EN: Log the exit code - HmHost writes its own stack trace to
                        //     HmHost\HmHost.log next to its exe.
                        //     FR: Journaliser le code de sortie - HmHost écrit sa propre trace
                        //     dans HmHost\HmHost.log à côté de son exe.
                        int exitCode = -1;
                        try { exitCode = _process.ExitCode; } catch { }
                        _process = null;
                        Log("HmHost exited while active (code " + exitCode + DescribeHostExit(exitCode) + ") - restarting. Check HmHost\\HmHost.log.");
                        string err = StartHost();
                        if (err != null)
                        {
                            Log("Restart failed: " + err);
                            continue;
                        }
                        string resp = AskHost("ACTIVATE", 90000);
                        Log("Re-ACTIVATE: " + (resp ?? "no reply"));
                    }
                }
                catch (Exception ex)
                {
                    Log("Monitor error: " + ex.Message);
                }
            }
        }

        private static string StartHost()
        {
            // EN: The HmHost publish folder ships next to the service executable
            // FR: Le dossier publish HmHost est livré à côté de l'exécutable du service
            string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HmHost", "HmHost.exe");
            if (!File.Exists(exe))
                return "HmHost.exe not found at " + exe + " (deployment issue)";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exe;
                psi.WorkingDirectory = Path.GetDirectoryName(exe);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                _process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                return "Failed to start HmHost: " + ex.Message;
            }

            // EN: Wait for the host pipe to answer PING (up to 30 s)
            // FR: Attendre que le pipe de l'hôte réponde à PING (jusqu'à 30 s)
            for (int i = 0; i < 150; i++)
            {
                Thread.Sleep(200);
                try
                {
                    if (_process.HasExited)
                    {
                        int exitCode = -1;
                        try { exitCode = _process.ExitCode; } catch { }
                        return "HmHost exited during startup (code " + exitCode + DescribeHostExit(exitCode) + ")";
                    }
                }
                catch { }

                string pong = AskHost("PING", 1000);
                if (pong != null && pong.StartsWith("PONG"))
                    return null;
            }
            return "HmHost pipe not ready after 30 s";
        }

        private static bool IsHostAlive()
        {
            try
            {
                return _process != null && !_process.HasExited;
            }
            catch { return false; }
        }

        private static void WaitForExit(int timeoutMs)
        {
            try
            {
                if (_process != null && !_process.WaitForExit(timeoutMs))
                    _process.Kill();
            }
            catch { }
            try { if (_process != null) _process.Dispose(); } catch { }
            _process = null;
        }

        // [V57h/V57i] EN: Diagnose the apphost exit codes of the FRAMEWORK-DEPENDENT
        //     HmHost.exe: 0x80008082 / 0x80008083 mean the .NET runtime is missing or
        //     mismatched. Per user decision [V57i] HmHost stays framework-dependent: the
        //     .NET 10 runtime is a machine prerequisite, the app checks it at the
        //     RawInput (UMDF2) mode activation and proposes the download (DotNetRuntimeChecker).
        //     HmHost is spawned by the SYSTEM service in session 0, where the apphost's
        //     "You must install .NET" GUI dialog can never be shown to the user - so when
        //     the check was bypassed ("continue anyway"), this log line is the ONLY trace.
        //     FR: Diagnostique les codes de sortie apphost du HmHost.exe
        //     FRAMEWORK-DEPENDENT : 0x80008082 / 0x80008083 signifient que le runtime
        //     .NET est absent ou incompatible. Par décision utilisateur [V57i] HmHost
        //     reste framework-dependent : le runtime .NET 10 est un prérequis machine,
        //     l'app le vérifie à l'activation du mode RawInput (UMDF2) et propose le
        //     téléchargement (DotNetRuntimeChecker). HmHost est lancé par le service
        //     SYSTEM en session 0, où le dialogue GUI apphost « You must install .NET »
        //     ne peut jamais être montré à l'utilisateur - si la vérification a été
        //     contournée (« continuer quand même »), cette ligne de log est le SEUL
        //     indice.
        private static string DescribeHostExit(int exitCode)
        {
            // EN: The apphost exit codes 0x80008082 / 0x80008083 (.NET runtime missing or
            //     mismatched) are returned as NEGATIVE ints by Process.ExitCode.
            //     FR: Les codes de sortie apphost 0x80008082 / 0x80008083 (runtime .NET
            //     absent ou incompatible) arrivent en ints NÉGATIFS via Process.ExitCode.
            if (exitCode == unchecked((int)0x80008082) || exitCode == unchecked((int)0x80008083))
                return " - .NET 10 RUNTIME MISSING: install the .NET Runtime 10.0.x Windows x64 (https://dotnet.microsoft.com/download/dotnet/10.0 - column 'Run apps - Runtime', NOT the SDK)";
            return "";
        }

        private static string AskHost(string command, int connectTimeoutMs)
        {
            try
            {
                using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", HostPipeName, PipeDirection.InOut))
                {
                    pipe.Connect(connectTimeoutMs);
                    using (StreamWriter sw = new StreamWriter(pipe, System.Text.Encoding.UTF8, 1024, true) { AutoFlush = true })
                    {
                        sw.WriteLine(command);
                    }
                    using (StreamReader sr = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 1024, true))
                    {
                        return sr.ReadLine();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static void Log(string message)
        {
            DriverController.Log("[HmHostSupervisor] " + message);
        }
    }
}
