using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HIDMaestro;

// ============================================================================
// HmPoc - POC V57c Phase A (Wiimote4Guns x HIDMaestro UMDF2)
// Prouve : 4 x (souris ABSOLUE report 0x03 + clavier report 0x07) sur UN device
// UMDF2 par joueur, cles d'identite P1..P4, routage RawInput sans fuite entre
// joueurs (exigence emulators/DemulShooter). AUCUN fichier de l'app modifie.
// ============================================================================

internal static class Program
{
    private const int PlayerCount = 4;
    private const string VidPidToken = "vid_00ff&pid_bacc"; // VID/PID vmulti (compatibilite)
    private const byte RidMouse = 0x03;                     // vmulti.h REPORTID_MOUSE
    private const byte RidKb = 0x07;                        // vmulti.h REPORTID_KEYBOARD

    private static StreamWriter _logW;

    private static int Main(string[] args)
    {
        int holdSec = 60;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals("--hold", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int h))
                holdSec = h;

        string logFile = Path.Combine(AppContext.BaseDirectory, "hm-poc-log.txt");

        if (!IsAdmin())
        {
            Console.WriteLine("[POC] Pas eleve - relance admin : ACCEPTEZ l'invite UAC a l'ecran...");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath ?? "HmPoc.exe",
                    Arguments = "--elevated --hold " + holdSec,
                    Verb = "runas",
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory,
                };
                using var p = Process.Start(psi);
                bool done = p == null || p.WaitForExit(300000);
                Console.WriteLine($"[POC] Instance elevee terminee (exit={p?.ExitCode ?? -1}, attendu={done}).");
                Console.WriteLine($"[POC] Log complet : {logFile}");
            }
            catch (System.ComponentModel.Win32Exception wex)
            {
                Console.WriteLine($"[POC] UAC refuse/annule (code {wex.NativeErrorCode}). Rien n'a ete installe.");
            }
            return 0;
        }

        _logW = new StreamWriter(logFile, false, new UTF8Encoding(false)) { AutoFlush = true };
        L("==============================================================================");
        L("[POC] Wiimote4Guns x HIDMaestro UMDF2 - POC 4 joueurs souris+clavier (ELEVE)");
        L($"[POC] .NET {Environment.Version} | 64-bit process: {Environment.Is64BitProcess} | OS: {Environment.OSVersion}");
        L($"[POC] Base: {AppContext.BaseDirectory}");
        try
        {
            return RunElevated(holdSec);
        }
        catch (Exception ex)
        {
            L($"[POC] FATAL: {ex.GetType().Name}: {ex.Message}");
            L(ex.StackTrace ?? "");
            return 1;
        }
    }

    private static void L(string m)
    {
        Console.WriteLine(m);
        try
        {
            _logW?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {m}");
            _logW?.Flush();
        }
        catch { }
    }

    private static bool IsAdmin()
    {
        using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
        return new System.Security.Principal.WindowsPrincipal(id)
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    internal static bool IsOursPath(string path)
        => path != null && path.ToLowerInvariant().Contains(VidPidToken);

    private static string Short(string path)
    {
        string p = path.StartsWith("\\\\?\\", StringComparison.Ordinal) ? path.Substring(4) : path;
        return p.Length > 110 ? p.Substring(0, 110) + "..." : p;
    }

    // ------------------------------------------------------------------ flow

    private static int RunElevated(int holdSec)
    {
        string profilesDir = FindProfilesDir(AppContext.BaseDirectory);
        L($"[POC] Dossier profils: {profilesDir}");

        // 1. Baseline RawInput
        var before = RawInputLister.Snapshot();
        var beforeOurs = new HashSet<string>(
            before.Where(d => IsOursPath(d.Path)).Select(d => d.Path), StringComparer.OrdinalIgnoreCase);
        L($"[POC] RawInput AVANT : {before.Count} devices | {before.Count(d => d.Type == 0)} souris, "
          + $"{before.Count(d => d.Type == 1)} claviers, {before.Count(d => d.Type == 2)} hid generiques.");
        if (beforeOurs.Count > 0)
            L($"[POC] NOTE : {beforeOurs.Count} interface(s) VID/PID vmulti deja presentes (vmulti actif ou run precedente).");

        // 2. SDK + profil custom
        using var ctx = new HMContext();
        int loaded = ctx.LoadProfilesFromDirectory(profilesDir);
        L($"[POC] {loaded} profil(s) charge(s) depuis le dossier.");
        HMProfile profile = ctx.GetProfile("wiimotegun-mkb")
            ?? throw new InvalidOperationException("Profil 'wiimotegun-mkb' introuvable dans " + profilesDir);
        L($"[POC] Profil: {profile.Id}");

        // 3. Driver UMDF2 (self-bootstrap : cert auto-signe -> Root+TrustedPublisher)
        var sw = Stopwatch.StartNew();
        ctx.InstallDriver();
        L($"[POC] InstallDriver OK en {sw.ElapsedMilliseconds} ms (cert auto-signe + INF installes une seule fois).");

        // 4. Creation P1..P4 + attribution des interfaces par diff (ordre de creation)
        string[] keys = { "P1", "P2", "P3", "P4" };
        var ctrls = new HMController[PlayerCount];
        var playerIfaces = new List<RawInputLister.Dev>[PlayerCount];
        for (int i = 0; i < PlayerCount; i++)
        {
            playerIfaces[i] = new List<RawInputLister.Dev>();
            sw.Restart();
            ctrls[i] = ctx.CreateController(profile, keys[i]);
            L($"[POC] {keys[i]} : controller cree (identite '{keys[i]}') en {sw.ElapsedMilliseconds} ms.");
            for (int t = 0; t < 30; t++)
            {
                Thread.Sleep(400);
                var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < i; j++)
                    foreach (var d in playerIfaces[j]) taken.Add(d.Path);
                playerIfaces[i] = RawInputLister.Snapshot()
                    .Where(d => IsOursPath(d.Path) && !beforeOurs.Contains(d.Path) && !taken.Contains(d.Path))
                    .ToList();
                bool hasMouse = playerIfaces[i].Any(d => d.Type == 0);
                bool hasKb = playerIfaces[i].Any(d => d.Type == 1);
                if (hasMouse && hasKb) break;
            }
            foreach (var d in playerIfaces[i])
                L($"[POC]   {keys[i]} iface type={TypeName(d.Type)} : {Short(d.Path)}");
            if (!playerIfaces[i].Any(d => d.Type == 0) || !playerIfaces[i].Any(d => d.Type == 1))
                L($"[POC]   ATTENTION {keys[i]} : {playerIfaces[i].Count} interface(s) vue(s) (attendu >=2 dont souris ET clavier).");
        }

        // 5. Stabilite des chemins entre vies (runs)
        string pathsFile = Path.Combine(AppContext.BaseDirectory, "hm-poc-paths.txt");
        var currentLines = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < PlayerCount; i++)
            foreach (var d in playerIfaces[i])
                currentLines.Add($"{keys[i]}|{TypeName(d.Type)}|{d.Path}");
        if (File.Exists(pathsFile))
        {
            var prev = new SortedSet<string>(File.ReadAllLines(pathsFile)
                .Where(s => s.Trim().Length > 0), StringComparer.OrdinalIgnoreCase);
            bool same = prev.SetEquals(currentLines);
            L($"[POC] STABILITE inter-runs : {(same ? "CHEMINS IDENTIQUES (P1..P4 retrouves) OK" : "CHEMINS DIFFERENTS")}");
            if (!same)
                foreach (var p in prev)
                    if (!currentLines.Contains(p)) L($"[POC]   disparu : {p}");
        }
        File.WriteAllLines(pathsFile, currentLines);
        L($"[POC] Chemins sauvegardes : {pathsFile}");

        // 6. Preuve de routage RawInput par joueur
        using var listener = new RawListener();
        listener.Start();
        L("[POC] Listener RawInput demarre (usages souris+clavier, RIDEV_INPUTSINK).");

        var phases = new Dictionary<string, RawListener.PhaseStats>();
        for (int i = 0; i < PlayerCount; i++)
        {
            string phase = keys[i];
            listener.BeginPhase();
            // Souris absolue : cercle ~2 s @ ~30 Hz (comme un lightgun qui vise)
            for (int f = 0; f < 60; f++)
            {
                double a = f / 60.0 * 6.0 * Math.PI;
                SubmitMouse(ctrls[i], 16384 + (int)(6100 * Math.Cos(a)), 16384 + (int)(6100 * Math.Sin(a)), 0, 0);
                Thread.Sleep(33);
            }
            SubmitMouse(ctrls[i], 16000, 16000, 0x01, 0); Thread.Sleep(100);  // bouton gauche
            SubmitMouse(ctrls[i], 16000, 16000, 0x00, 0); Thread.Sleep(60);
            SubmitMouse(ctrls[i], 16000, 16000, 0x02, 0); Thread.Sleep(100);  // bouton droit
            SubmitMouse(ctrls[i], 16000, 16000, 0x00, 0); Thread.Sleep(60);
            SubmitMouse(ctrls[i], 16000, 16000, 0x00, 1);  Thread.Sleep(60);  // molette +1
            SubmitMouse(ctrls[i], 16000, 16000, 0x00, -1); Thread.Sleep(60);  // molette -1
            // Clavier : Shift+digit puis 'p'+digit (preuve modificateurs + scancodes)
            SubmitKeys(ctrls[i], 0x02, (byte)(0x1E + i)); Thread.Sleep(90);
            SubmitKeys(ctrls[i], 0x00); Thread.Sleep(90);
            SubmitKeys(ctrls[i], 0x00, 0x13, (byte)(0x1E + i)); Thread.Sleep(90);
            SubmitKeys(ctrls[i], 0x00); Thread.Sleep(250);
            phases[phase] = listener.EndPhase();
        }

        L("");
        L("[POC] =================== VERDICT ROUTAGE RAWINPUT ===================");
        int failures = 0;
        for (int i = 0; i < PlayerCount; i++)
        {
            string phase = keys[i];
            var st = phases[phase];
            string myMouse = playerIfaces[i].Where(d => d.Type == 0).Select(d => d.Path).FirstOrDefault() ?? "";
            string myKb = playerIfaces[i].Where(d => d.Type == 1).Select(d => d.Path).FirstOrDefault() ?? "";
            int myMouseCount = st.Counts.GetValueOrDefault(myMouse, 0);
            int myKbCount = st.Counts.GetValueOrDefault(myKb, 0);
            int stray = 0;
            for (int j = 0; j < PlayerCount; j++)
            {
                if (j == i) continue;
                foreach (var d in playerIfaces[j])
                    stray += st.Counts.GetValueOrDefault(d.Path, 0);
            }
            bool ok = myMouseCount >= 20 && myKbCount >= 2 && stray <= 2;
            if (!ok) failures++;
            L($"[POC] {phase} : souris={myMouseCount} ev, clavier={myKbCount} ev, fuite vers autres joueurs={stray}"
              + $" -> {(ok ? "PASS" : "FAIL")}");
            if (st.LastMouse.Count > 0)
                L($"[POC]   dernieretat souris: {st.LastMouse.Values.First()}");
            if (st.LastKeys.Count > 0)
                L($"[POC]   dernieretat clavier: {st.LastKeys.Values.First()}");
        }
        L("[POC] ====================================================================");

        // 7. Maintien en vie pour verification externe (Gestionnaire de peripheriques, DemulShooter...)
        L($"[POC] Devices P1..P4 maintenus vivants {holdSec} s - testez de votre cote si besoin.");
        var holdEnd = DateTime.UtcNow.AddSeconds(holdSec);
        int w = 0;
        while (DateTime.UtcNow < holdEnd)
        {
            Thread.Sleep(500);
            long tick = Environment.TickCount64 % 4000;
            if (tick < 500) // bref mouvement d'un joueur a la fois, toutes les ~4 s
            {
                int pl = w++ % PlayerCount;
                for (int f = 0; f < 8; f++)
                {
                    double a = f * 0.7;
                    SubmitMouse(ctrls[pl], 16384 + (int)(5000 * Math.Cos(a)), 16384 + (int)(5000 * Math.Sin(a)), 0, 0);
                    Thread.Sleep(20);
                }
            }
        }

        // 8. Teardown + verif zero residu
        L("[POC] Disposition des 4 controllers...");
        for (int i = 0; i < PlayerCount; i++) ctrls[i].Dispose();
        Thread.Sleep(1500);
        var after = RawInputLister.Snapshot();
        var leftover = after.Where(d => IsOursPath(d.Path) && !beforeOurs.Contains(d.Path)).ToList();
        L($"[POC] Apres disposition : {leftover.Count} interface(s) residuelle(s) (attendu 0).");
        foreach (var d in leftover) L($"[POC]   RESIDUEL: {Short(d.Path)}");

        listener.Dispose();
        ctx.Dispose();
        L("[POC] TERMINE " + (failures == 0 ? "- POC REUSSI ✔" : $"- {failures} ECHEC(S)"));
        return failures == 0 ? 0 : 2;
    }

    private static string FindProfilesDir(string baseDir)
    {
        var cur = new DirectoryInfo(baseDir);
        for (int up = 0; up < 8 && cur != null; up++, cur = cur.Parent)
        {
            string cand = Path.Combine(cur.FullName, "profiles");
            if (File.Exists(Path.Combine(cand, "wiimotegun-mkb.json"))) return cand;
        }
        throw new DirectoryNotFoundException("profiles/wiimotegun-mkb.json introuvable au-dessus de " + baseDir);
    }

    private static string TypeName(uint t) => t switch { 0 => "MOUSE", 1 => "KEYBOARD", _ => $"HID({t})" };

    // ------------------------------------------------- soumission des rapports

    private static void SubmitMouse(HMController c, int x, int y, int buttons, int wheel)
    {
        // VMULTI_MOUSE_REPORT : ReportID, Button, X(2 LE), Y(2 LE), Wheel - absolus 0..32767
        x = Math.Max(0, Math.Min(0x7FFF, x));
        y = Math.Max(0, Math.Min(0x7FFF, y));
        Span<byte> rep = stackalloc byte[7];
        rep[0] = RidMouse;
        rep[1] = (byte)buttons;
        ushort ux = (ushort)x, uy = (ushort)y;
        MemoryMarshal.Write(rep.Slice(2), ref ux);
        MemoryMarshal.Write(rep.Slice(4), ref uy);
        rep[6] = (byte)wheel;
        c.SubmitRawExtendedReport(rep);
    }

    private static void SubmitKeys(HMController c, byte modifiers, params byte[] keys)
    {
        // VMULTI_KEYBOARD_REPORT : ReportID, Modifiers, Reserved, KeyCodes[6]
        Span<byte> rep = stackalloc byte[9];
        rep[0] = RidKb;
        rep[1] = modifiers;
        rep[2] = 0;
        for (int i = 0; i < 6; i++) rep[3 + i] = i < keys.Length ? keys[i] : (byte)0;
        c.SubmitRawExtendedReport(rep);
    }
}

// ============================================================================
// Enumeration RawInput (GetRawInputDeviceList)
// ============================================================================

internal static class RawInputLister
{
    public sealed record Dev(IntPtr Handle, uint Type, string Path);

    public static List<Dev> Snapshot()
    {
        uint count = 0;
        if (GetRawInputDeviceList(null, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>()) != 0)
            return new List<Dev>();
        if (count == 0) return new List<Dev>();
        var arr = new RAWINPUTDEVICELIST[count];
        uint got = GetRawInputDeviceList(arr, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>());
        var list = new List<Dev>();
        for (uint i = 0; i < got; i++)
        {
            if (arr[i].hDevice == IntPtr.Zero) continue;
            list.Add(new Dev(arr[i].hDevice, arr[i].dwType, DeviceName(arr[i].hDevice)));
        }
        return list;
    }

    internal static string DeviceName(IntPtr h)
    {
        uint size = 512;
        var sb = new StringBuilder((int)size);
        uint n = GetRawInputDeviceInfoW(h, 0x2000000 /*RIDI_DEVICENAME*/, sb, ref size);
        if (n == 0 || n == 0xFFFFFFFF) return "";
        string s = sb.ToString();
        int z = s.IndexOf('\0');
        return z >= 0 ? s.Substring(0, z) : s;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    [DllImport("user32.dll")]
    private static extern uint GetRawInputDeviceList([In, Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfoW(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);
}

// ============================================================================
// Ecoute WM_INPUT : compte les evenements PAR device pendant chaque phase
// ============================================================================

internal sealed class RawListener : IDisposable
{
    public sealed record PhaseStats(Dictionary<string, int> Counts, Dictionary<string, string> LastMouse, Dictionary<string, string> LastKeys);

    private const uint WM_INPUT = 0x00FF;
    private const uint WM_APP_STOP = 0x8001;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RID_INPUT = 0x10000003;

    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER { public uint dwType; public uint dwSize; public IntPtr hDevice; public IntPtr wParam; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public uint dwFlags; public IntPtr hwndTarget; }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize; public uint style; public IntPtr lpfnWndProc; public int cbClsExtra; public int cbWndExtra;
        public IntPtr hInstance; public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
        public uint time; public int ptX; public int ptY; public uint lPrivate;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style,
        int x, int y, int w, int h, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll")]
    private static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);

    private static readonly WndProc WndProcThunk = StaticWndProc;
    private static RawListener _current;
    private static readonly Dictionary<IntPtr, string> NameCache = new();

    private readonly object _gate = new();
    private readonly Dictionary<string, int> _phaseCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lastMouse = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lastKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ManualResetEvent _ready = new(false);
    private Thread _thread;
    private IntPtr _hwnd;

    public void Start()
    {
        _current = this;
        _thread = new Thread(Pump) { IsBackground = true, Name = "HmPocRawInput" };
        _thread.Start();
        _ready.WaitOne(5000);
    }

    private void Pump()
    {
        try
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcThunk),
                lpszClassName = "HmPocWnd",
                hInstance = GetModuleHandleW(null),
            };
            RegisterClassExW(ref wc);
            _hwnd = CreateWindowExW(0, "HmPocWnd", "", 0, 0, 0, 0, 0, new IntPtr(-3) /*HWND_MESSAGE*/,
                IntPtr.Zero, wc.hInstance, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException("CreateWindowEx a echoue (err " + Marshal.GetLastWin32Error() + ")");
            var rid = new RAWINPUTDEVICE[]
            {
                new() { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = _hwnd }, // souris
                new() { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_INPUTSINK, hwndTarget = _hwnd }, // clavier
            };
            bool ok = RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
            if (!ok) throw new InvalidOperationException("RegisterRawInputDevices a echoue");
            _ready.Set();
            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
                DispatchMessageW(ref msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[POC] Listener FATAL: " + ex.Message);
            _ready.Set();
        }
    }

    private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_INPUT) _current?.HandleInput(lParam);
        if (msg == WM_APP_STOP) PostQuitMessage(0);
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void HandleInput(IntPtr hRawInput)
    {
        uint size = 0;
        if (GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, HeaderSize) == 0xFFFFFFFF || size == 0) return;
        if (size > 8192) return;
        var buf = new byte[size];
        IntPtr p = Marshal.AllocHGlobal((int)size);
        try
        {
            uint s2 = size;
            if (GetRawInputData(hRawInput, RID_INPUT, p, ref s2, HeaderSize) == 0xFFFFFFFF) return;
            if (s2 > size) s2 = size;
            Marshal.Copy(p, buf, 0, (int)s2);
        }
        finally { Marshal.FreeHGlobal(p); }

        if (buf.Length < HeaderSize + 4) return;
        uint type = BitConverter.ToUInt32(buf, 0);
        long hdev = BitConverter.ToInt64(buf, 8);
        if (hdev == 0) return;

        string path;
        lock (NameCache)
        {
            if (!NameCache.TryGetValue(new IntPtr(hdev), out path))
            {
                path = RawInputLister.DeviceName(new IntPtr(hdev));
                NameCache[new IntPtr(hdev)] = path;
            }
        }
        if (!Program.IsOursPath(path)) return; // on ne compte que nos 8 interfaces

        int body = (int)HeaderSize;
        lock (_gate)
        {
            _phaseCounts.TryGetValue(path, out int c);
            _phaseCounts[path] = c + 1;
            if (type == 0 && buf.Length >= body + 20)
            {
                int x = BitConverter.ToInt32(buf, body + 12);
                int y = BitConverter.ToInt32(buf, body + 16);
                ushort bf = BitConverter.ToUInt16(buf, body + 4);
                _lastMouse[path] = $"pos=({x},{y}) buttonFlags=0x{bf:X}";
            }
            else if (type == 1 && buf.Length >= body + 2)
            {
                ushort vkey = BitConverter.ToUInt16(buf, body);
                _lastKeys[path] = $"vkey=0x{vkey:X}";
            }
        }
    }

    public void BeginPhase()
    {
        lock (_gate) { _phaseCounts.Clear(); _lastMouse.Clear(); _lastKeys.Clear(); }
    }

    public PhaseStats EndPhase()
    {
        lock (_gate)
        {
            return new PhaseStats(
                new Dictionary<string, int>(_phaseCounts, StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(_lastMouse, StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(_lastKeys, StringComparer.OrdinalIgnoreCase));
        }
    }

    public void Dispose()
    {
        try { if (_hwnd != IntPtr.Zero) PostMessageW(_hwnd, WM_APP_STOP, IntPtr.Zero, IntPtr.Zero); } catch { }
        try { _thread?.Join(3000); } catch { }
        _ready.Dispose();
    }
}
