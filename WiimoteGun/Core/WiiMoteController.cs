using WiimoteLib;
using WiimoteLib.Events;
using WiimoteLib.DataTypes;
using WiimoteLib.Geometry;
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

        // Weapon recoil rumble (EN/FR: Vibration recul arme)
        private System.Threading.Timer _rumbleTimer;
        private bool _isTriggerPressed = false;
        private bool _isRumbling = false;
        private DateTime _lastRumbleTime = DateTime.MinValue;
        private bool _hasIRSensor = false; // Track if LEDs are visible (EN/FR: Suivi si LEDs sont visibles)

        // Gesture State (EN/FR: État des gestes)
        private DateTime _lastShakeTime = DateTime.MinValue;
        private bool _isShaking = false; // Track shake state (EN/FR: Suivi état secousse)
        private DateTime _lastGrenadeTime = DateTime.MinValue;
        
        // Off-screen Reload state tracking (EN/FR: Suivi d'état rechargement hors écran)
        private bool _wasOnScreen = true;
        private int _offScreenReloadClickSequence = 0; // 0=idle, 1-4=sending 2 clicks
        
        // Gesture Click Frame Counters (EN/FR: Compteurs de frames pour clics gestuels)
        private int _gestureRightClickFrameCount = 0;
        private int _gestureMiddleClickFrameCount = 0;
        private const int GESTURE_CLICK_DURATION_FRAMES = 6; // Reverted to ~100ms (6 frames) for reliability

        // Cooldowns (EN/FR: Délais de récupération)
        const int SHAKE_COOLDOWN_MS = 500;
        const int GRENADE_COOLDOWN_MS = 1000;
        
        // Startup safety (EN/FR: Sécurité au démarrage)
        private DateTime _controllerStartTime = DateTime.Now;
        private const int STARTUP_GRACE_PERIOD_MS = 2000; // Ignore gestures for 2s after start

        // ... existing code ...

        private bool CheckShake(WiimoteState state)
        {
            if (!Options.Instance.EnableDevGestures) return false;
            if (!Options.Instance.EnableShakeReload) return false;
            
            // Ignore gestures during startup grace period (EN/FR: Ignorer gestes pendant période de grâce)
            if ((DateTime.Now - _controllerStartTime).TotalMilliseconds < STARTUP_GRACE_PERIOD_MS) return false;

            // Get acceleration (EN/FR: Obtenir accélération)
            Point3F accel = Options.Instance.ShakeFromNunchuk && state.ExtensionType == ExtensionType.Nunchuk 
                ? state.Nunchuk.Accel.Values 
                : state.Accel.Values;

            // Calculate magnitude (approximate) (EN/FR: Calculer magnitude)
            double magnitude = Math.Sqrt(accel.X * accel.X + accel.Y * accel.Y + accel.Z * accel.Z);
            
            // Thresholds: Low=2.8g, Medium=2.0g, High=1.5g
            double threshold = 2.0;
            switch (Options.Instance.ShakeSensitivity)
            {
                case 0: threshold = 2.8; break; // Low sensitivity
                case 1: threshold = 2.0; break; // Medium
                case 2: threshold = 1.5; break; // High sensitivity
            }

            // State Machine for Shake Detection (EN/FR: Machine à états pour détection secousse)
            bool triggered = false;

            if (_isShaking)
            {
                // If currently shaking, wait for return to rest (hysteresis)
                // (EN/FR: Si en cours de secousse, attendre retour au repos)
                double resetThreshold = 1.3; // Increased to 1.3g to be more forgiving
                
                // Force reset if shaking for too long (> 1s) - prevents getting stuck
                // (EN/FR: Reset forcé si secousse trop longue (> 1s) - évite blocage)
                bool timeOut = (DateTime.Now - _lastShakeTime).TotalMilliseconds > 1000;

                if (magnitude < resetThreshold || timeOut)
                {
                    _isShaking = false;
                    // SimpleLogger.Instance.Debug($"Shake reset (Mag: {magnitude:F2}, Timeout: {timeOut})");
                }
            }
            else
            {
                // If not shaking, check for trigger threshold
                // (EN/FR: Si pas de secousse, vérifier seuil déclenchement)
                if (magnitude > threshold)
                {
                    _isShaking = true;
                    
                    // Check cooldown only for firing the event
                    if ((DateTime.Now - _lastShakeTime).TotalMilliseconds > SHAKE_COOLDOWN_MS)
                    {
                        _lastShakeTime = DateTime.Now;
                        triggered = true;
                        SimpleLogger.Instance.Info($"Shake fired! Mag: {magnitude:F2} > {threshold}");
                    }
                }
            }

            return triggered;
        }
        private System.Collections.Generic.Queue<float> _accelZHistory = new System.Collections.Generic.Queue<float>();
        private const int ACCEL_HISTORY_SIZE = 20; // Approx 20 samples (depends on report rate)

        // Gyroscope Aiming Mode (EN/FR: Mode visée gyroscopique)
        private bool _gyroAimingEnabled = false; // Current player has gyro enabled (EN/FR: Joueur actuel a gyro activé)
        private float _lastGyroYaw = 0f;   // Last Yaw value in °/s or accel delta (EN/FR: Dernière valeur Yaw en °/s ou delta accel)
        private float _lastGyroPitch = 0f; // Last Pitch value in °/s or accel delta (EN/FR: Dernière valeur Pitch en °/s ou delta accel)
        private float _lastGyroRoll = 0f;  // Last Roll value in °/s (EN/FR: Dernière valeur Roll en °/s)
        
        // Previous accelerometer values for delta calculation (EN/FR: Valeurs accéléromètre précédentes pour calcul delta)
        private float _lastAccelX = 0f;
        private float _lastAccelY = 0f;
        private float _lastAccelZ = 0f;
        
        // Gyroscope smoothing (EN/FR: Lissage gyroscope)
        private System.Collections.Generic.Queue<float> _gyroYawHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _gyroPitchHistory = new System.Collections.Generic.Queue<float>();
        private const int GYRO_SMOOTHING_SAMPLES = 3; // Number of frames to average (EN/FR: Nombre de frames à moyenner)
        
        // Gyroscope drift detection (EN/FR: Détection dérive gyroscope)
        private float _gyroDriftYaw = 0f;
        private float _gyroDriftPitch = 0f;
        private DateTime _lastGyroStillTime = DateTime.Now;
        private const float GYRO_STILL_THRESHOLD = 0.5f; // °/s threshold to consider "still" (EN/FR: Seuil °/s pour considérer "immobile")
        private const float ACCEL_SENSITIVITY = 50f; // Multiplier for accelerometer delta (EN/FR: Multiplicateur pour delta accéléromètre)
        private bool _gyroFirstRun = true; // Track first run for logging (EN/FR: Suivi premier run pour logging)

        // Hybrid Tracking Mode (EN/FR: Mode tracking hybride)
        private bool _useGyroForTracking = false; // Currently using gyro for cursor movement (EN/FR: Utilise actuellement gyro pour mouvement curseur)
        private DateTime _lastIRSeenTime = DateTime.Now; // Last time IR was valid (EN/FR: Dernière fois que l'IR était valide)
        private const float IR_LOST_TIMEOUT_MS = 100f; // Time before switching to gyro (EN/FR: Temps avant basculement vers gyro)

        // Button assignment mode (EN/FR: Mode assignation bouton)
        private static bool _inputsLocked = false; // Locks all controllers input (EN/FR: Verrouille inputs de tous contrôleurs)
        public static event EventHandler<ButtonPressedEventArgs> ButtonPressed; // Fired when button is pressed in assign mode (EN/FR: Déclenché quand bouton pressé en mode assign)
        
        public Wiimote Wiimote { get; }
        public int PlayerIndex { get; }
        public int ScreenIndex { get; set; }
        public ScreenPositionCalculator Calculator { get { return _calculator; } }
        public IVirtualMouse VirtualMouse { get { return _virtualMouse; } } // Public accessor for mouse (EN/FR: Accesseur public pour la souris)
        public IVirtualJoy VirtualJoy { get { return _joy; } } // Public accessor for keyboard/joy (EN/FR: Accesseur public pour clavier/joy)

        internal WiiMoteController(Wiimote wiimote, int playerIndex)
        {
            Wiimote = wiimote;
            PlayerIndex = playerIndex;
            ScreenIndex = Options.Instance.MonitorId;

            // Enable Virtual Driver via Service (if in RawInput mode)
            if (Options.Instance.DefaultMouseMode == MouseMode.RawInput)
            {
                 ServiceClient.EnablePlayer(PlayerIndex);
                 // Allow sufficient time (2s) for pnputil to execute and device to enumerate
                 System.Threading.Thread.Sleep(2000); 
            }

            _lastState = new ButtonState();
            _lastNunchukState = new NunchukState();

            // Get player-specific mappings (EN/FR: Obtenir les mappings spécifiques au joueur)
            _playerMappings = Options.Instance.GetMappingsForPlayer(playerIndex);

            // Select keyboard implementation based on configuration (EN/FR: Sélectionner implémentation clavier selon configuration)
            if (Options.Instance.DefaultMouseMode == MouseMode.SendInput)
            {
                // SendInput mode: Use simple SendInput keyboard (EN/FR: Mode SendInput : Utiliser clavier SendInput simple)
                _joy = new VirtualSendInputKeyboard();
                SimpleLogger.Instance.Info($"P{playerIndex}: Using SendInput keyboard");
            }
            else
            {
                // RawInput mode: Use Interception keyboard (EN/FR: Mode RawInput : Utiliser clavier Interception)
                _joy = new VirtualInterceptionKeyboard(playerIndex);
                SimpleLogger.Instance.Info($"P{playerIndex}: Using Interception keyboard");
            }
            
            // Select mouse implementation based on configuration (EN/FR: Sélectionner implémentation souris selon configuration)
            string uniqueId = Wiimote.Address.ToString().Replace(":", "");
            
            if (Options.Instance.DefaultMouseMode == MouseMode.SendInput)
            {
                // SendInput mode: Only Player 1 gets a mouse (EN/FR: Mode SendInput : Seul Joueur 1 a une souris)
                if (playerIndex == 1)
                {
                    _virtualMouse = new VirtualSendInputMouse();
                    SimpleLogger.Instance.Info($"P{playerIndex}: Using SendInput mouse (single-player mode)");
                    
                    // Subscribe to left mouse button events for rumble (EN/FR: S'abonner aux événements bouton gauche pour vibration)
                    if (_virtualMouse is VirtualSendInputMouse sendInputMouse)
                    {
                        sendInputMouse.OnLeftMouseButtonChanged += HandleTriggerButton;
                    }
                }
                else
                {
                    SimpleLogger.Instance.Warning($"P{playerIndex}: SendInput mode only supports Player 1. Mouse disabled for this player.");
                    _virtualMouse = null; // No mouse for P2-P4 in SendInput mode
                }
            }
            else // MouseMode.RawInput
            {
                // RawInput/Interception mode: All players get independent mice (EN/FR: Mode RawInput : Tous les joueurs ont des souris indépendantes)
                _virtualMouse = new VirtualInterceptionMouse(playerIndex, uniqueId);
                SimpleLogger.Instance.Info($"P{playerIndex}: Using Interception mouse (multi-player mode)");
                
                // Subscribe to left mouse button events for rumble (EN/FR: S'abonner aux événements bouton gauche pour vibration)
                if (_virtualMouse is VirtualInterceptionMouse interceptionMouse)
                {
                    interceptionMouse.OnLeftMouseButtonChanged += HandleTriggerButton;
                }
            }
            
            // Pass player index for per-player calibration (EN/FR: Passer l'index joueur pour calibration par joueur)
            _calculator = new ScreenPositionCalculator(ScreenIndex, PlayerIndex);

            SetupWiimote();

            _watchDolphinThread = new Thread(CheckDolphin);
            _watchDolphinThread.IsBackground = true;
            _watchDolphinThread.Start();

            // Start auto-sleep check timer (check every minute) (EN/FR: Démarrer le timer de vérification de mise en veille)
            _sleepCheckTimer = new System.Threading.Timer(_ => CheckSleep(), null, 60000, 60000);
            
            // Initialize rumble timer (disabled by default) (EN/FR: Initialiser timer vibration (désactivé par défaut))
            _rumbleTimer = new System.Threading.Timer(_ => RumbleRepetitionCallback(), null, Timeout.Infinite, Timeout.Infinite);
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

                    // Apply IR Sensitivity from Options (EN/FR: Appliquer sensibilité IR depuis Options)
                    // Mapping: 0=Level1, 1=Level2, 2=Level3, 3=Level4, 4=Level5, 5=Maximum
                    IRSensitivity sensitivity = (IRSensitivity)Options.Instance.IRSensitivity;
                    Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, sensitivity, true);

                    // Enable MotionPlus for gyroscope tracking (EN/FR: Activer MotionPlus pour tracking gyroscope)
                    // Required for gyroscope aiming mode to work (EN/FR: Nécessaire pour mode visée gyroscopique)
                    try
                    {
                        // Detect extension type and enable MotionPlus accordingly (EN/FR: Détecter type extension et activer MotionPlus)
                        Thread.Sleep(100); // Wait for extension detection
                        
                        if (Wiimote.WiimoteState.ExtensionType == ExtensionType.Nunchuk)
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
                            SimpleLogger.Instance.Info($"MotionPlus enabled with Nunchuk for Player {PlayerIndex}");
                        }
                        else
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.NoExtension);
                            SimpleLogger.Instance.Info($"MotionPlus enabled (no extension) for Player {PlayerIndex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Warning($"Failed to enable MotionPlus for Player {PlayerIndex}: {ex.Message}");
                    }

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

                        // Wait for status with timeout (EN/FR: Attendre le statut avec timeout)
                        if (!statusThread.Join(2000))
                        {
                            SimpleLogger.Instance.Warning("GetStatus timeout, continuing anyway");
                        }
                        else if (statusException != null)
                        {
                            SimpleLogger.Instance.Error($"GetStatus exception: {statusException.Message}");

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
                        else if (statusCompleted)
                        {
                            SimpleLogger.Instance.Info("GetStatus completed successfully");
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

            // Display connection notification with mouse mode (EN/FR: Afficher notification connexion avec mode souris)
            string mouseMode = Options.Instance.DefaultMouseMode == MouseMode.SendInput ? "SendInput (Legacy)" : "RawInput/Interception";
            Program.Notify($"Wiimote P{PlayerIndex} connected - {mouseMode}");
            SimpleLogger.Instance.Info($"Wiimote P{PlayerIndex} connected with HID path: {Wiimote.DevicePath}");

            if (_hiddenWnd == null && Options.Instance.DefaultMouseMode == MouseMode.SendInput)
            {
                _hiddenWnd = new WiimoteHiddenWnd();
                Program.PostToUIThread(() =>
                {
                    if (_hiddenWnd == null) return;
                    _hiddenWnd.Create();
                    _hiddenWnd.SetMode(((int)_mode) + 1);
                });
            }
            else if (_hiddenWnd != null && Options.Instance.DefaultMouseMode == MouseMode.RawInput)
            {
                var wnd = _hiddenWnd;
                _hiddenWnd = null;
                Program.PostToUIThread(wnd.Dispose);
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

            // Disable Virtual Driver via Service
            if (Options.Instance.DefaultMouseMode == MouseMode.RawInput)
            {
                ServiceClient.DisablePlayer(PlayerIndex);
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

        /// <summary>
        /// Process gyroscope/accelerometer data (EN/FR: Traiter données gyroscope/accéléromètre)
        /// Supports both MotionPlus (precise) and Accelerometer fallback (compatible with all Wiimotes)
        /// </summary>
        private void ProcessGyroscopeData(WiimoteState state)
        {
            // Check if gyro aiming is enabled for current player (EN/FR: Vérifier si visée gyro activée pour joueur actuel)
            if (_playerMappings != null)
            {
                _gyroAimingEnabled = _playerMappings.EnableGyroAiming;
            }

            // Skip if gyro not enabled (EN/FR: Ignorer si gyro non activé)
            if (!_gyroAimingEnabled)
                return;

            // Detect available motion tracking method (EN/FR: Détecter méthode de tracking disponible)
            bool hasMotionPlus = (state.ExtensionType == ExtensionType.MotionPlus || 
                                  state.ExtensionType == ExtensionType.MotionPlusNunchuk);

            float rawYaw = 0f;
            float rawPitch = 0f;
            float rawRoll = 0f;

            // Log tracking mode once (EN/FR: Logger mode de tracking une fois)
            if (_gyroFirstRun)
            {
                if (hasMotionPlus)
                {
                    SimpleLogger.Instance.Info($"P{PlayerIndex}: Using MotionPlus for gyro aiming (precise mode)");
                }
                else
                {
                    SimpleLogger.Instance.Info($"P{PlayerIndex}: Using Accelerometer fallback for gyro aiming (compatible mode)");
                }
                _gyroFirstRun = false;
            }

            if (hasMotionPlus)
            {
                // --- MODE 1: MotionPlus (Precise) (EN/FR: MotionPlus (Précis)) ---
                rawYaw = state.MotionPlus.Values.Yaw;
                rawPitch = state.MotionPlus.Values.Pitch;
                rawRoll = state.MotionPlus.Values.Roll;
            }
            else
            {
                // --- MODE 2: Accelerometer Fallback (Compatible) (EN/FR: Accéléromètre (Compatible)) ---
                // Use accelerometer delta to estimate rotation (EN/FR: Utiliser delta accéléromètre pour estimer rotation)
                float accelX = state.Accel.Values.X;
                float accelY = state.Accel.Values.Y;
                float accelZ = state.Accel.Values.Z;

                // Calculate delta from previous frame (EN/FR: Calculer delta depuis frame précédente)
                float deltaX = accelX - _lastAccelX;
                float deltaY = accelY - _lastAccelY;
                float deltaZ = accelZ - _lastAccelZ;

                // Store current for next frame (EN/FR: Stocker actuel pour prochaine frame)
                _lastAccelX = accelX;
                _lastAccelY = accelY;
                _lastAccelZ = accelZ;

                // Map accelerometer deltas to rotation estimates (EN/FR: Mapper deltas accéléromètre vers estimations rotation)
                // Note: This is less precise than gyroscope but works without MotionPlus!
                rawYaw = -deltaX * ACCEL_SENSITIVITY;   // X-axis tilt → Yaw (horizontal rotation)
                rawPitch = deltaY * ACCEL_SENSITIVITY;  // Y-axis tilt → Pitch (vertical rotation)
                rawRoll = deltaZ * ACCEL_SENSITIVITY;   // Z-axis tilt → Roll (not used for FPS)
            }

            // --- Common Processing (EN/FR: Traitement commun) ---

            // Add to smoothing history (EN/FR: Ajouter à l'historique de lissage)
            _gyroYawHistory.Enqueue(rawYaw);
            _gyroPitchHistory.Enqueue(rawPitch);

            // Keep only recent samples for smoothing (EN/FR: Garder seulement échantillons récents pour lissage)
            while (_gyroYawHistory.Count > GYRO_SMOOTHING_SAMPLES)
                _gyroYawHistory.Dequeue();
            while (_gyroPitchHistory.Count > GYRO_SMOOTHING_SAMPLES)
                _gyroPitchHistory.Dequeue();

            // Calculate smoothed values (moving average) (EN/FR: Calculer valeurs lissées (moyenne mobile))
            float smoothedYaw = _gyroYawHistory.Count > 0 ? _gyroYawHistory.Average() : 0f;
            float smoothedPitch = _gyroPitchHistory.Count > 0 ? _gyroPitchHistory.Average() : 0f;

            // Apply deadzone to filter out drift (EN/FR: Appliquer zone morte pour filtrer dérive)
            float deadzone = Options.Instance.GyroDeadzone;
            if (Math.Abs(smoothedYaw) < deadzone)
                smoothedYaw = 0f;
            if (Math.Abs(smoothedPitch) < deadzone)
                smoothedPitch = 0f;

            // Detect stillness for auto-calibration (EN/FR: Détecter immobilité pour auto-calibration)
            float totalMovement = Math.Abs(rawYaw) + Math.Abs(rawPitch) + Math.Abs(rawRoll);
            if (totalMovement < GYRO_STILL_THRESHOLD)
            {
                // If still for >2 seconds, update drift correction (EN/FR: Si immobile >2s, mettre à jour correction dérive)
                if ((DateTime.Now - _lastGyroStillTime).TotalSeconds > 2.0)
                {
                    _gyroDriftYaw = smoothedYaw;
                    _gyroDriftPitch = smoothedPitch;
                    // SimpleLogger.Instance.Debug($"Gyro drift calibration: Yaw={_gyroDriftYaw:F2}, Pitch={_gyroDriftPitch:F2}");
                }
            }
            else
            {
                _lastGyroStillTime = DateTime.Now;
            }

            // Apply drift correction (EN/FR: Appliquer correction dérive)
            smoothedYaw -= _gyroDriftYaw;
            smoothedPitch -= _gyroDriftPitch;

            // Store processed values (EN/FR: Stocker valeurs traitées)
            _lastGyroYaw = smoothedYaw;
            _lastGyroPitch = smoothedPitch;
            _lastGyroRoll = rawRoll; // Roll is usually not used for FPS aiming

            // Log for debugging (only when values are significant) (EN/FR: Logger pour débogage (seulement si valeurs significatives))
            // Note: Commented out to avoid log spam. Uncomment for debugging. (EN/FR: Commenté pour éviter spam logs. Décommenter pour débogage.)
            /*
            if (Math.Abs(smoothedYaw) > 1.0f || Math.Abs(smoothedPitch) > 1.0f)
            {
                string mode = _usingMotionPlus ? "MP" : "Accel";
                SimpleLogger.Instance.Debug($"Gyro[{mode}] P{PlayerIndex}: Yaw={smoothedYaw:F2}, Pitch={smoothedPitch:F2}");
            }
            */
        }

        private void OnWiiMoteStateChanged(object sender, WiimoteStateEventArgs e)
        {
            if (e.WiimoteState == null)
                return;

            lock (_lock)
            {
                ButtonState buttons = e.WiimoteState.Buttons;
                IRState ir = e.WiimoteState.IRState;

                // Read and process gyroscope data (EN/FR: Lire et traiter données gyroscope)
                ProcessGyroscopeData(e.WiimoteState);

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

                // Check if inputs are locked for button assignment (EN/FR: Vérifier si inputs verrouillés pour assignation)
                if (_inputsLocked)
                {
                    // In assignment mode: detect button press and fire event (EN/FR: En mode assignation : détecter pression bouton et déclencher événement)
                    DetectAndFireButtonEvent(buttons, e.WiimoteState);
                    return; // Don't process normal input (EN/FR: Ne pas traiter input normal)
                }

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

                    // --- HYBRID IR + GYRO TRACKING (EN/FR: Tracking hybride IR + Gyro) ---
                    bool hasValidIR = scaledPos.HasValue;
                    
                    // Get screen bounds for edge detection (EN/FR: Obtenir limites écran pour détection bord)
                    var screen = System.Windows.Forms.Screen.AllScreens[ScreenIndex];
                    int screenWidth = screen.Bounds.Width;
                    int screenHeight = screen.Bounds.Height;
                    const int EDGE_MARGIN = 50; // Pixels from edge to trigger gyro (EN/FR: Pixels du bord pour déclencher gyro)
                    
                    if (hasValidIR)
                    {
                        // IR is valid - use IR tracking (EN/FR: IR valide - utiliser tracking IR)
                        x = (int)scaledPos.Value.X;
                        y = (int)scaledPos.Value.Y;
                        
                        // Check if cursor is at screen edge (EN/FR: Vérifier si curseur est au bord écran)
                        bool atLeftEdge = x <= EDGE_MARGIN;
                        bool atRightEdge = x >= (screenWidth - EDGE_MARGIN);
                        bool atTopEdge = y <= EDGE_MARGIN;
                        bool atBottomEdge = y >= (screenHeight - EDGE_MARGIN);
                        bool atEdge = atLeftEdge || atRightEdge || atTopEdge || atBottomEdge;
                        
                        if (_gyroAimingEnabled && atEdge)
                        {
                            // Cursor at edge - ACTIVATE GYRO to allow continued rotation
                            // (EN/FR: Curseur au bord - ACTIVER GYRO pour rotation continue)
                            if (!_useGyroForTracking)
                            {
                                _useGyroForTracking = true;
                                string edgeDirection = atLeftEdge ? "Left" : atRightEdge ? "Right" : atTopEdge ? "Top" : "Bottom";
                                SimpleLogger.Instance.Info($"P{PlayerIndex}: Gyro activated (cursor at {edgeDirection} edge)");
                            }
                            
                            // Apply gyro movement (EN/FR: Appliquer mouvement gyro)
                            float sensitivityX = Options.Instance.GyroSensitivityX;
                            float sensitivityY = Options.Instance.GyroSensitivityY;
                            
                            // Calculate movement delta (EN/FR: Calculer delta de mouvement)
                            int deltaX = (int)(_lastGyroYaw * sensitivityX);
                            int deltaY = (int)(-_lastGyroPitch * sensitivityY); // Inverted for natural feel
                            
                            // Add delta to current position (EN/FR: Ajouter delta à position actuelle)
                            x = _lastX + deltaX;
                            y = _lastY + deltaY;
                            
                            // Clamp to screen bounds (EN/FR: Limiter aux bords de l'écran)
                            x = Math.Max(0, Math.Min(screenWidth - 1, x));
                            y = Math.Max(0, Math.Min(screenHeight - 1, y));
                            
                            // Log gyro tracking (uncomment for debugging) (EN/FR: Logger tracking gyro)
                            // SimpleLogger.Instance.Debug($"P{PlayerIndex} Gyro@Edge: ({x},{y}) delta=({deltaX},{deltaY})");
                        }
                        else
                        {
                            // Cursor NOT at edge - use normal IR tracking and disable gyro
                            // (EN/FR: Curseur PAS au bord - utiliser tracking IR normal et désactiver gyro)
                            if (_useGyroForTracking)
                            {
                                _useGyroForTracking = false;
                                SimpleLogger.Instance.Info($"P{PlayerIndex}: Gyro deactivated (cursor back in screen center)");
                            }
                        }
                        
                        _lastX = x;
                        _lastY = y;
                        _lastIRSeenTime = DateTime.Now;
                    }
                    else if (_gyroAimingEnabled)
                    {
                        // IR lost - fallback to gyro (EN/FR: IR perdu - basculer vers gyro)
                        double msSinceIRLost = (DateTime.Now - _lastIRSeenTime).TotalMilliseconds;
                        
                        if (msSinceIRLost > IR_LOST_TIMEOUT_MS)
                        {
                            // Use gyroscope for tracking (EN/FR: Utiliser gyroscope pour tracking)
                            if (!_useGyroForTracking)
                            {
                                _useGyroForTracking = true;
                                SimpleLogger.Instance.Info($"P{PlayerIndex}: Switched to Gyro tracking (IR lost)");
                            }

                            // Apply gyro movement (EN/FR: Appliquer mouvement gyro)
                            float sensitivityX = Options.Instance.GyroSensitivityX;
                            float sensitivityY = Options.Instance.GyroSensitivityY;

                            // Calculate movement delta (EN/FR: Calculer delta de mouvement)
                            int deltaX = (int)(_lastGyroYaw * sensitivityX);
                            int deltaY = (int)(-_lastGyroPitch * sensitivityY); // Inverted for natural feel

                            // Add delta to current position (EN/FR: Ajouter delta à position actuelle)
                            x = _lastX + deltaX;
                            y = _lastY + deltaY;

                            // Clamp to screen bounds (EN/FR: Limiter aux bords de l'écran)
                            x = Math.Max(0, Math.Min(screenWidth - 1, x));
                            y = Math.Max(0, Math.Min(screenHeight - 1, y));

                            _lastX = x;
                            _lastY = y;

                            // Log gyro tracking (uncomment for debugging) (EN/FR: Logger tracking gyro)
                            // SimpleLogger.Instance.Debug($"P{PlayerIndex} Gyro: ({x},{y}) delta=({deltaX},{deltaY})");
                        }
                    }
                    // --- END HYBRID TRACKING ---

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

            // --- GESTURE & RELOAD LOGIC ---

            // Track on-screen state for transition detection (EN/FR: Suivre l'état pour détecter les transitions)
            bool isOnScreen = scaledPos.HasValue;

            // 1. Off-screen Reload (EN/FR: Rechargement hors écran)
            if (Options.Instance.EnableOffScreenReload && !isOnScreen)
            {
                if (Options.Instance.OffScreenReloadAuto)
                {
                    // Automatic mode: Send 2 quick Right Clicks on transition (EN/FR: Mode auto : 2 clics rapides à la transition)
                    // Detect transition: on-screen → off-screen (EN/FR: Détecter transition : écran → hors écran)
                    if (_wasOnScreen)
                    {
                        _offScreenReloadClickSequence = 1; // Start click sequence
                    }
                    
                    // Execute 2-click sequence over 4 frames (EN/FR: Exécuter séquence de 2 clics sur 4 frames)
                    if (_offScreenReloadClickSequence > 0)
                    {
                        switch (_offScreenReloadClickSequence)
                        {
                            case 1: right = true; break;  // Click 1 Down
                            case 2: right = false; break; // Click 1 Up
                            case 3: right = true; break;  // Click 2 Down
                            case 4:
                                right = false;           // Click 2 Up
                                _offScreenReloadClickSequence = -1; // Will become 0 after ++
                                break;
                        }
                        _offScreenReloadClickSequence++;
                    }
                }
                else if (left)
                {
                    // On Click mode: Convert Left Click to Right Click (EN/FR: Mode clic : Convertir clic gauche en droit)
                    left = false;
                    right = true;
                }
            }
            
            // Update on-screen state for next frame (EN/FR: Mettre à jour l'état pour la prochaine frame)
            _wasOnScreen = isOnScreen;

                            // 2. Shake Reload (EN/FR: Rechargement par secousse)
                            if (CheckShake(e.WiimoteState))
                            {
                                _gestureRightClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                            }

                            // 3. Grenade Gesture (EN/FR: Geste Grenade)
                            if (CheckGrenadeGesture(e.WiimoteState))
                            {
                                _gestureMiddleClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                            }
                            
                            // Apply gesture clicks (EN/FR: Appliquer clics gestuels)
                            if (_gestureRightClickFrameCount > 0)
                            {
                                right = true;
                                _gestureRightClickFrameCount--;
                            }
                            
                            if (_gestureMiddleClickFrameCount > 0)
                            {
                                middle = true;
                                _gestureMiddleClickFrameCount--;
                            }
                            
                            // ------------------------------

                            // Pass scaledPos.HasValue as moveCursor argument
                            // If false, mouse cursor won't move, but buttons will still work (e.g. for off-screen reload)
                            // FR: Si false, le curseur ne bouge pas, mais les boutons fonctionnent (ex: rechargement hors écran)
                            
                            // Safety check: _virtualMouse might be null for Player 2+ in SendInput mode
                            // (EN/FR: Vérification sécurité : _virtualMouse peut être null pour Joueur 2+ en mode SendInput)
                            if (_virtualMouse != null)
                            {
                                _virtualMouse.UpdateMouse(x, y, left, right, middle, scaledPos.HasValue);
                            }
                            
                            // Update IR sensor status for rumble (EN/FR: Mettre à jour statut capteur IR pour vibration)
                            UpdateIRSensorStatus(scaledPos.HasValue);
                        }
                    }
                }

                if ((_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Keyboardpad) && _joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                {
                    // Mask inputs if Home is pressed (Hotkey mode) (EN/FR: Masquer inputs si Home pressé)
                    // This prevents standard actions from triggering while using hotkeys
                    bool homePressed = buttons.Home;

                    SendKeyEvent(_playerMappings.WiiA, buttons.A && !homePressed, _lastState.A);
                    SendKeyEvent(_playerMappings.WiiB, buttons.B && !homePressed, _lastState.B);
                    SendKeyEvent(_playerMappings.WiiUp, buttons.Up && !homePressed, _lastState.Up);
                    SendKeyEvent(_playerMappings.WiiDown, buttons.Down && !homePressed, _lastState.Down);
                    SendKeyEvent(_playerMappings.WiiLeft, buttons.Left && !homePressed, _lastState.Left);
                    SendKeyEvent(_playerMappings.WiiRight, buttons.Right && !homePressed, _lastState.Right);
                    SendKeyEvent(_playerMappings.WiiOne, buttons.One && !homePressed, _lastState.One);
                    SendKeyEvent(_playerMappings.WiiTwo, buttons.Two && !homePressed, _lastState.Two);
                    SendKeyEvent(_playerMappings.WiiPlus, buttons.Plus && !homePressed, _lastState.Plus);
                    SendKeyEvent(_playerMappings.WiiMinus, buttons.Minus && !homePressed, _lastState.Minus);

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

                // Hotkey detection: notify HotkeyManager of button presses/releases
                // (EN/FR: Détection hotkeys : notifier HotkeyManager des pressions/relâchements)
                DetectHotkeyButtonChanges(buttons, _lastState);

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

        /// <summary>
        /// Detect button changes and notify HotkeyManager for hotkey processing
        /// (EN/FR: Détecter changements de boutons et notifier HotkeyManager pour traitement hotkeys)
        /// </summary>
        private void DetectHotkeyButtonChanges(ButtonState currentState, ButtonState lastState)
        {
            // Check each button for state changes (EN/FR: Vérifier chaque bouton pour changements d'état)
            CheckButtonStateChange("Home", currentState.Home, lastState.Home);
            CheckButtonStateChange("A", currentState.A, lastState.A);
            CheckButtonStateChange("B", currentState.B, lastState.B);
            CheckButtonStateChange("One", currentState.One, lastState.One);
            CheckButtonStateChange("Two", currentState.Two, lastState.Two);
            CheckButtonStateChange("Plus", currentState.Plus, lastState.Plus);
            CheckButtonStateChange("Minus", currentState.Minus, lastState.Minus);
            CheckButtonStateChange("Up", currentState.Up, lastState.Up);
            CheckButtonStateChange("Down", currentState.Down, lastState.Down);
            CheckButtonStateChange("Left", currentState.Left, lastState.Left);
            CheckButtonStateChange("Right", currentState.Right, lastState.Right);
        }

        /// <summary>
        /// Helper to check individual button state change and notify HotkeyManager
        /// (EN/FR: Helper pour vérifier changement d'état d'un bouton et notifier HotkeyManager)
        /// </summary>
        private void CheckButtonStateChange(string buttonName, bool currentPressed, bool lastPressed)
        {
            if (currentPressed && !lastPressed)
            {
                // Button pressed (EN/FR: Bouton pressé)
                HotkeyManager.OnButtonPressed(PlayerIndex, buttonName);
            }
            else if (!currentPressed && lastPressed)
            {
                // Button released (EN/FR: Bouton relâché)
                HotkeyManager.OnButtonReleased(PlayerIndex, buttonName);
            }
        }


        private bool CheckGrenadeGesture(WiimoteState state)
        {
            if (!Options.Instance.EnableDevGestures) return false; // Dev feature locked (EN/FR: Fonctionnalité dev verrouillée)
            if (!Options.Instance.EnableGrenadeGesture) return false;
            if ((DateTime.Now - _lastGrenadeTime).TotalMilliseconds < GRENADE_COOLDOWN_MS) return false;

            // Monitor Y axis for "Pump" action from Wiimote or Nunchuk (EN/FR: Surveiller axe Y pour action "Pompe")
            float y;
            if (Options.Instance.GrenadeFromNunchuk && state.ExtensionType == ExtensionType.Nunchuk)
            {
                y = state.Nunchuk.Accel.Values.Y; // Use Nunchuk acceleration
            }
            else
            {
                y = state.Accel.Values.Y; // Use Wiimote acceleration
            }
            
            _accelZHistory.Enqueue(y); // Using _accelZHistory queue but storing Y values
            if (_accelZHistory.Count > ACCEL_HISTORY_SIZE)
                _accelZHistory.Dequeue();

            // Need at least 10 samples
            if (_accelZHistory.Count < 10) return false;

            // Look for pattern: Sharp Pull (+Y > 1.5) followed by Sharp Push (-Y < -1.5) or vice versa
            // We check if we have both a high positive and high negative peak in recent history
            // Increased thresholds significantly
            bool hasHighPos = _accelZHistory.Any(v => v > 2.5f);
            bool hasHighNeg = _accelZHistory.Any(v => v < -1.5f); // Gravity affects Y when pointing up/down, so thresholds need care

            // Simplified "Punch/Pump" detection: High magnitude change on Y
            // For now, let's just trigger on very high Y acceleration variance
            float min = _accelZHistory.Min();
            float max = _accelZHistory.Max();

            // Increased from 3.0f to 5.0f -> Lowered to 3.5f for Wiimote
            if (max - min > 3.5f) // Large swing in Y acceleration
            {
                _lastGrenadeTime = DateTime.Now;
                _accelZHistory.Clear(); // Clear history to prevent re-triggering (EN/FR: Vider l'historique pour éviter re-déclenchement)
                SimpleLogger.Instance.Info($"Grenade gesture detected! DeltaY: {max-min:F2}");
                return true;
            }

            return false;
        }

        private bool isButtonPressed(SpecialAction action, ButtonState buttons, NunchukState nunchuk, bool hasNunchuk)
        {
            // Safety check for null mappings (EN/FR: Vérification de sécurité pour mappings nuls)
            if (_playerMappings == null)
            {
                _playerMappings = Options.Instance.GetMappingsForPlayer(PlayerIndex);
                if (_playerMappings == null) return false;
            }

            if (_playerMappings.WiiA?.Special == action && buttons.A) return true;
            if (_playerMappings.WiiB?.Special == action && buttons.B) return true;
            if (_playerMappings.WiiUp?.Special == action && buttons.Up) return true;
            if (_playerMappings.WiiDown?.Special == action && buttons.Down) return true;
            if (_playerMappings.WiiLeft?.Special == action && buttons.Left) return true;
            if (_playerMappings.WiiRight?.Special == action && buttons.Right) return true;
            if (_playerMappings.WiiOne?.Special == action && buttons.One) return true;
            if (_playerMappings.WiiTwo?.Special == action && buttons.Two) return true;
            if (_playerMappings.WiiPlus?.Special == action && buttons.Plus) return true;
            if (_playerMappings.WiiMinus?.Special == action && buttons.Minus) return true;

            if (hasNunchuk)
            {
                if (_playerMappings.NunC?.Special == action && nunchuk.C) return true;
                if (_playerMappings.NunZ?.Special == action && nunchuk.Z) return true;
                if (_playerMappings.NunUp?.Special == action && nunchuk.Joystick.Y > 0.3f) return true;
                if (_playerMappings.NunDown?.Special == action && nunchuk.Joystick.Y < -0.3f) return true;
                if (_playerMappings.NunLeft?.Special == action && nunchuk.Joystick.X < -0.3f) return true;
                if (_playerMappings.NunRight?.Special == action && nunchuk.Joystick.X > 0.3f) return true;
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

        public static event EventHandler OverlayRequested;
        private bool _overlayTriggered = false;

        private void ManageCalibration(Wiimote wiimote, ButtonState buttons, ButtonState lastState)
        {
            if (_calculator.IsCalibrating)
                return;

            // CRITICAL: Check if any hotkey button is pressed with Home (EN/FR: Vérifier si bouton hotkey pressé avec Home)
            // This blocks Home native functions (mode switch, calibration) when hotkey is active
            bool isHotkeyActive = buttons.Home && (buttons.A || buttons.B || buttons.One || buttons.Two || 
                                                    buttons.Minus || buttons.Up || buttons.Down || 
                                                    buttons.Left || buttons.Right);
            
            if (isHotkeyActive)
            {
                // Cancel Home native actions for hotkey combos (EN/FR: Annuler actions Home natives pour hotkeys)
                // EXCEPT Home+Plus (overlay) which is handled below
                if (!buttons.Plus)
                {
                    ticks = -1;
                    return; // Don't process Home native functions
                }
            }

            // Check for Home + D-pad combo for offset adjustment (IR Visualizer) (EN/FR: Vérifier combo Home + D-pad pour ajustement offset)
            // This should be checked BEFORE Home + Plus to have priority (EN/FR: Vérifier AVANT Home + Plus pour priorité)
            if (buttons.Home && (buttons.Up || buttons.Down || buttons.Left || buttons.Right))
            {
                ticks = -1; // Cancel Home button standard action (EN/FR: Annuler action standard bouton Home)
                return; // Don't process other Home combinations (EN/FR: Ne pas traiter autres combinaisons Home)
            }

            // Check for Home + Plus combo to trigger Overlay (EN/FR: Vérifier combo Home + Plus pour déclencher l'overlay)
            if (buttons.Home && buttons.Plus)
            {
                if (!_overlayTriggered)
                {
                    _overlayTriggered = true;
                    SimpleLogger.Instance.Info("Home + Plus detected: Requesting Overlay");
                    OverlayRequested?.Invoke(this, EventArgs.Empty);
                    ticks = -1; // Cancel Home button standard action
                }
                return;
            }

            // Reset trigger when Home is released
            if (!buttons.Home)
            {
                _overlayTriggered = false;
            }

            if (lastState.Home != buttons.Home)
            {
                if (buttons.Home && ticks < 0)
                    ticks = Environment.TickCount;
                else if (!buttons.Home && ticks > 0)
                {
                    // Only switch mode if overlay wasn't triggered (EN/FR: Changer mode seulement si overlay non déclenché)
                    if (!_overlayTriggered)
                    {
                        SwitchMode(wiimote);
                    }
                    ticks = -1;
                }
            }
            else if (buttons.Home && ticks > 0 && Environment.TickCount - ticks >= 1000)
            {
                // Only calibrate if overlay wasn't triggered
                if (!_overlayTriggered)
                {
                    ticks = -1;

                    if (_mode == WiiMoteMode.Mouse)
                        _calculator.Calibrate();
                }
            }                        
        }

        private Process GetDolphinProcess(out bool locks)
        {
            locks = false;
            var list = Process.GetProcesses().ToList();

            Process px = list.FirstOrDefault(p => "dolphin".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null && Options.Instance.RestartOnDolphin)
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
            if (px != null && Options.Instance.RestartOnCemu)
            {
                locks = true;
                return px;
            }

            return null;
        }

        private void RumbleRepetitionCallback()
        {
            // Trigger rumble if trigger still pressed and continuous mode enabled (EN/FR: Déclencher vibration si gâchette maintenue et mode continu activé)
            // CRITICAL: Also check if IR sensor is active to prevent rumble loop when off-screen (EN/FR: Vérifier aussi si capteur IR actif pour éviter boucle vibration hors écran)
            if (_isTriggerPressed && _hasIRSensor && Options.Instance.GetAllowContinuousRumble(PlayerIndex))
            {
                TriggerWeaponRumble();
            }
        }

        private void TriggerWeaponRumble()
        {
            if (_isRumbling) return; // Prevent overlap (EN/FR: Empêcher chevauchement)
            
            int durationMs = Options.Instance.GetRumbleDurationMs(PlayerIndex);
            int intensity = Options.Instance.GetRumbleIntensity(PlayerIndex);
            
            // Adjust duration based on intensity (100% = full duration, 50% = half duration, etc.)
            // (EN/FR: Ajuster durée selon intensité)
            durationMs = (durationMs * intensity) / 100;
            
            if (durationMs > 0)
            {
                _isRumbling = true;
                
                try
                {
                    if (Wiimote != null && Wiimote.IsConnected)
                    {
                        Wiimote.SetRumble(true);
                        
                        // Schedule rumble stop (EN/FR: Programmer arrêt vibration)
                        System.Threading.Tasks.Task.Delay(durationMs).ContinueWith(_ =>
                        {
                            StopRumble();
                        });
                        
                        _lastRumbleTime = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"Error triggering rumble: {ex.Message}");
                    _isRumbling = false;
                }
            }
        }

        private void StopRumble()
        {
            if (_isRumbling)
            {
                try
                {
                    if (Wiimote != null && Wiimote.IsConnected)
                    {
                        Wiimote.SetRumble(false);
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"Error stopping rumble: {ex.Message}");
                }
                finally
                {
                    _isRumbling = false;
                }
            }
        }

        public void UpdateIRSensitivity()
        {
            if (Wiimote == null || !Wiimote.IsConnected) return;

            try
            {
                IRSensitivity sensitivity = (IRSensitivity)Options.Instance.IRSensitivity;
                // Preserve current report type but update sensitivity (EN/FR: Conserver type rapport mais màj sensibilité)
                // We assume ButtonsAccelIR10Ext6 is always used
                Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, sensitivity, true);
                SimpleLogger.Instance.Info($"Updated IR Sensitivity for P{PlayerIndex} to {sensitivity}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to update IR Sensitivity for P{PlayerIndex}: {ex.Message}");
            }
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

        // Called from WiimoteHiddenWnd when trigger button (left mouse) is pressed/released (EN/FR: Appelé depuis WiimoteHiddenWnd quand bouton tir pressé/relâché)
        public void HandleTriggerButton(bool isPressed)
        {
            if (!Options.Instance.GetEnableWeaponRumble(PlayerIndex))
                return;

            // Only rumble if LEDs are visible (screen is aimed) (EN/FR: Vibrer seulement si LEDs visibles (écran visé))
            if (!_hasIRSensor)
                return;

            // Trigger just pressed (rising edge) (EN/FR: Gâchette vient d'être pressée)
            if (isPressed && !_isTriggerPressed)
            {
                TriggerWeaponRumble();
                
                // Start continuous rumble timer if enabled (EN/FR: Démarrer timer vibration continue si activé)
                if (Options.Instance.GetAllowContinuousRumble(PlayerIndex))
                {
                    int intervalMs = Options.Instance.GetRumbleRepetitionMs(PlayerIndex);
                    _rumbleTimer?.Change(intervalMs, intervalMs);
                }
            }
            // Trigger released (falling edge) (EN/FR: Gâchette relâchée)
            else if (!isPressed && _isTriggerPressed)
            {
                // Stop continuous rumble (EN/FR: Arrêter vibration continue)
                _rumbleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                StopRumble();
            }
            
            _isTriggerPressed = isPressed;
        }

        // Update IR sensor status (EN/FR: Mettre à jour statut capteur IR)
        public void UpdateIRSensorStatus(bool hasSensor)
        {
            _hasIRSensor = hasSensor;
            
            // If sensor lost (off-screen), stop rumble immediately (EN/FR: Si capteur perdu (hors écran), arrêter vibration immédiatement)
            if (!hasSensor)
            {
                // Stop continuous rumble timer (EN/FR: Arrêter timer vibration continue)
                _rumbleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                
                // Stop current vibration (EN/FR: Arrêter vibration actuelle)
                StopRumble();
            }
        }
        
        /// <summary>
        /// Detect button presses and fire event for assignment mode (EN/FR: Détecter pressions bouton et déclencher événement pour mode assignation)
        /// </summary>
        private void DetectAndFireButtonEvent(ButtonState buttons, WiimoteState state)
        {
            // Check Wiimote buttons (EN/FR: Vérifier boutons Wiimote)
            if (buttons.A && !_lastState.A) FireButtonEvent("WiiA");
            else if (buttons.B && !_lastState.B) FireButtonEvent("WiiB");
            else if (buttons.One && !_lastState.One) FireButtonEvent("WiiOne");
            else if (buttons.Two && !_lastState.Two) FireButtonEvent("WiiTwo");
            else if (buttons.Plus && !_lastState.Plus) FireButtonEvent("WiiPlus");
            else if (buttons.Minus && !_lastState.Minus) FireButtonEvent("WiiMinus");
            else if (buttons.Up && !_lastState.Up) FireButtonEvent("WiiUp");
            else if (buttons.Down && !_lastState.Down) FireButtonEvent("WiiDown");
            else if (buttons.Left && !_lastState.Left) FireButtonEvent("WiiLeft");
            else if (buttons.Right && !_lastState.Right) FireButtonEvent("WiiRight");
            else if (buttons.Home && !_lastState.Home) FireButtonEvent("WiiHome");
            
            // Check Nunchuk buttons if connected (EN/FR: Vérifier boutons Nunchuk si connecté)
            if (state.ExtensionType == ExtensionType.Nunchuk)
            {
                NunchukState nunchuk = state.Nunchuk;
                
                // Check buttons first (EN/FR: Vérifier boutons d'abord)
                if (nunchuk.C && !_lastNunchukState.C) 
                    FireButtonEvent("NunchukC");
                else if (nunchuk.Z && !_lastNunchukState.Z) 
                    FireButtonEvent("NunchukZ");
                // Check joystick axes separately (EN/FR: Vérifier axes joystick séparément)
                // Only check axes if no button was pressed (EN/FR: Vérifier axes seulement si aucun bouton pressé)
                else
                {
                    // Threshold for axis detection (EN/FR: Seuil pour détection axe)
                    const float axisThreshold = 0.3f;
                    
                    // Up axis (EN/FR: Axe haut)
                    if (nunchuk.Joystick.Y > axisThreshold && _lastNunchukState.Joystick.Y <= axisThreshold)
                        FireButtonEvent("NunUp");
                    // Down axis (EN/FR: Axe bas)
                    else if (nunchuk.Joystick.Y < -axisThreshold && _lastNunchukState.Joystick.Y >= -axisThreshold)
                        FireButtonEvent("NunDown");
                    // Left axis (EN/FR: Axe gauche)
                    else if (nunchuk.Joystick.X < -axisThreshold && _lastNunchukState.Joystick.X >= -axisThreshold)
                        FireButtonEvent("NunLeft");
                    // Right axis (EN/FR: Axe droite)
                    else if (nunchuk.Joystick.X > axisThreshold && _lastNunchukState.Joystick.X <= axisThreshold)
                        FireButtonEvent("NunRight");
                }
                
                // Update last nunchuk state (EN/FR: Mettre à jour dernier état nunchuk)
                _lastNunchukState.C = nunchuk.C;
                _lastNunchukState.Z = nunchuk.Z;
                _lastNunchukState.Joystick = nunchuk.Joystick;
            }
            
            // Update last button state (EN/FR: Mettre à jour dernier état bouton)
            _lastState = buttons;
        }
        
        private void FireButtonEvent(string buttonName)
        {
            SimpleLogger.Instance.Info($"P{PlayerIndex}: Button {buttonName} pressed in assignment mode");
            ButtonPressed?.Invoke(this, new ButtonPressedEventArgs(PlayerIndex, buttonName));
        }
        
        /// <summary>
        /// Set input lock state for button assignment mode (EN/FR: Définir état verrouillage pour mode assignation)
        /// </summary>
        public static void SetInputLock(bool locked)
        {
            _inputsLocked = locked;
            SimpleLogger.Instance.Info($"Wiimote inputs {(locked ? "LOCKED" : "UNLOCKED")} for button assignment");
        }
    }

    /// <summary>
    /// Event args for button press detection (EN/FR: Arguments événement pour détection pression bouton)
    /// </summary>
    public class ButtonPressedEventArgs : EventArgs
    {
        public int PlayerIndex { get; set; }
        public string ButtonName { get; set; } // "WiiA", "WiiB", "NunchukC", etc.
        public ButtonPressedEventArgs(int playerIndex, string buttonName)
        {
            PlayerIndex = playerIndex;
            ButtonName = buttonName;
        }
    }

    enum WiiMoteMode
    {
        Mouse = 0,
        Keyboardpad = 1,
        Disabled = 2
    }
}
