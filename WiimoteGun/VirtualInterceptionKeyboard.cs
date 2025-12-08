using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using WiimoteGun.Interception;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual keyboard using Interception driver - supports multiple independent instances
    /// FR: Clavier virtuel utilisant le pilote Interception - supporte plusieurs instances indépendantes
    /// </summary>
    class VirtualInterceptionKeyboard : IVirtualJoy
    {
        private static IntPtr _context = IntPtr.Zero;
        private int _deviceId;
        private int _playerIndex;
        private static List<int> _availableKeyboards = new List<int>();
        private static List<VirtualInterceptionKeyboard> _instances = new List<VirtualInterceptionKeyboard>();

        // Mapping Wiimote buttons to Keys
        private Dictionary<InputKey, Keys> _keyMapping;
        private Dictionary<InputKey, bool> _buttonStates;

        // Public property to access current keyboard device ID (EN/FR: Propriété publique pour accéder à l'ID clavier actuel)
        public int KeyboardDeviceId => _deviceId;

        public VirtualInterceptionKeyboard(int playerIndex)
        {
            _playerIndex = playerIndex;
            _instances.Add(this);
            
            if (_context == IntPtr.Zero)
            {
                try
                {
                    _context = InterceptionDriver.interception_create_context();
                    ScanKeyboards();
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("Failed to create Interception context for keyboard: " + ex.Message);
                }
            }

            UpdateDeviceId();

            InitializeMapping();
            _buttonStates = new Dictionary<InputKey, bool>();
        }

        public static void RefreshDevices()
        {
            SimpleLogger.Instance.Info("Refreshing Interception Keyboard Devices...");
            ScanKeyboards();
            foreach (var instance in _instances)
            {
                instance.UpdateDeviceId();
            }
        }


        /// <summary>
        /// Refresh device ID assignment based on current preferences (EN/FR: Rafraîchir l'assignation du Device ID)
        /// </summary>
        public void RefreshDeviceId()
        {
            UpdateDeviceId();
        }

        private void UpdateDeviceId()
        {
            // Check if user forced a specific device ID (EN/FR: Vérifier si un Device ID est forcé)
            int forcedDeviceId = GetForcedDeviceId(_playerIndex);
            
            if (forcedDeviceId > 0)
            {
                _deviceId = forcedDeviceId;
                SimpleLogger.Instance.Info($"[KEYBOARD CONFIG] FORCED Keyboard Device ID {_deviceId} for Player {_playerIndex} (from settings.cfg)");
            }
            // Check if user has a preferred keyboard configured (EN/FR: Vérifier si un clavier préféré est configuré)
            else if (!Options.Instance.UseSharedKeyboard)
            {
                string preferredId = Options.Instance.GetPreferredKeyboardId(_playerIndex);
                
                // Try to find preferred keyboard in available list (EN/FR: Chercher le clavier préféré dans la liste disponible)
                if (!string.IsNullOrEmpty(preferredId) && _availableKeyboards.Count > 0)
                {
                    // Match by Hardware ID (VID/PID) for persistence across reboots
                    // (EN/FR: Correspondance par ID matériel pour persistance entre redémarrages)
                    foreach (int deviceId in _availableKeyboards)
                    {
                        string hardwareId = GetKeyboardHardwareId(deviceId);
                        if (!string.IsNullOrEmpty(hardwareId) && hardwareId == preferredId)
                        {
                            _deviceId = deviceId;
                            SimpleLogger.Instance.Info($"Assigned PREFERRED Keyboard Device ID {_deviceId} (Hardware: {hardwareId}) to Player {_playerIndex}");
                            return;
                        }
                    }
                }
                
                // Fallback to automatic assignment if preferred not found (EN/FR: Assignation automatique si préféré non trouvé)
                if (_availableKeyboards.Count > 0)
                {
                    int kbIndex = (_playerIndex - 1) % _availableKeyboards.Count;
                    _deviceId = _availableKeyboards[kbIndex];
                    SimpleLogger.Instance.Info($"Assigned AUTO Keyboard Device ID {_deviceId} to Player {_playerIndex} (preferred not found)");
                }
                else
                {
                    _deviceId = 1;
                    SimpleLogger.Instance.Warning($"No keyboards detected. Defaulting Player {_playerIndex} to Device ID 1.");
                }
            }
            // Shared keyboard mode (EN/FR: Mode clavier partagé)
            else if (_availableKeyboards.Count > 0)
            {
                // IMPORTANT: Check for VMulti preferred keyboard even in shared mode
                // (EN/FR: IMPORTANT: Vérifier le clavier VMulti préféré même en mode partagé)
                string preferredId = Options.Instance.GetPreferredKeyboardId(_playerIndex);
                
                // If VMulti keyboard is preferred, use it instead of default shared keyboard
                // (EN/FR: Si clavier VMulti préféré, l'utiliser au lieu du clavier partagé par défaut)
                if (!string.IsNullOrEmpty(preferredId))
                {
                    foreach (int deviceId in _availableKeyboards)
                    {
                        string hardwareId = GetKeyboardHardwareId(deviceId);
                        if (!string.IsNullOrEmpty(hardwareId) && hardwareId == preferredId)
                        {
                            _deviceId = deviceId;
                            SimpleLogger.Instance.Info($"Assigned PREFERRED Keyboard Device ID {_deviceId} (VMulti) to Player {_playerIndex} (shared mode overridden)");
                            return;
                        }
                    }
                }
                
                // Fallback to normal shared mode if no VMulti preference
                // (EN/FR: Retour au mode partagé normal si pas de préférence VMulti)
                _deviceId = _availableKeyboards[0];
                SimpleLogger.Instance.Info($"Assigned SHARED Keyboard Device ID {_deviceId} to Player {_playerIndex}");
            }
            else
            {
                // Fallback to ID 1 if no keyboards detected (should not happen if driver works)
                _deviceId = 1;
                SimpleLogger.Instance.Warning($"No keyboards detected via Interception. Defaulting Player {_playerIndex} to Device ID 1.");
            }
        }

        /// <summary>
        /// Get forced device ID from options if configured (EN/FR: Obtenir le Device ID forcé depuis les options)
        /// </summary>
        private int GetForcedDeviceId(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return Options.Instance.ForceKeyboardDeviceIdP1;
                case 2: return Options.Instance.ForceKeyboardDeviceIdP2;
                case 3: return Options.Instance.ForceKeyboardDeviceIdP3;
                case 4: return Options.Instance.ForceKeyboardDeviceIdP4;
                default: return 0;
            }
        }

        private static void ScanKeyboards()
        {
            _availableKeyboards.Clear();
            // Check all possible keyboards (EN/FR: Vérifier tous les claviers possibles)
            
            for (int i = 1; i <= InterceptionDriver.INTERCEPTION_MAX_KEYBOARD; i++)
            {
                if (InterceptionDriver.interception_is_keyboard(i) != 0)
                {
                    _availableKeyboards.Add(i);
                    // SimpleLogger.Instance.Info($"Detected Keyboard Device ID: {i}");
                }
            }
            
            SimpleLogger.Instance.Info($"Detected {_availableKeyboards.Count} keyboard(s) via Interception");
        }

        public static Dictionary<int, string> GetAvailableKeyboardsWithNames()
        {
            var result = new Dictionary<int, string>();
            
            if (_context == IntPtr.Zero)
            {
                try { _context = InterceptionDriver.interception_create_context(); }
                catch { return result; }
            }

            ScanKeyboards(); // Ensure list is up to date

            foreach (int deviceId in _availableKeyboards)
            {
                string hardwareId = GetKeyboardHardwareId(deviceId);
                string friendlyName = null;
                
                // Skip devices with no hardware ID (ghost/parasite devices)
                if (string.IsNullOrEmpty(hardwareId))
                    continue;

                if (!string.IsNullOrEmpty(hardwareId))
                {
                    // Pass hardwareId as both arguments: first for VID/PID extraction, second for VMulti path detection
                    friendlyName = DeviceHelper.GetKeyboardFriendlyName(hardwareId, hardwareId);
                }
                
                if (string.IsNullOrEmpty(friendlyName))
                {
                    friendlyName = $"Keyboard {deviceId}"; // Fallback (EN/FR: Nom par défaut)
                }
                
                result[deviceId] = friendlyName;
            }
            
            return result;
        }


        /// <summary>
        /// Get hardware ID for a specific keyboard device (EN/FR: Obtenir ID matériel pour un clavier spécifique)
        /// </summary>
        public static string GetKeyboardHardwareId(int deviceId)
        {
            if (_context == IntPtr.Zero) return null;

            byte[] buffer = new byte[1024];
            // Note: interception_get_hardware_id returns length in characters, not bytes
            uint length = InterceptionDriver.interception_get_hardware_id(_context, deviceId, buffer, (uint)buffer.Length);
            
            if (length > 0)
            {
                // Interception returns a wide string (wchar_t)
                // We need to convert bytes to string. Length is number of characters.
                string result = System.Text.Encoding.Unicode.GetString(buffer, 0, (int)length * 2);
                
                // Remove all NULL characters (0x00) to avoid XML serialization errors
                // (EN/FR: Supprimer tous les caractères NULL pour éviter erreurs XML)
                result = result.Replace("\0", "").Trim();
                
                return string.IsNullOrEmpty(result) ? null : result;
            }
            return null;
        }

        private void InitializeMapping()
        {
            _keyMapping = new Dictionary<InputKey, Keys>();
            
            // Default mapping
            _keyMapping[InputKey.up] = Keys.Up;
            _keyMapping[InputKey.down] = Keys.Down;
            _keyMapping[InputKey.left] = Keys.Left;
            _keyMapping[InputKey.right] = Keys.Right;
            
            _keyMapping[InputKey.a] = Keys.Enter;
            _keyMapping[InputKey.b] = Keys.Space;
            _keyMapping[InputKey.x] = Keys.Back; 
            _keyMapping[InputKey.y] = Keys.Tab;  
            _keyMapping[InputKey.start] = Keys.Escape; // Home -> Escape
            _keyMapping[InputKey.select] = Keys.D1; // Minus -> 1 (just for test)
        }

        public bool IsEnabled => _context != IntPtr.Zero;

        public void SetAxis(bool AxisX, int value)
        {
            if (AxisX)
            {
                SetButtonState(InputKey.left, value < 0);
                SetButtonState(InputKey.right, value > 0);
            }
            else
            {
                SetButtonState(InputKey.up, value < 0); // Corrected: Up is usually negative Y in joystick, but let's assume standard D-Pad
                SetButtonState(InputKey.down, value > 0);
            }
        }

        public void SetButton(uint nButton, bool value)
        {
            // Map generic button IDs to InputKey
            switch (nButton)
            {
                case 1: SetButtonState(InputKey.a, value); break;
                case 2: SetButtonState(InputKey.b, value); break;
                case 3: SetButtonState(InputKey.x, value); break; 
                case 4: SetButtonState(InputKey.y, value); break; 
                case 5: SetButtonState(InputKey.start, value); break; 
                case 6: SetButtonState(InputKey.select, value); break;
            }
        }

        private void SetButtonState(InputKey key, bool value)
        {
            if (!_buttonStates.ContainsKey(key))
                _buttonStates[key] = false;

            if (_buttonStates[key] != value)
            {
                _buttonStates[key] = value;
                SendKey(key, value);
            }
        }

        public void SendKeyEvent(Keys key, bool pressed)
        {
            if (key == Keys.None) return;

            ushort scanCode = (ushort)MapVirtualKey((uint)key, 0);

            InterceptionKeyStroke stroke = new InterceptionKeyStroke();
            stroke.code = scanCode;
            stroke.state = pressed ? (ushort)InterceptionKeyState.Down : (ushort)InterceptionKeyState.Up;
            
            if (IsExtendedKey(key))
            {
                stroke.state |= (ushort)InterceptionKeyState.E0;
            }

            // Debug logging if enabled (EN/FR: Logs de débogage si activé)
            if (Options.Instance.KeyboardDebugMode)
            {
                SimpleLogger.Instance.Info($"[KEYBOARD DEBUG] P{_playerIndex} sending {key} ({(pressed ? "DOWN" : "UP")}) to Device ID {_deviceId}, ScanCode: 0x{scanCode:X2}");
            }

            int result = InterceptionDriver.interception_send(_context, _deviceId, ref stroke, 1);
            
            if (result == 0)
            {
                SimpleLogger.Instance.Error($"[KEYBOARD ERROR] P{_playerIndex} failed to send {key} to Device ID {_deviceId}. Result: {result}");
            }
            else if (Options.Instance.KeyboardDebugMode)
            {
                SimpleLogger.Instance.Info($"[KEYBOARD DEBUG] P{_playerIndex} successfully sent {key} to Device ID {_deviceId}, Result: {result}");
            }
        }

        private void SendKey(InputKey key, bool down)
        {
            if (!_keyMapping.ContainsKey(key)) return;
            SendKeyEvent(_keyMapping[key], down);
        }

        private bool IsExtendedKey(Keys key)
        {
            return key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || 
                   key == Keys.Insert || key == Keys.Delete || key == Keys.Home || key == Keys.End || 
                   key == Keys.PageUp || key == Keys.PageDown;
        }

        public void CommitChanges() { }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
