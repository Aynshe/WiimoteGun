using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace WiimoteGun
{
    /// <summary>
    /// Monitors Dolphin and Cemu emulator processes to avoid Wiimote conflicts
    /// (EN/FR: Surveille les processus Dolphin et Cemu pour éviter les conflits Wiimote)
    /// </summary>
    public class EmulatorProcessMonitor : IDisposable
    {
        private Thread _watchThread;
        private ManualResetEvent _stopEvent;
        private bool _emulatorWasRunning = false;
        private readonly string[] _emulatorNames = { "Dolphin", "Cemu" };

        public event EventHandler EmulatorStarted;
        public event EventHandler EmulatorStopped;

        /// <summary>
        /// Check if any monitored emulator is currently running
        /// (EN/FR: Vérifier si un émulateur surveillé est en cours d'exécution)
        /// </summary>
        public bool IsEmulatorRunning()
        {
            try
            {
                foreach (var name in _emulatorNames)
                {
                    // Check options before detecting (EN/FR: Vérifier options avant détection)
                    if (name == "Dolphin" && !Options.Instance.RestartOnDolphin) continue;
                    if (name == "Cemu" && !Options.Instance.RestartOnCemu) continue;

                    var processes = Process.GetProcessesByName(name);
                    if (processes.Length > 0)
                    {
                        SimpleLogger.Instance.Info($"Emulator detected: {name}.exe");
                        foreach (var p in processes) p.Dispose();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"EmulatorProcessMonitor: Error checking processes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start monitoring for emulator process changes
        /// (EN/FR: Démarrer la surveillance des changements de processus émulateur)
        /// </summary>
        public void StartMonitoring()
        {
            if (_watchThread != null)
                return;

            _emulatorWasRunning = IsEmulatorRunning();
            _stopEvent = new ManualResetEvent(false);

            _watchThread = new Thread(MonitorLoop)
            {
                IsBackground = true,
                Name = "EmulatorProcessMonitor"
            };
            _watchThread.Start();

            SimpleLogger.Instance.Info("EmulatorProcessMonitor: Monitoring started");
        }

        /// <summary>
        /// Stop monitoring thread
        /// (EN/FR: Arrêter le thread de surveillance)
        /// </summary>
        public void StopMonitoring()
        {
            if (_watchThread == null)
                return;

            _stopEvent?.Set();
            if (!_watchThread.Join(2000))
            {
                SimpleLogger.Instance.Warning("EmulatorProcessMonitor: Thread did not stop gracefully");
            }

            _watchThread = null;
            _stopEvent?.Dispose();
            _stopEvent = null;

            SimpleLogger.Instance.Info("EmulatorProcessMonitor: Monitoring stopped");
        }

        private void MonitorLoop()
        {
            try
            {
                while (!_stopEvent.WaitOne(1000)) // Check every second
                {
                    bool isRunningNow = IsEmulatorRunning();

                    // Detect emulator start
                    if (isRunningNow && !_emulatorWasRunning)
                    {
                        SimpleLogger.Instance.Info("EmulatorProcessMonitor: Emulator started");
                        _emulatorWasRunning = true;
                        EmulatorStarted?.Invoke(this, EventArgs.Empty);
                    }
                    // Detect emulator stop
                    else if (!isRunningNow && _emulatorWasRunning)
                    {
                        SimpleLogger.Instance.Info("EmulatorProcessMonitor: Emulator stopped");
                        _emulatorWasRunning = false;
                        EmulatorStopped?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"EmulatorProcessMonitor: Monitor loop error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
