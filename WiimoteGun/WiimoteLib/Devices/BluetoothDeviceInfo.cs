using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiimoteLib.Native;
using WiimoteLib.Util;

namespace WiimoteLib.Devices {
	public class BluetoothDeviceInfo {
		internal BLUETOOTH_DEVICE_INFO DeviceInfo;

		//private DateTime pairingStarted;

		private static DateTime _lastInquiryTime = DateTime.MinValue;

		// [V39] Discovery window state (EN/FR: État de la fenêtre de découverte)
		private static DateTime _discoveryWindowEndUtc = DateTime.MinValue;
		private static int _lastKnownConnectedCount = 0;
		private const double DiscoveryWindowSeconds = 90.0;
		private const int MaxSupportedWiimotes = 4;

		// [V53] Inquiry health tracking: a WEDGED Microsoft Bluetooth stack returns from
		// BluetoothFindFirstDevice(fIssueInquiry=true) INSTANTLY with no device at all,
		// while a healthy inquiry takes ~1.3s even with no Wiimote in range. The
		// discovery loop reads these stats to detect the wedge and warn the user that
		// only a dongle power-cycle recovers it.
		// (EN/FR: Suivi santé inquiry : une pile Bluetooth Microsoft COINCÉE revient de
		// BluetoothFindFirstDevice(fIssueInquiry=true) INSTANTANÉMENT sans aucun
		// périphérique, alors qu'un inquiry sain prend ~1,3s même sans wiimote à portée.
		// La boucle de découverte lit ces stats pour détecter le blocage et prévenir
		// l'utilisateur que seul un cycle d'alimentation du dongle le rétablit.)
		internal static DateTime LastInquiryStartedUtc = DateTime.MinValue;
		internal static double LastInquiryDurationMs = -1.0;
		internal static int LastInquiryDeviceCount = -1;

		public BluetoothAddress Address => new BluetoothAddress(DeviceInfo.Address);
		public bool Connected => DeviceInfo.fConnected;
		public bool Remembered => DeviceInfo.fRemembered;
		public bool Authenticated => DeviceInfo.fAuthenticated;
		public string Name => DeviceInfo.szName;
		public DateTime LastSeen => DeviceInfo.stLastSeen.DateTime;
		public DateTime LastUsed => DeviceInfo.stLastUsed.DateTime;
		public DateTime LastSeenUtc => DeviceInfo.stLastSeen.DateTimeUtc;
		public DateTime LastUsedUtc => DeviceInfo.stLastUsed.DateTimeUtc;

		public bool IsInvalid => DeviceInfo.Address == 0;

		//internal bool IsPairing

		internal BluetoothDeviceInfo() {

		}

		internal BluetoothDeviceInfo(BLUETOOTH_DEVICE_INFO deviceInfo) {
			DeviceInfo = deviceInfo;
		}

		public BluetoothDeviceInfo(long address) {
			DeviceInfo = new BLUETOOTH_DEVICE_INFO(address);
			Refresh();
		}

		public BluetoothDeviceInfo(ulong address)
			: this(unchecked((long) address))
		{
		}

		public BluetoothDeviceInfo(string address)
			: this(BluetoothAddress.Parse(address).Int64)
		{
		}

		public BluetoothDeviceInfo(BluetoothAddress address)
			: this(address.Int64)
		{
		}

		public override string ToString() => $"{Name} ({Address})";

		public bool Refresh() {
			DeviceInfo.ulClassofDevice = 0;
			DeviceInfo.szName = "";
			return NativeMethods.BluetoothGetDeviceInfo(IntPtr.Zero, ref DeviceInfo) == 0;
		}

		internal bool PairDevice(CancellationToken token = default(CancellationToken)) {
			if (!Connected) {
				if (Remembered && !RemoveDevice(token))
					return false;
				if (token.IsCancellationRequested)
					return false;

				Guid[] services = new Guid[16];
				int serviceCount = services.Length;
				Guid uuid = Uuids.HumanInterfaceDeviceServiceClass;
				NativeMethods.BluetoothEnumerateInstalledServices(IntPtr.Zero, ref DeviceInfo, ref serviceCount, services);
				int res = NativeMethods.BluetoothSetServiceState(IntPtr.Zero, ref DeviceInfo, ref uuid, BluetoothServiceFlags.Enable);
				
				// EN: If standard HID enable fails (typical for RVL-CNT-01-TR with SSP), attempt PIN-less authentication
				// FR: Si l'activation HID standard échoue (typique pour RVL-CNT-01-TR avec SSP), tenter authentification sans PIN
				if (res != 0) {
					Log.Debug($"BluetoothSetServiceState returned {res}, attempting PIN-less BluetoothAuthenticateDevice for {this}...");
					NativeMethods.BluetoothAuthenticateDevice(IntPtr.Zero, IntPtr.Zero, ref DeviceInfo, "", 0);
					res = NativeMethods.BluetoothSetServiceState(IntPtr.Zero, ref DeviceInfo, ref uuid, BluetoothServiceFlags.Enable);
				}

				serviceCount = services.Length;
				NativeMethods.BluetoothEnumerateInstalledServices(IntPtr.Zero, ref DeviceInfo, ref serviceCount, services);
				if (res == 0) {
					Log.Info($"{this} Paired");
					return true;
				}
				return false;
			}
			return true;
		}

		internal bool RemoveDevice(CancellationToken token = default(CancellationToken)) {
			byte[] data = BitConverter.GetBytes(DeviceInfo.Address);
			if (NativeMethods.BluetoothRemoveDevice(data) == 0) {
				DeviceInfo.fConnected = false;
				Log.Info($"{this} Removed");
				return true;
			}
			return false;
		}

		public bool IsDiscoverable() {
			Stopwatch watch = Stopwatch.StartNew();
			Guid service = new Guid("{F13F471D-47CB-41d6-9609-BAD0690BF891}");
			//Guid service = Uuids.HumanInterfaceDeviceServiceClass;

			const AddressFamily Bluetooth = (AddressFamily) 32;
			const ProtocolType RFComm = (ProtocolType) 0x0003;
			Socket s = new Socket(Bluetooth, SocketType.Stream, RFComm);


			WSAQUERYSET wqs = new WSAQUERYSET();
			wqs.dwSize = 60;
			wqs.dwNameSpace = 16;

            GCHandle hservice = GCHandle.Alloc(service.ToByteArray(), GCHandleType.Pinned);
            wqs.lpServiceClassId = hservice.AddrOfPinnedObject();
            wqs.lpszContext = $"({Address.MacAddress})";

			IntPtr hLookup;
			int result;
			LookupFlags flags = LookupFlags.FlushCache | LookupFlags.ReturnName | LookupFlags.ReturnBlob;
			//flags = LookupFlags.FlushCache | LookupFlags.ReturnName;

			// Start looking for Bluetooth services

			result = NativeMethods.WSALookupServiceBegin(ref wqs, flags, out hLookup);
			int err = NativeMethods.WSAGetLastError();
			hservice.Free();
			Log.Debug($"IsDiscoverable: {watch.ElapsedMilliseconds}ms");
			if (result != 0)
				return false;
			NativeMethods.WSALookupServiceEnd(hLookup);
			return true;
		}

		internal static BluetoothDeviceInfo GetDevice(string hidPath) {
			return WiimoteRegistry.GetBluetoothDevice(hidPath);
		}

		internal static BluetoothDeviceInfo[] GetDevices() {
			return EnumerateDevices(new CancellationToken()).ToArray();
		}

		internal static BluetoothDeviceInfo[] GetDevices(CancellationToken token) {
			return EnumerateDevices(token).ToArray();
		}

		internal static BluetoothDeviceInfo[] GetDevices(Predicate<BluetoothDeviceInfo> match) {
			return EnumerateDevices(new CancellationToken(), match).ToArray();
		}

		internal static BluetoothDeviceInfo[] GetDevices(CancellationToken token, Predicate<BluetoothDeviceInfo> match) {
			return EnumerateDevices(token, match).ToArray();
		}

		internal static IEnumerable<BluetoothDeviceInfo> EnumerateDevices() {
			return EnumerateDevices(new CancellationToken());
		}

		internal static IEnumerable<BluetoothDeviceInfo> EnumerateDevices(CancellationToken token) {
			return EnumerateDevices(token, null);
		}

		internal static IEnumerable<BluetoothDeviceInfo> EnumerateDevices(Predicate<BluetoothDeviceInfo> match) {
			return EnumerateDevices(new CancellationToken(), match);
		}

		internal static IEnumerable<BluetoothDeviceInfo> EnumerateDevices(CancellationToken token, Predicate<BluetoothDeviceInfo> match) {
			IntPtr hFind = IntPtr.Zero;
			Stopwatch inquiryWatch = null; // [V53] Inquiry health tracking
			int inquiryDeviceCount = 0;
			try {
				BLUETOOTH_DEVICE_INFO btdi = new BLUETOOTH_DEVICE_INFO();
				BLUETOOTH_DEVICE_SEARCH_PARAMS srch = new BLUETOOTH_DEVICE_SEARCH_PARAMS();
				
				btdi.dwSize = Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>();
				srch.dwSize = Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>();

			srch.fReturnAuthenticated = true;
			srch.fReturnRemembered = true;
			srch.fReturnConnected = true;
			srch.fReturnUnknown = true;
			// [FIX V24→V39] A live Bluetooth inquiry hogs the radio for ~1.3s and degrades active
			// SSP/EDR links: on MotionPlus Inside (RVL-CNT-01-TR) Wiimotes the HID report stream
			// collapses into 27.5ms bursts (measured in WiimoteTimingDiagnostics CSV: gaps of
			// 27.5/2.4/27.4ms vs 4-16ms on V1). Connected and remembered devices are returned
			// without an inquiry; only pairing a brand new discoverable Wiimote needs one.
			//
			// [V39] Discovery window (user spec) replaces the flat 30s throttle:
			// - 0 Wiimote connected: full inquiry on EVERY enumeration (fast pairing, pre-V24
			//   behavior). Required for automated pairing of the first Wiimote.
			// - >=1 connected: keep the fast inquiry for a 90s window starting at the observed
			//   connection. Each NEW connection restarts the window (players turn their guns on
			//   one after another). Each DISCONNECTION also restarts the window so a dropped
			//   Wiimote can re-pair immediately.
			// - Outside the window: NO live inquiry at all (zero radio pollution during play).
			//   Remembered Wiimotes are still returned and reconnect without inquiry.
			// - 4/4 connected: no inquiry (max players reached).
			bool issueInquiry;
			int connectedCount = WiimoteManager.WiimoteCount;
			if (connectedCount == 0) {
				// Nothing connected yet: keep searching fast (EN/FR: Rien de connecté : recherche rapide continue)
				issueInquiry = true;
				_discoveryWindowEndUtc = DateTime.MinValue; // window (re)opens on next observed connection
			}
			else {
				if (connectedCount != _lastKnownConnectedCount) {
					// Connection or disconnection observed -> (re)start the 90s discovery window
					// (EN/FR: Connexion ou déconnexion observée -> (re)démarre la fenêtre de découverte de 90s)
					_discoveryWindowEndUtc = DateTime.UtcNow.AddSeconds(DiscoveryWindowSeconds);
					_lastInquiryTime = DateTime.MinValue; // force an immediate inquiry inside the window
				}

				if (connectedCount >= MaxSupportedWiimotes) {
					// 4/4 connected: nothing left to discover (EN/FR: 4/4 connectées : plus rien à découvrir)
					issueInquiry = false;
				}
				else if (DateTime.UtcNow < _discoveryWindowEndUtc) {
					// Inside the window: fast inquiry on every enumeration (EN/FR: Dans la fenêtre : inquiry rapide à chaque énumération)
					issueInquiry = true;
				}
				else {
					// [V39b] Window expired: fall back to the V24 slow cadence (max one inquiry per 30s).
					// A Wiimote powered on again (1+2) is ONLY visible through a live inquiry, and
					// re-pairing always removes the remembered entry first (PairDevice) — so the
					// search can never stop completely while Wiimotes can still join or come back.
					// (EN/FR: Fenêtre expirée : repli sur la cadence lente V24 (1 inquiry max toutes
					// les 30s). Une wiimote rallumée (1+2) n'est visible QUE via un inquiry live et
					// le ré-appairage supprime toujours l'entrée mémorisée d'abord (PairDevice) —
					// la recherche ne peut donc jamais s'arrêter totalement tant qu'une wiimote
					// peut encore revenir ou rejoindre.)
					issueInquiry = (DateTime.UtcNow - _lastInquiryTime).TotalSeconds >= 30.0;
				}
			}
			_lastKnownConnectedCount = connectedCount;
			if (issueInquiry) {
				_lastInquiryTime = DateTime.UtcNow;
				LastInquiryStartedUtc = DateTime.UtcNow; // [V53] Inquiry health tracking
				inquiryWatch = Stopwatch.StartNew();
			}
			else {
				LastInquiryDurationMs = -1.0; // [V53b] No inquiry this round: invalidate stale stats
			}
			srch.fIssueInquiry = issueInquiry;
				srch.cTimeoutMultiplier = 1;
				srch.hRadio = IntPtr.Zero;
				//srch.hRadio = InTheHand.Net.Bluetooth.BluetoothRadio.PrimaryRadio.Handle;

				hFind = NativeMethods.BluetoothFindFirstDevice(ref srch, ref btdi);
				do {
					BluetoothDeviceInfo device = new BluetoothDeviceInfo(btdi);
					if (match?.Invoke(device) ?? true) {
						inquiryDeviceCount++; // [V53]
						yield return device;
					}
					if (token.IsCancellationRequested)
						break;
				}
				while (NativeMethods.BluetoothFindNextDevice(hFind, ref btdi));
			}
			finally {
				if (hFind != IntPtr.Zero)
					NativeMethods.BluetoothFindDeviceClose(hFind);
				if (inquiryWatch != null) { // [V53] Publish inquiry health stats
					LastInquiryDurationMs = inquiryWatch.ElapsedMilliseconds;
					LastInquiryDeviceCount = inquiryDeviceCount;
				}
			}
		}
	}
}
