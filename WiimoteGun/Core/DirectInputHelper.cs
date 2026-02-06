using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Linq;
using System.Threading;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN/FR: Helper class to enumerate joysticks and identify DirectInput indices.
    /// Classe d'aide pour énumérer les joysticks et identifier les index DirectInput.
    /// </summary>
    public static class DirectInputHelper
    {
        #region Native API (SetupAPI & HID)

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        private static extern bool HidD_GetProductString(SafeFileHandle hidDeviceObject, byte[] buffer, uint bufferLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        #endregion

        private const int INVALID_HANDLE_VALUE = -1;
        private const int HIDP_STATUS_SUCCESS = 0x110000;

        #region WinMM API (Legacy fallback or info)
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct JOYCAPS
        {
            public ushort wMid;
            public ushort wPid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint wXmin;
            public uint wXmax;
            public uint wYmin;
            public uint wYmax;
            public uint wZmin;
            public uint wZmax;
            public uint wNumButtons;
            public uint wPeriodMin;
            public uint wPeriodMax;
            public uint wRmin;
            public uint wRmax;
            public uint wUmin;
            public uint wUmax;
            public uint wVmin;
            public uint wVmax;
            public uint wCaps;
            public uint wMaxAxes;
            public uint wNumAxes;
            public uint wMaxButtons;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szRegKey;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szOEMVxD;
        }

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint joyGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint joyGetDevCaps(IntPtr uJoyID, ref JOYCAPS pjc, uint cbjc);

        private const uint JOYERR_NOERROR = 0;
        
        #endregion

        /// <summary>
        /// EN/FR: Finds the DirectInput index (1-based) for a VMulti device by matching its VID and PID.
        /// Trouve l'index DirectInput (basé sur 1) pour un périphérique VMulti en comparant son VID et PID.
        /// </summary>
        public static int GetDirectInputIndex(ushort targetVid, ushort targetPid)
        {
            uint numDevs = joyGetNumDevs();
            JOYCAPS caps = new JOYCAPS();
            uint capsSize = (uint)Marshal.SizeOf(typeof(JOYCAPS));

            // joyGetDevCaps uses 0-based IDs for Joy1-Joy16
            for (uint i = 0; i < numDevs; i++)
            {
                if (joyGetDevCaps((IntPtr)i, ref caps, capsSize) == JOYERR_NOERROR)
                {
                    if (caps.wMid == targetVid || (targetVid == 0 && caps.szPname.Contains("VMulti")))
                    {
                        if (targetPid == 0 || caps.wPid == targetPid)
                        {
                            return (int)(i + 1); // Return 1-based index (Joy 1, Joy 2...)
                        }
                    }
                }
            }

            return -1; // Not found
        }

        /// <summary>
        /// EN/FR: Improved search using SetupAPI for VMulti devices with retry logic.
        /// Recherche améliorée utilisant SetupAPI pour les périphériques VMulti avec logique de retry.
        /// </summary>
        public static int FindVMultiGamepadIndex(int playerIndex)
        {
            ushort targetVid = 0;
            switch (playerIndex)
            {
                case 1: targetVid = 0x001F; break;
                case 2: targetVid = 0x002F; break;
                case 3: targetVid = 0x003F; break;
                case 4: targetVid = 0x004F; break;
            }

            if (targetVid == 0) return 0;

            SimpleLogger.Instance.Info(string.Format("[DInput] Predicting DInput index for Player {0} (VID=0x{1:X4}) using SetupAPI...", playerIndex, targetVid));

            // EN: Retry logic (Windows might take time to register the new gamepad)
            // FR: Logique de retry (Windows peut mettre du temps à enregistrer le nouveau gamepad)
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var controllers = new List<DetectedJoystick>();
                Guid hidGuid;
                HidD_GetHidGuid(out hidGuid);

                IntPtr hDevInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);
                if (hDevInfo != (IntPtr)INVALID_HANDLE_VALUE)
                {
                    try
                    {
                        SP_DEVICE_INTERFACE_DATA interfaceData = new SP_DEVICE_INTERFACE_DATA();
                        interfaceData.cbSize = Marshal.SizeOf(interfaceData);

                        uint index = 0;
                        while (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref hidGuid, index++, ref interfaceData))
                        {
                            uint size = 0;
                            SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, IntPtr.Zero, 0, out size, IntPtr.Zero);

                            if (size == 0) continue;

                            IntPtr detailPtr = Marshal.AllocHGlobal((int)size);
                            try
                            {
                                Marshal.WriteInt32(detailPtr, (IntPtr.Size == 4) ? 4 + Marshal.SystemDefaultCharSize : 8);
                                if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, detailPtr, size, out size, IntPtr.Zero))
                                {
                                    IntPtr pDevicePath = detailPtr + 4;
                                    string devicePath = Marshal.PtrToStringAuto(pDevicePath);

                                    // EN: Opened in READ mode to check if it's a Joystick
                                    using (SafeFileHandle handle = CreateFile(devicePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
                                    {
                                        if (handle.IsInvalid) continue;

                                        IntPtr preparsedData = IntPtr.Zero;
                                        if (HidD_GetPreparsedData(handle, out preparsedData))
                                        {
                                            try
                                            {
                                                HIDP_CAPS caps = new HIDP_CAPS();
                                                int status = HidP_GetCaps(preparsedData, ref caps);
                                                if (status == HIDP_STATUS_SUCCESS) 
                                                {
                                                    // Usage Page 1 (Generic Desktop), Usage 4 (Joystick) or 5 (GamePad)
                                                    if (caps.UsagePage == 1 && (caps.Usage == 4 || caps.Usage == 5))
                                                    {
                                                        HIDD_ATTRIBUTES attr = new HIDD_ATTRIBUTES();
                                                        attr.Size = Marshal.SizeOf(attr);
                                                        if (HidD_GetAttributes(handle, ref attr))
                                                        {
                                                            if (controllers.Any(x => x.DevicePath.Equals(devicePath, StringComparison.OrdinalIgnoreCase)))
                                                                continue;

                                                            string name = GetOemName(attr.VendorID, attr.ProductID);
                                                            controllers.Add(new DetectedJoystick
                                                            {
                                                                Vid = attr.VendorID,
                                                                Pid = attr.ProductID,
                                                                DevicePath = devicePath,
                                                                Name = name
                                                            });
                                                        }
                                                    }
                                                    else if (devicePath.Contains("vmulti"))
                                                    {
                                                        // Log why a vmulti device was skipped
                                                        SimpleLogger.Instance.Debug(string.Format("[DInput] Skipping vmulti interface: {0} (UsagePage=0x{1:X4}, Usage=0x{2:X4})", 
                                                            devicePath.Split('#')[1], caps.UsagePage, caps.Usage));
                                                    }
                                                }
                                                else
                                                {
                                                    SimpleLogger.Instance.Debug(string.Format("[DInput] HidP_GetCaps failed with status 0x{0:X8} for {1}", status, devicePath));
                                                }
                                            }
                                            finally
                                            {
                                                HidD_FreePreparsedData(preparsedData);
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(detailPtr);
                            }
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }

                if (controllers.Count > 0)
                {
                    for (int i = 0; i < controllers.Count; i++)
                    {
                        var c = controllers[i];
                        SimpleLogger.Instance.Info(string.Format("[DInput] Attempt {0}, List[{1}]: {2} (VID=0x{3:X4}, PID=0x{4:X4})", attempt, i, c.Name, c.Vid, c.Pid));
                    }

                    for (int i = 0; i < controllers.Count; i++)
                    {
                        if (controllers[i].Vid == targetVid)
                        {
                            SimpleLogger.Instance.Info(string.Format("[DInput] SUCCESS: P{0} (VID=0x{1:X4}) found at DInput Index {2}", playerIndex, targetVid, i + 1));
                            return i + 1;
                        }
                    }
                }

                if (attempt < 3)
                {
                    SimpleLogger.Instance.Debug(string.Format("[DInput] P{0} not found, retrying in 500ms...", playerIndex));
                    Thread.Sleep(500);
                }
            }

            SimpleLogger.Instance.Warning(string.Format("[DInput] FAILED: P{0} (VID=0x{1:X4}) not found after retries.", playerIndex, targetVid));
            return 0;
        }

        private struct DetectedJoystick
        {
            public string Name;
            public ushort Vid;
            public ushort Pid;
            public string DevicePath;
        }

        private static string GetOemName(ushort vid, ushort pid)
        {
            string regKey = string.Format("VID_{0:X4}&PID_{1:X4}", vid, pid);
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\" + regKey))
                {
                    if (key != null)
                    {
                        return key.GetValue("OEMName") as string ?? regKey;
                    }
                }
            }
            catch { }
            
            // Fallback for vmulti specifically if registry fails
            if (vid == 0x001F) return "Virtual Multitouch Device (VMulti P1)";
            if (vid == 0x002F) return "Virtual Multitouch Device (VMulti P2)";
            if (vid == 0x003F) return "Virtual Multitouch Device (VMulti P3)";
            if (vid == 0x004F) return "Virtual Multitouch Device (VMulti P4)";

            return regKey;
        }
    }
}
