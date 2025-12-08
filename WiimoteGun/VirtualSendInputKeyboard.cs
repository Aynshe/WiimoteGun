using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual keyboard using keybd_event API (single-player legacy mode)
    /// (EN/FR: Clavier virtuel utilisant keybd_event (mode legacy mono-joueur))
    /// </summary>
    class VirtualSendInputKeyboard : IVirtualJoy
    {
        // Win32 keybd_event API (EN/FR: API Win32 keybd_event)
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        // State tracking for buttons (EN/FR: Suivi d'état des boutons)
        private Dictionary<InputKey, bool> _buttonStates;

        public bool IsEnabled => true;

        public VirtualSendInputKeyboard()
        {
            _buttonStates = new Dictionary<InputKey, bool>();
            SimpleLogger.Instance.Info("VirtualSendInputKeyboard initialized (keybd_event mode - single player)");
        }

        public void SetAxis(bool AxisX, int value)
        {
            // Axis handling not needed for SendInput keyboard mode (EN/FR: Gestion axes non nécessaire)
        }

        public void SetButton(uint nButton, bool value)
        {
            // Generic button handling (EN/FR: Gestion boutons génériques)
        }

        public void SendKeyEvent(Keys key, bool pressed)
        {
            if (key == Keys.None)
                return;

            try
            {
                byte vkCode = (byte)key;
                uint flags = pressed ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;

                // Check if extended key (EN/FR: Vérifier si touche étendue)
                if (IsExtendedKey(key))
                {
                    flags |= KEYEVENTF_EXTENDEDKEY;
                }

                keybd_event(vkCode, 0, flags, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"VirtualSendInputKeyboard SendKeyEvent error: {ex.Message}");
            }
        }

        private bool IsExtendedKey(Keys key)
        {
            // Extended keys (EN/FR: Touches étendues)
            return key == Keys.Right || key == Keys.Left || key == Keys.Up || key == Keys.Down ||
                   key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown ||
                   key == Keys.Insert || key == Keys.Delete;
        }

        public void CommitChanges()
        {
            // No batch commit needed for keybd_event (EN/FR: Pas de commit par lot nécessaire)
        }
    }
}
