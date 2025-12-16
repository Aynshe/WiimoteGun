using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WiimoteGun
{
    /// <summary>
    /// Helper class to retrieve device information from Windows (EN/FR: Classe helper pour récupérer infos périphériques)
    /// </summary>
    public static class DeviceHelper
    {
        // SetupAPI constants
        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_DEVICEINTERFACE = 0x00000010;
        private const int SPDRP_DEVICEDESC = 0x00000000;
        private const int SPDRP_FRIENDLYNAME = 0x0000000C;
        private const int ERROR_NO_MORE_ITEMS = 259;

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

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

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            int Property,
            out int PropertyRegDataType,
            StringBuilder PropertyBuffer,
            int PropertyBufferSize,
            out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetupDiGetDeviceProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            IntPtr PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize,
            uint Flags);

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
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }
        
        // DEVPKEY_Device_BusReportedDeviceDesc {540b947e-8b40-45bc-a8a2-6a0b894cbda2}, 4
        private static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc = new DEVPROPKEY { fmtid = new Guid("540b947e-8b40-45bc-a8a2-6a0b894cbda2"), pid = 4 };

        /// <summary>
        /// VMulti 4-Player Device Mapping (EN/FR: Mapping dispositifs VMulti 4-joueurs)
        /// Maps VID to (UniqueID, PlayerNumber) for proper device identification
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, (string UniqueId, int PlayerNumber)> VMultiPlayerMapping = new System.Collections.Generic.Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
        {
            { "001F", ("2D595CA7", 1) },  // vmultia - Player 1
            { "002F", ("4784345", 2) },   // vmultib - Player 2
            { "003F", ("1731F3EA", 3) },  // vmultic - Player 3
            { "004F", ("29EBA48F", 4) }   // vmultid - Player 4
        };

        /// <summary>
        /// Known mouse vendors and products (EN/FR: Base de données vendeurs/produits connus)
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> KnownVendors = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "046D", "Logitech" },
            { "1EA7", "SHARKOON" },
            { "045E", "Microsoft" },
            { "1532", "Razer" },
            { "046A", "Cherry" },
            { "0D62", "Darfon Electronics" },
            { "04F2", "Chicony Electronics" },
            { "093A", "Pixart Imaging" },
            { "3938", "Hanvon Ugee" },
            { "00FF", "Pentablet HID" },
        };

        private static readonly System.Collections.Generic.Dictionary<string, string> KnownProducts = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Logitech
            { "046D_C52B", "Logitech Unifying Receiver" },
            { "046D_C534", "Logitech Unifying Receiver" },
            { "046D_C077", "Logitech Mouse M105/M100" },
            { "046D_C05A", "Logitech M90/M100 Optical Mouse" },
            { "046D_C050", "Logitech RX 250 Optical Mouse" },
            // SHARKOON
            { "1EA7_0169", "SHARKOON 2.4GHz Wireless Rechargeable Gaming Mouse" },
            // VMulti
            { "002F_0001", "DJP Inc. Virtual Keyboard" }, // Assuming PID 0001 or generic match
        };
        /// Manual mapping for common gaming brands to ensure clean names (EN/FR: Mapping manuel pour marques gaming)
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> ManualBrandMappings = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "046D", "Logitech" },
            { "1532", "Razer" },
            { "1B1C", "Corsair" },
            { "1038", "SteelSeries" },
            { "0951", "HyperX" },
            { "0B05", "ASUS" },
            { "045E", "Microsoft" },
            { "04F2", "Chicony" },
            { "413C", "Dell" },
            { "03F0", "HP" },
            { "17EF", "Lenovo" },
            { "04CA", "Lite-On" },
            { "2516", "Cooler Master" },
            { "28BD", "Roccat" },
            { "1189", "Acer" },
            { "320F", "Glorious" },
            { "19F7", "Matte" }, // Redragon often uses generic IDs but some are specific
        };

        /// <summary>
        /// Get friendly name of a mouse device by its Hardware ID (EN/FR: Obtenir nom commercial d'une souris)
        /// </summary>
        public static string GetDeviceFriendlyName(string vidPid, string devicePath = null)
        {
            // Mouse class GUID
            Guid mouseGuid = new Guid("{4d36e96f-e325-11ce-bfc1-08002be10318}");
            return GetFriendlyName(vidPid, mouseGuid, "Mouse", devicePath);
        }

        /// <summary>
        /// Get friendly name of a keyboard device by its Hardware ID (EN/FR: Obtenir nom commercial d'un clavier)
        /// </summary>
        public static string GetKeyboardFriendlyName(string vidPid, string devicePath = null)
        {
            // Keyboard class GUID
            Guid keyboardGuid = new Guid("{4D36E96B-E325-11CE-BFC1-08002BE10318}");
            return GetFriendlyName(vidPid, keyboardGuid, "Keyboard", devicePath);
        }

        private static string GetFriendlyName(string vidPid, Guid classGuid, string deviceType, string devicePath = null)
        {
            // Robust path-based check for VMulti devices (Requested by User)
            // (EN/FR: Vérification robuste basée sur le chemin pour VMulti)
            if (!string.IsNullOrEmpty(devicePath))
            {
                string vMultiId = "Unknown";
                
                // Force SetupAPI lookup for VMulti devices to ensure we get the real Windows Instance ID
                // and avoid extracting garbage from the Interception string (e.g. UP:0001...)
                if (devicePath.IndexOf("vmultia", StringComparison.OrdinalIgnoreCase) >= 0) vMultiId = FindVMultiUniqueId("vmultia");
                else if (devicePath.IndexOf("vmultib", StringComparison.OrdinalIgnoreCase) >= 0) vMultiId = FindVMultiUniqueId("vmultib");
                else if (devicePath.IndexOf("vmultic", StringComparison.OrdinalIgnoreCase) >= 0) vMultiId = FindVMultiUniqueId("vmultic");
                else if (devicePath.IndexOf("vmultid", StringComparison.OrdinalIgnoreCase) >= 0) vMultiId = FindVMultiUniqueId("vmultid");
                else 
                {
                    // Fallback for non-VMulti or generic usage if needed
                    vMultiId = ExtractVMultiIdFromPath(devicePath);
                }

                if (string.IsNullOrEmpty(vMultiId)) vMultiId = "Unknown";

                // Extract VID for display
                var vidRes = ExtractVidPid(vidPid);
                string displayVid = vidRes.vid ?? "XXXX";

                if (devicePath.IndexOf("vmultia", StringComparison.OrdinalIgnoreCase) >= 0) return $"DJP Inc. ({vMultiId}) {deviceType} ({displayVid}:N/A) [Player 1]";
                if (devicePath.IndexOf("vmultib", StringComparison.OrdinalIgnoreCase) >= 0) return $"DJP Inc. ({vMultiId}) {deviceType} ({displayVid}:N/A) [Player 2]";
                if (devicePath.IndexOf("vmultic", StringComparison.OrdinalIgnoreCase) >= 0) return $"DJP Inc. ({vMultiId}) {deviceType} ({displayVid}:N/A) [Player 3]";
                if (devicePath.IndexOf("vmultid", StringComparison.OrdinalIgnoreCase) >= 0) return $"DJP Inc. ({vMultiId}) {deviceType} ({displayVid}:N/A) [Player 4]";
            }
            if (string.IsNullOrEmpty(vidPid))
                return null;

            // Extract VID/PID if in format VID_XXXX&PID_YYYY
            var vidPidResult = ExtractVidPid(vidPid);
            string vid = vidPidResult.vid;
            string pid = vidPidResult.pid;
            
            SimpleLogger.Instance.Info($"[DEVICE HELPER] Get{deviceType}FriendlyName input: '{vidPid}' -> VID: '{vid}', PID: '{pid}'");
            
            if (vid == null)
                return null;

            // Legacy VMulti mapping removed to avoid confusion with incorrect hardcoded IDs
            // We now rely exclusively on path-based detection (vmultia/b/c/d) above.
            /*
            if (VMultiPlayerMapping.ContainsKey(vid))
            {
                var (uniqueId, playerNumber) = VMultiPlayerMapping[vid];
                return $"DJP Inc. ({uniqueId}) {deviceType} ({vid}:N/A)";
            }
            */

            // Check known products database first (EN/FR: Vérifier d'abord la base de données)
            if (pid != null)
            {
                string productKey = $"{vid}_{pid}";
                if (KnownProducts.ContainsKey(productKey))
                {
                    return KnownProducts[productKey];
                }
            }

            // Try to find the device via SetupAPI to get BusReportedDeviceDesc (iProduct)
            // This is the most accurate "commercial name" reported by the device itself
            try
            {
                IntPtr deviceInfoSet = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT);

                if (deviceInfoSet != IntPtr.Zero && deviceInfoSet.ToInt64() != -1)
                {
                    try
                    {
                        SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
                        devInfo.cbSize = Marshal.SizeOf(devInfo);

                        for (int i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref devInfo); i++)
                        {
                            StringBuilder instanceId = new StringBuilder(512);
                            if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfo, instanceId, instanceId.Capacity, out _))
                            {
                                string deviceInstanceId = instanceId.ToString();
                                
                                bool vidMatch = deviceInstanceId.IndexOf($"VID_{vid}", StringComparison.OrdinalIgnoreCase) >= 0;
                                bool pidMatch = pid == null || deviceInstanceId.IndexOf($"PID_{pid}", StringComparison.OrdinalIgnoreCase) >= 0;

                                if (vidMatch && pidMatch)
                                {
                                    // Try to get BusReportedDeviceDesc (iProduct)
                                    uint propertyType;
                                    uint requiredSize;
                                    byte[] buffer = new byte[1024];
                                    IntPtr bufferPtr = Marshal.AllocHGlobal(buffer.Length);
                                    
                                    try
                                    {
                                        if (SetupDiGetDeviceProperty(deviceInfoSet, ref devInfo, ref DEVPKEY_Device_BusReportedDeviceDesc, out propertyType, bufferPtr, (uint)buffer.Length, out requiredSize, 0))
                                        {
                                            string busReportedName = Marshal.PtrToStringUni(bufferPtr);
                                            if (!string.IsNullOrEmpty(busReportedName))
                                            {
                                                SimpleLogger.Instance.Info($"[DEVICE HELPER] Found BusReportedDeviceDesc: {busReportedName}");
                                                
                                                // Try to get Manufacturer
                                                string manufacturer = null;
                                                StringBuilder mfgBuffer = new StringBuilder(512);
                                                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref devInfo, 0x0000000B /* SPDRP_MFG */, out _, mfgBuffer, mfgBuffer.Capacity, out _))
                                                {
                                                    manufacturer = mfgBuffer.ToString();
                                                    // Filter out generic Microsoft drivers
                                                    if (manufacturer.Contains("Microsoft") || manufacturer.Contains("Standard"))
                                                        manufacturer = null;
                                                }

                                                // If we have a valid manufacturer from SetupAPI, use it
                                                if (!string.IsNullOrEmpty(manufacturer))
                                                {
                                                    // Clean up manufacturer name if it's in our manual list
                                                    // e.g. "Logitech, Inc." -> "Logitech"
                                                    foreach (var kvp in ManualBrandMappings)
                                                    {
                                                        if (manufacturer.IndexOf(kvp.Value, StringComparison.OrdinalIgnoreCase) >= 0)
                                                        {
                                                            manufacturer = kvp.Value;
                                                            break;
                                                        }
                                                    }
                                                    return $"{manufacturer} {busReportedName}";
                                                }
                                                
                                                // Otherwise try to get Vendor from Manual Mappings or UsbIdProvider
                                                string vendorName = null;
                                                if (ManualBrandMappings.ContainsKey(vid))
                                                    vendorName = ManualBrandMappings[vid];
                                                else
                                                    vendorName = UsbIdProvider.GetVendorName(vid);

                                                if (!string.IsNullOrEmpty(vendorName))
                                                    return $"{vendorName} {busReportedName}";

                                                return busReportedName;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(bufferPtr);
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
                SimpleLogger.Instance.Error($"Error getting device property: {ex.Message}");
            }

            // Try USB ID Database (EN/FR: Essayer la base de données USB ID)
            if (pid != null)
            {
                string usbDbName = UsbIdProvider.GetProductName(vid, pid);
                if (!string.IsNullOrEmpty(usbDbName))
                {
                    string vendor = null;
                    if (ManualBrandMappings.ContainsKey(vid))
                        vendor = ManualBrandMappings[vid];
                    else
                        vendor = UsbIdProvider.GetVendorName(vid);
                        
                    return string.IsNullOrEmpty(vendor) ? usbDbName : $"{vendor} {usbDbName}";
                }
            }

            // Fallback to vendor name if product unknown (EN/FR: Utiliser nom vendeur si produit inconnu)
            if (KnownVendors.ContainsKey(vid))
            {
                return $"{KnownVendors[vid]} {deviceType} ({vid}:{pid ?? "N/A"})";
            }
            
            // Try Manual Mappings or USB ID Database for Vendor only
            string usbDbVendor = null;
            if (ManualBrandMappings.ContainsKey(vid))
                usbDbVendor = ManualBrandMappings[vid];
            else
                usbDbVendor = UsbIdProvider.GetVendorName(vid);

            if (!string.IsNullOrEmpty(usbDbVendor))
            {
                return $"{usbDbVendor} {deviceType} ({vid}:{pid ?? "N/A"})";
            }
            
            // Last resort: try Windows API (EN/FR: Dernier recours : API Windows)
            try
            {
                IntPtr deviceInfoSet = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
                    return null;

                try
                {
                    SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
                    devInfo.cbSize = Marshal.SizeOf(devInfo);

                    for (int i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref devInfo); i++)
                    {
                        // Get device instance ID
                        StringBuilder instanceId = new StringBuilder(512);
                        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfo, instanceId, instanceId.Capacity, out _))
                        {
                            string deviceInstanceId = instanceId.ToString();
                            
                            // Check if this device matches the VID/PID
                            bool vidMatch = deviceInstanceId.IndexOf($"VID_{vid}", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool pidMatch = pid == null || deviceInstanceId.IndexOf($"PID_{pid}", StringComparison.OrdinalIgnoreCase) >= 0;

                            if (vidMatch && pidMatch)
                            {
                                // Try to get friendly name first
                                StringBuilder friendlyName = new StringBuilder(512);
                                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref devInfo, SPDRP_FRIENDLYNAME, out _, friendlyName, friendlyName.Capacity, out _))
                                {
                                    string name = friendlyName.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                        return name;
                                }

                                // Fallback to device description
                                StringBuilder deviceDesc = new StringBuilder(512);
                                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref devInfo, SPDRP_DEVICEDESC, out _, deviceDesc, deviceDesc.Capacity, out _))
                                {
                                    string name = deviceDesc.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                        return name;
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
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error getting device friendly name: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Extract VID/PID from hardware ID string (EN/FR: Extraire VID/PID depuis chaîne Hardware ID)
        /// </summary>
        public static (string vid, string pid) ExtractVidPid(string hardwareId)
        {
            if (string.IsNullOrEmpty(hardwareId))
                return (null, null);

            var vidMatch = System.Text.RegularExpressions.Regex.Match(hardwareId, @"VID_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var pidMatch = System.Text.RegularExpressions.Regex.Match(hardwareId, @"PID_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return (vidMatch.Success ? vidMatch.Groups[1].Value : null, 
                    pidMatch.Success ? pidMatch.Groups[1].Value : null);
        }

        /// <summary>
        /// Extract Unique ID from VMulti Device Path (Requested by User)
        /// (EN/FR: Extraire ID Unique du chemin VMulti)
        /// Format: ...\1&[UNIQUE_ID]&...
        /// </summary>
        private static string ExtractVMultiIdFromPath(string devicePath)
        {
            try 
            {
                // Format example: HID\VMULTIA&COL03\1&2D595CA7&1E&0002
                // We want: 2D595CA7
                
                int lastSlash = devicePath.LastIndexOf('\\');
                if (lastSlash >= 0 && lastSlash < devicePath.Length - 1)
                {
                    // Get '1&2D595CA7&1E&0002'
                    string instanceId = devicePath.Substring(lastSlash + 1);
                    
                    // Split by '&'
                    string[] parts = instanceId.Split('&');
                    
                    // The unique ID is typically the 2nd part (index 1)
                    if (parts.Length >= 2)
                    {
                        return parts[1];
                    }
                }
            }
            catch 
            {
                // Ignore parsing errors
            }
            return "Unknown";
        }

        /// <summary>
        /// Check if two Hardware IDs match (partial/fuzzy matching) (EN/FR: Vérifier si deux Hardware IDs correspondent - matching partiel)
        /// This fixes the issue where exact string match fails due to variations in Hardware ID format
        /// </summary>
        public static bool IsHardwareIdMatch(string preferred, string current)
        {
            if (string.IsNullOrEmpty(preferred) || string.IsNullOrEmpty(current))
                return false;

            // Normalize (EN/FR: Normaliser)
            preferred = preferred.ToUpperInvariant();
            current = current.ToUpperInvariant();

            // Exact match (EN/FR: Correspondance exacte)
            if (preferred == current)
            {
                SimpleLogger.Instance.Debug($"[HW ID MATCH] Exact: {preferred}");
                return true;
            }

            // Extract VID/PID if present (EN/FR: Extraire VID/PID si présent)
            var (preferredVid, preferredPid) = ExtractVidPid(preferred);
            var (currentVid, currentPid) = ExtractVidPid(current);

            // VID/PID match is good enough (EN/FR: Correspondance VID/PID suffit)
            if (!string.IsNullOrEmpty(preferredVid) && preferredVid == currentVid)
            {
                // If both have PID, they must match
                if (!string.IsNullOrEmpty(preferredPid) && !string.IsNullOrEmpty(currentPid))
                {
                    if (preferredPid == currentPid)
                    {
                        SimpleLogger.Instance.Debug($"[HW ID MATCH] VID/PID: {preferredVid}_{preferredPid} matches {currentVid}_{currentPid}");
                        return true;
                    }
                }
                // If one is missing PID (e.g. VMulti), match based on VID only
                else
                {
                    SimpleLogger.Instance.Debug($"[HW ID MATCH] VID match (PID ignored/missing): {preferredVid}");
                    return true;
                }
            }

            // Partial match: check if significant parts overlap (EN/FR: Correspondance partielle : vérifier parties significatives)
            var preferredParts = preferred.Split('\\', '&', '#');
            var currentParts = current.Split('\\', '&', '#');

            int matchCount = 0;
            foreach (var prefPart in preferredParts)
            {
                // Skip very short or common parts (EN/FR: Ignorer parties très courtes ou communes)
                if (string.IsNullOrWhiteSpace(prefPart) || prefPart.Length < 3)
                    continue;

                // Skip generic prefixes (EN/FR: Ignorer préfixes génériques)
                if (prefPart == "HID" || prefPart == "USB" || prefPart.StartsWith("COL"))
                    continue;

                foreach (var currPart in currentParts)
                {
                    if (prefPart.Equals(currPart, StringComparison.OrdinalIgnoreCase))
                    {
                        matchCount++;
                        break;
                    }
                }
            }

            // At least 2 significant segments must match (EN/FR: Au moins 2 segments significatifs doivent correspondre)
            if (matchCount >= 2)
            {
                SimpleLogger.Instance.Debug($"[HW ID MATCH] Partial: {matchCount} segments match between preferred and current");
                return true;
            }

            SimpleLogger.Instance.Debug($"[HW ID MATCH] No match: {preferred} vs {current}");
            return false;
        }

        // Missing P/Invoke definitions for IsDeviceConnected
        private static Guid HidGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            int MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            out int RequiredSize,
            IntPtr DeviceInfoData);

        private static string GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            int bufferSize = 0;
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, out bufferSize, IntPtr.Zero);

            if (bufferSize == 0) return null;

            IntPtr detailDataBuffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                SP_DEVICE_INTERFACE_DETAIL_DATA detailData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                detailData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
                
                // On 64-bit systems, cbSize must be 8. On 32-bit, it's 5. But Marshal.SizeOf returns correct size for struct.
                // However, for SetupDiGetDeviceInterfaceDetail, cbSize is strictly defined as sizeof(FIXED_PART).
                if (IntPtr.Size == 8) detailData.cbSize = 8;
                else detailData.cbSize = 5;

                Marshal.StructureToPtr(detailData, detailDataBuffer, false);

                if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, bufferSize, out _, IntPtr.Zero))
                {
                    var pDevicePathName = (IntPtr)((long)detailDataBuffer + 4); // Skip cbSize (4 bytes)
                    return Marshal.PtrToStringAuto(pDevicePathName);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(detailDataBuffer);
            }
            return null;
        }

        private static string GetHardwareId(string devicePath)
        {
            // This is a simplified version. Ideally we should use SetupDiGetDeviceRegistryProperty with SPDRP_HARDWAREID
            // But since we already have the device path, we can try to extract VID/PID from it directly if it contains it.
            // Device paths usually look like \\?\hid#vid_xxxx&pid_xxxx...
            
            // However, IsDeviceConnected uses SetupDiEnumDeviceInterfaces which gives us SP_DEVICE_INTERFACE_DATA.
            // We can get the DevInst from SP_DEVINFO_DATA corresponding to the interface.
            // But here we are iterating interfaces.
            
            // Let's implement a proper GetHardwareId using the DeviceInfoSet and InterfaceData
            // Actually, IsDeviceConnected logic I wrote earlier calls GetHardwareId(devicePath).
            // Let's just extract it from the string for simplicity as it usually contains VID/PID.
            return devicePath;
        }

        /// <summary>
        /// Check if a device with specific VID is connected (EN/FR: Vérifier si un périphérique avec VID spécifique est connecté)
        /// </summary>
        public static bool IsDeviceConnected(string vid)
        {
            if (string.IsNullOrEmpty(vid)) return false;

            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref HidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet == IntPtr.Zero) return false;

            try
            {
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                for (int i = 0; SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref HidGuid, i, ref deviceInterfaceData); i++)
                {
                    string devicePath = GetDevicePath(deviceInfoSet, ref deviceInterfaceData);
                    if (string.IsNullOrEmpty(devicePath)) continue;

                    // Check if device path contains VID
                    // Device path format: \\?\hid#vid_002f&pid_0001...
                    if (devicePath.IndexOf($"vid_{vid}", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error checking connected devices: {ex.Message}");
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }
        /// <summary>
        /// Find the Unique/Instance ID of a VMulti device (mouse or keyboard) by scanning Windows devices via SetupAPI.
        /// Works for both enabled and disabled devices.
        /// (EN/FR: Trouver l'ID Unique VMulti (souris ou clavier) via SetupAPI - fonctionne pour devices activés et désactivés)
        /// </summary>
        public static string FindVMultiMouseUniqueId(string vmultiSuffix)
        {
            return FindVMultiUniqueId(vmultiSuffix);
        }

        /// <summary>
        /// Find the Unique/Instance ID of a VMulti device by scanning Windows devices via SetupAPI.
        /// Useful when Interception provides a truncated hardware ID.
        /// (EN/FR: Trouver l'ID Unique VMulti via SetupAPI quand Interception donne un ID tronqué)
        /// </summary>
        private static string FindVMultiUniqueId(string vmultiSuffix)
        {
            string foundId = "Unknown";
            IntPtr deviceInfoSet = IntPtr.Zero;
            
            try
            {
                // GUID HID Class
                Guid hidGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");
                
                deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                
                if (deviceInfoSet != IntPtr.Zero && deviceInfoSet.ToInt64() != -1)
                {
                    SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
                    devInfo.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                    
                    for (int i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref devInfo); i++)
                    {
                        StringBuilder instanceIdSb = new StringBuilder(1024);
                        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfo, instanceIdSb, instanceIdSb.Capacity, out _))
                        {
                            string instanceId = instanceIdSb.ToString();
                            // Look for something like HID\VMULTIA&COL03\1&2D595CA7&1E&0002
                            // Check if it contains the suffix (e.g. "vmultia")
                            if (instanceId.IndexOf(vmultiSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // Extract the unique part
                                // Format: ...\1&[UNIQUE_ID]&...
                                var parts = instanceId.Split('&');
                                // Usually the unique part is the second element (index 1) after the last slash section
                                // Let's try to verify with the logic:
                                // HID\VMULTIA&COL03 \ 1&2D595CA7&1E&0002
                                // 1 -> 2D595CA7 -> 1E -> 0002
                                
                                int lastSlash = instanceId.LastIndexOf('\\');
                                if (lastSlash >= 0)
                                {
                                    string relPath = instanceId.Substring(lastSlash + 1);
                                    var subParts = relPath.Split('&');
                                    // Usually index 1 is the unique hash generated by Windows for the device instance
                                    if (subParts.Length > 1) 
                                    {
                                        foundId = subParts[1]; // Return the first candidate found
                                        SimpleLogger.Instance.Info($"[VMulti Lookup] Found real ID for {vmultiSuffix}: {foundId} in {instanceId}");
                                        return foundId;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error finding VMulti ID for {vmultiSuffix}: {ex.Message}");
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero && deviceInfoSet.ToInt64() != -1)
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            
            return foundId;
        }

    }
}
