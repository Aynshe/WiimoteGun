using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib.Geometry;
using WiimoteLib.Util;

namespace WiimoteLib.DataTypes {
	/// <summary>
	/// Current state of the Nunchuk extension
	/// </summary>
	[Serializable]
	public struct NunchukState {
		/// <summary>
		/// Calibration data for Nunchuk extension
		/// </summary>
		public NunchukCalibrationInfo CalibrationInfo;
		/// <summary>
		/// State of accelerometers
		/// </summary>
		public AccelState Accel;
		/// <summary>
		/// Raw joystick position before normalization.  Values range between 0 and 255.
		/// </summary>
		public Point2I RawJoystick;
		/// <summary>
		/// Normalized joystick position.  Values range between -0.5 and 0.5
		/// </summary>
		public Point2F Joystick;
		/// <summary>
		/// Digital button on Nunchuk extension
		/// </summary>
		public bool C, Z;

		const int AnalogStickCenter = 128;
		static readonly Point2I JoystickMin = new Point2I(35, 27);
		static readonly Point2I JoystickNax = new Point2I(228, 220);

		internal void Parse(byte[] buff, int off, bool passthrough) {
			if (!passthrough) {
				Accel.ParseNunchuk(buff, off, passthrough, CalibrationInfo.AccelCalibration);
				Z = !buff.GetBit(off + 5, 0);
				C = !buff.GetBit(off + 5, 1);

                // Normal Mode: Parse Joystick
                RawJoystick.X = buff[off + 0];
			    RawJoystick.Y = buff[off + 1];

			    if (CalibrationInfo.Max.X != 0)
				    Joystick.X = (float) (RawJoystick.X - CalibrationInfo.Mid.X) /
									     (CalibrationInfo.Max.X - CalibrationInfo.Min.X);
			    else
				    Joystick.X = 0f;

			    if (CalibrationInfo.Max.Y != 0)
				    Joystick.Y = (float) (RawJoystick.Y - CalibrationInfo.Mid.Y) /
									     (CalibrationInfo.Max.Y - CalibrationInfo.Min.Y);
			    else
				    Joystick.Y = 0f;
			}
			else {
				// EN: In passthrough mode (0x05), Accel data is replaced by Gyro data in bytes 2,3,4.
				// FR: En mode passthrough (0x05), les données Accel sont remplacées par le Gyro en octets 2,3,4.
				// [FIX V20b] WiiBrew Passthrough Nunchuk Byte 5 bit shifts:
				//   Normal:      Bit 0=BZ, Bit 1=BC
				//   Passthrough: Bit 0 → Bit 2, Bit 1 → Bit 3 (shifted per WiiBrew documentation)
				//   So: BZ is at Bit 2, BC is at Bit 3
				// V15 ERROR: Had Z=Bit3, C=Bit4 (shifted one position too high, causing phantom button presses)
				// Confirmed by the original ParsePassthrough code (commented below): Z=0x4(bit2), C=0x8(bit3)
				Z = !buff.GetBit(off + 5, 2); 
				C = !buff.GetBit(off + 5, 3); 

                // [FIX] User Request: Silence Nunchuk ACCEL only (Keep Joystick)
                // In Passthrough, Bytes 0-1 are Joystick.
                RawJoystick.X = buff[off + 0];
			    RawJoystick.Y = buff[off + 1];

			    if (CalibrationInfo.Max.X != 0)
				    Joystick.X = (float) (RawJoystick.X - CalibrationInfo.Mid.X) /
									     (CalibrationInfo.Max.X - CalibrationInfo.Min.X);
			    else
				    Joystick.X = 0f;

			    if (CalibrationInfo.Max.Y != 0)
				    Joystick.Y = (float) (RawJoystick.Y - CalibrationInfo.Mid.Y) /
									     (CalibrationInfo.Max.Y - CalibrationInfo.Min.Y);
			    else
				    Joystick.Y = 0f;

                // [FIX V20c] EN: Parse Nunchuk Accel in passthrough mode (data IS available).
                // WiiBrew confirms Bytes 2-4 = AX/AY/AZ[9:2], Byte 5 bits 2-7 = LSBs.
                // FR: Parser l'accel Nunchuk en passthrough (les données SONT disponibles).
                Accel.ParseNunchuk(buff, off, passthrough, CalibrationInfo.AccelCalibration);
			}
		}

		/*internal void ParsePassthrough(byte[] buff, int off) {
			Accel.ParseNunchukPassThrough(buff, off, CalibrationInfo.AccelCalibration);

			Z = (buff[off + 5] & 0x4) != 0;
			C = (buff[off + 5] & 0x8) != 0;

			Z = buff.GetBit(off + 5, 2);
			C = buff.GetBit(off + 5, 3);

			ParseJoystick(buff, off);
		}

		private void ParseJoystick(byte[] buff, int off) {
			RawJoystick.X = buff[off + 0];
			RawJoystick.Y = buff[off + 1];

			if (CalibrationInfo.Max.X != 0x00)
				Joystick.X = (float) (RawJoystick.X - CalibrationInfo.Mid.X) /
									 (CalibrationInfo.Max.X - CalibrationInfo.Min.X);

			if (CalibrationInfo.Max.Y != 0x00)
				Joystick.Y = (float) (RawJoystick.Y - CalibrationInfo.Mid.Y) /
									 (CalibrationInfo.Max.Y - CalibrationInfo.Min.Y);
		}*/
	}
}
