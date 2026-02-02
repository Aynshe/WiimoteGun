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
		
		// EN: Blocklist for HID paths that failed to connect (DolphinBar phantom ports)
		// FR: Liste de blocage pour les chemins HID qui ont échoué la connexion (ports fantômes DolphinBar)
		// Key = HID device path, Value = timestamp when blocked
		private static readonly Dictionary<string, DateTime> _failedHidPaths = new Dictionary<string, DateTime>();
		private static readonly TimeSpan _failedHidBlockDuration = TimeSpan.FromSeconds(5);
		private static readonly object _failedHidLock = new object();



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
				
				// EN: Skip HID paths that are in the blocklist (DolphinBar phantom ports)
				// FR: Ignorer les chemins HID dans la liste de blocage (ports fantômes DolphinBar)
				lock (_failedHidLock) {
					if (_failedHidPaths.TryGetValue(hid.DevicePath, out DateTime blockedTime)) {
						if (DateTime.Now - blockedTime < _failedHidBlockDuration) {
							// Still blocked, skip this device
							continue;
						}
						else {
							// Block expired, remove from list and retry
							_failedHidPaths.Remove(hid.DevicePath);
						}
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
								// EN: Connection successful, ensure not in blocklist
								// FR: Connexion réussie, s'assurer qu'il n'est pas dans la liste de blocage
								lock (_failedHidLock) {
									_failedHidPaths.Remove(hid.DevicePath);
								}
							}
							catch (Exception ex) {
								// EN: Connection failed - add to blocklist to prevent endless retry
								// FR: Connexion échouée - ajouter à la liste de blocage pour éviter les réessais infinis
								lock (_failedHidLock) {
									_failedHidPaths[hid.DevicePath] = DateTime.Now;
									Log.Debug($"HID device blocked for {_failedHidBlockDuration.TotalSeconds}s: {hid.DevicePath}");
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

		private static bool BluetoothDiscoverLoop(CancellationToken token)
		{
			HashSet<BluetoothAddress> missingDevices = new HashSet<BluetoothAddress>(ConnectedAddresses);
			var devices = BluetoothDeviceInfo.EnumerateDevices(token, MatchBluetooth);
			Stopwatch watch = Stopwatch.StartNew();
			bool anyPaired = false;
			foreach (BluetoothDeviceInfo device in devices)
			{
				if (token.IsCancellationRequested)
					return false;

				Log.Debug($"Took {watch.ElapsedMilliseconds}ms to enumerate bluetooth device");

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
