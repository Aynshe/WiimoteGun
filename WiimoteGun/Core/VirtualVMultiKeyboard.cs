using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WiimoteGun.VMulti;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual keyboard using VMulti driver - Direct HID communication without Interception
    /// (FR: Clavier virtuel utilisant le pilote VMulti - Communication HID directe sans Interception)
    /// </summary>
    class VirtualVMultiKeyboard : IVirtualJoy
    {
        private VMultiClient _client;
        private int _playerIndex;
        private bool _disposed = false;

        // Mapping Wiimote buttons to Keys (EN/FR: Mapping des boutons Wiimote vers les touches)
        private Dictionary<InputKey, Keys> _keyMapping;
        
        // Track currently pressed keys (EN/FR: Suivi des touches actuellement pressées)
        private HashSet<byte> _pressedKeys = new HashSet<byte>();
        private VMultiKeyboardModifier _currentModifiers = VMultiKeyboardModifier.None;

        // Public property for accessing keyboard client state (EN/FR: Propriété publique pour état client)
        public bool IsConnected => _client != null && _client.IsConnected;

        public VirtualVMultiKeyboard(int playerIndex)
        {
            _playerIndex = playerIndex;

            SimpleLogger.Instance.Info($"[VMultiKeyboard] Creating VMulti keyboard for Player {playerIndex}");

            try
            {
                _client = VMultiClient.GetSharedClient(playerIndex);

                if (_client.Connect())
                {
                    SimpleLogger.Instance.Info($"[VMultiKeyboard] Connected successfully for P{playerIndex}");
                }
                else
                {
                    SimpleLogger.Instance.Warning($"[VMultiKeyboard] Could not connect initially for P{playerIndex}. Will retry on first use.");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiKeyboard] Failed to create VMulti client for P{playerIndex}: {ex.Message}");
            }

            InitializeMapping();
        }

        /// <summary>
        /// Initialize default key mapping (EN/FR: Initialiser le mapping par défaut)
        /// </summary>
        private void InitializeMapping()
        {
            _keyMapping = new Dictionary<InputKey, Keys>()
            {
                { InputKey.up, Keys.Up },
                { InputKey.down, Keys.Down },
                { InputKey.left, Keys.Left },
                { InputKey.right, Keys.Right },
                { InputKey.a, Keys.Enter },
                { InputKey.b, Keys.Space },
                { InputKey.x, Keys.Back },
                { InputKey.y, Keys.Tab },
                { InputKey.start, Keys.Escape },
                { InputKey.select, Keys.D1 }
            };
        }

        public bool IsEnabled => _client != null && !_disposed;

        public void SetAxis(bool axisX, int value)
        {
            if (axisX)
            {
                SetButtonState(InputKey.left, value < 0);
                SetButtonState(InputKey.right, value > 0);
            }
            else
            {
                SetButtonState(InputKey.up, value < 0);
                SetButtonState(InputKey.down, value > 0);
            }
        }

        public void SetButton(uint nButton, bool value)
        {
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
            if (!_keyMapping.ContainsKey(key))
                return;

            Keys winKey = _keyMapping[key];
            SendKeyEvent(winKey, value);
        }

        /// <summary>
        /// Send a key event (press or release)
        /// (EN/FR: Envoyer un événement de touche)
        /// </summary>
        public void SendKeyEvent(Keys key, bool pressed)
        {
            if (key == Keys.None || _client == null || _disposed)
                return;

            // EN/FR: Extraire les modificateurs de la touche (Extract modifiers from key)
            bool hasShift = (key & Keys.Shift) == Keys.Shift;
            bool hasCtrl = (key & Keys.Control) == Keys.Control;
            bool hasAlt = (key & Keys.Alt) == Keys.Alt;
            Keys pureKey = key & Keys.KeyCode;

            // Update modifiers (EN/FR: Mettre à jour les modificateurs)
            bool isModifier = UpdateModifiers(pureKey, pressed);

            byte hidKeyCode = ConvertKeysToHidKeyCode(pureKey);
            
            // Allow processing if it's a modifier OR has a valid HID code
            // (EN/FR: Autoriser si c'est un modificateur OU s'il y a un code HID valide)
            if (hidKeyCode == 0 && !isModifier)
                return;

            // Update pressed keys set (EN/FR: Mettre à jour l'ensemble des touches pressées)
            if (hidKeyCode > 0)
            {
                if (pressed)
                {
                    _pressedKeys.Add(hidKeyCode);
                }
                else
                {
                    _pressedKeys.Remove(hidKeyCode);
                }
            }

            // EN/FR: Combiner les modificateurs globaux et ceux de la touche (Combine global modifiers and key modifiers)
            VMultiKeyboardModifier extraMods = VMultiKeyboardModifier.None;
            if (hasShift) extraMods |= VMultiKeyboardModifier.LeftShift;
            if (hasCtrl) extraMods |= VMultiKeyboardModifier.LeftControl;
            if (hasAlt) extraMods |= VMultiKeyboardModifier.LeftAlt;

            VMultiKeyboardModifier finalModifiers = _currentModifiers;
            if (pressed)
            {
                finalModifiers |= extraMods;
            }

            // Send keyboard report with all pressed keys (EN/FR: Envoyer le rapport clavier avec toutes les touches pressées)
            SendKeyboardReport(finalModifiers);

            // Debug logging if enabled (EN/FR: Logs de débogage si activé)
            if (Options.Instance.KeyboardDebugMode)
            {
                SimpleLogger.Instance.Info($"[VMultiKeyboard] P{_playerIndex} key {key} ({(pressed ? "DOWN" : "UP")}), HID: 0x{hidKeyCode:X2}");
            }
        }

        /// <summary>
        /// Update modifier flags based on key (EN/FR: Mettre à jour les flags modificateurs)
        /// Returns true if the key was a modifier.
        /// </summary>
        private bool UpdateModifiers(Keys key, bool pressed)
        {
            VMultiKeyboardModifier mod = VMultiKeyboardModifier.None;
            bool isModifier = true;

            switch (key)
            {
                case Keys.LShiftKey:
                case Keys.ShiftKey:
                    mod = VMultiKeyboardModifier.LeftShift;
                    break;
                case Keys.RShiftKey:
                    mod = VMultiKeyboardModifier.RightShift;
                    break;
                case Keys.LControlKey:
                case Keys.ControlKey:
                    mod = VMultiKeyboardModifier.LeftControl;
                    break;
                case Keys.RControlKey:
                    mod = VMultiKeyboardModifier.RightControl;
                    break;
                case Keys.LMenu:
                case Keys.Menu:
                case Keys.Alt:
                    mod = VMultiKeyboardModifier.LeftAlt;
                    break;
                case Keys.RMenu:
                    mod = VMultiKeyboardModifier.RightAlt;
                    break;
                case Keys.LWin:
                    mod = VMultiKeyboardModifier.LeftGui;
                    break;
                case Keys.RWin:
                    mod = VMultiKeyboardModifier.RightGui;
                    break;
                default:
                    isModifier = false;
                    break;
            }

            if (mod != VMultiKeyboardModifier.None)
            {
                if (pressed)
                    _currentModifiers |= mod;
                else
                    _currentModifiers &= ~mod;
            }

            return isModifier;
        }

        /// <summary>
        /// Send the current keyboard state to VMulti
        /// (EN/FR: Envoyer l'état actuel du clavier à VMulti)
        /// </summary>
        private void SendKeyboardReport(VMultiKeyboardModifier modifiers)
        {
            if (_client == null)
                return;

            // Convert pressed keys to array (max 6) (EN/FR: Convertir les touches pressées en tableau)
            byte[] keyCodes = new byte[6];
            int index = 0;

            foreach (byte keyCode in _pressedKeys)
            {
                if (index >= 6)
                    break;
                keyCodes[index++] = keyCode;
            }

            _client.UpdateKeyboard(modifiers, keyCodes);
        }

        /// <summary>
        /// Convert Windows.Forms.Keys to HID keyboard scan code
        /// (EN/FR: Convertir Windows.Forms.Keys vers scan code HID clavier)
        /// </summary>
        private byte ConvertKeysToHidKeyCode(Keys key)
        {
            // USB HID keyboard scan codes (EN/FR: Codes de scan clavier USB HID)
            // Reference: USB HID Usage Tables, Section 10 (Keyboard/Keypad Page 0x07)
            switch (key & Keys.KeyCode)
            {
                // Letters (EN/FR: Lettres)
                case Keys.A: return 0x04;
                case Keys.B: return 0x05;
                case Keys.C: return 0x06;
                case Keys.D: return 0x07;
                case Keys.E: return 0x08;
                case Keys.F: return 0x09;
                case Keys.G: return 0x0A;
                case Keys.H: return 0x0B;
                case Keys.I: return 0x0C;
                case Keys.J: return 0x0D;
                case Keys.K: return 0x0E;
                case Keys.L: return 0x0F;
                case Keys.M: return 0x10;
                case Keys.N: return 0x11;
                case Keys.O: return 0x12;
                case Keys.P: return 0x13;
                case Keys.Q: return 0x14;
                case Keys.R: return 0x15;
                case Keys.S: return 0x16;
                case Keys.T: return 0x17;
                case Keys.U: return 0x18;
                case Keys.V: return 0x19;
                case Keys.W: return 0x1A;
                case Keys.X: return 0x1B;
                case Keys.Y: return 0x1C;
                case Keys.Z: return 0x1D;

                // Numbers (EN/FR: Chiffres)
                case Keys.D1: return 0x1E;
                case Keys.D2: return 0x1F;
                case Keys.D3: return 0x20;
                case Keys.D4: return 0x21;
                case Keys.D5: return 0x22;
                case Keys.D6: return 0x23;
                case Keys.D7: return 0x24;
                case Keys.D8: return 0x25;
                case Keys.D9: return 0x26;
                case Keys.D0: return 0x27;

                // Special keys (EN/FR: Touches spéciales)
                case Keys.Enter: return 0x28;
                case Keys.Escape: return 0x29;
                case Keys.Back: return 0x2A;
                case Keys.Tab: return 0x2B;
                case Keys.Space: return 0x2C;

                // Punctuation (EN/FR: Ponctuation)
                case Keys.OemMinus: return 0x2D;
                case Keys.Oemplus: return 0x2E;
                case Keys.OemOpenBrackets: return 0x2F;
                case Keys.OemCloseBrackets: return 0x30;
                case Keys.OemPipe: return 0x31; // Oem5 = OemPipe (AZERTY * µ)
                case Keys.OemSemicolon: return 0x33; // Oem1 = OemSemicolon (AZERTY ù %)
                case Keys.OemQuotes: return 0x34; // Oem7 = OemQuotes
                case Keys.Oemtilde: return 0x35;
                case Keys.Oemcomma: return 0x36;
                case Keys.OemPeriod: return 0x37;
                case Keys.OemQuestion: return 0x38; // Oem2 = OemQuestion (AZERTY : /)
                case Keys.Oem102: return 0x64; // Non-US backslash key (AZERTY < >)


                // Function keys (EN/FR: Touches de fonction)
                case Keys.CapsLock: return 0x39;
                case Keys.F1: return 0x3A;
                case Keys.F2: return 0x3B;
                case Keys.F3: return 0x3C;
                case Keys.F4: return 0x3D;
                case Keys.F5: return 0x3E;
                case Keys.F6: return 0x3F;
                case Keys.F7: return 0x40;
                case Keys.F8: return 0x41;
                case Keys.F9: return 0x42;
                case Keys.F10: return 0x43;
                case Keys.F11: return 0x44;
                case Keys.F12: return 0x45;

                // Navigation (EN/FR: Navigation)
                case Keys.PrintScreen: return 0x46;
                case Keys.Scroll: return 0x47;
                case Keys.Pause: return 0x48;
                case Keys.Insert: return 0x49;
                case Keys.Home: return 0x4A;
                case Keys.PageUp: return 0x4B;
                case Keys.Delete: return 0x4C;
                case Keys.End: return 0x4D;
                case Keys.PageDown: return 0x4E;
                case Keys.Right: return 0x4F;
                case Keys.Left: return 0x50;
                case Keys.Down: return 0x51;
                case Keys.Up: return 0x52;

                // Numpad (EN/FR: Pavé numérique)
                case Keys.NumLock: return 0x53;
                case Keys.Divide: return 0x54;
                case Keys.Multiply: return 0x55;
                case Keys.Subtract: return 0x56;
                case Keys.Add: return 0x57;
                case Keys.NumPad1: return 0x59;
                case Keys.NumPad2: return 0x5A;
                case Keys.NumPad3: return 0x5B;
                case Keys.NumPad4: return 0x5C;
                case Keys.NumPad5: return 0x5D;
                case Keys.NumPad6: return 0x5E;
                case Keys.NumPad7: return 0x5F;
                case Keys.NumPad8: return 0x60;
                case Keys.NumPad9: return 0x61;
                case Keys.NumPad0: return 0x62;
                case Keys.Decimal: return 0x63;

                default:
                    return 0;
            }
        }

        public void CommitChanges()
        {
            // No-op for VMulti - reports are sent immediately
            // (EN/FR: Rien à faire pour VMulti - les rapports sont envoyés immédiatement)
        }

        /// <summary>
        /// Refresh device connection (EN/FR: Rafraîchir la connexion au périphérique)
        /// </summary>
        public void RefreshDevice()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client.Connect();
            }
        }

        /// <summary>
        /// EN: Release all pressed keys and reset modifiers immediately.
        /// FR: Relâcher toutes les touches pressées et réinitialiser les modificateurs immédiatement.
        /// </summary>
        public void ResetAll()
        {
            if (_client != null && _client.IsConnected)
            {
                _pressedKeys.Clear();
                _currentModifiers = VMultiKeyboardModifier.None;
                SendKeyboardReport(VMultiKeyboardModifier.None);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            SimpleLogger.Instance.Info($"[VMultiKeyboard] Disconnecting VMulti keyboard for player {_playerIndex}.");

            // Release all keys before disconnecting (EN/FR: Relâcher toutes les touches avant déconnexion)
            ResetAll();

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}
