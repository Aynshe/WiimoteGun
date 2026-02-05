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
                            // Increased timeout to 3000ms to avoid flaky connection failures
                            pipeClient.Connect(3000); 
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
        public static void UnregisterClient() { SendCommand("UNREGISTER_CLIENT"); }

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
    }
}
