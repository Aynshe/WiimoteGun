using System;
using System.Runtime.InteropServices;

namespace WiimoteGun
{
    public static class VMultiDll
    {
        [DllImport("VMultiDllWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool vmulti_connect(uint id);

        [DllImport("VMultiDllWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void vmulti_disconnect(uint id);
        
        [DllImport("VMultiDllWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool vmulti_update_mouse(uint id, byte b, ushort x, ushort y, byte wheel);

        [StructLayout(LayoutKind.Sequential)]
        public struct VMultiReport
        {
            public byte ReportID;
            public byte ReportLength;
            public byte Buttons;
            public ushort X;
            public ushort Y;
            public byte Wheel;
        }

        public const byte VMULTI_MOUSE_LEFT_BUTTON = 1;
        public const byte VMULTI_MOUSE_RIGHT_BUTTON = 2;
        public const byte VMULTI_MOUSE_MIDDLE_BUTTON = 4;
    }
}
