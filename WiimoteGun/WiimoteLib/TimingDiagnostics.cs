using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace WiimoteLib
{
    /// <summary>
    /// Low-overhead timing diagnostics for investigating Wiimote V1 vs MotionPlus Inside V2 jitter.
    /// Writes CSV to WiimoteTimingDiagnostics.csv next to the executable.
    /// Set Enabled=false to disable completely.
    /// </summary>
    internal static class TimingDiagnostics
    {
        public static bool Enabled = true;

        // [V29] Max file size before rotation (EN/FR: Taille max avant rotation)
        private const long MaxFileBytes = 2 * 1024 * 1024; // 2 MB

        private static readonly object Sync = new object();
        private static StreamWriter _writer;
        private static int _pending;
        private static long _rawSequence;
        private static long _stateSequence;

        private static readonly long Frequency = Stopwatch.Frequency;
        private static long NowTicks { get { return Stopwatch.GetTimestamp(); } }

        private static double Ms(long ticks)
        {
            return ticks * 1000.0 / Frequency;
        }

        private static StreamWriter Writer
        {
            get
            {
                if (_writer != null) return _writer;
                lock (Sync)
                {
                    if (_writer == null)
                    {
                        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteTimingDiagnostics.csv");
                        _writer = new StreamWriter(path, false, new UTF8Encoding(false), 65536);
                        _writer.WriteLine("utc;kind;seq;report;extension;dt_ms;duration_us;gyro_us;ir_us;position_us;ir0;ir1;x;y;notes");
                        _writer.Flush();
                    }
                }
                return _writer;
            }
        }

        private static void Write(string line)
        {
            if (!Enabled) return;
            try
            {
                lock (Sync)
                {
                    Writer.WriteLine(line);
                    _pending++;
                    if (_pending >= 64)
                    {
                        Writer.Flush();
                        _pending = 0;
                        RotateIfNeeded();
                    }
                }
            }
            catch { /* Diagnostics must never affect Wiimote processing. */ }
        }

        // [V29] 2MB rotation: rename the current file to WiimoteTimingDiagnostics.old.csv
        // (overwriting any previous .old) and let the next Write create a fresh file.
        // This bounds disk usage to ~4MB (2MB current + 2MB old) while keeping recent data
        // always available for support/debug. Caller must hold Sync.
        // (EN/FR: Rotation 2Mo : renomme le fichier courant en .old.csv (en écrasant
        // l'ancien .old) ; le prochain Write crée un fichier neuf. Usage disque borné ~4Mo.)
        private static void RotateIfNeeded()
        {
            try
            {
                if (_writer == null || _writer.BaseStream == null) return;
                if (_writer.BaseStream.Length < MaxFileBytes) return;
                string currentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteTimingDiagnostics.csv");
                string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteTimingDiagnostics.old.csv");
                _writer.Dispose();
                _writer = null;
                if (File.Exists(oldPath)) File.Delete(oldPath);
                if (File.Exists(currentPath)) File.Move(currentPath, oldPath);
            }
            catch { _writer = null; }
        }

        public static long BeginRaw()
        {
            return Enabled ? NowTicks : 0;
        }

        public static void RawReport(long startTicks, byte reportId, bool newInput, long previousEndTicks)
        {
            if (!Enabled) return;
            long end = NowTicks;
            long seq = Interlocked.Increment(ref _rawSequence);
            double dt = previousEndTicks == 0 ? 0 : Ms(startTicks - previousEndTicks);
            double durationUs = Ms(end - startTicks) * 1000.0;
            Write(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:O};raw;{1};0x{2:X2};-;{3:F3};{4:F1};-;-;-;-;-;-;-;-;newInput={5}",
                DateTime.UtcNow, seq, reportId, dt, durationUs, newInput ? 1 : 0));
        }

        public static long BeginState()
        {
            return Enabled ? NowTicks : 0;
        }

        public static long BeginStage()
        {
            return Enabled ? NowTicks : 0;
        }

        public static void StateReport(long startTicks, string extension, double gyroUs, double irUs,
            double positionUs, bool ir0, bool ir1, int x, int y, string notes)
        {
            if (!Enabled) return;
            long end = NowTicks;
            long seq = Interlocked.Increment(ref _stateSequence);
            double durationUs = Ms(end - startTicks) * 1000.0;
            Write(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:O};state;{1};-;{2};-;{3:F1};{4:F1};{5:F1};{6};{7};{8};{9};{10};{11}",
                DateTime.UtcNow, seq, extension ?? "None", 0.0, durationUs, gyroUs, irUs, positionUs,
                ir0 ? 1 : 0, ir1 ? 1 : 0, x, y, notes ?? ""));
        }

        public static double ElapsedUs(long startTicks)
        {
            return Enabled ? Ms(NowTicks - startTicks) * 1000.0 : 0.0;
        }

        public static void Shutdown()
        {
            lock (Sync)
            {
                if (_writer != null)
                {
                    try { _writer.Flush(); _writer.Dispose(); } catch { }
                    _writer = null;
                }
            }
        }
    }
}
