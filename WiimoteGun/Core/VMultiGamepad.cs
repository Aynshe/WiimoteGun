using System;
using WiimoteGun.VMulti;
using WiimoteGun.Core;

namespace WiimoteGun
{
    /// <summary>
    /// EN: VMulti GamePad Client - Wrapper around shared VMultiClient.
    /// FR: Client VMulti GamePad - Wrapper autour du VMultiClient partagé.
    /// </summary>
    public class VMultiGamepad : IVirtualGamepad
    {
        private readonly int _playerIndex;
        private VMultiClient _client;
        private VMultiGamepadReport _currentReport;
        private bool _disposed = false;

        public int PlayerIndex => _playerIndex;
        public bool IsConnected => _client != null && _client.IsConnected;
        public string DevicePath => _client?.DevicePath ?? "";

        public VMultiGamepad(int playerIndex)
        {
            _playerIndex = playerIndex;
            _currentReport = VMultiGamepadReport.Create();
            _client = VMultiClient.GetSharedClient(playerIndex);
            
            SimpleLogger.Instance.Info($"[VMultiGamepad] Using shared VMulti client for Player {playerIndex}");
        }

        public bool Connect()
        {
            return _client?.Connect() ?? false;
        }

        public void Disconnect()
        {
            // We don't disconnect the shared client here as others might be using it.
            // Disposal will handle the reference counting.
        }

        public void SetButton(GamePadButton button, bool pressed)
        {
            _currentReport.SetButton(button, pressed);
        }

        public void SetAxis(GamePadAxis axis, float x, float y)
        {
            _currentReport.SetAxis(axis, x, y);
        }

        public bool SendReport()
        {
            if (_client == null || _disposed) return false;
            return _client.SendReport(_currentReport);
        }

        public bool SendReport(VMultiGamepadReport report)
        {
            _currentReport = report;
            return SendReport();
        }

        public bool UpdateGamepad(
            float leftStickX = 0, float leftStickY = 0,
            float rightStickX = 0, float rightStickY = 0,
            VMultiGamepadButtons1 buttons1 = VMultiGamepadButtons1.None,
            VMultiGamepadButtons2 buttons2 = VMultiGamepadButtons2.None)
        {
            _currentReport.SetAxis(GamePadAxis.LeftStick, leftStickX, leftStickY);
            _currentReport.SetAxis(GamePadAxis.RightStick, rightStickX, rightStickY);
            
            _currentReport.Buttons = (ushort)buttons1;
            _currentReport.Buttons |= (ushort)((byte)(buttons2 & (VMultiGamepadButtons2)0x0F) << 8);

            _currentReport.SetButton(GamePadButton.DPadUp, (buttons2 & VMultiGamepadButtons2.DPadUp) != 0);
            _currentReport.SetButton(GamePadButton.DPadDown, (buttons2 & VMultiGamepadButtons2.DPadDown) != 0);
            _currentReport.SetButton(GamePadButton.DPadLeft, (buttons2 & VMultiGamepadButtons2.DPadLeft) != 0);
            _currentReport.SetButton(GamePadButton.DPadRight, (buttons2 & VMultiGamepadButtons2.DPadRight) != 0);

            return SendReport();
        }

        public byte Throttle
        {
            get => _currentReport.Throttle;
            set => _currentReport.Throttle = value;
        }

        public bool ResetAll()
        {
            _currentReport = VMultiGamepadReport.Create();
            return SendReport();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}
