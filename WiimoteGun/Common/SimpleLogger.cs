using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WiimoteGun
{
    public enum LogLevel
    {
        ALL = 0,
        TRACE = 1,
        DEBUG = 2,
        INFO = 3,
        WARNING = 4,
        ERROR = 5,
        FATAL = 6,
        NONE = 7
    }

    public class SimpleLogger
    {        
        private static SimpleLogger _instance;

        public static SimpleLogger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SimpleLogger();

                return _instance;
            }
        }

        public LogLevel Threshold { get; set; } = LogLevel.INFO;

        private void WriteFormattedLog(LogLevel level, string text)
        {
            if (Threshold == LogLevel.NONE) return;
            // threshold filtering logic (EN/FR: Logique de filtrage par seuil)
            if (level < Threshold && Threshold != LogLevel.ALL) return;

            string pretext;
            switch (level)
            {
                case LogLevel.TRACE:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [TRACE]   ";
                    break;
                case LogLevel.INFO:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [INFO]    ";
                    break;
                case LogLevel.DEBUG:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [DEBUG]   ";
                    break;
                case LogLevel.WARNING:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [WARNING] ";
                    break;
                case LogLevel.ERROR:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [ERROR]   ";
                    break;
                case LogLevel.FATAL:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [FATAL]   ";
                    break;
                default:
                    pretext = "";
                    break;
            }

            WriteLine(pretext + text);
        }

        private const string FILE_EXT = ".log";
        private readonly string datetimeFormat;
        private readonly string logFilename;

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        private SimpleLogger()
        {
            datetimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            logFilename = Path.ChangeExtension(System.Reflection.Assembly.GetEntryAssembly().Location, FILE_EXT);

            CheckRotation();
        }

        /// <summary>
        /// Log a DEBUG message
        /// </summary>
        /// <param name="text">Message</param>
        public void Debug(string text)
        {
            if (Threshold <= LogLevel.DEBUG)
                WriteFormattedLog(LogLevel.DEBUG, text);
        }

        /// <summary>
        /// Log an ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Error(string text)
        {
            if (Threshold <= LogLevel.ERROR)
                WriteFormattedLog(LogLevel.ERROR, text);
        }

        /// <summary>
        /// Log a FATAL ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Fatal(string text)
        {
            if (Threshold <= LogLevel.FATAL)
                WriteFormattedLog(LogLevel.FATAL, text);
        }

        /// <summary>
        /// Log an INFO message
        /// </summary>
        /// <param name="text">Message</param>
        public void Info(string text)
        {
            if (Threshold <= LogLevel.INFO)
                WriteFormattedLog(LogLevel.INFO, text);
        }

        /// <summary>
        /// Log a TRACE message
        /// </summary>
        /// <param name="text">Message</param>
        public void Trace(string text)
        {
            if (Threshold <= LogLevel.TRACE)
                WriteFormattedLog(LogLevel.TRACE, text);
        }

        /// <summary>
        /// Log a WARNING message
        /// </summary>
        /// <param name="text">Message</param>
        public void Warning(string text)
        {
            if (Threshold <= LogLevel.WARNING)
                WriteFormattedLog(LogLevel.WARNING, text);
        }

        private object _lock = new object();

        private void CheckRotation()
        {
            try
            {
                if (File.Exists(logFilename))
                {
                    // Rotate if log exceeds 1.5 MB (EN/FR: Rotation si log dépasse 1.5 Mo)
                    if (new FileInfo(logFilename).Length > 1.5 * 1024 * 1024)
                    {
                        string prevLog = logFilename + ".old";
                        try
                        {
                            if (File.Exists(prevLog))
                                File.Delete(prevLog);

                            File.Move(logFilename, prevLog);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void WriteLine(string text, bool append = true)
        {
            lock (_lock)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(text);

                    CheckRotation();

                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(logFilename, append, System.Text.Encoding.UTF8))
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            writer.WriteLine(text);
                        }
                    }
                }
                catch { }
            }
        }
    }
}
