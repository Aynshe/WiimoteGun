using System;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun
{
    /// <summary>
    /// Helper class to detect and manage VMulti virtual HID devices
    /// (EN/FR: Classe helper pour détecter et gérer les périphériques VMulti)
    /// </summary>
    /// <summary>
    /// Helper class to detect and manage VMulti virtual HID devices
    /// (EN/FR: Classe helper pour détecter et gérer les périphériques VMulti)
    /// </summary>
    public static class VMultiDeviceDetector
    {
        // Support for 4 players via vmultia, vmultib, vmultic, vmultid
        private static readonly string[] VMultiSuffixes = { "vmultia", "vmultib", "vmultic", "vmultid" };

        /// <summary>
        /// Detect VMulti devices for a specific player (1-based index)
        /// (EN/FR: Détecter périphériques VMulti pour un joueur spécifique)
        /// </summary>
        public static (string mouseId, string keyboardId) DetectPlayerVMultiDevices(int playerIndex)
        {
            string mouseId = null;
            string keyboardId = null;
            
            if (playerIndex < 1 || playerIndex > 4) return (null, null);

            string targetSuffix = VMultiSuffixes[playerIndex - 1];

            try
            {
                // Detect Mouse
                var context = Interception.InterceptionDriver.interception_create_context();
                if (context != IntPtr.Zero)
                {
                    for (int i = 11; i <= 20; i++)
                    {
                        if (Interception.InterceptionDriver.interception_is_mouse(i) != 0)
                        {
                            byte[] buffer = new byte[1000];
                            uint result = Interception.InterceptionDriver.interception_get_hardware_id(context, i, buffer, (uint)buffer.Length);
                            
                            if (result > 0)
                            {
                                int byteCount = Math.Min((int)result * 2, buffer.Length);
                                string hardwareId = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount);
                                hardwareId = hardwareId.Replace("\0", "").Trim();
                                
                                // Path-based check
                                if (hardwareId.IndexOf(targetSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    mouseId = hardwareId;
                                    SimpleLogger.Instance.Info($"[VMulti Detector] Found {targetSuffix} Mouse (P{playerIndex}): Device {i}");
                                    break;
                                }
                            }
                        }
                    }
                    Interception.InterceptionDriver.interception_destroy_context(context);
                }

                // Detect Keyboard
                var availableKeyboards = VirtualInterceptionKeyboard.GetAvailableKeyboardsWithNames();
                foreach (var kvp in availableKeyboards)
                {
                    string hardwareId = VirtualInterceptionKeyboard.GetKeyboardHardwareId(kvp.Key);
                    if (!string.IsNullOrEmpty(hardwareId))
                    {
                        if (hardwareId.IndexOf(targetSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            keyboardId = hardwareId;
                            SimpleLogger.Instance.Info($"[VMulti Detector] Found {targetSuffix} Keyboard (P{playerIndex}): Device {kvp.Key}");
                            break;
                        }
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
        /// (EN/FR: Auto-assigner les périphériques VMulti aux joueurs si l'option est activée)
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
                     // Optionnel: Reset si non trouvé? Ou garder précédent?
                     // Pour l'instant on ne reset pas pour éviter de perdre une config manuelle si le driver clignote
                     // Mais l'auto-lock implique souvent "force". 
                     // Le comportement précédent ne resetait pas explicitement sauf P2.
                     // On va logger.
                     SimpleLogger.Instance.Info($"[VMulti Auto-Assign] P{p} devices not found.");
                }
            }
            
            Options.Instance.Save();
        }

        /// <summary>
        /// Check if Player should have locked device selection in UI
        /// </summary>
        public static bool ShouldLockPlayerDevices(int playerIndex)
        {
            if (!Options.Instance.AutoLockVMultiDevices) return false;
            
            // Lock if corresponding VMulti device is detected
            var (mouseId, keyboardId) = DetectPlayerVMultiDevices(playerIndex);
            return mouseId != null || keyboardId != null;
        }
    }
}
