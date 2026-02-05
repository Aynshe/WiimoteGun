using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using WiimoteGun.VMulti;

namespace WiimoteGun
{
    /// <summary>
    /// VMulti HID Client - Direct communication with vmulti drivers without Interception
    /// (EN/FR: Client HID VMulti - Communication directe avec les pilotes vmulti sans Interception)
    /// </summary>
    public class VMultiClient : IDisposable
    {
        #region Native API Imports

        // SetupAPI for device enumeration (EN/FR: SetupAPI pour énumération des périphériques)
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

        // HID API (EN/FR: API HID)
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

        // Kernel32 for file handles (EN/FR: Kernel32 pour les handles de fichiers)
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        // Constants
        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;
        private const int HIDP_STATUS_SUCCESS = 0x110000;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string devicePath;
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
        private SafeFileHandle _controlHandle;
        private FileStream _controlStream;
        private string _devicePath;
        private bool _isConnected;
        private static readonly object _globalLock = new object();

        // Static device cache for all players (EN/FR: Cache statique des périphériques pour tous les joueurs)
        private static Dictionary<int, VMultiClient> _activeClients;

        static VMultiClient()
        {
            _activeClients = new Dictionary<int, VMultiClient>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Player index (1-4) (EN/FR: Index du joueur)
        /// </summary>
        public int PlayerIndex { get { return _playerIndex; } }

        /// <summary>
        /// Connection status (EN/FR: Statut de connexion)
        /// </summary>
        public bool IsConnected { get { return _isConnected && _controlHandle != null && !_controlHandle.IsClosed; } }

        /// <summary>
        /// Device path (EN/FR: Chemin du périphérique)
        /// </summary>
        public string DevicePath { get { return _devicePath; } }

        #endregion

        #region Constructor & Disposal

        /// <summary>
        /// Create a VMulti client for a specific player
        /// (EN/FR: Créer un client VMulti pour un joueur spécifique)
        /// </summary>
        /// <param name="playerIndex">Player index 1-4</param>
        public VMultiClient(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4)
                throw new ArgumentOutOfRangeException("playerIndex", "Player index must be between 1 and 4");

            _playerIndex = playerIndex;
            _isConnected = false;

            lock (_globalLock)
            {
                // Check if a client already exists for this player
                if (_activeClients.ContainsKey(playerIndex))
                {
                    SimpleLogger.Instance.Warning(string.Format("[VMultiClient] Client for P{0} already exists. Disposing old instance.", playerIndex));
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
        /// Connect to the VMulti device for this player
        /// (EN/FR: Se connecter au périphérique VMulti pour ce joueur)
        /// </summary>
        public bool Connect()
        {
            if (_isConnected)
                return true;

            try
            {
                SimpleLogger.Instance.Info(string.Format("[VMultiClient] Connecting to vmulti device for Player {0}...", _playerIndex));

                // Find the VMulti device for this player
                _devicePath = FindVMultiDevice(_playerIndex);

                if (string.IsNullOrEmpty(_devicePath))
                {
                    SimpleLogger.Instance.Error(string.Format("[VMultiClient] Could not find vmulti device for Player {0}", _playerIndex));
                    return false;
                }

                SimpleLogger.Instance.Info(string.Format("[VMultiClient] Found device: {0}", _devicePath));

                // Open the device for writing (EN/FR: Ouvrir le périphérique en écriture)
                _controlHandle = CreateFile(
                    _devicePath,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (_controlHandle.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    SimpleLogger.Instance.Error(string.Format("[VMultiClient] Failed to open device. Error: {0}", error));
                    return false;
                }

                // Set input buffer count (EN/FR: Définir le nombre de buffers d'entrée)
                HidD_SetNumInputBuffers(_controlHandle, 10);

                // Create file stream for easier writing (EN/FR: Créer un FileStream pour écriture simplifiée)
                _controlStream = new FileStream(_controlHandle, FileAccess.ReadWrite, VMultiControlReport.Size, false);

                _isConnected = true;
                SimpleLogger.Instance.Info(string.Format("[VMultiClient] Successfully connected to vmulti for Player {0}", _playerIndex));

                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[VMultiClient] Connection error for P{0}: {1}", _playerIndex, ex.Message));
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the VMulti device
        /// (EN/FR: Se déconnecter du périphérique VMulti)
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;

            if (_controlStream != null)
            {
                try { _controlStream.Close(); } catch { }
                _controlStream = null;
            }

            if (_controlHandle != null && !_controlHandle.IsClosed)
            {
                try { _controlHandle.Close(); } catch { }
                _controlHandle = null;
            }

            SimpleLogger.Instance.Info(string.Format("[VMultiClient] Disconnected from vmulti for Player {0}", _playerIndex));
        }

        #endregion

        #region Device Detection

        /// <summary>
        /// Find the VMulti device path for a specific player
        /// (EN/FR: Trouver le chemin du périphérique VMulti pour un joueur spécifique)
        /// </summary>
        private string FindVMultiDevice(int playerIndex)
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
                SimpleLogger.Instance.Error("[VMultiClient] SetupDiGetClassDevs failed");
                return null;
            }

            try
            {
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                uint memberIndex = 0;
                string targetSuffix = GetVMultiSuffix(playerIndex);
                ushort targetVid = VMultiDeviceIds.PlayerVids[playerIndex - 1];

                SimpleLogger.Instance.Info(string.Format("[VMultiClient] Searching for vmulti device: suffix={0}, VID=0x{1:X4}", targetSuffix, targetVid));

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, memberIndex++, ref deviceInterfaceData))
                {
                    string devicePath = GetDevicePath(deviceInfoSet, ref deviceInterfaceData);
                    
                    if (string.IsNullOrEmpty(devicePath))
                        continue;

                    // Check by device path containing vmulti suffix (EN/FR: Vérifier par chemin contenant le suffixe vmulti)
                    if (devicePath.IndexOf(targetSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Verify it's a control device (Usage 0x0001) (EN/FR: Vérifier que c'est un périphérique de contrôle)
                        if (IsVMultiControlDevice(devicePath, targetVid))
                        {
                            return devicePath;
                        }
                    }

                    // Alternative: Check by VID/PID (EN/FR: Alternative: Vérifier par VID/PID)
                    if (IsVMultiDeviceByVidPid(devicePath, targetVid, VMultiDeviceIds.DefaultPid))
                    {
                        if (IsVMultiControlDevice(devicePath, targetVid))
                        {
                            return devicePath;
                        }
                    }
                }

                SimpleLogger.Instance.Warning(string.Format("[VMultiClient] No device found for Player {0}", playerIndex));
                return null;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        /// <summary>
        /// Get device path from device interface data
        /// (EN/FR: Obtenir le chemin du périphérique depuis les données d'interface)
        /// </summary>
        private string GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            uint requiredSize = 0;

            // First call to get required size (EN/FR: Premier appel pour obtenir la taille requise)
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

            if (requiredSize == 0)
                return null;

            // Allocate memory for detail data (EN/FR: Allouer mémoire pour les données détaillées)
            IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);

            try
            {
                // Set cbSize - different for 32-bit and 64-bit (EN/FR: Définir cbSize - différent pour 32-bit et 64-bit)
                Marshal.WriteInt32(detailDataBuffer, IntPtr.Size == 8 ? 8 : 6);

                if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, requiredSize, out requiredSize, IntPtr.Zero))
                {
                    // Device path starts at offset 4 (EN/FR: Le chemin commence à l'offset 4)
                    return Marshal.PtrToStringAuto(IntPtr.Add(detailDataBuffer, 4));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(detailDataBuffer);
            }

            return null;
        }

        /// <summary>
        /// Check if device is the VMulti control device (Usage 0x0001)
        /// (EN/FR: Vérifier si le périphérique est le périphérique de contrôle VMulti)
        /// </summary>
        private bool IsVMultiControlDevice(string devicePath, ushort expectedVid)
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
                // Check VID/PID (EN/FR: Vérifier VID/PID)
                HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
                attributes.Size = Marshal.SizeOf(attributes);

                if (!HidD_GetAttributes(handle, ref attributes))
                    return false;

                // Check if VID matches (EN/FR: Vérifier si le VID correspond)
                // Allow either the custom VID or the default VMulti VID
                bool vidMatch = (attributes.VendorID == expectedVid) || 
                               (attributes.VendorID == VMultiDeviceIds.DefaultVid);

                if (!vidMatch)
                    return false;

                // Check usage page and usage (EN/FR: Vérifier la page d'usage et l'usage)
                IntPtr preparsedData;
                if (!HidD_GetPreparsedData(handle, out preparsedData))
                    return false;

                try
                {
                    HIDP_CAPS caps = new HIDP_CAPS();
                    if (HidP_GetCaps(preparsedData, ref caps) != HIDP_STATUS_SUCCESS)
                        return false;

                    // Control device: UsagePage=0xFF00, Usage=0x0001
                    return (caps.UsagePage == VMultiDeviceIds.ControlUsagePage && 
                            caps.Usage == VMultiDeviceIds.ControlUsage);
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

        /// <summary>
        /// Check if device matches VID/PID
        /// (EN/FR: Vérifier si le périphérique correspond au VID/PID)
        /// </summary>
        private bool IsVMultiDeviceByVidPid(string devicePath, ushort vid, ushort pid)
        {
            // Quick path-based check (EN/FR: Vérification rapide par chemin)
            string lowerPath = devicePath.ToLowerInvariant();
            string vidStr = string.Format("vid_{0:x4}", vid);
            string pidStr = string.Format("pid_{0:x4}", pid);

            return lowerPath.Contains(vidStr) && lowerPath.Contains(pidStr);
        }

        /// <summary>
        /// Get VMulti suffix for player index
        /// (EN/FR: Obtenir le suffixe VMulti pour l'index du joueur)
        /// </summary>
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

        #region Mouse Control

        /// <summary>
        /// Update absolute mouse position and buttons
        /// (EN/FR: Mettre à jour la position absolue de la souris et les boutons)
        /// </summary>
        /// <param name="x">X position (0-65535 for full screen)</param>
        /// <param name="y">Y position (0-65535 for full screen)</param>
        /// <param name="buttons">Button flags</param>
        /// <param name="wheel">Wheel position</param>
        public bool UpdateMouse(ushort x, ushort y, VMultiMouseButton buttons = VMultiMouseButton.None, byte wheel = 0)
        {
            if (!IsConnected)
            {
                if (!Connect())
                    return false;
            }

            try
            {
                // Create mouse report (EN/FR: Créer le rapport souris)
                VMultiMouseReport mouseReport = new VMultiMouseReport
                {
                    ReportID = VMultiReportIds.Mouse,
                    Button = (byte)buttons,
                    XValue = x,
                    YValue = y,
                    WheelPosition = wheel
                };

                // Wrap in control report (EN/FR: Encapsuler dans le rapport de contrôle)
                VMultiControlReport controlReport = VMultiControlReport.Create();
                controlReport.EmbedMouseReport(mouseReport);

                // Send via HID (EN/FR: Envoyer via HID)
                return SendReport(controlReport.ToByteArray());
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[VMultiClient] UpdateMouse error P{0}: {1}", _playerIndex, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Update mouse with normalized coordinates (0.0 to 1.0)
        /// (EN/FR: Mettre à jour la souris avec des coordonnées normalisées)
        /// </summary>
        public bool UpdateMouseNormalized(double x, double y, VMultiMouseButton buttons = VMultiMouseButton.None, byte wheel = 0)
        {
            // Clamp to valid range (EN/FR: Limiter à la plage valide)
            x = Math.Max(0.0, Math.Min(1.0, x));
            y = Math.Max(0.0, Math.Min(1.0, y));

            // Convert to 0-32767 range (VMulti coordinate space)
            ushort absX = (ushort)(x * VMultiMouseReport.MaxCoordinate);
            ushort absY = (ushort)(y * VMultiMouseReport.MaxCoordinate);

            return UpdateMouse(absX, absY, buttons, wheel);
        }

        #endregion

        #region Keyboard Control

        /// <summary>
        /// Update keyboard state
        /// (EN/FR: Mettre à jour l'état du clavier)
        /// </summary>
        /// <param name="modifiers">Modifier key flags</param>
        /// <param name="keyCodes">Up to 6 key codes</param>
        public bool UpdateKeyboard(VMultiKeyboardModifier modifiers, params byte[] keyCodes)
        {
            if (!IsConnected)
            {
                if (!Connect())
                    return false;
            }

            try
            {
                // Create keyboard report (EN/FR: Créer le rapport clavier)
                VMultiKeyboardReport keyboardReport = VMultiKeyboardReport.Create();
                keyboardReport.ShiftKeyFlags = (byte)modifiers;
                keyboardReport.SetKeyCodes(keyCodes ?? new byte[0]);

                // Wrap in control report (EN/FR: Encapsuler dans le rapport de contrôle)
                VMultiControlReport controlReport = VMultiControlReport.Create();
                controlReport.EmbedKeyboardReport(keyboardReport);

                // Send via HID (EN/FR: Envoyer via HID)
                return SendReport(controlReport.ToByteArray());
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[VMultiClient] UpdateKeyboard error P{0}: {1}", _playerIndex, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Send a single key press/release
        /// (EN/FR: Envoyer une pression/relâchement de touche unique)
        /// </summary>
        public bool SendKey(byte keyCode, bool pressed, VMultiKeyboardModifier modifiers = VMultiKeyboardModifier.None)
        {
            if (pressed)
            {
                return UpdateKeyboard(modifiers, keyCode);
            }
            else
            {
                // Release all keys (EN/FR: Relâcher toutes les touches)
                return UpdateKeyboard(VMultiKeyboardModifier.None);
            }
        }

        #endregion

        #region Low-Level Report Sending

        /// <summary>
        /// Send a raw HID report
        /// (EN/FR: Envoyer un rapport HID brut)
        /// </summary>
        private bool SendReport(byte[] report)
        {
            if (!IsConnected || _controlHandle == null || _controlHandle.IsClosed)
                return false;

            try
            {
                // Try WriteFile first (EN/FR: Essayer WriteFile d'abord)
                if (_controlStream != null)
                {
                    _controlStream.Write(report, 0, report.Length);
                    _controlStream.Flush();
                    return true;
                }

                // Fallback to SetOutputReport (EN/FR: Fallback vers SetOutputReport)
                return HidD_SetOutputReport(_controlHandle, report, (uint)report.Length);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[VMultiClient] SendReport error: {0}", ex.Message));
                
                // Try to reconnect on write error (EN/FR: Essayer de reconnecter sur erreur d'écriture)
                _isConnected = false;
                return false;
            }
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Check if VMulti device is available for a player
        /// (EN/FR: Vérifier si un périphérique VMulti est disponible pour un joueur)
        /// </summary>
        public static bool IsDeviceAvailable(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4)
                return false;

            using (VMultiClient tempClient = new VMultiClient(playerIndex))
            {
                string path = tempClient.FindVMultiDevice(playerIndex);
                return !string.IsNullOrEmpty(path);
            }
        }

        /// <summary>
        /// Get list of all available VMulti devices
        /// (EN/FR: Obtenir la liste de tous les périphériques VMulti disponibles)
        /// </summary>
        public static List<int> GetAvailablePlayers()
        {
            List<int> availablePlayers = new List<int>();

            for (int i = 1; i <= 4; i++)
            {
                if (IsDeviceAvailable(i))
                {
                    availablePlayers.Add(i);
                }
            }

            return availablePlayers;
        }

        #endregion
    }
}
