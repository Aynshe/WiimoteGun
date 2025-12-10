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
                // Move cursor if requested (EN/FR: Déplacer curseur si demandé)
                if (moveCursor)
                {
                    // HYBRID APPROACH FOR MAXIMUM COMPATIBILITY (EN/FR: Approche hybride pour compatibilité maximale)
                    // Combines legacy TeknoParrot support with modern FPS game support
                    
                    var screen = System.Windows.Forms.Screen.AllScreens[Options.Instance.MonitorId];
                    
                    // Calculate pixel position on the specific monitor
                    int screenPixelX = (int)((x / 65535.0) * screen.Bounds.Width) + screen.Bounds.Left;
                    int screenPixelY = (int)((y / 65535.0) * screen.Bounds.Height) + screen.Bounds.Top;
                    
                    // 1. SetCursorPos - For games that read GetCursorPos() (EN/FR: Pour jeux lisant GetCursorPos)
                    SetCursorPos(screenPixelX, screenPixelY);

                    // 2. Legacy mouse_event (ABSOLUTE + raw pixels) - For TeknoParrot
                    // (EN/FR: mouse_event legacy pour TeknoParrot)
                    mouse_event(MOUSEEVENTF_ABSOLUTE, screenPixelX, screenPixelY, 0, UIntPtr.Zero);
                    
                    // 3. Modern SendInput (MOVE | ABSOLUTE + normalized coords) - For FPS games with DirectInput/RawInput
                    // (EN/FR: SendInput moderne pour jeux FPS avec DirectInput/RawInput)
                    // Calculate normalized coordinates for all screens
                    int totalWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                    int totalHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                    int virtualLeft = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                    int virtualTop = System.Windows.Forms.SystemInformation.VirtualScreen.Top;
                    
                    int normalizedX = (int)(((screenPixelX - virtualLeft) * 65535.0) / totalWidth);
                    int normalizedY = (int)(((screenPixelY - virtualTop) * 65535.0) / totalHeight);
                    normalizedX = Math.Max(0, Math.Min(65535, normalizedX));
                    normalizedY = Math.Max(0, Math.Min(65535, normalizedY));
                    
                    // Send via SendInput for modern games
                    INPUT[] inputs = new INPUT[1];
                    inputs[0].type = INPUT_MOUSE;
                    inputs[0].mi.dx = normalizedX;
                    inputs[0].mi.dy = normalizedY;
                    inputs[0].mi.mouseData = 0;
                    inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
                    inputs[0].mi.time = 0;
                    inputs[0].mi.dwExtraInfo = IntPtr.Zero;
                    
                    SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
                    
                    _lastX = screenPixelX;
                    _lastY = screenPixelY;
                }

                // Handle button state changes (EN/FR: Gérer changements d'état des boutons)
                if (leftButton != _lastLeftButton)
                {
                    mouse_event(leftButton ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                    // Trigger rumble event (EN/FR: Déclencher événement vibration)
                    OnLeftMouseButtonChanged?.Invoke(leftButton);
                    _lastLeftButton = leftButton;
                }

                // Handle right button state changes (EN/FR: Gérer changements bouton droit)
                if (rightButton != _lastRightButton)
                {
                    mouse_event(rightButton ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    _lastRightButton = rightButton;
                }

                // Handle middle button state changes (EN/FR: Gérer changements bouton milieu)
                if (middleButton != _lastMiddleButton)
                {
                    mouse_event(middleButton ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    _lastMiddleButton = middleButton;
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
