using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WiimoteGun.Core.OutputRumble
{
    /// <summary>
    /// EN: [V62-OutputRumble] Local UDP receiver for OutputHooker game outputs. One datagram = one
    ///     output line, in one of two forms:
    ///       (a) friendly/test : "P<n> <EVENT> <value>"      e.g. "P1 FIRE 1", "P1 AMMO 12", "P2 DMG 3"
    ///       (b) OutputHooker  : "<raw output tag> <value>"  e.g. "mainpcb:Player1_Gun_Recoil 1"
    ///     Form (b) is resolved to (player, event) through the tag definitions file.
    /// FR: [V62-OutputRumble] Recepteur UDP local des outputs de jeu OutputHooker. Un datagramme =
    ///     une ligne, sous 2 formes : (a) test "P<n> EVENT valeur" ; (b) OutputHooker
    ///     "<tag brut> <valeur>", resolu via le fichier de definitions de tags.
    /// </summary>
    internal sealed class OutputHookServer : IDisposable
    {
        private UdpClient _udp;
        private Thread _thread;
        private volatile bool _running;
        private int _port;
        private int _lastDefsRefresh;
        private readonly OutputTagDefinitions _defs;
        private readonly ConcurrentDictionary<string, DateTime> _unknownLogged = new ConcurrentDictionary<string, DateTime>();

        /// <summary>EN: Raised on each resolved event (Phase 2 routes it to the controllers). FR: Leve pour chaque event resolu.</summary>
        public event Action<OutputResolvedEvent> EventReceived;

        public OutputHookServer(OutputTagDefinitions defs)
        {
            _defs = defs;
        }

        public bool IsRunning { get { return _running; } }
        public int Port { get { return _port; } }

        public bool Start(int port)
        {
            Stop();
            try
            {
                _port = port;
                var local = new IPEndPoint(IPAddress.Any, port);
                _udp = new UdpClient(local);
                try { _udp.Client.ReceiveBufferSize = 1 << 16; } catch { }
                _running = true;
                _thread = new Thread(Loop) { IsBackground = true, Name = "OutputHookServer" };
                _thread.Start();
                SimpleLogger.Instance.Info(string.Format(CultureInfo.InvariantCulture,
                    "[V62-OutputRumble] UDP receiver started on port {0} ({1} tag rule(s)).", port, _defs.RuleCount));
                return true;
            }
            catch (Exception ex)
            {
                _running = false;
                SimpleLogger.Instance.Warning(string.Format(CultureInfo.InvariantCulture,
                    "[V62-OutputRumble] UDP receiver start failed on port {0}: {1}", port, ex.Message));
                return false;
            }
        }

        public void Stop()
        {
            _running = false;
            try { _udp?.Close(); } catch { }
            _udp = null;
        }

        public void Dispose() { Stop(); }

        private void Loop()
        {
            while (_running)
            {
                if (_defs != null && (Environment.TickCount - _lastDefsRefresh) > 2000)
                {
                    _lastDefsRefresh = Environment.TickCount;
                    _defs.RefreshIfChanged();
                }

                IPEndPoint remote = null;
                byte[] data;
                try
                {
                    data = _udp.Receive(ref remote);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { if (_running) continue; else break; }
                catch (Exception) { if (_running) continue; else break; }

                if (data == null || data.Length == 0) continue;
                string msg = Encoding.UTF8.GetString(data).Trim();
                if (msg.Length == 0) continue;

                try
                {
                    OutputResolvedEvent ev = ParseAndResolve(msg);
                    if (ev != null)
                    {
                        var h = EventReceived;
                        if (h != null) h(ev);
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning("[V62-OutputRumble] Parse error for '" + msg + "': " + ex.Message);
                }
            }
        }

        private OutputResolvedEvent ParseAndResolve(string msg)
        {
            var tokens = msg.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return null;

            // 0) EN/FR: full-game control words (OutputHooker MameStart/MameStop). Tolerant to the
            // common 'GameOuput' spelling (missing the second 't'). Player 0 = broadcast.
            string head = tokens[0].Replace("_", "").Replace("-", "").ToUpperInvariant();
            if (head == "GAMEOUTPUTSTART" || head == "GAMEOUPUTSTART" || head == "GAMESTART" || head == "MAMESTART")
                return new OutputResolvedEvent { Player = 0, Event = OutputEvent.GameStart, Value = 1, TimestampUtc = DateTime.UtcNow };
            if (head == "GAMEOUTPUTEND" || head == "GAMEOUPUTEND" || head == "GAMEEND" || head == "MAMESTOP")
                return new OutputResolvedEvent { Player = 0, Event = OutputEvent.GameEnd, Value = 1, TimestampUtc = DateTime.UtcNow };

            // 1) Friendly/test semantic form: P<n> <EVENT> [value]
            if (tokens.Length >= 2 && tokens[0].Length == 2 &&
                (tokens[0][0] == 'P' || tokens[0][0] == 'p') && tokens[0][1] >= '1' && tokens[0][1] <= '4')
            {
                OutputEvent oe = ParseEventToken(tokens[1]);
                if (oe != OutputEvent.None)
                {
                    double v = 1;
                    if (tokens.Length >= 3) double.TryParse(tokens[tokens.Length - 1], NumberStyles.Any, CultureInfo.InvariantCulture, out v);
                    return new OutputResolvedEvent { Player = tokens[0][1] - '0', Event = oe, Value = v, TimestampUtc = DateTime.UtcNow };
                }
            }

            // 2) OutputHooker raw form: "<tag> <value>" -> resolve the tag
            if (tokens.Length >= 2)
            {
                string tag = tokens[0];
                double v = 0;
                double.TryParse(tokens[tokens.Length - 1], NumberStyles.Any, CultureInfo.InvariantCulture, out v);
                OutputTagRule rule;
                if (_defs != null && _defs.TryResolve(tag, out rule))
                    return new OutputResolvedEvent { Player = rule.Player, Event = rule.Event, Value = v, TimestampUtc = DateTime.UtcNow };
                LogUnknown(tag);
                return null;
            }

            // 3) Tag with no value (fire outputs may arrive as a single token)
            OutputTagRule r2;
            if (_defs != null && _defs.TryResolve(tokens[0], out r2))
                return new OutputResolvedEvent { Player = r2.Player, Event = r2.Event, Value = r2.Event == OutputEvent.Fire ? 1 : 0, TimestampUtc = DateTime.UtcNow };
            LogUnknown(tokens[0]);
            return null;
        }

        private void LogUnknown(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            DateTime last;
            if (_unknownLogged.TryGetValue(tag, out last) && (DateTime.UtcNow - last).TotalSeconds < 30) return;
            _unknownLogged[tag] = DateTime.UtcNow;
            SimpleLogger.Instance.Warning("[V62-OutputRumble] Unknown output tag (no rule) ignored: '" + tag + "'");
        }

        private static OutputEvent ParseEventToken(string s)
        {
            switch ((s ?? "").Trim().ToUpperInvariant())
            {
                case "FIRE": case "SHOT": case "RECOIL": return OutputEvent.Fire;
                case "AMMO": case "MAG": case "BULLETS": return OutputEvent.Ammo;
                case "RELOAD": return OutputEvent.Reload;
                case "DMG": case "DAMAGE": case "DAMAGED": case "HIT": return OutputEvent.Damage;
                case "GAMEOUTPUTSTART": case "GAMESTART": case "MAMESTART": return OutputEvent.GameStart;
                case "GAMEOUTPUTEND": case "GAMEEND": case "MAMESTOP": return OutputEvent.GameEnd;
                default: return OutputEvent.None;
            }
        }
    }
}
