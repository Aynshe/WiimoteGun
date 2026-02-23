using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib.Geometry;
using WiimoteLib.Util;

namespace WiimoteLib.DataTypes {
	/// <summary>
	/// Current state of the MotionPlus controller
	/// </summary>
	[Serializable]
	public struct MotionPlusState {

		private const int Zero = 8063;
		private const float UnitsToDegreesPerSecond = 8192f / 595f;
		private const float HighSpeed = 2000f / 440f;

		/// <summary>The calibration info for the Wii Motion Plus.</summary>
		public MotionPlusCalibrationInfo CalibrationInfo;

		/// <summary>
		/// Raw speed data
		/// <remarks>Values range between 0 - 16384</remarks>
		/// </summary>
		public PitchYawRollI RawValues;

		/// <summary>
		/// Values in degrees per second.
		/// </summary>
		public PitchYawRollF Values;

		/// <summary>
		/// Yaw/Pitch/Roll rotating "slowly"
		/// </summary>
		public bool YawSlow, RollSlow, PitchSlow;
		
		/// <summary>An extension is connected to the Wiimotion Plus</summary>
		public bool HasExtension;

		public MotionPlusExtensionType ExtensionType;

		public bool IsDetected;

		internal void Parse(byte[] buff, int off, bool passthrough) {
			if (!passthrough) {
				YawSlow = buff.GetBit(off + 3, 1);
				RollSlow = buff.GetBit(off + 4, 1);
				PitchSlow = buff.GetBit(off + 3, 0);

				HasExtension = buff.GetBit(off + 4, 0);

				// Mode 0x04: Get raw
				RawValues.Yaw = ((buff[off + 3] & 0xFC) << 6) | buff[off + 0];
				RawValues.Roll = ((buff[off + 4] & 0xFC) << 6) | buff[off + 1];
				RawValues.Pitch = ((buff[off + 5] & 0xFC) << 6) | buff[off + 2];
			}
			else {
				// [FIX V17] Mode 0x05: Nunchuk Passthrough
				// EN: WiiBrew: "The data format for the Motion Plus does not change" in passthrough.
				//     Only Byte 5 bits 0-1 become bookkeeping (frame type + extension connected),
				//     but the mask & 0xFC already excludes them. Use the SAME 14-bit formula as normal mode.
				// FR: WiiBrew : « Le format des données du MotionPlus ne change pas » en passthrough.
				//     Seuls les bits 0-1 du Byte 5 deviennent bookkeeping (type de frame + extension connectée),
				//     mais le masque & 0xFC les exclut déjà. Utiliser la MÊME formule 14 bits qu'en mode normal.

				// EN: Slow bits are in the SAME positions as normal mode for MotionPlus frames.
				//     WiiBrew: "The data format for the Motion Plus does not change" in passthrough.
				//     Byte 5 bit repositioning only applies to Nunchuk extension frames, NOT MP frames.
				// FR: Les bits Slow sont aux MÊMES positions qu'en mode normal pour les frames MotionPlus.
				//     WiiBrew : « Le format des données du MotionPlus ne change pas » en passthrough.
				//     Le repositionnement des bits du Byte 5 s'applique seulement aux frames extension Nunchuk.
				YawSlow = buff.GetBit(off + 3, 1); 
				RollSlow = buff.GetBit(off + 4, 1); 
				PitchSlow = buff.GetBit(off + 3, 0);
				HasExtension = buff.GetBit(off + 4, 0);

				// EN: SAME 14-bit formula as normal mode — WiiBrew confirms format is unchanged.
				//     The & 0xFC mask on bytes 3,4,5 strips the low 2 bits which are bookkeeping in passthrough.
				// FR: MÊME formule 14 bits qu'en mode normal — WiiBrew confirme que le format est inchangé.
				//     Le masque & 0xFC sur les bytes 3,4,5 supprime les 2 bits bas qui sont bookkeeping en passthrough.
				RawValues.Yaw   = ((buff[off + 3] & 0xFC) << 6) | buff[off + 0];
				RawValues.Roll  = ((buff[off + 4] & 0xFC) << 6) | buff[off + 1];
				RawValues.Pitch = ((buff[off + 5] & 0xFC) << 6) | buff[off + 2];
			}

			// Zero raw
			Values.Yaw   = RawValues.Yaw   - Zero;
			Values.Roll  = RawValues.Roll  - Zero;
			Values.Pitch = RawValues.Pitch - Zero;
			
			// Multiply when high speed
			Values.Yaw   *= (YawSlow   ? 1f : HighSpeed);
			Values.Roll  *= (RollSlow  ? 1f : HighSpeed);
			Values.Pitch *= (PitchSlow ? 1f : HighSpeed);

			// Convert units
			Values.Yaw   /= UnitsToDegreesPerSecond;
			Values.Roll  /= UnitsToDegreesPerSecond;
			Values.Pitch /= UnitsToDegreesPerSecond;
		}
	}
}
