using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WiimoteGun.Common;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: ViGEm Gamepad wrapper (XInput / Xbox 360).
    /// FR: Wrapper pour manette ViGEm (XInput / Xbox 360).
    /// </summary>
    public class ViGEmGamepad : IVirtualGamepad
    {
        private readonly int _playerIndex;
        private IXbox360Controller _controller;
        private bool _isConnected;
        private byte _throttle;

        public int PlayerIndex => _playerIndex;
        public bool IsConnected => _isConnected;

        public ViGEmGamepad(int playerIndex)
        {
            _playerIndex = playerIndex;
            var client = ViGEmClientManager.Instance.Client;
            if (client == null)
            {
                SimpleLogger.Instance.Error($"[ViGEmGamepad] P{playerIndex}: ViGEm client is not available.");
                return;
            }

            if (ViGEmClientManager.Instance.IsAvailable)
            {
                _controller = ViGEmClientManager.Instance.Client.CreateXbox360Controller();
                _controller.FeedbackReceived += (s, e) => { /* Rumble not implemented yet */ };
                _controller.AutoSubmitReport = false;
            }
            else
            {
                SimpleLogger.Instance.Error($"[ViGEmGamepad] P{_playerIndex} cannot be created: ViGEmBus driver not found.");
            }
        }

        public bool Connect()
        {
            if (_controller == null) return false;
            if (_isConnected) return true;

            try
            {
                _controller.Connect();
                _isConnected = true;
                SimpleLogger.Instance.Info($"[ViGEmGamepad] P{_playerIndex}: Xbox 360 Controller connected via ViGEmBus.");
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ViGEmGamepad] P{_playerIndex}: Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_controller != null && _isConnected)
            {
                try
                {
                    _controller.Disconnect();
                    SimpleLogger.Instance.Info($"[ViGEmGamepad] P{_playerIndex}: Xbox 360 Controller disconnected.");
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[ViGEmGamepad] P{_playerIndex}: Disconnect error: {ex.Message}");
                }
            }
            _isConnected = false;
        }

        public void SetButton(GamePadButton button, bool pressed)
        {
            if (_controller == null) return;

            // Mapping GamePadButton (WiimoteGun) to Xbox360Buttons (ViGEm)
            switch (button)
            {
                case GamePadButton.Button1: _controller.SetButtonState(Xbox360Button.A, pressed); break;
                case GamePadButton.Button2: _controller.SetButtonState(Xbox360Button.B, pressed); break;
                case GamePadButton.Button3: _controller.SetButtonState(Xbox360Button.X, pressed); break;
                case GamePadButton.Button4: _controller.SetButtonState(Xbox360Button.Y, pressed); break;
                case GamePadButton.Button5: _controller.SetButtonState(Xbox360Button.LeftShoulder, pressed); break;
                case GamePadButton.Button6: _controller.SetButtonState(Xbox360Button.RightShoulder, pressed); break;
                case GamePadButton.Button7: _controller.SetSliderValue(Xbox360Slider.LeftTrigger, pressed ? (byte)255 : (byte)0); break;
                case GamePadButton.Button8: _controller.SetSliderValue(Xbox360Slider.RightTrigger, pressed ? (byte)255 : (byte)0); break;
                case GamePadButton.Button9: _controller.SetButtonState(Xbox360Button.Back, pressed); break;
                case GamePadButton.Button10: _controller.SetButtonState(Xbox360Button.Start, pressed); break;
                case GamePadButton.Button11: _controller.SetButtonState(Xbox360Button.LeftThumb, pressed); break;
                case GamePadButton.Button12: _controller.SetButtonState(Xbox360Button.RightThumb, pressed); break;
                case GamePadButton.DPadUp: _controller.SetButtonState(Xbox360Button.Up, pressed); break;
                case GamePadButton.DPadDown: _controller.SetButtonState(Xbox360Button.Down, pressed); break;
                case GamePadButton.DPadLeft: _controller.SetButtonState(Xbox360Button.Left, pressed); break;
                case GamePadButton.DPadRight: _controller.SetButtonState(Xbox360Button.Right, pressed); break;
            }
        }

        public void SetAxis(GamePadAxis axis, float x, float y)
        {
            if (_controller == null) return;

            // ViGEm expects short (-32768 to 32767) where Up is positive
            // Internal convention is Negative = Up (to match VMulti/IR), so we invert Y here
            short shortX = (short)(x * 32767);
            short shortY = (short)(-y * 32767);

            // Note: Xbox Y axis is often inverted compared to our internal representation
            // so we handle it here to keep the rest of the logic consistent.
            if (axis == GamePadAxis.LeftStick)
            {
                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, shortX);
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, shortY);
            }
            else if (axis == GamePadAxis.RightStick)
            {
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, shortX);
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, shortY);
            }
        }

        public byte Throttle
        {
            get => _throttle;
            set
            {
                _throttle = value;
                if (_controller != null)
                {
                    // Xbox 360 Triggers are 0..255. Throttle is also 0..255.
                    // Let's map Throttle to Right Trigger for consistency if used for acceleration.
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, _throttle);
                }
            }
        }

        public bool SendReport()
        {
            if (_controller == null || !_isConnected) return false;
            _controller.SubmitReport();
            return true;
        }

        public bool ResetAll()
        {
            if (_controller == null) return false;
            // ViGEm doesn't have a direct "Reset" in IXbox360Controller, manually reset axes and buttons
            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            _controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
            _controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
            
            // Iterate over buttons if possible, or just clear mask if we had access to report
            // For now, ResetAll will just send a neutral report
            SendReport();
            return true;
        }

        public void Dispose()
        {
            Disconnect();
            _controller = null;
        }
    }
}
