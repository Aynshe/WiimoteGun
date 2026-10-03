using System;
using System.IO;
using System.Windows.Forms;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: [V55v] Writes a heartbeat file (WiimoteGun.heartbeat, next to the exe) every
    /// 5 seconds FROM THE UI THREAD (WinForms timer). The heartbeat stops when the UI
    /// thread stops pumping messages — i.e. exactly when the app is frozen ("not
    /// responding") — which is what the WiimoteGun Service CrashWatchdog detects to
    /// kill and restart the app as the interactive user (never admin).
    /// A background-thread timer would keep beating through a UI freeze and HIDE the
    /// hang, so the WinForms timer is a deliberate choice.
    /// FR: [V55v] Écrit un fichier de battement (WiimoteGun.heartbeat, à côté de l'exe)
    /// toutes les 5 secondes DEPUIS LE THREAD UI (timer WinForms). Le battement s'arrête
    /// quand le thread UI ne pompe plus les messages — c'est-à-dire exactement quand
    /// l'app est figée (« ne répond pas ») — ce que le CrashWatchdog du service
    /// WiimoteGun détecte pour tuer puis relancer l'app en tant qu'utilisateur
    /// interactif (jamais admin). Un timer sur thread d'arrière-plan continuerait de
    /// battre pendant un gel UI et MASQUERAIT le problème : le timer WinForms est un
    /// choix délibéré.
    /// </summary>
    public static class AppHeartbeat
    {
        private const string HeartbeatFileName = "WiimoteGun.heartbeat";
        private static Timer _timer;
        private static string _path;

        /// <summary>
        /// EN: Start the heartbeat (call once on the UI thread, before Application.Run).
        /// FR: Démarrer le battement (appeler une fois sur le thread UI, avant Application.Run).
        /// </summary>
        public static void Start()
        {
            if (_timer != null) return;

            try
            {
                _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HeartbeatFileName);
            }
            catch
            {
                return;
            }

            // EN/FR: [V55v] WinForms timer: ticks ONLY while the UI thread pumps
            // messages. A frozen UI stops the beats — the exact signal the service
            // watchdog monitors.
            _timer = new Timer();
            _timer.Interval = 5000;
            _timer.Tick += (s, e) => WriteBeat();
            WriteBeat(); // First beat immediately (so the service sees a fresh file at registration)
            _timer.Start();
            SimpleLogger.Instance.Info("[Heartbeat] Started (UI-thread pulse every 5s -> " + _path + ")");
        }

        private static void WriteBeat()
        {
            try
            {
                // EN/FR: Content is irrelevant — the file's LastWriteTimeUtc IS the beat.
                // WriteAllText truncates+rewrites, updating the timestamp atomically enough.
                File.WriteAllText(_path, DateTime.UtcNow.Ticks.ToString());
            }
            catch
            {
                // EN/FR: Never let a heartbeat write disturb the app (read-only dir, etc.)
            }
        }
    }
}
