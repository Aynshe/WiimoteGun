using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WiimoteGun.Service
{
    /// <summary>
    /// EN: [V55v] Crash/hang watchdog for the WiimoteGun client (WiimoteGun.exe).
    /// Monitors the registered client (REGISTER_CLIENT:PID via pipe):
    /// - CRASH: the client process exited WITHOUT unregistering (no UNREGISTER_CLIENT)
    ///   -> restart WiimoteGun.
    /// - HANG: the client is alive but its heartbeat file (WiimoteGun.heartbeat,
    ///   written every 5s BY THE APP UI THREAD) is stale for 90+ seconds = the UI
    ///   thread is frozen ("not responding") -> kill + restart WiimoteGun.
    /// A restart is ALWAYS performed as the INTERACTIVE CONSOLE USER, NEVER as
    /// administrator: the service uses WTSQueryUserToken (the session's user token,
    /// medium integrity) + CreateProcessAsUser. Deliberate exits (UNREGISTER_CLIENT,
    /// including the :RESTART update flow) disarm the watchdog, so normal quits and
    /// updates are never "restarted".
    /// FR: [V55v] Chien de garde de crash/gel pour le client WiimoteGun (WiimoteGun.exe).
    /// Surveille le client enregistré (REGISTER_CLIENT:PID via pipe) :
    /// - CRASH : le processus client s'est terminé SANS se désenregistrer (aucun
    ///   UNREGISTER_CLIENT) -> relancer WiimoteGun.
    /// - GEL : le client est vivant mais son fichier de battement (WiimoteGun.heartbeat,
    ///   écrit toutes les 5s PAR LE THREAD UI de l'app) est périmé depuis 90+ secondes =
    ///   le thread UI est figé (« ne répond pas ») -> tuer + relancer WiimoteGun.
    /// Un redémarrage est TOUJOURS effectué en tant qu'UTILISATEUR INTERACTIF DE LA
    /// CONSOLE, JAMAIS en administrateur : le service utilise WTSQueryUserToken (le
    /// jeton utilisateur de la session, intégrité moyenne) + CreateProcessAsUser.
    /// Les sorties volontaires (UNREGISTER_CLIENT, y compris le flux de mise à jour
    /// :RESTART) désarment le watchdog : les fermetures normales et les mises à jour
    /// ne sont jamais « relancées ».
    /// </summary>
    public static class CrashWatchdog
    {
        // EN/FR: Must match Core/AppHeartbeat.cs in the WiimoteGun app
        private const string HeartbeatFileName = "WiimoteGun.heartbeat";
        private const int CheckIntervalMs = 10000;
        private const int HeartbeatStaleSeconds = 90;   // 12+ missed 5s beats = frozen UI
        private const int PostRegisterGraceSeconds = 30; // let the app settle after registration
        private const int MaxRestarts = 5;               // crash-loop protection
        private const int RestartCounterResetSeconds = 120; // client stayed up 2min = reset counter

        private static Timer _timer;
        private static readonly object _lock = new object();
        private static int _clientPid;
        private static string _exePath;
        private static string _heartbeatPath;
        private static DateTime _armedUtc;
        private static DateTime _lastRestartUtc = DateTime.MinValue;
        private static int _restartCount;
        private static bool _dumpsConfigured = false; // [V55w] LocalDumps configured once per service run

        // ============================== Lifecycle ==============================

        public static void Start()
        {
            if (_timer != null) return;
            _timer = new Timer(CheckCallback, null, CheckIntervalMs, CheckIntervalMs);
            DriverController.Log("[CrashWatchdog] Started (client crash/hang detection, restarts as interactive user - never admin).");
        }

        /// <summary>
        /// EN: [V55w] Enable WER LocalDumps for WiimoteGun.exe (idempotent): full dumps of
        /// native crashes (heap corruption c0000374) are written INSIDE THE APP DIRECTORY
        /// (<appDir>\CrashDumps) — the app never writes outside its own folder.
        /// FR: [V55w] Active les LocalDumps WER pour WiimoteGun.exe (idempotent) : les dumps
        /// complets des crashes natifs (corruption de tas c0000374) sont écrits DANS LE
        /// DOSSIER DE L'APP (<appDir>\CrashDumps) — l'app n'écrit jamais hors de son
        /// propre dossier.
        /// </summary>
        private static void EnsureLocalDumpsEnabled(string appDir)
        {
            try
            {
                const string keyPath = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\WiimoteGun.exe";
                string dumpFolder = Path.Combine(appDir, "CrashDumps");
                Directory.CreateDirectory(dumpFolder);

                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath))
                {
                    if (key == null)
                    {
                        DriverController.Log("[CrashWatchdog] ERROR: could not open WER LocalDumps registry key.");
                        return;
                    }

                    // EN/FR: Full dump (2), keep the 3 most recent
                    key.SetValue("DumpFolder", dumpFolder, Microsoft.Win32.RegistryValueKind.String);
                    key.SetValue("DumpType", 2, Microsoft.Win32.RegistryValueKind.DWord);
                    key.SetValue("DumpCount", 3, Microsoft.Win32.RegistryValueKind.DWord);
                }

                DriverController.Log("[CrashWatchdog] WER LocalDumps enabled for WiimoteGun.exe (full dumps -> " + dumpFolder + ") — the next native crash (heap corruption c0000374) will be captured for root-cause analysis.");
            }
            catch (Exception ex)
            {
                DriverController.Log("[CrashWatchdog] Could not enable WER LocalDumps: " + ex.Message);
            }
        }

        /// <summary>
        /// EN: Called by PipeServer on REGISTER_CLIENT:PID. Resolves the client exe
        /// path (used both for the heartbeat file location and for the restart).
        /// FR: Appelé par PipeServer sur REGISTER_CLIENT:PID. Résout le chemin de l'exe
        /// client (utilisé pour l'emplacement du heartbeat et pour le redémarrage).
        /// </summary>
        public static void OnClientRegistered(int pid)
        {
            lock (_lock)
            {
                _clientPid = pid;
                _armedUtc = DateTime.UtcNow;

                // EN/FR: Reset the crash-loop counter when a client stayed up long enough
                if ((DateTime.UtcNow - _lastRestartUtc).TotalSeconds > RestartCounterResetSeconds)
                    _restartCount = 0;

                _exePath = null;
                _heartbeatPath = null;
                try
                {
                    Process p = Process.GetProcessById(pid);
                    _exePath = p.MainModule.FileName;
                    _heartbeatPath = Path.Combine(Path.GetDirectoryName(_exePath), HeartbeatFileName);

                    // [V55w] EN/FR: Configure WER LocalDumps INSIDE the app directory (once
                    // per service run, idempotent) — the app never writes outside its folder
                    if (!_dumpsConfigured)
                    {
                        EnsureLocalDumpsEnabled(Path.GetDirectoryName(_exePath));
                        _dumpsConfigured = true;
                    }
                }
                catch (Exception ex)
                {
                    DriverController.Log($"[CrashWatchdog] Could not resolve client PID {pid} exe path: {ex.Message} — watchdog limited until next registration.");
                }

                DriverController.Log($"[CrashWatchdog] Armed for client PID {pid} (exe: {_exePath ?? "?"}, heartbeat: {_heartbeatPath ?? "?"}).");
            }
        }

        /// <summary>
        /// EN: Called by PipeServer on UNREGISTER_CLIENT(:RESTART). Deliberate exit
        /// (user quit or controlled restart/update): the watchdog must NOT restart.
        /// FR: Appelé par PipeServer sur UNREGISTER_CLIENT(:RESTART). Sortie
        /// volontaire (fermeture utilisateur ou redémarrage/mise à jour contrôlée) :
        /// le watchdog ne doit PAS relancer.
        /// </summary>
        public static void OnClientUnregistered(bool isRestart)
        {
            lock (_lock)
            {
                _clientPid = 0;
                _exePath = null;
                _heartbeatPath = null;
                DriverController.Log($"[CrashWatchdog] Disarmed (client unregistered, controlled exit, restart={isRestart}).");
            }
        }

        // ============================== Monitor ==============================

        private static void CheckCallback(object state)
        {
            try
            {
                int pid;
                string exe;
                string hbPath;
                DateTime armed;
                lock (_lock)
                {
                    pid = _clientPid;
                    exe = _exePath;
                    hbPath = _heartbeatPath;
                    armed = _armedUtc;
                }
                if (pid <= 0 || string.IsNullOrEmpty(exe)) return; // Not armed / deliberate exit
                if ((DateTime.UtcNow - armed).TotalSeconds < PostRegisterGraceSeconds) return;

                bool alive;
                try
                {
                    Process proc = Process.GetProcessById(pid);
                    alive = !proc.HasExited;
                }
                catch
                {
                    alive = false;
                }

                if (!alive)
                {
                    DriverController.Log($"[CrashWatchdog] Client PID {pid} exited WITHOUT unregistering (CRASH) — restarting WiimoteGun as user.");
                    RestartClient(pid, exe, killFirst: false);
                    return;
                }

                // EN/FR: Hang detection — the heartbeat file is written by the app UI
                // thread every 5s; 90s stale = the UI is frozen ("not responding").
                if (!string.IsNullOrEmpty(hbPath) && File.Exists(hbPath))
                {
                    double staleSec;
                    try
                    {
                        staleSec = (DateTime.UtcNow - File.GetLastWriteTimeUtc(hbPath)).TotalSeconds;
                    }
                    catch
                    {
                        return; // File locked mid-write: re-check next round
                    }

                    if (staleSec > HeartbeatStaleSeconds)
                    {
                        DriverController.Log($"[CrashWatchdog] Client PID {pid} appears HUNG: heartbeat stale for {staleSec:F0}s (UI frozen) — killing and restarting WiimoteGun as user.");
                        RestartClient(pid, exe, killFirst: true);
                    }
                }
            }
            catch (Exception ex)
            {
                DriverController.Log("[CrashWatchdog] Check error: " + ex.Message);
            }
        }

        private static void RestartClient(int pid, string exePath, bool killFirst)
        {
            lock (_lock)
            {
                _restartCount++;
                _lastRestartUtc = DateTime.UtcNow;
                // EN/FR: Disarm now; the restarted instance re-registers via REGISTER_CLIENT
                _clientPid = 0;
                _exePath = null;
                _heartbeatPath = null;

                if (_restartCount > MaxRestarts)
                {
                    DriverController.Log($"[CrashWatchdog] ERROR: {MaxRestarts} restarts already performed — CRASH LOOP suspected. Automatic restarts DISABLED until a client stays up {RestartCounterResetSeconds}s+; manual intervention required.");
                    return;
                }
            }

            if (killFirst)
            {
                try
                {
                    Process hung = Process.GetProcessById(pid);
                    DriverController.Log($"[CrashWatchdog] Killing hung WiimoteGun PID {pid}...");
                    hung.Kill();
                    try { hung.WaitForExit(5000); } catch { }
                }
                catch (ArgumentException)
                {
                    // EN/FR: Already gone between checks — proceed with the restart
                }
                catch (Exception ex)
                {
                    DriverController.Log("[CrashWatchdog] Kill phase error: " + ex.Message);
                }
            }

            // EN/FR: Let the old instance's handles (pipe, heartbeat file, single-instance
            // mutex) release before launching the replacement.
            Thread.Sleep(2000);

            LaunchAsInteractiveUser(exePath);
        }

        // ==================== Launch as interactive user (NEVER admin) ====================

        [DllImport("wtsapi32.dll")]
        private static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(int sessionId, out IntPtr token);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inheritExisting);

        [DllImport("userenv.dll")]
        private static extern bool DestroyEnvironmentBlock(IntPtr environment);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUser(
            IntPtr token,
            string applicationName,
            string commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            string currentDirectory,
            ref STARTUPINFO startupInfo,
            out PROCESS_INFORMATION processInformation);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>
        /// EN: Launch WiimoteGun as the INTERACTIVE CONSOLE USER — never as admin and
        /// never as SYSTEM. WTSQueryUserToken returns the session user's (filtered,
        /// medium-integrity) token: the app restarts exactly as if double-clicked.
        /// FR: Lance WiimoteGun en tant qu'UTILISATEUR INTERACTIF DE LA CONSOLE — jamais
        /// en admin et jamais en SYSTEM. WTSQueryUserToken retourne le jeton (filtré,
        /// intégrité moyenne) de l'utilisateur de la session : l'app redémarre exactement
        /// comme si elle avait été double-cliquée.
        /// </summary>
        private static void LaunchAsInteractiveUser(string exePath)
        {
            try
            {
                int sessionId = WTSGetActiveConsoleSessionId();
                if (sessionId < 0)
                {
                    DriverController.Log("[CrashWatchdog] ERROR: no active console session (user logged off?) — restart aborted.");
                    return;
                }

                IntPtr userToken;
                if (!WTSQueryUserToken(sessionId, out userToken))
                {
                    DriverController.Log("[CrashWatchdog] ERROR: WTSQueryUserToken failed (error " + Marshal.GetLastWin32Error() + ") — restart aborted.");
                    return;
                }

                try
                {
                    // EN/FR: Build the USER environment block (APPDATA/TEMP of the user,
                    // not of the SYSTEM service)
                    IntPtr envBlock = IntPtr.Zero;
                    bool hasEnv = CreateEnvironmentBlock(out envBlock, userToken, false);

                    try
                    {
                        STARTUPINFO si = new STARTUPINFO();
                        si.cb = Marshal.SizeOf(typeof(STARTUPINFO));
                        si.lpDesktop = "winsta0\\default";

                        uint flags = 0;
                        if (hasEnv) flags |= CREATE_UNICODE_ENVIRONMENT;

                        PROCESS_INFORMATION pi;
                        // EN/FR: [V55v] NEVER elevated: the WTS token is the interactive
                        // session's filtered user token (medium integrity level).
                        bool ok = CreateProcessAsUser(
                            userToken,
                            exePath,
                            null,
                            IntPtr.Zero, IntPtr.Zero,
                            false,
                            flags,
                            hasEnv ? envBlock : IntPtr.Zero,
                            Path.GetDirectoryName(exePath),
                            ref si,
                            out pi);

                        if (ok)
                        {
                            DriverController.Log($"[CrashWatchdog] WiimoteGun restarted as console user (new PID {pi.dwProcessId}, exe: {exePath}).");
                            CloseHandle(pi.hProcess);
                            CloseHandle(pi.hThread);
                        }
                        else
                        {
                            DriverController.Log("[CrashWatchdog] ERROR: CreateProcessAsUser failed (error " + Marshal.GetLastWin32Error() + ") — restart aborted.");
                        }
                    }
                    finally
                    {
                        if (hasEnv) DestroyEnvironmentBlock(envBlock);
                    }
                }
                finally
                {
                    CloseHandle(userToken);
                }
            }
            catch (Exception ex)
            {
                DriverController.Log("[CrashWatchdog] Launch error: " + ex.Message);
            }
        }
    }
}
