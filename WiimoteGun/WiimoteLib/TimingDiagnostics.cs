using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace WiimoteLib
{
    /// <summary>
    /// Low-overhead timing diagnostics for investigating Wiimote V1 vs MotionPlus Inside V2 jitter.
    /// Writes CSV to WiimoteTimingDiagnostics.csv next to the executable.
    /// Enabled only when the logging level is ALL (Options > General > Logging Level).
    /// [V70-BT-BURST] The write is done on a DEDICATED background thread: the Wiimote read
    /// thread only enqueues a line (a lock-free operation) so a slow disk / the 2MB rotation can
    /// never freeze the tracking (observed: ~500 ms stalls on both Wiimotes at once).
    /// </summary>
    internal static class TimingDiagnostics
    {
        // [V70-THREAD-TRACKING] EN/FR: enabled only when the logging level is ALL (Options >
        // General > Logging Level). The CSV is an analysis tool: it only follows a deliberate
        // choice, never a default. Evaluated live so toggling the level takes effect at once.
        // (FR: actif UNIQUEMENT si le niveau de log est ALL (Options > General > Logging Level).
        //  Le CSV est un outil d'analyse : il suit un choix explicite, jamais un défaut.
        //  Évalué en direct pour que le changement de niveau prenne effet immédiatement.)
        private static bool _shutdown;
        public static bool Enabled
        {
            get { return !_shutdown && LoggingLevelIsAll(); }
            set { /* legacy setter: ignored, the log level decides */ }
        }

        private static bool LoggingLevelIsAll()
        {
            try { return WiimoteGun.Options.Instance.LoggingLevel == WiimoteGun.LogLevel.ALL; }
            catch { return false; }
        }

        // [V29] Max file size before rotation (EN/FR: Taille max avant rotation)
        private const long MaxFileBytes = 2 * 1024 * 1024; // 2 MB
        private const int FlushEveryLines = 64;

        private static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private static readonly AutoResetEvent Wake = new AutoResetEvent(false);
        private static Thread _writerThread;
        private static volatile bool _writerStopping;

        private static StreamWriter _writer;
        private static long _rawSequence;
        private static long _stateSequence;
        private static int _pending;

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
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteTimingDiagnostics.csv");
                _writer = new StreamWriter(path, false, new UTF8Encoding(false), 65536);
                _writer.WriteLine("utc;kind;seq;report;extension;dt_ms;duration_us;gyro_us;ir_us;position_us;ir0;ir1;x;y;notes");
                _writer.Flush();
                return _writer;
            }
        }

        // [V70-BT-BURST] EN: fast producer side — enqueue only (never blocks on the disk).
        // FR: côté producteur rapide — mise en file uniquement (ne bloque JAMAIS sur le disque).
        private static void Write(string line)
        {
            if (!Enabled) return;
            Queue.Enqueue(line);
            Wake.Set();
            EnsureWriterThread();
        }

        private static void EnsureWriterThread()
        {
            if (_writerThread != null) return;
            lock (typeof(TimingDiagnostics))
            {
                if (_writerThread != null) return;
                _writerThread = new Thread(WriterLoop)
                {
                    IsBackground = true,
                    Name = "WiimoteTimingDiagnostics",
                    Priority = ThreadPriority.BelowNormal
                };
                _writerThread.Start();
            }
        }

        // [V70-BT-BURST] EN: consumer thread — owns the StreamWriter (batched writes, rotation).
        // FR: thread consommateur — propriétaire du StreamWriter (écrits groupés, rotation).
        private static void WriterLoop()
        {
            while (true)
            {
                try
                {
                    string line;
                    int lines = 0;
                    while (lines < FlushEveryLines && Queue.TryDequeue(out line))
                    {
                        Writer.WriteLine(line);
                        lines++;
                        _pending++;
                    }

                    if (lines > 0 || Queue.IsEmpty)
                    {
                        if (_pending > 0)
                        {
                            Writer.Flush();
                            _pending = 0;
                            RotateIfNeeded();
                        }
                    }

                    if (Queue.IsEmpty)
                    {
                        Wake.WaitOne(500);
                        if (Queue.IsEmpty && _writerStopping) return;
                    }
                }
                catch { /* Diagnostics must never crash anything. */ }
            }
        }

        // [V29] 2MB rotation: rename the current file to WiimoteTimingDiagnostics.old.csv
        // (overwriting any previous .old) and let the next writer create a fresh file.
        // This bounds disk usage to ~4MB (2MB current + 2MB old) while keeping recent data
        // always available for support/debug. Called from the writer thread only.
        // (EN/FR: Rotation 2Mo : renomme le fichier courant en .old.csv (en écrasant l'ancien) ;
        //  le prochain writer crée un fichier neuf. Usage disque borné ~4Mo. Appelé par le thread
        //  writer uniquement.)
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
            _shutdown = true;
            _writerStopping = true;
            Wake.Set();
            if (_writerThread != null && _writerThread.IsAlive && !_writerThread.Join(1000))
            {
                try { _writerThread.Abort(); } catch { }
            }
            try
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                }
            }
            catch { }
            _writer = null;
            _writerThread = null;
            Enabled = false;
        }
    }
}
