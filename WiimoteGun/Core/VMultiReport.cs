using System;
using System.Runtime.InteropServices;

namespace WiimoteGun.VMulti
{
    /// <summary>
    /// VMulti HID Report structures based on vmulticommon.h
    /// (EN/FR: Structures de rapports HID VMulti basées sur vmulticommon.h)
    /// </summary>

    // Report IDs (EN/FR: Identifiants de rapports)
    public static class VMultiReportIds
    {
        public const byte MultiTouch = 0x01;
        public const byte Feature = 0x02;
        public const byte Mouse = 0x03;
        public const byte RelativeMouse = 0x04;
        public const byte Digitizer = 0x05;
        public const byte Joystick = 0x06;
        public const byte Keyboard = 0x07;
        public const byte Message = 0x10;
        public const byte Control = 0x40;
    }

    // VMulti device identification (EN/FR: Identification des périphériques VMulti)
    public static class VMultiDeviceIds
    {
        // Default VMulti VID/PID
        public const ushort DefaultVid = 0x00FF;
        public const ushort DefaultPid = 0xBACC;
        
        // Custom VIDs for multi-player support (EN/FR: VID personnalisés pour support multi-joueurs)
        // These correspond to vmultia, vmultib, vmultic, vmultid drivers
        public static readonly ushort[] PlayerVids = { 0x001F, 0x002F, 0x003F, 0x004F };
        
        // Usage pages for HID device detection (EN/FR: Pages d'usage pour détection HID)
        public const ushort ControlUsagePage = 0xFF00;
        public const ushort ControlUsage = 0x0001;
        public const ushort MessageUsagePage = 0xFF00;
        public const ushort MessageUsage = 0x0002;
    }

    // Control report header (EN/FR: En-tête de rapport de contrôle)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiControlReportHeader
    {
        public byte ReportID;      // REPORTID_CONTROL (0x40)
        public byte ReportLength;  // Length of the embedded report
    }

    // Mouse button flags (EN/FR: Flags des boutons de souris)
    [Flags]
    public enum VMultiMouseButton : byte
    {
        None = 0x00,
        Left = 0x01,
        Right = 0x02,
        Middle = 0x04
    }

    // Absolute mouse report (EN/FR: Rapport de souris absolue)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiMouseReport
    {
        public byte ReportID;       // REPORTID_MOUSE (0x03)
        public byte Button;         // Button flags
        public ushort XValue;       // Absolute X (0-32767)
        public ushort YValue;       // Absolute Y (0-32767)
        public byte WheelPosition;  // Wheel position (-127 to 127)

        public const ushort MinCoordinate = 0x0000;
        public const ushort MaxCoordinate = 0x7FFF;

        public static VMultiMouseReport Create()
        {
            return new VMultiMouseReport
            {
                ReportID = VMultiReportIds.Mouse,
                Button = 0,
                XValue = 0,
                YValue = 0,
                WheelPosition = 0
            };
        }
    }

    // Relative mouse report (EN/FR: Rapport de souris relative)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiRelativeMouseReport
    {
        public byte ReportID;       // REPORTID_RELATIVE_MOUSE (0x04)
        public byte Button;         // Button flags
        public sbyte XValue;        // Relative X (-127 to 127)
        public sbyte YValue;        // Relative Y (-127 to 127)
        public sbyte WheelPosition; // Wheel position (-127 to 127)

        public static VMultiRelativeMouseReport Create()
        {
            return new VMultiRelativeMouseReport
            {
                ReportID = VMultiReportIds.RelativeMouse,
                Button = 0,
                XValue = 0,
                YValue = 0,
                WheelPosition = 0
            };
        }
    }

    // Keyboard modifier flags (EN/FR: Flags des modificateurs clavier)
    [Flags]
    public enum VMultiKeyboardModifier : byte
    {
        None = 0x00,
        LeftControl = 0x01,
        LeftShift = 0x02,
        LeftAlt = 0x04,
        LeftGui = 0x08,
        RightControl = 0x10,
        RightShift = 0x20,
        RightAlt = 0x40,
        RightGui = 0x80
    }

    // Keyboard report (EN/FR: Rapport de clavier)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiKeyboardReport
    {
        public byte ReportID;            // REPORTID_KEYBOARD (0x07)
        public byte ShiftKeyFlags;       // Modifier key flags
        public byte Reserved;            // Always 0
        public byte KeyCode0;            // Key codes (up to 6 simultaneous keys)
        public byte KeyCode1;
        public byte KeyCode2;
        public byte KeyCode3;
        public byte KeyCode4;
        public byte KeyCode5;

        public const int MaxKeyCodes = 6;

        public static VMultiKeyboardReport Create()
        {
            return new VMultiKeyboardReport
            {
                ReportID = VMultiReportIds.Keyboard,
                ShiftKeyFlags = 0,
                Reserved = 0,
                KeyCode0 = 0,
                KeyCode1 = 0,
                KeyCode2 = 0,
                KeyCode3 = 0,
                KeyCode4 = 0,
                KeyCode5 = 0
            };
        }

        // Set key codes from array (EN/FR: Définir les codes de touche depuis un tableau)
        public void SetKeyCodes(byte[] keyCodes)
        {
            KeyCode0 = (keyCodes.Length > 0) ? keyCodes[0] : (byte)0;
            KeyCode1 = (keyCodes.Length > 1) ? keyCodes[1] : (byte)0;
            KeyCode2 = (keyCodes.Length > 2) ? keyCodes[2] : (byte)0;
            KeyCode3 = (keyCodes.Length > 3) ? keyCodes[3] : (byte)0;
            KeyCode4 = (keyCodes.Length > 4) ? keyCodes[4] : (byte)0;
            KeyCode5 = (keyCodes.Length > 5) ? keyCodes[5] : (byte)0;
        }

        // Get key codes as array (EN/FR: Obtenir les codes de touche comme tableau)
        public byte[] GetKeyCodes()
        {
            return new byte[] { KeyCode0, KeyCode1, KeyCode2, KeyCode3, KeyCode4, KeyCode5 };
        }
    }

    // Gamepad button flags for first byte (EN/FR: Flags des boutons gamepad pour premier octet)
    [Flags]
    public enum VMultiGamepadButtons1 : byte
    {
        None = 0x00,
        Button1 = 0x01,  // A
        Button2 = 0x02,  // B
        Button3 = 0x04,  // X
        Button4 = 0x08,  // Y
        Button5 = 0x10,  // Left Bumper
        Button6 = 0x20,  // Right Bumper
        Button7 = 0x40,  // Left Trigger (digital)
        Button8 = 0x80   // Right Trigger (digital)
    }

    // Gamepad button flags for second byte (EN/FR: Flags des boutons gamepad pour second octet)
    [Flags]
    public enum VMultiGamepadButtons2 : byte
    {
        None = 0x00,
        Button9 = 0x01,   // Back/Select
        Button10 = 0x02,  // Start
        Button11 = 0x04,  // Left Stick Click
        Button12 = 0x08,  // Right Stick Click
        DPadUp = 0x10,
        DPadDown = 0x20,
        DPadLeft = 0x40,
        DPadRight = 0x80
    }

    // Gamepad report for Col06 (DirectInput HID Game Controller)
    // Matches VMultiJoystickReport from vmulticommon.h (9 bytes)
    // (EN/FR: Rapport gamepad pour Col06 - Correspond à VMultiJoystickReport de vmulticommon.h)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiGamepadReport
    {
        public byte ReportID;      // REPORTID_JOYSTICK (0x06)
        public byte Throttle;      // Throttle (0-255)
        public sbyte XValue;        // Left Stick X (-127 to 127) - SIGNED
        public sbyte YValue;        // Left Stick Y (-127 to 127) - SIGNED
        public byte Hat;           // Hat Switch (0-7, 8=Neutral)
        public byte RXValue;       // Right Stick X (0-255) - UNSIGNED
        public byte RYValue;       // Right Stick Y (0-255) - UNSIGNED
        public ushort Buttons;     // Buttons (16 bits)

        public const byte AxisMin = 0;
        public const byte AxisMax = 255;
        public const sbyte AxisCenterSigned = 0; // Center for Signed SByte
        public const byte HatNeutral = 8; // 8 = Null State (Center) for standard 4-bit HAT (0-7 range)

        // Fields already defined above (lines 211-218)

        // Private state for Hat calculation
        private bool _dpadUp;
        private bool _dpadDown;
        private bool _dpadLeft;
        private bool _dpadRight;

        public static VMultiGamepadReport Create()
        {
            var report = new VMultiGamepadReport
            {
                ReportID = VMultiReportIds.Joystick,
                Throttle = 0,
                XValue = AxisCenterSigned,
                YValue = AxisCenterSigned,
                Hat = HatNeutral,
                RXValue = 128, // Center for Unsigned Byte (0-255)
                RYValue = 128, // Center for Unsigned Byte (0-255)
                Buttons = 0
            };
            report._dpadUp = false;
            report._dpadDown = false;
            report._dpadLeft = false;
            report._dpadRight = false;
            return report;
        }
    
        // ... SetButton ...

        public void SetAxis(GamePadAxis axis, float x, float y)
        {
            switch (axis)
            {
                case GamePadAxis.LeftStick:
                    // Signed: -1.0..1.0 -> -127..127
                    XValue = ClampConvertSigned(x);
                    YValue = ClampConvertSigned(y);
                    break;
                case GamePadAxis.RightStick:
                    // Unsigned: -1.0..1.0 -> 0..255
                    RXValue = ClampConvertUnsigned(x);
                    RYValue = ClampConvertUnsigned(y);
                    break;
            }
        }

        private byte ClampConvertUnsigned(float val)
        {
            // Clamp -1.0 to 1.0
            val = Math.Max(-1.0f, Math.Min(1.0f, val));
            // Map to 0..255
            float scaled = (val + 1.0f) * 127.5f;
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(scaled)));
        }

        private sbyte ClampConvertSigned(float val)
        {
            // Clamp -1.0 to 1.0
            val = Math.Max(-1.0f, Math.Min(1.0f, val));
            
            // Map to -127..127
            // 0.0 -> 0
            float scaled = val * 127.0f;
            return (sbyte)Math.Round(scaled);
        }

        /// <summary>
        /// EN: Set button state using GamePadButton enum.
        /// FR: Définir l'état d'un bouton via l'enum GamePadButton.
        /// </summary>
        public void SetButton(GamePadButton button, bool pressed)
        {
            // Handle DPad separately for Hat calculation but ALSO allow button text mapping
            switch (button)
            {
                case GamePadButton.DPadUp: _dpadUp = pressed; UpdateHat(); break;
                case GamePadButton.DPadDown: _dpadDown = pressed; UpdateHat(); break;
                case GamePadButton.DPadLeft: _dpadLeft = pressed; UpdateHat(); break;
                case GamePadButton.DPadRight: _dpadRight = pressed; UpdateHat(); break;
            }

            // Map other buttons to bitmask
            ushort flag = 0;
            switch (button)
            {
                case GamePadButton.Button1: flag = 0x01; break; // A
                case GamePadButton.Button2: flag = 0x02; break; // B
                case GamePadButton.Button3: flag = 0x04; break; // X
                case GamePadButton.Button4: flag = 0x08; break; // Y
                case GamePadButton.Button5: flag = 0x10; break; // LB
                case GamePadButton.Button6: flag = 0x20; break; // RB
                case GamePadButton.Button7: flag = 0x40; break; // LT (Digital)
                case GamePadButton.Button8: flag = 0x80; break; // RT (Digital)
                case GamePadButton.Button9: flag = 0x100; break; // Back
                case GamePadButton.Button10: flag = 0x200; break; // Start
                case GamePadButton.Button11: flag = 0x400; break; // LS Click
                case GamePadButton.Button12: flag = 0x800; break; // RS Click
                // Mirror D-Pad to Buttons (Bits 12-15) for compatibility
                case GamePadButton.DPadUp:    flag = 0x1000; break;
                case GamePadButton.DPadDown:  flag = 0x2000; break;
                case GamePadButton.DPadLeft:  flag = 0x4000; break;
                case GamePadButton.DPadRight: flag = 0x8000; break;
            }

            if (flag != 0)
            {
                if (pressed) Buttons |= flag;
                else Buttons &= (ushort)~flag;
            }
        }

        private void UpdateHat()
        {
            // DISABLED: User reported Hat conflicts and "All Up" behavior.
            // Using Buttons only for D-Pad now.
            Hat = HatNeutral;

            /*
            // Calculated Hat position based on DPad state (0-7, 8=Neutral)
            if (_dpadUp)
            {
                if (_dpadRight) Hat = 1; // Up-Right
                else if (_dpadLeft) Hat = 7; // Up-Left
                else Hat = 0; // Up
            }
            else if (_dpadDown)
            {
                if (_dpadRight) Hat = 3; // Down-Right
                else if (_dpadLeft) Hat = 5; // Down-Left
                else Hat = 4; // Down
            }
            else if (_dpadLeft)
            {
                Hat = 6; // Left
            }
            else if (_dpadRight)
            {
                Hat = 2; // Right
            }
            else
            {
                Hat = HatNeutral; // Center
            }
            */
        }

        // Old SetAxis removed to avoid ambiguity with the new signed version.
    }

    // Full control report structure for sending via HID (EN/FR: Structure de rapport de contrôle complète)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VMultiControlReport
    {
        public const int Size = 65; // CONTROL_REPORT_SIZE (0x41)
        
        public byte ReportID;      // REPORTID_CONTROL (0x40)
        public byte ReportLength;  // Length of embedded report
        
        // Embedded report data (max 63 bytes)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 63)]
        public byte[] Data;

        public static VMultiControlReport Create()
        {
            return new VMultiControlReport
            {
                ReportID = VMultiReportIds.Control,
                ReportLength = 0,
                Data = new byte[63]
            };
        }

        // Embed a mouse report (EN/FR: Intégrer un rapport souris)
        public void EmbedMouseReport(VMultiMouseReport mouseReport)
        {
            ReportLength = (byte)Marshal.SizeOf(typeof(VMultiMouseReport));
            
            IntPtr ptr = Marshal.AllocHGlobal(ReportLength);
            try
            {
                Marshal.StructureToPtr(mouseReport, ptr, false);
                Marshal.Copy(ptr, Data, 0, ReportLength);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Embed a keyboard report (EN/FR: Intégrer un rapport clavier)
        public void EmbedKeyboardReport(VMultiKeyboardReport keyboardReport)
        {
            ReportLength = (byte)Marshal.SizeOf(typeof(VMultiKeyboardReport));
            
            IntPtr ptr = Marshal.AllocHGlobal(ReportLength);
            try
            {
                Marshal.StructureToPtr(keyboardReport, ptr, false);
                Marshal.Copy(ptr, Data, 0, ReportLength);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Embed a relative mouse report (EN/FR: Intégrer un rapport souris relative)
        public void EmbedRelativeMouseReport(VMultiRelativeMouseReport relativeMouseReport)
        {
            ReportLength = (byte)Marshal.SizeOf(typeof(VMultiRelativeMouseReport));
            
            IntPtr ptr = Marshal.AllocHGlobal(ReportLength);
            try
            {
                Marshal.StructureToPtr(relativeMouseReport, ptr, false);
                Marshal.Copy(ptr, Data, 0, ReportLength);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Embed a gamepad report (EN/FR: Intégrer un rapport gamepad)
        public void EmbedGamepadReport(VMultiGamepadReport gamepadReport)
        {
            ReportLength = (byte)Marshal.SizeOf(typeof(VMultiGamepadReport));
            
            IntPtr ptr = Marshal.AllocHGlobal(ReportLength);
            try
            {
                Marshal.StructureToPtr(gamepadReport, ptr, false);
                Marshal.Copy(ptr, Data, 0, ReportLength);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Convert to byte array for HID write (EN/FR: Convertir en tableau d'octets pour écriture HID)
        public byte[] ToByteArray()
        {
            byte[] result = new byte[Size];
            result[0] = ReportID;
            result[1] = ReportLength;
            
            if (Data != null)
            {
                Array.Copy(Data, 0, result, 2, Math.Min(Data.Length, 63));
            }
            
            return result;
        }
    }
}
