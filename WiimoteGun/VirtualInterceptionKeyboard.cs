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

        // Mapping Wiimote buttons to Keys
        private Dictionary<InputKey, Keys> _keyMapping;
        private Dictionary<InputKey, bool> _buttonStates;

        public VirtualInterceptionKeyboard(int playerIndex)
        {
            _playerIndex = playerIndex;
            
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

            // Check if user forced a specific device ID (EN/FR: Vérifier si un Device ID est forcé)
            int forcedDeviceId = GetForcedDeviceId(playerIndex);
            
            if (forcedDeviceId > 0)
            {
                _deviceId = forcedDeviceId;
                SimpleLogger.Instance.Info($"[KEYBOARD CONFIG] FORCED Keyboard Device ID {_deviceId} for Player {_playerIndex} (from settings.cfg)");
            }
            // Assign device ID based on player index and available keyboards (EN/FR: Assigner l'ID selon le joueur et les claviers disponibles)
            else if (_availableKeyboards.Count > 0)
            {
                // Check if shared keyboard mode is enabled (EN/FR: Vérifier si le mode clavier partagé est activé)
                if (Options.Instance.UseSharedKeyboard)
                {
                    // All players share the same keyboard (Device ID 1) (EN/FR: Tous les joueurs partagent le même clavier)
                    _deviceId = _availableKeyboards[0];
                    SimpleLogger.Instance.Info($"Assigned SHARED Keyboard Device ID {_deviceId} to Player {_playerIndex}");
                }
                else
                {
                    // Try to give each player a unique keyboard if available (EN/FR: Essayer de donner un clavier unique à chaque joueur)
                    int kbIndex = (playerIndex - 1) % _availableKeyboards.Count;
                    _deviceId = _availableKeyboards[kbIndex];
                    SimpleLogger.Instance.Info($"Assigned UNIQUE Keyboard Device ID {_deviceId} to Player {_playerIndex}");
                }
            }
            else
            {
                // Fallback to ID 1 if no keyboards detected (should not happen if driver works)
                _deviceId = 1;
                SimpleLogger.Instance.Warning($"No keyboards detected via Interception. Defaulting Player {_playerIndex} to Device ID 1.");
            }

            InitializeMapping();
            _buttonStates = new Dictionary<InputKey, bool>();
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
            int maxKeyboards = 2; // Limit to first 2 working keyboards for cyclic rotation (EN/FR: Limiter aux 2 premiers claviers fonctionnels)
            
            for (int i = 1; i <= InterceptionDriver.INTERCEPTION_MAX_KEYBOARD && _availableKeyboards.Count < maxKeyboards; i++)
            {
                if (InterceptionDriver.interception_is_keyboard(i) != 0)
                {
                    _availableKeyboards.Add(i);
                    SimpleLogger.Instance.Info($"Detected Keyboard Device ID: {i}");
                }
            }
            
            SimpleLogger.Instance.Info($"Using {_availableKeyboards.Count} keyboard(s) for up to 4 players (cyclic rotation)");
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
