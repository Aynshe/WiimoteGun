using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiimoteLib.Devices;
using WiimoteLib.Util;

namespace WiimoteLib {
	public static partial class WiimoteManager {
		
		// EN: Progressive blocklist for HID paths that failed to connect (DolphinBar phantom ports)
		// FR: Liste de blocage PROGRESSIVE pour les chemins HID qui ont échoué la connexion (ports fantômes DolphinBar)
		// Key = HID device path, Value = failure state (count + blocked until)
		private class HidPathBlock
		{
			public int FailureCount;
			public DateTime BlockedUntil;
		}
		private static readonly Dictionary<string, HidPathBlock> _failedHidPaths = new Dictionary<string, HidPathBlock>();
		// [V61-FIX-13] EN: HID paths where a Wiimote has connected at least once ("proven"
		// ports). A proven port can host a Wiimote → it must NEVER be blocklisted (the
		// user may come back with the same or another Wiimote on that port).
		// FR: Chemins HID où une Wiimote s'est connectée au moins une fois (ports
		// « prouvés »). Un port prouvé peut héberger une Wiimote → il ne doit JAMAIS
		// être bloqué (l'utilisateur peut revenir avec la même ou une autre Wiimote).
		private static readonly HashSet<string> _provenHidPaths = new HashSet<string>();
		private static readonly object _failedHidLock = new object();

		/// <summary>
		/// [V61-FIX-11] EN: Progressive block durations: 5s, 30s, 60s repeating — NEVER
		///     session-long. A session-long tier blocked the REAL port too: each time the
		///     user turns the Wiimote off, that port fails the handshake while empty and
		///     climbs the ladder, ending permanently blocked → Wiimote reconnects to the
		///     bar but WiimoteGun never detects it (user report). The 60s repeat tier
		///     quiets phantom ports to at most 1 attempt/min while guaranteeing detection
		///     of a returned Wiimote within ≤60s; a Windows device-change event (fired by
		///     the Mayflash bar on each Wiimote sync) clears the whole blocklist for a
		///     fast reconnect (ResetHidBlocklist).
		///     FR: Durées de blocage PROGRESSIVES : 5s, 30s, 60s répétés — JAMAIS la fin
		///     de session. Le palier « session » bloquait AUSSI le vrai port : à chaque
		///     extinction de la Wiimote, ce port échouait le handshake (vide) et gravissait
		///     l'échelle jusqu'au blocage permanent → la Wiimote se reconnectait à la
		///     barre mais WiimoteGun ne la détectait jamais (rapport user). Le palier 60s
		///     répété réduit les ports fantômes à 1 tentative/minute maximum tout en
		///     garantissant la détection d'une Wiimote revenue sous ≤60s ; un événement
		///     Windows de changement de périphérique (émis par la barre Mayflash à chaque
		///     synchro) vide toute la blocklist pour une reconnexion rapide
		///     (ResetHidBlocklist).
		/// </summary>
		private static int GetFailedHidBlockSeconds(int failureCount)
		{
			if (failureCount >= 3) return 60; // 60s repeating tier (never session-long)
			if (failureCount == 2) return 30;  // 30 seconds
			return 5;                          // first failure: 5 seconds
		}

		/// <summary>
		/// EN: [V61-FIX-11] Clears the HID blocklist (all ports). Called on any Windows
		///     device-change event: the Mayflash bar fires one on each Wiimote
		///     sync/unsync, so a reconnected Wiimote is discovered on the very next
		///     discovery round instead of waiting for the phantom port's block to expire.
		///     FR: [V61-FIX-11] Vide la blocklist HID (tous les ports). Appelé à chaque
		///     événement Windows de changement de périphérique : la barre Mayflash en émet
		///     un à chaque synchro/dé-synchro Wiimote, donc une Wiimote reconnectée est
		///     découverte au tour de discovery suivant au lieu d'attendre l'expiration du
		///     blocage du port fantôme.
		/// </summary>
		public static void ResetHidBlocklist()
		{
			int cleared;
			lock (_failedHidLock) {
				cleared = _failedHidPaths.Count;
				_failedHidPaths.Clear();
			}
			if (cleared > 0)
				Log.Info($"HID blocklist cleared ({cleared} path(s)) - device change detected");
		}



		public static void StartDiscovery() {
			// Discovery also handles idle functionality
			if (IsInIdleMode)
				StopIdle();

			lock (taskLock) {
				if (!IsInDiscoveryMode) {
					Log.Info("Discovery Mode: Start");
					
					// EN: Clear failed HID paths blocklist on new discovery session
					// FR: Vider la liste de blocage des chemins HID échoués pour une nouvelle session de découverte
					lock (_failedHidLock) {
						_failedHidPaths.Clear();
					}
					
					discoverToken = new CancellationTokenSource();
					CancellationToken token = discoverToken.Token;
					discoverTask = Task.Run(() => DiscoverTask(token), token);
				}
			}
		}

		public static void StopDiscovery() {
			lock (taskLock) {
				if (IsInDiscoveryMode) {
					Log.Info("Discovery Mode: Stop");
					discoverToken?.Cancel();
					discoverToken = null;
					discoverTask = null;
				}
			}

			// Start idle if Wiimotes are connected
			if (wiimotes.Any() && !IsInIdleMode)
				StartIdle();
		}

		private static void StartIdle() {
			if (IsInDiscoveryMode)
				StopDiscovery();

			lock (taskLock) {
				// Only idle if Wiimotes are connected
				if (wiimotes.Any() && !IsInIdleMode) {
					Log.Info("Idle Mode: Start");
					idleToken = new CancellationTokenSource();
					CancellationToken token = idleToken.Token;
					idleTask = Task.Run(() => IdleTask(token), token);
				}
			}
		}

		private static void StopIdle() {
			lock (taskLock) {
				if (IsInIdleMode) {
					Log.Info("Idle Mode: Stop");
					idleToken?.Cancel();
					idleToken = null;
					idleTask = null;
				}
			}
		}

		private static void StartWrite() {
			lock (taskLock) {
				if (!IsInWriteMode) {
					Log.Info("Write Mode: Start");
					writeToken = new CancellationTokenSource();
					CancellationToken token = writeToken.Token;
					writeTask = Task.Run(() => WriteTask(token), token);
				}
			}
		}
		private static void StopWrite() {
			lock (taskLock) {
				if (IsInWriteMode) {
					Log.Info("Write Mode: Stop");
					writeToken?.Cancel();
					writeToken = null;
					writeTask = null;
				}
			}
		}

		private static void StopAllTasks() {
			lock (taskLock) {
				if (IsInDiscoveryMode) {
					Log.Info("Discovery Mode: Stop");
					discoverToken?.Cancel();
					discoverToken = null;
					discoverTask = null;
				}
				else if (IsInIdleMode) {
					Log.Info("Idle Mode: Stop");
					idleToken?.Cancel();
					idleToken = null;
					idleTask = null;
				}
				if (IsInWriteMode) {
					Log.Info("Write Mode: Stop");
					writeToken?.Cancel();
					writeToken = null;
					writeTask = null;
				}
			}
		}

		private static void UpdateTaskMode() {
			if (WiimoteCount < autoDiscoveryCount) {
				if (!IsInDiscoveryMode)
					StartDiscovery();
			}
			else if (autoDiscoveryCount > 0) {
				if (IsInDiscoveryMode)
					StopDiscovery();
			}
			else if (WiimoteCount > 0) {
				StartIdle();
			}
			else {
				StopIdle();
			}
		}


		private static void DiscoverTask(CancellationToken token) {
			while (!token.IsCancellationRequested) {
				if (!DiscoverLoop(token))
					break;
			}
			Log.Info("Discover Mode: End");
		}

		private static bool DiscoverLoop(CancellationToken token) {

			bool result = true;

			if (DolphinBarMode && BluetoothMode)
			{
				Action dolphinBar = () => { if (DolphinBarMode) result &= HIDDiscoverLoop(token); };
				Action bluetooth = () => { if (BluetoothMode) result &= BluetoothDiscoverLoop(token); };

				Parallel.ForEach(new Action[] { dolphinBar, bluetooth }, a => a());
			}
			else
			{
				if (DolphinBarMode)
					result &= HIDDiscoverLoop(token);
				if (BluetoothMode)
					result &= BluetoothDiscoverLoop(token);
			}

			if (result)
			{
				// Dynamic delay logic to reduce lag when Wiimotes are connected
				int delay = retryDiscoverDelay;
				
				if (WiimoteCount > 0)
				{
					// If we have connected Wiimotes, check how long since the first connection
					TimeSpan timeSinceFirstConnection = DateTime.Now - _firstConnectionTime;
					
					if (timeSinceFirstConnection.TotalSeconds > 15)
					{
						// After 15 seconds of stable connection, slow down discovery significantly
						// This reduces CPU/Radio usage and prevents tracking lag
						delay = 5000; // 5 seconds
					}
				}
				
				token.Sleep(delay);
			}

			GC.Collect();

			return result;
		}

		private static bool HIDDiscoverLoop(CancellationToken token) {
			var hids = HIDDeviceInfo.EnumerateDevices(token, MatchHID);
			foreach (HIDDeviceInfo hid in hids) {
				if (token.IsCancellationRequested)
					return false;
				
				// EN: Skip HID paths that are in the PROGRESSIVE blocklist (DolphinBar phantom ports)
				// FR: Ignorer les chemins HID dans la liste de blocage PROGRESSIVE (ports fantômes DolphinBar)
				lock (_failedHidLock) {
					HidPathBlock block;
					if (_failedHidPaths.TryGetValue(hid.DevicePath, out block)) {
						// [V61-FIX-13] EN: NO block while zero Wiimote are connected (hunt phase:
						//     every port must be tried on each discovery round so a Wiimote
						//     connects IMMEDIATELY) and NO block on a PROVEN port (a Wiimote
						//     already connected here once, the user may come back with another).
						//     FR: AUCUN blocage quand aucune Wiimote n'est connectée (recherche :
						//     chaque port tenté à chaque tour pour une connexion IMMÉDIATE) et
						//     AUCUN blocage sur un port PROUVÉ (une Wiimote s'y est déjà
						//     connectée, l'utilisateur peut revenir avec une autre).
						bool huntPhase = WiimoteCount == 0;
						bool proven = _provenHidPaths.Contains(hid.DevicePath);
						if (!huntPhase && !proven &&
							(block.BlockedUntil == DateTime.MaxValue || DateTime.Now < block.BlockedUntil)) {
							// Still blocked, skip this device
							continue;
						}
						// Block expired or ignored - retry now (the failure count is KEPT: the next failure escalates)
						// (EN/FR: Blocage expiré ou ignoré - réessai (le compteur d'échecs est CONSERVÉ : le prochain échec escalade))
					}
				}
				
				Wiimote wiimote = null;
				lock (wiimotes) {
					wiimote = wiimotes.Find(wm => wm.DevicePath == hid.DevicePath);
				}

				if (wiimote == null) {
					if (autoConnect) {
						//FIXME: Handle BOTH Bluetooth and DolphinBarMode more gracefully.
						WiimoteDeviceInfo wiimoteDevice = null;
						try {
							// Try to resolve as a Bluetooth device first to get the real address
							wiimoteDevice = new WiimoteDeviceInfo(hid, false);
						} catch {
							// Fallback to DolphinBar mode (dummy address) if enabled
							if (DolphinBarMode)
								wiimoteDevice = new WiimoteDeviceInfo(hid, true);
						}

						if (wiimoteDevice != null) {
										try {
											Connect(wiimoteDevice);
											// EN: Connection successful: remove from the blocklist and mark the
											// port as PROVEN - it can host a Wiimote, so it must never be
											// blocklisted again (reconnect / next Wiimote on that port).
											// FR: Connexion réussie : retirer de la liste de blocage et marquer le
											// port comme PROUVÉ - il peut héberger une Wiimote, il ne doit plus
											// jamais être bloqué (reconnexion / Wiimote suivante sur ce port).
											lock (_failedHidLock) {
												_failedHidPaths.Remove(hid.DevicePath);
												_provenHidPaths.Add(hid.DevicePath);
											}
										}
							catch (Exception ex) {
								// [V61-FIX-13] EN: a PROVEN port is never blocklisted (only noise-logged):
								//     it can host a Wiimote, the user may come back with another.
								//     FR: un port PROUVÉ n'est jamais bloqué (seulement loggé) : il peut
								//     héberger une Wiimote, l'utilisateur peut revenir avec une autre.
								lock (_failedHidLock) {
									if (_provenHidPaths.Contains(hid.DevicePath)) {
										Log.Debug($"HID device failed (proven port, not blocked): {hid.DevicePath}");
									}
									else {
										// EN: Connection failed - add to the PROGRESSIVE blocklist (5s -> 30s -> 60s)
										// FR: Connexion échouée - ajouter à la liste de blocage PROGRESSIVE (5s -> 30s -> 60s)
										int failureCount = 1;
										HidPathBlock existing;
										if (_failedHidPaths.TryGetValue(hid.DevicePath, out existing))
											failureCount = existing.FailureCount + 1;
										int seconds = GetFailedHidBlockSeconds(failureCount);
										DateTime blockedUntil = seconds < 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(seconds);
										_failedHidPaths[hid.DevicePath] = new HidPathBlock { FailureCount = failureCount, BlockedUntil = blockedUntil };
										Log.Debug($"HID device blocked for {(seconds < 0 ? "the rest of the session" : seconds.ToString() + "s")} (failure #{failureCount}): {hid.DevicePath}");
									}
								}
								RaiseConnectionFailed(wiimoteDevice, ex);
							}
						}
					}
					else if (!RaiseDiscovered(null, hid)) {
						return false;
					}
				}
			}
			
			//token.Sleep(350);
			return true;
		}

		// [V53] Consecutive instant-empty inquiry rounds (radio wedge detection)
		// (EN/FR: Tours d'inquiry instantanés-vides consécutifs (détection radio coincée))
		private static int _btWedgeSuspicions = 0;

		// [V53b] Consecutive pathologically SLOW inquiries (degraded stack detection)
		// (EN/FR: Inquiry consécutifs pathologiquement LENTS (détection pile dégradée))
		private static int _btSlowInquiries = 0;

		/// <summary>
		/// EN: [V55v] Public read access to the radio-wedge suspicion counter (8+ = wedged).
		/// Used by the client-side BT watchdog to justify a reset ONLY on real evidence.
		/// FR: [V55v] Accès public en lecture au compteur de soupçons de radio coincée
		/// (8+ = coincée). Utilisé par le watchdog BT côté client pour ne justifier un
		/// reset QUE sur des preuves réelles.
		/// </summary>
		public static int BtWedgeSuspicions
		{
			get { return _btWedgeSuspicions; }
		}

		/// <summary>
		/// EN: [V55v] Public read access to the consecutive slow-inquiry counter (3+ = degraded stack).
		/// FR: [V55v] Accès public en lecture au compteur d'inquiry lents consécutifs (3+ = pile dégradée).
		/// </summary>
		public static int BtSlowInquiries
		{
			get { return _btSlowInquiries; }
		}

		private static bool BluetoothDiscoverLoop(CancellationToken token)
		{
			HashSet<BluetoothAddress> missingDevices = new HashSet<BluetoothAddress>(ConnectedAddresses);
			var devices = BluetoothDeviceInfo.EnumerateDevices(token, MatchBluetooth);
			Stopwatch watch = Stopwatch.StartNew();
			bool anyPaired = false;
			int foundCount = 0;
			foreach (BluetoothDeviceInfo device in devices)
			{
				if (token.IsCancellationRequested)
					return false;

				foundCount++;

				Wiimote wiimote = null;
				lock (wiimotes)
					wiimote = wiimotes.Find(wm => wm.Address == device.Address);

				if (device.Connected)
				{
					if (wiimote != null)
					{
						// Give Wiimote the updated Bluetooth device
						wiimote.Device.Bluetooth = device;
						missingDevices.Remove(device.Address);
					}
					else
					{
						HIDDeviceInfo hid = HIDDeviceInfo.GetDevice(device.Address);
						// Drivers must not be installed yet, let's wait a bit
						if (hid != null)
						{
							if (autoConnect)
							{
								WiimoteDeviceInfo wiimoteDevice = new WiimoteDeviceInfo(device, hid);
								try
								{
									Connect(wiimoteDevice);
								}
								catch (Exception ex)
								{
									RaiseConnectionFailed(wiimoteDevice, ex);
								}
							}
							else if (!RaiseDiscovered(device, hid))
							{
								return false;
							}
						}
						/*else if (device.PairDevice(token)) {
							anyPaired = true;
						}
						else {
							Log.WriteLine("{device} pair failed!");
						}*/
					}
				}
				else
				{
					if (wiimote != null)
					{
						lock (wiimotes)
						{
							wiimote.Dispose();
							wiimotes.Remove(wiimote);
							RaiseDisconnected(wiimote, DisconnectReason.ConnectionLost);
						}
					}
					else if (/*device.IsDiscoverable() || */!device.Remembered)
					{
						if (pairOnDiscover)
						{
							if (device.PairDevice(token))
							{
								anyPaired = true;
							}
							else
							{
								Log.Warning("{device} pair failed!");
							}
						}
					}
					else if (device.Remembered && unpairOnDisconnect)
					{
						device.RemoveDevice(token);
					}
			}

			// [V53] Enumeration summary: once per round (not once per device)
			// (EN/FR: Résumé d'énumération : une fois par tour (pas par périphérique))
			Log.Debug($"Took {watch.ElapsedMilliseconds}ms to enumerate bluetooth device(s) - {foundCount} found");

			// [V53] Radio wedge watchdog: a WEDGED Microsoft Bluetooth stack returns from
			// the inquiry INSTANTLY (<300ms) with ZERO device, while a healthy inquiry
			// takes ~1.3s even with no Wiimote in range. 8 consecutive suspicious rounds
			// = the radio is wedged: only a dongle power-cycle (unplug/replug or Device
			// Manager disable/enable) recovers it — restarting Wiimote4Guns does NOT.
			// (EN/FR: Chien de garde radio coincée : une pile Bluetooth Microsoft COINCÉE
			// revient de l'inquiry INSTANTANÉMENT (<300ms) sans AUCUN périphérique, alors
			// qu'un inquiry sain prend ~1,3s même sans wiimote à portée. 8 tours suspects
			// consécutifs = radio coincée : seul un cycle d'alimentation du dongle
			// (débrancher/rebrancher ou désactiver/activer dans le Gestionnaire de
			// périphériques) la rétablit — redémarrer Wiimote4Guns ne sert à RIEN.)
			if (BluetoothDeviceInfo.LastInquiryStartedUtc != DateTime.MinValue &&
			    BluetoothDeviceInfo.LastInquiryDurationMs >= 0 &&
			    BluetoothDeviceInfo.LastInquiryDurationMs < 300 &&
			    BluetoothDeviceInfo.LastInquiryDeviceCount == 0)
			{
				_btWedgeSuspicions++;
				_btSlowInquiries = 0;
				if (_btWedgeSuspicions == 8)
					Log.Error("[BT Wedge] Bluetooth inquiry returns instantly with no device 8 times in a row: the radio stack appears WEDGED. Unplug/replug the Bluetooth dongle (or disable/enable it in Device Manager) - restarting Wiimote4Guns will NOT fix this.");
				else if (_btWedgeSuspicions > 8 && _btWedgeSuspicions % 32 == 0)
					Log.Warning($"[BT Wedge] Still wedged ({_btWedgeSuspicions} instant empty inquiries). A dongle power-cycle is required.");
			}
			// [V53b] Degraded-stack detection: a SICK stack can still find devices and
			// pair, but its inquiries take a pathological 15s+ (healthy ~1.3s) and the
			// post-pairing HID connection never establishes (the Wiimote pairs, waits,
			// then powers off). Warn the user EARLY: a dongle power-cycle fixes it.
			// (EN/FR: Détection pile dégradée : une pile MALADE peut encore trouver des
			// périphériques et les apparier, mais ses inquiry prennent 15s+ pathologiques
			// (sain ~1,3s) et la connexion HID post-appairage ne s'établit jamais (la
			// wiimote se apparie, attend, puis s'éteint). Prévenir l'utilisateur TÔT :
			// un cycle d'alimentation du dongle corrige.)
			else if (BluetoothDeviceInfo.LastInquiryDurationMs > 15000)
			{
				_btSlowInquiries++;
				_btWedgeSuspicions = 0;
				Log.Warning($"[BT Health] Bluetooth inquiry took {BluetoothDeviceInfo.LastInquiryDurationMs / 1000.0:F1}s (healthy: ~1.3s) - the radio stack is degraded; pairing may succeed but the HID connection will likely fail.");
				if (_btSlowInquiries == 3)
					Log.Error("[BT Health] 3 consecutive slow inquiries: the Bluetooth stack is DEGRADED. Unplug/replug the Bluetooth dongle (or disable/enable it in Device Manager) - restart Wiimote4Guns will NOT fix this.");
			}
			else
			{
				_btWedgeSuspicions = 0;
				_btSlowInquiries = 0;
			}

			watch.Restart();
			}

			token.Sleep(anyPaired ? driverInstallDelay : 0);
			return true;
		}

		private static void IdleTask(CancellationToken token) {
			while (!token.IsCancellationRequested) {
				if (!IdleLoop(token))
					break;
			}
			Log.Debug("Idle Mode: End");
		}

		private static bool IdleLoop(CancellationToken token) {
			//FIXME: Handle BOTH Bluetooth and DolphinBarMode more gracefully.
			//if (DolphinBarMode)
			//	return HIDIdleLoop(token);
			//else
			//	return BluetoothIdleLoop(token);
			bool result = true;
			if (DolphinBarMode)
				result &= HIDIdleLoop(token);
			if (BluetoothMode)
				result &= BluetoothIdleLoop(token);
			return result;
		}

		private static bool HIDIdleLoop(CancellationToken token) {
			var wiimoteList = ConnectedWiimotes;
			if (wiimoteList.Length == 0)
				return false;

			foreach (Wiimote wiimote in wiimoteList) {
				if (token.IsCancellationRequested)
					return false;
				//FIXME: Quick fix to support both Bluetooth and DolphinBar connections.
				//       Notice, that we only continue when in Bluetooth mode, this ensures
				//       Bluetooth devices are handled properly even if they were connected
				//       otherwise.
				if (wiimote.Device.IsBluetooth && BluetoothMode)
					continue;
				try {
#if DEBUG
					wiimote.GetStatus();
#else
					wiimote.GetStatus(800);
#endif
				}
				catch (TimeoutException) {
					// Connection may have been lost
					lock (wiimotes) {
						wiimote.Dispose();
						wiimotes.Remove(wiimote);
						RaiseDisconnected(wiimote, DisconnectReason.ConnectionLost);
					}
				}
			}
			token.Sleep(200);
			return true;
		}

		private static bool BluetoothIdleLoop(CancellationToken token) {
			var wiimoteList = ConnectedWiimotes;
			if (wiimoteList.Length == 0)
				return false;

			foreach (Wiimote wiimote in wiimoteList) {
				if (token.IsCancellationRequested)
					return false;
				//FIXME: Quick fix to support both DolphinBar and Bluetooth connections.
				if (!wiimote.Device.IsBluetooth)
					continue;
				BluetoothDeviceInfo device = wiimote.Device.Bluetooth;
				Stopwatch watch2 = Stopwatch.StartNew();
				device.Refresh();
				// EN: Log commented to reduce log spam during Idle mode
				// FR: Log commenté pour réduire le spam dans le fichier log en mode Idle
				// Log.Debug($"Took {watch2.ElapsedMilliseconds}ms refresh {device}");
				if (!device.Connected) {
					lock (wiimotes) {
						wiimote.Dispose();
						wiimotes.Remove(wiimote);
						RaiseDisconnected(wiimote, DisconnectReason.ConnectionLost);
					}
				}
				token.Sleep(100);
			}
			token.Sleep(1500);
			return true;
		}

		private static void WriteTask(CancellationToken token) {
			while (!token.IsCancellationRequested) {
				WriteLoop(token);
			}
			Log.Debug("Write Mode: End");
		}

		private static void WriteLoop(CancellationToken token) {
			lock (writeQueue) {
				if (writeQueue.Count != 0) {
					WriteRequest request = writeQueue.Dequeue();
					request.Send();
				}
			}
			token.Sleep(maxWriteFrequency);
			if (writeQueue.Count == 0)
				writeReady.WaitOne();
		}

		/*private static void CheckForTimeOuts(IEnumerable<BluetoothAddress> missingDevices,
			CancellationToken token)
		{
			if (DisconnectTimeout == TimeSpan.Zero)
				return;

			foreach (BluetoothAddress address in missingDevices) {
				lock (wiimotes) {
					if (!wiimotes.TryGetValue(address, out WiimoteNew wiimote))
						continue;
					
					if (wiimote.Device.TimeSinceLastSeen >= DisconnectTimeout) {
						wiimote.Dispose();
						wiimotes.Remove(address);
						RaiseDisconnected(wiimote, DisconnectReason.TimedOut);
					}
				}
			}
		}*/
	}
}
