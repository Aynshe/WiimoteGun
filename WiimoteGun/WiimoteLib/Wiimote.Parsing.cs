using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiimoteLib.DataTypes;
using WiimoteLib.Events;
using WiimoteLib.Util;

namespace WiimoteLib {
	public partial class Wiimote : IDisposable {
		private byte[] interleavedBufferA;
		private DataReportAttribute interleavedReportA;
		private volatile bool _extensionChangePending = false;

		/// <summary>
		/// Parse a report sent by the Wiimote
		/// </summary>
		/// <param name="buff">Data buffer to parse</param>
		/// <returns>Returns a boolean noting whether an event needs to be posted</returns>
		private bool ParseInputReport(byte[] buff) {
			//try {
			InputReport type = (InputReport) buff[0];
			DataReportAttribute dataReport =
				EnumInfo<InputReport>.TryGetAttribute<DataReportAttribute>(type);

			if (dataReport != null) {
				// EN: Watchdog - check if we got the expected report type. 
				// SDL/external apps might change the mode, causing IR tracking to stop.
				// FR: Watchdog - vérifier si nous avons reçu le type de rapport attendu.
				// SDL/apps externes peuvent changer le mode, arrêtant le tracking IR.
				if (type != (InputReport)wiimoteState.ReportType) {
					if (wrongReportCount < 0) wrongReportCount = 0;
					wrongReportCount++;
					
					// EN: If we receive 0x30 (Buttons only) while expecting IR, it's almost certainly SDL/external interference.
					// FR: Si nous reçevons 0x30 (Boutons seuls) alors qu'on attend l'IR, c'est presque certainement SDL ou une interférence externe.
					bool isExternalReset = (type == InputReport.Buttons && ((InputReport)wiimoteState.ReportType).ToString().Contains("IR"));

					// EN: If we get even ONE wrong report, react immediately to eliminate stutter.
					if (!recovering) {
						recovering = true;
						recoveryCount++;

						// EN: Perform recovery in background to avoid blocking the read thread.
						// Blocking here causes deadlocks because ACKs won't be processed.
						// FR: Effectuer la récupération en arrière-plan pour éviter de bloquer le thread de lecture.
						// Bloquer ici cause des impasses car les ACKs ne seront pas traités.
						Task.Factory.StartNew(() => {
							try {
								bool deep = (recoveryCount % 100 == 0);
								if (isExternalReset && (deep || recoveryCount % 10 == 0))
									Log.Warning(string.Format("[Watchdog] Recovery #{0} (External reset={1}, Deep={2}). Re-asserting {3}...", recoveryCount, isExternalReset, deep, wiimoteState.ReportType));

								// EN: Force IR init if it was an external reset to Buttons mode, to be sure hardware is still on.
								// FR: Forcer l'init IR si c'était un reset externe vers Buttons, pour être sûr que le hardware est toujours actif.
								SetReportType(wiimoteState.ReportType, wiimoteState.IRState.Sensitivity, wiimoteState.ContinuousReport, isExternalReset || deep);
								
								// EN: Cooldown to avoid report flood
								System.Threading.Thread.Sleep(deep ? 50 : 20);
							}
							catch (Exception ex) {
								Log.Error(string.Format("[Watchdog] Async recovery failed: {0}", ex.Message));
							}
							finally {
								wrongReportCount = 0;
								recovering = false;
							}
						});
					}
				}
				else {
					// EN: Correct report received - reset conflict counter.
					// FR: Rapport correct reçu - réinitialiser le compteur de conflit.
					if (wrongReportCount > 0) wrongReportCount = 0;

					// EN: Stability detected - reset recovery cycle after 60 good reports (~1s).
					// FR: Stabilité détectée - réinitialiser le cycle de récupération après 60 bons rapports (~1s).
					if (recoveryCount > 0) {
						wrongReportCount--; // Using negative values as stability counter
						if (wrongReportCount < -60) {
							recoveryCount = 0;
							wrongReportCount = 0;
						}
					}
				}

				// Buttons are ALWAYS parsed
				if (dataReport.HasButtons)
					ParseButtons2(buff, dataReport.ButtonsOffset + 1);

				switch (dataReport.Interleave) {
				case Interleave.None:
					if (dataReport.HasAccel)
						ParseAccel2(buff, dataReport.AccelOffset + 1);

					if (dataReport.HasIR)
						ParseIR2(buff, dataReport.IROffset + 1, dataReport.IRSize);

					if (dataReport.HasExt)
						ParseExtension2(buff, dataReport.ExtOffset + 1, dataReport.ExtSize);
					break;
				case Interleave.A:
					interleavedBufferA = buff;
					interleavedReportA = dataReport;
					break;
				case Interleave.B:
					byte[] buffA = interleavedBufferA;
					byte[] buffB = buff;
					DataReportAttribute reportA = interleavedReportA;
					DataReportAttribute reportB = dataReport;
					ParseAccelInterleaved2(buffA, buffB, reportA.AccelOffset + 1, reportB.AccelOffset + 1);
					ParseIRInterleaved2(buffA, buffB, reportA.IROffset + 1, reportB.IROffset + 1);
					break;
				}

				return true;
			}
			else {
				switch (type) {
				case InputReport.Status:
						Log.Debug("******** STATUS ********");

					ExtensionType extensionTypeLast = wiimoteState.ExtensionType;
					bool extensionLast = wiimoteState.Status.Extension;
					ParseButtons2(buff, 1);
					ParseStatus2(buff, 3);
					bool extensionNew = WiimoteState.Status.Extension;

					using (AsyncReadState state = BeginAsyncRead()) {
						byte extensionType = 0;
						if (extensionNew)
							extensionType = ReadByte(Registers.ExtensionType2);

							Log.Debug($"Extension byte={extensionType:X2}");

							// extension connected?
							Log.Debug($"Extension, Old: {extensionLast}, New: {extensionNew}");

						// EN: Only initialize if extension connection state changed or if not yet initialized
						// FR: N'initialiser que si l'état de connexion de l'extension a changé ou si pas encore initialisé
						if (extensionNew != extensionLast || (extensionNew && (wiimoteState.ExtensionType == ExtensionType.None || wiimoteState.ExtensionType == ExtensionType.MotionPlus || wiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk))) {
							if (extensionNew) {
								// EN: If in standalone MotionPlus mode and extension just appeared, try passthrough directly
								// instead of generic InitializeExtension (which can't reliably read ID in MP mode)
								// FR: Si en mode MotionPlus standalone et extension détectée, tenter le passthrough directement
								// plutôt que InitializeExtension générique (qui ne peut pas lire l'ID fiablement en mode MP)
								if (wiimoteState.ExtensionType == ExtensionType.MotionPlus) {
									Log.Info("[ParseInputReport] Extension change detected while in MotionPlus standalone. Triggering passthrough probe.");
									System.Threading.Tasks.Task.Run(() => {
										try {
											System.Threading.Thread.Sleep(150);
											EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
										}
										catch { }
									});
								}
								else {
									InitializeExtension(extensionType);
								}
								SetReportType(wiimoteState.ReportType,
									wiimoteState.IRState.Sensitivity,
									wiimoteState.ContinuousReport);
							}
							else if (extensionLast) {
								wiimoteState.ExtensionType = ExtensionType.None;
								wiimoteState.Nunchuk = new NunchukState();
								wiimoteState.ClassicController = new ClassicControllerState();
								RaiseExtensionChanged(extensionTypeLast, false);
								SetReportType(wiimoteState.ReportType,
									wiimoteState.IRState.Sensitivity,
									wiimoteState.ContinuousReport);
							}
						}
					}
					statusDone.Set();
					//Respond(OutputReport.Status, true);
					break;
				case InputReport.ReadData:
						Log.Debug("******** READ DATA ********");
					ParseButtons2(buff, 1);
					ParseReadData(buff);
					break;
				case InputReport.AcknowledgeOutputReport:
						Log.Debug("******** ACKNOWLEDGE ********");
					ParseButtons2(buff, 1);
					OutputReport outputType = (OutputReport) buff[3];
					WriteResult result = (WriteResult) buff[4];
					if (outputType == OutputReport.WriteMemory) {
						writeDone.Set();
							Log.Debug("Write done");
					}
					//Acknowledge(outputType, result);
					break;
				default:
						Log.Warning($"Unknown input report: {type}");
					break;
				}
			}
			//}
			//catch (TimeoutException) { }
			return true;
		}

		/// <summary>
		/// Handles setting up an extension when plugged in
		/// </summary>
		private void InitializeExtension(byte extensionType) {
			Log.Debug("InitExtension");

			// [FIX V15] RESTORE GUARD: Writing 0x55 to 0xA400F0 is the MotionPlus DEACTIVATION command (WiiBrew).
			// We MUST NOT send it when MP is active (extensionType 0x04/0x05), or it will deactivate the MP
			// we just activated. V14 removed this guard causing MP to be deactivated immediately.
			// The Nunchuk is initialized/decrypted BEFORE MP activation in EnableMotionPlus instead.
			// (EN: Skip init writes when MP is active to avoid deactivating it)
			// (FR: Ne pas écrire l'init quand le MP est actif pour éviter de le désactiver)
			if (extensionType != 0x04 && extensionType != 0x05) {
				WriteByte(Registers.ExtensionInit1, 0x55);
				WriteByte(Registers.ExtensionInit2, 0x00);
			}

			// start reading again
			byte[] buff = ReadData(Registers.ExtensionType1, 6);
			long type = ((long) buff[0] << 40) | ((long) buff[1] << 32) | ((long) buff[2]) << 24 | ((long) buff[3]) << 16 | ((long) buff[4]) << 8 | buff[5];
			// EN: Mask out byte 0 (bits 40-47) which can differ on TR models (0x01 prefix instead of 0x00)
			// FR: Masquer l'octet 0 (bits 40-47) qui peut différer sur les modèles TR (préfixe 0x01 au lieu de 0x00)
			long cleanType = type & 0x000000FFFFFFFFFFL;

			switch ((ExtensionType) cleanType) {
			case ExtensionType.None:
			case ExtensionType.ParitallyInserted:
				wiimoteState.Extension = false;
				wiimoteState.ExtensionType = ExtensionType.None;
				return;
			case ExtensionType.Nunchuk:
                // [FIX] If we detect a Nunchuk ID, but we know MotionPlus Passthrough is active or MP hardware is detected (extensionType 0x05)
                // we MUST treat it as MotionPlusNunchuk to ensure correct parsing.
                if (wiimoteState.MotionPlus.ExtensionType == MotionPlusExtensionType.Nunchuk || extensionType == 0x05)
                {
                    Log.Info($"[InitializeExtension] Override: Nunchuk ID detected ({type:x12}) but MP Passthrough active -> Force MotionPlusNunchuk");
                    wiimoteState.ExtensionType = ExtensionType.MotionPlusNunchuk;
                }
                else
                {
				    wiimoteState.ExtensionType = (ExtensionType) cleanType;
                }
				wiimoteState.Extension = true; // Ensure manager sees it (EN/FR: S'assurer que le manager le voit)
				break;
			case ExtensionType.ClassicController:
			case ExtensionType.MotionPlus:
			case ExtensionType.MotionPlusNunchuk:
				wiimoteState.ExtensionType = (ExtensionType) cleanType;
				wiimoteState.Extension = true; // Ensure manager sees it (EN/FR: S'assurer que le manager le voit)
				break;
			default:
				// EN: If no physical extension is attached according to status, set None
				// FR: Si aucune extension physique n'est attachée selon le statut, mettre None
				if (!wiimoteState.Status.Extension)
				{
					Log.Info($"[InitializeExtension] Unknown extension ID: {type:x12}, but Status.Extension is false -> ExtensionType.None");
					wiimoteState.Extension = false;
					wiimoteState.ExtensionType = ExtensionType.None;
					return;
				}
				// Workaround: Treat unknown extensions as Nunchuk ONLY if an extension is physically connected
				// (EN/FR: Traiter les extensions inconnues comme Nunchuk UNIQUEMENT si une extension est physiquement branchée)
				Log.Warning($"Unknown extension controller found: {type:x12}, treating as Nunchuk");
				wiimoteState.ExtensionType = ExtensionType.Nunchuk;
				break;
			}

			switch (wiimoteState.ExtensionType) {
			case ExtensionType.Nunchuk:
				buff = ReadData(Registers.ExtensionCalibration, 16);

				wiimoteState.Nunchuk.CalibrationInfo.Parse(buff, 0);
				ValidateNunchukCalibration();

					Log.Debug("Nunchuk Calibration:");
				var calib = wiimoteState.Nunchuk.CalibrationInfo;
					Log.Debug($"Max={calib.Max} Min={calib.Min} Mid={calib.Mid}");
				break;
			case ExtensionType.ClassicController:
				buff = ReadData(Registers.ExtensionCalibration, 16);

				wiimoteState.ClassicController.CalibrationInfo.MaxXL = (byte) (buff[0] >> 2);
				wiimoteState.ClassicController.CalibrationInfo.MinXL = (byte) (buff[1] >> 2);
				wiimoteState.ClassicController.CalibrationInfo.MidXL = (byte) (buff[2] >> 2);
				wiimoteState.ClassicController.CalibrationInfo.MaxYL = (byte) (buff[3] >> 2);
				wiimoteState.ClassicController.CalibrationInfo.MinYL = (byte) (buff[4] >> 2);
				wiimoteState.ClassicController.CalibrationInfo.MidYL = (byte) (buff[5] >> 2);

				wiimoteState.ClassicController.CalibrationInfo.MaxXR = (byte) (buff[6] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MinXR = (byte) (buff[7] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MidXR = (byte) (buff[8] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MaxYR = (byte) (buff[9] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MinYR = (byte) (buff[10] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MidYR = (byte) (buff[11] >> 3);

				// this doesn't seem right...
				//					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MinTriggerL = (byte)(buff[12] >> 3);
				//					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MaxTriggerL = (byte)(buff[14] >> 3);
				//					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MinTriggerR = (byte)(buff[13] >> 3);
				//					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MaxTriggerR = (byte)(buff[15] >> 3);
				wiimoteState.ClassicController.CalibrationInfo.MinTriggerL = 0;
				wiimoteState.ClassicController.CalibrationInfo.MaxTriggerL = 31;
				wiimoteState.ClassicController.CalibrationInfo.MinTriggerR = 0;
				wiimoteState.ClassicController.CalibrationInfo.MaxTriggerR = 31;
				break;
			case ExtensionType.MotionPlusOther:
                // [FIX V22h] EN: Same issue as MotionPlusNunchuk — 0xA40040 returns MP data when MP is active.
                // FR: Même problème que MotionPlusNunchuk — 0xA40040 retourne des données MP quand le MP est actif.
                ApplyDefaultNunchukCalibration();

				goto case ExtensionType.MotionPlus;
			case ExtensionType.MotionPlusNunchuk:
				// [FIX V22h] EN: Cannot read Nunchuk calibration from 0xA40040 (PassthroughCalibration) when MP is active.
				// The address space 0xA400xx is remapped to MotionPlus — reads return corrupt MP register data.
				// This was the SECOND code path overwriting the defaults set by EnableMotionPlus.
				// FR: Impossible de lire la calibration Nunchuk à 0xA40040 quand le MP est actif.
				// L'espace 0xA400xx est remappé au MP — les lectures retournent des données corrompues.
				// C'était le SECOND chemin qui écrasait les défauts définis par EnableMotionPlus.
				ApplyDefaultNunchukCalibration();

				goto case ExtensionType.MotionPlus;
			case ExtensionType.MotionPlus:
				// Doesn't do anything yet
				buff = ReadData(Registers.ExtensionCalibration, 32);
				wiimoteState.MotionPlus.CalibrationInfo.Parse(buff, 0);
				break;
			}
			Log.Debug(wiimoteState.ExtensionType.ToString());
			RaiseExtensionChanged(wiimoteState.ExtensionType, true);
		}
		private void ParseStatus2(byte[] buff, int off) {
			wiimoteState.Status.Parse(buff, off);
		}

		private void ParseButtons2(byte[] buff, int off) {
			wiimoteState.Buttons.Parse(buff, off);
		}

		private void ParseAccel2(byte[] buff, int off) {
			wiimoteState.Accel.ParseWiimote(buff, off, wiimoteState.AccelCalibrationInfo);
		}

		private void ParseAccelInterleaved2(byte[] buffA, byte[] buffB, int offA, int offB) {
			wiimoteState.Accel.ParseWiimoteInterleaved(
				buffA, buffB, offA, offB, wiimoteState.AccelCalibrationInfo);
		}

		private void ParseIR2(byte[] buff, int off, int size) {
			wiimoteState.IRState.IRSensor0.RawPosition.X = buff[off + 0] | ((buff[off + 2] >> 4) & 0x03) << 8;
			wiimoteState.IRState.IRSensor0.RawPosition.Y = buff[off + 1] | ((buff[off + 2] >> 6) & 0x03) << 8;

			switch (wiimoteState.IRState.Mode) {
			case IRMode.Basic:
				wiimoteState.IRState.IRSensor1.RawPosition.X = buff[off + 3] | ((buff[off + 2] >> 0) & 0x03) << 8;
				wiimoteState.IRState.IRSensor1.RawPosition.Y = buff[off + 4] | ((buff[off + 2] >> 2) & 0x03) << 8;

				wiimoteState.IRState.IRSensor2.RawPosition.X = buff[off + 5] | ((buff[off + 7] >> 4) & 0x03) << 8;
				wiimoteState.IRState.IRSensor2.RawPosition.Y = buff[off + 6] | ((buff[off + 7] >> 6) & 0x03) << 8;

				wiimoteState.IRState.IRSensor3.RawPosition.X = buff[off + 8] | ((buff[off + 7] >> 0) & 0x03) << 8;
				wiimoteState.IRState.IRSensor3.RawPosition.Y = buff[off + 9] | ((buff[off + 7] >> 2) & 0x03) << 8;

				wiimoteState.IRState.IRSensor0.Size = 0x00;
				wiimoteState.IRState.IRSensor1.Size = 0x00;
				wiimoteState.IRState.IRSensor2.Size = 0x00;
				wiimoteState.IRState.IRSensor3.Size = 0x00;

				wiimoteState.IRState.IRSensor0.Found = !(buff[off + 0] == 0xff && buff[off + 1] == 0xff);
				wiimoteState.IRState.IRSensor1.Found = !(buff[off + 3] == 0xff && buff[off + 4] == 0xff);
				wiimoteState.IRState.IRSensor2.Found = !(buff[off + 5] == 0xff && buff[off + 6] == 0xff);
				wiimoteState.IRState.IRSensor3.Found = !(buff[off + 8] == 0xff && buff[off + 9] == 0xff);
				break;
			case IRMode.Extended:
				wiimoteState.IRState.IRSensor1.RawPosition.X = buff[off + 3] | ((buff[off + 5] >> 4) & 0x03) << 8;
				wiimoteState.IRState.IRSensor1.RawPosition.Y = buff[off + 4] | ((buff[off + 5] >> 6) & 0x03) << 8;
				wiimoteState.IRState.IRSensor2.RawPosition.X = buff[off + 6] | ((buff[off + 8] >> 4) & 0x03) << 8;
				wiimoteState.IRState.IRSensor2.RawPosition.Y = buff[off + 7] | ((buff[off + 8] >> 6) & 0x03) << 8;
				wiimoteState.IRState.IRSensor3.RawPosition.X = buff[off + 9] | ((buff[off + 11] >> 4) & 0x03) << 8;
				wiimoteState.IRState.IRSensor3.RawPosition.Y = buff[off + 10] | ((buff[off + 11] >> 6) & 0x03) << 8;

				wiimoteState.IRState.IRSensor0.Size = buff[off + 2] & 0x0f;
				wiimoteState.IRState.IRSensor1.Size = buff[off + 5] & 0x0f;
				wiimoteState.IRState.IRSensor2.Size = buff[off + 8] & 0x0f;
				wiimoteState.IRState.IRSensor3.Size = buff[off + 11] & 0x0f;

				wiimoteState.IRState.IRSensor0.Found = !(buff[off + 0] == 0xff && buff[off + 1] == 0xff && buff[off + 2] == 0xff);
				wiimoteState.IRState.IRSensor1.Found = !(buff[off + 3] == 0xff && buff[off + 4] == 0xff && buff[off + 5] == 0xff);
				wiimoteState.IRState.IRSensor2.Found = !(buff[off + 6] == 0xff && buff[off + 7] == 0xff && buff[off + 8] == 0xff);
				wiimoteState.IRState.IRSensor3.Found = !(buff[off + 9] == 0xff && buff[off + 10] == 0xff && buff[off + 11] == 0xff);
				break;
			}

			wiimoteState.IRState.IRSensor0.Position.X = (float) (wiimoteState.IRState.IRSensor0.RawPosition.X / 1023.5f);
			wiimoteState.IRState.IRSensor1.Position.X = (float) (wiimoteState.IRState.IRSensor1.RawPosition.X / 1023.5f);
			wiimoteState.IRState.IRSensor2.Position.X = (float) (wiimoteState.IRState.IRSensor2.RawPosition.X / 1023.5f);
			wiimoteState.IRState.IRSensor3.Position.X = (float) (wiimoteState.IRState.IRSensor3.RawPosition.X / 1023.5f);

			wiimoteState.IRState.IRSensor0.Position.Y = (float) (wiimoteState.IRState.IRSensor0.RawPosition.Y / 767.5f);
			wiimoteState.IRState.IRSensor1.Position.Y = (float) (wiimoteState.IRState.IRSensor1.RawPosition.Y / 767.5f);
			wiimoteState.IRState.IRSensor2.Position.Y = (float) (wiimoteState.IRState.IRSensor2.RawPosition.Y / 767.5f);
			wiimoteState.IRState.IRSensor3.Position.Y = (float) (wiimoteState.IRState.IRSensor3.RawPosition.Y / 767.5f);

			if (wiimoteState.IRState.IRSensor0.Found && wiimoteState.IRState.IRSensor1.Found) {
				wiimoteState.IRState.RawMidpoint.X = (wiimoteState.IRState.IRSensor1.RawPosition.X + wiimoteState.IRState.IRSensor0.RawPosition.X) / 2;
				wiimoteState.IRState.RawMidpoint.Y = (wiimoteState.IRState.IRSensor1.RawPosition.Y + wiimoteState.IRState.IRSensor0.RawPosition.Y) / 2;

				wiimoteState.IRState.Midpoint.X = (wiimoteState.IRState.IRSensor1.Position.X + wiimoteState.IRState.IRSensor0.Position.X) / 2.0f;
				wiimoteState.IRState.Midpoint.Y = (wiimoteState.IRState.IRSensor1.Position.Y + wiimoteState.IRState.IRSensor0.Position.Y) / 2.0f;
			}
			else
				wiimoteState.IRState.Midpoint.X = wiimoteState.IRState.Midpoint.Y = 0.0f;
		}

		private void ParseNunchuk(byte[] buff, int off) {
			wiimoteState.Nunchuk.Parse(buff, off, false);
		}

		private void ParseClassicController(byte[] buff, int off) {
			//mWiimoteState.ClassicControllerState.Parse(buff, off, false);
		}

		private void ParseMotionPlus(byte[] buff, int off) {
			if (wiimoteState.ExtensionType == ExtensionType.MotionPlus) {
				// EN: Standalone MotionPlus mode (0x04) — parse full resolution gyro
				// FR: Mode MotionPlus standalone (0x04) — parser le gyro pleine résolution
				wiimoteState.MotionPlus.Parse(buff, off, false);

				// EN: WiiBrew: Byte 5 bit 0 is 1 if an extension is plugged into the MotionPlus
				// FR: WiiBrew: L'octet 5 bit 0 vaut 1 si une extension est branchée dans le MotionPlus
				if (buff.GetBit(off + 5, 0)) {
					if (!_extensionChangePending) {
						_extensionChangePending = true;
						System.Threading.Tasks.Task.Run(() => {
							try {
								System.Threading.Thread.Sleep(100);
								GetStatus();
							}
							catch { }
							finally { _extensionChangePending = false; }
						});
					}
				}
				return;
			}

            // [FIX V16] WiiBrew: "Bit 1 of Byte 5 is used to determine which type of report is received:
            //   it is 1 when it contains MotionPlus Data and 0 when it contains extension data."
            // (EN: Bit1=1 → MotionPlus (gyro), Bit1=0 → Extension (Nunchuk))
            // (FR: Bit1=1 → MotionPlus (gyro), Bit1=0 → Extension (Nunchuk))
            bool isMotionPlusData = buff.GetBit(off + 5, 1);  // Bit 1 = 1 -> MotionPlus gyro data
            bool isExtensionData = !isMotionPlusData;          // Bit 1 = 0 -> Extension (Nunchuk) data

            // [DIAGNOSTIC] Log frame type occasionally (limit to avoid flooding)
            if (DateTime.Now.Millisecond < 10) {
                if (isExtensionData)
                    Log.Debug($"[DIAGNOSTIC] Ext Frame (Nunchuk): Byte5={buff[off + 5]:X2} (Bit1=0)");
                else
                    Log.Debug($"[DIAGNOSTIC] MP Frame (Gyro): Byte5={buff[off + 5]:X2} (Bit1=1)");
            }

			if (isMotionPlusData) {
                // EN: Parse as MotionPlus passthrough format (0x05 reduced resolution)
                // FR: Parser en format passthrough MotionPlus (0x05 résolution réduite)
				wiimoteState.MotionPlus.Parse(buff, off, true);
                // EN: Nunchuk state persists (no reset needed)
                // FR: L'état du Nunchuk persiste (pas de reset nécessaire)
			}
			else {
				// EN: Extension data frame — parse Nunchuk in passthrough mode
				// FR: Frame de données extension — parser le Nunchuk en mode passthrough
				wiimoteState.Nunchuk.Parse(buff, off, true);
			}
		}

		private void ParseExtension2(byte[] buff, int off, int size) {
			switch (wiimoteState.ExtensionType) {
			case ExtensionType.Nunchuk:
				ParseNunchuk(buff, off);
				break;
			case ExtensionType.ClassicController:
				ParseClassicController(buff, off);
				break;
			case ExtensionType.MotionPlus:
			case ExtensionType.MotionPlusNunchuk:
			case ExtensionType.MotionPlusOther:
				ParseMotionPlus(buff, off);
				break;
			}
		}

		private void ParseIRInterleaved2(byte[] buffA, byte[] buffB, int offA, int offB) {

		}

		private void ValidateNunchukCalibration() {
			// Check if calibration data is valid, if not use defaults (EN/FR: Vérifier si la calibration est valide, sinon utiliser les valeurs par défaut)
			if (wiimoteState.Nunchuk.CalibrationInfo.Max.X == 0 && wiimoteState.Nunchuk.CalibrationInfo.Max.Y == 0 &&
				wiimoteState.Nunchuk.CalibrationInfo.Min.X == 0 && wiimoteState.Nunchuk.CalibrationInfo.Min.Y == 0) {
				Log.Warning("Invalid Nunchuk calibration detected (all zeros), using default values");
				wiimoteState.Nunchuk.CalibrationInfo.Max.X = 228;
				wiimoteState.Nunchuk.CalibrationInfo.Max.Y = 228;
				wiimoteState.Nunchuk.CalibrationInfo.Min.X = 35;
				wiimoteState.Nunchuk.CalibrationInfo.Min.Y = 35;
				wiimoteState.Nunchuk.CalibrationInfo.Mid.X = 130;
				wiimoteState.Nunchuk.CalibrationInfo.Mid.Y = 129;
			}
		}
	}
}
