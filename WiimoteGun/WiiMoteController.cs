using WiimoteLib;
using WiimoteLib.Events;
using WiimoteLib.DataTypes;
using System.Threading;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WiimoteGun
{
    class WiiMoteController : IDisposable
    {
        private WiiMoteMode _mode = WiiMoteMode.Mouse;
        private ScreenPositionCalculator _calculator;
        private IVirtualMouse _virtualMouse;
        private IVirtualJoy _joy;
        private int ticks = -1;
        private ButtonState _lastState;
        private NunchukState _lastNunchukState;
        private object _lock = new object();
        private WiimoteHiddenWnd _hiddenWnd;
        private Thread _watchDolphinThread;
        private AutoResetEvent _watchDolphinfinishEvent = new AutoResetEvent(false);
        private Process _runningProcess;
        private bool _processLocking;
        private PlayerMappings _playerMappings;

        // Auto-sleep after inactivity (EN/FR: Mise en veille automatique après inactivité)
        private DateTime _lastActivityTime = DateTime.Now;
        private System.Threading.Timer _sleepCheckTimer;
        private const int SLEEP_TIMEOUT_MINUTES = 10;

        public Wiimote Wiimote { get; }
        public int PlayerIndex { get; }
        public int ScreenIndex { get; set; }

        internal WiiMoteController(Wiimote wiimote, int playerIndex)
        {
            Wiimote = wiimote;
            PlayerIndex = playerIndex;
            ScreenIndex = Options.Instance.MonitorId;

            _lastState = new ButtonState();
            _lastNunchukState = new NunchukState();

            // Get player-specific mappings (EN/FR: Obtenir les mappings spécifiques au joueur)
            _playerMappings = Options.Instance.GetMappingsForPlayer(playerIndex);

            // Use Interception keyboard for independent player input (EN/FR: Utiliser le clavier Interception pour des entrées indépendantes)
            _joy = new VirtualInterceptionKeyboard(playerIndex);
            
            string uniqueId = Wiimote.Address.ToString().Replace(":", "");
            _virtualMouse = new VirtualInterceptionMouse(playerIndex, uniqueId);
            
            // Pass player index for per-player calibration (EN/FR: Passer l'index joueur pour calibration par joueur)
            _calculator = new ScreenPositionCalculator(ScreenIndex, PlayerIndex);

            SetupWiimote();

            _watchDolphinThread = new Thread(CheckDolphin);
            _watchDolphinThread.IsBackground = true;
            _watchDolphinThread.Start();

            // Start auto-sleep check timer (check every minute) (EN/FR: Démarrer le timer de vérification de mise en veille)
            _sleepCheckTimer = new System.Threading.Timer(_ => CheckSleep(), null, 60000, 60000);
        }

        private void CheckSleep()
        {
            try
            {
                if (Wiimote == null || !Wiimote.IsConnected)
                    return;

                double inactiveMinutes = (DateTime.Now - _lastActivityTime).TotalMinutes;
                
                if (inactiveMinutes >= SLEEP_TIMEOUT_MINUTES)
                {
                    SimpleLogger.Instance.Info($"Wiimote P{PlayerIndex} ({Wiimote.Address}) auto-sleep after {SLEEP_TIMEOUT_MINUTES} minutes of inactivity");
                    
                    // Disconnect to save battery (EN/FR: Déconnecter pour économiser la batterie)
                    Wiimote.Disconnect();
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error in CheckSleep: {ex.Message}");
            }
        }

        private void ResetSleepTimer()
        {
            _lastActivityTime = DateTime.Now;
        }

        private void SetupWiimote()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    // CRITICAL FIX: Clear any stuck rumble state (EN/FR: Arrêter la vibration bloquée)
                    Wiimote.SetRumble(false);
                    Thread.Sleep(50);

                    Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true);

                    // Force status check with timeout to prevent blocking (EN/FR: GetStatus avec timeout pour éviter le blocage)
                    if (Wiimote != null && Wiimote.IsConnected)
                    {
                        bool statusCompleted = false;
                        Exception statusException = null;

                        Thread statusThread = new Thread(() =>
                        {
                            try
                            {
                                if (Wiimote != null && Wiimote.IsConnected)
                                {
                                    Wiimote.GetStatus();
                                    statusCompleted = true;
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                SimpleLogger.Instance.Warning("Wiimote disconnected during GetStatus");
                            }
                            catch (Exception ex)
                            {
                                statusException = ex;
                            }
                        });

                        statusThread.IsBackground = true;
                        statusThread.Start();

                        // Wait up to 5 seconds for GetStatus (EN/FR: Attendre max 5 secondes)
                        if (!statusThread.Join(5000))
                        {
                            SimpleLogger.Instance.Error("GetStatus timed out after 5 seconds");

                            // Force rumble off even on timeout (EN/FR: Arrêter le rumble même en timeout)
                            try
                            {
                                if (Wiimote != null && Wiimote.IsConnected)
                                {
                                    Wiimote.SetRumble(false);
                                }
                            }
                            catch { }

                            return;
                        }

                        if (statusException != null)
                        {
                            SimpleLogger.Instance.Error("Error in GetStatus: " + statusException);

                            // Force rumble off on error (EN/FR: Arrêter le rumble en cas d'erreur)
                            try
                            {
                                if (Wiimote != null && Wiimote.IsConnected)
                                {
                                    Wiimote.SetRumble(false);
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    SimpleLogger.Instance.Warning("Wiimote disconnected during setup");
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("Error in SetupWiimote: " + ex);

                    // Always try to turn off rumble on error (EN/FR: Toujours arrêter le rumble en cas d'erreur)
                    try
                    {
                        if (Wiimote != null && Wiimote.IsConnected)
                        {
                            Wiimote.SetRumble(false);
                        }
                    }
                    catch { }
                }
            });


            bool led1 = PlayerIndex == 1;
            bool led2 = PlayerIndex == 2;
            bool led3 = PlayerIndex == 3;
            bool led4 = PlayerIndex == 4;
            Wiimote.SetLEDs(led1, led2, led3, led4);

            Wiimote.StateChanged += OnWiiMoteStateChanged;

            Program.Notify($"Wiimote P{PlayerIndex} connected");
            SimpleLogger.Instance.Info($"Wiimote P{PlayerIndex} connected with HID path: {Wiimote.DevicePath}");

            if (_hiddenWnd == null)
            {
                _hiddenWnd = new WiimoteHiddenWnd();
                Program.PostToUIThread(() =>
                {
                    if (_hiddenWnd == null) return;
                    _hiddenWnd.Create();
                    _hiddenWnd.SetMode(((int)_mode) + 1);
                });
            }

            Vibrate(Wiimote);
        }

        public void Dispose()
        {
            if (_watchDolphinThread != null)
            {
                _watchDolphinfinishEvent.Set();
                _watchDolphinThread.Join();
                _watchDolphinThread = null;
            }

            if (Wiimote != null)
            {
                Wiimote.StateChanged -= OnWiiMoteStateChanged;
            }

            if (_virtualMouse != null)
            {
                _virtualMouse.Dispose();
                _virtualMouse = null;
            }

            // Dispose sleep timer (EN/FR: Disposer le timer de veille)
            if (_sleepCheckTimer != null)
            {
                _sleepCheckTimer.Dispose();
                _sleepCheckTimer = null;
            }

            if (_hiddenWnd != null)
            {
                var wnd = _hiddenWnd;
                _hiddenWnd = null;
                Program.PostToUIThread(wnd.Dispose);
            }
        }

        private static void Vibrate(Wiimote wm)
        {
            Program.PostToUIThread(() =>
            {
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 350;
                timer.Tick += (a, b) =>
                {
                    try { wm.SetRumble(false); }
                    catch { }

                    try { timer.Dispose(); }
                    catch { }
                };

                timer.Start();

                try { wm.SetRumble(true); }
                catch { }
            });
        }

        private bool _lockUntilABreleased = false;

        private int _lastX = 0;
        private int _lastY = 0;

        private void OnWiiMoteStateChanged(object sender, WiimoteStateEventArgs e)
        {
            if (e.WiimoteState == null)
                return;

            lock (_lock)
            {
                ButtonState buttons = e.WiimoteState.Buttons;
                IRState ir = e.WiimoteState.IRState;

                // Reset sleep timer on any activity (EN/FR: Réinitialiser le timer de veille sur toute activité)
                // Activity is detected if any button is pressed, IR is detected, or nunchuk is moved
                bool hasActivity = buttons.A || buttons.B || buttons.Up || buttons.Down || buttons.Left || buttons.Right ||
                                   buttons.One || buttons.Two || buttons.Plus || buttons.Minus || buttons.Home ||
                                   ir.IRSensor0.Found || ir.IRSensor1.Found ||
                                   (e.WiimoteState.ExtensionType == ExtensionType.Nunchuk && 
                                    (e.WiimoteState.Nunchuk.C || e.WiimoteState.Nunchuk.Z || 
                                     Math.Abs(e.WiimoteState.Nunchuk.Joystick.X) > 0.15f || 
                                     Math.Abs(e.WiimoteState.Nunchuk.Joystick.Y) > 0.15f));
                
                if (hasActivity)
                    ResetSleepTimer();


                if (_runningProcess != null && _runningProcess.HasExited)
                {
                    if (_processLocking && _mode == WiiMoteMode.Mouse)
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            try { e.Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true); }
                            catch { }
                        });
                    }
                    _processLocking = false;
                    _runningProcess = null;
                }

                if (_runningProcess != null && _processLocking)
                    return;
                    
                ManageCalibration(e.Wiimote, buttons, _lastState);

                NunchukState nunchuk = e.WiimoteState.Nunchuk;
                bool hasNunchuk = e.WiimoteState.ExtensionType == ExtensionType.Nunchuk;

                if (_mode == WiiMoteMode.Mouse)
                {
                    bool wasCalibrating = _calculator.IsCalibrating;
                    var scaledPos = _calculator.GetScaledPosition(ir, buttons, _lastState);

                    int x = _lastX;
                    int y = _lastY;

                    if (scaledPos.HasValue)
                    {
                        x = (int)scaledPos.Value.X;
                        y = (int)scaledPos.Value.Y;
                        _lastX = x;
                        _lastY = y;
                    }

                    if (wasCalibrating || _calculator.IsCalibrating)
                    {
                        if (scaledPos.HasValue) // Only update if we have a valid position during calibration
                        {
                            _virtualMouse.UpdateMouse(x, y, false, false, false, true);
                            _lockUntilABreleased = true;
                        }
                    }
                    else
                    {
                        if (_lockUntilABreleased && !buttons.B && !buttons.A)
                            _lockUntilABreleased = false;

                        if (!_lockUntilABreleased)
                        {
                            bool left = isButtonPressed(SpecialAction.LeftMouse, buttons, nunchuk, hasNunchuk);
                            bool right = isButtonPressed(SpecialAction.RightMouse, buttons, nunchuk, hasNunchuk);
                            bool middle = isButtonPressed(SpecialAction.MiddleMouse, buttons, nunchuk, hasNunchuk);

                            // Pass scaledPos.HasValue as moveCursor argument
                            // If false, mouse cursor won't move, but buttons will still work (e.g. for off-screen reload)
                            // FR: Si false, le curseur ne bouge pas, mais les boutons fonctionnent (ex: rechargement hors écran)
                            _virtualMouse.UpdateMouse(x, y, left, right, middle, scaledPos.HasValue);
                        }
                    }
                }

                if ((_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Keyboardpad) && _joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                {
                    SendKeyEvent(_playerMappings.WiiA, buttons.A, _lastState.A);
                    SendKeyEvent(_playerMappings.WiiB, buttons.B, _lastState.B);
                    SendKeyEvent(_playerMappings.WiiUp, buttons.Up, _lastState.Up);
                    SendKeyEvent(_playerMappings.WiiDown, buttons.Down, _lastState.Down);
                    SendKeyEvent(_playerMappings.WiiLeft, buttons.Left, _lastState.Left);
                    SendKeyEvent(_playerMappings.WiiRight, buttons.Right, _lastState.Right);
                    SendKeyEvent(_playerMappings.WiiOne, buttons.One, _lastState.One);
                    SendKeyEvent(_playerMappings.WiiTwo, buttons.Two, _lastState.Two);
                    SendKeyEvent(_playerMappings.WiiPlus, buttons.Plus, _lastState.Plus);
                    SendKeyEvent(_playerMappings.WiiMinus, buttons.Minus, _lastState.Minus);

                    if (hasNunchuk)
                    {
                        SendKeyEvent(_playerMappings.NunC, nunchuk.C, _lastNunchukState.C);
                        SendKeyEvent(_playerMappings.NunZ, nunchuk.Z, _lastNunchukState.Z);
                        SendKeyEvent(_playerMappings.NunUp, nunchuk.Joystick.Y > 0.3f, _lastNunchukState.Joystick.Y > 0.3f);
                        SendKeyEvent(_playerMappings.NunDown, nunchuk.Joystick.Y < -0.3f, _lastNunchukState.Joystick.Y < -0.3f);
                        SendKeyEvent(_playerMappings.NunLeft, nunchuk.Joystick.X < -0.3f, _lastNunchukState.Joystick.X < -0.3f);
                        SendKeyEvent(_playerMappings.NunRight, nunchuk.Joystick.X > 0.3f, _lastNunchukState.Joystick.X > 0.3f);
                    }

                    _joy.CommitChanges();
                }

                _lastState = e.WiimoteState.Buttons;
                if (hasNunchuk)
                    _lastNunchukState = e.WiimoteState.Nunchuk;
            }
        }

        private void SendKeyEvent(ButtonAction action, bool pressed, bool lastPressed)
        {
            if (pressed == lastPressed || action.Special != SpecialAction.None)
                return;

            _joy.SendKeyEvent(action.Key, pressed);
        }

        private bool isButtonPressed(SpecialAction action, ButtonState buttons, NunchukState nunchuk, bool hasNunchuk)
        {
            if (_playerMappings.WiiA.Special == action && buttons.A) return true;
            if (_playerMappings.WiiB.Special == action && buttons.B) return true;
            if (_playerMappings.WiiUp.Special == action && buttons.Up) return true;
            if (_playerMappings.WiiDown.Special == action && buttons.Down) return true;
            if (_playerMappings.WiiLeft.Special == action && buttons.Left) return true;
            if (_playerMappings.WiiRight.Special == action && buttons.Right) return true;
            if (_playerMappings.WiiOne.Special == action && buttons.One) return true;
            if (_playerMappings.WiiTwo.Special == action && buttons.Two) return true;
            if (_playerMappings.WiiPlus.Special == action && buttons.Plus) return true;
            if (_playerMappings.WiiMinus.Special == action && buttons.Minus) return true;

            if (hasNunchuk)
            {
                if (_playerMappings.NunC.Special == action && nunchuk.C) return true;
                if (_playerMappings.NunZ.Special == action && nunchuk.Z) return true;
                if (_playerMappings.NunUp.Special == action && nunchuk.Joystick.Y > 0.3f) return true;
                if (_playerMappings.NunDown.Special == action && nunchuk.Joystick.Y < -0.3f) return true;
                if (_playerMappings.NunLeft.Special == action && nunchuk.Joystick.X < -0.3f) return true;
                if (_playerMappings.NunRight.Special == action && nunchuk.Joystick.X > 0.3f) return true;
            }

            return false;
        }

        private void SwitchMode(Wiimote wiimote)
        {
            int mode = (int)_mode;
            mode++;

            if (mode > (int)WiiMoteMode.Disabled)
                mode = 0;

            _mode = (WiiMoteMode)mode;

            if (_hiddenWnd != null)
                _hiddenWnd.SetMode(((int)_mode) + 1);

            if (_mode == WiiMoteMode.Mouse)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    try { wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true); }
                    catch { }
                });
            }

            SimpleLogger.Instance.Info($"Wiimote P{PlayerIndex} set to mode : {_mode}");

            if (_mode == WiiMoteMode.Disabled)
                Program.Notify($"WiimoteGun P{PlayerIndex} disabled");
            else
                Program.Notify($"WiimoteGun P{PlayerIndex} {_mode} activated");
        }

        private void ManageCalibration(Wiimote wiimote, ButtonState buttons, ButtonState lastState)
        {
            if (_calculator.IsCalibrating)
                return;

            if (lastState.Home != buttons.Home)
            {
                if (buttons.Home && ticks < 0)
                    ticks = Environment.TickCount;
                else if (!buttons.Home && ticks > 0)
                {
                    SwitchMode(wiimote);
                    ticks = -1;
                }
            }
            else if (buttons.Home && ticks > 0 && Environment.TickCount - ticks >= 1000)
            {
                ticks = -1;

                if (_mode == WiiMoteMode.Mouse)
                    _calculator.Calibrate();
            }                        
        }

        private Process GetDolphinProcess(out bool locks)
        {
            locks = false;
            var list = Process.GetProcesses().ToList();

            Process px = list.FirstOrDefault(p => "dolphin".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                locks = true;
                return px;
            }

            px = list.FirstOrDefault(p => "retroarch".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                var commandLine = px.GetProcessCommandline();
                if (!string.IsNullOrEmpty(commandLine))
                    locks = commandLine.Contains("dolphin_libretro.dll");
                return px;
            }

            px = list.FirstOrDefault(p => "cemu".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                locks = true;
                return px;
            }

            return null;
        }

        private void CheckDolphin()
        {
            while (true)
            {
                if (_watchDolphinfinishEvent.WaitOne(100))
                    break;

                if (_runningProcess == null)
                    _runningProcess = GetDolphinProcess(out _processLocking);
            }
        }
    }

    enum WiiMoteMode
    {
        Mouse = 0,
        Keyboardpad = 1,
        Disabled = 2
    }
}
