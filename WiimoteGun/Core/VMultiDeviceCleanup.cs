using System;

namespace WiimoteGun.Core
{
    /// <summary>
    /// Utility class to clean up unwanted VMulti HID collections.
    /// EN: Uses the WiimoteGun Service (admin) to disable unwanted collections.
    /// FR: Utilise le Service WiimoteGun (admin) pour désactiver les collections non désirées.
    /// </summary>
    public static class VMultiDeviceCleanup
    {
        /// <summary>
        /// EN: Request the service to cleanup unwanted VMulti collections (COL01, COL02, COL04, COL05, COL06).
        /// FR: Demander au service de nettoyer les collections VMulti non désirées.
        /// </summary>
        public static void RemoveUnwantedCollections()
        {
            try
            {
                SimpleLogger.Instance.Info("[VMultiCleanup] Requesting service to cleanup unwanted VMulti collections...");
                ServiceClient.CleanupVMulti();
                SimpleLogger.Instance.Info("[VMultiCleanup] Cleanup request sent to service.");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiCleanup] Error sending cleanup request: {ex.Message}");
            }
        }

        /// <summary>
        /// EN: Schedule cleanup after Wiimote connection (async with delay).
        /// FR: Planifier le nettoyage après connexion Wiimote (async avec délai).
        /// The delay allows time for Windows to enumerate the VMulti devices.
        /// </summary>
        public static void ScheduleCleanupAfterWiimoteConnect()
        {
            // EN: Run cleanup asynchronously with a delay to allow device enumeration
            // FR: Exécuter le nettoyage de manière asynchrone avec délai pour énumération
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    // EN: Wait for Windows to enumerate VMulti devices after Wiimote connect
                    // FR: Attendre que Windows énumère les périphériques VMulti après connexion Wiimote
                    await System.Threading.Tasks.Task.Delay(3000).ConfigureAwait(false);
                    
                    SimpleLogger.Instance.Info("[VMultiCleanup] Scheduled cleanup triggered after Wiimote connect.");
                    ServiceClient.CleanupVMulti();
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[VMultiCleanup] Error in ScheduleCleanupAfterWiimoteConnect: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// EN: Remove (hide) all COL03 mouse devices at application startup.
        /// FR: Supprimer (masquer) tous les périphériques souris COL03 au démarrage de l'application.
        /// This prevents ghost lightgun icons in EmulationStation.
        /// </summary>
        public static void RemoveAllMiceAtStartup()
        {
            try
            {
                SimpleLogger.Instance.Info("[VMultiCleanup] Removing all COL03 mice at startup...");
                ServiceClient.RemoveMouseForAllPlayers();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiCleanup] Error removing mice at startup: {ex.Message}");
            }
        }

        /// <summary>
        /// EN: Remove COL03 mouse for a disconnected player.
        /// FR: Supprimer la souris COL03 pour un joueur déconnecté.
        /// </summary>
        public static void RemoveMouseForDisconnectedPlayer(int playerIndex)
        {
            try
            {
                SimpleLogger.Instance.Info($"[VMultiCleanup] Removing COL03 mouse for disconnected P{playerIndex}...");
                ServiceClient.RemoveMouseForPlayer(playerIndex);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiCleanup] Error removing mouse for P{playerIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// EN: Remove COL03 mice for all players EXCEPT those currently connected.
        /// FR: Supprimer les souris COL03 pour tous les joueurs SAUF ceux actuellement connectés.
        /// Called after a Wiimote connects to hide ghost mice from other slots.
        /// </summary>
        public static void RemoveMouseForUnconnectedPlayers(int[] connectedPlayerIndexes)
        {
            try
            {
                string connected = connectedPlayerIndexes.Length > 0 
                    ? string.Join(",", connectedPlayerIndexes) 
                    : "none";
                SimpleLogger.Instance.Info($"[VMultiCleanup] Removing COL03 mice except connected players: {connected}");
                ServiceClient.RemoveMouseExceptPlayers(connectedPlayerIndexes);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiCleanup] Error removing mice for unconnected players: {ex.Message}");
            }
        }
    }
}
