using System;
using System.Runtime.InteropServices;
using WiimoteGun.Common.Win32;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual mouse implementation using mouse_event API (single-player legacy mode)
    /// (EN/FR: Implémentation souris virtuelle utilisant mouse_event (mode legacy mono-joueur))
    /// </summary>
    internal class VirtualSendInputMouse : IVirtualMouse
    {
        // Win32 mouse_event and SendInput APIs (EN/FR: APIs Win32 mouse_event et SendInput)
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        // State tracking (EN/FR: Suivi d'état)
        private bool _lastLeftButton = false;
        private bool _lastRightButton = false;
        private bool _lastMiddleButton = false;
        private int _lastX = -1;
        private int _lastY = -1;

        // Event for rumble integration (EN/FR: Événement pour intégration vibration)
        public event Action<bool> OnLeftMouseButtonChanged;

        public VirtualSendInputMouse()
        {
            SimpleLogger.Instance.Info("VirtualSendInputMouse initialized (mouse_event mode - single player)");
        }

        public void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton, bool moveCursor = true)
        {
            try
            {
                // Atomic SendInput construction (EN/FR: Construction d'un SendInput atomique)
                // Combine movement and button flags in a single operation
                // (EN/FR : Combiner mouvement et boutons en une seule opération)
                uint sendInputFlags = 0;
                bool needsUpdate = false;

                if (moveCursor)
                {
                    sendInputFlags |= MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
                    needsUpdate = true;
                }

                if (leftButton != _lastLeftButton)
                {
                    sendInputFlags |= leftButton ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
                    _lastLeftButton = leftButton;
                    needsUpdate = true;
                    OnLeftMouseButtonChanged?.Invoke(leftButton);
                }

                if (rightButton != _lastRightButton)
                {
                    sendInputFlags |= rightButton ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
                    _lastRightButton = rightButton;
                    needsUpdate = true;
                }

                if (middleButton != _lastMiddleButton)
                {
                    sendInputFlags |= middleButton ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
                    _lastMiddleButton = middleButton;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    var screen = System.Windows.Forms.Screen.AllScreens[Options.Instance.MonitorId];
                    int screenPixelX = (int)((x / 65535.0) * screen.Bounds.Width) + screen.Bounds.Left;
                    int screenPixelY = (int)((y / 65535.0) * screen.Bounds.Height) + screen.Bounds.Top;

                    // 1. SetCursorPos & mouse_event remains for legacy/visual sync (TeknoParrot)
                    if (moveCursor)
                    {
                        SetCursorPos(screenPixelX, screenPixelY);
                        mouse_event(MOUSEEVENTF_ABSOLUTE, screenPixelX, screenPixelY, 0, UIntPtr.Zero);
                    }

                    // 2. Modern Atomic SendInput (MOVE | ABSOLUTE | BUTTONS)
                    int totalWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                    int totalHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                    int virtualLeft = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                    int virtualTop = System.Windows.Forms.SystemInformation.VirtualScreen.Top;

                    int normalizedX = (int)(((screenPixelX - virtualLeft) * 65535.0) / totalWidth);
                    int normalizedY = (int)(((screenPixelY - virtualTop) * 65535.0) / totalHeight);
                    normalizedX = Math.Max(0, Math.Min(65535, normalizedX));
                    normalizedY = Math.Max(0, Math.Min(65535, normalizedY));

                    INPUT[] inputs = new INPUT[1];
                    inputs[0].type = INPUT_MOUSE;
                    inputs[0].mi.dx = normalizedX;
                    inputs[0].mi.dy = normalizedY;
                    inputs[0].mi.mouseData = 0;
                    inputs[0].mi.dwFlags = sendInputFlags;
                    inputs[0].mi.time = 0;
                    inputs[0].mi.dwExtraInfo = IntPtr.Zero;

                    SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

                    if (moveCursor)
                    {
                        _lastX = screenPixelX;
                        _lastY = screenPixelY;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"VirtualSendInputMouse error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            SimpleLogger.Instance.Info("VirtualSendInputMouse disposed");
        }
    }
}
