using System;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: Common interface for virtual gamepads (DInput/XInput).
    /// FR: Interface commune pour les manettes virtuelles (DInput/XInput).
    /// </summary>
    public interface IVirtualGamepad : IDisposable
    {
        int PlayerIndex { get; }
        bool IsConnected { get; }
        
        bool Connect();
        void Disconnect();
        
        void SetButton(GamePadButton button, bool pressed);
        void SetAxis(GamePadAxis axis, float x, float y);
        byte Throttle { get; set; }
        
        bool SendReport();
        bool ResetAll();
    }
}
