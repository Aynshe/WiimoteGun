using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib.DataTypes;
using WiimoteLib.Events;
using WiimoteLib.Util;

namespace WiimoteLib {
	public partial class Wiimote : IDisposable {
		/// <summary>Initialize the MotionPlus extension.</summary>
		public void EnableMotionPlus(MotionPlusExtensionType extension = MotionPlusExtensionType.NoExtension) {
			// EN: Only update if mode changes to avoid disruptive init writes
			// FR: Ne mettre à jour que si le mode change pour éviter les écritures init disruptives
			if (wiimoteState.MotionPlus.ExtensionType == extension && extension != MotionPlusExtensionType.NoExtension)
				return;

			Log.Debug("InitializeMotionPlus: " + extension);

            // [FIX V20] WiiBrew Official Sequence for MotionPlus Activation
            // EN: The correct init sequence according to WiiBrew (https://wiibrew.org/wiki/Wiimote/Extension_Controllers/Wii_Motion_Plus):
            //   1. Write 0x55 to 0xA400F0 (ExtensionInit1) — initializes the extension (Nunchuk) behind the MP
            //   2. Write 0x00 to 0xA400FB (ExtensionInit2) — decrypts the extension
            //   3. Write mode byte to 0xA600FE (MotionPlusEnable) — activates MP directly in desired mode
            //      0x04 = MP only, 0x05 = Nunchuk passthrough, 0x07 = Classic Controller passthrough
            //
            // IMPORTANT: 0xA400F0 is both ExtensionInit1 AND MotionPlusDisable. However, writing 0x55 here
            //   only deactivates the MP if it is ALREADY active. Before first activation, this is safe and
            //   required to initialize the extension behind the MP adapter.
            //
            // V19 ERROR: We skipped 0x55 for Nunchuk passthrough, preventing proper Nunchuk init.
            // V15 ERROR: We wrote 0x04 then 0x05 sequentially, but 0x04 locked the MP in no-extension mode.
            //
            // FR: Séquence officielle WiiBrew. Le 0x55 est NÉCESSAIRE pour initialiser le Nunchuk derrière
            //     l'adaptateur MP avant activation. L'écriture de 0x04 puis 0x05 bloquait en mode sans extension.
            //     On écrit directement le mode souhaité (0x05 pour passthrough Nunchuk).

            // Step 1: Initialize extension behind MP adapter (EN/FR: Initialiser l'extension derrière l'adaptateur MP)
            Log.Info($"[EnableMotionPlus] Step 1: Extension init (0x55 to 0xA400F0, 0x00 to 0xA400FB)");
            WriteByte(Registers.ExtensionInit1, 0x55);
            WriteByte(Registers.ExtensionInit2, 0x00);
            System.Threading.Thread.Sleep(50);
            
            // Step 2: Activate MotionPlus directly in desired mode (EN/FR: Activer le MP directement dans le mode souhaité)
            Log.Info($"[EnableMotionPlus] Step 2: Activate MP (0x{(byte)extension:X2} to 0xA600FE)");
		    WriteByte(Registers.MotionPlusEnable, (byte) extension);
            System.Threading.Thread.Sleep(50);

            // [FIX] Verify Activation Success
            // EN: Read Extension ID at 0xA400FA to confirm MP mode.
            //     WiiBrew: "The standard extension identifier at 0x(4)A400FA now reads 00 00 A4 20 04 05"
            //     MP only (0x04): ID = 0x0000A4200405 (ExtensionType.MotionPlus)
            //     Nunchuk passthrough (0x05): ID = 0x0000A4200505 (ExtensionType.MotionPlusNunchuk)
            // FR: Lire l'ID extension à 0xA400FA pour confirmer le mode MP.
            System.Threading.Thread.Sleep(50); // Wait for state change (EN/FR: Attendre le changement d'état)
            byte[] idBuff = ReadData(Registers.ExtensionType1, 6);
            long id = ((long)idBuff[0] << 40) | ((long)idBuff[1] << 32) | ((long)idBuff[2] << 24) | ((long)idBuff[3] << 16) | ((long)idBuff[4] << 8) | idBuff[5];
            
            bool activationFailed = false;
            
            if (id == 0x0000A4200000)
            {
                // EN: ID is still Nunchuk — MP did not activate
                // FR: L'ID est toujours Nunchuk — le MP ne s'est pas activé
                Log.Warning($"[EnableMotionPlus] Activation Failed: ID is still Nunchuk (0x{id:X12}). Reverting to Nunchuk mode.");
                activationFailed = true;
            }
            else if (extension == MotionPlusExtensionType.Nunchuk && id == (long)ExtensionType.MotionPlusNunchuk)
            {
                // EN: Expected ID for Nunchuk passthrough — perfect!
                // FR: ID attendu pour le passthrough Nunchuk — parfait !
                Log.Info($"[EnableMotionPlus] Activation Success! ID: 0x{id:X12} (MotionPlusNunchuk passthrough)");
            }
            else if (extension == MotionPlusExtensionType.NoExtension && id == (long)ExtensionType.MotionPlus)
            {
                // EN: Expected ID for MP only — perfect!
                // FR: ID attendu pour le MP seul — parfait !
                Log.Info($"[EnableMotionPlus] Activation Success! ID: 0x{id:X12} (MotionPlus only)");
            }
            else if (id == (long)ExtensionType.MotionPlusNunchuk)
            {
                // [FIX V22c] EN: Hardware is in Nunchuk passthrough mode (0x0000A4200505).
                // This happens when a concurrent AutoEnableMotionPlus call activated passthrough
                // between our write and our ID read. Accept the hardware state to stay in sync.
                // FR: Le hardware est en mode passthrough Nunchuk. Cela arrive quand un appel
                // concurrent a activé le passthrough entre notre écriture et notre lecture d'ID.
                // Accepter l'état hardware pour rester synchronisé.
                extension = MotionPlusExtensionType.Nunchuk;
                Log.Info($"[EnableMotionPlus] Hardware in Nunchuk passthrough (0x{id:X12}). Syncing state to passthrough.");
            }
            else if (id == (long)ExtensionType.MotionPlus && extension != MotionPlusExtensionType.NoExtension)
            {
                // [FIX V22c] EN: Hardware is in standalone mode (0x0000A4200405) but we asked
                // for passthrough. A concurrent call may have switched back to standalone.
                // Accept the hardware state.
                // FR: Le hardware est en standalone mais on a demandé passthrough. Un appel
                // concurrent a pu basculer en standalone. Accepter l'état hardware.
                extension = MotionPlusExtensionType.NoExtension;
                Log.Info($"[EnableMotionPlus] Hardware in standalone (0x{id:X12}). Syncing state to standalone.");
            }
            else
            {
                // EN: Unknown ID — log for debugging but continue
                // FR: ID inconnu — logger pour le debug mais continuer
                Log.Info($"[EnableMotionPlus] Unexpected Extension ID: 0x{id:X12} (expected mode 0x{(byte)extension:X2})");
            }

			wiimoteState.MotionPlus.ExtensionType = extension;

            // EN: Force ExtensionType update so parsing logic knows what to expect
            // FR: Forcer la mise à jour du ExtensionType pour la logique de parsing
            if (activationFailed)
            {
                // [FIX V21] EN: MP activation failed (no MP adapter). The init writes (0x55+0x00+0x05)
                // have corrupted the Nunchuk state. We must re-initialize the Nunchuk properly:
                //   1. Re-send 0x55 + 0x00 to re-init the extension
                //   2. Re-read calibration data
                // Without this, the joystick and accel data are stuck at zero.
                // FR: L'activation MP a échoué (pas d'adapteur MP). Les écritures init (0x55+0x00+0x05)
                // ont corrompu le Nunchuk. On doit le ré-initialiser :
                //   1. Renvoyer 0x55 + 0x00 au Nunchuk
                //   2. Relire les données de calibration
                Log.Info("[EnableMotionPlus] Re-initializing Nunchuk after failed MP activation...");
                WriteByte(Registers.ExtensionInit1, 0x55);
                WriteByte(Registers.ExtensionInit2, 0x00);
                System.Threading.Thread.Sleep(50);

                // EN: Re-read Nunchuk calibration (FR: Relire la calibration du Nunchuk)
                try {
                    byte[] calibBuff = ReadData(Registers.ExtensionCalibration, 16);
                    wiimoteState.Nunchuk.CalibrationInfo.Parse(calibBuff, 0);
                    Log.Info("[EnableMotionPlus] Nunchuk re-initialized successfully after MP activation failure.");
                    var calib = wiimoteState.Nunchuk.CalibrationInfo;
                    Log.Debug($"[EnableMotionPlus] Nunchuk Calib: Max=({calib.Max.X},{calib.Max.Y}) Min=({calib.Min.X},{calib.Min.Y}) Mid=({calib.Mid.X},{calib.Mid.Y})");
                }
                catch (Exception ex) {
                    Log.Warning($"[EnableMotionPlus] Failed to re-read Nunchuk calibration: {ex.Message}");
                }

                // EN: Revert to Nunchuk mode (FR: Revenir en mode Nunchuk)
                wiimoteState.ExtensionType = ExtensionType.Nunchuk;
                wiimoteState.MotionPlus.ExtensionType = MotionPlusExtensionType.NoExtension;
            }
            else
            {
                switch(extension)
                {
                    case MotionPlusExtensionType.Nunchuk:
                        wiimoteState.ExtensionType = ExtensionType.MotionPlusNunchuk;
                        
                        // [FIX V22h] EN: Cannot read Nunchuk calibration registers (0xA40020) when MotionPlus is active.
                        // The address space 0xA400xx is remapped to MotionPlus, so ReadData returns MP register data,
                        // not Nunchuk calibration. V22g read returned Mid=(39,48) instead of ~(128,128), causing
                        // normalized joystick to exceed 0.3f threshold at rest → phantom NunUp/NunDown inputs.
                        // Solution: Always apply known-good default values for passthrough mode.
                        // FR: Impossible de lire la calibration Nunchuk quand le MP est actif (espace 0xA400xx remappé).
                        // V22g lisait Mid=(39,48) au lieu de ~(128,128), déclenchant des inputs fantômes.
                        ApplyDefaultNunchukCalibration();
                        break;
                    case MotionPlusExtensionType.ClassicController:
                        wiimoteState.ExtensionType = ExtensionType.MotionPlusOther;
                        break;
                    case MotionPlusExtensionType.NoExtension:
                        wiimoteState.ExtensionType = ExtensionType.MotionPlus;
                        break;
                }
            }
            wiimoteState.Extension = true;
		}

		/// <summary>
		/// [FIX V22g] EN: Apply safe default Nunchuk calibration values when the read fails or returns zeros.
		/// FR: Appliquer des valeurs de calibration Nunchuk par défaut quand la lecture échoue ou retourne des zéros.
		/// </summary>
		private void ApplyDefaultNunchukCalibration()
		{
			// EN: Joystick defaults (typical Nunchuk range)
			// FR: Défauts joystick (plage typique du Nunchuk)
			wiimoteState.Nunchuk.CalibrationInfo.Min.X = 35;
			wiimoteState.Nunchuk.CalibrationInfo.Mid.X = 128;
			wiimoteState.Nunchuk.CalibrationInfo.Max.X = 228;
			wiimoteState.Nunchuk.CalibrationInfo.Min.Y = 27;
			wiimoteState.Nunchuk.CalibrationInfo.Mid.Y = 128;
			wiimoteState.Nunchuk.CalibrationInfo.Max.Y = 220;

			// EN: Accelerometer defaults (Zero=512, Gravity≈+200)
			// FR: Défauts accéléromètre (Zéro=512, Gravité≈+200)
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Zero.X = 512;
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Zero.Y = 512;
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Zero.Z = 512;
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Gravity.X = 712;
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Gravity.Y = 712;
			wiimoteState.Nunchuk.CalibrationInfo.AccelCalibration.Gravity.Z = 712;

			Log.Info("[ApplyDefaultNunchukCalibration] Default calibration applied: Joy Mid=(128,128) Max=(228,220) Min=(35,27)");
		}

		/// <summary>Turns off the MotionPlus extension.</summary>
		public void DisableMotionPlus() {
			Log.Debug("DisableMotionPlus");
			//if (mWiimoteState.MotionPlus.ExtensionType != MotionPlusExtensionType.NoExtension) {
			WriteByte(Registers.MotionPlusDisable, 0x55);
			wiimoteState.MotionPlus.ExtensionType = MotionPlusExtensionType.NoExtension;
			//}
		}

		/// <summary>Set Wiimote reporting mode (if using an IR report type, IR
		/// sensitivity is set to WiiLevel3).</summary>
		/// <param name="type">Report type</param>
		/// <param name="continuous">Continuous data</param>
		public void SetReportType(ReportType type, bool continuous) {
			Log.Debug("SetReportType: " + type);
			SetReportType(type, IRSensitivity.Maximum, continuous);
		}

		/// <summary>Set Wiimote reporting mode.</summary>
		/// <param name="reportType">Report type</param>
		/// <param name="irSensitivity">IR sensitivity</param>
		/// <param name="continuous">Continuous data</param>
		/// <param name="forceIRInit">EN: Force full IR sensor initialization / FR: Forcer l'initialisation complète du capteur IR</param>
		public void SetReportType(ReportType reportType, IRSensitivity irSensitivity, bool continuous, bool forceIRInit = true) {
			Log.Debug(string.Format("[Wiimote] SetReportType: {0} (Force IR: {1})", reportType, forceIRInit));
			Log.Debug(new StackTrace().ToString());
			InputReport type = (InputReport) reportType;
			DataReportAttribute dataReport =
				EnumInfo<InputReport>.TryGetAttribute<DataReportAttribute>(type);

			if (dataReport == null)
				throw new WiimoteException(this, string.Format("{0} is not a valid report type!", type));

			if (forceIRInit) {
				int irSize = dataReport.IRSize;
				if (dataReport.IsInterleaved)
					irSize *= 2;

				switch (dataReport.IRSize) {
				case 10:
					EnableIR(IRMode.Basic, irSensitivity);
					break;
				case 12:
					EnableIR(IRMode.Extended, irSensitivity);
					break;
				case 36:
					EnableIR(IRMode.Full, irSensitivity);
					break;
				default:
					DisableIR();
					break;
				}
			}
			
			byte[] buff = CreateReport(OutputReport.InputReportType);

			buff[1] = (byte) (continuous ? 0x04 : 0x00);
			buff[2] = (byte) type;

			WriteReport(buff);
			wiimoteState.ReportType = reportType;
			wiimoteState.ContinuousReport = continuous;
		}

		/// <summary>Set the LEDs on the Wiimote.</summary>
		/// <param name="led1">LED 1</param>
		/// <param name="led2">LED 2</param>
		/// <param name="led3">LED 3</param>
		/// <param name="led4">LED 4</param>
		public void SetLEDs(bool led1, bool led2, bool led3, bool led4) {
			LEDs leds = LEDs.None;
			if (led1) leds |= LEDs.LED1;
			if (led2) leds |= LEDs.LED2;
			if (led3) leds |= LEDs.LED3;
			if (led4) leds |= LEDs.LED4;
			SetLEDs(leds);
		}

		/// <summary>Set the LEDs on the Wiimote.</summary>
		public void SetLEDs(LEDs leds) {
			wiimoteState.Status.LEDs = leds;

			byte[] buff = CreateReport(OutputReport.LEDs);
			buff[1] = (byte) ((byte) leds << 4);
			
			WriteReport(buff);
		}

		/// <summary>Set 1-indexed player LED.</summary>
		public void SetPlayerLED(int player) {
			LEDs leds = LEDs.None;
			switch (player) {
			case 1: leds = LEDs.LED1; break;
			case 2: leds = LEDs.LED2; break;
			case 3: leds = LEDs.LED3; break;
			case 4: leds = LEDs.LED4; break;
			case 5: leds = LEDs.LED1 | LEDs.LED2; break;
			case 6: leds = LEDs.LED1 | LEDs.LED3; break;
			case 7: leds = LEDs.LED1 | LEDs.LED4; break;
			case 8: leds = LEDs.LED1 | LEDs.LED2 | LEDs.LED3; break;
			case 9: leds = LEDs.LED1 | LEDs.LED2 | LEDs.LED4; break;
			}
			if (player > 9)
				leds = LEDs.LED1 | LEDs.LED2 | LEDs.LED3 | LEDs.LED4;
			SetLEDs(leds);
		}

		/// <summary>Toggle rumble.</summary>
		/// <param name="on">On or off</param>
		public void SetRumble(bool on) {
			wiimoteState.Status.Rumble = on;

			byte[] buff = CreateReport(OutputReport.Rumble);
			WriteReport(buff);
		}
	}
}
