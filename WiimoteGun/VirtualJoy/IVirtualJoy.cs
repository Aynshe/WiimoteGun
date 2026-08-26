using System;
using System.Windows.Forms;

namespace WiimoteGun
{
    public interface IVirtualJoy : IDisposable
    {
        bool IsEnabled { get; }

        void SetAxis(bool AxisX, int value);
        void SetButton(uint nButton, bool value);
        void SendKeyEvent(Keys key, bool pressed);
        void CommitChanges();
        void ResetAll();
    }
}