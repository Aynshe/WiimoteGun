using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

namespace WiimoteGun
{
    public static class ServiceClient
    {
        private const string PIPE_NAME = "WiimoteGunService";

        private static readonly object _lock = new object();
        private static Task _lastTask = CreateCompletedTask();

        private static Task CreateCompletedTask()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public static void SendCommand(string command)
        {
            lock (_lock)
            {
                _lastTask = _lastTask.ContinueWith(delegate(Task _)
                {
                    try
                    {
                        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut))
                        {
                            // EN: Increased timeout to 10000ms. Service DEVCON commands (ex: CLEANUP_VMULTI) can take ~3-4 seconds
                            // FR: Augmentation du timeout à 10000ms. Les commandes DEVCON du Service (ex: CLEANUP_VMULTI) peuvent prendre ~3-4 secondes
                            pipeClient.Connect(10000); 
                            using (StreamWriter sw = new StreamWriter(pipeClient))
                            {
                                sw.AutoFlush = true;
                                sw.WriteLine(command);
                            }
                        }
                        SimpleLogger.Instance.Info(string.Format("Service Command Sent: {0}", command));
                    }
                    catch (Exception ex)
                    {
                        // Service likely not running or not installed
                        SimpleLogger.Instance.Debug(string.Format("Service IPC failed ({0}): {1}", command, ex.Message));
                    }
                });
            }
        }

        public static void EnablePlayer(int index) { SendCommand(string.Format("ENABLE_P{0}", index)); }
        public static void DisablePlayer(int index) { SendCommand(string.Format("DISABLE_P{0}", index)); }
        
        /// <summary>
        /// EN: Request service to cleanup unwanted VMulti collections (requires admin via service).
        /// FR: Demander au service de nettoyer les collections VMulti non désirées (nécessite admin via service).
        /// </summary>
        public static void CleanupVMulti() { SendCommand("CLEANUP_VMULTI"); }

        /// <summary>
        /// EN: Disable (hide) COL03 mouse for all players at startup.
        /// FR: Désactiver (masquer) COL03 souris pour tous les joueurs au démarrage.
        /// </summary>
        public static void RemoveMouseForAllPlayers() { SendCommand("REMOVE_MOUSE_ALL"); }

        /// <summary>
        /// EN: Disable (hide) COL03 mouse for a specific player.
        /// FR: Désactiver (masquer) COL03 souris pour un joueur spécifique.
        /// </summary>
        public static void RemoveMouseForPlayer(int playerIndex) { SendCommand(string.Format("REMOVE_MOUSE_P{0}", playerIndex)); }

        /// <summary>
        /// EN: Disable (hide) COL03 mouse for all players EXCEPT those connected.
        /// FR: Désactiver (masquer) COL03 souris pour tous les joueurs SAUF ceux connectés.
        /// </summary>
        public static void RemoveMouseExceptPlayers(int[] connectedPlayerIndexes)
        {
            string players = string.Join(",", connectedPlayerIndexes);
            SendCommand(string.Format("REMOVE_MOUSE_EXCEPT:{0}", players));
        }

        /// <summary>
        /// EN: Register the current process with the service for crash/exit monitoring.
        /// FR: Enregistrer le processus actuel auprès du service pour la surveillance crash/sortie.
        /// When the process exits, the service will trigger COL03 cleanup.
        /// </summary>
        public static void RegisterClient()
        {
            int pid = Process.GetCurrentProcess().Id;
            SendCommand(string.Format("REGISTER_CLIENT:{0}", pid));
        }

        /// <summary>
        /// EN: Unregister the current process from the service (called on clean shutdown).
        /// FR: Désenregistrer le processus actuel du service (appelé lors d'un arrêt propre).
        /// </summary>
        /// <param name="isRestarting">EN: True if the app is restarting / FR: Vrai si l'app redémarre</param>
        public static void UnregisterClient(bool isRestarting = false) 
        { 
            if (isRestarting)
                SendCommand("UNREGISTER_CLIENT:RESTART");
            else
                SendCommand("UNREGISTER_CLIENT"); 
        }

        // ========== GamePad Mode Col06 Commands (EN/FR: Commandes GamePad Mode Col06) ==========

        /// <summary>
        /// EN: Enable Col06 gamepad device for a specific player.
        /// FR: Activer le périphérique gamepad Col06 pour un joueur spécifique.
        /// </summary>
        public static void EnableGamepad(int playerIndex) { SendCommand(string.Format("ENABLE_GAMEPAD_P{0}", playerIndex)); }

        /// <summary>
        /// [V57g] EN: Enable the gamepad device for a specific player WITH the output API
        ///     ("DINPUT" or "XINPUT"). In RawInputUmdf mode the service routes this to
        ///     HmHost (ACTIVATE_GP:<p>:<api>), which creates the HIDMaestro gamepad device
        ///     (or swaps DInput<->XInput at a stable identity); on the vmulti path the
        ///     suffix is ignored. The unsuffixed overload above keeps the exact legacy
        ///     command for the vmulti flows (old services ignore the suffixed form).
        ///     FR: Active le device gamepad pour un joueur spécifique AVEC l'api de sortie
        ///     (« DINPUT » ou « XINPUT »). En mode RawInputUmdf le service route vers
        ///     HmHost (ACTIVATE_GP:<p>:<api>), qui crée le device gamepad HIDMaestro (ou
        ///     échange DInput<->XInput à identité stable) ; sur le chemin vmulti le
        ///     suffixe est ignoré. La surcharge sans suffixe ci-dessus conserve la
        ///     commande legacy exacte pour les flux vmulti (les vieux services ignorent
        ///     la forme suffixée).
        /// </summary>
        public static void EnableGamepad(int playerIndex, string api)
        {
            string a = (api != null && api.Trim().Equals("XINPUT", StringComparison.OrdinalIgnoreCase)) ? "XINPUT" : "DINPUT";
            SendCommand(string.Format("ENABLE_GAMEPAD_P{0}:{1}", playerIndex, a));
        }

        /// <summary>
        /// EN: Remove (disable) Col06 gamepad device for a specific player.
        /// FR: Supprimer (désactiver) le périphérique gamepad Col06 pour un joueur spécifique.
        /// </summary>
        public static void RemoveGamepad(int playerIndex) { SendCommand(string.Format("REMOVE_GAMEPAD_P{0}", playerIndex)); }

        /// <summary>
        /// EN: [V55] Ask the service to reset the Windows Bluetooth adapter (disable then re-enable).
        /// This is used when the BT stack hangs and Wiimotes cannot reconnect.
        /// The service handles the 30-second cooldown internally.
        /// FR: [V55] Demander au service de réinitialiser l'adaptateur Bluetooth Windows (disable/enable).
        /// Utilisé quand la pile BT se bloque et que les Wiimotes ne peuvent pas se reconnecter.
        /// Le service gère le cooldown de 30s en interne.
        /// </summary>
        public static void RequestBtReset()
        {
            SimpleLogger.Instance.Info("[BT-Reset] Sending BT_RESET to service...");
            SendCommand("BT_RESET");
        }

        // ========== [V57d] UMDF2/HIDMaestro RawInput Mode (EN/FR: Mode RawInput UMDF2/HIDMaestro) ==========

        /// <summary>
        /// EN: Send a command to the service and read back its one-line reply
        ///     (HM_* lifecycle commands). Returns null when the service is not
        ///     running or did not reply in time.
        /// FR: Envoie une commande au service et lit sa réponse d'une ligne
        ///     (commandes de cycle de vie HM_*). Retourne null si le service ne
        ///     tourne pas ou n'a pas répondu à temps.
        /// </summary>
        public static string SendCommandWithResponse(string command, int timeoutMs = 30000)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut))
                {
                    pipeClient.Connect(timeoutMs);
                    using (StreamWriter sw = new StreamWriter(pipeClient, System.Text.Encoding.UTF8, 1024, true) { AutoFlush = true })
                    {
                        sw.WriteLine(command);
                    }
                    using (StreamReader sr = new StreamReader(pipeClient, System.Text.Encoding.UTF8, false, 1024, true))
                    {
                        string response = sr.ReadLine();
                        SimpleLogger.Instance.Info(string.Format("Service Response '{0}' -> {1}", command, response ?? "(none)"));
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Debug(string.Format("Service response IPC failed ({0}): {1}", command, ex.Message));
                return null;
            }
        }

        /// <summary>
        /// EN: Ask the service to activate the UMDF2/HIDMaestro virtual devices (P1..P4).
        ///     First ever run installs the driver and its self-signed certificate (a few seconds).
        /// FR: Demande au service d'activer les périphériques virtuels UMDF2/HIDMaestro (P1..P4).
        ///     Le tout premier lancement installe le pilote et son certificat auto-signé (quelques secondes).
        /// </summary>
        public static string HmActivate()
        {
            // EN: Generous timeout: first ever activation installs driver + certificate
            // FR: Timeout généreux : la toute première activation installe pilote + certificat
            return SendCommandWithResponse("HM_ACTIVATE", 120000);
        }

        /// <summary>
        /// EN: Ask the service to deactivate and stop the UMDF2 host (mode switch/exit).
        /// FR: Demande au service de désactiver et d'arrêter l'hôte UMDF2 (changement de mode/sortie).
        /// </summary>
        public static void HmDeactivate()
        {
            SendCommand("HM_DEACTIVATE");
        }

        /// <summary>
        /// EN: True when the UMDF2 host is active (devices P1..P4 live).
        /// FR: Vrai quand l'hôte UMDF2 est actif (P1..P4 vivants).
        /// </summary>
        public static bool HmIsActive()
        {
            string resp = SendCommandWithResponse("HM_STATUS", 5000);
            return resp != null && resp.StartsWith("OK ACTIVE");
        }

        /// <summary>
        /// [V57g] EN: Raw one-line reply of HM_STATUS (e.g. "OK ACTIVE GP=1D,2X,3-,4-"
        ///     with a V57g host, "OK ACTIVE"/"OK INACTIVE" with an older one). Used by
        ///     HmGamepad to wait for the player's gamepad device to come live.
        ///     FR: Réponse brute d'une ligne de HM_STATUS (ex. « OK ACTIVE GP=1D,2X,3-,4- »
        ///     avec un hôte V57g, « OK ACTIVE »/« OK INACTIVE » avec un ancien). Utilisée
        ///     par HmGamepad pour attendre que le device gamepad du joueur soit vivant.
        /// </summary>
        public static string HmStatusRaw()
        {
            return SendCommandWithResponse("HM_STATUS", 5000);
        }

        // ========== Service Version Management (EN/FR: Gestion Version Service) ==========

        private const string SERVICE_NAME = "WiimoteGunHelper";
        private const string UPDATE_SUBFOLDER = @"WiimoteGun.Service\update_service";

        /// <summary>
        /// EN: Checks if the installed service is outdated and prompts the user to update.
        /// FR: Vérifie si le service installé est obsolète et invite l'utilisateur à le mettre à jour.
        /// </summary>
        public static void CheckAndPromptServiceUpdate()
        {
            try
            {
                string installedServicePath = GetInstalledServicePath();
                if (string.IsNullOrEmpty(installedServicePath) || !File.Exists(installedServicePath))
                {
                    SimpleLogger.Instance.Debug("Service not found in registry, skipping version check.");
                    return;
                }

                FileVersionInfo installedVersion = FileVersionInfo.GetVersionInfo(installedServicePath);
                SimpleLogger.Instance.Info(string.Format("Service Version (Installed): {0}", installedVersion.FileVersion));
                
                // Get packaged version (relative to main app)
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string packagedServicePath = Path.Combine(appDir, UPDATE_SUBFOLDER, "WiimoteGun.Service.exe");

                if (!File.Exists(packagedServicePath))
                {
                    SimpleLogger.Instance.Debug("No update service EXE found in " + UPDATE_SUBFOLDER + ", skipping.");
                    return;
                }

                FileVersionInfo packagedVersion = FileVersionInfo.GetVersionInfo(packagedServicePath);

                Version vInstalled = new Version(installedVersion.FileVersion);
                Version vPackaged = new Version(packagedVersion.FileVersion);

                if (vPackaged > vInstalled)
                {
                    SimpleLogger.Instance.Info(string.Format("Service update available! Installed: {0}, Packaged: {1}", vInstalled, vPackaged));
                    
                    string msg = string.Format(
                        "A new version of the WiimoteGun Helper Service is available.\n\n" +
                        "Installed: {0}\n" +
                        "New Version: {1}\n\n" +
                        "Do you want to update the service now? (Requires Admin rights)\n\n" +
                        "Une nouvelle version du Service WiimoteGun est disponible.\n\n" +
                        "Voulez-vous mettre à jour le service maintenant ? (Nécessite les droits Admin)",
                        vInstalled, vPackaged);

                    if (System.Windows.Forms.MessageBox.Show(msg, "Service Update", 
                        System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                    {
                        TriggerServiceUpdate(installedServicePath, packagedServicePath);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("Error during service version check: " + ex.Message);
            }
        }

        /// <summary>
        /// [V57k] EN: File version of the INSTALLED WiimoteGun.Service exe (e.g.
        ///     "3.0.0.24"), or null when the service is not found. Used by the app start
        ///     to re-show the Setup Wizard once after the service was updated from a
        ///     pre-3.0.0.24 version.
        ///     FR: Version de fichier du WiimoteGun.Service INSTALLÉ (ex. « 3.0.0.24 »),
        ///     ou null si le service est introuvable. Utilisée au démarrage de l'app pour
        ///     ré-afficher le Setup Wizard une fois après une mise à jour du service depuis
        ///     une version pré-3.0.0.24.
        /// </summary>
        public static string GetInstalledServiceVersion()
        {
            try
            {
                string path = GetInstalledServicePath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return null;
                return System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
            }
            catch
            {
                return null;
            }
        }

        private static string GetInstalledServicePath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + SERVICE_NAME))
                {
                    if (key != null)
                    {
                        string imagePath = key.GetValue("ImagePath") as string;
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            // Remove quotes if present
                            return imagePath.Replace("\"", "").Trim();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static void TriggerServiceUpdate(string installedServicePath, string packagedServicePath)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string scriptDir = Path.Combine(appDir, "WiimoteGun.Service");
                string scriptPath = Path.Combine(scriptDir, "UpdateService.ps1");

                // Launch the PowerShell script as admin, passing the installation destination path
                if (File.Exists(scriptPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -File \"{0}\" -ServicePath \"{1}\"", scriptPath, installedServicePath),
                        Verb = "runas", // Force Admin
                        UseShellExecute = true,
                        WorkingDirectory = scriptDir
                    };
                    Process.Start(psi);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Update script not found: " + scriptPath, "Error", 
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Failed to trigger update: " + ex.Message, "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// EN: [V57d] Persistent high-frequency frame pipe to HmHost (UMDF2 virtual input).
    ///     One connection is kept open and reused for all players' mouse/keyboard
    ///     frames; auto-reconnects silently and drops frames while the host is down.
    /// FR: [V57d] Pipe persistant haute fréquence vers HmHost (entrée virtuelle UMDF2).
    ///     Une connexion unique réutilisée pour les frames souris/clavier de tous les
    ///     joueurs ; reconnexion silencieuse et frames abandonnées tant que l'hôte est absent.
    /// </summary>
    internal static class HmFrameClient
    {
        private const string HostPipeName = "WiimoteGunHmHost";

        private static readonly object _gate = new object();
        private static NamedPipeClientStream _pipe;
        private static StreamWriter _writer;
        private static DateTime _lastWarnUtc = DateTime.MinValue;

        /// <summary>EN: Absolute mouse frame (x/y 0..32767, btn bits: 1=Left 2=Right 4=Middle) / FR: Frame souris absolue</summary>
        public static void SendMouse(int playerIndex, int x, int y, int buttons, int wheel)
        {
            Send(string.Format(CultureInfo.InvariantCulture, "MOUSE:{0}:{1}:{2}:{3}:{4}", playerIndex, x, y, buttons, wheel));
        }

        /// <summary>EN: Relative mouse frame (dX/dY in sbyte range) / FR: Frame souris relative</summary>
        public static void SendMouseRelative(int playerIndex, int dx, int dy, int buttons, int wheel)
        {
            Send(string.Format(CultureInfo.InvariantCulture, "MOUSER:{0}:{1}:{2}:{3}:{4}", playerIndex, dx, dy, buttons, wheel));
        }

        /// <summary>EN: Keyboard frame (HID modifier bits + up to 6 HID codes) / FR: Frame clavier</summary>
        public static void SendKeys(int playerIndex, int modifiers, byte[] keyCodes)
        {
            string keys = (keyCodes == null) ? "" : string.Join(",", keyCodes);
            Send(string.Format(CultureInfo.InvariantCulture, "KEYS:{0}:{1}:{2}", playerIndex, modifiers, keys));
        }

        /// <summary>
        /// [V57g] EN: DInput gamepad frame - the vmulti Joystick report 0x06 payload:
        ///     throttle 0..255, x/y signed -127..127, hat 0..8 (8 = neutral, the app
        ///     mirrors the DPad to button bits 12..15), rx/ry 0..255 (center 128),
        ///     buttons 0..65535.
        ///     FR: Frame gamepad DInput - la charge du rapport Joystick vmulti 0x06 :
        ///     throttle 0..255, x/y signés -127..127, hat 0..8 (8 = neutre, l'app
        ///     reflète le DPad sur les bits boutons 12..15), rx/ry 0..255 (centre 128),
        ///     boutons 0..65535.
        /// </summary>
        public static void SendGamepadRaw(int playerIndex, int throttle, int x, int y, int hat, int rx, int ry, int buttons)
        {
            Send(string.Format(CultureInfo.InvariantCulture, "GP:{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                playerIndex, throttle, x, y, hat, rx, ry, buttons));
        }

        /// <summary>
        /// [V57g] EN: XInput gamepad frame (Xbox 360 wired profile): sticks and triggers
        ///     0..65535 (center 32768 for sticks, 0 = released for triggers), hat = HMHat
        ///     octant 0..8, buttons = HMButton bitmask.
        ///     FR: Frame gamepad XInput (profil Xbox 360 filaire) : sticks et gâchettes
        ///     0..65535 (centre 32768 pour les sticks, 0 = relâché pour les gâchettes),
        ///     hat = octant HMHat 0..8, boutons = masque HMButton.
        /// </summary>
        public static void SendGamepadXInput(int playerIndex, int lx, int ly, int rx, int ry, int lt, int rt, int hat, int buttons)
        {
            Send(string.Format(CultureInfo.InvariantCulture, "GPX:{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}",
                playerIndex, lx, ly, rx, ry, lt, rt, hat, buttons));
        }

        private static void Send(string message)
        {
            try
            {
                lock (_gate)
                {
                    if (!EnsureConnected())
                        return; // EN/FR: drop the frame while HmHost is not reachable
                    _writer.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Close();
                WarnThrottled("send failed: " + ex.Message);
            }
        }

        private static bool EnsureConnected()
        {
            if (_writer != null)
                return true;
            try
            {
                _pipe = new NamedPipeClientStream(".", HostPipeName, PipeDirection.Out);
                _pipe.Connect(200);
                _writer = new StreamWriter(_pipe) { AutoFlush = true };
                SimpleLogger.Instance.Info("[HmFrameClient] Connected to HmHost pipe.");
                return true;
            }
            catch (Exception ex)
            {
                Close();
                WarnThrottled("connect failed: " + ex.GetType().Name);
                return false;
            }
        }

        private static void Close()
        {
            try { if (_writer != null) _writer.Dispose(); } catch { }
            try { if (_pipe != null) _pipe.Dispose(); } catch { }
            _writer = null;
            _pipe = null;
        }

        private static void WarnThrottled(string message)
        {
            // EN: At most one warning per minute to avoid flooding the log at 60-240 frames/s
            // FR: Au plus une alerte par minute pour ne pas noyer le log à 60-240 frames/s
            if ((DateTime.UtcNow - _lastWarnUtc).TotalSeconds < 60)
                return;
            _lastWarnUtc = DateTime.UtcNow;
            SimpleLogger.Instance.Debug("[HmFrameClient] " + message + " (HmHost pipe not reachable?)");
        }
    }
}
