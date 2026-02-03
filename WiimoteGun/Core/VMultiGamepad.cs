using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WiimoteGun.VMulti;

namespace WiimoteGun
{
    /// <summary>
    /// EN: VMulti GamePad Client - Communication with Col06 gamepad device.
    /// FR: Client VMulti GamePad - Communication avec le périphérique gamepad Col06.
    /// </summary>
    public class VMultiGamepad : IDisposable
    {
        #region Native API Imports

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            IntPtr enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo,
            IntPtr devInfo,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            out uint requiredSize,
            IntPtr deviceInfoData);

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

        [DllImport("hid.dll")]
        private static extern bool HidD_SetNumInputBuffers(SafeFileHandle hidDeviceObject, uint numBuffers);

        [DllImport("hid.dll")]
        private static extern bool HidD_SetOutputReport(SafeFileHandle hidDeviceObject, byte[] reportBuffer, uint reportBufferLength);

        [DllImport("hid.dll")]
        private static extern bool HidD_SetFeature(SafeFileHandle hidDeviceObject, byte[] reportBuffer, uint reportBufferLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;
        private const int HIDP_STATUS_SUCCESS = 0x110000;

        // Col06 = Joystick/Gamepad collection (EN/FR: Collection Joystick/Gamepad)
        private const ushort GAMEPAD_USAGE_PAGE = 0x01;  // Generic Desktop
        private const ushort GAMEPAD_USAGE = 0x04;       // Joystick

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

        #region Instance Fields

        private readonly int _playerIndex;
        private SafeFileHandle _deviceHandle;
        private FileStream _deviceStream;
        private string _devicePath;
        private bool _isConnected;
        private VMultiGamepadReport _currentReport;
        private static readonly object _globalLock = new object();
        private static Dictionary<int, VMultiGamepad> _activeClients = new Dictionary<int, VMultiGamepad>();

        #endregion

        #region Properties

        public int PlayerIndex => _playerIndex;
        public bool IsConnected => _isConnected && _deviceHandle != null && !_deviceHandle.IsClosed;
        public string DevicePath => _devicePath;

        #endregion

        #region Constructor & Disposal

        /// <summary>
        /// EN: Create a VMulti Gamepad client for a specific player.
        /// FR: Créer un client VMulti Gamepad pour un joueur spécifique.
        /// </summary>
        public VMultiGamepad(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player index must be between 1 and 4");

            _playerIndex = playerIndex;
            _isConnected = false;
            _currentReport = VMultiGamepadReport.Create();

            lock (_globalLock)
            {
                if (_activeClients.ContainsKey(playerIndex))
                {
                    SimpleLogger.Instance.Warning($"[VMultiGamepad] Client for P{playerIndex} already exists. Disposing old instance.");
                    _activeClients[playerIndex].Dispose();
                }
                _activeClients[playerIndex] = this;
            }
        }

        public void Dispose()
        {
            Disconnect();

            lock (_globalLock)
            {
                if (_activeClients.ContainsKey(_playerIndex) && _activeClients[_playerIndex] == this)
                {
                    _activeClients.Remove(_playerIndex);
                }
            }
        }

        #endregion

        #region Connection Methods

        /// <summary>
        /// EN: Connect to the VMulti gamepad device (Col06) for this player.
        /// FR: Se connecter au périphérique gamepad VMulti (Col06) pour ce joueur.
        /// </summary>
        public bool Connect()
        {
            if (_isConnected)
                return true;

            try
            {
                SimpleLogger.Instance.Info($"[VMultiGamepad] Connecting to VMulti Control Interface for Player {_playerIndex}...");

                _devicePath = FindGamepadDevice(_playerIndex);

                if (string.IsNullOrEmpty(_devicePath))
                {
                    SimpleLogger.Instance.Error($"[VMultiGamepad] Could not find gamepad device for Player {_playerIndex}");
                    return false;
                }

                SimpleLogger.Instance.Info($"[VMultiGamepad] Found device: {_devicePath}");

                _deviceHandle = CreateFile(
                    _devicePath,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (_deviceHandle.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    SimpleLogger.Instance.Error($"[VMultiGamepad] Failed to open device. Error: {error}");
                    return false;
                }

                HidD_SetNumInputBuffers(_deviceHandle, 10);
                _deviceStream = new FileStream(_deviceHandle, FileAccess.ReadWrite, VMultiControlReport.Size, false);

                _isConnected = true;
                _currentReport = VMultiGamepadReport.Create();

                SimpleLogger.Instance.Info($"[VMultiGamepad] Successfully connected to gamepad for Player {_playerIndex}");
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiGamepad] Connection error for P{_playerIndex}: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;

            if (_deviceStream != null)
            {
                try { _deviceStream.Close(); } catch { }
                _deviceStream = null;
            }

            if (_deviceHandle != null && !_deviceHandle.IsClosed)
            {
                try { _deviceHandle.Close(); } catch { }
                _deviceHandle = null;
            }

            SimpleLogger.Instance.Info($"[VMultiGamepad] Disconnected gamepad for Player {_playerIndex}");
        }

        #endregion

        #region Device Detection

        /// <summary>
        /// EN: Find the VMulti gamepad device path (Col06) for a specific player.
        /// FR: Trouver le chemin du périphérique gamepad VMulti (Col06) pour un joueur spécifique.
        /// </summary>
        private string FindGamepadDevice(int playerIndex)
        {
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);

            IntPtr deviceInfoSet = SetupDiGetClassDevs(
                ref hidGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == (IntPtr)(-1))
            {
                SimpleLogger.Instance.Error("[VMultiGamepad] SetupDiGetClassDevs failed");
                return null;
            }

            try
            {
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                uint memberIndex = 0;
                string targetSuffix = GetVMultiSuffix(playerIndex);
                ushort targetVid = VMultiDeviceIds.PlayerVids[playerIndex - 1];

                SimpleLogger.Instance.Info($"[VMultiGamepad] Searching for vmulti control: suffix={targetSuffix}, VID=0x{targetVid:X4}");

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, memberIndex++, ref deviceInterfaceData))
                {
                    string devicePath = GetDevicePath(deviceInfoSet, ref deviceInterfaceData);

                    if (string.IsNullOrEmpty(devicePath))
                        continue;

                    // Check for Col06 in path (EN/FR: Vérifier Col06 dans le chemin)
                    // REMOVED: We need Control Interface (usually Col08 or similar), not Gamepad Interface (Col06)
                    // if (devicePath.IndexOf("col06", StringComparison.OrdinalIgnoreCase) < 0)
                    //    continue;

                    // Check by device path containing vmulti suffix
                    if (devicePath.IndexOf(targetSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (IsGamepadDevice(devicePath, targetVid))
                        {
                            return devicePath;
                        }
                    }
                }

                SimpleLogger.Instance.Warning($"[VMultiGamepad] No gamepad device found for Player {playerIndex}");
                return null;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        private string GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            uint requiredSize = 0;
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

            if (requiredSize == 0)
                return null;

            IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);

            try
            {
                Marshal.WriteInt32(detailDataBuffer, IntPtr.Size == 8 ? 8 : 6);

                if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, requiredSize, out requiredSize, IntPtr.Zero))
                {
                    return Marshal.PtrToStringAuto(IntPtr.Add(detailDataBuffer, 4));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(detailDataBuffer);
            }

            return null;
        }

        private bool IsGamepadDevice(string devicePath, ushort expectedVid)
        {
            SafeFileHandle handle = CreateFile(
                devicePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
                return false;

            try
            {
                HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
                attributes.Size = Marshal.SizeOf(attributes);

                if (!HidD_GetAttributes(handle, ref attributes))
                    return false;

                bool vidMatch = (attributes.VendorID == expectedVid) ||
                               (attributes.VendorID == VMultiDeviceIds.DefaultVid);

                if (!vidMatch)
                    return false;

                IntPtr preparsedData;
                if (!HidD_GetPreparsedData(handle, out preparsedData))
                    return false;

                try
                {
                    HIDP_CAPS caps = new HIDP_CAPS();
                    if (HidP_GetCaps(preparsedData, ref caps) != HIDP_STATUS_SUCCESS)
                        return false;

                    // Debug Log Caps
                    SimpleLogger.Instance.Info($"[VMultiGamepad] HID Caps for {devicePath}: Input={caps.InputReportByteLength}, Output={caps.OutputReportByteLength}, Feature={caps.FeatureReportByteLength}");

                    // Check for VMulti Control Interface (UsagePage=0xFF00, Usage=0x01)
                    // Accessing this interface allows sending reports to any VMulti sub-device (Mouse, Keyboard, Gamepad)
                    // (EN/FR: Interface de contrôle VMulti : permet d'envoyer des rapports à tous les sous-périphériques)
                    return (caps.UsagePage == 0xFF00 && caps.Usage == 0x01);
                }
                finally
                {
                    HidD_FreePreparsedData(preparsedData);
                }
            }
            finally
            {
                handle.Close();
            }
        }

        private static string GetVMultiSuffix(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return "vmultia";
                case 2: return "vmultib";
                case 3: return "vmultic";
                case 4: return "vmultid";
                default: return "vmultia";
            }
        }

        #endregion

        #region Gamepad Control

        /// <summary>
        /// EN: Update button state.
        /// FR: Mettre à jour l'état d'un bouton.
        /// </summary>
        public void SetButton(GamePadButton button, bool pressed)
        {
            _currentReport.SetButton(button, pressed);
        }

        /// <summary>
        /// EN: Update axis from normalized values (-1.0 to 1.0).
        /// FR: Mettre à jour un axe depuis des valeurs normalisées (-1.0 à 1.0).
        /// </summary>
        public void SetAxis(GamePadAxis axis, float x, float y)
        {
            _currentReport.SetAxis(axis, x, y);
        }

        /// <summary>
        /// EN: Send the current gamepad state to the device.
        /// FR: Envoyer l'état actuel du gamepad au périphérique.
        /// </summary>
        /// <summary>
        /// EN: Send a specific gamepad report.
        /// FR: Envoyer un rapport gamepad spécifique.
        /// </summary>
        public bool SendReport(VMultiGamepadReport report)
        {
            _currentReport = report;
            return SendReport();
        }

        public bool SendReport()
        {
            if (!IsConnected)
            {
                if (!Connect())
                    return false;
            }

            try
            {
                VMultiControlReport controlReport = VMultiControlReport.Create();
                controlReport.EmbedGamepadReport(_currentReport);

                return SendRawReport(controlReport.ToByteArray());
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiGamepad] SendReport error P{_playerIndex}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// EN: Update all inputs and send report in one call.
        /// FR: Mettre à jour toutes les entrées et envoyer le rapport en un seul appel.
        /// </summary>
        public bool UpdateGamepad(
            float leftStickX = 0, float leftStickY = 0,
            float rightStickX = 0, float rightStickY = 0,
            VMultiGamepadButtons1 buttons1 = VMultiGamepadButtons1.None,
            VMultiGamepadButtons2 buttons2 = VMultiGamepadButtons2.None)
        {
            _currentReport.SetAxis(GamePadAxis.LeftStick, leftStickX, leftStickY);
            _currentReport.SetAxis(GamePadAxis.RightStick, rightStickX, rightStickY);
            
            // Map Buttons1 (A, B, X, Y, LB, RB, LT, RT) - 1:1 mapping to low byte
            _currentReport.Buttons = (ushort)buttons1;

            // Map Buttons2 (Back, Start, L3, R3) - Low nibble to high byte
            // Note: We use |= to preserve Buttons1
            _currentReport.Buttons |= (ushort)((byte)(buttons2 & (VMultiGamepadButtons2)0x0F) << 8);

            // Handle DPad (Hat Switch) logic via SetButton helper to update Hat
            // We use SetButton because it encapsulates the Hat logic
            _currentReport.SetButton(GamePadButton.DPadUp, (buttons2 & VMultiGamepadButtons2.DPadUp) != 0);
            _currentReport.SetButton(GamePadButton.DPadDown, (buttons2 & VMultiGamepadButtons2.DPadDown) != 0);
            _currentReport.SetButton(GamePadButton.DPadLeft, (buttons2 & VMultiGamepadButtons2.DPadLeft) != 0);
            _currentReport.SetButton(GamePadButton.DPadRight, (buttons2 & VMultiGamepadButtons2.DPadRight) != 0);

            return SendReport();
        }

        /// <summary>
        /// EN: Reset all inputs to neutral state.
        /// FR: Réinitialiser toutes les entrées à l'état neutre.
        /// </summary>
        public bool ResetAll()
        {
            _currentReport = VMultiGamepadReport.Create();
            return SendReport();
        }

        private bool SendRawReport(byte[] report)
        {
            if (!IsConnected || _deviceHandle == null || _deviceHandle.IsClosed)
                return false;

            try
            {
                // 1. Try WriteFile (Standard HID Output)
                if (_deviceStream != null)
                {
                    try
                    {
                        _deviceStream.Write(report, 0, report.Length);
                        _deviceStream.Flush();
                        return true;
                    }
                    catch (IOException)
                    {
                        // Function Incorrect (1) often happens here if WriteFile is not supported
                        // Don't log error yet, try fallbacks
                        // SimpleLogger.Instance.Debug($"[VMultiGamepad] WriteFile failed: {ex.Message}. Trying fallbacks...");
                    }
                }

                // 2. Try SetOutputReport (Control Pipe Output)
                if (HidD_SetOutputReport(_deviceHandle, report, (uint)report.Length))
                    return true;

                // 3. Try SetFeature (Control Pipe Feature - used by some VMulti variants)
                if (HidD_SetFeature(_deviceHandle, report, (uint)report.Length))
                    return true;

                // All failed
                int err = Marshal.GetLastWin32Error();
                SimpleLogger.Instance.Error($"[VMultiGamepad] SendRawReport all methods failed. LastError: {err}");
                return false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiGamepad] SendRawReport fatal error: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// EN: Check if gamepad device is available for a player.
        /// FR: Vérifier si un périphérique gamepad est disponible pour un joueur.
        /// </summary>
        public static bool IsDeviceAvailable(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4)
                return false;

            using (VMultiGamepad tempClient = new VMultiGamepad(playerIndex))
            {
                string path = tempClient.FindGamepadDevice(playerIndex);
                return !string.IsNullOrEmpty(path);
            }
        }

        #endregion
    }
}
