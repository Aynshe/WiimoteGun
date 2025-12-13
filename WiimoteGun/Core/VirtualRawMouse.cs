using System;
using VMultiDllWrapper;

namespace WiimoteGun
{
    class VirtualRawMouse : IDisposable
    {
        private VMulti _vmulti;
        private int _playerId;

        // Button constants, assuming they are not in an enum in the wrapper
        private const byte VMULTI_MOUSE_LEFT_BUTTON = 1;
        private const byte VMULTI_MOUSE_RIGHT_BUTTON = 2;
        private const byte VMULTI_MOUSE_MIDDLE_BUTTON = 4;

        public VirtualRawMouse(int playerIndex, string uniqueId)
        {
            _vmulti = new VMulti();
            _playerId = playerIndex;
            string deviceName = $"WiimoteGun_{uniqueId}_P{playerIndex}";
            SimpleLogger.Instance.Info($"Initializing virtual mouse: {deviceName}");

            _vmulti.connect(_playerId);
            SimpleLogger.Instance.Info($"Successfully connected vmulti for player {_playerId}.");
        }

        public void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton)
        {
            MouseButton buttons = 0;
            if (leftButton) buttons |= MouseButton.LeftButton;
            if (rightButton) buttons |= MouseButton.RightButton;
            if (middleButton) buttons |= MouseButton.MiddleButton;

            ushort scaledX = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, x));
            ushort scaledY = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, y));

            MouseReport report = new MouseReport();
            report.SetButtons(buttons);
            report.MouseX = scaledX;
            report.MouseY = scaledY;
            report.WheelPosition = 0;

            _vmulti.updateMouse(report);
        }

        public void Dispose()
        {
            if (_vmulti != null)
            {
                SimpleLogger.Instance.Info($"Disconnecting vmulti for player {_playerId}.");
                _vmulti.disconnect();
                _vmulti = null;
            }
        }
    }
}
