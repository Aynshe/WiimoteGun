using System;

namespace WiimoteGun
{
    /// <summary>
    /// Helper class to detect and manage VMulti virtual HID devices
    /// Uses VMultiClient directly instead of Interception
    /// (EN/FR: Classe helper pour détecter et gérer les périphériques VMulti)
    /// (EN/FR: Utilise VMultiClient directement au lieu d'Interception)
    /// </summary>
    public static class VMultiDeviceDetector
    {
        // Support for 4 players via vmultia, vmultib, vmultic, vmultid
        private static readonly string[] VMultiSuffixes = { "vmultia", "vmultib", "vmultic", "vmultid" };

        /// <summary>
        /// Detect VMulti devices for a specific player (1-based index)
        /// Now uses VMultiClient for detection instead of Interception
        /// (EN/FR: Détecter périphériques VMulti pour un joueur spécifique)
        /// </summary>
        public static (string mouseId, string keyboardId) DetectPlayerVMultiDevices(int playerIndex)
        {
            string mouseId = null;
            string keyboardId = null;
            
            if (playerIndex < 1 || playerIndex > 4) return (null, null);

            string targetSuffix = VMultiSuffixes[playerIndex - 1];
            string vid = GetVidFromSuffix(targetSuffix);

            try
            {
                // Detect Mouse using VMultiClient (EN/FR: Détecter souris via VMultiClient)
                // VMultiClient handles both mouse and keyboard on the same device
                if (VMultiClient.IsDeviceAvailable(playerIndex))
                {
                    // Device is available - construct the expected hardware ID format
                    // (EN/FR: Périphérique disponible - construire le format hardware ID attendu)
                    mouseId = $"HID\\{targetSuffix}&Col03HID\\VID_{vid}&UP:0001_U:0002HID_DEVICE";
                    keyboardId = $"HID\\{targetSuffix}&Col07HID\\VID_{vid}&UP:0001_U:0006HID_DEVICE";
                    
                    SimpleLogger.Instance.Info($"[VMulti Detector] Found {targetSuffix} devices (P{playerIndex}): Mouse & Keyboard available");
                }
                else
                {
                    // Try fallback detection via DeviceHelper (EN/FR: Essayer détection fallback via DeviceHelper)
                    string uniqueId = DeviceHelper.FindVMultiMouseUniqueId(targetSuffix);
                    if (!string.IsNullOrEmpty(uniqueId) && uniqueId != "Unknown")
                    {
                        mouseId = $"HID\\{targetSuffix}&Col03HID\\VID_{vid}&UP:0001_U:0002HID_DEVICE";
                        keyboardId = $"HID\\{targetSuffix}&Col07HID\\VID_{vid}&UP:0001_U:0006HID_DEVICE";
                        SimpleLogger.Instance.Info($"[VMulti Detector] Found {targetSuffix} (P{playerIndex}) via DeviceHelper fallback");
                    }
                    else
                    {
                        SimpleLogger.Instance.Debug($"[VMulti Detector] No {targetSuffix} device found for P{playerIndex}");
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMulti Detector] Error detecting P{playerIndex} devices: {ex.Message}");
            }

            return (mouseId, keyboardId);
        }

        /// <summary>
        /// Auto-assign VMulti devices to players if option is enabled
        /// Simplified: just checks if device is available and assigns fixed IDs
        /// (EN/FR: Auto-assigner les périphériques VMulti aux joueurs si option activée)
        /// </summary>
        public static void AutoAssignVMultiDevices()
        {
            if (!Options.Instance.AutoLockVMultiDevices) return;

            SimpleLogger.Instance.Info("[VMulti Auto-Assign] Starting 4-Player auto-assignment...");

            for (int p = 1; p <= 4; p++)
            {
                var (mouseId, keyboardId) = DetectPlayerVMultiDevices(p);

                if (mouseId != null)
                {
                    Options.Instance.SetPreferredMouseId(p, mouseId);
                    SimpleLogger.Instance.Info($"[VMulti Auto-Assign] Locked P{p} Mouse: {mouseId}");
                }
                
                if (keyboardId != null)
                {
                    Options.Instance.SetPreferredKeyboardId(p, keyboardId);
                    SimpleLogger.Instance.Info($"[VMulti Auto-Assign] Locked P{p} Keyboard: {keyboardId}");
                }
                
                if (mouseId == null && keyboardId == null)
                {
                    SimpleLogger.Instance.Info($"[VMulti Auto-Assign] P{p} devices not found.");
                }
            }
            
            Options.Instance.Save();
        }

        /// <summary>
        /// Check if Player should have locked device selection in UI
        /// Simplified: just checks if VMulti device is available
        /// (EN/FR: Vérifier si le joueur doit avoir la sélection verrouillée)
        /// </summary>
        public static bool ShouldLockPlayerDevices(int playerIndex)
        {
            if (!Options.Instance.AutoLockVMultiDevices) return false;
            
            // Lock if corresponding VMulti device is detected
            return VMultiClient.IsDeviceAvailable(playerIndex);
        }

        /// <summary>
        /// Check if any VMulti driver is installed
        /// (EN/FR: Vérifier si un pilote VMulti est installé)
        /// </summary>
        public static bool IsAnyVMultiInstalled()
        {
            for (int i = 1; i <= 4; i++)
            {
                if (VMultiClient.IsDeviceAvailable(i))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get list of installed VMulti player indices
        /// (EN/FR: Obtenir la liste des indices de joueurs VMulti installés)
        /// </summary>
        public static System.Collections.Generic.List<int> GetInstalledPlayers()
        {
            return VMultiClient.GetAvailablePlayers();
        }

        /// <summary>
        /// Get VID from VMulti suffix (EN/FR: Obtenir VID depuis suffixe VMulti)
        /// </summary>
        private static string GetVidFromSuffix(string suffix)
        {
            switch (suffix.ToLowerInvariant())
            {
                case "vmultia": return "001F";
                case "vmultib": return "002F";
                case "vmultic": return "003F";
                case "vmultid": return "004F";
                default: return "XXXX";
            }
        }

        /// <summary>
        /// Get VMulti suffix from player index (EN/FR: Obtenir suffixe VMulti depuis index joueur)
        /// </summary>
        public static string GetSuffixFromPlayer(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4) return "vmultia";
            return VMultiSuffixes[playerIndex - 1];
        }
    }
}
