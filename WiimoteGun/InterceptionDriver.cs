using System;
using System.Runtime.InteropServices;

namespace WiimoteGun.Interception
{
    // Interception API wrapper
    // Based on https://github.com/oblitum/Interception
    
    public enum InterceptionMouseState : ushort
    {
        LeftButtonDown = 0x001,
        LeftButtonUp = 0x002,
        RightButtonDown = 0x004,
        RightButtonUp = 0x008,
        MiddleButtonDown = 0x010,
        MiddleButtonUp = 0x020,
        Button4Down = 0x040,
        Button4Up = 0x080,
        Button5Down = 0x100,
        Button5Up = 0x200,
        MouseWheel = 0x400,
        MouseHWheel = 0x800,
        Move = 0x000
    }

    public enum InterceptionMouseFlag : ushort
    {
        MoveRelative = 0x000,
        MoveAbsolute = 0x001,
        VirtualDesktop = 0x002,
        AttributesChanged = 0x004
    }

    public enum InterceptionKeyState : ushort
    {
        Down = 0x00,
        Up = 0x01,
        E0 = 0x02,
        E1 = 0x04,
        Term = 0x08,
        All = 0xFFFF
    }

    public enum InterceptionFilterKeyState : ushort
    {
        None = 0x0000,
        All = 0xFFFF,
        KeyDown = 0x0001,
        KeyUp = 0x0002,
        KeyE0 = 0x0004,
        KeyE1 = 0x0008,
        KeyTerm = 0x0010,
        KeyAll = 0xFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InterceptionMouseStroke
    {
        public InterceptionMouseState state;
        public InterceptionMouseFlag flags;
        public short rolling;
        public int x;
        public int y;
        public uint information;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InterceptionKeyStroke
    {
        public ushort code;
        public ushort state;
        public uint information;
    }

    public static class InterceptionDriver
    {
        private const string DllName = "interception.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr interception_create_context();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void interception_destroy_context(IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int interception_send(IntPtr context, int device, ref InterceptionMouseStroke stroke, uint nstroke);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int interception_send(IntPtr context, int device, ref InterceptionKeyStroke stroke, uint nstroke);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void interception_set_filter(IntPtr context, Predicate predicate, ushort filter);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int interception_is_keyboard(int device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int interception_is_mouse(int device);

        public delegate int Predicate(int device);

        public const int INTERCEPTION_MOUSE_BUTTON_DOWN = 1;
        public const int INTERCEPTION_MOUSE_BUTTON_UP = 2;
        
        public const int INTERCEPTION_MAX_MOUSE = 10;
        public const int INTERCEPTION_MAX_KEYBOARD = 10;
    }
}
