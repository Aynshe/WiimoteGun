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

        // [V55y] Reload rumble sequencer (EN/FR: Séquenceur vibration rechargement)
        private System.Threading.Timer _reloadRumbleTimer;
        private volatile bool _isReloadRumbling;
        private int[] _reloadRumblePattern;   // Alternating ON/OFF durations in ms (EN/FR: Durées ON/OFF alternées en ms)
        private int _reloadRumbleStepIdx;
        private bool _lastRawReloadButton;   // Physical reload button edge tracking (EN/FR: Suivi d'arête bouton reload physique)
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
        private bool _wasOnScreen = false;
        private bool _hasAimedAtScreenOnce = false; // Must aim at screen first before reload can trigger (EN/FR: Doit viser l'écran avant autorisation reload)
        private bool _offScreenReloadPerformed = false; // Only 1 AUTO reload allowed per off-screen session (EN/FR: 1 seul reload AUTO par session hors écran)
        private int _offScreenReloadClickSequence = 0; // 0=idle, 1-3=holding right click, -1=done
        private bool _osTriggerReloadWasActive = false; // [V55u] Previous frame state of the off-screen trigger→reload redirect (edge detection for log/rumble)
        private DateTime _lastAutoReloadTime = DateTime.MinValue; // Cooldown for auto-reload (EN/FR: Cooldown pour rechargement auto)
        private int _onScreenConsecutiveFrames = 0; // Stability counter for on-screen (EN/FR: Compteur de stabilité sur l'écran)
        private int _offScreenConsecutiveFrames = 0; // Stability counter for off-screen (EN/FR: Compteur de stabilité hors écran)
        
        
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

        /// <summary>
        /// [V57g] EN: True when the live virtual gamepad is an XInput output (ViGEmBus in
        ///     RawInput/VMulti mode, HIDMaestro XUSB in RawInputUmdf mode).
        ///     FR: Vrai quand la manette virtuelle vivante est une sortie XInput (ViGEmBus
        ///     en mode RawInput/VMulti, XUSB HIDMaestro en mode RawInputUmdf).
        /// </summary>
        public bool UsesXInputOutput
        {
            get
            {
                return (_virtualGamepad is ViGEmGamepad) ||
                       (_virtualGamepad is HmGamepad hmGp && hmGp.UsesXInput);
            }
        }

        /// <summary>
        /// [V57g] EN: The XInput slot (0..3) of the UMDF2 XUSB gamepad, -1 when unknown or
        ///     not applicable (DInput players / VMulti ViGEm players). XInput slots follow
        ///     the companion CREATION order, not the player number, so the emulator
        ///     profile automation reads this per player.
        ///     FR: Le slot XInput (0..3) du gamepad XUSB UMDF2, -1 si inconnu ou non
        ///     applicable (joueurs DInput / joueurs ViGEm du mode VMulti). Les slots XInput
        ///     suivent l'ordre de CRÉATION des companions, pas le numéro de joueur :
        ///     l'automatisation des profils émulateurs le lit par joueur.
        /// </summary>
        public int XInputSlot
        {
            get { return (_virtualGamepad is HmGamepad hmGp) ? hmGp.LastXInputSlot : -1; }
        }

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
        private bool _suppressLeftClickUntilRelease = false;

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
            // EN: Inhibit shake reload if MotionPlus/accelerometer is disabled via settings
            // FR: Inhiber le rechargement par secousse si MP/accéléromètre est désactivé via les paramètres
            if (Options.Instance.DisableMotionPlusAndAccelerometer) return false;

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

        private int _diagReportCount = 0;
        private DateTime _diagWindowStart = DateTime.MinValue;
        private DateTime _diagLastReportTime = DateTime.MinValue;
        private double _diagDeltaSum = 0;
        private double _diagDeltaMax = 0;
        private int _diagDeltaCount = 0;

        private double _avgReportIntervalMs = 10.0;
        private float _smoothPredVelX = 0f;
        private float _smoothPredVelY = 0f;

        // [DIAG] Timing instrumentation for V1 vs MotionPlus Inside V2 jitter.
        private long _diagLastStateTicks = 0;

        // [FIX V25] Burst de-jitter replay buffer.
        // The Windows Bluetooth stack aggregates the RVL-CNT-01-TR (V2) HID stream into
        // batches of 2-4 reports delivered every ~30-40ms (measured in TimingDiagnostics CSV:
        // gaps of 27.5/2.4ms vs 4-16ms on V1). We buffer real IR positions and replay them on
        // a uniform time grid shifted by an adaptive delay, so the cursor moves smoothly
        // even though the transport delivers in bursts.
        private readonly object _replayLock = new object();
        private readonly System.Collections.Generic.List<double> _replayTimeMs = new System.Collections.Generic.List<double>();
        private readonly System.Collections.Generic.List<int> _replayX = new System.Collections.Generic.List<int>();
        private readonly System.Collections.Generic.List<int> _replayY = new System.Collections.Generic.List<int>();
        private long _replayLastSampleTicks = 0;
        private double _lastBurstBoundaryMs = -1.0;
        private double _burstPeriodEmaMs = 0.0;
        private double _replayDelayMs = 24.0;

        // [DIAG] Output-side (Virtual Polling) diagnostics: measures what the cursor
        // actually receives (replay vs fallback), not just the raw input rate.
        private long _diagOutEmissions = 0;
        private long _diagOutReplay = 0;
        private long _diagOutFallback = 0;
        private double _diagOutWindowStartMs = -1.0;

        // [FIX V26] Cooldown for MotionPlus re-activation. Some V1 external MotionPlus
        // adapters report extension=0 in every status report after standalone activation,
        // which raised endless "MotionPlus Removed" events and caused an infinite
        // activate/status/activate loop (~20 writes + GetStatus every 600ms, observed in
        // WiimoteTimingDiagnostics CSV as periodic report-backlog dumps).
        private DateTime _lastMpActivationCompleted = DateTime.MinValue;

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

        // [V28] Per-Wiimote-model option resolution: V1 (RVL-CNT-01) vs V2 TR (RVL-CNT-01-TR).
        // The left value in Options applies to V1; the "V2" fields apply to Wiimote Plus only.
        // (EN/FR: Résolution des options par modèle : la valeur de gauche s'applique à la V1,
        // les champs "V2" uniquement à la Wiimote Plus.)
        private bool IsWiimoteV2TR
        {
            get { return Wiimote != null && (Wiimote.Type & WiimoteType.WiimotePlus) == WiimoteType.WiimotePlus; }
        }

        private int ActiveVirtualPollingRate
        {
            get { return IsWiimoteV2TR ? Options.Instance.VirtualPollingRateV2 : Options.Instance.VirtualPollingRate; }
        }

        private int ActiveIRSmoothingStrength
        {
            get { return IsWiimoteV2TR ? Options.Instance.IRSmoothingStrengthV2 : Options.Instance.IRSmoothingStrength; }
        }

        private float ActiveIRExtrapolationStrength
        {
            get { return IsWiimoteV2TR ? Options.Instance.IRExtrapolationStrengthV2 : Options.Instance.IRExtrapolationStrength; }
        }
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
            if (Options.Instance.EnableVirtualPolling && ActiveVirtualPollingRate > 0)
            {
                int interval = 1000 / ActiveVirtualPollingRate;
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

            // [V55y] Reload rumble sequencer timer (EN/FR: Timer du séquenceur de vibration rechargement)
            _reloadRumbleTimer = new System.Threading.Timer(ReloadRumbleStepCallback, null, Timeout.Infinite, Timeout.Infinite);

            // Initialize time-based fields (EN/FR: Initialiser les champs basés sur le temps)
            _lastActivityTime = GetNow();
            _lastReportTime = GetNow();
            _lastIRSeenTime = GetNow();
            _lastGyroStillTime = GetNow();
            _controllerStartTime = GetNow();

            // [V55m] If game session is active and lock option enabled, inherit locked state
            // (EN/FR: Si une session de jeu est active et l'option cochée, hériter de l'état verrouillé)
            _modeLocked = Options.Instance.LockModeOnGameStart && EsScriptIntegration.HasCurrentGame;
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
                            // (EN/FR: Réactiver MotionPlus car il a pu être réinitialisé)
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
                    // [V57e] EN: UMDF2/HIDMaestro mode - enable the player's device through
                    //     the service. The service asks HmHost to CREATE the player's device
                    //     with its stable identity (P<n>): devices exist ONLY for connected
                    //     wiimotes, exactly like the vmulti enable/disable flow, so Emulation
                    //     Station never sees "phantom" lightguns for empty players.
                    //     FR: Mode UMDF2/HIDMaestro - activer le périphérique du joueur via
                    //     le service. Le service demande à HmHost de CRÉER le device du
                    //     joueur avec son identité stable (P<n>) : les devices n'existent QUE
                    //     pour les wiimotes connectées, exactement comme le flux
                    //     enable/disable vmulti - EmulationStation ne voit jamais de
                    //     lightguns « fantômes » pour les joueurs inoccupés.
                    if (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf)
                    {
                        // [V57h] EN: Guard against the reboot race reported by the user: the
                        //     app fires HM_ACTIVATE in the background at startup, but a
                        //     wiimote can auto-reconnect and send ENABLE_P BEFORE the service
                        //     flagged the UMDF2 host active - the command then fell to the
                        //     legacy vmulti devcon path (activate vmulti drivers). The guard
                        //     makes the connect flow self-healing: ensure the host is active
                        //     (HmActivate is idempotent) BEFORE requesting the device.
                        //     FR: Garde-fou contre la course de reboot rapportée par
                        //     l'utilisateur : l'app déclenche HM_ACTIVATE en arrière-plan au
                        //     démarrage, mais une wiimote peut se reconnecter automatiquement
                        //     et envoyer ENABLE_P AVANT que le service n'ait flaggé l'hôte
                        //     UMDF2 actif - la commande tombait alors sur le chemin devcon
                        //     vmulti legacy (activation des pilotes vmulti). Le garde-fou rend
                        //     le flux de connexion auto-réparant : s'assurer que l'hôte est
                        //     actif (HmActivate est idempotent) AVANT de demander le device.
                        if (!ServiceClient.HmIsActive())
                        {
                            SimpleLogger.Instance.Warning("[V57h] UMDF2: host not active at wiimote connect - re-activating before ENABLE_P (HmActivate is idempotent)...");
                            string act = ServiceClient.HmActivate();
                            SimpleLogger.Instance.Info("[V57h] UMDF2: re-activation -> " + (act ?? "no reply"));
                        }
                        ServiceClient.EnablePlayer(PlayerIndex);
                        SimpleLogger.Instance.Info($"[V57e] UMDF2: P{PlayerIndex} device activation requested via the service (HmHost identity P{PlayerIndex}, stable paths).");
                    }

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
                    else if (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf)
                    {
                        // [V57d] RawInput UMDF2/HIDMaestro mode: all players get an independent
                        // keyboard and mouse, hosted by HmHost (service-supervised, no UAC).
                        // (EN/FR: Mode RawInput UMDF2/HIDMaestro : clavier et souris indépendants
                        // par joueur, hébergés par HmHost - supervisé par le service, sans UAC.)
                        _joy = new VirtualHmKeyboard(PlayerIndex);
                        _virtualMouse = new VirtualHmMouse(PlayerIndex);

                        if (_virtualMouse is VirtualHmMouse hmMouse)
                        {
                            hmMouse.OnLeftMouseButtonChanged += HandleTriggerButton;
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
            // [V57d] EN: Include the UMDF2 mode in the connection notification
            //     FR: Inclure le mode UMDF2 dans la notification de connexion
            string mouseMode = Options.Instance.DefaultMouseMode == MouseMode.SendInput
                ? "SendInput (Legacy)"
                : (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf ? "UMDF2 HIDMaestro (Multi-Player)" : "VMulti (Multi-Player)");
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
            else if (_hiddenWnd != null && Options.Instance.DefaultMouseMode != MouseMode.SendInput) // [V57d] EN/FR: dispose the hidden window for every non-SendInput mode (RawInput + UMDF2)
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

            if (_joy != null)
            {
                _joy.ResetAll();
                _joy.Dispose();
                _joy = null;
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

            // [V55y] Dispose the reload rumble sequencer (EN/FR: Disposer le séquenceur vibration rechargement)
            if (_reloadRumbleTimer != null)
            {
                _reloadRumbleTimer.Dispose();
                _reloadRumbleTimer = null;
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
            else if (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf)
            {
                // [V57e] EN: UMDF2 - remove the player's device on wiimote disconnect.
                //     HmHost disposes the controller; the stable identity recreates it
                //     with the SAME paths on the next connect, so nothing lingers
                //     between sessions and EmulationStation never sees an empty gun.
                //     FR: UMDF2 - supprimer le device du joueur à la déconnexion de la
                //     wiimote. HmHost supprime le controller ; l'identité stable le
                //     recrée avec les MÊMES chemins à la prochaine connexion : rien ne
                //     subsiste entre les sessions et EmulationStation ne voit jamais
                //     de gun fantôme.
                ServiceClient.DisablePlayer(PlayerIndex);
                SimpleLogger.Instance.Info($"[V57e] UMDF2: P{PlayerIndex} device removed (wiimote disconnected).");
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
            // EN: Check if Motion Plus and Accelerometer are disabled via settings
            // FR: Vérifier si MP et Accel sont désactivés via les paramètres
            if (Options.Instance.DisableMotionPlusAndAccelerometer)
            {
                return; // Skip all gyroscope/accelerometer processing
            }


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

                        // [FIX V26] EN: Cooldown against the standalone-MP re-activation loop.
                        // External MP adapters without a passthrough extension report extension=0 in
                        // every status report, which triggers "MotionPlus Removed" → re-activation →
                        // status → removed → ... every ~600ms. When no real passthrough extension is
                        // detected (standalone MP or None), refuse to re-activate within 8s of the
                        // last completed activation. Real Nunchuk/Classic hot-plug still goes through.
                        // FR: Cooldown contre la boucle de ré-activation MP standalone. Les adaptateurs
                        // MP externes sans extension en passthrough rapportent extension=0 dans chaque
                        // rapport de statut, ce qui déclenche « MotionPlus Removed » → ré-activation →
                        // statut → removed → ... toutes les ~600ms. Sans vraie extension détectée
                        // (MP standalone ou None), refuser de ré-activer dans les 8s suivant la
                        // dernière activation terminée. Le hot-plug réel Nunchuk/Classic passe toujours.
                        if ((ext == ExtensionType.None || ext == ExtensionType.MotionPlus) &&
                            (DateTime.Now - _lastMpActivationCompleted).TotalSeconds < 8.0)
                        {
                            SimpleLogger.Instance.Info(string.Format("[P{0}] [V26] AutoEnableMotionPlus skipped (standalone MP cooldown, ext: {1})", PlayerIndex, ext));
                            return;
                        }

                        // EN: Decision based ONLY on detected ExtensionType — do NOT use Status.Extension here.
                        // On V2 TR Wiimotes, Status.Extension is ALWAYS true (built-in MP shows as extension),
                        // so it cannot be used to detect Nunchuk presence. The bit-0 detection in ParseMotionPlus
                        // handles hot-plug reliably once in standalone mode.
                        // FR: Décision basée UNIQUEMENT sur l'ExtensionType détecté — ne PAS utiliser Status.Extension ici.
                        // Sur les V2 TR, Status.Extension est TOUJOURS true (le MP intégré occupe le port extension),
                        // donc on ne peut pas l'utiliser pour détecter la présence du Nunchuk.
                        if (ext == ExtensionType.ClassicController || ext == ExtensionType.MotionPlusOther)
                        {
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.ClassicController);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled with Classic Passthrough", PlayerIndex));
                            StopMpNunchukProbe();
                        }
                        else if (ext == ExtensionType.Nunchuk || ext == ExtensionType.MotionPlusNunchuk)
                        {
                            // EN: Real Nunchuk detected (not just MP showing as extension)
                            // FR: Vrai Nunchuk détecté (pas juste le MP qui occupe le port extension)
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.Nunchuk);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled with Nunchuk Passthrough (ExtType: {1})", PlayerIndex, ext));
                            StopMpNunchukProbe();
                        }
                        else
                        {
                            // EN: MotionPlus detected (standalone, ext == MotionPlus or None).
                            // Always start in standalone mode (0x04). ParseMotionPlus bit-0 detection
                            // will fire GetStatus() when a Nunchuk is hot-plugged, then switch to passthrough.
                            // FR: MotionPlus détecté (standalone, ext == MotionPlus ou None).
                            // Toujours démarrer en mode standalone (0x04). La détection bit-0 dans ParseMotionPlus
                            // déclenchera GetStatus() quand un Nunchuk sera branché, puis basculera en passthrough.
                            Wiimote.EnableMotionPlus(MotionPlusExtensionType.NoExtension);
                            SimpleLogger.Instance.Info(string.Format("[P{0}] MotionPlus enabled standalone (No extension / ExtType: {1})", PlayerIndex, ext));
                            StopMpNunchukProbe();
                        }
                        
                        // Ensure report type is maintained (MotionPlus activation can reset it)
                        // (EN/FR: S'assurer que le type de rapport est maintenu)
                        UpdateIRSensitivity();
                        _lastMpActivationCompleted = DateTime.Now;
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

            DateTime diagNow = _lastReportTime;
            long diagStateStart = TimingDiagnostics.BeginState();
            double diagStateDtMs = _diagLastStateTicks == 0 ? 0.0 : (Stopwatch.GetTimestamp() - _diagLastStateTicks) * 1000.0 / Stopwatch.Frequency;
            _diagLastStateTicks = Stopwatch.GetTimestamp();
            double diagGyroUs = 0.0;
            double diagPositionUs = 0.0;
            double diagIrUs = 0.0;
            if (_diagLastReportTime != DateTime.MinValue)
            {
                double delta = (diagNow - _diagLastReportTime).TotalMilliseconds;
                if (delta >= 0 && delta < 1000)
                {
                    _diagDeltaSum += delta;
                    _diagDeltaCount++;
                    if (delta > _diagDeltaMax) _diagDeltaMax = delta;
                }
            }
            _diagLastReportTime = diagNow;
            _diagReportCount++;
            if (_diagWindowStart == DateTime.MinValue)
                _diagWindowStart = diagNow;
            else if ((diagNow - _diagWindowStart).TotalMilliseconds >= 5000)
            {
                double rate = _diagReportCount / (diagNow - _diagWindowStart).TotalSeconds;
                double avgGap = _diagDeltaCount > 0 ? _diagDeltaSum / _diagDeltaCount : 0;
                SimpleLogger.Instance.Info(string.Format("[P{0}] [DIAG] Rate: {1:F0} Hz, Gap avg: {2:F1}ms, Gap max: {3:F0}ms", PlayerIndex, rate, avgGap, _diagDeltaMax));
                _avgReportIntervalMs = avgGap > 1.0 ? avgGap : 10.0;
                _diagReportCount = 0;
                _diagWindowStart = diagNow;
                _diagDeltaSum = 0;
                _diagDeltaMax = 0;
                _diagDeltaCount = 0;
            }

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
                long diagGyroStart = TimingDiagnostics.BeginStage();
                ProcessGyroscopeData(e.WiimoteState);
                diagGyroUs = TimingDiagnostics.ElapsedUs(diagGyroStart);

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
                long diagPositionStart = TimingDiagnostics.BeginStage();
                var scaledPos = _calculator.GetScaledPosition(ir, buttons, _lastState);
                diagPositionUs = TimingDiagnostics.ElapsedUs(diagPositionStart);

                // Apply Aspect Ratio Correction (EN/FR: Appliquer correction de format d'image)
                if (scaledPos.HasValue)
                {
                    scaledPos = ApplyAspectRatioCorrection(scaledPos.Value, _mode);
                }

                ManageCalibration(e.Wiimote, buttons, _lastState, scaledPos);

                bool isOnScreen = scaledPos.HasValue;

                bool mLeft = false, mRight = false, mMiddle = false;
                int finalX = _lastX, finalY = _lastY;
                bool reloadTriggered = false; // Define here to be accessible in processMouseMotion

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

                    // [V55y] Capture the PHYSICAL reload button state (right-click mapping +
                    // shake reload gesture) BEFORE the off-screen block: the edge detection
                    // below must ignore the reload inputs injected by the off-screen redirect.
                    // (EN/FR: Capturer l'état du bouton reload PHYSIQUE (mapping clic droit +
                    // geste shake) AVANT le bloc hors écran : la détection d'arête ci-dessous
                    // doit ignorer les entrées recharge injectées par la redirection hors écran.)
                    bool rawReloadButton = mRight;

                    // 3. Off-screen Reload Logic (EN/FR: Logique Rechargement Hors-écran)
                    DateTime now = GetNow();

                    if (isOnScreen)
                    {
                        _onScreenConsecutiveFrames++;
                        _offScreenConsecutiveFrames = 0;
                    }
                    else
                    {
                        _offScreenConsecutiveFrames++;
                        _onScreenConsecutiveFrames = 0;
                    }

                    // [V55] Per-profile resolution: legacy Off-Screen Reload (override or
                    // global) + TC Cover mode (inhibits the two legacy functions).
                    // TC COVER LOGIC (Time Crisis): hold button ON-screen (exit cover/planque),
                    // release OFF-screen (enter cover/planque). This is the OPPOSITE of a
                    // standard reload — in TC games you hold the pedal to AIM, release to HIDE.
                    // (EN/FR: Résolution par profil : Off-Screen Reload héritage + mode Planque TC.
                    // LOGIQUE TC : maintien bouton EN visant l'écran (sortie planque), relâché HORS
                    // écran (rentrée planque). Inverse du reload classique — dans TC on maintient
                    // la pédale pour viser, on relâche pour se planquer.)
                    bool offScreenReloadEnabled = ResolveOffScreenReloadEnabled();
                    bool tcCoverActive = _playerMappings != null && _playerMappings.TCCoverReload;

                    if (!isOnScreen)
                    {
                        if (tcCoverActive)
                        {
                            // [V55] TC Cover: OFF-screen = in cover (planque) → RELEASE the TC
                            // button so the game registers the hide. Lock all mouse outputs.
                            // (EN/FR: Planque TC : HORS écran = en planque → RELÂCHER le bouton TC
                            // pour que le jeu enregistre la planque. Sorties souris verrouillées.)
                            bool rawTriggerTc = mLeft;
                            mLeft = false;
                            mMiddle = false;
                            mRight = false;

                            if (_hasAimedAtScreenOnce)
                            {
                                // Ensure any held TC key is released while off-screen
                                // (EN/FR: S'assurer que la touche TC maintenue est relâchée hors écran)
                                ReleaseTcCoverKey();
                                if (rawTriggerTc) _suppressLeftClickUntilRelease = true;
                            }

                            // [V56] TC Cover: entering cover = the TC reload moment. The input is
                            // INVERTED in TC mode: going OFF-screen RELEASES the TC button (= hide
                            // and reload in the game) — vibrate exactly here, once per transition.
                            // (EN/FR: Planque TC : l'entrée en planque = le moment du rechargement
                            // TC. L'entrée est INVERSÉE en mode TC : passer HORS écran RELÂCHE le
                            // bouton TC (= se cacher et recharger dans le jeu) — vibrer exactement
                            // ici, une seule fois par transition.)
                            if (_wasOnScreen && _hasAimedAtScreenOnce)
                            {
                                TriggerReloadRumble();
                            }
                        }
                        else
                        {
                            // [V55t/V55u] UNIFIED NON-TC OFF-SCREEN HANDLING:
                            // - The TRIGGER (mLeft / Mouse Left = railshooter fire) is ALWAYS
                            //   locked while off-screen. The ONLY case where an off-screen
                            //   trigger press does something is Off-Screen Reload enabled
                            //   (global option or per-profile override): EVERY physical
                            //   trigger pulse is redirected to the reload input (mRight),
                            //   mirroring the physical state (no 1-per-session limit — only
                            //   the "Auto" mode is 1x per off-screen session). Anti-accident:
                            //   a trigger held off-screen never fires when returning on-screen.
                            // - The PHYSICAL RELOAD BUTTON (mRight / Mouse Right) is NEVER
                            //   locked and NEVER limited: it passes through freely while
                            //   off-screen, whether Off-Screen Reload is enabled or not.
                            // - Middle click passes through as well: ONLY the trigger is locked.
                            // (EN/FR: GESTION HORS ÉCRAN UNIFIÉE SANS PLANQUE TC :
                            // - La GÂCHETTE (mLeft / Mouse Left = tir railshooter) est
                            //   TOUJOURS verrouillée hors écran. Le SEUL cas où un appui
                            //   gâchette hors écran fait quelque chose est le Off-Screen
                            //   Reload activé (option globale ou override par profil) :
                            //   CHAQUE impulsion physique est redirigée vers l'input
                            //   reload (mRight), en reflétant l'état physique (PAS de limite
                            //   1 par session — seul le mode « Auto » est 1× par session hors
                            //   écran). Anti-accident : une gâchette maintenue hors écran ne
                            //   tire jamais au retour à l'écran.
                            // - Le BOUTON PHYSIQUE DE RELOAD (mRight / Mouse Right) n'est
                            //   JAMAIS verrouillé ni limité à 1 impulsion : il passe
                            //   librement hors écran, que le Off-Screen Reload soit activé
                            //   ou non.
                            // - Le clic milieu passe également : SEULE la gâchette est verrouillée.)
                            bool rawTrigger = mLeft;

                            // [V55s/V55t] Lock ONLY the trigger while off-screen
                            // (EN/FR: Verrouiller SEULEMENT la gâchette hors écran)
                            mLeft = false;

                            if (rawTrigger && _hasAimedAtScreenOnce)
                            {
                                // Prevent the shot firing when returning on-screen with the trigger still held
                                // (EN/FR: Empêcher le tir au retour à l'écran si la gâchette est toujours maintenue)
                                _suppressLeftClickUntilRelease = true;
                            }

                            // Startup protection: No reload allowed if the Wiimote hasn't aimed at the screen at least once
                            // (EN/FR: Protection démarrage: Pas de reload si la Wiimote n'a pas encore visé l'écran au moins une fois)
                            if (offScreenReloadEnabled && _hasAimedAtScreenOnce)
                            {
                                if (ResolveOffScreenAutoEnabled())
                                {
                                    // Auto-reload: trigger a single right-click when going off-screen (1x)
                                    // Only 1 reload per off-screen session is allowed
                                    // Cooldown of 250ms prevents rapid re-triggering / bounce at the screen edge
                                    // (EN/FR: Rechargement auto: 1 seul clic droit par session hors écran avec délai 250ms anti-rebond)
                                    if (!_offScreenReloadPerformed && _wasOnScreen && _offScreenReloadClickSequence == 0 && (now - _lastAutoReloadTime).TotalMilliseconds >= 250)
                                    {
                                        _offScreenReloadClickSequence = 1;
                                        _lastAutoReloadTime = now;
                                        _offScreenReloadPerformed = true;
                                        reloadTriggered = true;
                                        TriggerReloadRumble(); // [V55y] Configurable reload rumble (EN/FR: Vibration recharge paramétrable)
                                        SimpleLogger.Instance.Info($"[P{PlayerIndex}] Auto-reload sequence started (1x mRight)");
                                    }

                                    if (_offScreenReloadClickSequence > 0)
                                    {
                                        if (_offScreenReloadClickSequence <= 3) // Hold right-click for 3 frames to ensure game registers it
                                        {
                                            mRight = true;
                                            _offScreenReloadClickSequence++;
                                        }
                                        else
                                        {
                                            _offScreenReloadClickSequence = -1; // Sequence finished
                                        }
                                    }
                                }

                                // Manual reload via TRIGGER REDIRECT: EVERY physical trigger pulse
                                // off-screen is redirected to the reload input (mRight) — NOT
                                // limited to 1 per session ([V55u]: only the "Auto" mode is 1x
                                // per off-screen session). The redirected state mirrors the
                                // physical trigger state (press and hold supported).
                                // (EN/FR: Rechargement manuel par REDIRECTION GÂCHETTE : CHAQUE
                                // impulsion physique de la gâchette hors écran est redirigée
                                // vers l'input reload (mRight) — SANS limite de 1 par session
                                // ([V55u] : seul le mode « Auto » est limité à 1× par session
                                // hors écran). L'état redirigé reflète l'état physique de la
                                // gâchette (appui et maintien pris en charge).)
                                if (rawTrigger)
                                {
                                    mRight = true;
                                    if (!_osTriggerReloadWasActive)
                                    {
                                        // Rising edge of a new deliberate reload press: log + rumble
                                        // (EN/FR: Front montant d'un nouvel appui reload délibéré : log + vibration)
                                        reloadTriggered = true;
                                        TriggerReloadRumble(); // [V55y] Configurable reload rumble (EN/FR: Vibration recharge paramétrable)
                                        SimpleLogger.Instance.Info($"[P{PlayerIndex}] Trigger reload (redirected to mRight)");
                                    }
                                }
                                _osTriggerReloadWasActive = rawTrigger;
                            }
                            else
                            {
                                // EN/FR: [V55u] Redirect not active (reload disabled or no first aim yet): reset the edge tracker
                                _osTriggerReloadWasActive = false;
                            }
                        }
                    }
                    else if (isOnScreen)
                    {
                        // [V55] ON-screen: TC Cover = hold the TC button (player is aiming = out of cover)
                        // (EN/FR: À l'ÉCRAN : Planque TC = maintenir le bouton TC (joueur vise = sort de planque))
                        if (tcCoverActive && _hasAimedAtScreenOnce)
                        {
                            ApplyTcCoverHold(ref mLeft, ref mMiddle, ref mRight);
                        }

                        // Debounce logic: If user was holding the trigger off-screen, suppress the shot on-screen until they release it
                        // (EN/FR: Logique anti-rebond: Si le joueur maintenait la gâchette hors écran, on bloque le tir jusqu'au relâchement)
                        if (_suppressLeftClickUntilRelease)
                        {
                            if (mLeft) mLeft = false; // Suppress the shot
                            else _suppressLeftClickUntilRelease = false; // Trigger released, allow next shot
                        }

                        // Re-arm off-screen reload ONLY when tracking is stable on-screen (at least 3 consecutive frames)
                        // (EN/FR: Réarmer le reload hors écran UNIQUEMENT si le tracking est stable sur l'écran >= 3 frames)
                        if (_onScreenConsecutiveFrames >= 3)
                        {
                            _hasAimedAtScreenOnce = true;
                            _offScreenReloadPerformed = false; // [V55u] Reset AUTO reload authorization for next off-screen session
                            if (_offScreenReloadClickSequence != 0)
                            {
                                _offScreenReloadClickSequence = 0;
                            }
                        }
                    }
                    _wasOnScreen = isOnScreen;

                    // [V55y] Reload rumble on the PHYSICAL reload button (right-click mapping
                    // or shake reload gesture) rising edge — fires whether Off-Screen Reload
                    // is enabled or not (per user spec), on-screen and off-screen.
                    // (EN/FR: Vibration recharge au front montant du bouton reload PHYSIQUE
                    // (mapping clic droit ou geste shake) — se déclenche que le Off-Screen
                    // Reload soit activé ou non (spec utilisateur), à l'écran comme hors écran.)
                    if (rawReloadButton && !_lastRawReloadButton)
                    {
                        TriggerReloadRumble();
                    }
                    _lastRawReloadButton = rawReloadButton;

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
                            // [V28b] Strength 0 = smoothing disabled for this Wiimote model (V1 or V2 column).
                            // FR: Force 0 = lissage désactivé pour ce modèle de Wiimote (colonne V1 ou V2).
                            if (Options.Instance.EnableIRSmoothing && ActiveIRSmoothingStrength > 0 && _lastX != 0 && _lastY != 0)
                            {
                                float alpha = 1.0f / Math.Max(1, Math.Min(10, ActiveIRSmoothingStrength));
                                finalX = (int)(alpha * finalX + (1.0f - alpha) * _lastX);
                                finalY = (int)(alpha * finalY + (1.0f - alpha) * _lastY);
                            }

                            // Velocity & Extrapolation
                            if (_lastX != 0 && _lastY != 0)
                            {
                                velocityX = finalX - _lastX;
                                velocityY = finalY - _lastY;
                                // [V28b] Strength 0 = extrapolation disabled for this Wiimote model.
                                // FR: Force 0 = extrapolation désactivée pour ce modèle de Wiimote.
                                if (Options.Instance.UseIRExtrapolation && ActiveIRExtrapolationStrength > 0f)
                                {
                                    finalX = (int)(finalX + velocityX * ActiveIRExtrapolationStrength);
                                    finalY = (int)(finalY + velocityY * ActiveIRExtrapolationStrength);
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

                            // Update global tracking of which screen is being aimed at
                            // (EN/FR: Mettre à jour le suivi global de l'écran visé)
                            Program.LastActiveScreenIndex = this.ScreenIndex;

                            // Virtual Polling Storage
                            _lastX_Raw = finalX;
                            _lastY_Raw = finalY;
                            _lastVelX_Diag = velocityX;
                            _lastVelY_Diag = velocityY;

                            // [FIX V25] Push the real position into the replay buffer and
                            // track burst cycle length (gap > 20ms = burst boundary).
                            long replayNowTicks = Stopwatch.GetTimestamp();
                            long replayPrevTicks = _replayLastSampleTicks;
                            _replayLastSampleTicks = replayNowTicks;
                            double replayNowMs = replayNowTicks * 1000.0 / Stopwatch.Frequency;
                            if (replayPrevTicks != 0)
                            {
                                double replayPrevMs = replayPrevTicks * 1000.0 / Stopwatch.Frequency;
                                if (replayNowMs - replayPrevMs > 20.0)
                                {
                                    // [FIX V27] Track the dead-gap size (not the full burst period) to size the
                                    // replay delay. The delay only needs to cover the LARGEST inter-sample gap
                                    // (dead time before a new batch lands), not the whole burst cycle. V26 sized
                                    // the delay on the full period EMA (~36-39ms) + 6ms = 42-45ms, which was
                                    // over-conservative by ~12ms and made the cursor feel unresponsive.
                                    // FR: Suivre la taille du trou mort (pas le cycle complet) pour dimensionner
                                    // le délai. Le délai doit seulement couvrir le PLUS GRAND écart entre
                                    // échantillons, pas tout le cycle de salve. V26 utilisait la période EMA
                                    // (~36-39ms) + 6ms = 42-45ms, soit ~12ms de trop → curseur peu réactif.
                                    _burstPeriodEmaMs = _burstPeriodEmaMs <= 0.0 ? (replayNowMs - replayPrevMs) : (0.8 * _burstPeriodEmaMs + 0.2 * (replayNowMs - replayPrevMs));
                                    _replayDelayMs = Math.Max(22.0, Math.Min(55.0, _burstPeriodEmaMs + 3.0));
                                }
                            }
                            lock (_replayLock)
                            {
                                _replayTimeMs.Add(replayNowMs);
                                _replayX.Add(finalX);
                                _replayY.Add(finalY);
                                if (_replayTimeMs.Count > 64)
                                {
                                    int excess = _replayTimeMs.Count - 64;
                                    _replayTimeMs.RemoveRange(0, excess);
                                    _replayX.RemoveRange(0, excess);
                                    _replayY.RemoveRange(0, excess);
                                }
                                while (_replayTimeMs.Count > 2 && replayNowMs - _replayTimeMs[0] > 250.0)
                                {
                                    _replayTimeMs.RemoveAt(0);
                                    _replayX.RemoveAt(0);
                                    _replayY.RemoveAt(0);
                                }
                            }
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

                    // [DIAG] One compact CSV row per StateChanged callback, emitted after cursor
                    // processing so callback cadence (dt) and hot-path stage costs can be compared
                    // between V1 and MotionPlus Inside V2. See WiimoteLib/TimingDiagnostics.cs.
                    TimingDiagnostics.StateReport(
                        diagStateStart,
                        e.WiimoteState.ExtensionType.ToString(),
                        diagGyroUs,
                        diagIrUs,
                        diagPositionUs,
                        ir.IRSensor0.Found,
                        ir.IRSensor1.Found,
                        _lastX,
                        _lastY,
                        string.Format("dt={0:F3}ms;mode={1};report={2};mp={3}",
                            diagStateDtMs, _mode, e.WiimoteState.ReportType,
                            (e.WiimoteState.ExtensionType == ExtensionType.MotionPlus || e.WiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk) ? 1 : 0));
                }

                if ((_mode == WiiMoteMode.Mouse || _mode == WiiMoteMode.Mouse43 || _mode == WiiMoteMode.MouseFPS || _mode == WiiMoteMode.Keyboardpad) && _joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                {
                    // Lock keyboard shooting/reloading inputs while off-screen (EN/FR: Bloquer inputs clavier tir/recharge hors écran)
                    // [V54] Lock keyboard/mouse outputs while OFF-screen for BOTH modes:
                    // legacy off-screen reload and TC cover (all outputs are silenced in cover).
                    // (EN/FR: Verrouiller sorties clavier/souris HORS écran pour les DEUX
                    // modes : reload hors-écran hérité et planque TC (tout est masqué en planque).)
                    bool lockOffscreenShooting = (ResolveOffScreenReloadEnabled() || (_playerMappings != null && _playerMappings.TCCoverReload)) && !isOnScreen;

                    // Mask inputs if specific button is consumed by hotkey (EN/FR: Masquer inputs si bouton consommé par hotkey)
                    SendKeyEvent(_playerMappings.WiiA, !lockOffscreenShooting && buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A"), _lastState.A);
                    SendKeyEvent(_playerMappings.WiiB, !lockOffscreenShooting && buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B"), _lastState.B);
            SendKeyEvent(_playerMappings.WiiUp, buttons.Up && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Up") && !_isOffsetAdjustmentActive && !UI.Modern.Forms.EsProfileTileDialog.IsOpen, _lastState.Up);
            SendKeyEvent(_playerMappings.WiiDown, buttons.Down && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Down") && !_isOffsetAdjustmentActive && !UI.Modern.Forms.EsProfileTileDialog.IsOpen, _lastState.Down);
            SendKeyEvent(_playerMappings.WiiLeft, buttons.Left && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Left") && !_isOffsetAdjustmentActive && !UI.Modern.Forms.EsProfileTileDialog.IsOpen, _lastState.Left);
            SendKeyEvent(_playerMappings.WiiRight, buttons.Right && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Right") && !_isOffsetAdjustmentActive && !UI.Modern.Forms.EsProfileTileDialog.IsOpen, _lastState.Right);
                    SendKeyEvent(_playerMappings.WiiOne, buttons.One && !HotkeyManager.IsButtonConsumed(PlayerIndex, "One"), _lastState.One);
                    SendKeyEvent(_playerMappings.WiiTwo, buttons.Two && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Two"), _lastState.Two);
                    SendKeyEvent(_playerMappings.WiiPlus, buttons.Plus && !suppressMinusPlus && !UI.Modern.Forms.EsProfileTileDialog.IsOpen && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus"), _lastState.Plus);
                    SendKeyEvent(_playerMappings.WiiMinus, buttons.Minus && !suppressMinusPlus && !UI.Modern.Forms.EsProfileTileDialog.IsOpen && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Minus"), _lastState.Minus);

                    if (hasNunchuk)
                    {
                        SendKeyEvent(_playerMappings.NunC, !lockOffscreenShooting && nunchuk.C && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunC"), _lastNunchukState.C);
                        SendKeyEvent(_playerMappings.NunZ, !lockOffscreenShooting && nunchuk.Z && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunZ"), _lastNunchukState.Z);
                        SendKeyEvent(_playerMappings.NunUp, nunchuk.Joystick.Y > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp"), _lastNunchukState.Joystick.Y > 0.3f);
                        SendKeyEvent(_playerMappings.NunDown, nunchuk.Joystick.Y < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown"), _lastNunchukState.Joystick.Y < -0.3f);
                        SendKeyEvent(_playerMappings.NunLeft, nunchuk.Joystick.X < -0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft"), _lastNunchukState.Joystick.X < -0.3f);
                        SendKeyEvent(_playerMappings.NunRight, nunchuk.Joystick.X > 0.3f && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight"), _lastNunchukState.Joystick.X > 0.3f);
                    }

                    // [V55] TC Bi-Pedal Mouse Mode: Two pedals configured by user (default Left=DPadLeft, Right=DPadRight)
                    // (EN/FR: Pédale TC bi-directionnelle mode souris : deux pédales configurées)
                    if (_playerMappings != null && _playerMappings.TCBiPedal && !_playerMappings.TCCoverReload)
                    {
                        string leftBtnId = string.IsNullOrEmpty(_playerMappings.TCBiPedalLeftButton) ? "WiiLeft" : _playerMappings.TCBiPedalLeftButton;
                        string rightBtnId = string.IsNullOrEmpty(_playerMappings.TCBiPedalRightButton) ? "WiiRight" : _playerMappings.TCBiPedalRightButton;

                        bool pLeft = IsPhysicalButtonPressed(leftBtnId, buttons, nunchuk, hasNunchuk);
                        bool pRight = IsPhysicalButtonPressed(rightBtnId, buttons, nunchuk, hasNunchuk);

                        if (isOnScreen)
                        {
                            if (pLeft && !pRight) _mouseBiPedalActive = -1;
                            if (pRight && !pLeft) _mouseBiPedalActive = 1;

                            System.Windows.Forms.Keys targetKey = System.Windows.Forms.Keys.None;
                            if (_mouseBiPedalActive == -1)
                            {
                                ButtonAction bAct = GetPhysicalButtonAction(leftBtnId) ?? _playerMappings.WiiLeft;
                                targetKey = (bAct != null && bAct.Key != System.Windows.Forms.Keys.None) ? bAct.Key : System.Windows.Forms.Keys.Left;
                            }
                            else if (_mouseBiPedalActive == 1)
                            {
                                ButtonAction bAct = GetPhysicalButtonAction(rightBtnId) ?? _playerMappings.WiiRight;
                                targetKey = (bAct != null && bAct.Key != System.Windows.Forms.Keys.None) ? bAct.Key : System.Windows.Forms.Keys.Right;
                            }

                            if (targetKey != _mouseBiPedalHeldKey)
                            {
                                if (_mouseBiPedalHeldKey != System.Windows.Forms.Keys.None)
                                    _joy.SendKeyEvent(_mouseBiPedalHeldKey, false);
                                _mouseBiPedalHeldKey = targetKey;
                                if (_mouseBiPedalHeldKey != System.Windows.Forms.Keys.None)
                                    _joy.SendKeyEvent(_mouseBiPedalHeldKey, true);
                            }
                        }
                        else
                        {
                            // [V56a] TC Bi-Pedal: entering cover (ON->OFF transition). The pedal
                            // input is RELEASED (= taking cover = TC reload) — vibrate exactly
                            // here, once per transition, same principle as the single TC cover.
                            // (EN/FR: Pédale TC bi-directionnelle : entrée en planque (transition
                            // ON->OFF). L'entrée pédale est RELÂCHÉE (= se planquer = rechargement
                            // TC) — vibrer exactement ici, une seule fois par transition, même
                            // principe que la planque TC sur un seul bouton.)
                            if (_mouseBiPedalWasOnScreen && _hasAimedAtScreenOnce)
                            {
                                TriggerReloadRumble();
                            }

                            if (_mouseBiPedalHeldKey != System.Windows.Forms.Keys.None)
                            {
                                _joy.SendKeyEvent(_mouseBiPedalHeldKey, false);
                                _mouseBiPedalHeldKey = System.Windows.Forms.Keys.None;
                            }
                        }

                        // [V56a] Track the on-screen state for the cover-enter transition detection
                        // (EN/FR: Suivre l'état à l'écran pour la détection de transition d'entrée en planque)
                        _mouseBiPedalWasOnScreen = isOnScreen;
                    }

                    // --- Keyboard/Mouse Motion Action Processor (EN/FR: Traitement Actions Souris via Mouvement) ---
                    Action<ButtonAction, bool, bool> processMouseMotion = (action, isPressed, wasPressed) =>
                    {
                        if (action == null) return;
                        if (action.Key != System.Windows.Forms.Keys.None) SendKeyEvent(action, isPressed, wasPressed);
                        // Don't override mouse buttons if off-screen reload is active (EN/FR: Ne pas écraser les boutons souris si reload hors écran actif)
                        if (!reloadTriggered)
                        {
                            if (action.Special == SpecialAction.LeftMouse && isPressed) mLeft = true;
                            if (action.Special == SpecialAction.RightMouse && isPressed) mRight = true;
                            if (action.Special == SpecialAction.MiddleMouse && isPressed) mMiddle = true;
                        }
                    };

                    // --- Accel & Gyro Motion Actions Processing (EN/FR: Traitement des actions de mouvement Accel & Gyro) ---
                    // EN: Skip all accelerometer and gyro motion actions if disabled via settings
                    // FR: Ignorer toutes les actions de mouvement accéléromètre et gyro si désactivé dans les options
                    if (!Options.Instance.DisableMotionPlusAndAccelerometer)
                    {
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

                            _lastGyroMotionPlusUp = gUp; _lastGyroMotionPlusDown = gDown; _lastGyroMotionPlusLeft = gLeft; _lastGyroMotionPlusRight = gRight;
                            _lastGyroMotionPlusRollLeft = gRollLeft; _lastGyroMotionPlusRollRight = gRollRight;
                        }
                    }

                    _joy.CommitChanges();

                    // --- FINALLY: Send Mouse State (EN/FR: ENFIN : Envoyer l'état de la souris) ---
                    if (_virtualMouse != null)
                    {
                        // [FIX V23a/V23b] Movement is delegated to the prediction thread ONLY when it
                        // actually runs (rate > 0). At rate 0 the prediction is disabled, so the real
                        // thread must move the cursor or it freezes (observed: pointer static until a button press).
                        // FR: Le mouvement est délégué au thread de prédiction SEULEMENT quand il
                        // s'exécute réellement (rate > 0). À rate 0 la prédiction est désactivée, donc le
                        // thread réel doit déplacer le curseur sinon il gèle (constaté: pointeur immobile jusqu'à un bouton).
                        // [V28] Rate is resolved per Wiimote model (V2 TR column in options).
                        // FR: Le taux est résolu selon le modèle de Wiimote (colonne V2 TR dans les options).
                        if (Options.Instance.EnableVirtualPolling && ActiveVirtualPollingRate > 0)
                        {
                            // Real thread only sends clicks, Virtual Polling thread handles cursor movement
                            // We MUST pass isAbsolute=true and _lastX_Raw/_lastY_Raw so VMulti uses the absolute digitizer for clicks
                            _virtualMouse.UpdateMouse(_lastX_Raw, _lastY_Raw, mLeft, mRight, mMiddle, false, true);
                        }
                        else
                        {
                            // Standard polling (or Virtual Polling with low target rate) handles both cursor movement and clicks
                            if (_lastMoveCursor_Raw)
                                _virtualMouse.UpdateMouse(_lastX_Raw, _lastY_Raw, mLeft, mRight, mMiddle, true, true);
                            else
                            {
                                // EN: When off-screen, only send button states without moving/locking the cursor,
                                // allowing the physical PC mouse to retain control freely.
                                // FR: Hors écran, envoyer uniquement l'état des boutons sans déplacer/bloquer le curseur,
                                // laissant la souris physique du PC totalement libre.
                                _virtualMouse.UpdateMouse(_lastX_Raw, _lastY_Raw, mLeft, mRight, mMiddle, false, true);
                            }
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

        // =====================================================================================
        // [V55] TC COVER AUTO-RELOAD (EN/FR: PLANQUE TC - AUTO-RECHARGEMENT)
        // Time Crisis (CORRECTED): aiming ON-screen HOLDS the TC button (exit cover),
        // aiming OFF-screen RELEASES it (enter cover). Inhibits legacy off-screen
        // reload functions while active for the profile.
        // =====================================================================================
        private bool _tcKeyHeld = false;                                       // A TC key is currently held down (EN/FR: Touche TC tenue)
        private System.Windows.Forms.Keys _tcHeldKey = System.Windows.Forms.Keys.None;
        private bool _gpTcHasAimedOnce = false;                                // GamePad side: has aimed at the screen once (EN/FR: A visé l'écran au moins une fois)
        private bool _gpTcWasOnScreen = false;                                 // GamePad side: was on-screen in TC Cover (EN/FR: Était à l'écran en planque TC)
        private bool _gpFireWasPressed = false;                                // [V56] GamePad: previous frame state of the physical fire button (EN/FR: État bouton tir physique à la frame précédente)
        private bool _gpReloadBtnWasPressed = false;                            // [V56] GamePad: previous frame state of the physical reload button (EN/FR: État bouton reload physique à la frame précédente)
        private bool _gpFireHeldRumble = false;                                // [V56a] GamePad: fire button held ON-screen (continuous rumble source, like mouse mode)
        private bool _mouseBiPedalWasOnScreen = false;                          // [V56a] Mouse TC Bi-Pedal: was on-screen last frame (cover-enter detection)
        private bool _gpBiPedalWasOnScreen = false;                             // [V56a] GamePad TC Bi-Pedal: was on-screen last frame (cover-enter detection)

        // [V55] Off-Screen Reload GamePad state (EN/FR: État Reload hors-écran GamePad)
        private bool _gpOffScreenReloadPerformed = false;  // One reload per off-screen session (EN/FR: 1 reload par session hors écran)
        private bool _gpWasOnScreen = false;               // Was on-screen previous frame (EN/FR: Était à l'écran frame précédente)
        private bool _gpHasAimedOnce = false;              // Has aimed at screen since startup (EN/FR: A visé l'écran depuis le démarrage)
        private DateTime _gpLastAutoReloadTime = DateTime.MinValue; // Cooldown for auto reload (EN/FR: Cooldown auto-reload)

        // [V55] TC Bi-directional Pedal state (EN/FR: État Pédale TC bi-directionnelle)
        // 0=none, -1=Left held, 1=Right held
        private int _gpBiPedalActive = 0;
        private int _mouseBiPedalActive = 0;
        private System.Windows.Forms.Keys _mouseBiPedalHeldKey = System.Windows.Forms.Keys.None;

        /// <summary>
        /// EN: [V54] Effective legacy Off-Screen Reload state: per-profile override (-1 = follow global).
        /// FR: [V54] État effectif du reload hors-écran hérité : override par profil (-1 = suivre le global).
        /// </summary>
        private bool ResolveOffScreenReloadEnabled()
        {
            if (_playerMappings == null) return Options.Instance.EnableOffScreenReload;
            if (_playerMappings.OffScreenReloadOverride == 1) return true;
            if (_playerMappings.OffScreenReloadOverride == 0) return false;
            return Options.Instance.EnableOffScreenReload;
        }

        /// <summary>
        /// EN: [V55] Effective Auto Off-Screen Reload state: per-profile override (-1=follow global).
        /// FR: [V55] État effectif du mode Auto Reload hors-écran : override par profil (-1=suivre le global).
        /// </summary>
        private bool ResolveOffScreenAutoEnabled()
        {
            if (_playerMappings == null) return Options.Instance.OffScreenReloadAuto;
            if (_playerMappings.OffScreenAutoOverride == 1) return true;
            if (_playerMappings.OffScreenAutoOverride == 0) return false;
            return Options.Instance.OffScreenReloadAuto;
        }

        /// <summary>
        /// EN: [V54] Resolve the TC cover action (mouse mode). Null = hold the right-click
        /// (the existing auto behavior) — used for "auto" or an empty forced mapping.
        /// FR: [V54] Résout l'action planque TC (mode souris). Null = maintenir le clic
        /// droit (comportement auto existant) — utilisé pour "auto" ou un mapping forcé vide.
        /// </summary>
        private ButtonAction ResolveTcCoverAction()
        {
            if (_playerMappings == null) return null;
            string id = _playerMappings.TCCoverButton;
            if (string.IsNullOrEmpty(id) || id.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return null;

            ButtonAction act = GetPhysicalButtonAction(id);
            if (act == null || (act.Special == SpecialAction.None && act.Key == System.Windows.Forms.Keys.None))
                return null; // Empty mapping -> fallback right-click hold (EN/FR: Mapping vide -> repli clic droit)
            return act;
        }

        private ButtonAction GetPhysicalButtonAction(string id)
        {
            switch (id)
            {
                case "WiiA": return _playerMappings.WiiA;
                case "WiiB": return _playerMappings.WiiB;
                case "WiiOne": return _playerMappings.WiiOne;
                case "WiiTwo": return _playerMappings.WiiTwo;
                case "WiiPlus": return _playerMappings.WiiPlus;
                case "WiiMinus": return _playerMappings.WiiMinus;
                case "NunC": return _playerMappings.NunC;
                case "NunZ": return _playerMappings.NunZ;
                default: return null;
            }
        }

        /// <summary>
        /// EN: [V54] Hold the TC cover action while OFF-screen (mouse mode): a mouse
        /// button stays pressed every frame, a keyboard key is sent DOWN once and
        /// tracked for release.
        /// FR: [V54] Maintien de l'action planque TC HORS écran (mode souris) : un bouton
        /// souris reste pressé à chaque frame, une touche clavier est envoyée ENFONCÉE
        /// une seule fois et suivie pour le relâchement.
        /// </summary>
        private void ApplyTcCoverHold(ref bool mLeft, ref bool mMiddle, ref bool mRight)
        {
            ButtonAction act = ResolveTcCoverAction();
            if (act == null) { mRight = true; return; } // Auto / empty = right-click hold (EN/FR: Auto / vide = clic droit maintenu)

            if (act.Special == SpecialAction.RightMouse) mRight = true;
            else if (act.Special == SpecialAction.LeftMouse) mLeft = true;
            else if (act.Special == SpecialAction.MiddleMouse) mMiddle = true;
            else if (act.Key != System.Windows.Forms.Keys.None)
            {
                if (!_tcKeyHeld || _tcHeldKey != act.Key)
                {
                    if (_tcKeyHeld && _tcHeldKey != System.Windows.Forms.Keys.None)
                        _joy.SendKeyEvent(_tcHeldKey, false); // Safety: release a previously held key (EN/FR: Sécurité : relâcher l'ancienne touche)
                    _tcHeldKey = act.Key;
                    _joy.SendKeyEvent(act.Key, true);
                    _tcKeyHeld = true;
                    SimpleLogger.Instance.Info($"[P{PlayerIndex}] TC cover: holding key '{act.Key}' (off-screen)");
                }
            }
        }

        /// <summary>
        /// EN: [V54] Release the TC cover key when back ON-screen.
        /// FR: [V54] Relâche la touche planque TC au retour à l'écran.
        /// </summary>
        private void ReleaseTcCoverKey()
        {
            if (!_tcKeyHeld) return;
            if (_tcHeldKey != System.Windows.Forms.Keys.None)
            {
                _joy.SendKeyEvent(_tcHeldKey, false);
                SimpleLogger.Instance.Info($"[P{PlayerIndex}] TC cover: released key '{_tcHeldKey}' (on-screen)");
            }
            _tcKeyHeld = false;
            _tcHeldKey = System.Windows.Forms.Keys.None;
        }

        /// <summary>
        /// EN: [V54] GamePad side: resolve the TC button's GamePad mapping. "auto" = the
        /// physical button mapped to right-click on the mouse side, applied to the
        /// GamePad side; fallback B.
        /// FR: [V54] Côté GamePad : résout le mapping GamePad du bouton TC. "auto" = le
        /// bouton physique mappé clic droit côté souris, appliqué côté GamePad ; repli B.
        /// </summary>
        private GamePadButton ResolveTcGamePadButton(GamePadMappings mappings)
        {
            string id = mappings.TCCoverButton;
            if (string.IsNullOrEmpty(id) || id.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                id = FindRightClickPhysicalButtonName();
                if (id == null) return mappings.WiiB; // Fallback: B (EN/FR: Repli : B)
            }

            GamePadButton b = GamePadButtonForName(mappings, id);
            return b != GamePadButton.None ? b : mappings.WiiB;
        }

        private GamePadButton GamePadButtonForName(GamePadMappings m, string id)
        {
            switch (id)
            {
                case "WiiA": return m.WiiA;
                case "WiiB": return m.WiiB;
                case "Wii1":
                case "WiiOne": return m.Wii1;
                case "Wii2":
                case "WiiTwo": return m.Wii2;
                case "Wii+":
                case "WiiPlus": return m.WiiPlus;
                case "Wii-":
                case "WiiMinus": return m.WiiMinus;
                case "NunchukC":
                case "NunC": return m.NunchukC;
                case "NunchukZ":
                case "NunZ": return m.NunchukZ;
                default: return GamePadButton.None;
            }
        }

        /// <summary>
        /// EN: [V54] Name of the first physical button mapped to RIGHT-CLICK on the mouse
        /// side, null when none is mapped. Used by the GamePad TC auto resolution.
        /// FR: [V54] Nom du premier bouton physique mappé sur CLIC DROIT côté souris,
        /// null si aucun. Utilisé par la résolution auto TC côté GamePad.
        /// </summary>
        private string FindRightClickPhysicalButtonName()
        {
            PlayerMappings pm = Options.Instance.GetMappingsForPlayer(PlayerIndex);
            if (pm == null) return null;
            if (IsRightMouse(pm.WiiA)) return "WiiA";
            if (IsRightMouse(pm.WiiB)) return "WiiB";
            if (IsRightMouse(pm.WiiOne)) return "WiiOne";
            if (IsRightMouse(pm.WiiTwo)) return "WiiTwo";
            if (IsRightMouse(pm.WiiPlus)) return "WiiPlus";
            if (IsRightMouse(pm.WiiMinus)) return "WiiMinus";
            if (IsRightMouse(pm.NunC)) return "NunC";
            if (IsRightMouse(pm.NunZ)) return "NunZ";
            return null;
        }

        private static bool IsRightMouse(ButtonAction a)
        {
            return a != null && a.Special == SpecialAction.RightMouse;
        }

        /// <summary>
        /// EN: [V55] Checks if a configured physical button ID is pressed.
        /// FR: [V55] Vérifie si l'ID d'un bouton physique configuré est pressé.
        /// </summary>
        private bool IsPhysicalButtonPressed(string buttonId, ButtonState buttons, NunchukState nunchuk, bool hasNunchuk)
        {
            if (string.IsNullOrEmpty(buttonId)) return false;
            switch (buttonId)
            {
                case "WiiLeft":
                case "DPadLeft":
                    return buttons.Left && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiLeft") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Left");
                case "WiiRight":
                case "DPadRight":
                    return buttons.Right && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiRight") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Right");
                case "WiiUp":
                case "DPadUp":
                    return buttons.Up && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiUp") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Up");
                case "WiiDown":
                case "DPadDown":
                    return buttons.Down && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiDown") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Down");
                case "WiiA":
                    return buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiA") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A");
                case "WiiB":
                    return buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiB") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B");
                case "WiiOne":
                    return buttons.One && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiOne") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "One");
                case "WiiTwo":
                    return buttons.Two && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiTwo") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Two");
                case "WiiPlus":
                    return buttons.Plus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiPlus") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus");
                case "WiiMinus":
                    return buttons.Minus && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiMinus") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "Minus");
                case "NunC":
                    return hasNunchuk && nunchuk.C && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunC");
                case "NunZ":
                    return hasNunchuk && nunchuk.Z && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunZ");
                case "auto":
                    string rc = FindRightClickPhysicalButtonName();
                    if (!string.IsNullOrEmpty(rc) && !rc.Equals("auto", StringComparison.OrdinalIgnoreCase))
                        return IsPhysicalButtonPressed(rc, buttons, nunchuk, hasNunchuk);
                    return buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A");
                default:
                    return false;
            }
        }

        // [V42] Long-press PLUS state (EN/FR: État appui long PLUS)
        private DateTime _plusHoldStartUtc = DateTime.MinValue;
        private bool _plusLongPressFired = false;

        /// <summary>
        /// Detect button changes and notify HotkeyManager for hotkey processing
        /// (EN/FR: Détecter changements de boutons et notifier HotkeyManager pour traitement hotkeys)
        /// </summary>
        private void DetectHotkeyButtonChanges(ButtonState currentState, ButtonState lastState, NunchukState currentNunchuk, NunchukState lastNunchuk, bool hasNunchuk)
        {
            // [V42] Long-press PLUS (>= 1s) on ANY connected Wiimote requests the profile
            // tile modal (profiles concern all players at once).
            // [V49] While the modal is OPEN: PLUS (single click) = validate the focused
            // tile/button, MINUS = back inside a folder / close at root.
            // (EN/FR: Appui long PLUS (>= 1s) sur N'IMPORTE QUELLE wiimote connectée
            // demande la modale. Modale OUVERTE : PLUS (un clic) = valider la tuile/bouton
            // focus, MINUS = retour dans un dossier / fermer à la racine.)
            try
            {
                bool esTileOpen = UI.Modern.Forms.EsProfileTileDialog.IsOpen;

                if (esTileOpen)
                {
                    // Single-click remote control (EN/FR: Télécommande un clic)
                    if (currentState.Plus && !lastState.Plus)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimotePlus();
                    if (currentState.Minus && !lastState.Minus)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimoteMinus();

                    // [V52] DPad remote control: the PHYSICAL wiimote DPad navigates the
                    // modal in ANY mode (Mouse, XInput GamePad, DInput GamePad) — the
                    // button is read directly, BEFORE the virtual driver, so the driver
                    // (DInput or XInput) does not matter.
                    // (EN/FR: Télécommande DPad : le DPad PHYSIQUE de la wiimote navigue
                    // la modale dans N'IMPORTE QUEL mode (Souris, XInput, DInput) — le
                    // bouton est lu directement, AVANT le pilote virtuel, donc le pilote
                    // n'a aucune importance.)
                    if (currentState.Up && !lastState.Up)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimoteDirection(0, -1);
                    if (currentState.Down && !lastState.Down)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimoteDirection(0, 1);
                    if (currentState.Left && !lastState.Left)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimoteDirection(-1, 0);
                    if (currentState.Right && !lastState.Right)
                        UI.Modern.Forms.EsProfileTileDialog.NotifyWiimoteDirection(1, 0);
                }

                if (currentState.Plus && !esTileOpen && Options.Instance.EsTileHotkeyEnabled &&
                    !HotkeyManager.IsButtonConsumed(PlayerIndex, "Plus"))
                {
                    if (_plusHoldStartUtc == DateTime.MinValue) _plusHoldStartUtc = DateTime.UtcNow;

                    // [V43] Configurable delay (default 4s): coexists with custom hotkeys
                    // using [+] — short presses never trigger the modal.
                    // (EN/FR: Délai configurable (4s par défaut) : coexiste avec les hotkeys
                    // custom utilisant [+] — les pressions courtes ne déclenchent jamais la modale.)
                    long delayMs = Options.Instance.EsTileHotkeyDelayMs > 0 ? Options.Instance.EsTileHotkeyDelayMs : 4000;
                    if (!_plusLongPressFired && (DateTime.UtcNow - _plusHoldStartUtc).TotalMilliseconds >= delayMs)
                    {
                        _plusLongPressFired = true;
                        SimpleLogger.Instance.Info($"[P{PlayerIndex}] Long press PLUS ({delayMs}ms): requesting profile tile modal");
                        EsScriptIntegration.RaiseTileModalRequested();
                    }
                }
                else
                {
                    _plusHoldStartUtc = DateTime.MinValue;
                    _plusLongPressFired = false;
                }
            }
            catch { }

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
            // EN: Inhibit grenade gesture if MotionPlus/accelerometer is disabled via settings
            // FR: Inhiber le geste grenade si MP/accéléromètre est désactivé via les paramètres
            if (Options.Instance.DisableMotionPlusAndAccelerometer) return false;

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
            int modeVal;

            // [V55m] When mode is locked during game session: only allow 4:3 toggle within active mode family!
            // Mouse <-> Mouse43, GamePad <-> GamePad43.
            // (EN/FR: [V55m] Quand verrouillé en session de jeu : autoriser uniquement la bascule 4:3 dans la famille active !)
            if (_modeLocked)
            {
                if (_mode == WiiMoteMode.Mouse)
                    modeVal = (int)WiiMoteMode.Mouse43;
                else if (_mode == WiiMoteMode.Mouse43)
                    modeVal = (int)WiiMoteMode.Mouse;
                else if (_mode == WiiMoteMode.MouseFPS)
                    modeVal = (int)WiiMoteMode.Mouse43;
                else if (_mode == WiiMoteMode.GamePad)
                    modeVal = (int)WiiMoteMode.GamePad43;
                else if (_mode == WiiMoteMode.GamePad43)
                    modeVal = (int)WiiMoteMode.GamePad;
                else if (_mode == WiiMoteMode.GamePadFPS)
                    modeVal = (int)WiiMoteMode.GamePad43;
                else
                {
                    SimpleLogger.Instance.Info($"[P{PlayerIndex}] SwitchMode blocked (mode locked to {_mode}, no 4:3 swap available)");
                    return;
                }

                SimpleLogger.Instance.Info($"[P{PlayerIndex}] Mode locked - 4:3 toggle: {_mode} -> {(WiiMoteMode)modeVal}");
            }
            else
            {
                modeVal = (int)_mode;
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

                    // [V55] Skip Keyboardpad mode: removed from Home swap cycle in this fork
                    // (EN/FR: [V55] Mode Keyboardpad supprimé de la séquence Home dans ce fork)
                    if (nextMode == WiiMoteMode.Keyboardpad)
                    {
                        foundValidMode = false;
                        continue;
                    }
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

                    if (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf)
                    {
                        // [V57g] EN: In UMDF2 mode the gamepad device is ALWAYS removed when
                        //     leaving GamePad mode (spec parity with the rawinput mouse: the
                        //     gamepad exists ONLY while the wiimote is in GamePad mode, so
                        //     EmulationStation/gun games never see a phantom controller).
                        //     PersistentGamePads is a vmulti Col06 concept and is bypassed.
                        //     FR: En mode UMDF2 le device gamepad est TOUJOURS supprimé en
                        //     quittant le mode GamePad (parité spec avec la souris rawinput :
                        //     le gamepad n'existe QUE tant que la wiimote est en mode GamePad,
                        //     EmulationStation/les jeux gun ne voient jamais de manette
                        //     fantôme). PersistentGamePads est un concept Col06 vmulti,
                        //     contourné ici.
                        WiimoteGun.ServiceClient.RemoveGamepad(PlayerIndex);
                        SimpleLogger.Instance.Info(string.Format("[GamePad P{0}] UMDF2: gamepad device removed (mouse mode back).", PlayerIndex));
                    }
                    else if (!Options.Instance.PersistentGamePads || !Options.Instance.EnableGamePadSwapMode)
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

                // [V31] Ensure the GamePad default.remap fallback profile exists
                // (EN/FR: Garantir l'existence du profil de repli default.remap GamePad)
                try
                {
                    RemapProfileManager.EnsureDefaultGamePadProfile();
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning(string.Format("[GamePad P{0}] Failed to ensure default GamePad profile: {1}", PlayerIndex, ex.Message));
                }

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
                    // [V57g] EN: In RawInputUmdf mode HmGamepad drives BOTH apis (HmHost creates
                    //     or swaps the HIDMaestro gamepad device; the service routes the suffixed
                    //     command). The mouse device stays alive but inert in non-hybrid GamePad
                    //     mode: send a NEUTRAL mouse frame so a button held at the switch cannot
                    //     stay stuck forever (the HmHost watchdog re-submits held frames).
                    //     FR: En mode RawInputUmdf, HmGamepad pilote les DEUX apis (HmHost crée ou
                    //     échange le device gamepad HIDMaestro ; le service route la commande
                    //     suffixée). Le device souris reste vivant mais inerte en mode GamePad
                    //     non-hybride : envoyer une frame souris NEUTRE pour qu'un bouton tenu
                    //     au moment de la bascule ne reste pas bloqué à vie (le watchdog HmHost
                    //     re-soumet les frames tenues).
                    bool umdf2 = Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf;
                    if (umdf2)
                    {
                        WiimoteGun.ServiceClient.EnableGamepad(PlayerIndex, useXInput ? "XINPUT" : "DINPUT");
                        if (!hybridWantsMouse)
                        {
                            (_virtualMouse as VirtualHmMouse)?.ResetAll();
                        }
                    }

                    bool gpOutputMismatch = GamePadOutputApiMismatch(useXInput);
                    if (_virtualGamepad == null || gpOutputMismatch)
                    {
                        if (_virtualGamepad != null)
                        {
                            _virtualGamepad.Disconnect();
                            _virtualGamepad.Dispose();
                        }

                        if (umdf2)
                        {
                            _virtualGamepad = new HmGamepad(PlayerIndex, useXInput);
                        }
                        else if (useXInput)
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
            else if (_mode == WiiMoteMode.Keyboardpad)
            {
                // [V55] Keyboardpad mode is kept for internal use but NOT announced
                // (EN/FR: Mode Keyboardpad conservé en interne mais non annoncé via notification)
            }
            else
            {
                Program.Notify(string.Format("WiimoteGun P{0} : {1} activated", PlayerIndex, modeName));
                if (_mode == WiiMoteMode.GamePad || _mode == WiiMoteMode.GamePad43 || _mode == WiiMoteMode.GamePadFPS)
                {
                    string activeProfile = Program.GetActiveGamePadProfileName();
                    if (!string.IsNullOrEmpty(activeProfile) && !_modeLocked)
                    {
                        // EN: Delay profile notification to appear after mode notification (avoid overlap)
                        // FR: Retarder la notification du profil pour qu'elle apparaisse apres la notification de mode
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            System.Threading.Thread.Sleep(2500);
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
        /// <summary>
        /// Blocks SwitchMode for a short period after calibration is opened or cancelled.
        /// Prevents accidental mode switch when Home is used to cancel calibration.
        /// (EN/FR: Bloque SwitchMode pendant une courte période après ouverture/annulation calibration.
        /// Empêche le changement de mode accidentel quand Home ferme la calibration.)
        /// </summary>
        // [V55] Mode lock on ES game-start (EN/FR: Verrouillage du mode au lancement de jeu ES)
        private bool _modeLocked = false;

        /// <summary>
        /// EN: [V55] Lock or unlock the current WiimoteGun mode. When locked, the Home button
        /// cannot cycle modes. Called by Program.cs on ES game-start / game-end events.
        /// FR: [V55] Verrouille ou déverrouille le mode actuel. Quand verrouillé, le bouton
        /// Home ne peut pas changer le mode. Appelé par Program.cs lors des événements
        /// game-start / game-end ES.
        /// </summary>
        public void LockMode(bool locked)
        {
            _modeLocked = locked;
            if (locked)
                SimpleLogger.Instance.Info($"[P{PlayerIndex}] Mode locked to {_mode} (4:3 toggle permitted)");
            else
                SimpleLogger.Instance.Info($"[P{PlayerIndex}] Mode unlocked");
        }

        private DateTime _modeSwitchBlockedUntil = DateTime.MinValue;

        private void ManageCalibration(Wiimote wiimote, ButtonState buttons, ButtonState lastState, Point2F? scaledPos)
        {

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
                {
                    ticks = Environment.TickCount;
                }
                else if (!buttons.Home && ticks > 0)
                {
                    // Block SwitchMode if calibration was recently opened or cancelled (500ms cooldown)
                    // (EN/FR: Bloquer SwitchMode si calibration récemment ouverte ou annulée - cooldown 500ms)
                    bool modeSwitchBlocked = DateTime.Now < _modeSwitchBlockedUntil;

                    if (_calculator.IsCalibrating || _calculator.IsSelectingMode)
                    {
                        // Close calibration / mode selection (EN/FR: Fermer calibration / sélection mode)
                        _calculator.CancelCalibration();
                        _modeSwitchBlockedUntil = DateTime.Now.AddMilliseconds(500); // Block SwitchMode for 500ms
                    }
                    else if (!modeSwitchBlocked && !_overlayTriggered)
                    {
                        // Normal case: switch mode (EN/FR: Cas normal : changer de mode)
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
                    {
                        // Block SwitchMode after calibration opens (async form creation via PostToUIThread)
                        // (EN/FR: Bloquer SwitchMode après ouverture calibration - création form async via PostToUIThread)
                        _modeSwitchBlockedUntil = DateTime.Now.AddMilliseconds(5000); // Block until calibration confirmed open or cancelled
                        _calculator.Calibrate();
                    }
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
            // [V56a] Also accept the GamePad fire button held ON-screen: full parity with
            // the mouse-mode trigger rumble (same Assign intensity/duration).
            // (EN/FR: Accepter aussi le bouton tir GamePad maintenu À l'écran : parité
            // complète avec la vibration de gâchette du mode souris.)
            if (((_isTriggerPressed && _hasIRSensor) || _gpFireHeldRumble) &&
                Options.Instance.GetAllowContinuousRumble(PlayerIndex))
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
                try
                {
                    if (Wiimote != null && Wiimote.IsConnected)
                    {
                        // [V55y] Latch _isRumbling ONLY on a successful start: the old code
                        // set it BEFORE the connection check, so a rumble requested during
                        // a transient disconnect latched the flag forever and silenced ALL
                        // later reload rumbles ("two long vibrations then nothing").
                        // (EN/FR: Verrouiller _isRumbling SEULEMENT après un démarrage réussi :
                        // l'ancien code le fixait AVANT la vérification de connexion, donc une
                        // vibration demandée pendant une déconnexion transitoire verrouillait
                        // le drapeau pour toujours et réduisait au silence TOUTES les
                        // vibrations de recharge suivantes (« deux vibrations puis plus rien »).)
                        _isRumbling = true;

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

        // ====================================================================
        // [V55y] RELOAD RUMBLE ENGINE (EN/FR: MOTEUR DE VIBRATION RECHARGEMENT)
        // Plays a configurable rumble pattern when a RELOAD occurs, whatever its
        // source: off-screen Auto sequence, off-screen trigger redirect, physical
        // reload button press (right-click mapping / shake reload gesture) — even
        // when Off-Screen Reload is disabled. Patterns simulate a weapon reload
        // ("crique-crique" mechanical style by default). Intensity (0-100) scales
        // the ON pulses (the Wiimote motor is binary: intensity is achieved via
        // pulse width). Robust by design: NO latched state — every path ends with
        // the rumble stopped and the flag cleared.
        // (EN/FR: Joue un motif de vibration configurable quand une RECHARGE survient,
        // quelle que soit sa source : séquence Auto hors écran, redirection gâchette
        // hors écran, appui du bouton reload physique (mapping clic droit / geste
        // shake) — même si le Off-Screen Reload est désactivé. Les motifs simulent
        // un rechargement d'arme (style « crique-crique » mécanique par défaut).
        // L'intensité (0-100) met à l'échelle les impulsions ON (le moteur de la
        // Wiimote est binaire : l'intensité passe par la largeur d'impulsion).
        // Robuste par conception : AUCUN état verrouillé — chaque chemin se termine
        // par la vibration arrêtée et le drapeau libéré.)
        // ====================================================================

        /// <summary>
        /// EN: [V55y/V55z] Build the rumble pattern for a style, with intensity applied.
        /// Style 0 = "Ratchet": short mechanical clicks then a chunk (weapon reload feel).
        /// Style 1 = Short: single pulse. Style 2 = Long: single continuous pulse.
        /// Style 3 = Custom: N tics of (OnMs, OffMs) configured in Options > Gestures.
        /// Intensity (0-100) maps to a 0.25x..3x multiplier on the ON pulses, so the whole
        /// slider range is perceptible and 100% is clearly the maximum (the Wiimote motor
        /// is binary: intensity is achieved via pulse width).
        /// Every pattern ends with an OFF step so the sequence always finishes stopped.
        /// FR: [V55y/V55z] Construit le motif de vibration pour un style, intensité appliquée.
        /// Style 0 = « Ratchet » : petits clics mécaniques puis une taloche (sensation de
        /// rechargement d'arme). Style 1 = Court : impulsion unique. Style 2 = Long :
        /// impulsion continue. Style 3 = Personnalisé : N tics (OnMs, OffMs) configurés
        /// dans Options > Gestures. L'intensité (0-100) correspond à un multiplicateur
        /// 0,25×..3× sur les impulsions ON : toute la plage du curseur est perceptible et
        /// 100 % est clairement le maximum (le moteur de la Wiimote est binaire :
        /// l'intensité passe par la largeur d'impulsion). Tout motif se termine par une
        /// étape OFF pour finir arrêté.
        /// </summary>
        private static int[] BuildReloadRumblePattern(int style, int intensity)
        {
            // [V55z] Intensity multiplier: 0% -> 0.25x, 50% -> 1.625x, 100% -> 3x
            // (EN/FR: Multiplicateur d'intensité : 0% -> 0,25×, 50% -> 1,625×, 100% -> 3×)
            int clampedIntensity = Math.Min(100, Math.Max(0, intensity));
            double mult = 0.25 + (2.75 * clampedIntensity / 100.0);

            int[] raw;
            switch (style)
            {
                case 1:
                    raw = new int[] { 120, 60 };   // Short: single pulse (EN/FR: Court : impulsion unique)
                    break;
                case 2:
                    raw = new int[] { 420, 60 };   // Long: continuous (EN/FR: Long : continue)
                    break;
                case 3:
                {
                    // [V55z] Custom: N tics of (OnMs, OffMs) from Options > Gestures
                    // (EN/FR: Personnalisé : N tics (OnMs, OffMs) depuis Options > Gestures)
                    int ticks = Math.Min(10, Math.Max(1, Options.Instance.ReloadRumbleCustomTicks));
                    int onMs = Math.Min(500, Math.Max(10, Options.Instance.ReloadRumbleCustomOnMs));
                    int offMs = Math.Min(500, Math.Max(10, Options.Instance.ReloadRumbleCustomOffMs));
                    System.Collections.Generic.List<int> custom = new System.Collections.Generic.List<int>();
                    for (int t = 0; t < ticks; t++)
                    {
                        custom.Add(onMs);
                        custom.Add(offMs);
                    }
                    raw = custom.ToArray();
                    break;
                }
                default:
                    // 0 = Ratchet: click-click-click... CHNK (mechanical reload)
                    // (EN/FR: Ratchet : clic-clic-clic... TCHAK - rechargement mécanique)
                    raw = new int[] { 45, 55, 45, 55, 45, 55, 110, 70, 60, 90 };
                    break;
            }

            // EN/FR: Intensity scales the ON pulses (even indexes); 15ms floor keeps it perceptible
            int[] pattern = new int[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                if (i % 2 == 0)
                {
                    int on = (int)(raw[i] * mult);
                    pattern[i] = Math.Max(15, on);
                }
                else
                {
                    pattern[i] = raw[i];
                }
            }
            return pattern;
        }

        /// <summary>
        /// EN: [V55y] Start playing a reload rumble pattern (no overlap: ignored while
        /// already playing). Takes over the weapon rumble if one is running so the two
        /// never fight over the motor.
        /// FR: [V55y] Démarre la lecture d'un motif de vibration rechargement (pas de
        /// chevauchement : ignoré pendant une lecture en cours). Prend la main sur la
        /// vibration d'arme si une est en cours pour qu'elles ne se disputent jamais le moteur.
        /// </summary>
        private void PlayReloadRumblePattern(bool enabled, int intensity, int style)
        {
            if (!enabled) return;
            if (intensity <= 0) return;
            if (_isReloadRumbling) return; // Already playing (EN/FR: Lecture en cours)

            _reloadRumblePattern = BuildReloadRumblePattern(style, intensity);
            if (_reloadRumblePattern == null || _reloadRumblePattern.Length == 0) return;

            // EN/FR: Take over the weapon rumble if it is running (avoid SetRumble fights)
            if (_isRumbling) StopRumble();

            _reloadRumbleStepIdx = 0;
            _isReloadRumbling = true;
            _reloadRumbleTimer?.Change(0, Timeout.Infinite); // Start on the next tick
        }

        /// <summary>
        /// EN: [V55y] Reload rumble sequencer step: alternates the motor ON/OFF following
        /// the pattern. Always terminates cleanly (flag cleared, motor stopped).
        /// FR: [V55y] Étape du séquenceur : alterne le moteur ON/OFF selon le motif.
        /// Se termine toujours proprement (drapeau libéré, moteur arrêté).
        /// </summary>
        private void ReloadRumbleStepCallback(object state)
        {
            try
            {
                if (!_isReloadRumbling) return;

                int[] pattern = _reloadRumblePattern;
                if (pattern == null || _reloadRumbleStepIdx >= pattern.Length)
                {
                    FinishReloadRumble();
                    return;
                }

                bool on = (_reloadRumbleStepIdx % 2 == 0);
                int ms = pattern[_reloadRumbleStepIdx];
                _reloadRumbleStepIdx++;

                try
                {
                    if (Wiimote != null && Wiimote.IsConnected) Wiimote.SetRumble(on);
                }
                catch { }

                _reloadRumbleTimer?.Change(Math.Max(10, ms), Timeout.Infinite);
            }
            catch
            {
                FinishReloadRumble();
            }
        }

        /// <summary>
        /// EN: [V55y] Stop the reload rumble and clear its state (never latches).
        /// FR: [V55y] Arrête la vibration rechargement et libère son état (jamais verrouillé).
        /// </summary>
        private void FinishReloadRumble()
        {
            _isReloadRumbling = false;
            _reloadRumbleStepIdx = 0;
            try
            {
                if (Wiimote != null && Wiimote.IsConnected) Wiimote.SetRumble(false);
            }
            catch { }
        }

        /// <summary>
        /// EN: [V55y] Effective reload rumble state (mouse profiles): per-profile override
        /// (-1 = follow Options > Gestures global).
        /// FR: [V55y] État effectif vibration rechargement (profils souris) : override par
        /// profil (-1 = suivre le global Options > Gestures).
        /// </summary>
        private bool ResolveReloadRumbleEnabled()
        {
            if (_playerMappings != null)
            {
                if (_playerMappings.ReloadRumbleOverride == 1) return true;
                if (_playerMappings.ReloadRumbleOverride == 0) return false;
            }
            return Options.Instance.ReloadRumbleEnabled;
        }

        private int ResolveReloadRumbleIntensity()
        {
            int v = (_playerMappings != null && _playerMappings.ReloadRumbleIntensityOverride >= 0)
                ? _playerMappings.ReloadRumbleIntensityOverride
                : Options.Instance.ReloadRumbleIntensity;
            return Math.Min(100, Math.Max(0, v));
        }

        private int ResolveReloadRumbleStyle()
        {
            int v = (_playerMappings != null && _playerMappings.ReloadRumbleStyleOverride >= 0)
                ? _playerMappings.ReloadRumbleStyleOverride
                : Options.Instance.ReloadRumbleStyle;
            // [V55z] 0=Ratchet, 1=Short, 2=Long, 3=Custom
            return (v >= 0 && v <= 3) ? v : 0;
        }

        /// <summary>
        /// EN: [V55y] Trigger the reload rumble (mouse mode: resolves the profile overrides).
        /// FR: [V55y] Déclenche la vibration rechargement (mode souris : résout les overrides profil).
        /// </summary>
        private void TriggerReloadRumble()
        {
            PlayReloadRumblePattern(ResolveReloadRumbleEnabled(), ResolveReloadRumbleIntensity(), ResolveReloadRumbleStyle());
        }

        /// <summary>
        /// EN: [V55y] Trigger the reload rumble (GamePad mode: resolves the GamePad profile
        /// overrides, falling back to the global options).
        /// FR: [V55y] Déclenche la vibration rechargement (mode GamePad : résout les overrides
        /// du profil GamePad, à défaut les options globales).
        /// </summary>
        private void TriggerReloadRumble(GamePadMappings mappings)
        {
            bool enabled = Options.Instance.ReloadRumbleEnabled;
            int intensity = Options.Instance.ReloadRumbleIntensity;
            int style = Options.Instance.ReloadRumbleStyle;

            if (mappings != null)
            {
                if (mappings.ReloadRumbleOverride == 1) enabled = true;
                else if (mappings.ReloadRumbleOverride == 0) enabled = false;
                if (mappings.ReloadRumbleIntensityOverride >= 0) intensity = mappings.ReloadRumbleIntensityOverride;
                if (mappings.ReloadRumbleStyleOverride >= 0) style = mappings.ReloadRumbleStyleOverride;
            }

            PlayReloadRumblePattern(enabled, Math.Min(100, Math.Max(0, intensity)), (style >= 0 && style <= 3) ? style : 0);
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
                        // [V57g] EN: Neutral mouse frame in UMDF2 so a held button cannot stay
                        //     stuck once the mouse frames stop (HmHost watchdog parity).
                        //     FR: Frame souris neutre en UMDF2 pour qu'un bouton tenu ne
                        //     reste pas bloqué quand les frames souris s'arrêtent (parité
                        //     watchdog HmHost).
                        (_virtualMouse as VirtualHmMouse)?.ResetAll();
                    }
                }

                // Check if user changed XInput mode at runtime
                // [V57g] EN: One unified re-init for every output path (VMulti Col06,
                //     ViGEm XInput, UMDF2 HIDMaestro) - also invoked directly by
                //     Program.SetGamePadOutputApi (modal swap / mapping checkbox).
                //     FR: Une seule réinit pour tous les chemins de sortie (Col06 vmulti,
                //     ViGEm XInput, HIDMaestro UMDF2) - aussi appelée directement par
                //     Program.SetGamePadOutputApi (bascule modale / case mapping).
                if (GamePadOutputApiMismatch(mappings.UseXInput))
                {
                    ReinitGamepadOutput();
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
                bool dpadActive = !_isOffsetAdjustmentActive && !UI.Modern.Forms.EsProfileTileDialog.IsOpen; // [V52] No double navigation while the tile modal is open (the wiimote DPad drives it directly)

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
                
                float nJoyX = 0, nJoyY = 0;
                bool nUp = false, nDown = false, nLeft = false, nRight = false;
                bool lnUp = false, lnDown = false, lnLeft = false, lnRight = false;

                if (hasNunchuk) 
                {
                    float nunSXOff = 0, nunSYOff = 0;
                    var stickCalib = Options.Instance.GetCalibration(Wiimote != null ? Wiimote.UniqueId : "");
                    if (stickCalib != null) { nunSXOff = stickCalib.NunStickXOffset; nunSYOff = stickCalib.NunStickYOffset; }
                    
                    nJoyX = (state.Nunchuk.Joystick.X - nunSXOff) * 2.0f;
                    nJoyY = (state.Nunchuk.Joystick.Y - nunSYOff) * 2.0f;
                    if (float.IsNaN(nJoyX) || float.IsInfinity(nJoyX)) nJoyX = 0f;
                    if (float.IsNaN(nJoyY) || float.IsInfinity(nJoyY)) nJoyY = 0f;
                    if (Math.Abs(nJoyX) < 0.25f) nJoyX = 0f;
                    if (Math.Abs(nJoyY) < 0.25f) nJoyY = 0f;
                    nJoyX = Math.Max(-1.0f, Math.Min(1.0f, nJoyX));
                    nJoyY = Math.Max(-1.0f, Math.Min(1.0f, nJoyY));

                    float lnJoyX = (_lastNunchukState.Joystick.X - nunSXOff) * 2.0f;
                    float lnJoyY = (_lastNunchukState.Joystick.Y - nunSYOff) * 2.0f;

                    nUp = (nJoyY > 0.5f);
                    nDown = (nJoyY < -0.5f);
                    nLeft = (nJoyX < -0.5f);
                    nRight = (nJoyX > 0.5f);
                    lnUp = (lnJoyY > 0.5f);
                    lnDown = (lnJoyY < -0.5f);
                    lnLeft = (lnJoyX < -0.5f);
                    lnRight = (lnJoyX > 0.5f);

                    execHybrid(mappings.NunchukCHybrid, state.Nunchuk.C, _lastNunchukState.C, "NunchukC");
                    execHybrid(mappings.NunchukZHybrid, state.Nunchuk.Z, _lastNunchukState.Z, "NunchukZ");
                    execHybrid(mappings.NunchukUpHybrid, nUp, lnUp, "NunJoyUp");
                    execHybrid(mappings.NunchukDownHybrid, nDown, lnDown, "NunJoyDown");
                    execHybrid(mappings.NunchukLeftHybrid, nLeft, lnLeft, "NunJoyLeft");
                    execHybrid(mappings.NunchukRightHybrid, nRight, lnRight, "NunJoyRight");
                }

                // EN: Gesture Logic (Shake Reload, Grenade) - also runs in GamePad/Hybrid mode
                // FR: Logique des gestes (Shake Reload, Grenade) - s'exécute aussi en mode GamePad/Hybride
                if (CheckShake(state)) _gestureRightClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;
                if (CheckGrenadeGesture(state)) _gestureMiddleClickFrameCount = GESTURE_CLICK_DURATION_FRAMES;

                if (_gestureRightClickFrameCount > 0) { hRight = true; _gestureRightClickFrameCount--; }
                if (_gestureMiddleClickFrameCount > 0) { hMiddle = true; _gestureMiddleClickFrameCount--; }

                _lastHybridActive = isHybridActive;

                // Regular GamePad Buttons (disabled during hybrid)
                // [V56c] While the tile modal is open, the wiimote A/B drive the modal
                // DIRECTLY (native wiring: A = validate, B = back). Suppress them on the
                // virtual gamepad too, so a ViGEm (XInput) or VMulti gamepad polled by the
                // modal never double-activates. Physical XInput controllers are unaffected
                // (they are not driven by the wiimote).
                // (EN/FR: Pendant que la modale tuiles est ouverte, les A/B de la wiimote
                // pilotent la modale DIRECTEMENT (câblage natif : A = valider, B = retour).
                // Les supprimer aussi sur le gamepad virtuel, pour qu'un gamepad ViGEm
                // (XInput) ou VMulti sondé par la modale ne double-actionne jamais.
                // Les manettes XInput physiques ne sont pas affectées (elles ne sont pas
                // pilotées par la wiimote).)
                bool modalNavActive = UI.Modern.Forms.EsProfileTileDialog.IsOpen;

                _virtualGamepad.SetButton(mappings.WiiA, !isHybridActive && state.Buttons.A && !modalNavActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A"));
                _virtualGamepad.SetButton(mappings.WiiB, !isHybridActive && state.Buttons.B && !modalNavActive && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B"));
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

                    _virtualGamepad.SetButton(mappings.NunchukUp, !isHybridActive && nUp && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp"));
                    _virtualGamepad.SetButton(mappings.NunchukDown, !isHybridActive && nDown && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown"));
                    _virtualGamepad.SetButton(mappings.NunchukLeft, !isHybridActive && nLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft"));
                    _virtualGamepad.SetButton(mappings.NunchukRight, !isHybridActive && nRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight"));

                    // --- Nunchuk Joystick Axis / Dpad ---
                    if (mappings.NunchukJoystickAxis != GamePadAxis.None)
                    {
                        if (mappings.NunchukJoystickAxis == GamePadAxis.Dpad)
                        {
                            bool dUp = nUp && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp");
                            bool dDown = nDown && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown");
                            bool dRight = nRight && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight");
                            bool dLeft = nLeft && !HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft");

                            _virtualGamepad.SetButton(GamePadButton.DPadUp, dUp);
                            _virtualGamepad.SetButton(GamePadButton.DPadDown, dDown);
                            _virtualGamepad.SetButton(GamePadButton.DPadLeft, dLeft);
                            _virtualGamepad.SetButton(GamePadButton.DPadRight, dRight);
                        }
                        else
                        {
                            float finalJoyX = nJoyX;
                            float finalJoyY = nJoyY;

                            if (nJoyX > 0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunRight")) finalJoyX = 0f;
                            if (nJoyX < -0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunLeft")) finalJoyX = 0f;
                            if (nJoyY > 0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunUp")) finalJoyY = 0f;
                            if (nJoyY < -0.3f && HotkeyManager.IsButtonConsumed(PlayerIndex, "NunDown")) finalJoyY = 0f;

                            _virtualGamepad.SetAxis(mappings.NunchukJoystickAxis, finalJoyX, -finalJoyY);
                        }
                    }
                }

                // [V55n] TC Cover GamePad (Time Crisis pedal hold/release)
                // Runs independently of Nunchuk presence (standard GunCon / Wiimote configuration).
                // (EN/FR: Planque TC GamePad : maintien/relâche de la pédale Time Crisis.
                // S'exécute indépendamment de la présence du Nunchuk - config GunCon standard.)
                if (mappings.TCCoverReload)
                {
                    bool tcOnScreen = scaledPos.HasValue;
                    if (tcOnScreen) _gpTcHasAimedOnce = true;

                    GamePadButton tcButton = ResolveTcGamePadButton(mappings);

                    if (_gpTcHasAimedOnce && tcOnScreen) // ON-screen = hold (exit cover)
                    {
                        if (tcButton != GamePadButton.None)
                            _virtualGamepad.SetButton(tcButton, true); // HOLD while aiming on-screen (EN/FR: MAINTENU en visant l'écran)

                        if (!_gpTcWasOnScreen)
                        {
                            _gpTcWasOnScreen = true;
                            SimpleLogger.Instance.Info($"[GamePad P{PlayerIndex}] TC Cover: ON-screen (exit cover) -> HOLD {tcButton} ({mappings.TCCoverButton})");
                        }
                    }
                    else if (_gpTcWasOnScreen)
                    {
                        _gpTcWasOnScreen = false;
                        // [V56] Entering cover (ON->OFF transition) = TC reload -> rumble
                        // (EN/FR: Entrée en planque (transition ON->OFF) = rechargement TC -> vibration)
                        TriggerReloadRumble(mappings);
                        SimpleLogger.Instance.Info($"[GamePad P{PlayerIndex}] TC Cover: OFF-screen (in cover) -> RELEASE {tcButton} ({mappings.TCCoverButton})");
                    }
                    // OFF-screen: physical state governs (button released unless physically held)
                    // (EN/FR: Hors écran : état physique prime - bouton relâché sauf si physiquement maintenu)
                }

                // [V55] Off-Screen Reload GamePad: 0=Off, 1=Trigger, 2=Auto
                // Sends a brief A-button press to reload when off-screen (works like mouse mode equivalent).
                // Runs independently of Nunchuk presence.
                // (EN/FR: Reload hors-écran GamePad : 0=Off, 1=Trigger, 2=Auto
                // Envoie une pression brève du bouton A pour recharger hors écran. S'exécute sans Nunchuk.)
                if (mappings.OffScreenReloadMode > 0 && !mappings.TCCoverReload)
                {
                    bool gpOnScreen = scaledPos.HasValue;
                    if (gpOnScreen)
                    {
                        _gpHasAimedOnce = true;
                        if (_gpWasOnScreen == false)
                        {
                            // Returned to screen: re-arm reload
                            // (EN/FR: Retour à l'écran : réarmer le reload)
                            _gpOffScreenReloadPerformed = false;
                        }
                        _gpWasOnScreen = true;
                    }
                    else if (_gpHasAimedOnce) // off-screen
                    {
                        _gpWasOnScreen = false;

                        if (mappings.OffScreenReloadMode == 2) // Auto
                        {
                            // One auto-reload when transitioning off-screen (cooldown 250ms)
                            // (EN/FR: Une recharge auto à la sortie de l'écran - cooldown 250ms)
                            if (!_gpOffScreenReloadPerformed &&
                                (DateTime.Now - _gpLastAutoReloadTime).TotalMilliseconds >= 250)
                            {
                                _gpOffScreenReloadPerformed = true;
                                _gpLastAutoReloadTime = DateTime.Now;
                                // Send A-button (reload) for 1 frame via the mapping
                                // (EN/FR: Envoyer bouton A (reload) pour 1 frame via le mapping)
                                GamePadButton reloadBtn = mappings.WiiA;
                                if (reloadBtn != GamePadButton.None)
                                    _virtualGamepad.SetButton(reloadBtn, true);
                                TriggerReloadRumble(mappings); // [V55y] Configurable reload rumble (EN/FR: Vibration recharge paramétrable)
                                SimpleLogger.Instance.Info($"[P{PlayerIndex}] GP Auto-reload fired (off-screen)");
                            }
                        }
                        else if (mappings.OffScreenReloadMode == 1) // Trigger
                        {
                            // Manual: press A (fire) off-screen to reload once
                            // (EN/FR: Manuel : appuyer sur A (tir) hors écran pour recharger une fois)
                            bool rawA = state.Buttons.A && !HotkeyManager.IsButtonConsumed(PlayerIndex, "A") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiA");
                            bool rawB = state.Buttons.B && !HotkeyManager.IsButtonConsumed(PlayerIndex, "B") && !HotkeyManager.IsButtonConsumed(PlayerIndex, "WiiB");
                            if ((rawA || rawB) && !_gpOffScreenReloadPerformed)
                            {
                                _gpOffScreenReloadPerformed = true;
                                GamePadButton reloadBtn = mappings.WiiA;
                                if (reloadBtn != GamePadButton.None)
                                    _virtualGamepad.SetButton(reloadBtn, true);
                                TriggerReloadRumble(mappings); // [V55y] Configurable reload rumble (EN/FR: Vibration recharge paramétrable)
                                SimpleLogger.Instance.Info($"[P{PlayerIndex}] GP Trigger-reload fired (off-screen)");
                            }
                        }
                    }
                }

                // [V56] PHYSICAL FIRE / RELOAD rumble buttons (GamePad mode)
                // - FireButton (default B): its ON-screen press triggers the weapon rumble
                //   (same feature as the mouse-mode trigger rumble, gated by the per-player
                //   weapon rumble option). OFF-screen presses are left to the off-screen
                //   reload logic (Trigger mode) which fires its own reload rumble.
                // - OffScreenReloadButton (default 2): its press triggers the reload rumble,
                //   ON-screen and OFF-screen, gated by the global Reload Rumble option and
                //   the per-profile override (works whether Off-Screen Reload is enabled or not).
                // (EN/FR: BOUTONS physiques TIR / RECHARGE (mode GamePad) :
                // - FireButton (défaut B) : son appui À l'écran déclenche la vibration d'arme
                //   (même fonction que la vibration de gâchette en mode souris, gated sur
                //   l'option de vibration d'arme par joueur). Les appuis HORS écran relèvent
                //   de la logique reload hors écran (mode Trigger) qui déclenche sa propre
                //   vibration de recharge.
                // - OffScreenReloadButton (défaut 2) : son appui déclenche la vibration de
                //   recharge, À l'écran comme HORS écran, gated sur l'option globale Reload
                //   Rumble et l'override par profil (fonctionne que le Off-Screen Reload
                //   soit activé ou non).)
                {
                    string gpFireId = string.IsNullOrEmpty(mappings.FireButton) ? "WiiB" : mappings.FireButton;
                    bool gpFireRaw = IsGamePadButtonPressed(gpFireId, state.Buttons, state.Nunchuk, hasNunchuk);

                    // [V56a] FULL PARITY with the mouse-mode trigger rumble: intensity and
                    // duration come from "Assign wiimotes" (TriggerWeaponRumble reads
                    // GetRumbleDurationMs/GetRumbleIntensity), and the CONTINUOUS repetition
                    // runs while the fire button is held ON-screen when AllowContinuousRumble
                    // is enabled (GetRumbleRepetitionMs), stopping on release.
                    // (EN/FR: PARITÉ COMPLÈTE avec la vibration de gâchette du mode souris :
                    // l'intensité et la durée viennent d'« Assign wiimotes » (TriggerWeaponRumble
                    // lit GetRumbleDurationMs/GetRumbleIntensity), et la répétition CONTINUE
                    // tourne tant que le bouton de tir est maintenu À l'écran quand
                    // AllowContinuousRumble est activé (GetRumbleRepetitionMs), arrêt au relâchement.)
                    bool gpFireRumbleActive = gpFireRaw && scaledPos.HasValue && Options.Instance.GetEnableWeaponRumble(PlayerIndex);
                    _gpFireHeldRumble = gpFireRumbleActive;

                    if (gpFireRaw && !_gpFireWasPressed && gpFireRumbleActive)
                    {
                        TriggerWeaponRumble(); // [V56] On-screen fire press -> weapon rumble (EN/FR: Appui tir à l'écran -> vibration d'arme)

                        if (Options.Instance.GetAllowContinuousRumble(PlayerIndex))
                        {
                            int intervalMs = Options.Instance.GetRumbleRepetitionMs(PlayerIndex);
                            _rumbleTimer?.Change(intervalMs, intervalMs);
                        }
                    }
                    else if (!gpFireRaw && _gpFireWasPressed)
                    {
                        _rumbleTimer?.Change(Timeout.Infinite, Timeout.Infinite); // [V56a] Release -> stop the continuous rumble
                    }
                    _gpFireWasPressed = gpFireRaw;

                    string gpReloadId = string.IsNullOrEmpty(mappings.OffScreenReloadButton) ? "Wii2" : mappings.OffScreenReloadButton;
                    bool gpReloadRaw = IsGamePadButtonPressed(gpReloadId, state.Buttons, state.Nunchuk, hasNunchuk);
                    if (gpReloadRaw && !_gpReloadBtnWasPressed)
                    {
                        TriggerReloadRumble(mappings); // [V56] Physical reload button press -> reload rumble (EN/FR: Appui bouton reload physique -> vibration recharge)
                    }
                    _gpReloadBtnWasPressed = gpReloadRaw;
                }

                // [V55] TC Bi-directional Pedal: DPad Left/Right hold a direction while aiming on-screen
                // [V55] TC Bi-directional Pedal: Two pedals configured by user (default Left=DPadLeft, Right=DPadRight)
                // Runs independently of Nunchuk presence.
                // (EN/FR: Pédale TC bi-directionnelle : deux pédales configurées, défaut G=DPadLeft, D=DPadRight.
                // S'exécute indépendamment de la présence du Nunchuk.)
                if (mappings.TCBiPedal && !mappings.TCCoverReload)
                {
                    bool biOnScreen = scaledPos.HasValue;
                    string leftBtnId = string.IsNullOrEmpty(mappings.TCBiPedalLeftButton) ? "WiiLeft" : mappings.TCBiPedalLeftButton;
                    string rightBtnId = string.IsNullOrEmpty(mappings.TCBiPedalRightButton) ? "WiiRight" : mappings.TCBiPedalRightButton;

                    bool dLeft  = IsPhysicalButtonPressed(leftBtnId, state.Buttons, state.Nunchuk, hasNunchuk);
                    bool dRight = IsPhysicalButtonPressed(rightBtnId, state.Buttons, state.Nunchuk, hasNunchuk);

                    if (biOnScreen)
                    {
                        // On-screen: process new direction requests
                        // (EN/FR: À l'écran : traiter les nouvelles demandes de direction)
                        if (dLeft && !dRight)  _gpBiPedalActive = -1;  // Left pedal
                        if (dRight && !dLeft)  _gpBiPedalActive =  1;  // Right pedal
                    }
                    else
                    {
                        // [V56a] TC Bi-Pedal: entering cover (ON->OFF transition). The pedal input
                        // is RELEASED (= taking cover = TC reload) — vibrate exactly here, once
                        // per transition, same principle as the single TC cover.
                        // (EN/FR: Pédale TC bi-directionnelle : entrée en planque (transition
                        // ON->OFF). L'entrée pédale est RELÂCHÉE (= se planquer = rechargement
                        // TC) — vibrer exactement ici, une seule fois par transition, même
                        // principe que la planque TC sur un seul bouton.)
                        if (_gpBiPedalWasOnScreen)
                        {
                            TriggerReloadRumble(mappings);
                        }

                        // Off-screen: release without changing state
                        // (EN/FR: Hors écran : relâcher sans changer l'état)
                        _gpBiPedalActive = 0;
                    }

                    // Apply the held direction as DPad button state
                    // (EN/FR: Appliquer la direction tenue comme état du DPad)
                    _virtualGamepad.SetButton(GamePadButton.DPadLeft,  _gpBiPedalActive == -1);
                    _virtualGamepad.SetButton(GamePadButton.DPadRight, _gpBiPedalActive ==  1);

                    // [V56a] Track the on-screen state for the cover-enter transition detection
                    // (EN/FR: Suivre l'état à l'écran pour la détection de transition d'entrée en planque)
                    _gpBiPedalWasOnScreen = biOnScreen;
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

                    bool nAccUp = _lastAccelNunchukUp ? nMotY > nRelDZ : nMotY > nActDZ;
                    bool nAccDown = _lastAccelNunchukDown ? nMotY < -nRelDZ : nMotY < -nActDZ;
                    bool nAccLeft = _lastAccelNunchukLeft ? nMotX < -nRelDZ : nMotX < -nActDZ;
                    bool nAccRight = _lastAccelNunchukRight ? nMotX > nRelDZ : nMotX > nActDZ;

                    // EN: Ensure mutual exclusivity on the same axis (can't be Up and Down)
                    if (nAccUp && nAccDown) { nAccUp = false; nAccDown = false; }
                    if (nAccLeft && nAccRight) { nAccLeft = false; nAccRight = false; }

                    if (nAccUp) { applyMotionAction(mappings.AccelNunchukUp, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukUpHybrid, true, _lastAccelNunchukUp, "AccelNunchukUp"); }
                    else { execHybrid(mappings.AccelNunchukUpHybrid, false, _lastAccelNunchukUp, "AccelNunchukUp"); }

                    if (nAccDown) { applyMotionAction(mappings.AccelNunchukDown, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukDownHybrid, true, _lastAccelNunchukDown, "AccelNunchukDown"); }
                    else { execHybrid(mappings.AccelNunchukDownHybrid, false, _lastAccelNunchukDown, "AccelNunchukDown"); }

                    if (nAccLeft) { applyMotionAction(mappings.AccelNunchukLeft, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukLeftHybrid, true, _lastAccelNunchukLeft, "AccelNunchukLeft"); }
                    else { execHybrid(mappings.AccelNunchukLeftHybrid, false, _lastAccelNunchukLeft, "AccelNunchukLeft"); }

                    if (nAccRight) { applyMotionAction(mappings.AccelNunchukRight, nMotX * 3f, nMotY * 3f, nMotZ * 3f, mappings.AccelNunchukSensitivity); execHybrid(mappings.AccelNunchukRightHybrid, true, _lastAccelNunchukRight, "AccelNunchukRight"); }
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
        /// [V57g] EN: True when the live virtual gamepad does not match the requested
        /// output (DInput/XInput) or the input mode's expected backend (HmGamepad in
        /// RawInputUmdf, ViGEm/VMultiGamepad otherwise). Shared by the runtime
        /// detection and the explicit swap so every path agrees on "needs re-init".
        /// FR: Vrai quand la manette virtuelle vivante ne correspond pas à la sortie
        /// demandée (DInput/XInput) ni au backend attendu du mode d'entrée (HmGamepad
        /// en RawInputUmdf, ViGEm/VMultiGamepad sinon). Partagé par la détection
        /// runtime et la bascule explicite pour que tous les chemins s'accordent sur
        /// « réinit nécessaire ».
        /// </summary>
        private bool GamePadOutputApiMismatch(bool useXInput)
        {
            bool currentIsXInput = (_virtualGamepad is ViGEmGamepad) ||
                                   (_virtualGamepad is HmGamepad hmGp && hmGp.UsesXInput);
            bool umdf2 = Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf;
            bool currentIsUmdf2 = _virtualGamepad is HmGamepad;
            return useXInput != currentIsXInput || umdf2 != currentIsUmdf2;
        }

        /// <summary>
        /// [V57g] EN: Re-initialize the virtual gamepad output after a DInput/XInput
        ///     change (mapping checkbox, modal swap button, runtime profile change) or an
        ///     input-mode change. Single implementation for every backend: VMulti Col06,
        ///     ViGEm XInput, and UMDF2 HIDMaestro (HmHost creates or swaps the device at
        ///     a stable "GPn" identity). Safe to call on the wiimote report thread.
        ///     FR: Réinitialise la sortie manette virtuelle après un changement
        ///     DInput/XInput (case mapping, bouton de bascule de la modale, changement de
        ///     profil runtime) ou un changement de mode d'entrée. Implémentation unique
        ///     pour tous les backends : Col06 vmulti, ViGEm XInput et UMDF2 HIDMaestro
        ///     (HmHost crée ou échange le device à l'identité stable « GPn »). Appelable
        ///     en sécurité sur le thread de rapports wiimote.
        /// </summary>
        public void ReinitGamepadOutput()
        {
            try
            {
                GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(PlayerIndex);
                bool useXInput = mappings != null && mappings.UseXInput;
                bool umdf2 = Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf;

                if (!GamePadOutputApiMismatch(useXInput) && _virtualGamepad != null)
                    return;

                SimpleLogger.Instance.Info($"[GamePad P{PlayerIndex}] Output mode changed. Re-initializing virtual gamepad ({(useXInput ? "XInput" : "DInput")}{(umdf2 ? " / UMDF2-HIDMaestro" : "")})...");
                if (_virtualGamepad != null)
                {
                    _virtualGamepad.Disconnect();
                    _virtualGamepad.Dispose();
                }

                if (umdf2)
                {
                    _virtualGamepad = new HmGamepad(PlayerIndex, useXInput);
                    WiimoteGun.ServiceClient.EnableGamepad(PlayerIndex, useXInput ? "XINPUT" : "DINPUT");
                }
                else if (useXInput)
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
                // (EN/FR: Laisser un peu de temps à la connexion avant d'envoyer des rapports pour ne pas perdre le premier état)
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[GamePad P{PlayerIndex}] ReinitGamepadOutput failed: {ex.Message}");
            }
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

            // [FIX V23b] Allow prediction at ANY rate > 0 (previously short-circuited at <= 110).
            // With the V2 TR streaming at ~83Hz, a rate of 100 makes the prediction FILL the 12ms gaps
            // between real reports without upsampling beyond the native rate.
            // FR: Autoriser la prédiction à TOUT taux > 0 (précédemment court-circuitée à <= 110).
            // Avec la V2 TR qui stream à ~83Hz, un taux de 100 fait combler par la prédiction les gaps
            // de 12ms entre rapports réels sans dépasser le taux natif.
            // [V28] Rate is resolved per Wiimote model (V2 TR column in options).
            // (EN/FR: Le taux est résolu selon le modèle de Wiimote.)
            if (ActiveVirtualPollingRate <= 0) return;

            DateTime now = GetNow();
            double msSinceLastAny = (now - _lastAnyReportTime).TotalMilliseconds;

            double targetIntervalMs = 1000.0 / Math.Max(1, ActiveVirtualPollingRate);

            // Rate limiting: keep the output on the configured uniform grid.
            // (EN/FR: Limitation de débit : garder la sortie sur la grille uniforme configurée.)
            if (msSinceLastAny >= 0 && msSinceLastAny < (targetIntervalMs * 0.85)) return;

            double nowMs = Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

            // [FIX V25] If burst cycles stopped recurring, drop the retiming mode.
            // (EN/FR: Si les cycles de salves ne se reproduisent plus, abandonner le re-chronométrage.)
            if (_burstPeriodEmaMs > 0.0 && _lastBurstBoundaryMs >= 0.0 && nowMs - _lastBurstBoundaryMs > 500.0)
                _burstPeriodEmaMs = 0.0;

            int outX = 0, outY = 0;
            bool havePosition = false;

            // [FIX V25] Retiming path: replay the buffered real positions, interpolated at
            // (now - replayDelay). Converts 2-4 report batches every ~30-40ms into a perfectly
            // uniform output stream at the configured rate. Only engages when burst cycles
            // are actually recurring (measured period 20-100ms), i.e. on affected links.
            // FR: Chemin de re-chronométrage : rejouer les positions réelles du tampon, interpolées
            // à (now - délai). Convertit les salves de 2-4 rapports tous les ~30-40ms en flux de
            // sortie parfaitement uniforme au taux configuré. Ne s'active que si des cycles de
            // salves se reproduisent réellement (période mesurée 20-100ms).
            if (_burstPeriodEmaMs > 0.0 && _burstPeriodEmaMs < 100.0)
            {
                double targetMs = nowMs - _replayDelayMs;
                lock (_replayLock)
                {
                    int n = _replayTimeMs.Count;
                    if (n >= 2 && nowMs - _replayTimeMs[n - 1] < 80.0)
                    {
                        if (targetMs <= _replayTimeMs[0])
                        {
                            outX = _replayX[0];
                            outY = _replayY[0];
                            havePosition = true;
                        }
                        else
                        {
                            for (int i = n - 2; i >= 0; i--)
                            {
                                if (_replayTimeMs[i] <= targetMs)
                                {
                                    double span = _replayTimeMs[i + 1] - _replayTimeMs[i];
                                    double f = span > 0.0 ? (targetMs - _replayTimeMs[i]) / span : 0.0;
                                    outX = (int)(_replayX[i] + (_replayX[i + 1] - _replayX[i]) * f);
                                    outY = (int)(_replayY[i] + (_replayY[i + 1] - _replayY[i]) * f);
                                    havePosition = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (!havePosition)
            {
                // Fallback: plain velocity extrapolation (V23a behavior), used while the
                // burst period is not yet measured or on links without burst aggregation.
                // (EN/FR: Repli : extrapolation de vélocité simple (V23a), utilisée tant que la
                // période des salves n'est pas mesurée ou sur les liens sans agrégation.)
                double msSinceLastReal = (now - _lastProcessingTime).TotalMilliseconds;
                if (!(msSinceLastReal > 1.0 && msSinceLastReal < 20.0)) return;

                // [FIX V23a] Scale the prediction by the MEASURED average report interval instead of a
                // hardcoded 10ms. V2 TR Wiimotes stream at ~83Hz (12ms gaps), so the old /10.0 factor
                // overshot every tick by ~20% and made stuttering worse at 110-300Hz.
                // FR: Calibrer la prédiction sur l'intervalle moyen MESURÉ au lieu d'un 10ms codé en dur.
                // Les Wiimotes V2 TR streament à ~83Hz (gaps de 12ms), donc l'ancien facteur /10.0
                // dépassait chaque tick de ~20% et aggravait le bégaiement à 110-300Hz.
                float frameFactor = (float)(msSinceLastReal / Math.Max(1.0, _avgReportIntervalMs));

                // [FIX V23a] Use an EMA-smoothed velocity for prediction: the raw per-report IR delta is
                // noisy and made the predicted position oscillate ahead/back of the real one.
                // FR: Utiliser une vélocité lissée EMA pour la prédiction : le delta IR brut par rapport
                // est bruité et faisait osciller la position prédite devant/derrière la position réelle.
                _smoothPredVelX = (0.5f * _lastVelX_Diag) + (0.5f * _smoothPredVelX);
                _smoothPredVelY = (0.5f * _lastVelY_Diag) + (0.5f * _smoothPredVelY);

                outX = (int)(_lastX_Raw + _smoothPredVelX * frameFactor);
                outY = (int)(_lastY_Raw + _smoothPredVelY * frameFactor);
            }

            outX = Math.Max(0, Math.Min(65535, outX));
            outY = Math.Max(0, Math.Min(65535, outY));

            // [DIAG] Output-side rate: what the cursor ACTUALLY receives after V25 retiming.
            // (EN/FR: Taux côté sortie : ce que le curseur reçoit RÉELLEMENT après le
            // re-chronométrage V25. replay% élevé + Hz réguliers = de-jitter engagé.)
            _diagOutEmissions++;
            if (havePosition && _burstPeriodEmaMs > 0.0 && _burstPeriodEmaMs < 100.0) _diagOutReplay++;
            else _diagOutFallback++;
            if (_diagOutWindowStartMs < 0.0) _diagOutWindowStartMs = nowMs;
            else if (nowMs - _diagOutWindowStartMs >= 5000.0)
            {
                double outRate = _diagOutEmissions / ((nowMs - _diagOutWindowStartMs) / 1000.0);
                long total = _diagOutReplay + _diagOutFallback;
                int replayPct = total > 0 ? (int)(_diagOutReplay * 100 / total) : 0;
                SimpleLogger.Instance.Info(string.Format(
                    "[P{0}] [DIAG] Output: {1:F0} Hz, replay: {2}%, delay: {3:F0}ms, burstEMA: {4:F0}ms",
                    PlayerIndex, outRate, replayPct, _replayDelayMs, _burstPeriodEmaMs));
                _diagOutEmissions = 0;
                _diagOutReplay = 0;
                _diagOutFallback = 0;
                _diagOutWindowStartMs = nowMs;
            }

            _virtualMouse.UpdateMouse(outX, outY, _lastLeft_Raw, _lastRight_Raw, _lastMiddle_Raw, true);
            _lastAnyReportTime = now;
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
