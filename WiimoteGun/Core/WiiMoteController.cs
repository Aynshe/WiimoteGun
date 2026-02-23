using WiimoteLib;
using WiimoteLib.Events;
using WiimoteLib.DataTypes;
using WiimoteLib.Geometry;
using System.Threading;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using WiimoteGun.Core;
using WiimoteGun.VMulti;

namespace WiimoteGun
{
    public class WiiMoteController : IDisposable
    {
        private WiiMoteMode _mode = WiiMoteMode.Mouse;
        public WiiMoteMode Mode { get { return _mode; } }
        private ScreenPositionCalculator _calculator;
        private IVirtualMouse _virtualMouse;
        private IVirtualGamepad _virtualGamepad; // GamePad mode (EN/FR: Mode GamePad)
        private IVirtualJoy _joy;
        private int ticks = -1;
        private ButtonState _lastState;
        private NunchukState _lastNunchukState;
        private object _lock = new object();
        private string _uniqueId;
        private WiimoteHiddenWnd _hiddenWnd;
        private Thread _watchDolphinThread;
        private AutoResetEvent _watchDolphinfinishEvent = new AutoResetEvent(false);
        private Process _runningProcess;
        private bool _processLocking;
        private PlayerMappings _playerMappings;

        // Auto-sleep after inactivity (EN/FR: Mise en veille automatique après inactivité)
        private DateTime _lastActivityTime;
        private System.Threading.Timer _sleepCheckTimer;
        private const int SLEEP_TIMEOUT_MINUTES = 10;

        // [FIX V22d] EN: Timer to periodically probe for Nunchuk behind active MP adapter
        // FR: Timer pour sonder périodiquement le Nunchuk derrière un adaptateur MP actif
        private System.Threading.Timer _mpNunchukProbeTimer;

        // [FIX V22g] EN: Guard to prevent concurrent EnableMotionPlus calls that corrupt the MP adapter
        // FR: Guard pour empêcher les appels concurrents à EnableMotionPlus qui corrompent l'adaptateur MP
        private volatile bool _mpActivationInProgress = false;
        private readonly object _mpActivationLock = new object();

        // Weapon recoil rumble (EN/FR: Vibration recul arme)
        private System.Threading.Timer _rumbleTimer;
        private System.Threading.Timer _rumbleStopTimer;
        private bool _isTriggerPressed = false;
        private bool _isRumbling = false;
        private DateTime _lastRumbleTime = DateTime.MinValue;
        private bool _hasIRSensor = false; // Track if LEDs are visible (EN/FR: Suivi si LEDs sont visibles)

        // Gesture State (EN/FR: État des gestes)
        private DateTime _lastShakeTime = DateTime.MinValue;
        private bool _isShaking = false; // Track shake state (EN/FR: Suivi état secousse)
        private DateTime _lastGrenadeTime = DateTime.MinValue;
        
        // Manual Device Disable Hotkey (EN/FR: Hotkey Désactivation Manuelle)
        private DateTime _manualDisableStartTime = DateTime.MinValue;
        private bool _manualDisableTriggered = false;
        
        // Off-screen Reload state tracking (EN/FR: Suivi d'état rechargement hors écran)
        private bool _wasOnScreen = true;
        private int _offScreenReloadClickSequence = 0; // 0=idle, 1-4=sending 2 clicks
        
        // Gesture Click Frame Counters (EN/FR: Compteurs de frames pour clics gestuels)
        private int _gestureRightClickFrameCount = 0;
        private int _gestureMiddleClickFrameCount = 0;
        private const int GESTURE_CLICK_DURATION_FRAMES = 6; // Reverted to ~100ms (6 frames) for reliability

        // HID Watchdog (EN/FR: Watchdog HID)
        private DateTime _lastReportTime;
        private const int HID_TIMEOUT_MS = 2000; // 2 seconds threshold

        // Cooldowns (EN/FR: Délais de récupération)
        const int SHAKE_COOLDOWN_MS = 500;
        const int GRENADE_COOLDOWN_MS = 1000;
        
        // Startup safety (EN/FR: Sécurité au démarrage)
        private DateTime _controllerStartTime;
        private const int STARTUP_GRACE_PERIOD_MS = 2000; // Ignore gestures for 2s after start

        // In-Game Offset Adjustment (EN/FR: Ajustement offset en jeu)
        private bool _isOffsetAdjustmentActive = false;
        private DateTime _lastOffsetAdjustTime = DateTime.MinValue;
        private DateTime _offsetAdjustmentEndTime = DateTime.MinValue;
        private const int OFFSET_ADJUST_REPEAT_MS = 80; // Repeat rate for held DPad (EN/FR: Taux de répétition pour DPad maintenu)
        private const int OFFSET_OVERLAY_FADE_MS = 10000; // Match overlay fade duration (EN/FR: Correspond à la durée de fondu de l'overlay)
        public static event Action<int, int, int, bool, System.Drawing.Point?> OffsetAdjustmentChanged; // playerIndex, offsetX, offsetY, isActive, irPosition (pixels)
        
        // DInput detection (EN/FR: Détection DInput)
        private int _lastDInputIndex = 0;
        public int DInputIndex { get { return _lastDInputIndex; } }

        // High Performance Timer (EN/FR: Timer haute performance)
        // Replaces DateTime.Now calls in hot path when enabled
        // (EN/FR: Remplace les appels DateTime.Now dans le chemin critique quand activé)
        private Stopwatch _perfStopwatch = Stopwatch.StartNew();

        // Virtual Polling (Upsampling) (EN/FR: Polling Virtuel / Upsampling)
        private WiimoteLib.Helpers.MultimediaTimer _virtualPollingTimer;
        private int _lastX_Raw = 0; // Last processed but non-extrapolated X
        private int _lastY_Raw = 0; // Last processed but non-extrapolated Y
        private float _lastVelX_Diag = 0f; // Last calculated velocity X
        private float _lastVelY_Diag = 0f; // Last calculated velocity Y
        private bool _lastLeft_Raw = false;
        private bool _lastRight_Raw = false;
        private bool _lastMiddle_Raw = false;
        private bool _lastMoveCursor_Raw = false;
        private DateTime _lastProcessingTime = DateTime.MinValue;
        private DateTime _lastAnyReportTime = DateTime.MinValue; // Last report time, real OR virtual (EN/FR: Temps dernier rapport, réel OU virtuel)
        private bool _isHybridToggleActive = false; // State for hybrid mode toggle (EN/FR: État de la bascule du mode hybride)
        private bool _lastHybridActive = false;     // Track hybrid state from previous frame (EN/FR: Suivi de l'état hybride de la frame précédente)
        private DateTime _hybridActivationTime = DateTime.MinValue; // Time of last hybrid activation (EN/FR: Temps de la dernière activation hybride)
        private DateTime _hybridDeactivationTime = DateTime.MinValue; // Time of last hybrid deactivation (EN/FR: Temps de la dernière désactivation hybride)
        private bool _profileWantsHybridMouse = false; // EN: Track if current profile wants hybrid mouse (FR: Suivi si profil veut souris hybride)
        private bool _lastRuntimeWantsMouse = false;   // EN: Track if mouse was moving last frame (FR: Suivi si souris bougeait la frame d'avant)
        private bool _lastHybridLeft = false;
        private bool _lastHybridRight = false;
        private bool _lastHybridMiddle = false;

        private bool _lastAccelWiimoteUp = false;
        private bool _lastAccelWiimoteDown = false;
        private bool _lastAccelWiimoteLeft = false;
        private bool _lastAccelWiimoteRight = false;
        private bool _lastAccelWiimoteShake = false;

        private bool _lastAccelNunchukUp = false;
        private bool _lastAccelNunchukDown = false;
        private bool _lastAccelNunchukLeft = false;
        private bool _lastAccelNunchukRight = false;
        private bool _lastAccelNunchukShake = false;

        private bool _lastGyroMotionPlusUp = false;
        private bool _lastGyroMotionPlusDown = false;
        private bool _lastGyroMotionPlusLeft = false;
        private bool _lastGyroMotionPlusRight = false;
        private bool _lastGyroMotionPlusRollLeft = false;
        private bool _lastGyroMotionPlusRollRight = false;

        // --- Shake Derivative Tracking ---
        private float _lastWMotX = 0f;
        private float _lastWMotY = 0f;
        private float _lastWMotZ = 0f;
        private float _lastNMotX = 0f;
        private float _lastNMotY = 0f;
        private float _lastNMotZ = 0f;

        // --- Shake Peak-to-Peak Tracking (EN/FR: Suivi pic-à-pic secousse) ---
        // EN: Track the last strong direction to detect true back-and-forth oscillation
        // FR: Suivre la dernière direction forte pour détecter une vraie oscillation aller-retour
        private int _wShakePeakDir = 0;       // -1 = negative peak, +1 = positive peak, 0 = none
        private int _wShakeOscillationCount = 0;
        private DateTime _lastWShakeOscillationTime = DateTime.MinValue;
        private int _wShakeActiveFrames = 0; // EN/FR: Persistance du shake sur plusieurs frames
        private int _nShakePeakDir = 0;
        private int _nShakeOscillationCount = 0;
        private DateTime _lastNShakeOscillationTime = DateTime.MinValue;
        private int _nShakeActiveFrames = 0;
        
        // --- Gyro Smoothing (EMA) ---
        private float _smoothGyroYaw = 0f;
        private float _smoothGyroPitch = 0f;
        private float _smoothGyroRoll = 0f;
        private const float GYRO_SMOOTH_ALPHA = 0.4f; // Adjust for smoothness vs latency

        // --- Gyro Roll Anti-Wobble Tracking ---
        private DateTime _lastRollLeftTime = DateTime.MinValue;
        private DateTime _lastRollRightTime = DateTime.MinValue;

        private bool CheckShake(WiimoteState state)
        {
            if (!Options.Instance.EnableShakeReload) return false;

            // Ignore gestures during startup grace period (EN/FR: Ignorer gestes pendant période de grâce)
            if ((GetNow() - _controllerStartTime).TotalMilliseconds < STARTUP_GRACE_PERIOD_MS) return false;

            // Get magnitudes independently (EN/FR: Obtenir magnitudes indépendamment)
            Point3F wiimoteAccel = state.Accel.Values;
            double wiimoteMag = Math.Sqrt(wiimoteAccel.X * wiimoteAccel.X + wiimoteAccel.Y * wiimoteAccel.Y + wiimoteAccel.Z * wiimoteAccel.Z);
            
            bool nunchukAvailable = (state.ExtensionType == ExtensionType.Nunchuk || state.ExtensionType == ExtensionType.MotionPlusNunchuk);
            double nunchukMag = 0;
            if (nunchukAvailable)
            {
                Point3F nunchukAccel = state.Nunchuk.Accel.Values;
                nunchukMag = Math.Sqrt(nunchukAccel.X * nunchukAccel.X + nunchukAccel.Y * nunchukAccel.Y + nunchukAccel.Z * nunchukAccel.Z);
            }

            // Normalize: Some clones report ~28 units for 1G. Normalize to Gs for thresholds.
            // (EN/FR: Normaliser : Certains clones rapportent ~28 unités pour 1G. Normalisation en G pour les seuils.)
            if (wiimoteMag > 10) wiimoteMag /= 28.0;
            if (nunchukAvailable && nunchukMag > 10) nunchukMag /= 28.0;

            // Decide which magnitude to use based on settings (EN/FR: Décider quelle magnitude utiliser selon les paramètres)
            double magnitude = 0;
            if (Options.Instance.ShakeFromNunchuk)
            {
                if (!nunchukAvailable) return false; // Nunchuk selected but not connected (EN/FR: Nunchuk sélectionné mais pas connecté)
                magnitude = nunchukMag;
            }
            else
            {
                magnitude = wiimoteMag;
            }
            
            // Thresholds: Very Low=3.5g, Low=2.8g, Medium=2.0g, High=1.5g
            // (EN/FR: Seuils : Très Bas=3.5g, Bas=2.8g, Moyen=2.0g, Haut=1.5g)
            double threshold = 2.0;
            switch (Options.Instance.ShakeSensitivity)
            {
                case 0: threshold = 3.5; break; // Very Low (EN/FR: Très Bas)
                case 1: threshold = 2.8; break; // Low (EN/FR: Bas)
                case 2: threshold = 2.0; break; // Medium (EN/FR: Moyen)
                case 3: threshold = 1.5; break; // High (EN/FR: Haut)
            }        

            // State Machine for Shake Detection (EN/FR: Machine à états pour détection secousse)
            bool triggered = false;

            if (_isShaking)
            {
                // If currently shaking, wait for return to rest (hysteresis)
                // (EN/FR: Si en cours de secousse, attendre retour au repos)
                // INCREASED: 1.5g reset threshold to avoid jitter (EN/FR: Seuil reset monté à 1.5g)
                double resetThreshold = 1.5; 
                
                // Force reset if shaking for too long (> 500ms) - prevents getting stuck
                // (EN/FR: Reset forcé si secousse trop longue (> 500ms) - évite blocage)
                bool timeOut = (GetNow() - _lastShakeTime).TotalMilliseconds > 500;

                if (magnitude < resetThreshold || timeOut)
                {
                    _isShaking = false;
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
                    if ((GetNow() - _lastShakeTime).TotalMilliseconds > SHAKE_COOLDOWN_MS)
                    {
                        _lastShakeTime = GetNow();
                        triggered = true;
                        SimpleLogger.Instance.Info(string.Format("Shake fired! Mag: {0:F2} > {1}", magnitude, threshold));
                    }
                }
            }

            return triggered;
        }
        private System.Collections.Generic.Queue<float> _accelZHistory = new System.Collections.Generic.Queue<float>();
        private const int ACCEL_HISTORY_SIZE = 20; // Approx 20 samples (depends on report rate)

        private float _lastGyroYaw = 0f;   // Last Yaw value in °/s or accel delta (EN/FR: Dernière valeur Yaw en °/s ou delta accel)
        private float _lastGyroPitch = 0f; // Last Pitch value in °/s or accel delta (EN/FR: Dernière valeur Pitch en °/s ou delta accel)
        private float _lastGyroRoll = 0f;  // Last Roll value in °/s (EN/FR: Dernière valeur Roll en °/s)

        // Battery level monitoring (EN/FR: Suivi du niveau de batterie)
        private float _lastBatteryLevel = -1f;
        private DateTime _lastBatteryLogTime = DateTime.MinValue;
        
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
        private DateTime _lastGyroStillTime;
        private const float GYRO_STILL_THRESHOLD = 0.5f; // °/s threshold to consider "still" (EN/FR: Seuil °/s pour considérer "immobile")
        private const float ACCEL_SENSITIVITY = 50f; // Multiplier for accelerometer delta (EN/FR: Multiplicateur pour delta accéléromètre)
        private bool _gyroFirstRun = true; // Track first run for logging (EN/FR: Suivi premier run pour logging)

        // Hybrid Tracking Mode (EN/FR: Mode tracking hybride)
        // private bool _useGyroForTracking = false; // Currently using gyro for cursor movement (EN/FR: Utilise actuellement gyro pour mouvement curseur)
        private DateTime _lastIRSeenTime; // Last time IR was valid (EN/FR: Dernière fois que l'IR était valide)
        // private int _diagFrameCount = 0; // Counter for diagnostic logging (EN/FR: Compteur pour logs diagnostic)
        private const float IR_LOST_TIMEOUT_MS = 100f; // Time before switching to gyro (EN/FR: Temps avant basculement vers gyro)

        // GamePad Sticky IR (EN/FR: Maintien IR pour GamePad)
        private float _lastValidIRX = 0.5f;
        private float _lastValidIRY = 0.5f;
        private long _debugCounter = 0;

        // Button assignment mode (EN/FR: Mode assignation bouton)
        private static bool _inputsLocked = false; // Locks all controllers input (EN/FR: Verrouille inputs de tous contrôleurs)
        public static event EventHandler<ButtonPressedEventArgs> ButtonPressed; // Fired when button is pressed in assign mode (EN/FR: Déclenché quand bouton pressé en mode assign)
        
        /// <summary>
        /// EN: Get current time based on high performance timer setting.
        /// FR: Obtenir l'heure actuelle selon le réglage du timer haute performance.
        /// </summary>
        private DateTime GetNow() => Options.Instance.UseHighPerfTimers ? DateTime.UtcNow : DateTime.Now;

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
            _uniqueId = Wiimote.Address.ToString().Replace(":", "");

            _lastState = new ButtonState();
            _lastNunchukState = new NunchukState();

            _joy = null; // Will be initialized in SetupWiimote (EN/FR: Sera initialisé dans SetupWiimote)
            _virtualMouse = null;

            // Pass player index for per-player calibration (EN/FR: Passer l'index joueur pour calibration par joueur)
            _calculator = new ScreenPositionCalculator(ScreenIndex, PlayerIndex);

            SetupWiimote();

            // Setup Hypersampling timer if enabled (EN/FR: Configurer timer de hypersampling si activé)
            if (Options.Instance.EnableVirtualPolling)
            {
                int interval = 1000 / Options.Instance.VirtualPollingRate;
                _virtualPollingTimer = new WiimoteLib.Helpers.MultimediaTimer(interval, OnVirtualPollingTick);
                _virtualPollingTimer.Start();
            }

            _watchDolphinThread = new Thread(CheckDolphin);
            _watchDolphinThread.IsBackground = true;
            _watchDolphinThread.Start();

            // Start auto-sleep/disconnect check timer (EN/FR: Démarrer le timer de vérification veille/déconnexion)
            // Increased interval to 5s to reduce overhead on the main thread/timer.
            // (EN/FR: Intervalle augmenté à 5s pour réduire la charge sur le thread principal/timer.)
            int checkInterval = 5000; 
            _sleepCheckTimer = new System.Threading.Timer(_ => CheckSleep(), null, checkInterval, checkInterval);
            
            // Initialize rumble timers (disabled by default) (EN/FR: Initialiser timers vibration (désactivés par défaut))
            _rumbleTimer = new System.Threading.Timer(_ => RumbleRepetitionCallback(), null, Timeout.Infinite, Timeout.Infinite);
            _rumbleStopTimer = new System.Threading.Timer(_ => StopRumble(), null, Timeout.Infinite, Timeout.Infinite);

            // Initialize time-based fields (EN/FR: Initialiser les champs basés sur le temps)
            _lastActivityTime = GetNow();
            _lastReportTime = GetNow();
            _lastIRSeenTime = GetNow();
            _lastGyroStillTime = GetNow();
            _controllerStartTime = GetNow();
        }

        private void CheckSleep()
        {
            try
            {
                if (Wiimote == null || !Wiimote.IsConnected)
                    return;

                // DolphinBar: Use GetStatus to detect disconnections (EN/FR: Utiliser GetStatus pour détecter déconnexions)
                // GetStatus will timeout if Wiimote is turned off, triggering exception handling
                if (!Wiimote.Device.IsBluetooth)
                {
                    try
                    {
                        Wiimote.GetStatus(1500); // EN: Increased from 500ms for stability / FR: Augmenté de 500ms pour la stabilité
                    }
                    catch (TimeoutException)
                    {
                        SimpleLogger.Instance.Warning($"DolphinBar Wiimote P{PlayerIndex} disconnected (GetStatus timeout)");
                        Wiimote.Disconnect();
                    }
                    return;
                }

                // Bluetooth/DolphinBar: HID Watchdog (EN/FR: Watchdog HID)
                // If we haven't received a report in 2s while active, re-send the report mode command
                // (EN/FR: Si aucun rapport reçu en 2s alors qu'actif, renvoyer la commande de mode)
                if (_mode != WiiMoteMode.Disabled && (GetNow() - _lastReportTime).TotalMilliseconds > HID_TIMEOUT_MS)
                {
                    SimpleLogger.Instance.Debug(string.Format("[P{0}] HID communication timeout ({1}ms). Attempting report mode recovery...", PlayerIndex, HID_TIMEOUT_MS));
                    
                    // Update timer to avoid spamming recovery
                    _lastReportTime = GetNow();

                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            IRSensitivity sensitivity = (IRSensitivity)Options.Instance.IRSensitivity;
                            Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, sensitivity, true);
                            
                            // Try to enable MotionPlus again as it might have been reset
                            if (Wiimote.WiimoteState.ExtensionType == ExtensionType.Nunchuk)
                                Wiimote.EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
                            else
                                Wiimote.EnableMotionPlus(MotionPlusExtensionType.NoExtension);
                                
                            SimpleLogger.Instance.Debug(string.Format("[P{0}] Report mode recovery command sent.", PlayerIndex));
                        }
                        catch (Exception ex)
                        {
                            SimpleLogger.Instance.Error(string.Format("[P{0}] Report mode recovery failed: {1}", PlayerIndex, ex.Message));
                        }
                    });
                }

                // Bluetooth: Auto-sleep after inactivity (EN/FR: Mise en veille auto après inactivité)
                double inactiveMinutes = (GetNow() - _lastActivityTime).TotalMinutes;
                
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
            _lastActivityTime = GetNow();
        }

        private void SetupWiimote()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    // -------------------------------------------------------------------
                    // PHASE 0: VMulti Initialization (if in RawInput mode)
                    // (EN/FR: PHASE 0 : Initialisation VMulti (si mode RawInput))
                    // -------------------------------------------------------------------
                    if (Options.Instance.DefaultMouseMode == MouseMode.RawInput)
                    {
                        ServiceClient.EnablePlayer(PlayerIndex);

                        // Proactive gamepad removal if global mode is disabled (EN/FR: Suppression proactive du gamepad si le mode global est désactivé)
                        if (!Options.Instance.EnableGamePadSwapMode)
                        {
                            ServiceClient.RemoveGamepad(PlayerIndex);
                        }

                        SimpleLogger.Instance.Info($"Waiting for VMulti P{PlayerIndex} initialization...");

                        bool deviceReady = false;
                        // Try for up to 6 seconds (12 * 500ms)
                        for (int i = 0; i < 13; i++) // 13 iterations to allow iteration 0 (immediate check)
                        {
                            // EN: Early-check (iteration 0) to avoid unnecessary 500ms sleep on restart
                            // FR: Check précoce (itération 0) pour éviter un sleep de 500ms inutile au redémarrage
                            if (i > 0) Thread.Sleep(500);

                            VMultiDeviceDetector.PlayerDevices devices = VMultiDeviceDetector.DetectPlayerVMultiDevices(PlayerIndex);
                            if (!string.IsNullOrEmpty(devices.MouseId))
                            {
                                deviceReady = true;
                                SimpleLogger.Instance.Info(string.Format("VMulti P{0} ready after {1}ms", PlayerIndex, Math.Max(0, (i) * 500)));
                                break;
                            }
                        }

                        if (!deviceReady)
                        {
                            SimpleLogger.Instance.Warning($"VMulti P{PlayerIndex} detection timed out. Retrying enable...");
                            ServiceClient.EnablePlayer(PlayerIndex);
                            Thread.Sleep(1500);
                            
                            VMultiDeviceDetector.PlayerDevices devicesRetry = VMultiDeviceDetector.DetectPlayerVMultiDevices(PlayerIndex);
                            if (string.IsNullOrEmpty(devicesRetry.MouseId))
                            {
                                SimpleLogger.Instance.Error($"VMulti P{PlayerIndex} mouse not detected! HID operations will fail.");
                            }
                        }

                        // Auto-detect and save VMulti mouse after activation (EN/FR: Auto-détecter et sauvegarder souris VMulti après activation)
                        // VMulti mice only exist AFTER EnablePlayer, so we detect them here, not at startup
                        if (Options.Instance.AutoLockVMultiDevices)
                        {
                            VMultiDeviceDetector.PlayerDevices finalDevices = VMultiDeviceDetector.DetectPlayerVMultiDevices(PlayerIndex);
                            string mouseId = finalDevices.MouseId;
                            if (!string.IsNullOrEmpty(mouseId))
                            {
                                Options.Instance.SetPreferredMouseId(PlayerIndex, mouseId);
                                Options.Instance.Save();
                                SimpleLogger.Instance.Info(string.Format("[VMulti Post-Activation] Auto-saved P{0} Mouse: {1}", PlayerIndex, mouseId));
                            }
                        }
                    }

                    // -------------------------------------------------------------------
                    // PHASE 1: Virtual Devices Creation
                    // (EN/FR: PHASE 1 : Création des périphériques virtuels)
                    // -------------------------------------------------------------------
                    if (Options.Instance.DefaultMouseMode == MouseMode.SendInput)
                    {
                        // SendInput mode: Use simple SendInput keyboard
                        _joy = new VirtualSendInputKeyboard();
                        
                        // Only Player 1 gets a mouse in SendInput mode
                        if (PlayerIndex == 1)
                        {
                            _virtualMouse = new VirtualSendInputMouse();
                            if (_virtualMouse is VirtualSendInputMouse sendInputMouse)
                            {
                                sendInputMouse.OnLeftMouseButtonChanged += HandleTriggerButton;
                            }
                        }
                    }
                    else
                    {
                        // RawInput/VMulti mode: All players get independent keyboard and mice
                        _joy = new VirtualVMultiKeyboard(PlayerIndex);
                        _virtualMouse = new VirtualVMultiMouse(PlayerIndex, _uniqueId);
                        
                        if (_virtualMouse is VirtualVMultiMouse vmultiMouse)
                        {
                            vmultiMouse.OnLeftMouseButtonChanged += HandleTriggerButton;
                        }
                    }

                    SimpleLogger.Instance.Info($"P{PlayerIndex}: Virtual devices initialized (Mode: {Options.Instance.DefaultMouseMode})");

                    // CRITICAL FIX: Clear any stuck rumble state (EN/FR: Arrêter la vibration bloquée)
                    Wiimote.SetRumble(false);
                    Thread.Sleep(50);

                    // Apply IR Sensitivity from Options (EN/FR: Appliquer sensibilité IR depuis Options)
                    // Mapping: 0=Level1, 1=Level2, 2=Level3, 3=Level4, 4=Level5, 5=Maximum
                    IRSensitivity sensitivity = (IRSensitivity)Options.Instance.IRSensitivity;
                    Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, sensitivity, true);

                    // CRITICAL: We don't call AutoEnableMotionPlus() here anymore.
                    // Instead, we wait for GetStatus() below to identify the extension first.
                    // (EN/FR: On ne l'appelle plus ici. On attend GetStatus() pour identifier l'extension d'abord.)

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

                                    // Log battery level after status is fetched (EN/FR: Logger le niveau de batterie après récupération du statut)
                                    _lastBatteryLevel = Wiimote.WiimoteState.Status.Battery;
                                    _lastBatteryLogTime = GetNow();
                                    SimpleLogger.Instance.Info($"[P{PlayerIndex}] Battery status fetched: {_lastBatteryLevel:F1}% " + (Wiimote.WiimoteState.Status.BatteryLow ? "(LOW!)" : ""));
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
                            
                            // -------------------------------------------------------------------
                            // SUCCESS NOTIFICATION: Wiimote is fully initialized and ready
                            // (EN/FR: NOTIFICATION SUCCÈS : Wiimote initialisée et prête)
                            // -------------------------------------------------------------------
                            Vibrate(Wiimote);

                            // EN: Auto-enable MotionPlus AFTER we are sure the extension ID is read.
                            // FR: Activer auto le MotionPlus APRÈS être sûr que l'ID d'extension est lu.
                            AutoEnableMotionPlus();
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
            Wiimote.ExtensionChanged += OnWiiMoteExtensionChanged;

            // Display connection notification with mouse mode (EN/FR: Afficher notification connexion avec mode souris)
            string mouseMode = Options.Instance.DefaultMouseMode == MouseMode.SendInput ? "SendInput (Legacy)" : "VMulti (Multi-Player)";
            string connType = Wiimote.Device.IsBluetooth ? "Bluetooth" : "DolphinBar";
            
            Program.Notify(string.Format("Wiimote P{0} connected ({1}) - {2}", PlayerIndex, connType, mouseMode));
            SimpleLogger.Instance.Info(string.Format("Wiimote P{0} connected via {1}. HID path: {2}", PlayerIndex, connType, Wiimote.DevicePath));

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

            // CRITICAL: Vibrate moved to SetupWiimote (end of background initialization)
            // (EN/FR: Vibration déplacée dans SetupWiimote (fin d'initialisation asynchrone))
            // Vibrate(Wiimote); 
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

            if (_virtualGamepad != null)
            {
                _virtualGamepad.ResetAll();
                _virtualGamepad.Disconnect();
                _virtualGamepad.Dispose();
                _virtualGamepad = null;

                // Explicitly remove gamepad on disconnect if not persistent or if global mode is disabled
                // (EN/FR: Supprimer explicitement le gamepad à la déconnexion si non persistant ou si mode global désactivé)
                if (!Options.Instance.PersistentGamePads || !Options.Instance.EnableGamePadSwapMode)
                {
                    WiimoteGun.ServiceClient.RemoveGamepad(PlayerIndex);
                }
            }

            // Dispose sleep timer (EN/FR: Disposer le timer de veille)
            if (_sleepCheckTimer != null)
            {
                _sleepCheckTimer.Dispose();
                _sleepCheckTimer = null;
            }

            // [FIX V22d] Dispose MP probe timer
            if (_mpNunchukProbeTimer != null)
            {
                _mpNunchukProbeTimer.Dispose();
                _mpNunchukProbeTimer = null;
            }

            // Dispose rumble timers (EN/FR: Disposer les timers de vibration)
            if (_rumbleTimer != null)
            {
                _rumbleTimer.Dispose();
                _rumbleTimer = null;
            }
            if (_rumbleStopTimer != null)
            {
                _rumbleStopTimer.Dispose();
                _rumbleStopTimer = null;
            }

            // Dispose virtual polling timer (EN/FR: Disposer le timer de polling virtuel)
            if (_virtualPollingTimer != null)
            {
                _virtualPollingTimer.Stop();
                _virtualPollingTimer = null;
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
                // Persistent mode: Keep device enabled to avoid Windows PnP instability
                // (EN/FR: Mode persistant : Garder activé pour éviter l'instabilité PnP Windows)
                SimpleLogger.Instance.Info($"[Persistent P{PlayerIndex}] Keeping VMulti device enabled.");
            }
        }

        /// <summary>
        /// EN: Explicitly refresh/enable the VMulti device for this player.
        /// FR: Rafraîchir/activer explicitement le périphérique VMulti pour ce joueur.
        /// Used at startup to ensuring devices are active if they were accidentally cleaned up.
        /// </summary>
        public void RefreshVMultiState()
        {
            if (Options.Instance.DefaultMouseMode == MouseMode.RawInput)
            {
                SimpleLogger.Instance.Info($"[WiiMoteController] Refreshing VMulti state for Player {PlayerIndex}");
                ServiceClient.EnablePlayer(PlayerIndex);
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
            int smoothingFrames = 3; // Fixed value or Move to constant/Advanced option if needed
            while (_gyroYawHistory.Count > smoothingFrames)
                _gyroYawHistory.Dequeue();
            while (_gyroPitchHistory.Count > smoothingFrames)
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

            // Reduce overhead: Limit queue size check to once every few frames? 
            // Currently O(1) mostly, so fine.
        }

        private void OnWiiMoteExtensionChanged(object sender, WiimoteExtensionEventArgs e)
        {
            SimpleLogger.Instance.Info(string.Format("[P{0}] Extension changed: {1} (Inserted: {2})", PlayerIndex, e.ExtensionType, e.Inserted));
            
            // Re-initialize MotionPlus with proper passthrough if needed
            // (EN/FR: Réinitialiser MotionPlus avec le bon passthrough si nécessaire)
            AutoEnableMotionPlus();
        }

        private void AutoEnableMotionPlus()
        {
            // EN: Wrap in Task.Run to avoid blocking the caller (especially the Wiimote read thread).
            // FR: Envelopper dans Task.Run pour éviter de bloquer l'appelant (surtout le thread de lecture Wiimote).
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // [FIX V22g] EN: Prevent concurrent EnableMotionPlus calls. Two concurrent writes
                    // (e.g. from probe timer + extension change event) corrupt the MP adapter (ID=0x000000000000).
                    // FR: Empêcher les appels concurrents à EnableMotionPlus. Deux écritures simultanées
                    // (ex: probe timer + event extension change) corrompent l'adaptateur MP (ID=0x000000000000).
                    lock (_mpActivationLock)
                    {
                        if (_mpActivationInProgress)
                        {
                            SimpleLogger.Instance.Debug($"[P{PlayerIndex}] [V22g] AutoEnableMotionPlus skipped (already in progress)");
                            return;
                        }
                        _mpActivationInProgress = true;
                    }

                    try
                    {
                        if (Wiimote == null || !Wiimote.IsConnected) return;

                        // Wait a bit for extension detection to stabilize if called from event
                        // (EN/FR: Attendre un peu que la détection d'extension se stabilise)
                        // EN: This Sleep is now safe because we are in a background task (fixes IR lag).
                        // FR: Ce Sleep est maintenant sûr car on est dans une tâche de fond (corrige le lag IR).
                        Thread.Sleep(200);

                        ExtensionType ext = Wiimote.WiimoteState.ExtensionType;
                        
                        if (ext == ExtensionType.Nunchuk || ext == ExtensionType.MotionPlusNunchuk)
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled with Nunchuk Passthrough (Type: {1})", PlayerIndex, ext));
                            
                            // [FIX V22d] Stop probing if Nunchuk detected
                            StopMpNunchukProbe();
                        }
                        else if (ext == ExtensionType.ClassicController)
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.ClassicController);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled with Classic Passthrough", PlayerIndex));
                            
                            // [FIX V22d] Stop probing if Classic detected
                            StopMpNunchukProbe();
                        }
                        else if (ext == ExtensionType.MotionPlus)
                        {
                            // [FIX V22] EN: MP is active in standalone mode (0x04). A status report with MotionPlus type
                            // could mean a Nunchuk was just hot-plugged into the pass-through port.
                            // Try enabling passthrough Nunchuk (0x05) — EnableMotionPlus will verify via extension ID.
                            // If no Nunchuk is present, Fix V21 handles the fallback gracefully.
                            // FR: MP actif en standalone (0x04). Un status report avec type MotionPlus peut signifier
                            // qu'un Nunchuk vient d'être branché dans le port pass-through.
                            // Tenter le passthrough Nunchuk (0x05) — EnableMotionPlus vérifie via l'ID extension.
                            // Si pas de Nunchuk, le Fix V21 gère le fallback proprement.
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus: attempting Nunchuk passthrough after extension change", PlayerIndex));
                            
                            // [FIX V22d] Start probing for Nunchuk behind MP (in case passthrough attempt failed/sync'd back to MP)
                            StartMpNunchukProbe();
                        }
                        else
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.NoExtension);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled (No extension / Standalone)", PlayerIndex));
                            
                            // [FIX V22d] Start probing for Nunchuk behind MP if we enabled MP standalone
                            StartMpNunchukProbe();
                        }
                        
                        // Ensure report type is maintained (MotionPlus activation can reset it)
                        // (EN/FR: S'assurer que le type de rapport est maintenu)
                        UpdateIRSensitivity();
                    }
                    finally
                    {
                        // [FIX V22g] EN: Always release the flag, even on exception
                        // FR: Toujours libérer le flag, même en cas d'exception
                        _mpActivationInProgress = false;
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning(string.Format("[P{0}] Failed to auto-enable MotionPlus: {1}", PlayerIndex, ex.Message));
                }
            });
        }

        // [FIX V22d] Helper methods for MP Nunchuk probing
        private void StartMpNunchukProbe()
        {
            if (_mpNunchukProbeTimer == null)
            {
                SimpleLogger.Instance.Info($"[P{PlayerIndex}] [MP Probe] Starting periodic Nunchuk probe timer (5s)");
                _mpNunchukProbeTimer = new System.Threading.Timer((state) =>
                {
                    try
                    {
                        if (Wiimote != null && Wiimote.IsConnected)
                        {
                            SimpleLogger.Instance.Info($"[P{PlayerIndex}] [MP Probe] Timer fired. Checking for Nunchuk...");
                            AutoEnableMotionPlus();
                        }
                        else
                        {
                            StopMpNunchukProbe();
                        }
                    }
                    catch { }
                }, null, 5000, 5000);
            }
        }

        private void StopMpNunchukProbe()
        {
            if (_mpNunchukProbeTimer != null)
            {
                SimpleLogger.Instance.Info($"[P{PlayerIndex}] [MP Probe] Stopping probe timer");
                _mpNunchukProbeTimer.Dispose();
                _mpNunchukProbeTimer = null;
            }
        }

        private void OnWiiMoteStateChanged(object sender, WiimoteStateEventArgs e)
        {
            if (e.WiimoteState == null)
                return;

            _lastReportTime = Options.Instance.UseHighPerfTimers 
                ? DateTime.UtcNow   // UtcNow is faster than Now (~0.1µs vs ~1µs) (EN/FR: UtcNow plus rapide que Now)
                : DateTime.Now;     // Standard fallback (EN/FR: Fallback standard)
            lock (_lock)
            {
                ButtonState buttons = e.WiimoteState.Buttons;
                IRState ir = e.WiimoteState.IRState;
                int velocityX = 0;
                int velocityY = 0;

                // --- MANUAL DISABLE HOTKEY (Off-Screen + Minus + Plus > 3s) ---
                bool isOffScreen = !ir.IRSensor0.Found && !ir.IRSensor1.Found;
                bool suppressMinusPlus = false;

                if (isOffScreen && buttons.Minus && buttons.Plus)
                {
                    if (_manualDisableStartTime == DateTime.MinValue)
                        _manualDisableStartTime = DateTime.Now;

                    if (!_manualDisableTriggered && (DateTime.Now - _manualDisableStartTime).TotalSeconds >= 3.0)
                    {
                        SimpleLogger.Instance.Info($"[P{PlayerIndex}] Manual Disable Hotkey Triggered (Off-Screen + Minus + Plus)");
                        ServiceClient.DisablePlayer(PlayerIndex);
                        Vibrate(e.Wiimote); // Feedback
                        _manualDisableTriggered = true;
                    }
                    
                    suppressMinusPlus = true;
                }
                else
                {
                    _manualDisableStartTime = DateTime.MinValue;
                    _manualDisableTriggered = false;
                }

                // Read and process gyroscope data (EN/FR: Lire et traiter données gyroscope)
                ProcessGyroscopeData(e.WiimoteState);

                // Activity is detected if any button is pressed, IR is detected, or nunchuk is moved
                bool hasNunchukForActivity = (e.WiimoteState.ExtensionType == ExtensionType.Nunchuk || e.WiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk);
                bool hasActivity = buttons.A || buttons.B || buttons.Up || buttons.Down || buttons.Left || buttons.Right ||
                                   buttons.One || buttons.Two || buttons.Plus || buttons.Minus || buttons.Home ||
                                   ir.IRSensor0.Found || ir.IRSensor1.Found ||
                                   (hasNunchukForActivity && 
                                    (e.WiimoteState.Nunchuk.C || e.WiimoteState.Nunchuk.Z || 
                                     Math.Abs(e.WiimoteState.Nunchuk.Joystick.X) > 0.15f || 
                                     Math.Abs(e.WiimoteState.Nunchuk.Joystick.Y) > 0.15f));
                
                if (hasActivity)
                    ResetSleepTimer();

                // Battery monitoring (EN/FR: Suivi batterie)
                // Log if significant change (> 5%) or every 5 minutes (EN/FR: Logger si changement significatif (> 5%) ou toutes les 5 min)
                float currentBattery = e.WiimoteState.Status.Battery;
                bool batteryChanged = Math.Abs(currentBattery - _lastBatteryLevel) > 5f;
                bool timeoutLog = (DateTime.Now - _lastBatteryLogTime).TotalMinutes >= 5;

                if (batteryChanged || timeoutLog)
                {
                    _lastBatteryLevel = currentBattery;
                    _lastBatteryLogTime = DateTime.Now;
                    SimpleLogger.Instance.Info(string.Format("[P{0}] Battery: {1:F1}%{2}", PlayerIndex, currentBattery, (e.WiimoteState.Status.BatteryLow ? " (LOW!)" : "")));
                }

                // Check if inputs are locked for button assignment (EN/FR: Vérifier si inputs verrouillés pour assignation)
                if (_inputsLocked)
                {
                    // In assignment mode: detect button press and fire event (EN/FR: En mode assignation : détecter pression bouton et déclencher événement)
                    DetectAndFireButtonEvent(buttons, e.WiimoteState);
                    return; // Don't process normal input (EN/FR: Ne pas traiter input normal)
                }

                bool hasExited = false;
                if (_runningProcess != null)
                {
                    try
                    {
                        hasExited = _runningProcess.HasExited;
                    }
                    catch (Exception)
                    {
                        // Access Denied usually means the process is running but we lack permissions to check it
                        // (EN/FR: Accès refusé signifie généralement que le processus tourne mais on manque de permissions)
                        // SimpleLogger.Instance.Debug(string.Format("Could not check if process {0} exited.", _runningProcess.ProcessName));
                    }
                }

                if (hasExited)
                {
                    if (_processLocking && (_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Mouse43 || _mode == WiiMoteMode.MouseFPS))
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


                    
                NunchukState nunchuk = e.WiimoteState.Nunchuk;
                bool hasNunchuk = e.WiimoteState.ExtensionType == ExtensionType.Nunchuk || e.WiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk;

                // Hotkey detection: notify HotkeyManager FIRST (EN/FR: Détection hotkeys : notifier d'abord)
                DetectHotkeyButtonChanges(buttons, _lastState, nunchuk, _lastNunchukState, hasNunchuk);

                // This enables "Autocalibration" (Gun4IR/RetroShooter layouts) for GamePad mode.
                var scaledPos = _calculator.GetScaledPosition(ir, buttons, _lastState);

                // Apply Aspect Ratio Correction (EN/FR: Appliquer correction de format d'image)
                if (scaledPos.HasValue)
                {
                    scaledPos = ApplyAspectRatioCorrection(scaledPos.Value, _mode);
                }

                ManageCalibration(e.Wiimote, buttons, _lastState, scaledPos);

                bool mLeft = false, mRight = false, mMiddle = false;
                int finalX = _lastX, finalY = _lastY;

                if (_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Mouse43 || _mode == WiiMoteMode.MouseFPS)
                {
                    bool wasCalibrating = _calculator.IsCalibrating;

                    int x = _lastX;
                    int y = _lastY;

                    // --- UNIFIED HYBRID IR + GYRO TRACKING (EN/FR: Tracking hybride IR + Gyro unifié) ---
                    // 1. Get primary mouse buttons (EN/FR: Lire les boutons principaux)
                    mLeft = isButtonPressed(SpecialAction.LeftMouse, buttons, nunchuk, hasNunchuk);
                    mRight = isButtonPressed(SpecialAction.RightMouse, buttons, nunchuk, hasNunchuk);
                    mMiddle = isButtonPressed(SpecialAction.MiddleMouse, buttons, nunchuk, hasNunchuk);

                    // 2. Gesture Logic (Shake, Grenade) (EN/FR: Logique des gestes)
                    bool shakeDetected = CheckShake(e.WiimoteState);
                    
                    // --- SHAKE INHIBITION FOR RELOAD ---
                    // EN: Check if shake is already mapped to a keyboard/mouse action to avoid double-firing reload
                    // FR: Vérifier si le shake est déjà mappé pour éviter un double déclenchement du rechargement
                    bool isShakeInhibited = false;
                    if (Options.Instance.ShakeFromNunchuk)
                    {
                        if (_playerMappings != null && _playerMappings.AccelNunchukShake != null && (_playerMappings.AccelNunchukShake.Special != SpecialAction.None || _playerMappings.AccelNunchukShake.Key != System.Windows.Forms.Keys.None)) isShakeInhibited = true;
                    }
                    else
                    {
                        if (_playerMappings != null && _playerMappings.AccelWiimoteShake != null && (_playerMappings.AccelWiimoteShake.Special != SpecialAction.None || _playerMappings.AccelWiimoteShake.Key != System.Windows.Forms.Keys.None)) isShakeInhibited = true;
                    }

                    if (shakeDetected && !isShakeInhibited) _gestureRightClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                    if (CheckGrenadeGesture(e.WiimoteState)) _gestureMiddleClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                    
                    if (_gestureRightClickFrameCount > 0) { mRight = true; _gestureRightClickFrameCount--; }
                    if (_gestureMiddleClickFrameCount > 0) { mMiddle = true; _gestureMiddleClickFrameCount--; }

                    // 3. Off-screen Reload Logic (EN/FR: Logique Rechargement Hors-écran)
                    bool isOnScreen = scaledPos.HasValue;
                    if (Options.Instance.EnableOffScreenReload && !isOnScreen)
                    {
                        if (Options.Instance.OffScreenReloadAuto)
                        {
                            if (_wasOnScreen) _offScreenReloadClickSequence = 1;
                            if (_offScreenReloadClickSequence > 0)
                            {
                                switch (_offScreenReloadClickSequence)
                                {
                                    case 1: mRight = true; break;
                                    case 2: mRight = false; break;
                                    case 3: mRight = true; break;
                                    case 4: mRight = false; _offScreenReloadClickSequence = -1; break;
                                }
                                _offScreenReloadClickSequence++;
                            }
                        }
                        else if (mLeft) { mLeft = false; mRight = true; }
                    }
                    _wasOnScreen = isOnScreen;

                    // 4. Update Tracking (Pure Gyro or Absolute IR) (EN/FR: Mise à jour du tracking)
                    if (wasCalibrating || _calculator.IsCalibrating)
                    {
                        if (isOnScreen && _virtualMouse != null)
                        {
                            _virtualMouse.UpdateMouse((int)scaledPos.Value.X, (int)scaledPos.Value.Y, false, false, false, true, true);
                            _lockUntilABreleased = true;
                        }
                    }
                    else if (!_lockUntilABreleased)
                    {

                        if (isOnScreen)
                        {
                            // --- ABSOLUTE IR TRACKING ---
                            velocityX = 0; velocityY = 0;
                            finalX = (int)scaledPos.Value.X;
                            finalY = (int)scaledPos.Value.Y;

                            // Smoothing
                            if (Options.Instance.EnableIRSmoothing && _lastX != 0 && _lastY != 0)
                            {
                                float alpha = 1.0f / Math.Max(1, Math.Min(10, Options.Instance.IRSmoothingStrength));
                                finalX = (int)(alpha * finalX + (1.0f - alpha) * _lastX);
                                finalY = (int)(alpha * finalY + (1.0f - alpha) * _lastY);
                            }

                            // Velocity & Extrapolation
                            if (_lastX != 0 && _lastY != 0)
                            {
                                velocityX = finalX - _lastX;
                                velocityY = finalY - _lastY;
                                if (Options.Instance.UseIRExtrapolation)
                                {
                                    finalX = (int)(finalX + velocityX * Options.Instance.IRExtrapolationStrength);
                                    finalY = (int)(finalY + velocityY * Options.Instance.IRExtrapolationStrength);
                                    finalX = Math.Max(0, Math.Min(65535, finalX));
                                    finalY = Math.Max(0, Math.Min(65535, finalY));
                                }
                            }

                            // Virtual Polling Storage setup
                            // Mouse will be updated at the end of the Mouse block after motion checks
                            _lastMoveCursor_Raw = true;

                            _lastX = finalX;
                            _lastY = finalY;
                            _lastIRSeenTime = GetNow();

                            // Virtual Polling Storage
                            _lastX_Raw = finalX;
                            _lastY_Raw = finalY;
                            _lastVelX_Diag = velocityX;
                            _lastVelY_Diag = velocityY;
                        }
                        else
                        {
                            _lastMoveCursor_Raw = false;
                        }

                        // Shared state for virtual polling and watchdog
                        if (_virtualMouse != null)
                        {
                            _lastLeft_Raw = mLeft;
                            _lastRight_Raw = mRight;
                            _lastMiddle_Raw = mMiddle;
                            _lastProcessingTime = GetNow();
                            _lastAnyReportTime = _lastProcessingTime;
                            _lastReportTime = _lastProcessingTime;
                        }
                    }

                    if (_lockUntilABreleased && !buttons.B && !buttons.A)
                        _lockUntilABreleased = false;

                    UpdateIRSensorStatus(isOnScreen);
                }

                if ((_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Mouse43 || _mode == WiiMoteMode.MouseFPS || _mode == WiiMoteMode.Keyboardpad) && _joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                {
                    // Mask inputs if specific button is consumed by hotkey (EN/FR: Masquer inputs si bouton consommé par hotkey)
                    SendKeyEvent(_playerMappings.WiiA, buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A"), _lastState.A);
                    SendKeyEvent(_playerMappings.WiiB, buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B"), _lastState.B);
                    SendKeyEvent(_playerMappings.WiiUp, buttons.Up && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Up") && !_isOffsetAdjustmentActive, _lastState.Up);
                    SendKeyEvent(_playerMappings.WiiDown, buttons.Down && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Down") && !_isOffsetAdjustmentActive, _lastState.Down);
                    SendKeyEvent(_playerMappings.WiiLeft, buttons.Left && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Left") && !_isOffsetAdjustmentActive, _lastState.Left);
                    SendKeyEvent(_playerMappings.WiiRight, buttons.Right && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Right") && !_isOffsetAdjustmentActive, _lastState.Right);
                    SendKeyEvent(_playerMappings.WiiOne, buttons.One && !HotkeyManager.IsButtonConsumed(PlayerIndex, "One"), _lastState.One);
                    SendKeyEvent(_playerMappings.WiiTwo, buttons.Two && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Two"), _lastState.Two);
                    SendKeyEvent(_playerMappings.WiiPlus, buttons.Plus && !suppressMinusPlus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus"), _lastState.Plus);
                    SendKeyEvent(_playerMappings.WiiMinus, buttons.Minus && !suppressMinusPlus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Minus"), _lastState.Minus);

                    if (hasNunchuk)
                    {
                        SendKeyEvent(_playerMappings.NunC, nunchuk.C && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunC"), _lastNunchukState.C);
                        SendKeyEvent(_playerMappings.NunZ, nunchuk.Z && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunZ"), _lastNunchukState.Z);
                        SendKeyEvent(_playerMappings.NunUp, nunchuk.Joystick.Y > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp"), _lastNunchukState.Joystick.Y > 0.3f);
                        SendKeyEvent(_playerMappings.NunDown, nunchuk.Joystick.Y < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown"), _lastNunchukState.Joystick.Y < -0.3f);
                        SendKeyEvent(_playerMappings.NunLeft, nunchuk.Joystick.X < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft"), _lastNunchukState.Joystick.X < -0.3f);
                        SendKeyEvent(_playerMappings.NunRight, nunchuk.Joystick.X > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight"), _lastNunchukState.Joystick.X > 0.3f);
                    }

                    // --- Keyboard/Mouse Motion Action Processor (EN/FR: Traitement Actions Souris via Mouvement) ---
                    Action<ButtonAction, bool, bool> processMouseMotion = (action, isPressed, wasPressed) => 
                    {
                        if (action == null) return;
                        if (action.Key != System.Windows.Forms.Keys.None) SendKeyEvent(action, isPressed, wasPressed);
                        if (action.Special == SpecialAction.LeftMouse && isPressed) mLeft = true;
                        if (action.Special == SpecialAction.RightMouse && isPressed) mRight = true;
                        if (action.Special == SpecialAction.MiddleMouse && isPressed) mMiddle = true;
                    };

                    float accXOff = 0, accYOff = 0, accZOff = 0;
                    float nunXOff = 0, nunYOff = 0, nunZOff = 0;
                    var calib = Options.Instance.GetCalibration(Wiimote != null ? Wiimote.UniqueId : "");
                    if (calib != null)
                    {
                        accXOff = calib.AccXOffset; accYOff = calib.AccYOffset; accZOff = calib.AccZOffset;
                        nunXOff = calib.NunAccXOffset; nunYOff = calib.NunAccYOffset; nunZOff = calib.NunAccZOffset;
                    }

                    // 1. Accel Wiimote
                    float wRawX = e.WiimoteState.Accel.Values.X;
                    float wRawY = e.WiimoteState.Accel.Values.Y;
                    float wRawZ = e.WiimoteState.Accel.Values.Z;
                    float wMotX = (wRawX - accXOff) * 6f * _playerMappings.AccelWiimoteSensitivity; // Default 0.5 sens = 3.0x multiplier
                    float wMotY = (wRawY - accYOff) * 6f * _playerMappings.AccelWiimoteSensitivity;
                    float wMotZ = (wRawZ - accZOff) * 6f * _playerMappings.AccelWiimoteSensitivity;
                    
                    float wDeadzone = _playerMappings.AccelWiimoteDeadzone;
                    bool wUp = wMotY > wDeadzone;
                    bool wDown = wMotY < -wDeadzone;
                    bool wLeft = wMotX < -wDeadzone;
                    bool wRight = wMotX > wDeadzone;
                    
                    float wDeltaX = Math.Abs(wMotX - _lastWMotX);
                    float wDeltaY = Math.Abs(wMotY - _lastWMotY);
                    float wDeltaZ = Math.Abs(wMotZ - _lastWMotZ);
                    float wDeltaTotal = wDeltaX + wDeltaY + wDeltaZ;
                    
                    _lastWMotX = wMotX;
                    _lastWMotY = wMotY;
                    _lastWMotZ = wMotZ;

                    // (EN/FR: Utiliser le delta pour détecter un vrai shake brusque et pas juste une inclinaison)
                    bool wShake = wDeltaTotal > (wDeadzone * 1.5f);

                    processMouseMotion(_playerMappings.AccelWiimoteUp, wUp && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelWiimoteUp"), _lastAccelWiimoteUp);
                    processMouseMotion(_playerMappings.AccelWiimoteDown, wDown && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelWiimoteDown"), _lastAccelWiimoteDown);
                    processMouseMotion(_playerMappings.AccelWiimoteLeft, wLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelWiimoteLeft"), _lastAccelWiimoteLeft);
                    processMouseMotion(_playerMappings.AccelWiimoteRight, wRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelWiimoteRight"), _lastAccelWiimoteRight);
                    processMouseMotion(_playerMappings.AccelWiimoteShake, wShake && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelWiimoteShake"), _lastAccelWiimoteShake);
                    
                    _lastAccelWiimoteUp = wUp; _lastAccelWiimoteDown = wDown; _lastAccelWiimoteLeft = wLeft; _lastAccelWiimoteRight = wRight; _lastAccelWiimoteShake = wShake;

                    // 2. Accel Nunchuk
                    if (hasNunchuk)
                    {
                        float nRawX = nunchuk.Accel.Values.X;
                        float nRawY = nunchuk.Accel.Values.Y;
                        float nRawZ = nunchuk.Accel.Values.Z;
                        float nMotX = (nRawX - nunXOff) * 6f * _playerMappings.AccelNunchukSensitivity;
                        float nMotY = (nRawY - nunYOff) * 6f * _playerMappings.AccelNunchukSensitivity;
                        float nMotZ = (nRawZ - nunZOff) * 6f * _playerMappings.AccelNunchukSensitivity;

                        float nDeadzone = _playerMappings.AccelNunchukDeadzone;
                        bool nUp = nMotY > nDeadzone;
                        bool nDown = nMotY < -nDeadzone;
                        bool nLeft = nMotX < -nDeadzone;
                        bool nRight = nMotX > nDeadzone;

                        float nDeltaX = Math.Abs(nMotX - _lastNMotX);
                        float nDeltaY = Math.Abs(nMotY - _lastNMotY);
                        float nDeltaZ = Math.Abs(nMotZ - _lastNMotZ);
                        float nDeltaTotal = nDeltaX + nDeltaY + nDeltaZ;
                        
                        _lastNMotX = nMotX;
                        _lastNMotY = nMotY;
                        _lastNMotZ = nMotZ;

                        bool nShake = nDeltaTotal > (nDeadzone * 1.5f);

                        processMouseMotion(_playerMappings.AccelNunchukUp, nUp && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelNunchukUp"), _lastAccelNunchukUp);
                        processMouseMotion(_playerMappings.AccelNunchukDown, nDown && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelNunchukDown"), _lastAccelNunchukDown);
                        processMouseMotion(_playerMappings.AccelNunchukLeft, nLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelNunchukLeft"), _lastAccelNunchukLeft);
                        processMouseMotion(_playerMappings.AccelNunchukRight, nRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelNunchukRight"), _lastAccelNunchukRight);
                        processMouseMotion(_playerMappings.AccelNunchukShake, nShake && !HotkeyManager.IsButtonConsumed(PlayerIndex, "AccelNunchukShake"), _lastAccelNunchukShake);

                        _lastAccelNunchukUp = nUp; _lastAccelNunchukDown = nDown; _lastAccelNunchukLeft = nLeft; _lastAccelNunchukRight = nRight; _lastAccelNunchukShake = nShake;
                    }

                    // 3. Gyro Motion Plus
                    if (e.WiimoteState.ExtensionType == ExtensionType.MotionPlus || e.WiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk)
                    {
                        float gSensMult = _playerMappings.GyroSensitivity * 2.0f; // Default 0.5 sens = 1.0x multiplier
                        float gMotX = (e.WiimoteState.MotionPlus.Values.Yaw / 500.0f) * gSensMult;
                        float gMotY = (e.WiimoteState.MotionPlus.Values.Pitch / 500.0f) * gSensMult;
                        float gMotZ = (e.WiimoteState.MotionPlus.Values.Roll / 500.0f) * gSensMult;

                        float gDeadzone = _playerMappings.GyroDeadzone;
                        bool gUp = gMotY < -gDeadzone;
                        bool gDown = gMotY > gDeadzone;
                        bool gLeft = gMotX < -gDeadzone;
                        bool gRight = gMotX > gDeadzone;
                        
                        float rollCooldownMs = 150f;
                        bool gRollLeft = gMotZ < -gDeadzone;
                        bool gRollRight = gMotZ > gDeadzone;

                        // Anti-wobble logic (EN/FR: Empêche le rebond physique du roll dans le sens inverse)
                        if (gRollLeft)
                        {
                            if ((GetNow() - _lastRollRightTime).TotalMilliseconds < rollCooldownMs)
                                gRollLeft = false;
                            else
                                _lastRollLeftTime = GetNow();
                        }
                        if (gRollRight)
                        {
                            if ((GetNow() - _lastRollLeftTime).TotalMilliseconds < rollCooldownMs)
                                gRollRight = false;
                            else
                                _lastRollRightTime = GetNow();
                        }

                        processMouseMotion(_playerMappings.GyroMotionPlusUp, gUp && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusUp"), _lastGyroMotionPlusUp);
                        processMouseMotion(_playerMappings.GyroMotionPlusDown, gDown && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusDown"), _lastGyroMotionPlusDown);
                        processMouseMotion(_playerMappings.GyroMotionPlusLeft, gLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusLeft"), _lastGyroMotionPlusLeft);
                        processMouseMotion(_playerMappings.GyroMotionPlusRight, gRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusRight"), _lastGyroMotionPlusRight);
                        processMouseMotion(_playerMappings.GyroMotionPlusRollLeft, gRollLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusRollLeft"), _lastGyroMotionPlusRollLeft);
                        processMouseMotion(_playerMappings.GyroMotionPlusRollRight, gRollRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "GyroMotionPlusRollRight"), _lastGyroMotionPlusRollRight);

                        _lastGyroMotionPlusUp = gUp; _lastGyroMotionPlusDown = gDown; _lastGyroMotionPlusLeft = gLeft; _lastGyroMotionPlusRight = gRight; _lastGyroMotionPlusRollLeft = gRollLeft; _lastGyroMotionPlusRollRight = gRollRight;
                    }

                    _joy.CommitChanges();

                    // --- FINALLY: Send Mouse State (EN/FR: ENFIN : Envoyer l'état de la souris) ---
                    if (_virtualMouse != null)
                    {

                        if (Options.Instance.EnableVirtualPolling)
                        {
                            if (_lastMoveCursor_Raw)
                                _virtualMouse.UpdateMouse(_lastX_Raw, _lastY_Raw, mLeft, mRight, mMiddle, false, false);
                            else
                                _virtualMouse.UpdateMouse(0, 0, mLeft, mRight, mMiddle, false, false);
                        }
                        else
                        {
                            if (_lastMoveCursor_Raw)
                                _virtualMouse.UpdateMouse(_lastX_Raw, _lastY_Raw, mLeft, mRight, mMiddle, true, true);
                            else
                                _virtualMouse.UpdateMouse(0, 0, mLeft, mRight, mMiddle, false, false);
                        }
                    }
                }


                if (_mode == WiiMoteMode.GamePad || _mode == WiiMoteMode.GamePad43 || _mode == WiiMoteMode.GamePadFPS)
                {
                    UpdateGamePadState(e.WiimoteState, scaledPos);
                }

                // [FIX] EN: Manual property-by-property copy to avoid reference tracking issues (most WiimoteLib versions reuse state objects)
                // FR: Copie manuelle propriété par propriété pour éviter les problèmes de suivi par référence
                _lastState.A = e.WiimoteState.Buttons.A;
                _lastState.B = e.WiimoteState.Buttons.B;
                _lastState.Plus = e.WiimoteState.Buttons.Plus;
                _lastState.Minus = e.WiimoteState.Buttons.Minus;
                _lastState.Home = e.WiimoteState.Buttons.Home;
                _lastState.One = e.WiimoteState.Buttons.One;
                _lastState.Two = e.WiimoteState.Buttons.Two;
                _lastState.Up = e.WiimoteState.Buttons.Up;
                _lastState.Down = e.WiimoteState.Buttons.Down;
                _lastState.Left = e.WiimoteState.Buttons.Left;
                _lastState.Right = e.WiimoteState.Buttons.Right;

                if (hasNunchuk)
                {
                    _lastNunchukState.C = e.WiimoteState.Nunchuk.C;
                    _lastNunchukState.Z = e.WiimoteState.Nunchuk.Z;
                    // Joy and Accel not used for hybrid button tracking but kept for consistency
                    _lastNunchukState.Joystick = e.WiimoteState.Nunchuk.Joystick;
                }
            }
        }

        private void SendKeyEvent(ButtonAction action, bool pressed, bool lastPressed)
        {
            if (pressed == lastPressed)
                return;

            _joy.SendKeyEvent(action.Key, pressed);
        }

        /// <summary>
        /// Detect button changes and notify HotkeyManager for hotkey processing
        /// (EN/FR: Détecter changements de boutons et notifier HotkeyManager pour traitement hotkeys)
        /// </summary>
        private void DetectHotkeyButtonChanges(ButtonState currentState, ButtonState lastState, NunchukState currentNunchuk, NunchukState lastNunchuk, bool hasNunchuk)
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

            if (hasNunchuk)
            {
                CheckButtonStateChange("NunC", currentNunchuk.C, lastNunchuk.C);
                CheckButtonStateChange("NunZ", currentNunchuk.Z, lastNunchuk.Z);

                // Nunchuk Stick Directions (Threshold > 0.3)
                CheckButtonStateChange("NunUp", currentNunchuk.Joystick.Y > 0.3f, lastNunchuk.Joystick.Y > 0.3f);
                CheckButtonStateChange("NunDown", currentNunchuk.Joystick.Y < -0.3f, lastNunchuk.Joystick.Y < -0.3f);
                CheckButtonStateChange("NunLeft", currentNunchuk.Joystick.X < -0.3f, lastNunchuk.Joystick.X < -0.3f);
                CheckButtonStateChange("NunRight", currentNunchuk.Joystick.X > 0.3f, lastNunchuk.Joystick.X > 0.3f);
            }
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
            if (!Options.Instance.EnableGrenadeGesture) return false;
            if ((DateTime.Now - _lastGrenadeTime).TotalMilliseconds < GRENADE_COOLDOWN_MS) return false;

            // Monitor Y axis for "Pump" action from selected device (EN/FR: Surveiller axe Y pour action "Pompe" sur l'appareil sélectionné)
            float y = 0;
            bool nunchukAvailable = (state.ExtensionType == ExtensionType.Nunchuk || state.ExtensionType == ExtensionType.MotionPlusNunchuk);
            
            if (Options.Instance.GrenadeFromNunchuk)
            {
                if (!nunchukAvailable) return false;
                y = state.Nunchuk.Accel.Values.Y;
            }
            else
            {
                y = state.Accel.Values.Y;
            }

            if (Math.Abs(y) > 10) y /= 28.0f; // Normalize if raw (EN/FR: Normaliser si raw)
            
            if (float.IsNaN(y) || float.IsInfinity(y)) return false; // Sanity check (EN/FR: Vérification de sécurité)
            
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

            // EN: Dynamic threshold based on device source - Wiimote (wrist) needs lower threshold than Nunchuk (arm)
            // FR: Seuil dynamique selon le device source - Wiimote (poignet) nécessite un seuil plus bas que Nunchuk (bras)
            float grenadeThreshold = Options.Instance.GrenadeFromNunchuk ? 3.5f : 2.5f;
            if (max - min > grenadeThreshold) // Large swing in Y acceleration
            {
                _lastGrenadeTime = DateTime.Now;
                _accelZHistory.Clear(); // Clear history to prevent re-triggering (EN/FR: Vider l'historique pour éviter re-déclenchement)
                SimpleLogger.Instance.Info(string.Format("Grenade gesture detected! DeltaY: {0:F2}", max - min));
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

            if (_playerMappings.WiiA != null && _playerMappings.WiiA.Special == action && buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A")) return true;
            if (_playerMappings.WiiB != null && _playerMappings.WiiB.Special == action && buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B")) return true;
            if (_playerMappings.WiiUp != null && _playerMappings.WiiUp.Special == action && buttons.Up && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Up")) return true;
            if (_playerMappings.WiiDown != null && _playerMappings.WiiDown.Special == action && buttons.Down && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Down")) return true;
            if (_playerMappings.WiiLeft != null && _playerMappings.WiiLeft.Special == action && buttons.Left && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Left")) return true;
            if (_playerMappings.WiiRight != null && _playerMappings.WiiRight.Special == action && buttons.Right && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Right")) return true;
            if (_playerMappings.WiiOne != null && _playerMappings.WiiOne.Special == action && buttons.One && !HotkeyManager.IsButtonConsumed(PlayerIndex, "One")) return true;
            if (_playerMappings.WiiTwo != null && _playerMappings.WiiTwo.Special == action && buttons.Two && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Two")) return true;
            if (_playerMappings.WiiPlus != null && _playerMappings.WiiPlus.Special == action && buttons.Plus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus")) return true;
            if (_playerMappings.WiiMinus != null && _playerMappings.WiiMinus.Special == action && buttons.Minus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Minus")) return true;

            if (hasNunchuk)
            {
                if (_playerMappings.NunC != null && _playerMappings.NunC.Special == action && nunchuk.C && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunC")) return true;
                if (_playerMappings.NunZ != null && _playerMappings.NunZ.Special == action && nunchuk.Z && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunZ")) return true;
                if (_playerMappings.NunUp != null && _playerMappings.NunUp.Special == action && nunchuk.Joystick.Y > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp")) return true;
                if (_playerMappings.NunDown != null && _playerMappings.NunDown.Special == action && nunchuk.Joystick.Y < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown")) return true;
                if (_playerMappings.NunLeft != null && _playerMappings.NunLeft.Special == action && nunchuk.Joystick.X < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft")) return true;
                if (_playerMappings.NunRight != null && _playerMappings.NunRight.Special == action && nunchuk.Joystick.X > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight")) return true;
            }

            return false;
        }

        private void SwitchMode(Wiimote wiimote)
        {
            int modeVal = (int)_mode;
            bool foundValidMode = false;

            while (!foundValidMode)
            {
                modeVal++;

                // Wrap around if past Disabled (EN/FR: Boucler si au-delà de Disabled)
                if (modeVal > (int)WiiMoteMode.Disabled)
                    modeVal = 0;

                WiiMoteMode nextMode = (WiiMoteMode)modeVal;
                foundValidMode = true;

                // Skip GamePad modes if option is not enabled (EN/FR: Passer les modes GamePad si option non activée)
                if ((nextMode == WiiMoteMode.GamePad || nextMode == WiiMoteMode.GamePad43 || nextMode == WiiMoteMode.GamePadFPS) 
                    && !Options.Instance.EnableGamePadSwapMode)
                {
                    foundValidMode = false;
                    continue;
                }

                // Skip FPS modes if option is not enabled (EN/FR: Passer les modes FPS si option non activée)
                if ((nextMode == WiiMoteMode.MouseFPS || nextMode == WiiMoteMode.GamePadFPS) 
                    && !Options.Instance.EnableFPSMode)
                {
                    foundValidMode = false;
                    continue;
                }
            }

            int mode = modeVal;

            // Handle leaving previous mode (EN/FR: Gérer la sortie du mode précédent)
            WiiMoteMode previousMode = _mode;
            _mode = (WiiMoteMode)mode;

            // EN/FR: Reset Hybrid state when changing mode
            _isHybridToggleActive = false;
            _lastHybridActive = false;
            _profileWantsHybridMouse = false; // Reset tracking on mode switch (EN/FR: Reset lors du changement de mode)
            _lastRuntimeWantsMouse = false;

            // Handle Col06 gamepad enable/disable via service
            // (EN/FR: Gérer activation/désactivation Col06 gamepad via service)
            if ((previousMode == WiiMoteMode.GamePad || previousMode == WiiMoteMode.GamePad43 || previousMode == WiiMoteMode.GamePadFPS) && 
                (_mode != WiiMoteMode.GamePad && _mode != WiiMoteMode.GamePad43 && _mode != WiiMoteMode.GamePadFPS))
            {
                // Leaving GamePad mode - disconnect and request Col06 removal
                // (EN/FR: Quitter mode GamePad - déconnecter et demander suppression Col06)
                try
                {
                    if (_virtualGamepad != null)
                    {
                        _virtualGamepad.ResetAll();
                        _virtualGamepad.Disconnect();
                    }

                    if (!Options.Instance.PersistentGamePads || !Options.Instance.EnableGamePadSwapMode)
                    {
                        WiimoteGun.ServiceClient.RemoveGamepad(PlayerIndex);
                    }
                    else
                    {
                        SimpleLogger.Instance.Info(string.Format("[GamePad P{0}] Keeping Col06 persistent.", PlayerIndex));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning(string.Format("[GamePad] Error removing Col06 for P{0}: {1}", PlayerIndex, ex.Message));
                }
            }

            if (_mode == WiiMoteMode.GamePad || _mode == WiiMoteMode.GamePad43 || _mode == WiiMoteMode.GamePadFPS)
            {
                // Entering GamePad mode - enable Col06 and connect
                // (EN/FR: Entrer mode GamePad - activer Col06 et connecter)
                try
                {
                    // Initialize Virtual Gamepad settings (EN/FR: Initialiser les paramètres du Gamepad Virtuel)
                    var gamepadMappings = Options.Instance.GetGamePadMappingsForPlayer(PlayerIndex);
                    bool useXInput = gamepadMappings != null && gamepadMappings.UseXInput;

                    // EN: Disable Mouse (COL03) in GamePad mode to avoid interference ONLY if Hybrid mode is disabled or doesn't use mouse features
                    // FR: Désactiver la souris (COL03) en mode GamePad pour éviter les interférences SEULEMENT si le mode Hybride est désactivé ou n'utilise pas la souris
                    bool gestureWantsMouse = Options.Instance.EnableShakeReload || Options.Instance.EnableGrenadeGesture;
                    bool hybridWantsMouse = (gamepadMappings != null && 
                                           !string.IsNullOrEmpty(gamepadMappings.HybridTriggerButton) && 
                                           gamepadMappings.HybridTriggerButton != "None" &&
                                           (gamepadMappings.IRHybridAsMouse || gamepadMappings.HasAnyHybridMouseAction()))
                                           || gestureWantsMouse;

                    if (!hybridWantsMouse)
                    {
                        WiimoteGun.ServiceClient.RemoveMouseForPlayer(PlayerIndex);
                    }
                    else
                    {
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Hybrid Mode configured - Keeping VMulti Mouse active...", PlayerIndex));
                        WiimoteGun.ServiceClient.EnablePlayer(PlayerIndex); // Ensure VMulti mouse service is active
                        if (_virtualMouse != null && _virtualMouse is VirtualVMultiMouse vmm)
                        {
                            // EN: Force refresh to ensure COL03 is picked up after service enablement (FR: Forcer rafraîchissement pour capter COL03)
                            vmm.RefreshDevice();
                        }
                    }

                    _profileWantsHybridMouse = hybridWantsMouse;

                    if (useXInput)
                    {
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Switching to XInput mode - Ensuring VMulti GamePad (Col06) is disabled...", PlayerIndex));
                        WiimoteGun.ServiceClient.RemoveGamepad(PlayerIndex);
                    }
                    else
                    {
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Switching to VMulti GamePad mode - Requesting Col06 enable...", PlayerIndex));
                        WiimoteGun.ServiceClient.EnableGamepad(PlayerIndex);
                    }

                    // Initialize Virtual Gamepad if needed (EN/FR: Initialiser le Gamepad Virtuel si nécessaire)

                    if (_virtualGamepad == null || (useXInput != (_virtualGamepad is ViGEmGamepad)))
                    {
                        if (_virtualGamepad != null)
                        {
                            _virtualGamepad.Disconnect();
                            _virtualGamepad.Dispose();
                        }

                        if (useXInput)
                        {
                            _virtualGamepad = new ViGEmGamepad(PlayerIndex);
                        }
                        else
                        {
                            _virtualGamepad = new VMultiGamepad(PlayerIndex);
                        }
                    }

                    // Small delay then connect (Col06 needs to be enabled first)
                    // (EN/FR: Petit délai puis connecter - Col06 doit être activé d'abord)
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(200);
                        try
                        {
                            if (_virtualGamepad != null && !_virtualGamepad.IsConnected)
                            {
                                _virtualGamepad.Connect();
                            }

                            // EN/FR: Log DirectInput Index for the virtual gamepad
                            // Identifier et logger l'index DirectInput pour le gamepad virtuel
                            RefreshDInputIndex();

                            // EN/FR: Ensure IR mode is active even in GamePad mode for lightgun tracking
                            wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true);
                        }
                        catch (Exception ex)
                        {
                            SimpleLogger.Instance.Error(string.Format("[GamePad] Connect error for P{0}: {1}", PlayerIndex, ex.Message));
                        }
                    });
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error(string.Format("[GamePad] Error enabling Col06 for P{0}: {1}", PlayerIndex, ex.Message));
                }
            }

            if (_hiddenWnd != null)
            {
                // Map complex modes to 1-4 for legacy display if needed, but better to use notifications
                int displayMode = (int)_mode;
                if (displayMode > 3) displayMode = 3; // Cap for legacy UI if it only expects 1-4
                _hiddenWnd.SetMode(displayMode + 1);
            }

            if (_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Mouse43 || _mode == WiiMoteMode.MouseFPS)
            {
                // EN: Enable Mouse (COL03) when in Mouse mode
                // FR: Activer la souris (COL03) quand on est en mode Mouse
                WiimoteGun.ServiceClient.EnablePlayer(PlayerIndex);

                ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true);
                    }
                    catch { }
                });

                // Log initial battery level (EN/FR: Logger le niveau de batterie initial)
                _lastBatteryLevel = wiimote.WiimoteState.Status.Battery;
                _lastBatteryLogTime = DateTime.Now;
                SimpleLogger.Instance.Info(string.Format("[P{0}] Battery connected: {1:F1}% {2}", PlayerIndex, _lastBatteryLevel, (wiimote.WiimoteState.Status.BatteryLow ? "(LOW!)" : "")));
            }

            if (_mode == WiiMoteMode.Keyboardpad)
            {
                // EN: Disable Mouse (COL03) in Keyboardpad mode
                // FR: Désactiver la souris (COL03) en mode Keyboardpad
                WiimoteGun.ServiceClient.RemoveMouseForPlayer(PlayerIndex);

                // EN/FR: Ensure IR mode is active even in Keyboardpad mode for lightgun tracking
                ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true);
                    }
                    catch { }
                });
            }

            if (_mode == WiiMoteMode.Disabled)
            {
                // EN: Disable Mouse (COL03) when Wiimote is disabled
                // FR: Désactiver la souris (COL03) quand la Wiimote est désactivée
                WiimoteGun.ServiceClient.RemoveMouseForPlayer(PlayerIndex);
            }

            string modeName = _mode.ToString();
            // User friendly names (EN)
            switch(_mode)
            {
                case WiiMoteMode.Mouse: modeName = "Mouse"; break;
                case WiiMoteMode.Mouse43: modeName = "Mouse (4:3)"; break;
                case WiiMoteMode.MouseFPS: modeName = "Mouse (FPS)"; break;
                case WiiMoteMode.GamePad: modeName = "GamePad"; break;
                case WiiMoteMode.GamePad43: modeName = "GamePad (4:3)"; break;
                case WiiMoteMode.GamePadFPS: modeName = "GamePad (FPS)"; break;
                case WiiMoteMode.Keyboardpad: modeName = "Keyboardpad"; break;
                case WiiMoteMode.Disabled: modeName = "Disabled"; break;
            }

            if (_mode == WiiMoteMode.Disabled)
            {
                Program.Notify(string.Format("WiimoteGun P{0} : {1}", PlayerIndex, modeName));
            }
            else
            {
                Program.Notify(string.Format("WiimoteGun P{0} : {1} activated", PlayerIndex, modeName));
                if (_mode == WiiMoteMode.GamePad || _mode == WiiMoteMode.GamePad43 || _mode == WiiMoteMode.GamePadFPS)
                {
                    string activeProfile = Program.GetActiveGamePadProfileName();
                    if (!string.IsNullOrEmpty(activeProfile))
                    {
                        // EN: Delay profile notification to appear after mode notification (avoid overlap)
                        // FR: Retarder la notification du profil pour qu'elle apparaisse après la notification de mode (éviter chevauchement)
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            System.Threading.Thread.Sleep(2500); // 2.5 second delay to be sure first one is gone
                            Program.Notify($"GamePad Profile: {activeProfile}");
                        });
                    }
                }
            }

            // EN: Trigger profile updates when mode changes to ensure tags are updated in emulators
            // FR: Déclencher la mise à jour des profils lors du changement de mode pour mettre à jour les tags
            Program.WiiMoteManager?.RefreshAllDInputIndices();
        }

        public static event EventHandler OverlayRequested;
        private bool _overlayTriggered = false;

        private void ManageCalibration(Wiimote wiimote, ButtonState buttons, ButtonState lastState, Point2F? scaledPos)
        {
            if (_calculator.IsCalibrating)
                return;

            // Check for Home + D-pad or Minus + D-pad combo for IN-GAME offset adjustment 
    // (EN/FR: Vérifier combo Home/Minus + D-pad pour ajustement offset EN JEU)
    bool modifierPressed = buttons.Home || (!wiimote.Device.IsBluetooth && buttons.Minus);
    bool dpadPressed = buttons.Up || buttons.Down || buttons.Left || buttons.Right;
    bool isOffsetComboActive = modifierPressed && dpadPressed;
    bool wasOffsetAdjustmentActive = _isOffsetAdjustmentActive;
    
    // Check if we are in the grace period (fade-out phase)
    // (EN/FR: Vérifier si nous sommes dans la période de grâce (phase de disparition))
    bool isGracePeriodActive = (DateTime.Now - _offsetAdjustmentEndTime).TotalMilliseconds < OFFSET_OVERLAY_FADE_MS;

    // Process if active OR in grace period (EN/FR: Traiter si actif OU en période de grâce)
    if (isOffsetComboActive || (_isOffsetAdjustmentActive && modifierPressed) || isGracePeriodActive)
    {
        // Calculate pixel position for overlay anyway to have real-time tracking (EN/FR: Calculer position pixel pour suivi temps réel)
        System.Drawing.Point? irPixelPos = null;
        if (scaledPos.HasValue)
        {
            var screen = System.Windows.Forms.Screen.AllScreens[ScreenIndex];
            int px = (int)((scaledPos.Value.X / 65535f) * screen.Bounds.Width) + screen.Bounds.Left;
            int py = (int)((scaledPos.Value.Y / 65535f) * screen.Bounds.Height) + screen.Bounds.Top;
            irPixelPos = new System.Drawing.Point(px, py);
        }

        int currentOffsetX = Options.Instance.GetDynamicPerspectiveOffsetX(PlayerIndex);
        int currentOffsetY = Options.Instance.GetDynamicPerspectiveOffsetY(PlayerIndex);

        if (isOffsetComboActive || (_isOffsetAdjustmentActive && modifierPressed))
        {
            // --- ACTIVE ADJUSTMENT MODE (EN/FR: MODE AJUSTEMENT ACTIF) ---
            if (!_isOffsetAdjustmentActive)
            {
                _isOffsetAdjustmentActive = true;
                SimpleLogger.Instance.Info(string.Format("[P{0}] Offset adjustment mode activated", PlayerIndex));
            }

            // Apply offset changes ONLY IF D-pad is pressed (limit repeat rate)
            // (EN/FR: Appliquer changements offset SEULEMENT SI D-pad pressé)
            if (dpadPressed && (DateTime.Now - _lastOffsetAdjustTime).TotalMilliseconds >= OFFSET_ADJUST_REPEAT_MS)
            {
                bool changed = false;
                
                if (buttons.Left) { currentOffsetX--; changed = true; }
                else if (buttons.Right) { currentOffsetX++; changed = true; }
                
                if (buttons.Up) { currentOffsetY--; changed = true; }
                else if (buttons.Down) { currentOffsetY++; changed = true; }
                
                if (changed)
                {
                    // Clamp values (-200 to +200) (EN/FR: Limiter valeurs)
                    currentOffsetX = Math.Max(-200, Math.Min(200, currentOffsetX));
                    currentOffsetY = Math.Max(-200, Math.Min(200, currentOffsetY));
                    
                    Options.Instance.SetDynamicPerspectiveOffsetX(PlayerIndex, currentOffsetX);
                    Options.Instance.SetDynamicPerspectiveOffsetY(PlayerIndex, currentOffsetY);
                    _lastOffsetAdjustTime = DateTime.Now;
                }
            }

            // Notify overlay (isActive: true)
            OffsetAdjustmentChanged?.Invoke(PlayerIndex, currentOffsetX, currentOffsetY, true, irPixelPos);
            
            ticks = -1; // Cancel Home button standard action (EN/FR: Annuler action standard bouton Home)
            return; // Don't process other Home combinations (EN/FR: Ne pas traiter autres combinaisons Home)
        }
        else
        {
            // --- GRACE PERIOD / FADE-OUT (EN/FR: PÉRIODE DE GRÂCE / DISPARITION) ---
            // Continue sending tracking updates with isActive: false
            OffsetAdjustmentChanged?.Invoke(PlayerIndex, currentOffsetX, currentOffsetY, false, irPixelPos);
        }
    }
    
    // Auto-save when modifier button is released (EN/FR: Auto-save quand bouton modificateur relâché)
    if (wasOffsetAdjustmentActive && !modifierPressed)
    {
        _isOffsetAdjustmentActive = false;
        _offsetAdjustmentEndTime = DateTime.Now; // Start grace period timer
        
        int finalOffsetX = Options.Instance.GetDynamicPerspectiveOffsetX(PlayerIndex);
        int finalOffsetY = Options.Instance.GetDynamicPerspectiveOffsetY(PlayerIndex);
        Options.Instance.Save();
        SimpleLogger.Instance.Info($"[P{PlayerIndex}] Offset adjustment saved: X={finalOffsetX}, Y={finalOffsetY}");
        
        // Notify overlay to hide (start fade) but keep IR position for seamless tracking
        // (EN/FR: Notifier début disparition mais garder position IR pour suivi fluide)
        System.Drawing.Point? irPixelPos = null;
        if (scaledPos.HasValue)
        {
            var screen = System.Windows.Forms.Screen.AllScreens[ScreenIndex];
            int px = (int)((scaledPos.Value.X / 65535f) * screen.Bounds.Width) + screen.Bounds.Left;
            int py = (int)((scaledPos.Value.Y / 65535f) * screen.Bounds.Height) + screen.Bounds.Top;
            irPixelPos = new System.Drawing.Point(px, py);
        }
        OffsetAdjustmentChanged?.Invoke(PlayerIndex, finalOffsetX, finalOffsetY, false, irPixelPos);
    }
    
    // Final safety reset when no modifier is pressed
    if (!modifierPressed)
    {
        _isOffsetAdjustmentActive = false;
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

            // CRITICAL: Check if Home is consumed by HotkeyManager (EN/FR: Vérifier si Home consommé par HotkeyManager)
            // Checked AFTER specific combos to allow them to work (EN/FR: Vérifié APRÈS combos spécifiques pour les laisser fonctionner)
            if (HotkeyManager.IsButtonConsumed(PlayerIndex, "Home"))
            {
                ticks = -1; // Suppress Home native functions (Mode Switch, Calibration)
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
                // EN: Disable locking if current Wiimote is in GamePad mode
                // FR: Désactiver le verrouillage si la Wiimote actuelle est en mode GamePad
                if (_mode != WiiMoteMode.GamePad && _mode != WiiMoteMode.GamePad43 && _mode != WiiMoteMode.GamePadFPS)
                {
                    locks = true;
                }
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
                        // Reuse _rumbleStopTimer to prevent garbage collection of the callback
                        // (EN/FR: Réutiliser _rumbleStopTimer pour éviter le ramasse-miettes)
                        _rumbleStopTimer?.Change(durationMs, Timeout.Infinite);
                        
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
            // Disarm stop timer (EN/FR: Désactiver le timer d'arrêt)
            _rumbleStopTimer?.Change(Timeout.Infinite, Timeout.Infinite);

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
            // Trigger just pressed (rising edge) (EN/FR: Gâchette vient d'être pressée)
            if (isPressed && !_isTriggerPressed)
            {
                _isTriggerPressed = true; // Update state early for logic (EN/FR: Màj état tôt pour la logique)

                if (Options.Instance.GetEnableWeaponRumble(PlayerIndex) && _hasIRSensor)
                {
                    TriggerWeaponRumble();
                    
                    // Start continuous rumble timer if enabled (EN/FR: Démarrer timer vibration continue si activé)
                    if (Options.Instance.GetAllowContinuousRumble(PlayerIndex))
                    {
                        int intervalMs = Options.Instance.GetRumbleRepetitionMs(PlayerIndex);
                        _rumbleTimer?.Change(intervalMs, intervalMs);
                    }
                }
            }
            // Trigger released (falling edge) (EN/FR: Gâchette relâchée)
            else if (!isPressed && _isTriggerPressed)
            {
                _isTriggerPressed = false; // Always update state (EN/FR: Toujours màj l'état)

                // Stop continuous rumble (EN/FR: Arrêter vibration continue)
                _rumbleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                StopRumble();
            }
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
            if (state.ExtensionType == ExtensionType.Nunchuk || state.ExtensionType == ExtensionType.MotionPlusNunchuk)
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
            SimpleLogger.Instance.Info(string.Format("P{0}: Button {1} pressed in assignment mode", PlayerIndex, buttonName));
            ButtonPressed?.Invoke(this, new ButtonPressedEventArgs(PlayerIndex, buttonName));
        }
        
        /// <summary>
        /// Set input lock state for button assignment mode (EN/FR: Définir état verrouillage pour mode assignation)
        /// </summary>
        public static void SetInputLock(bool locked)
        {
            _inputsLocked = locked;
            SimpleLogger.Instance.Info(string.Format("Wiimote inputs {0} for button assignment", (locked ? "LOCKED" : "UNLOCKED")));
        }
        private bool IsGamePadButtonPressed(string buttonId, ButtonState btnState, NunchukState nunchukState, bool hasNunchuk)
        {
            switch (buttonId)
            {
                case "WiiA": return btnState.A;
                case "WiiB": return btnState.B;
                case "Wii1": return btnState.One;
                case "Wii2": return btnState.Two;
                case "WiiPlus": return btnState.Plus;
                case "WiiMinus": return btnState.Minus;
                case "WiiUp": return btnState.Up;
                case "WiiDown": return btnState.Down;
                case "WiiLeft": return btnState.Left;
                case "WiiRight": return btnState.Right;
                case "WiiHome": return btnState.Home;
                case "NunchukC": return hasNunchuk && nunchukState.C;
                case "NunchukZ": return hasNunchuk && nunchukState.Z;
                default: return false;
            }
        }

        private void UpdateGamePadState(WiimoteState state, Point2F? scaledPos)
        {
            try
            {
                if (_virtualGamepad == null)
                    return;

                GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(PlayerIndex);
                if (mappings == null) return;

                // EN: Dynamic hybrid mouse detection (FR: Détection dynamique de la souris hybride)
                // This ensures COL03 is removed/restored if user changes profile at runtime
                bool hybridWantsMouse = !string.IsNullOrEmpty(mappings.HybridTriggerButton) && 
                                       mappings.HybridTriggerButton != "None" &&
                                       (mappings.IRHybridAsMouse || mappings.HasAnyHybridMouseAction());

                if (hybridWantsMouse != _profileWantsHybridMouse)
                {
                    _profileWantsHybridMouse = hybridWantsMouse;
                    if (hybridWantsMouse)
                    {
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Hybrid Profile detected at runtime - Enabling VMulti Mouse", PlayerIndex));
                        WiimoteGun.ServiceClient.EnablePlayer(PlayerIndex);
                        if (_virtualMouse != null && _virtualMouse is VirtualVMultiMouse vmm)
                        {
                            vmm.RefreshDevice();
                        }
                    }
                    else
                    {
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Non-Hybrid Profile detected at runtime - Disabling VMulti Mouse", PlayerIndex));
                        WiimoteGun.ServiceClient.RemoveMouseForPlayer(PlayerIndex);
                    }
                }

                // Check if user changed XInput mode at runtime
                if (mappings.UseXInput != (_virtualGamepad is ViGEmGamepad))
                {
                    SimpleLogger.Instance.Info($"[GamePad P{PlayerIndex}] Output mode changed at runtime. Re-initializing virtual gamepad...");
                    _virtualGamepad.Disconnect();
                    _virtualGamepad.Dispose();
                    
                    if (mappings.UseXInput)
                    {
                        _virtualGamepad = new ViGEmGamepad(PlayerIndex);
                        WiimoteGun.ServiceClient.RemoveGamepad(PlayerIndex); // Disable VMulti Col06 (EN/FR: Désactiver VMulti Col06)
                    }
                    else
                    {
                        _virtualGamepad = new VMultiGamepad(PlayerIndex);
                        WiimoteGun.ServiceClient.EnableGamepad(PlayerIndex); // Enable VMulti Col06 (EN/FR: Activer VMulti Col06)
                    }
                        
                    _virtualGamepad.Connect();
                    
                    // Allow some time for connection before sending reports to avoid dropping the first state
                    Thread.Sleep(100);
                }

                if (!_virtualGamepad.IsConnected)
                    return;

                // Logging (EN/FR: Log pour vérifier l'activité du mode GamePad)
                if (_debugCounter % 500 == 0)
                {
                    SimpleLogger.Instance.Info(string.Format("[GamePadActivity] P{0} Mode={1} Motion={2}", PlayerIndex, Mode, mappings.MotionMode));
                }

                // --- Buttons ---
                // Suppress Home / Minus / DPAD if they are being used for offset adjustment
                // Also suppress ANY button consumed by a hotkey combo
                bool homePressed = state.Buttons.Home && !_isOffsetAdjustmentActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Home");
                bool minusPressed = state.Buttons.Minus && !_isOffsetAdjustmentActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Minus");
                bool dpadActive = !_isOffsetAdjustmentActive;

                // --- Hybrid Mode Logic ---
                bool hasNunchuk = state.ExtensionType == ExtensionType.Nunchuk || state.ExtensionType == ExtensionType.MotionPlusNunchuk;
                bool isTriggerPressed = false;
                bool wasTriggerPressed = false;

                if (!string.IsNullOrEmpty(mappings.HybridTriggerButton) && mappings.HybridTriggerButton != "None")
                {
                    isTriggerPressed = IsGamePadButtonPressed(mappings.HybridTriggerButton, state.Buttons, state.Nunchuk, hasNunchuk);
                    wasTriggerPressed = IsGamePadButtonPressed(mappings.HybridTriggerButton, _lastState, _lastNunchukState, hasNunchuk);
                }

                // Handle Toggle vs Hold (EN/FR: Gérer Bascule vs Maintien)
                bool isHybridActive = false;
                bool wasHybridActive = _lastHybridActive;

                if (mappings.HybridToggle)
                {
                    if (isTriggerPressed && !wasTriggerPressed)
                    {
                        _isHybridToggleActive = !_isHybridToggleActive;
                        if (_isHybridToggleActive) _hybridActivationTime = GetNow();
                        else _hybridDeactivationTime = GetNow();
                        SimpleLogger.Instance.Info(string.Format("[P{0}] Hybrid Toggle: {1}", PlayerIndex, _isHybridToggleActive ? "ON" : "OFF"));
                    }
                    isHybridActive = _isHybridToggleActive;
                }
                else
                {
                    if (isTriggerPressed && !wasTriggerPressed) _hybridActivationTime = GetNow();
                    if (!isTriggerPressed && wasTriggerPressed) _hybridDeactivationTime = GetNow();
                    isHybridActive = isTriggerPressed;
                }

                bool physicalHybridActive = isHybridActive;
                // logicalHybridActive persists for 20ms after physical release (EN/FR: État logique persiste 20ms après relâchement physique)
                bool logicalHybridActive = physicalHybridActive || (GetNow() - _hybridDeactivationTime).TotalMilliseconds < 20;
                
                // Use logicalHybridActive for Gamepad suppression and Mouse mode activation
                isHybridActive = logicalHybridActive;

                // (EN/FR: État hybride stable après le délai d'activation de 50ms)
                // (Stable hybrid state after the 50ms activation delay)
                bool isHybridStable = isHybridActive && (GetNow() - _hybridActivationTime).TotalMilliseconds >= 50;

                bool hLeft = false, hRight = false, hMiddle = false;

                // (EN/FR: Les actions hybrides s'exécutent si le mode est actif OU si le bouton est celui qui active le mode)
                // (This fixes the bug where the trigger button itself wouldn't fire its action)
                Action<ButtonAction, bool, bool, string> execHybrid = (action, isPressed, wasPressed, buttonId) => {
                    if (action == null) return;
                    
                    // Logic: A button fires its hybrid action if (Mode is Active OR it's the Trigger Button)
                    // We must use 'isPressed' for the state and 'effectivePressed != effectiveLastPressed' for transitions.
                    bool effectivePressed = (isHybridActive || (buttonId == mappings.HybridTriggerButton)) && isPressed;
                    bool effectiveLastPressed = (wasHybridActive || (buttonId == mappings.HybridTriggerButton)) && wasPressed;

                    if (action.Key != System.Windows.Forms.Keys.None && _joy != null && _joy.IsEnabled) 
                    {
                        SendKeyEvent(action, effectivePressed, effectiveLastPressed);
                    }
                    if (action.Special == SpecialAction.LeftMouse && effectivePressed) 
                    {
                        // (EN/FR: Ajouter un léger délai si l'action est déclenchée par le bouton de gâchette hybride lui-même)
                        // (Allows games to transition from GamePad to Mouse input mode)
                        bool isTriggerBtn = (buttonId == mappings.HybridTriggerButton);
                        if (isTriggerBtn && (GetNow() - _hybridActivationTime).TotalMilliseconds < 20)
                        {
                            // Skip this frame (EN/FR: Ignorer cette frame)
                        }
                        else
                        {
#pragma warning disable CS0219
                            hLeft = true;
#pragma warning restore CS0219
                        }
                    }
                    if (action.Special == SpecialAction.RightMouse && effectivePressed)
                    {
                        bool isTriggerBtn = (buttonId == mappings.HybridTriggerButton);
                        if (isTriggerBtn && (GetNow() - _hybridActivationTime).TotalMilliseconds < 20) { }
                        else 
                        {
#pragma warning disable CS0219
                            hRight = true;
#pragma warning restore CS0219
                        }
                    }
                    if (action.Special == SpecialAction.MiddleMouse && effectivePressed)
                    {
                        bool isTriggerBtn = (buttonId == mappings.HybridTriggerButton);
                        if (isTriggerBtn && (GetNow() - _hybridActivationTime).TotalMilliseconds < 20) { }
                        else
                        {
#pragma warning disable CS0219
                            hMiddle = true;
#pragma warning restore CS0219
                        }
                    }
                };

                execHybrid(mappings.WiiAHybrid, state.Buttons.A, _lastState.A, "WiiA");
                execHybrid(mappings.WiiBHybrid, state.Buttons.B, _lastState.B, "WiiB");
                execHybrid(mappings.Wii1Hybrid, state.Buttons.One, _lastState.One, "Wii1");
                execHybrid(mappings.Wii2Hybrid, state.Buttons.Two, _lastState.Two, "Wii2");
                execHybrid(mappings.WiiPlusHybrid, state.Buttons.Plus, _lastState.Plus, "WiiPlus");
                execHybrid(mappings.WiiMinusHybrid, state.Buttons.Minus, _lastState.Minus, "WiiMinus");
                execHybrid(mappings.WiiUpHybrid, state.Buttons.Up, _lastState.Up, "WiiUp");
                execHybrid(mappings.WiiDownHybrid, state.Buttons.Down, _lastState.Down, "WiiDown");
                execHybrid(mappings.WiiLeftHybrid, state.Buttons.Left, _lastState.Left, "WiiLeft");
                execHybrid(mappings.WiiRightHybrid, state.Buttons.Right, _lastState.Right, "WiiRight");
                execHybrid(mappings.WiiHomeHybrid, state.Buttons.Home, _lastState.Home, "WiiHome");
                
                if (hasNunchuk) 
                {
                    execHybrid(mappings.NunchukCHybrid, state.Nunchuk.C, _lastNunchukState.C, "NunchukC");
                    execHybrid(mappings.NunchukZHybrid, state.Nunchuk.Z, _lastNunchukState.Z, "NunchukZ");
                }

                // EN: Gesture Logic (Shake Reload, Grenade) - also runs in GamePad/Hybrid mode
                // FR: Logique des gestes (Shake Reload, Grenade) - s'exécute aussi en mode GamePad/Hybride
                if (CheckShake(state)) _gestureRightClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                if (CheckGrenadeGesture(state)) _gestureMiddleClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;

                if (_gestureRightClickFrameCount > 0) { hRight = true; _gestureRightClickFrameCount--; }
                if (_gestureMiddleClickFrameCount > 0) { hMiddle = true; _gestureMiddleClickFrameCount--; }

                _lastHybridActive = isHybridActive;

                // Regular GamePad Buttons (disabled during hybrid)
                _virtualGamepad.SetButton(mappings.WiiA, !isHybridActive && state.Buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A"));
                _virtualGamepad.SetButton(mappings.WiiB, !isHybridActive && state.Buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B"));
                _virtualGamepad.SetButton(mappings.Wii1, !isHybridActive && state.Buttons.One && !HotkeyManager.IsButtonConsumed(PlayerIndex, "One"));
                _virtualGamepad.SetButton(mappings.Wii2, !isHybridActive && state.Buttons.Two && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Two"));
                _virtualGamepad.SetButton(mappings.WiiPlus, !isHybridActive && state.Buttons.Plus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus"));
                _virtualGamepad.SetButton(mappings.WiiMinus, !isHybridActive && minusPressed);
                _virtualGamepad.SetButton(mappings.WiiUp, !isHybridActive && state.Buttons.Up && dpadActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Up"));
                _virtualGamepad.SetButton(mappings.WiiDown, !isHybridActive && state.Buttons.Down && dpadActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Down"));
                _virtualGamepad.SetButton(mappings.WiiLeft, !isHybridActive && state.Buttons.Left && dpadActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Left"));
                _virtualGamepad.SetButton(mappings.WiiRight, !isHybridActive && state.Buttons.Right && dpadActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Right"));
                _virtualGamepad.SetButton(mappings.WiiHome, !isHybridActive && homePressed);

                if (hasNunchuk)
                {
                    _virtualGamepad.SetButton(mappings.NunchukC, !isHybridActive && state.Nunchuk.C && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunC"));
                    _virtualGamepad.SetButton(mappings.NunchukZ, !isHybridActive && state.Nunchuk.Z && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunZ"));

                    // --- Nunchuk Joystick ---
                    if (mappings.NunchukJoystickAxis != GamePadAxis.None)
                    {
                        float nunSXOff = 0, nunSYOff = 0;
                        var stickCalib = Options.Instance.GetCalibration(Wiimote != null ? Wiimote.UniqueId : "");
                        if (stickCalib != null)
                        {
                            nunSXOff = stickCalib.NunStickXOffset;
                            nunSYOff = stickCalib.NunStickYOffset;
                        }

                        // Apply calibration offset to raw center
                        float rawX = state.Nunchuk.Joystick.X - nunSXOff;
                        float rawY = state.Nunchuk.Joystick.Y - nunSYOff;

                        float joyX = rawX * 2.0f;
                        float joyY = rawY * 2.0f;

                        if (float.IsNaN(joyX) || float.IsInfinity(joyX)) joyX = 0f;
                        if (float.IsNaN(joyY) || float.IsInfinity(joyY)) joyY = 0f;

                        if (Math.Abs(joyX) < 0.25f) joyX = 0f;
                        if (Math.Abs(joyY) < 0.25f) joyY = 0f;

                        joyX = Math.Max(-1.0f, Math.Min(1.0f, joyX));
                        joyY = Math.Max(-1.0f, Math.Min(1.0f, joyY));

                        if (mappings.NunchukJoystickAxis == GamePadAxis.Dpad)
                        {
                            bool dUp = joyY > 0.5f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp");
                            bool dDown = joyY < -0.5f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown");
                            bool dRight = joyX > 0.5f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight");
                            bool dLeft = joyX < -0.5f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft");

                            _virtualGamepad.SetButton(GamePadButton.DPadUp, dUp);
                            _virtualGamepad.SetButton(GamePadButton.DPadDown, dDown);
                            _virtualGamepad.SetButton(GamePadButton.DPadLeft, dLeft);
                            _virtualGamepad.SetButton(GamePadButton.DPadRight, dRight);
                        }
                        else
                        {
                            float finalJoyX = joyX;
                            float finalJoyY = joyY;

                            if (joyX > 0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight")) finalJoyX = 0f;
                            if (joyX < -0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft")) finalJoyX = 0f;
                            if (joyY > 0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp")) finalJoyY = 0f;
                            if (joyY < -0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown")) finalJoyY = 0f;

                            _virtualGamepad.SetAxis(mappings.NunchukJoystickAxis, finalJoyX, -finalJoyY);
                        }
                    }
                }

                // --- IR Sensor Axis ---
                bool irFound = scaledPos.HasValue;
                if (irFound)
                {
                    _lastValidIRX = scaledPos.Value.X / 65535.0f;
                    _lastValidIRY = scaledPos.Value.Y / 65535.0f;
                }

                float margin = mappings.IROverscan;
                float scale = 1.0f / (1.0f - 2.0f * margin);
                
                float xOverscan = (_lastValidIRX - margin) * scale;
                float yOverscan = (_lastValidIRY - margin) * scale;

                xOverscan = Math.Max(0f, Math.Min(1f, xOverscan));
                yOverscan = Math.Max(0f, Math.Min(1f, yOverscan));

                float normX = (xOverscan * 2.0f) - 1.0f;
                float normY = (yOverscan * 2.0f) - 1.0f;

                if (mappings.IRLinearity > 0 && Math.Abs(mappings.IRLinearity - 1.0f) > 0.001f)
                {
                    normX = (float)(Math.Sign(normX) * Math.Pow(Math.Abs(normX), mappings.IRLinearity));
                    normY = (float)(Math.Sign(normY) * Math.Pow(Math.Abs(normY), mappings.IRLinearity));
                }

                // (EN/FR: Le stick ne se centre que si le mode hybride est STABLE, évitant les sauts de caméra)
                // (Stick only centers if hybrid mode is STABLE, preventing camera jumps)
                bool shouldCenterStick = isHybridStable && mappings.IRHybridAsMouse;

                if (!isHybridActive || !shouldCenterStick)
                {
                    // (EN/FR: Appliquer compensation zone morte pour XInput afin d'éliminer le point neutre logiciel des jeux)
                    // (Allows instantaneous movement response even with small IR deviations)
                    if (mappings.UseXInput && (Math.Abs(normX) > 0.0001f || Math.Abs(normY) > 0.0001f))
                    {
                        float threshold = mappings.IRAntiDeadzone; // Configurable anti-deadzone
                        normX = Math.Sign(normX) * (threshold + Math.Abs(normX) * (1.0f - threshold));
                        normY = Math.Sign(normY) * (threshold + Math.Abs(normY) * (1.0f - threshold));
                    }

                    _virtualGamepad.SetAxis(mappings.IRSensorAxis, normX, normY);
                }
                else
                {
                    _virtualGamepad.SetAxis(mappings.IRSensorAxis, 0f, 0f); // Center stick when used as mouse
                }

                // --- Motion Support ---
                float accXOff = 0, accYOff = 0, accZOff = 0;
                float nunXOff = 0, nunYOff = 0, nunZOff = 0;
                var calib = Options.Instance.GetCalibration(Wiimote != null ? Wiimote.UniqueId : "");
                if (calib != null)
                {
                    accXOff = calib.AccXOffset;
                    accYOff = calib.AccYOffset;
                    accZOff = calib.AccZOffset;
                    
                    nunXOff = calib.NunAccXOffset;
                    nunYOff = calib.NunAccYOffset;
                    nunZOff = calib.NunAccZOffset;
                }

                Action<GamePadMotionAction, float, float, float, float> applyMotionAction = (motionAction, rawMotX, rawMotY, rawMotZ, sensitivity) =>
                {
                    if (motionAction == null || motionAction.TargetType == GamePadMotionTargetType.None) return;

                    float motX = rawMotX * sensitivity;
                    float motY = rawMotY * sensitivity;

                    if (motionAction.TargetType == GamePadMotionTargetType.Axis)
                    {
                        if (motionAction.TargetAxis == GamePadAxis.RightStick)
                            _virtualGamepad.SetAxis(GamePadAxis.RightStick, motX, motY);
                        else if (motionAction.TargetAxis == GamePadAxis.LeftStick)
                            _virtualGamepad.SetAxis(GamePadAxis.LeftStick, motX, motY);
                        else if (motionAction.TargetAxis == GamePadAxis.Throttle)
                        {
                            float throttleVal = (motY + 1.0f) * 127.5f;
                            _virtualGamepad.Throttle = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(throttleVal)));
                        }
                    }
                    else if (motionAction.TargetType == GamePadMotionTargetType.Button)
                    {
                        // EN: Caller already performed deadzone check for specific direction or shake
                        // FR: L'appelant a déjà effectué le contrôle de zone morte pour la direction ou le shake
                        _virtualGamepad.SetButton(motionAction.TargetButton, true);
                    }
                };

                // --- Gesture Axis Reset (EN/FR: Réinitialisation des axes de gestes) ---
                // Identify all axes targeted by gestures to prevent they stay stuck after movement
                // --- Gesture Reset (EN/FR: Réinitialisation des gestes) ---
                // Reset all gesture targets (axes and buttons) to neutral state at frame start
                // (EN/FR: Réinitialiser toutes les cibles de gestes à l'état neutre en début de frame)
                Action<GamePadMotionAction> resetAction = (ma) => {
                    if (ma == null || ma.TargetType == GamePadMotionTargetType.None) return;
                    if (ma.TargetType == GamePadMotionTargetType.Axis) _virtualGamepad.SetAxis(ma.TargetAxis, 0f, 0f);
                    else if (ma.TargetType == GamePadMotionTargetType.Button) _virtualGamepad.SetButton(ma.TargetButton, false);
                };

                resetAction(mappings.AccelWiimoteUp);
                resetAction(mappings.AccelWiimoteDown);
                resetAction(mappings.AccelWiimoteLeft);
                resetAction(mappings.AccelWiimoteRight);
                resetAction(mappings.AccelWiimoteShake);

                if (hasNunchuk)
                {
                    resetAction(mappings.AccelNunchukUp);
                    resetAction(mappings.AccelNunchukDown);
                    resetAction(mappings.AccelNunchukLeft);
                    resetAction(mappings.AccelNunchukRight);
                    resetAction(mappings.AccelNunchukShake);
                }

                if (state.ExtensionType == ExtensionType.MotionPlus || state.ExtensionType == ExtensionType.MotionPlusNunchuk)
                {
                    resetAction(mappings.GyroMotionPlusUp);
                    resetAction(mappings.GyroMotionPlusDown);
                    resetAction(mappings.GyroMotionPlusLeft);
                    resetAction(mappings.GyroMotionPlusRight);
                    resetAction(mappings.GyroMotionPlusRollLeft);
                    resetAction(mappings.GyroMotionPlusRollRight);
                }

                // Accel Wiimote
                // EN: Apply deadzone on RAW values first, then sensitivity multiplier AFTER (prevents cross-triggering)
                // FR: Appliquer la deadzone sur les valeurs BRUTES d'abord, puis la sensibilité APRÈS (évite les déclenchements croisés)
                float wRawX = state.Accel.Values.X;
                float wRawY = state.Accel.Values.Y;
                float wRawZ = state.Accel.Values.Z;

                if (wRawX == 0 && wRawY == 0 && wRawZ == 0 && _debugCounter % 500 == 0)
                    SimpleLogger.Instance.Warning($"[P{PlayerIndex}] Accelerometer data is ZEROS. Check report type or connectivity.");

                // EN: Subtract calibration offset only (no amplification before deadzone)
                // FR: Soustraire uniquement l'offset de calibration (pas d'amplification avant la deadzone)
                float wMotX = (wRawX - accXOff);
                float wMotY = (wRawY - accYOff);
                float wMotZ = (wRawZ - accZOff);

                // EN: Normalize to Gs if values are raw units (~28 per G)
                // FR: Normaliser en G si les valeurs sont brutes (~28 par G)
                float wMag = (float)Math.Sqrt(wMotX * wMotX + wMotY * wMotY + wMotZ * wMotZ);
                if (wMag > 10) { wMotX /= 28.0f; wMotY /= 28.0f; wMotZ /= 28.0f; }

                float wDeadzone = mappings.AccelWiimoteDeadzone;

                float wAbsX = Math.Abs(wMotX);
                float wAbsY = Math.Abs(wMotY);
                float wActDZ = wDeadzone * 1.1f; // 110% to ACTIVATE (EN/FR: 110% pour ACTIVER)
                float wRelDZ = wDeadzone * 0.9f; // 90% to RELEASE (EN/FR: 90% pour RELÂCHER)

                bool wUp = false, wDown = false, wLeft = false, wRight = false;

                // EN: Axis Exclusivity: Only process the axis with the strongest magnitude
                // FR: Exclusivité d'axe : Ne traiter que l'axe avec la plus forte magnitude
                if (wAbsY >= wAbsX)
                {
                    wUp = _lastAccelWiimoteUp ? wMotY > wRelDZ : wMotY > wActDZ;
                    wDown = _lastAccelWiimoteDown ? wMotY < -wRelDZ : wMotY < -wActDZ;
                }
                else
                {
                    wLeft = _lastAccelWiimoteLeft ? wMotX < -wRelDZ : wMotX < -wActDZ;
                    wRight = _lastAccelWiimoteRight ? wMotX > wRelDZ : wMotX > wActDZ;
                }

                if (wUp) { applyMotionAction(mappings.AccelWiimoteUp, wMotX * 3f, wMotY * 3f, wMotZ * 3f, mappings.AccelWiimoteSensitivity); execHybrid(mappings.AccelWiimoteUpHybrid, true, _lastAccelWiimoteUp, "AccelWiimoteUp"); }
                else { execHybrid(mappings.AccelWiimoteUpHybrid, false, _lastAccelWiimoteUp, "AccelWiimoteUp"); }

                if (wDown) { applyMotionAction(mappings.AccelWiimoteDown, wMotX * 3f, wMotY * 3f, wMotZ * 3f, mappings.AccelWiimoteSensitivity); execHybrid(mappings.AccelWiimoteDownHybrid, true, _lastAccelWiimoteDown, "AccelWiimoteDown"); }
                else { execHybrid(mappings.AccelWiimoteDownHybrid, false, _lastAccelWiimoteDown, "AccelWiimoteDown"); }

                if (wLeft) { applyMotionAction(mappings.AccelWiimoteLeft, wMotX * 3f, wMotY * 3f, wMotZ * 3f, mappings.AccelWiimoteSensitivity); execHybrid(mappings.AccelWiimoteLeftHybrid, true, _lastAccelWiimoteLeft, "AccelWiimoteLeft"); }
                else { execHybrid(mappings.AccelWiimoteLeftHybrid, false, _lastAccelWiimoteLeft, "AccelWiimoteLeft"); }

                if (wRight) { applyMotionAction(mappings.AccelWiimoteRight, wMotX * 3f, wMotY * 3f, wMotZ * 3f, mappings.AccelWiimoteSensitivity); execHybrid(mappings.AccelWiimoteRightHybrid, true, _lastAccelWiimoteRight, "AccelWiimoteRight"); }
                else { execHybrid(mappings.AccelWiimoteRightHybrid, false, _lastAccelWiimoteRight, "AccelWiimoteRight"); }

                // --- PEAK-TO-PEAK SHAKE DETECTION (Wiimote) ---
                // EN: A true shake requires the acceleration to exceed the threshold
                //     in one direction, then exceed it in the OPPOSITE direction.
                //     A simple directional movement (left→rest) never triggers shake
                //     because the return to rest doesn't exceed the threshold.
                // FR: Un vrai shake nécessite que l'accélération dépasse le seuil
                //     dans une direction, puis le dépasse dans la direction OPPOSÉE.
                //     Un simple mouvement directionnel ne déclenche jamais le shake.
                float wShakeThreshold = mappings.AccelWiimoteShakeDeadzone;
                
                // EN: Find the dominant axis magnitude (use the strongest axis)
                // FR: Trouver la magnitude de l'axe dominant (utiliser l'axe le plus fort)
                wAbsX = Math.Abs(wMotX);
                wAbsY = Math.Abs(wMotY);
                float wAbsZ = Math.Abs(wMotZ);
                float wMaxAbs = Math.Max(wAbsX, Math.Max(wAbsY, wAbsZ));
                
                // EN: Determine the sign of the dominant axis
                // FR: Déterminer le signe de l'axe dominant
                int wCurrentDir = 0;
                if (wMaxAbs > wShakeThreshold)
                {
                    if (wMaxAbs == wAbsX) wCurrentDir = wMotX > 0 ? 1 : -1;
                    else if (wMaxAbs == wAbsY) wCurrentDir = wMotY > 0 ? 1 : -1;
                    else wCurrentDir = wMotZ > 0 ? 1 : -1;
                }
                
                // EN: Count oscillation only when peak direction REVERSES (positive↔negative)
                // FR: Compter oscillation seulement quand la direction pic S'INVERSE
                if (wCurrentDir != 0 && _wShakePeakDir != 0 && wCurrentDir != _wShakePeakDir)
                {
                    _wShakeOscillationCount++;
                    _lastWShakeOscillationTime = GetNow();
                }
                if (wCurrentDir != 0) _wShakePeakDir = wCurrentDir;

                // EN: Reset if at rest (below threshold) for too long, or if pause between oscillations > 300ms
                // FR: Réinitialiser si au repos (sous le seuil) trop longtemps, ou si pause entre oscillations > 300ms
                if (_wShakeOscillationCount > 0)
                {
                    double wElapsed = (GetNow() - _lastWShakeOscillationTime).TotalMilliseconds;
                    if ((wCurrentDir == 0 && wElapsed > 300) || wElapsed > 500)
                    {
                        _wShakeOscillationCount = 0;
                        _wShakePeakDir = 0;
                    }
                }

                int wShakeRequired = Math.Max(2, mappings.ShakeOscillationRequired);
                if (_wShakeOscillationCount >= wShakeRequired)
                {
                    _wShakeActiveFrames = 10; // EN/FR: Maintenir pendant ~100ms
                    _wShakeOscillationCount = 0;
                    _wShakePeakDir = 0;
                }

                bool wShake = _wShakeActiveFrames > 0;
                if (_wShakeActiveFrames > 0) _wShakeActiveFrames--;

                if (wShake) { applyMotionAction(mappings.AccelWiimoteShake, wMotX * 3f, wMotY * 3f, wMotZ * 3f, mappings.AccelWiimoteSensitivity); execHybrid(mappings.AccelWiimoteShakeHybrid, true, _lastAccelWiimoteShake, "AccelWiimoteShake"); }
                else { execHybrid(mappings.AccelWiimoteShakeHybrid, false, _lastAccelWiimoteShake, "AccelWiimoteShake"); }

                _lastWMotX = wMotX;
                _lastWMotY = wMotY;
                _lastWMotZ = wMotZ;
                _lastAccelWiimoteUp = wUp;
                _lastAccelWiimoteDown = wDown;
                _lastAccelWiimoteLeft = wLeft;
                _lastAccelWiimoteRight = wRight;
                _lastAccelWiimoteShake = wShake;

                // Accel Nunchuk
                if (state.ExtensionType == ExtensionType.Nunchuk || state.ExtensionType == ExtensionType.MotionPlusNunchuk)
                {
                    float nRawX = state.Nunchuk.Accel.Values.X;
                    float nRawY = state.Nunchuk.Accel.Values.Y;
                    float nRawZ = state.Nunchuk.Accel.Values.Z;

                    if (nRawX == 0 && nRawY == 0 && nRawZ == 0 && _debugCounter % 500 == 0)
                        SimpleLogger.Instance.Warning($"[P{PlayerIndex}] Nunchuk Accel data is ZEROS.");

                    // EN: Subtract calibration offset only (no amplification before deadzone)
                    // FR: Soustraire uniquement l'offset de calibration (pas d'amplification avant la deadzone)
                    float nMotX = (nRawX - nunXOff);
                    float nMotY = (nRawY - nunYOff);
                    float nMotZ = (nRawZ - nunZOff);

                    // EN: Normalize to Gs if values are raw units (~28 per G)
                    // FR: Normaliser en G si les valeurs sont brutes (~28 par G)
                    float nMag = (float)Math.Sqrt(nMotX * nMotX + nMotY * nMotY + nMotZ * nMotZ);
                    if (nMag > 10) { nMotX /= 28.0f; nMotY /= 28.0f; nMotZ /= 28.0f; }

                    float nDeadzone = mappings.AccelNunchukDeadzone;

                    float nAbsX = Math.Abs(nMotX);
                    float nAbsY = Math.Abs(nMotY);
                    float nActDZ = nDeadzone * 1.1f;
                    float nRelDZ = nDeadzone * 0.9f;

                    bool nUp = _lastAccelNunchukUp ? nMotY > nRelDZ : nMotY > nActDZ;
                    bool nDown = _lastAccelNunchukDown ? nMotY < -nRelDZ : nMotY < -nActDZ;
                    bool nLeft = _lastAccelNunchukLeft ? nMotX < -nRelDZ : nMotX < -nActDZ;
                    bool nRight = _lastAccelNunchukRight ? nMotX > nRelDZ : nMotX > nActDZ;

                    // EN: Ensure mutual exclusivity on the same axis (can't be Up and Down)
                    if (nUp && nDown) { nUp = false; nDown = false; }
                    if (nLeft && nRight) { nLeft = false; nRight = false; }

                    if (nUp) { applyMotionAction(mappings.AccelNunchukUp, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukUpHybrid, true, _lastAccelNunchukUp, "AccelNunchukUp"); }
                    else { execHybrid(mappings.AccelNunchukUpHybrid, false, _lastAccelNunchukUp, "AccelNunchukUp"); }

                    if (nDown) { applyMotionAction(mappings.AccelNunchukDown, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukDownHybrid, true, _lastAccelNunchukDown, "AccelNunchukDown"); }
                    else { execHybrid(mappings.AccelNunchukDownHybrid, false, _lastAccelNunchukDown, "AccelNunchukDown"); }

                    if (nLeft) { applyMotionAction(mappings.AccelNunchukLeft, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukLeftHybrid, true, _lastAccelNunchukLeft, "AccelNunchukLeft"); }
                    else { execHybrid(mappings.AccelNunchukLeftHybrid, false, _lastAccelNunchukLeft, "AccelNunchukLeft"); }

                    if (nRight) { applyMotionAction(mappings.AccelNunchukRight, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukRightHybrid, true, _lastAccelNunchukRight, "AccelNunchukRight"); }
                    else { execHybrid(mappings.AccelNunchukRightHybrid, false, _lastAccelNunchukRight, "AccelNunchukRight"); }

                    // --- PEAK-TO-PEAK SHAKE DETECTION (Nunchuk) ---
                    float nShakeThreshold = mappings.AccelNunchukShakeDeadzone;
                    nAbsX = Math.Abs(nMotX);
                    nAbsY = Math.Abs(nMotY);
                    float nAbsZ = Math.Abs(nMotZ);
                    float nMaxAbs = Math.Max(nAbsX, Math.Max(nAbsY, nAbsZ));
                    
                    int nCurrentDir = 0;
                    if (nMaxAbs > nShakeThreshold)
                    {
                        if (nMaxAbs == nAbsX) nCurrentDir = nMotX > 0 ? 1 : -1;
                        else if (nMaxAbs == nAbsY) nCurrentDir = nMotY > 0 ? 1 : -1;
                        else nCurrentDir = nMotZ > 0 ? 1 : -1;
                    }
                    
                    if (nCurrentDir != 0 && _nShakePeakDir != 0 && nCurrentDir != _nShakePeakDir)
                    {
                        _nShakeOscillationCount++;
                        _lastNShakeOscillationTime = GetNow();
                    }
                    if (nCurrentDir != 0) _nShakePeakDir = nCurrentDir;

                    // EN: Reset if at rest or pause too long (only when count > 0)
                    // FR: Réinitialiser si repos ou pause trop longue (seulement si count > 0)
                    if (_nShakeOscillationCount > 0)
                    {
                        double nElapsed = (GetNow() - _lastNShakeOscillationTime).TotalMilliseconds;
                        if ((nCurrentDir == 0 && nElapsed > 300) || nElapsed > 500)
                        {
                            _nShakeOscillationCount = 0;
                            _nShakePeakDir = 0;
                        }
                    }

                    int nShakeRequired = Math.Max(2, mappings.ShakeOscillationRequired);
                    if (_nShakeOscillationCount >= nShakeRequired)
                    {
                        _nShakeActiveFrames = 10;
                        _nShakeOscillationCount = 0;
                        _nShakePeakDir = 0;
                    }

                    bool nShake = _nShakeActiveFrames > 0;
                    if (_nShakeActiveFrames > 0) _nShakeActiveFrames--;

                    if (nShake) { applyMotionAction(mappings.AccelNunchukShake, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukShakeHybrid, true, _lastAccelNunchukShake, "AccelNunchukShake"); }
                    else { execHybrid(mappings.AccelNunchukShakeHybrid, false, _lastAccelNunchukShake, "AccelNunchukShake"); }

                    _lastNMotX = nMotX;
                    _lastNMotY = nMotY;
                    _lastNMotZ = nMotZ;
                    _lastAccelNunchukUp = nUp;
                    _lastAccelNunchukDown = nDown;
                    _lastAccelNunchukLeft = nLeft;
                    _lastAccelNunchukRight = nRight;
                    _lastAccelNunchukShake = nShake;
                }

                // Gyro Motion Plus
                if (state.ExtensionType == ExtensionType.MotionPlus || state.ExtensionType == ExtensionType.MotionPlusNunchuk)
                {
                    // EN: Apply EMA smoothing to reduce jitter (FR: Appliquer lissage EMA pour réduire le jitter)
                    float rawYaw = (state.MotionPlus.Values.Yaw) / 500.0f;
                    float rawPitch = (state.MotionPlus.Values.Pitch) / 500.0f;
                    float rawRoll = (state.MotionPlus.Values.Roll) / 500.0f;

                    _smoothGyroYaw = (GYRO_SMOOTH_ALPHA * rawYaw) + ((1.0f - GYRO_SMOOTH_ALPHA) * _smoothGyroYaw);
                    _smoothGyroPitch = (GYRO_SMOOTH_ALPHA * rawPitch) + ((1.0f - GYRO_SMOOTH_ALPHA) * _smoothGyroPitch);
                    _smoothGyroRoll = (GYRO_SMOOTH_ALPHA * rawRoll) + ((1.0f - GYRO_SMOOTH_ALPHA) * _smoothGyroRoll);

                    float gMotX = _smoothGyroYaw;
                    float gMotY = _smoothGyroPitch;
                    float gMotZ = _smoothGyroRoll;

                    float gDeadzone = mappings.GyroDeadzone;

                    if (gMotY < -gDeadzone) { applyMotionAction(mappings.GyroMotionPlusUp, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusUpHybrid, true, _lastGyroMotionPlusUp, "GyroMotionPlusUp"); }
                    else { execHybrid(mappings.GyroMotionPlusUpHybrid, false, _lastGyroMotionPlusUp, "GyroMotionPlusUp"); }

                    if (gMotY > gDeadzone) { applyMotionAction(mappings.GyroMotionPlusDown, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusDownHybrid, true, _lastGyroMotionPlusDown, "GyroMotionPlusDown"); }
                    else { execHybrid(mappings.GyroMotionPlusDownHybrid, false, _lastGyroMotionPlusDown, "GyroMotionPlusDown"); }

                    if (gMotX < -gDeadzone) { applyMotionAction(mappings.GyroMotionPlusLeft, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusLeftHybrid, true, _lastGyroMotionPlusLeft, "GyroMotionPlusLeft"); }
                    else { execHybrid(mappings.GyroMotionPlusLeftHybrid, false, _lastGyroMotionPlusLeft, "GyroMotionPlusLeft"); }

                    if (gMotX > gDeadzone) { applyMotionAction(mappings.GyroMotionPlusRight, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusRightHybrid, true, _lastGyroMotionPlusRight, "GyroMotionPlusRight"); }
                    else { execHybrid(mappings.GyroMotionPlusRightHybrid, false, _lastGyroMotionPlusRight, "GyroMotionPlusRight"); }

                    float rollCooldownMs = 150f;
                    // EN: Fix roll direction interpretation (Swapped < and >)
                    // FR: Correction de l'interprétation du sens de l'inclinaison (Inversion de < et >)
                    bool isRollLeft = gMotZ > gDeadzone;
                    bool isRollRight = gMotZ < -gDeadzone;

                    // Anti-wobble logic (EN/FR: Empêche le rebond physique du roll dans le sens inverse)
                    if (isRollLeft)
                    {
                        if ((GetNow() - _lastRollRightTime).TotalMilliseconds < rollCooldownMs)
                            isRollLeft = false;
                        else
                            _lastRollLeftTime = GetNow();
                    }
                    if (isRollRight)
                    {
                        if ((GetNow() - _lastRollLeftTime).TotalMilliseconds < rollCooldownMs)
                            isRollRight = false;
                        else
                            _lastRollRightTime = GetNow();
                    }

                    if (isRollLeft) { applyMotionAction(mappings.GyroMotionPlusRollLeft, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusRollLeftHybrid, true, _lastGyroMotionPlusRollLeft, "GyroMotionPlusRollLeft"); }
                    else { execHybrid(mappings.GyroMotionPlusRollLeftHybrid, false, _lastGyroMotionPlusRollLeft, "GyroMotionPlusRollLeft"); }

                    if (isRollRight) { applyMotionAction(mappings.GyroMotionPlusRollRight, gMotX, gMotY, gMotZ, mappings.GyroSensitivity); execHybrid(mappings.GyroMotionPlusRollRightHybrid, true, _lastGyroMotionPlusRollRight, "GyroMotionPlusRollRight"); }
                    else { execHybrid(mappings.GyroMotionPlusRollRightHybrid, false, _lastGyroMotionPlusRollRight, "GyroMotionPlusRollRight"); }

                    _lastGyroMotionPlusUp = gMotY < -gDeadzone;
                    _lastGyroMotionPlusDown = gMotY > gDeadzone;
                    _lastGyroMotionPlusLeft = gMotX < -gDeadzone;
                    _lastGyroMotionPlusRight = gMotX > gDeadzone;
                    _lastGyroMotionPlusRollLeft = isRollLeft;
                    _lastGyroMotionPlusRollRight = isRollRight;
                }

                if (_virtualMouse != null)
                {
                    bool wantsMouseMovement = isHybridActive && mappings.IRHybridAsMouse;
                    bool hasMouseActivity = hLeft || hRight || hMiddle || _lastHybridLeft || _lastHybridRight || _lastHybridMiddle || wantsMouseMovement || _lastRuntimeWantsMouse;

                    if (hasMouseActivity)
                    {
                        if (wantsMouseMovement && irFound)
                        {
                            _virtualMouse.UpdateMouse((int)scaledPos.Value.X, (int)scaledPos.Value.Y, hLeft, hRight, hMiddle, true, true);
                        }
                        else
                        {
                            _virtualMouse.UpdateMouse(0, 0, hLeft, hRight, hMiddle, false, false);
                        }

                        _lastHybridLeft = hLeft;
                        _lastHybridRight = hRight;
                        _lastHybridMiddle = hMiddle;
                        _lastRuntimeWantsMouse = wantsMouseMovement;
                    }
                }

                _virtualGamepad.SendReport();
                
                _debugCounter++;
            }
            catch (Exception ex)
            {
                if (_debugCounter % 300 == 0)
                {
                     SimpleLogger.Instance.Error(string.Format("[GamePad Update Error] P{0}: {1}", PlayerIndex, ex.Message));
                }
            }
        }

        private void ApplyAxis(ref VMultiGamepadReport report, GamePadAxis axis, float x, float y)
        {
            if (axis == GamePadAxis.None) return;
            
            // Invert Y for IR stick mapping (Up/Top of screen should be -1.0 for gamepad stick Y)
            // Nunchuk Y also inverted in UpdateGamePadState. 
            // In standard HID: Y - is Up.
            report.SetAxis(axis, x, y);
        }


        /// <summary>
        /// Applies 4:3 aspect ratio stretching if the current mode is a 4:3 mode and the screen is wide.
        /// (EN/FR: Applique l'étirement du format 4:3 si le mode actuel est en 4:3 et que l'écran est large.)
        /// </summary>
        private Point2F ApplyAspectRatioCorrection(Point2F pos, WiiMoteMode mode)
        {
            if (mode != WiiMoteMode.Mouse43 && mode != WiiMoteMode.GamePad43 && 
                mode != WiiMoteMode.MouseFPS && mode != WiiMoteMode.GamePadFPS)
                return pos;

            var screen = System.Windows.Forms.Screen.AllScreens[ScreenIndex];
            double screenRatio = (double)screen.Bounds.Width / screen.Bounds.Height;
            
            // For FPS modes, we use aggressive 1:1 stretching to better align with centered crosshairs
            // For 4:3 modes, we use standard 4:3 correction.
            bool isFPS = (mode == WiiMoteMode.MouseFPS || mode == WiiMoteMode.GamePadFPS);
            double targetRatio = isFPS ? 1.0 : 4.0 / 3.0;

            // Only apply if screen is significantly wider than 4:3 (e.g. 16:9, 21:9, etc.)
            if (screenRatio > targetRatio + 0.01)
            {
                // factor = 1.77 / 1.33 = 1.333
                double factor = screenRatio / targetRatio;
                // offset = (1 - 1/factor) / 2
                // For Widescreen on 4:3, offset is calculated based on ratio
                double offset = (1.0 - (1.0 / factor)) / 2.0;

                // input pos.X is 0..65535 (scaled from 0..1)
                float normX = pos.X / 65535.0f;
                
                // transform: normX' = (normX - offset) / (1 - 2*offset)
                float normXCorrected = (float)((normX - offset) / (1.0 - 2.0 * offset));

                // Clamp to valid range (0..1)
                normXCorrected = Math.Max(0f, Math.Min(1f, normXCorrected));
                
                return new Point2F(normXCorrected * 65535.0f, pos.Y);
            }

            return pos;
        }

        /// <summary>
        /// EN: Refresh the predicted DirectInput index for the virtual gamepad.
        /// FR: Rafraîchir l'index DirectInput prédit pour le gamepad virtuel.
        /// </summary>
        /// <param name="silent">If true, only log if the index actually changes. (EN/FR: Si vrai, logger uniquement si l'index change)</param>
        public void RefreshDInputIndex(bool silent = false)
        {
            if (_mode != WiiMoteMode.GamePad && _mode != WiiMoteMode.GamePad43 && _mode != WiiMoteMode.GamePadFPS) return;

            int dinputIndex = DirectInputHelper.FindVMultiGamepadIndex(PlayerIndex);
            
            if (dinputIndex != _lastDInputIndex)
            {
                if (dinputIndex > 0)
                {
                    SimpleLogger.Instance.Info(string.Format("[P{0}] Virtual GamePad DirectInput Index changed: Joy{1} (was Joy{2})", 
                        PlayerIndex, dinputIndex, _lastDInputIndex > 0 ? _lastDInputIndex.ToString() : "None"));
                }
                else
                {
                    SimpleLogger.Instance.Warning(string.Format("[P{0}] Virtual GamePad DirectInput Index lost (was Joy{1})", 
                        PlayerIndex, _lastDInputIndex));
                }
                _lastDInputIndex = dinputIndex;
            }
            else if (!silent)
            {
                if (dinputIndex > 0)
                {
                    SimpleLogger.Instance.Info(string.Format("[P{0}] Virtual GamePad detected at DirectInput Index: Joy{1}", PlayerIndex, dinputIndex));
                }
                else
                {
                    SimpleLogger.Instance.Warning(string.Format("[P{0}] Could not identify DirectInput index for Virtual GamePad.", PlayerIndex));
                }
            }
        }

        /// <summary>
        /// EN: Virtual Polling (Hypersampling) callback.
        /// FR: Callback de Polling Virtuel (Hypersampling).
        /// Sends predicted positions between hardware reports to increase perceived polling rate.
        /// </summary>
        private void OnVirtualPollingTick()
        {
            if (!Options.Instance.EnableVirtualPolling || _mode == WiiMoteMode.Disabled) return;
            if (!_lastMoveCursor_Raw) return;

            // Short-circuit: Do not send virtual reports if the target rate is close to native Wiimote (100Hz)
            // (EN/FR: Ne pas envoyer de rapports virtuels si le taux est proche du natif Wiimote)
            if (Options.Instance.VirtualPollingRate <= 110) return;

            // Calculate time since last real report (for prediction vector) 
            // and time since any report (for rate limiting synchronization)
            // (EN/FR: Calculer temps depuis dernier rapport réel et depuis n'importe quel rapport)
            DateTime now = GetNow();
            double msSinceLastReal = (now - _lastProcessingTime).TotalMilliseconds;
            double msSinceLastAny = (now - _lastAnyReportTime).TotalMilliseconds;

            // Target interval for the configured polling rate (e.g. 4.0ms for 250Hz)
            // (EN/FR: Intervalle cible pour le taux configuré)
            double targetIntervalMs = 1000.0 / Options.Instance.VirtualPollingRate;

            // Only predict if within a reasonable window (EN/FR: Prédire uniquement dans une fenêtre raisonnable)
            // AND if enough time has passed to maintain the target rate (synchronization)
            // We use a 0.85 factor to allow for slight jitter while being closer to target than additive
            if (msSinceLastReal > 1.0 && msSinceLastReal < 20.0 && msSinceLastAny >= (targetIntervalMs * 0.85))
            {
                // Predict position using last known velocity
                // Multiplier (msSinceLastReal / 10.0) approximates frames (10ms per frame)
                float frameFactor = (float)(msSinceLastReal / 10.0);
                
                int predX = (int)(_lastX_Raw + _lastVelX_Diag * frameFactor);
                int predY = (int)(_lastY_Raw + _lastVelY_Diag * frameFactor);

                predX = Math.Max(0, Math.Min(65535, predX));
                predY = Math.Max(0, Math.Min(65535, predY));

                // Send extrapolated update
                _virtualMouse.UpdateMouse(predX, predY, _lastLeft_Raw, _lastRight_Raw, _lastMiddle_Raw, true);
                _lastAnyReportTime = now;
            }
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

    public enum WiiMoteMode
    {
        Mouse = 0,
        Mouse43 = 1,
        MouseFPS = 2,
        GamePad = 3,
        GamePad43 = 4,
        GamePadFPS = 5,
        Keyboardpad = 6,
        Disabled = 7
    }
}
