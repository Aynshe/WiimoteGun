using System;
using System.Diagnostics;
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
        /// EN: Remove (disable) Col06 gamepad device for a specific player.
        /// FR: Supprimer (désactiver) le périphérique gamepad Col06 pour un joueur spécifique.
        /// </summary>
        public static void RemoveGamepad(int playerIndex) { SendCommand(string.Format("REMOVE_GAMEPAD_P{0}", playerIndex)); }

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
}
