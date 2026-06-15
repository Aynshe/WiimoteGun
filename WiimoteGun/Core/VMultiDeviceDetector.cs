using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

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
        // SetupAPI imports for non-present device enumeration
        // (EN/FR: Imports SetupAPI pour l'énumération des périphériques non présents)
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            int MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        // HID Class GUID: {745a17a0-74d3-11d0-b6fe-00a0c90f57da}
        private static readonly Guid HidClassGuid = new Guid("745a17a0-74d3-11d0-b6fe-00a0c90f57da");

        public struct PlayerDevices
        {
            public string MouseId;
            public string KeyboardId;
        }

        // Support for 4 players via vmultia, vmultib, vmultic, vmultid
        private static readonly string[] VMultiSuffixes = { "vmultia", "vmultib", "vmultic", "vmultid" };

        /// <summary>
        /// Detect VMulti devices for a specific player (1-based index)
        /// Now uses VMultiClient for detection instead of Interception
        /// (EN/FR: Détecter périphériques VMulti pour un joueur spécifique)
        /// </summary>
        public static PlayerDevices DetectPlayerVMultiDevices(int playerIndex)
        {
            PlayerDevices devices = new PlayerDevices();
            devices.MouseId = null;
            devices.KeyboardId = null;
            
            if (playerIndex < 1 || playerIndex > 4) return devices;

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
                    devices.MouseId = string.Format("HID\\{0}&Col03HID\\VID_{1}&UP:0001_U:0002HID_DEVICE", targetSuffix, vid);
                    devices.KeyboardId = string.Format("HID\\{0}&Col07HID\\VID_{1}&UP:0001_U:0006HID_DEVICE", targetSuffix, vid);
                    
                    SimpleLogger.Instance.Info(string.Format("[VMulti Detector] Found {0} devices (P{1}): Mouse & Keyboard available", targetSuffix, playerIndex));
                }
                else
                {
                    // Try fallback detection via DeviceHelper (EN/FR: Essayer détection fallback via DeviceHelper)
                    string uniqueId = DeviceHelper.FindVMultiMouseUniqueId(targetSuffix);
                    if (!string.IsNullOrEmpty(uniqueId) && uniqueId != "Unknown")
                    {
                        devices.MouseId = string.Format("HID\\{0}&Col03HID\\VID_{1}&UP:0001_U:0002HID_DEVICE", targetSuffix, vid);
                        devices.KeyboardId = string.Format("HID\\{0}&Col07HID\\VID_{1}&UP:0001_U:0006HID_DEVICE", targetSuffix, vid);
                        SimpleLogger.Instance.Info(string.Format("[VMulti Detector] Found {0} (P{1}) via DeviceHelper fallback", targetSuffix, playerIndex));
                    }
                    else
                    {
                        SimpleLogger.Instance.Debug(string.Format("[VMulti Detector] No {0} device found for P{1}", targetSuffix, playerIndex));
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[VMulti Detector] Error detecting P{0} devices: {1}", playerIndex, ex.Message));
            }

            return devices;
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
                PlayerDevices devices = DetectPlayerVMultiDevices(p);

                if (devices.MouseId != null)
                {
                    Options.Instance.SetPreferredMouseId(p, devices.MouseId);
                    SimpleLogger.Instance.Info(string.Format("[VMulti Auto-Assign] Locked P{0} Mouse: {1}", p, devices.MouseId));
                }
                
                if (devices.KeyboardId != null)
                {
                    Options.Instance.SetPreferredKeyboardId(p, devices.KeyboardId);
                    SimpleLogger.Instance.Info(string.Format("[VMulti Auto-Assign] Locked P{0} Keyboard: {1}", p, devices.KeyboardId));
                }
                
                if (devices.MouseId == null && devices.KeyboardId == null)
                {
                    SimpleLogger.Instance.Info(string.Format("[VMulti Auto-Assign] P{0} devices not found.", p));
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
            // 1. Check if any VMulti device is currently active/available
            // (EN/FR: 1. Vérifier si un périphérique VMulti est actuellement actif/disponible)
            for (int i = 1; i <= 4; i++)
            {
                if (VMultiClient.IsDeviceAvailable(i))
                {
                    SimpleLogger.Instance.Info(string.Format("[VMulti Detector] Driver detected via active VMultiClient (Player {0})", i));
                    return true;
                }
            }

            // 2. Enumerate HIDClass devices (including non-present/disabled ones) via SetupAPI
            // (EN/FR: 2. Énumérer les périphériques HIDClass (y compris non présents/désactivés) via SetupAPI)
            try
            {
                Guid hidGuid = HidClassGuid;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, 0x00000010); // DIGCF_DEVICEINTERFACE only (all present & non-present)
                if (deviceInfoSet != IntPtr.Zero && deviceInfoSet.ToInt64() != -1)
                {
                    try
                    {
                        SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
                        devInfo.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

                        for (int i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref devInfo); i++)
                        {
                            StringBuilder instanceIdSb = new StringBuilder(1024);
                            if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfo, instanceIdSb, instanceIdSb.Capacity, out _))
                            {
                                string instanceId = instanceIdSb.ToString();
                                foreach (string suffix in VMultiSuffixes)
                                {
                                    if (instanceId.IndexOf(suffix, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        SimpleLogger.Instance.Info(string.Format("[VMulti Detector] Driver detected via SetupAPI device instance: {0}", instanceId));
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(deviceInfoSet);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("[VMulti Detector] SetupAPI check failed: {0}", ex.Message));
            }

            SimpleLogger.Instance.Debug("[VMulti Detector] No VMulti drivers detected.");
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
