using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using WiimoteLib.Geometry;
using WiimoteLib.DataTypes;
using WiimoteGun.UI.Calibrate;

namespace WiimoteGun
{
    public class ScreenPositionCalculator
    {
        public ScreenPositionCalculator(int screenIndex, int playerIndex)
        {
            _screenIndex = screenIndex;
            _playerIndex = playerIndex;
            _ledLayout = Options.Instance.LEDLayout; // Load LED layout type (EN/FR: Charger le type de layout LED)

            // Load player-specific calibration - 4-POINT FORMAT (EN/FR: Charger calibration spécifique au joueur - FORMAT 4-POINTS)
            float[] calibration = Options.Instance.GetCalibrationForPlayer(playerIndex);
            
            // Array format: [0]=TL.X, [1]=TL.Y, [2]=TR.X, [3]=TR.Y, [4]=BR.X, [5]=BR.Y, [6]=BL.X, [7]=BL.Y
            if (calibration != null && calibration.Length >= 8 && calibration[0] != -1)
            {
                _topLeftPt = new Point2F(calibration[0], calibration[1]);
                _topRightPt = new Point2F(calibration[2], calibration[3]);
                _bottomRightPt = new Point2F(calibration[4], calibration[5]);
                _bottomLeftPt = new Point2F(calibration[6], calibration[7]);
            }

            // [V59-FIXES] EN: Restore the PnP reference state persisted with the last
            //     calibration (effective focal + reference distance), so the 3D
            //     corrections keep working after an app restart without recalibrating.
            //     FR: Restaure l'état de référence PnP persisté avec la dernière
            //     calibration (focale effective + distance de référence), pour que les
            //     corrections 3D continuent de fonctionner après un relancement sans
            //     recalibrer.
            if (EffLayout == LEDLayoutType.WiimoteBarPnP)
            {
                float savedFocal = Options.Instance.GetPnpEffectiveFocal(playerIndex);
                float savedDist = Options.Instance.GetPnpCalibrationDistance(playerIndex);
                if (savedFocal > 0 && savedDist > 0 &&
                    _topLeftPt.HasValue && _topRightPt.HasValue &&
                    _bottomRightPt.HasValue && _bottomLeftPt.HasValue)
                {
                    _pnpFocalX = savedFocal;
                    _pnpRefDistance = savedDist;
                    _pnpRefSeparation = (PnpLedSpacingMM * savedFocal) / (savedDist * 1000f);
                    _pnpIROffsetM = Options.Instance.GetIROffsetMM(playerIndex) / 1000f;
                    SimpleLogger.Instance.Info(string.Format(
                        "[V59] P{0}: PnP Hybrid reference restored — focal={1:F3}, calDist={2:F2}m",
                        _playerIndex, savedFocal, savedDist));
                }
            }

            // Load Gun4IR or 4-Corners calibration
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
            {
                _gun4irPoints = Options.Instance.GetGun4IRCalibration(playerIndex);
            }
            else if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
            {
                _gun4irPoints = Options.Instance.GetTwoWiimoteBarCalibration(playerIndex);

                // [V59-FIX-13] EN: Restore the cross-calibration data (bar midpoints,
                //     centers, pair separation) and REBUILD the homography — the user
                //     should NOT have to recalibrate every restart (user request).
                //     FR: Restaure les données de calibration croix (milieux de barres,
                //     centres, séparation de paire) et RECONSTRUIT l'homographie —
                //     l'utilisateur ne doit PAS recalibrer à chaque relance (demande user).
                float[] twbData = Options.Instance.GetTwbCalData(playerIndex);
                if (twbData != null && twbData.Length >= 31 && twbData[0] != 0f)
                {
                    int idx = 0;
                    for (int i = 0; i < 5; i++) { _twbCalTopMid[i] = new Point2F(twbData[idx++], twbData[idx++]); }
                    for (int i = 0; i < 5; i++) { _twbCalBotMid[i] = new Point2F(twbData[idx++], twbData[idx++]); }
                    for (int i = 0; i < 5; i++) { _twbCalCenter[i] = new Point2F(twbData[idx++], twbData[idx++]); }
                    _twbCalPairSep = twbData[idx];
                    _twbSepSmoothedRatio = 1f;
                    _twbLastTopMid = _twbCalTopMid[0].Value;
                    _twbLastBotMid = _twbCalBotMid[0].Value;

                    // Rebuild the least-squares homography from the restored 5 clicks
                    var hSrc = new Point2F[] {
                        _twbCalCenter[0].Value, _twbCalCenter[1].Value,
                        _twbCalCenter[2].Value, _twbCalCenter[3].Value,
                        _twbCalCenter[4].Value };
                    var hDst = new Point2F[] {
                        new Point2F(0.5f, 0.5f), new Point2F(0.5f, 0f),
                        new Point2F(0.5f, 1f), new Point2F(0f, 0.5f),
                        new Point2F(1f, 0.5f) };
                    _twbHomography = ComputeHomographyLeastSquares(hSrc, hDst);

                    if (_twbHomography != null)
                    {
                        SimpleLogger.Instance.Info(string.Format(
                            "[V59] P{0}: TWB cross calibration restored — pair sep={1:F3}, homography rebuilt",
                            _playerIndex, _twbCalPairSep));
                    }
                }

                // Initialize dimensions from calibration if available (EN/FR: Initialiser dimensions depuis calibration si dispo)
                if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                    _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                {
                    // TL=1, TR=2, BR=3, BL=4 (Indices based on 5-step calibration)
                    // Width = Average of Top and Bottom widths
                    float topW = Math.Abs(_gun4irPoints[2].Value.X - _gun4irPoints[1].Value.X);
                    float botW = Math.Abs(_gun4irPoints[3].Value.X - _gun4irPoints[4].Value.X);
                    _observedWidth = (topW + botW) / 2.0f;
                    
                    // Height = Average of Left and Right heights
                    float leftH = Math.Abs(_gun4irPoints[4].Value.Y - _gun4irPoints[1].Value.Y);
                    float rightH = Math.Abs(_gun4irPoints[3].Value.Y - _gun4irPoints[2].Value.Y);
                    _observedHeight = (leftH + rightH) / 2.0f;
                }
            }
            else if (_ledLayout == LEDLayoutType.FourCorners)
            {
                _gun4irPoints = Options.Instance.GetFourCornersCalibration(playerIndex);
                
                // Initialize dimensions from calibration if available (EN/FR: Initialiser dimensions depuis calibration si dispo)
                if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                    _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                {
                    // TL=1, TR=2, BR=3, BL=4 (Indices based on 5-step calibration)
                    // Width = Average of Top and Bottom widths
                    float topW = Math.Abs(_gun4irPoints[2].Value.X - _gun4irPoints[1].Value.X);
                    float botW = Math.Abs(_gun4irPoints[3].Value.X - _gun4irPoints[4].Value.X);
                    _observedWidth = (topW + botW) / 2.0f;
                    
                    // Height = Average of Left and Right heights
                    float leftH = Math.Abs(_gun4irPoints[4].Value.Y - _gun4irPoints[1].Value.Y);
                    float rightH = Math.Abs(_gun4irPoints[3].Value.Y - _gun4irPoints[2].Value.Y);
                    _observedHeight = (leftH + rightH) / 2.0f;
                }
            }

            // Load Dynamic Perspective mode setting (EN/FR: Charger paramètre mode Perspective Dynamique)
            UseDynamicPerspective = Options.Instance.GetUseDynamicPerspective(playerIndex);
        }

        private int _screenIndex;
        private int _playerIndex;
        private LEDLayoutType _ledLayout;

        private Point2F _firstSensorPos;
        private Point2F _secondSensorPos;
        private Point2F _midSensorPos;
        // [V59-FIX-24] Continuous 1-LED delta tracking & reacquisition decay offset
        private bool _extrapolatingOneLed = false;
        private Point2F _lastSurvivingPos = new Point2F();
        private bool _rearmReacquisition = false;
        private Point2F _reacquisitionOffset = new Point2F(0f, 0f);
        private Point2F _lastOutputScreenPos = new Point2F(0.5f, 0.5f);

        // PER-INSTANCE calibration points (EN/FR: Points de calibration par instance)
        // CRITICAL: NOT static - each player needs their own calibration
        // (EN/FR: CRITIQUE : PAS static - chaque joueur a besoin de sa propre calibration)
        
        // 4-Point Calibration for WiimoteBar (improved precision on large screens)
        // (EN/FR: Calibration 4-points pour WiimoteBar - précision améliorée grands écrans)
        private Point2F? _topLeftPt;     // Point 1: Top-Left corner
        private Point2F? _topRightPt;    // Point 2: Top-Right corner
        private Point2F? _bottomRightPt; // Point 3: Bottom-Right corner
        private Point2F? _bottomLeftPt;  // Point 4: Bottom-Left corner
        
        // Gun4IR / 4 Corners Calibration Points (5 points: Center, Top, Right, Bottom, Left)
        private Point2F?[] _gun4irPoints = new Point2F?[5]; // 0=Center, 1=Top, 2=Right, 3=Bottom, 4=Left
        

        
        // OLD: private Dictionary<int, Point2F> _dynamicOffsets = new Dictionary<int, Point2F>();

        private Point2F _lastRawPoint; // Raw IR point for calibration (EN/FR: Point IR brut pour calibration))

        // Tracking robustness (EN/FR: Robustesse du tracking)
        private Point2F _lastValidCenter = new Point2F(0.5f, 0.5f); // Default center in normalized coords (0-1)
        private Point2F _lastSmoothedCenter = new Point2F(0.5f, 0.5f); // Smoothed output in normalized coords
        private float _maxObservedDiagonal = 0f;
        private float _observedHeight = 0f; // Learned height of the rectangle (EN/FR: Hauteur apprise du rectangle)
        private float _observedWidth = 0f;  // Learned width of the rectangle (EN/FR: Largeur apprise du rectangle)
        // v16: Double-T Tracking Fields (EN/FR: Champs Suivi Double-T)
        private Point2F _lastM1 = new Point2F(0.5f, 0.5f);
        private Point2F _lastM2 = new Point2F(0.5f, 0.5f);
        private bool _doubleTInitialized = false;
        private float _doubleTRatio = 0.5f;
        private Dictionary<int, Point2F> _lastFramePoints = new Dictionary<int, Point2F>();
        private bool _wasUsingRelativeTracking = false;
        private int _framesSinceTransition = 0;

        // Cached static homography matrix — avoids recomputing Gaussian elimination every frame
        // Invalidated on calibration change. Only used for static modes (not Dynamic).
        // (EN/FR: Cache matrice homographie statique — évite recalcul élimination Gauss chaque frame)
        private float[] _cachedStaticHomography = null;

        private CalibrateForm _calibrateForm;
        private CalibrationModeSelectionForm _modeSelectionForm; // Reference to mode selection form for cancellation via Home button
        private WiimoteGun.UI.Legacy.IRVisualizerForm _irVisualizer; // (EN/FR: Fenêtre visualisation IR)

        // Distance compensation for manual calibration modes (NOT dynamic/auto mode)
        // Stores the apparent IR target size at calibration time to detect proximity changes
        // (EN/FR: Compensation de distance pour modes calibration manuelle (PAS mode auto)
        // Stocke la taille IR apparente lors de la calibration pour détecter les changements de distance)
        private float _calibrationIRSpan = 0f; // IR span saved at calibration end (EN/FR: Taille IR sauvegardée en fin de calibration)
        private float _lastIRSpan        = 0f; // IR span measured at last frame (EN/FR: Taille IR mesurée à la dernière frame)
        private float _lastValidDistFactor = 1.0f; // Persistent scaling factor for 1-LED tracking (EN/FR: Facteur d'échelle persistant pour suivi 1 point)
        private List<float> _calibrationSpanSamples = new List<float>(); // IR span samples collected during calibration clicks (EN/FR: Échantillons de taille IR collectés lors des clics de calibration)
        private const float DistanceCompensationThreshold = 0.04f; // 4% threshold to avoid over-correcting noise (EN/FR: Seuil 4% pour éviter sur-correction du bruit)
        private const float DistanceFactorSmoothing = 0.15f; // Smoothing factor for distFactor (0-1, lower = smoother)

        // [V59] EN: Wiimote Bar PnP — experimental hybrid tracking state. The bilinear
        //     trapezoid mapping is the BASE (fisheye-compatible), PnP corrections
        //     (distance, roll, IR offset) are applied on top. Focal length is
        //     ESTIMATED during the 5-point calibration (never hard-coded) so both
        //     standard and fisheye/wide-angle lenses work.
        //     FR: Wiimote Bar PnP — état du tracking hybride expérimental. Le mapping
        //     trapézoïdal bilinéaire est la BASE (compatible fisheye), les corrections
        //     PnP (distance, roll, IR offset) s'appliquent par-dessus. La focale est
        //     ESTIMÉE pendant la calibration 5 points (jamais codée en dur) pour que
        //     les lentilles standard et fisheye/grand-angle fonctionnent.
        private const float PnpLedSpacingMM = 190f;
        // [V59-FIXES] EN: Factory focal in NORMALIZED camera units (IR positions are
        //     0-1, NOT raw pixels). A Wii IR camera covers ~45° horizontally over
        //     1024 raw pixels → focal = (1024/2)/tan(22.5°)/1024 ≈ 1.207. The previous
        //     value 2450 (pixel units) applied to normalized spans produced distances
        //     ~2000x too large: the [dist] display and the IR-offset correction were
        //     dead, and the distance validation divided by a zero reference (forced
        //     3-reject accept on every point).
        //     FR: Focale usine en unités caméra NORMALISÉES (positions IR 0-1, PAS des
        //     pixels bruts). La caméra IR Wii couvre ~45° sur 1024 pixels bruts →
        //     focale = (1024/2)/tan(22,5°)/1024 ≈ 1,207. L'ancienne valeur 2450 (pixels)
        //     appliquée aux spans normalisés donnait des distances ~2000x trop grandes :
        //     affichage [dist] et correction IR-offset morts, et validation distance
        //     divisant par une référence nulle (acceptation forcée après 3 refus).
        // [V59-FIX-28] Focale usine normalisée basée sur 38° mesuré : 0.5 / tan(19°) ≈ 1.452
        private const float PnpFactoryFocalNorm = 1.452f;
        private float _pnpFocalX = 0f;
        private float _pnpRefDistance = 0f;
        private float _pnpRefSeparation = 0f;
        private float _pnpLastSeparation = 0f;
        private float _pnpIROffsetM = 0.030f;
        private float _pnpSmoothedDistRatio = 1.0f;
        // [V59-FIXES] Live per-frame distance estimate (m): feeds the calibration form's
        // [dist] readout, the IR visualizer, and ApplyPnpCorrections.
        private float _pnpLastDistanceM = 0f;
        // [V59-FIX-3] EN: TWO-CLICK capture state (user request). Click 1 arms the
        //     point and starts per-frame sampling, click 2 confirms; the captured point
        //     is the MEDIAN of the samples gathered between the two clicks — removes
        //     trigger jerk without rejecting shaky users.
        //     FR: État de capture en DEUX CLICS (demande user). Le clic 1 arme le point
        //     et démarre l'échantillonnage par image, le clic 2 confirme ; le point
        //     capturé est la MÉDIANE des échantillons entre les 2 clics — supprime le
        //     sursaut de gâchette sans refuser les utilisateurs tremblants.
        private bool _pnpClickArmed = false;
        private readonly List<Point2F> _pnpClickSamples = new List<Point2F>();
        private const int PnpClickSampleCap = 90;
        // [V59-FIX-6] EN: Distance/ratio diagnostic logging state (user request) —
        //     one log line whenever the ratio moves >5% from the last logged value
        //     (2s throttle) so test sessions can be correlated with the experience.
        //     FR: État du log diagnostic distance/ratio (demande user) — une ligne
        //     quand le ratio s'écarte de >5 % de la dernière valeur loggée
        //     (limité à 1/2 s) pour corréler les sessions de test au ressenti.
        private float _pnpLastLoggedDistRatio = float.MinValue;
        private int _pnpLastDistLogMs = 0;

        // [V59-FIX-18] EN: Edge-extrapolation seamless transition state.
        //     _lastPnpBilinearPos : last bilinear-mapped 0-1 position when BOTH LEDs
        //       were visible (captured BEFORE ApplyPnpCorrections). Used by the
        //       edge-zone gate to decide whether the cursor was near the screen
        //       boundary when the LED was lost.
        //     _lastPnpScreenPos   : last FULL output (bilinear + PnP corrections)
        //       when BOTH LEDs were visible. Anchors the extrapolated position at
        //       the exact PnP output so no jump occurs at the 2->1 LED transition.
        //     _pnpExtrapolationOffset : frozen delta (_lastPnpScreenPos -
        //       _lastPnpBilinearPos) applied to bilinear-extrapolated frames to
        //       preserve the last known 3D correction during edge glide.
        //     FR: Etat de transition fluide extrapolation bord.
        //       _lastPnpBilinearPos : derniere pos bilineaire 0-1 quand 2 LEDs
        //         visibles (avant ApplyPnpCorrections). Gate zone-bord.
        //       _lastPnpScreenPos : derniere sortie complete (bilineaire + PnP)
        //         quand 2 LEDs visibles. Ancre la pos extrapolee pour 0 saut.
        //       _pnpExtrapolationOffset : delta gele (_lastPnpScreenPos -
        //         _lastPnpBilinearPos) ajoute aux frames extrapolees.
        private Point2F _lastPnpBilinearPos = new Point2F(0.5f, 0.5f);
        private Point2F _lastPnpScreenPos   = new Point2F(0.5f, 0.5f);
        private Point2F _pnpExtrapolationOffset = new Point2F(0f, 0f);

        public bool IsCalibrating { get { return _calibrateForm != null; } }
        /// <summary>
        /// True when the auto/manual mode selection form is open (EN/FR: Vrai quand le formulaire de sélection auto/manuel est ouvert)
        /// </summary>
        public bool IsSelectingMode { get { return _modeSelectionForm != null; } }
        public bool IsCalibrated 
        { 
            get
            {
                // [V59-FIX-4] EN: PnP Hybrid validates on the 4 corners (the 5th
                //     center point was removed — user decision).
                //     FR: Le PnP Hybrid valide sur les 4 coins (le 5e point central a
                //     été retiré — décision user).
                if (EffLayout == LEDLayoutType.WiimoteBarPnP)
                {
                    return _topLeftPt.HasValue && _topRightPt.HasValue &&
                           _bottomRightPt.HasValue && _bottomLeftPt.HasValue;
                }
                if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners || _ledLayout == LEDLayoutType.TwoWiimoteBar)
                {
                    // Gun4IR/FourCorners/TwoWiimoteBar: 5 points including center (all indices)
                    // (EN/FR: 5 points incluant le centre)
                    return _gun4irPoints != null && _gun4irPoints.All(p => p.HasValue);
                }
                // WiimoteBar: Check 4 points (TL, TR, BR, BL) OR 3 points in permissive mode
                // (EN/FR: WiimoteBar : Vérifier 4 points OU 3 points en mode permissif)
                bool has4Points = _topLeftPt.HasValue && _topRightPt.HasValue && 
                                  _bottomRightPt.HasValue && _bottomLeftPt.HasValue;
                
                // Permissive mode: Allow 3 of 4 points for very large screens (EN/FR: Mode permissif : 3 sur 4 points)
                bool has3Points = Options.Instance.PermissiveWiimoteBarCalibration &&
                                  (_topLeftPt.HasValue && _topRightPt.HasValue && _bottomRightPt.HasValue);
                return has4Points || has3Points;
            } 
        }


        /// <summary>
        /// Cancel any active calibration or mode selection screen (triggered by Home button).
        /// Closes both the mode selection form and the calibration form if open.
        /// (EN/FR: Annuler toute calibration ou écran de sélection actif (déclenché par bouton Home).
        /// Ferme le formulaire de sélection de mode ET le formulaire de calibration si ouverts.)
        /// </summary>
        public void CancelCalibration()
        {
            try
            {
                // Close mode selection form if open via UI thread (EN/FR: Fermer le formulaire de sélection via thread UI)
                var modeForm = _modeSelectionForm;
                if (modeForm != null && !modeForm.IsDisposed)
                {
                    Program.PostToUIThread(() =>
                    {
                        try { if (!modeForm.IsDisposed) { modeForm.DialogResult = System.Windows.Forms.DialogResult.Cancel; modeForm.Close(); } }
                        catch { }
                    });
                }

                // Cancel calibration form if open (EN/FR: Annuler le formulaire de calibration si ouvert)
                if (_calibrateForm != null)
                {
                    SimpleLogger.Instance.Info(string.Format("[P{0}] Calibration cancelled via Home button", _playerIndex));
                    ResetCalibration();
                    EndCalibrate();
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("[P{0}] CancelCalibration error: {1}", _playerIndex, ex.Message));
            }
        }

        // Expose the calculated center for visualization (EN/FR: Exposer le centre calculé pour la visualisation)
        public Point2F DoubleT_M1 { get { return _lastM1; } }
        public Point2F DoubleT_M2 { get { return _lastM2; } }

        // Convert from normalized (0-1) to raw (0-1023, 0-767) for visualizer display
        public Point2F LastCalculatedCenter 
        { 
            get 
            { 
                float rawX = _lastValidCenter.X * 1023f;
                float rawY = _lastValidCenter.Y * 767f;
                
                // Apply vertical offset if in Dynamic Perspective mode (EN/FR: Appliquer offset vertical si en mode Dynamic Perspective)
                if (UseDynamicPerspective && (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners))
                {
                    int offsetY = Options.Instance.GetDynamicPerspectiveOffsetY(_playerIndex);
                    rawY += offsetY;
                    
                    // Clamp to valid range (EN/FR: Limiter à la plage valide)
                    rawY = Math.Max(0, Math.Min(767, rawY));
                }
                
                return new Point2F(rawX, rawY);
            } 
        }

        // Expose calibration points for visualization (EN/FR: Exposer points de calibration pour visualisation)
        // Returns normalized coordinates (0-1)
        public Point2F?[] GetCalibrationPoints()
        {
            // [V59-FIX-16] EN: EffLayout — WiimoteBar + hybrid checkbox routes to
            //     WiimoteBarPnP, which has its own branch below.
            //     FR: EffLayout — Wiimote Bar + case hybride bascule sur WiimoteBarPnP,
            //     qui dispose de sa propre branche ci-dessous.
            if (EffLayout == LEDLayoutType.WiimoteBar)
            {
                // Return 4 points for WiimoteBar (EN/FR: Retourner 4 points pour WiimoteBar)
                return new Point2F?[] { _topLeftPt, _topRightPt, _bottomRightPt, _bottomLeftPt };
            }
            else if (EffLayout == LEDLayoutType.WiimoteBarPnP)
            {
                // [V59-FIX-4] EN: PnP Hybrid — the 4 captured corners in order
                //     TL, TR, BR, BL (the 5th center point was removed — user decision).
                //     FR: PnP Hybrid — les 4 coins capturés dans l'ordre HG, HD, BD, BG
                //     (le 5e point central a été retiré — décision user).
                return new Point2F?[] { _topLeftPt, _topRightPt, _bottomRightPt, _bottomLeftPt };
            }
            else // Gun4IR / FourCorners
            {
                return _gun4irPoints;
            }
        }

        public bool UseDynamicPerspective { get; set; }

        public void Calibrate()
        {
            if (_calibrateForm != null)
                return;

            // [V59-FIX-4] EN: PnP Hybrid — single calibration flow, straight to the
            //     4-point form. The fisheye/standard selection page was REMOVED (the
            //     flag changed nothing in the corrections — user decision).
            //     FR: PnP Hybrid — flux de calibration unique, directement vers le
            //     formulaire 4 points. La page de sélection fisheye/standard a été
            //     RETIRÉE (le flag ne changeait rien dans les corrections — décision user).
            // [V59-FIX-4c] EN: StartCalibrationForm MUST run on the UI thread (it
            //     creates and Shows the CalibrateForm). Calling it directly from the
            //     polling thread made the calibration page never appear — only the IR
            //     visualizer showed. Same pattern as the other modes (see below).
            //     FR: StartCalibrationForm doit tourner sur le thread UI (elle crée et
            //     affiche le CalibrateForm). L'appel direct depuis le thread de polling
            //     empêchait la page de calibration d'apparaître — seul le visualiseur IR
            //     s'affichait. Même schéma que les autres modes (voir ci-dessous).
            if (EffLayout == LEDLayoutType.WiimoteBarPnP)
            {
                Program.PostToUIThread(() => StartCalibrationForm());
                return;
            }

            // Gun4IR / 4 Corners: Ask user for mode (Dynamic vs Standard)
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners)
            {
                // Reset calibration immediately and SYNCHRONOUSLY on entry (User request v6)
                ResetCalibration();

                // Show full-screen mode selection form (EN/FR: Afficher le formulaire plein écran de sélection)
                Program.PostToUIThread(() =>
                {
                    string modeName = _ledLayout == LEDLayoutType.Gun4IRDiamond ? "Gun4IR" : "4 Corners";
                    
                    using (var selectionForm = new CalibrationModeSelectionForm(Options.Instance.MonitorId, modeName))
                    {
                        // Store reference so CancelCalibration() can close it via Home button
                        // (EN/FR: Stocker référence pour que CancelCalibration() puisse la fermer via Home)
                        _modeSelectionForm = selectionForm;
                        DialogResult result = selectionForm.ShowDialog();

                        if (!selectionForm.SelectionMade || result == DialogResult.Cancel)
                        {
                            // User cancelled (ESC key)
                            return;
                        }

                        if (result == DialogResult.Yes)
                        {
                            // Dynamic Mode
                            UseDynamicPerspective = true;
                            Options.Instance.SetUseDynamicPerspective(_playerIndex, true);
                            Program.Notify(string.Format("{0} Dynamic Mode Enabled (P{1})", modeName, _playerIndex));
                            // No need to show calibration form
                        }
                        else
                        {
                            // Standard Mode
                            UseDynamicPerspective = false;
                            Options.Instance.SetUseDynamicPerspective(_playerIndex, false);
                            StartCalibrationForm();
                        }
                        _modeSelectionForm = null; // Clear reference once closed
                    }
                });
            }
            else
            {
                // Other modes: Standard Calibration
                Program.PostToUIThread(() => StartCalibrationForm());
            }
        }

        private void StartCalibrationForm()
        {
            // Reset calibration immediately and SYNCHRONOUSLY (User request v6)
            ResetCalibration();
            
            // OPEN IR VISUALIZER (EN/FR: Ouvrir IR Visualizer) - Requested by User to be behind CalibrateForm
            Program.PostToUIThread(() =>
            {
                if (_irVisualizer == null || _irVisualizer.IsDisposed)
                {
                    _irVisualizer = new WiimoteGun.UI.Legacy.IRVisualizerForm();
                    _irVisualizer.TopMost = true; // Stay on top of other apps (EN/FR: Rester au-dessus des autres apps)
                    _irVisualizer.Show();
                }
            });
            // Pass LED layout to calibration form (EN/FR: Passer le layout LED au formulaire de calibration)
            // Use current MonitorId from options (EN/FR: Utiliser le MonitorId actuel des options)
            // [V59-FIX-11] EN/FR: the EFFECTIVE layout (Wiimote Bar + the hybrid checkbox
            //     behaves as PNP-Hybrid — the form then runs the PnP flow).
            _calibrateForm = new CalibrateForm(Options.Instance.MonitorId, EffLayout);
            
            // Handle ESC key cancellation (EN/FR: Gérer l'annulation avec touche ESC)
            _calibrateForm.CalibrationCancelled += (s, e) =>
            {
                SimpleLogger.Instance.Info(string.Format("Calibration cancelled by user for Player {0}", _playerIndex));
                ResetCalibration(); // Reset points like HOME button does (EN/FR: Réinitialiser points comme bouton HOME)
                EndCalibrate();     // Properly close and dispose (EN/FR: Fermer et disposer proprement)
            };
            
            _calibrateForm.Show();
        }

        public void EndCalibrate()
        {
            if (_calibrateForm == null)
                return;

            // Permissive mode: Extrapolate missing BL point if only 3 points captured
            // (EN/FR: Mode permissif : Extrapoler point BL manquant si seulement 3 points capturés)
            if (Options.Instance.PermissiveWiimoteBarCalibration && 
                // [V59-FIX-16] EN: EffLayout ensures PnP-Hybrid is EXCLUDED from the
                //     permissive-extrapolation path — losing a LED in PnP mode must NOT
                //     auto-accept the point (distance validation and 2-click arm/confirm
                //     are the only valid paths for that mode).
                //     FR: EffLayout exclut le PnP-Hybrid du chemin permissif — perdre
                //     une LED en mode PnP ne doit PAS accepter automatiquement le point.
                EffLayout == LEDLayoutType.WiimoteBar &&
                !_bottomLeftPt.HasValue && 
                _topLeftPt.HasValue && _bottomRightPt.HasValue)
            {
                // Extrapolate BL from TL.X and BR.Y (rectangular assumption)
                // (EN/FR: Extrapoler BL depuis TL.X et BR.Y - hypothèse rectangulaire)
                _bottomLeftPt = new Point2F(_topLeftPt.Value.X, _bottomRightPt.Value.Y);
                SimpleLogger.Instance.Info(string.Format("Permissive mode: Extrapolated Bottom-Left point from TL.X + BR.Y for Player {0}", _playerIndex));
            }

            // Save player-specific calibration - 4-POINT FORMAT (EN/FR: Sauvegarder calibration spécifique au joueur - FORMAT 4-POINTS)
            Options.Instance.SetCalibrationForPlayer(
                _playerIndex,
                _topLeftPt.HasValue ? _topLeftPt.Value.X : -1,
                _topLeftPt.HasValue ? _topLeftPt.Value.Y : -1,
                _topRightPt.HasValue ? _topRightPt.Value.X : -1,
                _topRightPt.HasValue ? _topRightPt.Value.Y : -1,
                _bottomRightPt.HasValue ? _bottomRightPt.Value.X : -1,
                _bottomRightPt.HasValue ? _bottomRightPt.Value.Y : -1,
                _bottomLeftPt.HasValue ? _bottomLeftPt.Value.X : -1,
                _bottomLeftPt.HasValue ? _bottomLeftPt.Value.Y : -1
            );

            // Save Gun4IR, 2-Wiimote Bar, or 4-Corners calibration
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
            {
                Options.Instance.SetGun4IRCalibration(_playerIndex, _gun4irPoints);
            }
            else if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
            {
                Options.Instance.SetTwoWiimoteBarCalibration(_playerIndex, _gun4irPoints);
            }
            else if (_ledLayout == LEDLayoutType.FourCorners)
            {
                Options.Instance.SetFourCornersCalibration(_playerIndex, _gun4irPoints);
            }

            // [V59-FIXES] EN: Persist the PnP reference state (effective focal +
            //     calibration distance) so the 3D corrections survive an app restart.
            //     The corners above already persist through the 4-point format.
            //     FR: Persiste l'état de référence PnP (focale effective + distance de
            //     calibration) pour que les corrections 3D survivent à un relancement.
            //     Les coins ci-dessus persistent déjà via le format 4-points.
            if (EffLayout == LEDLayoutType.WiimoteBarPnP && _pnpFocalX > 0 && _pnpRefDistance > 0)
            {
                Options.Instance.SetPnpEffectiveFocal(_playerIndex, _pnpFocalX);
                Options.Instance.SetPnpCalibrationDistance(_playerIndex, _pnpRefDistance);
                SimpleLogger.Instance.Info(string.Format(
                    "[V59] PnP: reference persisted — focal={0:F3}, calDist={1:F2}m",
                    _pnpFocalX, _pnpRefDistance));
            }

            // [V59-FIX-13] EN: Persist the TwoWiimoteBars cross-calibration data
            //     (user request — the user should NOT have to recalibrate every
            //     restart; only when they change a PnP-related option).
            //     31 floats: 5 top midpoints, 5 bottom midpoints, 5 centers,
            //     1 pair separation. The homography is rebuilt on load.
            //     FR: Persiste les données de calibration croix TwoWiimoteBars
            //     (demande user — l'utilisateur ne doit PAS recalibrer à chaque
            //     relance ; seulement s'il change une option liée au PnP).
            //     31 floats : 5 milieux hauts, 5 milieux bas, 5 centres,
            //     1 séparation de paire. L'homographie est reconstruite au chargement.
            if (_ledLayout == LEDLayoutType.TwoWiimoteBar &&
                _twbCalTopMid[0].HasValue && _twbCalTopMid[1].HasValue &&
                _twbCalTopMid[2].HasValue && _twbCalTopMid[3].HasValue &&
                _twbCalTopMid[4].HasValue)
            {
                float[] twbData = new float[31];
                int idx = 0;
                for (int i = 0; i < 5; i++) { twbData[idx++] = _twbCalTopMid[i].Value.X; twbData[idx++] = _twbCalTopMid[i].Value.Y; }
                for (int i = 0; i < 5; i++) { twbData[idx++] = _twbCalBotMid[i].Value.X; twbData[idx++] = _twbCalBotMid[i].Value.Y; }
                for (int i = 0; i < 5; i++) { twbData[idx++] = _twbCalCenter[i].Value.X; twbData[idx++] = _twbCalCenter[i].Value.Y; }
                twbData[idx] = _twbCalPairSep;
                Options.Instance.SetTwbCalData(_playerIndex, twbData);
                SimpleLogger.Instance.Info(string.Format(
                    "[V59] TWB: cross calibration persisted (P{0}) — pair sep={1:F3}",
                    _playerIndex, _twbCalPairSep));
            }

            Options.Instance.Save();
            _cachedStaticHomography = null; // Invalidate cache after new calibration (EN/FR: Invalider cache après nouvelle calibration)

            // Memorize IR span at calibration time for distance compensation
            // Only applies to manual calibration modes (NOT dynamic/auto)
            // (EN/FR: Mémoriser la taille IR en fin de calibration pour compensation de distance
            // Uniquement pour modes calibration manuelle (PAS mode auto))

            // Calculate average calibration IR span from collected samples
            // (EN/FR: Calculer la moyenne de la taille IR de calibration à partir des échantillons collectés)
            if (_calibrationSpanSamples.Count > 0)
            {
                _calibrationIRSpan = _calibrationSpanSamples.Average();
                SimpleLogger.Instance.Info(string.Format("[P{0}] Calibration: Reference IR span finalized = {1:F4} (from {2} clicks)", _playerIndex, _calibrationIRSpan, _calibrationSpanSamples.Count));
            }
            else
            {
                _calibrationIRSpan = 0f;
                SimpleLogger.Instance.Warning(string.Format("[P{0}] Calibration: No IR span captured! Distance compensation will be inactive.", _playerIndex));
            }
            if (_calibrationIRSpan > 0f)
            {
                SimpleLogger.Instance.Info(string.Format("[P{0}] Distance Compensation: Calibration IR span memorized = {1:F4}", _playerIndex, _calibrationIRSpan));
                // Initialize _lastIRSpan to calibration reference so compensation starts neutral (factor = 1.0)
                // This ensures the corrective factor is preserved even when only 1 LED is visible (edge aiming),
                // because _lastIRSpan is only updated when 2 sensors are found, and keeps its value otherwise.
                // (EN/FR: Initialiser _lastIRSpan à la référence de calibration pour que la compensation démarre neutre (facteur = 1.0)
                // Ainsi le facteur est conservé même quand 1 seul LED est visible (vise bord), car _lastIRSpan
                // n'est mis à jour que quand 2 capteurs sont visibles, et conserve sa valeur sinon.)
                _lastIRSpan = _calibrationIRSpan;
            }



            var frm = _calibrateForm;
            _calibrateForm = null;

            Program.PostToUIThread(() => 
            { 
                frm.Dispose(); 
                
                // Close IR Visualizer (EN/FR: Fermer IR Visualizer)
                if (_irVisualizer != null && !_irVisualizer.IsDisposed)
                {
                    _irVisualizer.Close();
                    _irVisualizer.Dispose();
                    _irVisualizer = null;
                }
            });
        }

        public void ResetCalibration()
        {
            _topLeftPt = null;
            _topRightPt = null;
            _bottomRightPt = null;
            _bottomLeftPt = null; // NEW: Reset 4th point (EN/FR: Réinitialiser 4e point)
            for(int i=0; i<5; i++) _gun4irPoints[i] = null; // Reset Gun4IR points
            _calibrationSpanSamples.Clear(); // Clear calibration samples (EN/FR: Effacer les échantillons de calibration)
            _calibrationIRSpan = 0f; // Reset reference span during calibration (EN/FR: Réinitialiser le span de référence pendant la calibration)
            _lastIRSpan = 0f;
            _lastValidDistFactor = 1.0f; // Reset persistent factor (EN/FR: Réinitialiser facteur persistant)
            _cachedStaticHomography = null; // Invalidate homography cache (EN/FR: Invalider cache homographie)
            // [V59] Reset PnP state
            // [V59-FIXES] _pnpFocalXIsSet removed (dead duplicate of the _pnpFocalX > 0
            // check) ; _pnpLastDistanceM added (live per-frame estimate).
            // [V59-FIX-4] _pnpCenterPt removed (5th point dropped — user decision).
            _pnpFocalX = 0f;
            _pnpRefDistance = 0f;
            _pnpRefSeparation = 0f;
            _pnpSmoothedDistRatio = 1.0f;
            _pnpLastDistanceM = 0f;
            // [V59-FIX-3] Two-click capture state reset
            _pnpClickArmed = false;
            _pnpClickSamples.Clear();
            // [V59-FIX-5] Shared roll-invariance angle reset
            _ledRollSmoothedRad = 0f;
            // [V59-FIX-8] TwoWiimoteBars cross-calibration reset
            for (int i = 0; i < 5; i++)
            {
                _twbCalTopMid[i] = null;
                _twbCalBotMid[i] = null;
                _twbCalCenter[i] = null;
            }
            _twbMapped = false;
            // [V59-FIX-9] Hybrid PnP state reset
            _twbCalPairSep = 0f;
            _twbSepSmoothedRatio = 1f;
            _twbLastLoggedSepRatio = float.MinValue;
            _twbLastSepLogMs = 0;
            // [V59-FIX-8c] Homography reset
            _twbHomography = null;
            // [V59-FIX-8e/28] Degraded-mode anchors reset
            _twbTopSpan = 0f;
            _twbBotSpan = 0f;
            _twbHaveQuad = false;
            _extrapolatingOneLed = false;
            _lastSurvivingPos = new Point2F();
            _rearmReacquisition = false;
            _reacquisitionOffset = new Point2F(0f, 0f);
            _lastOutputScreenPos = new Point2F(0.5f, 0.5f);
        }

        public Point2F? GetScaledPosition(WiimoteLib.DataTypes.IRState ir, WiimoteLib.DataTypes.ButtonState buttons, WiimoteLib.DataTypes.ButtonState lastState)
        {
            Point2F relativePosition = new Point2F();
            bool hasSensor = true;

            // --- GLOBAL IR NORMALIZATION (DISTANCE COMPENSATION v4) ---
            // 1. Always update raw _lastIRSpan if possible (using all found sensors for stability)
            // (EN/FR: Toujours mettre à jour le span brut si possible en utilisant tous les capteurs trouvés)
            _lastIRSpan = GetLayoutAwareSpan(ir);

            // 2. Apply normalization if enabled and criteria met
            // (EN/FR: Appliquer la normalisation si activé et critères remplis)
            // CRITICAL: Skip if calibrating, selecting mode, OR using dynamic auto-homography
            // NEW (v7): Explicitly restricted to Single Sensor Bar (WiimoteBar) by User request
            // [V59-FIX-11/32] EN: Distance Compensation is STRICTLY for a Single Sensor Bar WITHOUT Hybrid PnP.
            //     When WiimoteBarHybridPnp is active OR when using TwoWiimoteBars, distance compensation is inoperative
            //     (the PnP engine applies its own 3D scale correction).
            //     FR: La compensation de distance est STRICTEMENT réservée à une barre unique SANS Hybride PnP.
            //     Quand WiimoteBarHybridPnp est actif OU sous TwoWiimoteBars, la compensation est inopérante.
            if (Options.Instance.EnableDistanceCompensation && 
                !Options.Instance.WiimoteBarHybridPnp && 
                _ledLayout == LEDLayoutType.WiimoteBar && 
                EffLayout == LEDLayoutType.WiimoteBar && 
                !IsCalibrating && !IsSelectingMode && !UseDynamicPerspective && _calibrationIRSpan > 0f)
            {
                if (_lastIRSpan > 0f)
                {
                    float instantFactor = _lastIRSpan / _calibrationIRSpan;
                    
                    // Apply smoothing to prevent jitter (EN/FR: Appliquer lissage pour éviter les tremblements)
                    _lastValidDistFactor = (_lastValidDistFactor * (1.0f - DistanceFactorSmoothing)) + (instantFactor * DistanceFactorSmoothing);
                }

                // Apply persistent factor even if _lastIRSpan is 0 (LED lost at edge)
                // (EN/FR: Appliquer le facteur persistant même si _lastIRSpan est 0 (LED perdue au bord))
                float distFactor = _lastValidDistFactor;
                
                if (Math.Abs(distFactor - 1.0f) >= DistanceCompensationThreshold)
                {
                    // Normalize sensor points around camera center (0.5, 0.5)
                    if (ir.IRSensor0.Found) ir.IRSensor0.Position = NormalizePoint(ir.IRSensor0.Position, distFactor);
                    if (ir.IRSensor1.Found) ir.IRSensor1.Position = NormalizePoint(ir.IRSensor1.Position, distFactor);
                    if (ir.IRSensor2.Found) ir.IRSensor2.Position = NormalizePoint(ir.IRSensor2.Position, distFactor);
                    if (ir.IRSensor3.Found) ir.IRSensor3.Position = NormalizePoint(ir.IRSensor3.Position, distFactor);
                    
                    // Normalize midpoint
                    ir.Midpoint = NormalizePoint(ir.Midpoint, distFactor);
                }
            }

            // Calculate position based on LED layout (EN/FR: Calculer position selon layout LED)
            switch (EffLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    relativePosition = CalculateWiimoteBarPosition(ir, ref hasSensor);
                    break;

                // [V59] EN: PnP uses the same WiimoteBar bilinear base + 3D corrections
                //     FR: Le PnP utilise la même base bilinéaire WiimoteBar + corrections 3D
                case LEDLayoutType.WiimoteBarPnP:
                    // [V59-FIXES] EN: Live gun-to-screen distance from the current FULL
                    //     LED separation (normalized units — same helper and focal as
                    //     the calibration path; that consistency is what keeps the
                    //     distance ratio physically meaningful). 0 when < 2 LEDs visible.
                    //     FR: Distance gun-écran en direct depuis la séparation COMPLÈTE
                    //     des LEDs (unités normalisées — même helper et focale que le
                    //     chemin de calibration ; cette cohérence rend le ratio de
                    //     distance physiquement juste). 0 si < 2 LEDs visibles.
                    _pnpLastDistanceM = PnpEstimateDistance(GetPnpLedSeparation(ir));
                    // [V59-FIX-2] EN: RAW camera midpoint here. The 4-corner bilinear
                    //     base + PnP corrections are applied LATER (after the capture
                    //     logic) so calibration points are stored raw — exactly like
                    //     WiimoteBar — and corrections only touch the OUTPUT position.
                    //     FR: Midpoint caméra BRUT ici. La base bilinéaire 4 points et
                    //     les corrections PnP sont appliquées PLUS TARD (après la
                    //     logique de capture) pour que les points de calibration soient
                    //     stockés bruts — comme WiimoteBar — et que les corrections ne
                    //     touchent que la position de SORTIE.
                    relativePosition = CalculateWiimoteBarPosition(ir, ref hasSensor);
                    // [V59-FIX-18] EN: SMART edge-zone gate (replaces V59-FIX-17 naive gate).
                    //     Extrapolation is allowed ONLY when the opt-in is ON AND the
                    //     last confirmed 2-LED bilinear position (_lastPnpBilinearPos)
                    //     was near a screen boundary.  This prevents extrapolation from
                    //     firing on random noise/occlusion in the interior of the PnP
                    //     zone, which caused the position jump (PnP-corrected vs
                    //     bilinear-only) that produced jitter/saccades.
                    //     The frozen PnP-correction offset (_pnpExtrapolationOffset)
                    //     is added AFTER bilinear mapping (below) so the cursor
                    //     continues from the exact last PnP-corrected position with
                    // [V59-FIX-19] EN: Simple opt-in gate (replaces V59-FIX-18 EdgeZone gate).
                    //     When WiimoteBarPnpEdgeExtrapolation is ON, ANY 1-LED loss
                    //     triggers extrapolation — no zone restriction. The frozen
                    //     PnP offset (_pnpExtrapolationOffset, computed below after
                    //     bilinear+PnP) ensures the cursor continues from the exact
                    //     last PnP position with zero jump. The wide clamp
                    //     (+-0.5) in the 1-LED path below lets the cursor go well
                    //     past the calibrated PnP zone to reach the screen edge.
                    //     When OFF (default strict PnP): hasSensor=false -> freeze.
                    //     FR: Gate opt-in simple (remplace la gate EdgeZone FIX-18).
                    //     Option ON : toute perte de 1 LED declenche l'extrapolation
                    //     sans restriction de zone. L'offset PnP gele garantit
                    //     continuité parfaite. Le clamp large (+-0.5) dans le chemin
                    //     1-LED laisse le curseur atteindre le bord ecran au-dela
                    //     de la zone PnP calibree.
                    //     Option OFF (PnP strict par defaut) : hasSensor=false -> gele.
                    if (hasSensor && !(ir.IRSensor0.Found && ir.IRSensor1.Found) &&
                        !Options.Instance.WiimoteBarPnpEdgeExtrapolation)
                    {
                        hasSensor = false;
                    }
                    break;

                // [V59-FIX-21] EN: Multi-LED layouts (Gun4IR, TwoWiimoteBar, FourCorners)
                //     were missing from this switch! They were falling through with default
                //     (0,0) relativePosition, which after line 843 (1.0 - X) permanently
                //     stuck the cursor at top-right (1.0, 0.0) and broke TWB completely.
                //     FR: Layouts multi-LED manquants dans le switch ! Ils tombaient avec
                //     (0,0) par défaut, qui après le flip (1.0 - X) coinçait le curseur
                //     en haut à droite (1.0, 0.0) et cassait totalement le mode TWB.
                case LEDLayoutType.Gun4IRDiamond:
                case LEDLayoutType.TwoWiimoteBar:
                case LEDLayoutType.FourCorners:
                    relativePosition = CalculateMultiLEDPosition(ir, ref hasSensor);
                    break;
            }

            // [V59-FIX-5] EN: SHARED roll-invariance for the 2-LED modes (WiimoteBar AND
            //     PNP-Hybrid — user decision, so the comparison between both modes stays
            //     honest). The LED midpoint is un-rotated around the image center by the
            //     measured LED-line roll angle: captures AND live mapping work in a
            //     roll-neutral space — the cursor shows where the UN-TILTED gun aims (X
            //     and Y). Requires both LEDs visible (the angle needs 2 points).
            //     FR: Invariance en roulis PARTAGÉE pour les modes 2 LEDs (WiimoteBar ET
            //     PNP-Hybrid — décision user, pour que la comparaison entre les deux
            //     modes reste honnête). Le milieu LED est dé-roulé autour du centre de
            //     l'image de l'angle mesuré de la ligne LED : captures ET mapping vivent
            //     en espace neutre en roulis — le curseur montre où vise le gun
            //     NON-penché (X et Y). Exige les 2 LEDs visibles (l'angle a besoin de 2 points).
            if (hasSensor && (_ledLayout == LEDLayoutType.WiimoteBar || EffLayout == LEDLayoutType.WiimoteBarPnP))
            {
                UnrotateLedMidpointRoll(ref relativePosition, ir);
            }

            if (hasSensor)
            {
                // [V59-FIX-23] EN: Update sensor anchors ONLY when BOTH sensors are found.
                //     When dropping to 1 LED, CalculateWiimoteBarPosition incrementally updates
                //     _midSensorPos via delta tracking without clobbering the base anchors.
                //     FR: Met à jour les ancres UNIQUEMENT quand les 2 capteurs sont trouvés.
                // [V59-FIX-24] EN: Sensor anchors (_firstSensorPos, _secondSensorPos, _midSensorPos)
                //     are updated in CalculateWiimoteBarPosition in RAW camera coordinates.
                //     Do NOT overwrite _midSensorPos with unrotated relativePosition here,
                //     otherwise 1-LED extrapolation suffers an unwanted double-rotation!
                //     FR: Les ancres capteurs sont mises à jour en coords caméra brutes dans
                //     CalculateWiimoteBarPosition. Ne pas écraser avec relativePosition dé-roulée !

                // Store raw position for calibration (before visual offsets)
                // (EN/FR: Stocker position brute pour calibration (avant offsets visuels))
                _lastRawPoint = relativePosition;
                _lastRawPoint.X = 1.0f - _lastRawPoint.X;

                // [V59-FIX-21] EN: When _twbMapped is true, CalculateMultiLEDPosition
                //     already produced the SCREEN-mapped position (0=left, 1=right).
                //     Do NOT flip X again! Only raw camera positions need the 1.0 - X flip.
                //     FR: Quand _twbMapped est vrai, la position écran est déjà produite.
                //     Ne pas re-flipper X ! Seules les positions caméra brutes ont besoin du flip.
                if (!_twbMapped)
                {
                    relativePosition.X = 1.0f - relativePosition.X;
                }

            // Calibration Step Logic - 3-POINT SYSTEM (EN/FR: Logique de calibration - SYSTÈME 3-POINTS)
            if (_calibrateForm != null && ((buttons.A && !lastState.A) || (buttons.B && !lastState.B)))
            {
                // Capture current IR span sample if valid (EN/FR: Capturer l'échantillon de taille IR actuel si valide)
                if (_lastIRSpan > 0f) _calibrationSpanSamples.Add(_lastIRSpan);

                // [V59-FIX-16] EN/FR: EffLayout — when WiimoteBarHybridPnp is
                //     checked, EffLayout == WiimoteBarPnP, so the standard 1-click
                //     WiimoteBar capture no longer shadows the PnP 2-click branch.
                if (EffLayout == LEDLayoutType.WiimoteBar)
                {
                    // Capture 4 points sequentially: TopLeft → TopRight → BottomRight → BottomLeft
                    // (EN/FR: Capturer 4 points séquentiellement : HautGauche → HautDroit → BasDroit → BasGauche)
                    if (!_topLeftPt.HasValue) _topLeftPt = _lastRawPoint;              // Step 1/4
                    else if (!_topRightPt.HasValue) _topRightPt = _lastRawPoint;       // Step 2/4
                    else if (!_bottomRightPt.HasValue && !_bottomLeftPt.HasValue)
                    {
                        // Step 3: Bottom points (Flexible in Permissive Mode)
                        if (Options.Instance.PermissiveWiimoteBarCalibration)
                        {
                            // Flexible capture: Determine if BL or BR based on X coordinate
                            // (EN/FR: Capture flexible : Déterminer si BL ou BR selon coordonnée X)
                            float distToLeft = Math.Abs(_lastRawPoint.X - _topLeftPt.Value.X);
                            float distToRight = Math.Abs(_lastRawPoint.X - _topRightPt.Value.X);

                            if (distToLeft < distToRight)
                            {
                                // Captured BL
                                _bottomLeftPt = _lastRawPoint;
                                // Extrapolate BR from TR.X and BL.Y
                                _bottomRightPt = new Point2F(_topRightPt.Value.X, _bottomLeftPt.Value.Y);
                                SimpleLogger.Instance.Info($"Permissive: Captured BL, Extrapolated BR");
                            }
                            else
                            {
                                // Captured BR
                                _bottomRightPt = _lastRawPoint;
                                // Extrapolate BL from TL.X and BR.Y
                                _bottomLeftPt = new Point2F(_topLeftPt.Value.X, _bottomRightPt.Value.Y);
                                SimpleLogger.Instance.Info($"Permissive: Captured BR, Extrapolated BL");
                            }
                        }
                        else
                        {
                            _bottomRightPt = _lastRawPoint; // Standard Step 3
                        }
                    }
                    else if (!_bottomLeftPt.HasValue) _bottomLeftPt = _lastRawPoint;   // Step 4/4
                }
                // [V59] EN: PnP 5-point capture with distance validation.
                //     FR: Capture PnP 5 points avec validation de distance.
                else if (EffLayout == LEDLayoutType.WiimoteBarPnP)
                {
                    // [V59-FIX-3] EN: TWO-CLICK capture (user request). Click 1 arms the
                    //     point and starts per-frame sampling; click 2 confirms — the
                    //     captured point is the MEDIAN of the samples gathered between the
                    //     two clicks, which removes trigger-jerk noise without rejecting
                    //     shaky users. The sample buffer is filled in the calibration UI
                    //     update block below (after this capture logic).
                    //     FR: Capture en DEUX CLICS (demande user). Le clic 1 arme le
                    //     point et démarre l'échantillonnage par image ; le clic 2
                    //     confirme — le point capturé est la MÉDIANE des échantillons
                    //     recueillis entre les 2 clics, ce qui supprime le sursaut de
                    //     gâchette sans refuser les utilisateurs tremblants. Le tampon
                    //     d'échantillons est rempli dans le bloc de mise à jour UI de
                    //     calibration ci-dessous (après cette logique de capture).
                    if (!_pnpClickArmed)
                    {
                        // Click 1 — arm this point (no capture yet)
                        // (EN/FR: Clic 1 — armer ce point (pas encore de capture))
                        _pnpClickArmed = true;
                        _pnpClickSamples.Clear();
                        SimpleLogger.Instance.Info(string.Format(
                            "[V59] PnP: point {0}/4 armed — hold steady and click again to confirm",
                            PnpCapturedCount() + 1));
                    }
                    else
                    {
                        // Click 2 — distance validation + median capture
                        // (EN/FR: Clic 2 — validation distance + capture médiane)
                        float currentDist = _pnpLastDistanceM;
                        bool accept = false;
                        if (currentDist > 0f)
                        {
                            // [V59] EN: Distance validation (Lichtknarre-like). The FIRST
                            //     confirmed point records the reference and returns true;
                            //     later points are rejected when >12% from it (3
                            //     consecutive rejects = accept with warning, anti-deadlock).
                            //     FR: Validation distance (à la Lichtknarre). Le PREMIER
                            //     point confirmé enregistre la référence et renvoie vrai ;
                            //     les points suivants sont refusés à >12% de celle-ci
                            //     (3 refus consécutifs = accepter avec warning, anti-blocage).
                            accept = _calibrateForm.ValidateDistance(currentDist);
                            if (!accept)
                            {
                                SimpleLogger.Instance.Warning(string.Format(
                                    "[V59] PnP: distance {0:F2}m rejected (>12% from reference, trial {1}/3)",
                                    currentDist, _calibrateForm.DistanceRejectCount));
                                // EN/FR: Restart sampling — stale samples from a bad stance must not contaminate the median
                                _pnpClickSamples.Clear();
                            }
                        }
                        else
                        {
                            SimpleLogger.Instance.Warning("[V59] PnP: both sensor bar LEDs required to capture a point");
                        }

                        if (accept)
                        {
                            // [V59] First point sets the focal reference
                            //     FR: Premier point = référence de focale
                            if (_pnpFocalX <= 0)
                            {
                                // [V59-FIX-10] EN/FR: reference focal = the DECLARED focal
                                //     when the geometry mode is on (keeps the calibration
                                //     and the live ratios consistent).
                                GetPnpGeometry(out _, out float refFocal);
                                _pnpFocalX = refFocal;
                                _pnpRefSeparation = GetPnpLedSeparation(ir);
                                _pnpRefDistance = currentDist;
                                _pnpIROffsetM = Options.Instance.GetIROffsetMM(_playerIndex) / 1000f;
                                SimpleLogger.Instance.Info(string.Format(
                                    "[V59] PnP: reference set — dist={0:F2}m{1}", currentDist,
                                    Options.Instance.PnpDeclareGeometry
                                        ? string.Format(" (declared: {0:F0}mm, {1:F0}°)", Options.Instance.PnpLedSpacingMM, Options.Instance.PnpLensFovDeg)
                                        : " (approximation)"));
                            }

                            // [V59-FIX-3] EN: Captured point = MEDIAN of the samples
                            //     gathered between the two clicks (falls back to the
                            //     instantaneous raw point if the buffer is empty).
                            //     FR: Point capturé = MÉDIANE des échantillons recueillis
                            //     entre les 2 clics (repli sur le point brut instantané
                            //     si le tampon est vide).
                            Point2F captured = PnpMedianSample();

                            // [V59-FIX-4] EN: Capture the point — 4 corners ONLY (the 5th
                            //     center point and the anti-distortion anchor were
                            //     removed — user decision, Δ measurements proved to be
                            //     aim noise, not distortion).
                            //     FR: Capture du point — 4 coins SEULEMENT (le 5e point
                            //     central et l'ancre anti-distorsion ont été retirés —
                            //     décision user, les mesures Δ étaient du bruit de visée,
                            //     pas de la distorsion).
                            if (!_topLeftPt.HasValue) _topLeftPt = captured;
                            else if (!_topRightPt.HasValue) _topRightPt = captured;
                            else if (!_bottomRightPt.HasValue) _bottomRightPt = captured;
                            else if (!_bottomLeftPt.HasValue) _bottomLeftPt = captured;

                            _pnpClickArmed = false;
                            _pnpClickSamples.Clear();

                            // Update distance display on the calibration form
                            // (EN/FR: Mettre à jour l'affichage distance sur le formulaire de calibration)
                            _calibrateForm.UpdateDistance(currentDist);
                        }
                        // Rejected click 2 → stay ARMED, sampling restarts from a clean buffer
                        // (EN/FR: Clic 2 refusé → reste ARMÉ, l'échantillonnage repart d'un tampon propre)
                    }
                }
                else // Gun4IR / FourCorners / TwoWiimoteBar with center (5 points)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!_gun4irPoints[i].HasValue)
                        {
                            _gun4irPoints[i] = _lastRawPoint;
                            // [V59-FIX-8] EN: TwoWiimoteBars cross calibration — capture
                            //     the TWO bar midpoints at this click (_lastM1/_lastM2
                            //     were just computed by TryCalculateDoubleTCenter in
                            //     CalculateMultiLEDPosition: m1 = top pair midpoint,
                            //     m2 = bottom pair midpoint, sorted by Y).
                            //     FR: Calibration croise TwoWiimoteBars — capture des
                            //     DEUX milieux de barres à ce clic (_lastM1/_lastM2
                            //     viennent d'être calculés par TryCalculateDoubleTCenter
                            //     dans CalculateMultiLEDPosition : m1 = milieu de la
                            //     paire haute, m2 = milieu de la paire basse, triés par Y).
                            if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                            {
                                _twbCalCenter[i] = _lastRawPoint;
                                _twbCalTopMid[i] = _lastM1;
                                _twbCalBotMid[i] = _lastM2;
                                // [V59-FIX-9] EN/FR: reference pair separation at the CENTER click
                                //     (m1↔m2 distance) — feeds the hybrid PnP ratio.
                                if (i == 0)
                                {
                                    _twbCalPairSep = GetDistance(_lastM1, _lastM2);
                                    _twbSepSmoothedRatio = 1f;
                                    SimpleLogger.Instance.Info(string.Format(
                                        "[V59] TWB: cross calibration — center click, pair sep={0:F3}", _twbCalPairSep));
                                    // [V59-FIX-20] Guard: pair sep near 0 = only ONE bar visible.
                                    //     Homography will likely be degenerate -> cursor stuck.
                                    //     User MUST have BOTH IR bars visible (4 LEDs, top+bottom).
                                    if (!_doubleTInitialized || _twbCalPairSep < 0.02f)
                                        SimpleLogger.Instance.Warning(
                                            "[V59-FIX-20] TWB: pair sep~0 — only ONE bar detected. " +
                                            "BOTH IR bars must be visible simultaneously (4 LEDs). " +
                                            "Homography may be degenerate -> cursor stuck.");
                                }
                                // [V59-FIX-8d] EN: last click (RIGHT) — build the homography
                                //     as a LEAST-SQUARES fit over ALL FIVE clicks (center +
                                //     4 edges) — the initial BlancoM principle (user
                                //     confirmation): the center click participates in the
                                //     mapping like the others; the fit averages the aim
                                //     noise and anchors the screen middle where the action
                                //     is. The center check then reports the post-fit
                                //     residual of the center click.
                                //     FR: dernier clic (DROITE) — construit l'homographie
                                //     par MOINDRES CARRÉS sur les CINQ clics (centre + 4
                                //     bords) — le principe initial BlancoM (confirmation
                                //     user) : le clic centre PARTICIPE au mapping comme les
                                //     autres ; l'ajustement moyenne le bruit de visée et
                                //     ancre le milieu d'écran où se joue l'action. Le
                                //     contrôle du centre mesure ensuite le résidu post-fit
                                //     du clic centre.
                                if (i == 4 && _twbCalCenter[0].HasValue)
                                {
                                    var hSrc = new Point2F[] {
                                        _twbCalCenter[0].Value, _twbCalCenter[1].Value,
                                        _twbCalCenter[2].Value, _twbCalCenter[3].Value,
                                        _twbCalCenter[4].Value };
                                    var hDst = new Point2F[] {
                                        new Point2F(0.5f, 0.5f), new Point2F(0.5f, 0f),
                                        new Point2F(0.5f, 1f), new Point2F(0f, 0.5f),
                                        new Point2F(1f, 0.5f) };
                                    _twbHomography = ComputeHomographyLeastSquares(hSrc, hDst);
                                    if (_twbHomography != null)
                                    {
                                        float cw = _twbHomography[6] * _twbCalCenter[0].Value.X + _twbHomography[7] * _twbCalCenter[0].Value.Y + 1.0f;
                                        if (Math.Abs(cw) > 0.0001f)
                                        {
                                            float qcX = (_twbHomography[0] * _twbCalCenter[0].Value.X + _twbHomography[1] * _twbCalCenter[0].Value.Y + _twbHomography[2]) / cw;
                                            float qcY = (_twbHomography[3] * _twbCalCenter[0].Value.X + _twbHomography[4] * _twbCalCenter[0].Value.Y + _twbHomography[5]) / cw;
                                            float qcDev = (float)Math.Sqrt((qcX - 0.5f) * (qcX - 0.5f) + (qcY - 0.5f) * (qcY - 0.5f)) * 100f;
                                            SimpleLogger.Instance.Info(string.Format(
                                                "[V59] TWB: homography built — center check ({0:F2},{1:F2}), deviation {2:F1}% — {3}",
                                                qcX, qcY, qcDev, qcDev <= 4f ? "quality OK" : "QUALITY SUSPECT — recalibration advised"));
                                        }
                                    }
                                    else
                                    {
                                        SimpleLogger.Instance.Warning("[V59] TWB: homography build FAILED — falling back to the legacy bilinear");
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }

                // Wiimote Bar Logic - 4-POINT BILINEAR MAPPING (EN/FR: Logique WiimoteBar - MAPPING BILINÉAIRE 4-POINTS)
                // [V59-FIX-2] EN: PnP SHARES this bilinear base — it maps the RAW camera
                //     positions of the 4 captured corners to screen 0-1. Without it the
                //     PnP cursor was the raw midpoint (FOV-relative) while the screen
                //     covers only ~half the camera FOV → shrunken tracking zone, the
                //     player had to aim OFF-SCREEN to reach the screen edges (and then
                //     lost one of the two LEDs).
                //     FR: Le PnP PARTAGE cette base bilinéaire — elle mappe les positions
                //     caméra BRUTES des 4 coins capturés vers l'écran 0-1. Sans elle le
                //     curseur PnP était le midpoint brut (relatif au FOV caméra) alors
                //     que l'écran ne couvre que ~la moitié du FOV → zone de tracking
                //     réduite, il fallait viser HORS ÉCRAN pour atteindre les bords
                //     (en perdant une des 2 LEDs).
                if (_ledLayout == LEDLayoutType.WiimoteBar || EffLayout == LEDLayoutType.WiimoteBarPnP)
                {
                    if (_topLeftPt.HasValue && _topRightPt.HasValue && _bottomRightPt.HasValue && _bottomLeftPt.HasValue)
                    {
                        // Use 2D Bilinear Trapezoid mapping instead of perspective Homography
                        // This prevents the edges from being warped/curved by perspective simulation.
                        // (EN/FR: Utiliser mapping trapézoïdal bilinéaire 2D au lieu de l'homographie de perspective)
                        
                        float rawX = relativePosition.X;
                        float rawY = relativePosition.Y;

                        relativePosition.X = InterpolateX(rawX, rawY, _topLeftPt.Value, _topRightPt.Value, _bottomLeftPt.Value, _bottomRightPt.Value);
                        relativePosition.Y = InterpolateY(rawX, rawY, _topLeftPt.Value, _topRightPt.Value, _bottomLeftPt.Value, _bottomRightPt.Value);

                        // Distance Compensation: logic removed from here and moved to local LED extrapolation
                        // to preserve iron-sight aim consistency (scaling around center was problematic).

                        // [V59-FIX-19] EN: Clamp only when 2 LEDs present (PnP zone). In
                        //     1-LED extrapolation mode the clamp is skipped here; the
                        //     wider clamp (+-0.5) is applied in the offset block below
                        //     so the cursor can go past the calibrated zone to the edge.
                        //     FR: Clamp seulement quand 2 LEDs (zone PnP). En mode
                        //     extrapolation 1-LED le clamp est applique plus bas (+-0.5)
                        //     pour permettre d'atteindre le bord d'ecran.
                        // [V59-FIX-22] EN: When edge extrapolation is ON, do NOT artificially clamp
                        //     to [-0.05, 1.05] while 2 LEDs are present! Otherwise the cursor hits
                        //     a hard wall before the LEDs exit FOV, and then jumps/bonds to the edge.
                        //     Allow smooth travel up to [-0.5, 1.5] continuously.
                        //     FR: Quand l'extrapolation est active, ne PAS clamper artificiellement
                        //     à [-0.05, 1.05] tant que 2 LEDs sont présentes ! Sinon le curseur bute
                        //     contre un mur puis saute. Permettre une course fluide jusqu'à [-0.5, 1.5].
                        float clamp2LedMin = (EffLayout == LEDLayoutType.WiimoteBarPnP && Options.Instance.WiimoteBarPnpEdgeExtrapolation)
                            ? -0.50f : -0.05f;
                        float clamp2LedMax = (EffLayout == LEDLayoutType.WiimoteBarPnP && Options.Instance.WiimoteBarPnpEdgeExtrapolation)
                            ? 1.50f : 1.05f;

                        if (ir.IRSensor0.Found && ir.IRSensor1.Found)
                        {
                            relativePosition.X = Math.Max(clamp2LedMin, Math.Min(clamp2LedMax, relativePosition.X));
                            relativePosition.Y = Math.Max(clamp2LedMin, Math.Min(clamp2LedMax, relativePosition.Y));
                        }

                    }
                    // [V59-FIX-18] EN: Capture bilinear output (pre-PnP) when both LEDs
                    //     are visible - used by the edge-zone gate in the switch above
                    //     to decide whether extrapolation is appropriate on LED loss.
                    //     Also apply the frozen PnP offset when in 1-LED extrapolation
                    //     mode so the cursor continues from exactly where the PnP
                    //     correction left off (zero jump at the 2->1 LED transition).
                    //     FR: Capturer la sortie bilineaire (pre-PnP) quand 2 LEDs
                    //     visibles - gate zone-bord du switch.
                    //     Appliquer aussi l'offset PnP gele en mode extrapolation 1-LED.
                    if (EffLayout == LEDLayoutType.WiimoteBarPnP && hasSensor)
                    {
                        bool bothNow = ir.IRSensor0.Found && ir.IRSensor1.Found;
                        if (bothNow)
                        {
                            // 2 LEDs: record bilinear anchor for edge-zone detection
                            _lastPnpBilinearPos = relativePosition;
                        }
                        else
                        {
                            // [V59-FIX-19] EN: Wide clamp (+-0.5) — lets the cursor
                            //     travel 50% past the calibrated PnP zone boundary so
                            //     it can reach the physical screen edge regardless
                            //     of how the 4-corner calibration was positioned.
                            //     FR: Clamp large (+-0.5) — permet au curseur de
                            //     depasser la zone PnP calibree de 50% pour atteindre
                            //     le bord physique de l'ecran quelle que soit la
                            //     position de la calibration 4 coins.
                            relativePosition.X = Math.Max(-0.5f, Math.Min(1.5f,
                                relativePosition.X + _pnpExtrapolationOffset.X));
                            relativePosition.Y = Math.Max(-0.5f, Math.Min(1.5f,
                                relativePosition.Y + _pnpExtrapolationOffset.Y));
                        }
                    }

                    // [V59-FIX-2] EN: 3D corrections ON TOP of the bilinear base — applied
                    //     to the SCREEN-mapped position, never to the captured calibration
                    //     points (they are stored raw, like WiimoteBar).
                    //     FR: Corrections 3D PAR-DESSUS la base bilinéaire — appliquées à
                    //     la position mappée ÉCRAN, jamais aux points de calibration
                    //     (stockés bruts, comme WiimoteBar).
                    if (EffLayout == LEDLayoutType.WiimoteBarPnP && hasSensor)
                    {
                        ApplyPnpCorrections(ref relativePosition, ir);
                        // [V59-FIX-18] EN: Update full PnP output anchor + compute frozen offset
                        //     while both LEDs are visible.  _pnpExtrapolationOffset freezes
                        //     the PnP correction at the moment the 2nd LED is lost, so
                        //     the first extrapolated frame starts from the exact PnP
                        //     position with zero jump.
                        //     FR: Mise a jour de l'ancre de sortie PnP complete + calcul
                        //     de l'offset gele pendant que 2 LEDs sont visibles.
                        if (ir.IRSensor0.Found && ir.IRSensor1.Found)
                        {
                            // [V59-FIX-24] EN: When returning from 1-LED extrapolation, capture the
                            //     residual discrepancy between the extrapolated screen pos and the new
                            //     2-LED absolute pos into _reacquisitionOffset.
                            //     FR: Lors de la réacquisition des 2 LEDs, capture l'écart résiduel.
                            if (_rearmReacquisition)
                            {
                                _rearmReacquisition = false;
                                float maxOffset = 0.15f; // Clamp extreme teleports (>15% screen)
                                _reacquisitionOffset.X = Math.Max(-maxOffset, Math.Min(maxOffset, _lastOutputScreenPos.X - relativePosition.X));
                                _reacquisitionOffset.Y = Math.Max(-maxOffset, Math.Min(maxOffset, _lastOutputScreenPos.Y - relativePosition.Y));
                            }

                            // Clean, true 2-LED values — never distorted by blending
                            _lastPnpScreenPos = relativePosition;
                            _pnpExtrapolationOffset.X = _lastPnpScreenPos.X - _lastPnpBilinearPos.X;
                            _pnpExtrapolationOffset.Y = _lastPnpScreenPos.Y - _lastPnpBilinearPos.Y;
                        }

                        // [V59-FIX-24] EN: Apply smooth exponential decay on reacquisition offset.
                        //     At frame 0 of reacquisition: relativePosition + _reacquisitionOffset = _lastOutputScreenPos
                        //     -> EXACT 0 JUMP (continuous motion with zero rebound).
                        //     Subsequent frames smoothly glide towards true 2-LED position over ~60ms (decay * 0.75).
                        //     FR: Décroissance exponentielle d'écart : 0 rebond garanti à la réacquisition.
                        if (Math.Abs(_reacquisitionOffset.X) > 0.0001f || Math.Abs(_reacquisitionOffset.Y) > 0.0001f)
                        {
                            relativePosition.X += _reacquisitionOffset.X;
                            relativePosition.Y += _reacquisitionOffset.Y;
                            _reacquisitionOffset.X *= 0.75f;
                            _reacquisitionOffset.Y *= 0.75f;
                            if (Math.Abs(_reacquisitionOffset.X) < 0.0001f) _reacquisitionOffset.X = 0f;
                            if (Math.Abs(_reacquisitionOffset.Y) < 0.0001f) _reacquisitionOffset.Y = 0f;
                        }

                        _lastOutputScreenPos = relativePosition;
                    }
                }
                // Gun4IR / 4 Corners Logic
                else if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // Check if we should use Dynamic Mode (Auto) or Standard Calibration (Manual)
                    // Applies to Gun4IR and FourCorners if enabled.
                    bool useDynamic = UseDynamicPerspective && (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners);

                    if (useDynamic)
                    {
                        // --- DYNAMIC MODE (Auto-Calibrate / Absolute Perspective) ---
                        // Map CURRENT Camera Points -> IDEAL Screen Points
                        // Then transform the Camera Center (0.5, 0.5) to Screen Space.
                        
                        hasSensor = false; // Default to false, enable only if tracking succeeds
                        
                        Point2F[] src = null;
                        Point2F[] dst = null;
                        
                        // Reconstruct currentPoints from IR state
                        var currentPoints = new Dictionary<int, Point2F>();
                        if (ir.IRSensor0.Found) currentPoints[0] = ir.IRSensor0.Position;
                        if (ir.IRSensor1.Found) currentPoints[1] = ir.IRSensor1.Position;
                        if (ir.IRSensor2.Found) currentPoints[2] = ir.IRSensor2.Position;
                        if (ir.IRSensor3.Found) currentPoints[3] = ir.IRSensor3.Position;
                        
                        int count = currentPoints.Count;
                        
                        // 1. Identify Points
                        if (count >= 3)
                        {
                            var pts = currentPoints.Values.ToList();
                            float cx = 0, cy = 0;
                            foreach (var p in pts) { cx += p.X; cy += p.Y; }
                            cx /= pts.Count;
                            cy /= pts.Count;

                            var identifiedPoints = new Dictionary<int, Point2F>(); // 0=Top/TL, 1=Right/TR, 2=Bottom/BR, 3=Left/BL
                            
                            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                            {
                                // Diamond Identification (Inverted Logic)
                                foreach (var p in pts)
                                {
                                    float dx = p.X - cx;
                                    float dy = p.Y - cy;
                                    double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                                    
                                    // -90=Bottom, 0=Right, 90=Top, 180=Left
                                    if (angle >= -135 && angle < -45) identifiedPoints[3] = p; // Bottom (Index 3)
                                    else if (angle >= -45 && angle < 45) identifiedPoints[2] = p; // Right (Index 2)
                                    else if (angle >= 45 && angle < 135) identifiedPoints[1] = p; // Top (Index 1)
                                    else identifiedPoints[4] = p; // Left (Index 4)
                                }
                            }
                            else
                            {
                                // Rectangle Identification (Corrected for Left/Right inversion)
                                // (EN/FR: Identification Rectangle - Corrigée pour inversion Gauche/Droite)
                                foreach (var p in pts)
                                {
                                    float dx = p.X - cx;
                                    float dy = p.Y - cy;
                                    double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                                    
                                    // Swap Left/Right to fix horizontal inversion
                                    // (EN/FR: Inverser Gauche/Droite pour corriger l'inversion horizontale)
                                    if (angle >= -90 && angle < 0) identifiedPoints[3] = p; // BR (Index 3) - Was BL
                                    else if (angle >= -180 && angle < -90) identifiedPoints[4] = p; // BL (Index 4) - Was BR
                                    else if (angle >= 90 && angle <= 180) identifiedPoints[1] = p; // TL (Index 1) - Was TR
                                    else identifiedPoints[2] = p; // TR (Index 2) - Was TL
                                }
                            }
                            
                            // 2. Build src and dst arrays
                            List<Point2F> srcList = new List<Point2F>();
                            List<Point2F> dstList = new List<Point2F>();
                            
                            // Ideal Screen Coordinates
                            var idealPoints = new Dictionary<int, Point2F>();
                            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                            {
                                idealPoints[1] = new Point2F(0.5f, 0.0f); // Top
                                idealPoints[2] = new Point2F(1.0f, 0.5f); // Right
                                idealPoints[3] = new Point2F(0.5f, 1.0f); // Bottom
                                idealPoints[4] = new Point2F(0.0f, 0.5f); // Left
                            }
                            else
                            {
                                idealPoints[1] = new Point2F(0.0f, 0.0f); // TL
                                idealPoints[2] = new Point2F(1.0f, 0.0f); // TR
                                idealPoints[3] = new Point2F(1.0f, 1.0f); // BR
                                idealPoints[4] = new Point2F(0.0f, 1.0f); // BL
                            }
                            
                            foreach (var kvp in identifiedPoints)
                            {
                                srcList.Add(kvp.Value);
                                dstList.Add(idealPoints[kvp.Key]);
                            }
                            
                            // If 3 points, hallucinate the 4th
                            if (srcList.Count == 3)
                            {
                                int missingIndex = -1;
                                for (int i = 1; i <= 4; i++) if (!identifiedPoints.ContainsKey(i)) missingIndex = i;
                                
                                if (missingIndex != -1)
                                {
                                    Point2F pOpposite = new Point2F();
                                    Point2F pAdj1 = new Point2F();
                                    Point2F pAdj2 = new Point2F();
                                    
                                    int oppIndex = -1;
                                    if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                                    {
                                        if (missingIndex == 1) oppIndex = 3;
                                        else if (missingIndex == 2) oppIndex = 4;
                                        else if (missingIndex == 3) oppIndex = 1;
                                        else if (missingIndex == 4) oppIndex = 2;
                                    }
                                    else
                                    {
                                        if (missingIndex == 1) oppIndex = 3;
                                        else if (missingIndex == 2) oppIndex = 4;
                                        else if (missingIndex == 3) oppIndex = 1;
                                        else if (missingIndex == 4) oppIndex = 2;
                                    }
                                    
                                    if (identifiedPoints.ContainsKey(oppIndex))
                                    {
                                        pOpposite = identifiedPoints[oppIndex];
                                        foreach (var kvp in identifiedPoints)
                                        {
                                            if (kvp.Key != oppIndex)
                                            {
                                                if (pAdj1.X == 0 && pAdj1.Y == 0) pAdj1 = kvp.Value;
                                                else pAdj2 = kvp.Value;
                                            }
                                        }
                                        Point2F pMissing = new Point2F(pAdj1.X + pAdj2.X - pOpposite.X, pAdj1.Y + pAdj2.Y - pOpposite.Y);
                                        srcList.Add(pMissing);
                                        dstList.Add(idealPoints[missingIndex]);
                                    }
                                }
                            }
                            
                            if (srcList.Count == 4)
                            {
                                src = srcList.ToArray();
                                dst = dstList.ToArray();
                                
                                float[] H = ComputeHomography(src, dst);
                                
                                float camX = 0.5f;
                                float camY = 0.5f;
                                
                                float w = H[6] * camX + H[7] * camY + 1.0f;
                                if (Math.Abs(w) > 0.0001f)
                                {
                                    relativePosition.X = (H[0] * camX + H[1] * camY + H[2]) / w;
                                    relativePosition.Y = (H[3] * camX + H[4] * camY + H[5]) / w;
                                    hasSensor = true;
                                    _lastValidCenter = relativePosition;
                                }
                            }
                        }
                    }
                    else
                    {
                        // --- STANDARD CALIBRATION MODE (Static Homography) ---
                        // Map CALIBRATED Points -> IDEAL Screen Points
                        // Transform the current Relative Position (Midpoint) to Screen Space.
                        
                        // We need 4 outer points for Gun4IR (indices 1-4) or 4 corners for TwoWiimoteBar/FourCorners
                        // (EN/FR: 4 points externes pour Gun4IR ou 4 coins pour TwoWiimoteBar/FourCorners)
                        
                        if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                        {
                            // Gun4IR: Use indices 1-4 (Top, Right, Bottom, Left)
                            if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                                _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                            {
                                Point2F[] src = new Point2F[4];
                                Point2F[] dst = new Point2F[4];

                                src[0] = _gun4irPoints[1].Value; // Top
                                src[1] = _gun4irPoints[2].Value; // Right
                                src[2] = _gun4irPoints[3].Value; // Bottom
                                src[3] = _gun4irPoints[4].Value; // Left

                                dst[0] = new Point2F(0.5f, 0.0f); // Top Center
                                dst[1] = new Point2F(1.0f, 0.5f); // Right Center
                                dst[2] = new Point2F(0.5f, 1.0f); // Bottom Center
                                dst[3] = new Point2F(0.0f, 0.5f); // Left Center

                                // Compute or use cached Homography Matrix (EN/FR: Calculer ou utiliser cache)
                                float[] H;
                                if (Options.Instance.EnableHomographyCache && _cachedStaticHomography != null)
                                {
                                    H = _cachedStaticHomography;
                                }
                                else
                                {
                                    H = ComputeHomography(src, dst);
                                    if (Options.Instance.EnableHomographyCache)
                                        _cachedStaticHomography = H;
                                }

                                // Apply Homography to Current Position (Midpoint)
                                float x = relativePosition.X;
                                float y = relativePosition.Y;
                                float w = H[6] * x + H[7] * y + 1.0f;

                                if (Math.Abs(w) > 0.0001f)
                                {
                                    relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                                    relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                                }

                                // Distance Compensation: logic removed from here and moved to local LED extrapolation
                                // to preserve iron-sight aim consistency.
                            }
                        }
                        else if (_ledLayout == LEDLayoutType.FourCorners)
                        {
                            // FourCorners (5 pts captured): Use indices 1-4 (TL, TR, BR, BL) - Ignore 0 (Center)
                            // (EN/FR: Utiliser indices 1-4 (HG, HD, BD, BG) - Ignorer 0 (Centre))
                            if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                                _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                            {
                                Point2F[] src = new Point2F[4];
                                Point2F[] dst = new Point2F[4];

                                src[0] = _gun4irPoints[1].Value; // TL (was param 1)
                                src[1] = _gun4irPoints[2].Value; // TR (was param 2)
                                src[2] = _gun4irPoints[3].Value; // BR (was param 3)
                                src[3] = _gun4irPoints[4].Value; // BL (was param 4)

                                dst[0] = new Point2F(0.0f, 0.0f); // TL
                                dst[1] = new Point2F(1.0f, 0.0f); // TR
                                dst[2] = new Point2F(1.0f, 1.0f); // BR
                                dst[3] = new Point2F(0.0f, 1.0f); // BL

                                // Compute or use cached Homography Matrix (EN/FR: Calculer ou utiliser cache)
                                float[] H;
                                if (Options.Instance.EnableHomographyCache && _cachedStaticHomography != null)
                                {
                                    H = _cachedStaticHomography;
                                }
                                else
                                {
                                    H = ComputeHomography(src, dst);
                                    if (Options.Instance.EnableHomographyCache)
                                        _cachedStaticHomography = H;
                                }

                                // Apply Homography to Current Position (Midpoint)
                                float x = relativePosition.X;
                                float y = relativePosition.Y;
                                float w = H[6] * x + H[7] * y + 1.0f;

                                if (Math.Abs(w) > 0.0001f)
                                {
                                    relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                                    relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                                }

                                // Distance Compensation: manual mode only (NOT dynamic)
                                // (EN/FR: Compensation de distance : mode manuel uniquement (PAS mode dynamique))
                                /* REPLACED by Global IR Normalization v4 at start of method (EN/FR: Remplacé par Normalisation Globale v4 au début)
                if (Options.Instance.EnableDistanceCompensation && !UseDynamicPerspective)
                {
                    relativePosition = ApplyDistanceCompensation(relativePosition, _lastIRSpan);
                }
                */
                            }
                        }
                        else if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                        {
                            // [V59-FIX-8] EN: When the cross-map produced the cursor
                            //     (_twbMapped), it ALREADY includes the calibration mapping —
                            //     skip the legacy corner bilinear entirely (that path was the
                            //     old bounce source). When the cross-map is unavailable
                            //     (not calibrated), the legacy path stays as fallback.
                            //     FR: Quand le map croix a produit le curseur (_twbMapped),
                            //     il INCLUT déjà le mapping de calibration — on saute le
                            //     bilinéaire à coins legacy (ce chemin était la source des
                            //     rebonds d'avant). Si le map croix est indisponible (non
                            //     calibré), le chemin legacy reste en repli.
                            if (!_twbMapped)
                            {
                                if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue &&
                                    _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                                {
                                    float rawX = relativePosition.X;
                                    float rawY = relativePosition.Y;

                                    // gun4irPoints: 1=TL, 2=TR, 3=BR, 4=BL
                                    // Interpolate takes: TL, TR, BL, BR
                                    relativePosition.X = InterpolateX(rawX, rawY,
                                        _gun4irPoints[1].Value, _gun4irPoints[2].Value,
                                        _gun4irPoints[4].Value, _gun4irPoints[3].Value);

                                    relativePosition.Y = InterpolateY(rawX, rawY,
                                        _gun4irPoints[1].Value, _gun4irPoints[2].Value,
                                        _gun4irPoints[4].Value, _gun4irPoints[3].Value);
                                }
                            }
                        }
                        else // Final Fallback (Should be unreachable if all handled)
                        {
                             // ... existing fallback or empty ...
                        }
                    }
                }
            }

            if (_calibrateForm != null)
            {
                // Update UI Step
                int currentStep = 0;
                // [V59-FIX-16] EN/FR: EffLayout — routes PnP-Hybrid to its own
                //     4-point counter instead of the 3-point WiimoteBar counter.
                if (EffLayout == LEDLayoutType.WiimoteBar)
                {
                    // 3-POINT SYSTEM: Count how many points are set (EN/FR: Compter combien de points sont définis)
                    if (_topLeftPt.HasValue) currentStep++;
                    if (_topRightPt.HasValue) currentStep++;
                    if (_bottomRightPt.HasValue) currentStep++;
                }
                else if (EffLayout == LEDLayoutType.WiimoteBarPnP)
                {
                    // [V59-FIXES] EN: PnP Hybrid step counter — 4 corners.
                    //     (Without this branch the counter read _gun4irPoints (never
                    //     used in PnP) and the display stayed stuck on "1/5".)
                    //     FR: Compteur d'étapes PnP Hybrid — 4 coins.
                    if (_topLeftPt.HasValue) currentStep++;
                    if (_topRightPt.HasValue) currentStep++;
                    if (_bottomRightPt.HasValue) currentStep++;
                    if (_bottomLeftPt.HasValue) currentStep++;
                }
                else
                {
                    for(int i=0; i<5; i++) if (_gun4irPoints[i].HasValue) currentStep = i + 1;
                }
                _calibrateForm.SetStep(currentStep);
                // UpdateState: Pass 3 calibration points (EN/FR: Passer les 3 points de calibration)
                _calibrateForm.UpdateState(relativePosition, _topLeftPt, _topRightPt, _bottomRightPt, _bottomLeftPt);

                // [V59-FIXES] EN: Live distance readout while aiming at each PnP target —
                //     the [dist: x.xx m] line refreshes every frame, not only on clicks.
                //     FR: Lecture de distance en direct pendant la visée de chaque cible
                //     PnP — la ligne [dist: x.xx m] se rafraîchit à chaque image.
                if (EffLayout == LEDLayoutType.WiimoteBarPnP && _pnpLastDistanceM > 0f)
                {
                    _calibrateForm.UpdateDistance(_pnpLastDistanceM);
                }

                // [V59-FIX-3] EN: Per-frame sampling between the two clicks of a PnP
                //     point (median capture) + confirm-mode instruction sync on the form.
                //     FR: Échantillonnage par image entre les 2 clics d'un point PnP
                //     (capture médiane) + synchro de l'instruction de confirmation.
                if (EffLayout == LEDLayoutType.WiimoteBarPnP)
                {
                    if (_pnpClickArmed && _pnpLastDistanceM > 0f && _pnpClickSamples.Count < PnpClickSampleCap)
                    {
                        _pnpClickSamples.Add(_lastRawPoint);
                    }
                    _calibrateForm.SetPnpConfirmMode(_pnpClickArmed);
                }

                if (buttons.Home && !lastState.Home)
                {
                    ResetCalibration();
                    EndCalibrate();
                }
                else if (IsCalibrated)
                {
                    EndCalibrate();
                }
            }

            if (!hasSensor)
            {
                return null;
            }

            // Apply X/Y offset universally for all layouts and modes (EN/FR: Appliquer offset X/Y universellement)
            // This compensates for physical misalignment between IR camera center and aiming point
            // (EN/FR: Cela compense le décalage physique entre le centre de la caméra IR et le point de visée)
            int offsetX = Options.Instance.GetDynamicPerspectiveOffsetX(_playerIndex);
            int offsetY = Options.Instance.GetDynamicPerspectiveOffsetY(_playerIndex);
            
            if (offsetX != 0 || offsetY != 0)
            {
                // Convert pixel offset to normalized coords (0-1) (EN/FR: Convertir offset pixels en coords normalisées)
                float normOffsetX = offsetX / 1023f;
                float normOffsetY = offsetY / 767f;
                
                relativePosition.X += normOffsetX;
                relativePosition.Y += normOffsetY;
                
                // Clamp to valid range (EN/FR: Limiter à la plage valide)
                relativePosition.X = Math.Max(0, Math.Min(1, relativePosition.X));
                relativePosition.Y = Math.Max(0, Math.Min(1, relativePosition.Y));
            }

            // Scale to 0-65535 range for vmulti
            float scaledX = relativePosition.X * ushort.MaxValue;
            float scaledY = relativePosition.Y * ushort.MaxValue;

            return new Point2F(scaledX, scaledY);
        }

        private Point2F CalculateWiimoteBarPosition(IRState ir, ref bool hasSensor)
        {
            // EXISTING WIIMOTE LOGIC (2-LED horizontal bar)
            // (EN/FR: Logique Wiimote existante - barre 2 LED horizontale)
            Point2F relativePosition = new Point2F();

            if (ir.IRSensor0.Found && ir.IRSensor1.Found)
            {
                // [V59-FIX-24] Both LEDs visible: update raw anchors and arm reacquisition decay if returning from 1-LED
                _firstSensorPos = ir.IRSensor0.Position;
                _secondSensorPos = ir.IRSensor1.Position;
                _midSensorPos = ir.Midpoint;

                if (_extrapolatingOneLed)
                {
                    _extrapolatingOneLed = false;
                    _rearmReacquisition = true;
                }
                relativePosition = ir.Midpoint;
            }
            else if (ir.IRSensor0.Found || ir.IRSensor1.Found)
            {
                // [V59-FIX-24] EN: Continuous 1-LED relative tracking in RAW camera coords.
                //     On 2->1 transition: lock the surviving LED's identity ONCE.
                //     Subsequent frames accumulate delta from _lastSurvivingPos into _midSensorPos.
                //     Because _midSensorPos is kept strictly in raw camera space, UnrotateLedMidpointRoll
                //     applies exactly once downstream — eliminating any double-rotation rebound!
                //     FR: Suivi relatif 1-LED continu en coords caméra brutes, sans double rotation.
                Point2F curLed = ir.IRSensor0.Found ? ir.IRSensor0.Position : ir.IRSensor1.Position;
                if (!_extrapolatingOneLed)
                {
                    _extrapolatingOneLed = true;
                    _lastSurvivingPos = curLed;
                }
                else
                {
                    float dx = curLed.X - _lastSurvivingPos.X;
                    float dy = curLed.Y - _lastSurvivingPos.Y;
                    _midSensorPos.X += dx;
                    _midSensorPos.Y += dy;
                    _lastSurvivingPos = curLed;
                }
                relativePosition = _midSensorPos;
            }
            else
            {
                _extrapolatingOneLed = false;
                hasSensor = false;
            }

            if (hasSensor)
            {
                // Update last valid center in normalized coords (EN/FR: Mettre à jour le dernier centre valide en coords normalisées)
                _lastValidCenter = relativePosition;
            }

            return relativePosition;
        }

        private int _idTopLeft = -1;
        private int _idTopRight = -1;
        private int _idBottomLeft = -1;
        private int _idBottomRight = -1;

        // [V59-FIX-8] EN: TwoWiimoteBars CROSS calibration (user spec) — 5 clicks:
        //     0=Center, 1=Top-center, 2=Bottom-center, 3=Left-center, 4=Right-center.
        //     Each click stores the TWO bar midpoints (mTop = midpoint of the top LED
        //     pair, mBot = midpoint of the bottom pair) + the Double-T center (for the
        //     visualizer). The mapping then interpolates the cursor from these
        //     measured trajectories — nothing is assumed about the bar spacing
        //     (BlancoM fixed 250mm or extensible 250-1000mm both work).
        //     FR: Calibration CROIX TwoWiimoteBars (spec user) — 5 clics : 0=Centre,
        //     1=Haut-centre, 2=Bas-centre, 3=Gauche-centre, 4=Droite-centre. Chaque
        //     clic stocke les DEUX milieux de barres (mTop = milieu de la paire haute,
        //     mBot = milieu de la paire basse) + le centre Double-T (pour le
        //     visualiseur). Le mapping interpole ensuite le curseur depuis ces
        //     trajectoires mesurées — rien n'est supposé sur l'écartement des barres
        //     (BlancoM fixe 250 mm comme extensible 250-1000 mm fonctionnent).
        private readonly Point2F?[] _twbCalTopMid = new Point2F?[5];
        private readonly Point2F?[] _twbCalBotMid = new Point2F?[5];
        private readonly Point2F?[] _twbCalCenter = new Point2F?[5];
        private bool _twbMapped = false; // last frame used the cross map (skip the legacy bilinear)
        // [V59-FIX-9] EN: Hybrid PnP (optional checkbox) — bar-pair separation captured
        //     at the CENTER click (reference), smoothed live ratio, X-axis correction
        //     (the vertical distance error already cancels via the two-bar average).
        //     FR: PnP hybride (case optionnelle) — séparation des barres capturée au clic
        //     CENTRE (référence), ratio lissé en direct, correction sur l'axe X (l'erreur
        //     verticale de distance s'annule déjà via la moyenne des deux barres).
        private float _twbCalPairSep = 0f;
        private float _twbSepSmoothedRatio = 1f;
        private float _twbLastLoggedSepRatio = float.MinValue;
        private int _twbLastSepLogMs = 0;
        // [V59-FIX-8c] EN: Homography built from the 4 EDGE clicks (their Double-T
        //     centers ↔ screen (0.5,0),(0.5,1),(0,0.5),(1,0.5)). A proper 2D
        //     perspective map — the separable sy/sx model ignored the perspective
        //     coupling (user's transcription proved the center ≠ axes intersection)
        //     and produced a huge cursor lead. The CENTER click becomes a quality
        //     check (H(center) should be ≈ (0.5, 0.5)).
        //     FR: Homographie construite sur les 4 clics DE BORD (leurs centres
        //     Double-T ↔ écran (0,5;0),(0,5;1),(0;0,5),(1;0,5)). Une vraie map de
        //     perspective 2D — le modèle séparable sy/sx ignorait le couplage de
        //     perspective (la transcription user prouve centre ≠ intersection des
        //     axes) et produisait une énorme avance du curseur. Le clic CENTRE devient
        //     un contrôle qualité (H(centre) doit être ≈ (0,5 ; 0,5)).
        private float[] _twbHomography = null;
        // [V59-FIX-11] EN: EFFECTIVE layout — Wiimote Bar + the hybrid checkbox behaves
        //     as PNP-Hybrid; every runtime check must test this (not _ledLayout).
        //     FR: Layout EFFECTIF — Wiimote Bar + la case hybride se comporte comme
        //     PNP-Hybrid ; tout contrôle d'exécution doit tester ceci (pas _ledLayout).
        private LEDLayoutType EffLayout
        {
            get { return (_ledLayout == LEDLayoutType.WiimoteBar && Options.Instance.WiimoteBarHybridPnp) ? LEDLayoutType.WiimoteBarPnP : _ledLayout; }
        }
        // [V59-FIX-8e] EN: Degraded-mode anchors — last valid bar midpoints and
        //     within-bar LED spans (measured on 4-LED frames) let the mode RECONSTRUCT
        //     approximate midpoints when 1-2 LEDs remain at the FOV edge, instead of
        //     the generic Double-T 2-point pairing which misreads "one top + one
        //     bottom LED" as a single bar and bounces the cursor inward (user report).
        //     FR: Ancres du mode dégradé — derniers milieux de barres valides et
        //     portées LED intra-barre (mesurées sur les images à 4 LEDs) pour
        //     RECONSTRUIRE des milieux approximatifs quand 1-2 LEDs restent au bord du
        //     FOV, au lieu du pairing Double-T générique qui lit « une LED haute + une
        //     LED basse » comme une seule barre et fait rebondir le curseur vers
        //     l'intérieur (rapport user).
        private Point2F _twbLastTopMid;
        private Point2F _twbLastBotMid;
        private float _twbTopSpan = 0f;
        private float _twbBotSpan = 0f;

        // [V59-FIX-28] EN: Persistent 4-point trapezoidal Quad tracking for TwoWiimoteBar.
        //     Handles tilted screens (arcade cabinets) and perspective convergence without distortion.
        //     FR: Suivi Quad trapézoïdal persistant à 4 points pour TwoWiimoteBar (écrans arcade inclinés).
        private Point2F _twbTL = new Point2F(0.35f, 0.30f);
        private Point2F _twbTR = new Point2F(0.65f, 0.30f);
        private Point2F _twbBR = new Point2F(0.65f, 0.70f);
        private Point2F _twbBL = new Point2F(0.35f, 0.70f);
        private bool _twbHaveQuad = false;
        private Point2F _twbVTop = new Point2F(0.30f, 0f);
        private Point2F _twbVBot = new Point2F(0.30f, 0f);
        private Point2F _twbVLeft = new Point2F(0f, 0.40f);
        private Point2F _twbVRight = new Point2F(0f, 0.40f);
        private float _twbLeftSpan = 0.40f;
        private float _twbRightSpan = 0.40f;

        // v20: Restored Double-T Tracking Method (1D Horizontal Bars)
        // Mathematically predicts M1/M2 avoiding visual jumping or inverted axes.
        // (EN/FR: Méthode de suivi Double-T restaurée. Prédit M1/M2 évitant les sauts.)
        private bool TryCalculateDoubleTCenter(List<Point2F> pts, out Point2F center)
        {
            center = new Point2F(0.5f, 0.5f);
            if (pts.Count == 0) return false;
            
            // 1 point: Return false to fallback to relative delta tracking
            if (pts.Count == 1) return false;

            if (pts.Count == 4)
            {
                // [V59-FIX-28] Robust angle-based identification around centroid
                // Immune to gun roll and screen tilt (arcade cabinets)
                float cx = (pts[0].X + pts[1].X + pts[2].X + pts[3].X) * 0.25f;
                float cy = (pts[0].Y + pts[1].Y + pts[2].Y + pts[3].Y) * 0.25f;
                var byAngle = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                Point2F tl = byAngle[0];
                Point2F tr = byAngle[1];
                Point2F br = byAngle[2];
                Point2F bl = byAngle[3];

                Point2F m1 = new Point2F((tl.X + tr.X) * 0.5f, (tl.Y + tr.Y) * 0.5f);
                Point2F m2 = new Point2F((bl.X + br.X) * 0.5f, (bl.Y + br.Y) * 0.5f);

                _twbTL = tl; _twbTR = tr; _twbBR = br; _twbBL = bl;
                _twbHaveQuad = true;

                _twbTopSpan = GetDistance(tl, tr);
                _twbBotSpan = GetDistance(bl, br);
                _twbLeftSpan = GetDistance(tl, bl);
                _twbRightSpan = GetDistance(tr, br);

                _twbVTop = new Point2F(tr.X - tl.X, tr.Y - tl.Y);
                _twbVBot = new Point2F(br.X - bl.X, br.Y - bl.Y);
                _twbVLeft = new Point2F(bl.X - tl.X, bl.Y - tl.Y);
                _twbVRight = new Point2F(br.X - tr.X, br.Y - tr.Y);

                float avgW = (_twbTopSpan + _twbBotSpan) * 0.5f;
                float dist = GetDistance(m1, m2);
                if (avgW > 0.001f) _doubleTRatio = dist / avgW;

                _lastM1 = m1;
                _lastM2 = m2;
                _doubleTInitialized = true;

                center = new Point2F((m1.X + m2.X) * 0.5f, (m1.Y + m2.Y) * 0.5f);
                return true;
            }

            if (!_doubleTInitialized)
            {
                // Fallback if not initialized
                float cx = 0, cy = 0;
                foreach (var p in pts) { cx += p.X; cy += p.Y; }
                center = new Point2F(cx / pts.Count, cy / pts.Count);
                return true;
            }

            if (pts.Count == 3)
            {
                // 3 points: Find the intact bar (the two points with smallest Y difference)
                var sortedByY = pts.OrderBy(p => p.Y).ToList();
                float dy01 = Math.Abs(sortedByY[1].Y - sortedByY[0].Y);
                float dy12 = Math.Abs(sortedByY[2].Y - sortedByY[1].Y);

                Point2F intactM;
                bool isM1;
                Point2F pA, pB;

                if (dy01 < dy12)
                {
                    // Bar is points 0 and 1
                    pA = sortedByY[0]; pB = sortedByY[1];
                }
                else
                {
                    // Bar is points 1 and 2
                    pA = sortedByY[1]; pB = sortedByY[2];
                }
                intactM = new Point2F((pA.X + pB.X) / 2.0f, (pA.Y + pB.Y) / 2.0f);

                // Which bar is it? M1 or M2? Compare to memory
                if (GetDistance(intactM, _lastM1) < GetDistance(intactM, _lastM2))
                {
                    _lastM1 = intactM;
                    isM1 = true;
                }
                else
                {
                    _lastM2 = intactM;
                    isM1 = false;
                }

                // Predict the missing bar's midpoint
                float w = GetDistance(pA, pB);
                float expectedDist = w * _doubleTRatio;

                // Perpendicular vector
                float dx = pB.X - pA.X;
                float dy = pB.Y - pA.Y;
                float len = (float)Math.Sqrt(dx*dx + dy*dy);
                if (len < 0.001f) len = 0.001f;
                float nx = -dy / len;
                float ny = dx / len;

                // Ensure normal points from M1 to M2
                float oldDx = _lastM2.X - _lastM1.X;
                float oldDy = _lastM2.Y - _lastM1.Y;
                if (nx * oldDx + ny * oldDy < 0)
                {
                    nx = -nx; ny = -ny;
                }

                if (isM1)
                    _lastM2 = new Point2F(_lastM1.X + nx * expectedDist, _lastM1.Y + ny * expectedDist);
                else
                    _lastM1 = new Point2F(_lastM2.X - nx * expectedDist, _lastM2.Y - ny * expectedDist);

                center = new Point2F((_lastM1.X + _lastM2.X) / 2.0f, (_lastM1.Y + _lastM2.Y) / 2.0f);
                return true;
            }

            if (pts.Count == 2)
            {
                // 2 points: Assume it's an intact horizontal bar
                Point2F m = new Point2F((pts[0].X + pts[1].X) / 2.0f, (pts[0].Y + pts[1].Y) / 2.0f);
                
                // Which bar is it?
                bool isM1 = GetDistance(m, _lastM1) < GetDistance(m, _lastM2);
                if (isM1) _lastM1 = m; else _lastM2 = m;

                float w = GetDistance(pts[0], pts[1]);
                float expectedDist = w * _doubleTRatio;

                float dx = pts[1].X - pts[0].X;
                float dy = pts[1].Y - pts[0].Y;
                float len = (float)Math.Sqrt(dx*dx + dy*dy);
                if (len < 0.001f) len = 0.001f;
                float nx = -dy / len;
                float ny = dx / len;

                float oldDx = _lastM2.X - _lastM1.X;
                float oldDy = _lastM2.Y - _lastM1.Y;
                if (nx * oldDx + ny * oldDy < 0) { nx = -nx; ny = -ny; }

                if (isM1)
                    _lastM2 = new Point2F(_lastM1.X + nx * expectedDist, _lastM1.Y + ny * expectedDist);
                else
                    _lastM1 = new Point2F(_lastM2.X - nx * expectedDist, _lastM2.Y - ny * expectedDist);

                center = new Point2F((_lastM1.X + _lastM2.X) / 2.0f, (_lastM1.Y + _lastM2.Y) / 2.0f);
                return true;
            }

            return false;
        }

        private Point2F CalculateMultiLEDPosition(IRState ir, ref bool hasSensor)
        {
            // Gun4IR & 4-Corners: Robust Hybrid Tracking
            // 1. Absolute Geometry: Use when 3+ points or 2 diagonal points are visible.
            // 2. Relative Motion: Use when only 1 point or 2 side points are visible (prevent jumps).
            
            Point2F relativePosition = new Point2F();
            var currentPoints = new Dictionary<int, Point2F>();

            if (ir.IRSensor0.Found) currentPoints[0] = ir.IRSensor0.Position;
            if (ir.IRSensor1.Found) currentPoints[1] = ir.IRSensor1.Position;
            if (ir.IRSensor2.Found) currentPoints[2] = ir.IRSensor2.Position;
            if (ir.IRSensor3.Found) currentPoints[3] = ir.IRSensor3.Position;

            int count = currentPoints.Count;
            bool useAbsolute = false;
            Point2F absoluteCenter = new Point2F();

            // v16: Early Intercept for TwoWiimoteBar and FourCorners
            if ((_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners) && count > 0)
            {
                // [V59-FIX-30] Active homography & off-screen checks ONLY in normal gameplay mode
                // when a valid homography exists AND calibration is NOT active!
                // During calibration (_calibrateForm != null) or before calibration (_twbHomography == null),
                // the gun must use raw absolute tracking with hasSensor = true so the user can aim and click targets!
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar && _calibrateForm == null && _twbHomography != null)
                {
                    if (count >= 2 && count <= 3)
                    {
                        Point2F twbDegCursor;
                        if (TryMapTwoBarDegraded(currentPoints.Values.ToList(), out twbDegCursor))
                        {
                            _twbMapped = true;
                            _lastValidCenter = new Point2F((_lastM1.X + _lastM2.X) / 2f, (_lastM1.Y + _lastM2.Y) / 2f);
                            _wasUsingRelativeTracking = false;
                            _framesSinceTransition = 0;
                            hasSensor = true;
                            return twbDegCursor;
                        }
                        else
                        {
                            // Aim is off-screen! Declare off-screen state cleanly
                            _twbMapped = false;
                            hasSensor = false;
                            return twbDegCursor;
                        }
                    }
                    else if (count < 2)
                    {
                        // 0 or 1 LED in 4-LED TwoWiimoteBar: strictly off-screen
                        _twbMapped = false;
                        hasSensor = false;
                        return new Point2F(0.5f, 0.5f);
                    }
                }

                if (TryCalculateDoubleTCenter(currentPoints.Values.ToList(), out absoluteCenter))
                {
                    // [V59-FIX-14] EN: Live TWB distance — from the current bar pair
                    //     separation (m1↔m2) and the DECLARED geometry. Works BEFORE
                    //     calibration (user request: the distance must display live
                    //     from the moment LEDs are visible, not just after clicking).
                    //     FR: Distance TWB en direct — depuis la séparation courante
                    //     des barres (m1↔m2) et la géométrie DÉCLARÉE. Fonctionne
                    //     AVANT calibration (demande user : la distance doit
                    //     s'afficher en direct dès que les LEDs sont visibles).
                    if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                    {
                        float barSpan = (_twbTopSpan > 0.005f && _twbBotSpan > 0.005f)
                            ? (_twbTopSpan + _twbBotSpan) * 0.5f
                            : (_twbTopSpan > 0.005f ? _twbTopSpan : _twbBotSpan);
                        if (barSpan > 0.005f)
                        {
                            GetPnpGeometry(out float spacingMM, out float focalNorm);
                            _twbLastDistanceMLive = (spacingMM / 1000f) * focalNorm / barSpan;
                        }
                    }
                    // [V59-FIX-8c] EN: TwoWiimoteBars — map the cursor from the CURRENT
                    //     bar midpoints (_lastM1/_lastM2, just computed) through the
                    //     cross-calibrated trajectories. Replaces the old Double-T
                    //     center + corner bilinear, which bounced on LED loss. Each bar
                    //     alone can produce the full cursor estimate.
                    //     FR: TwoWiimoteBars — mappe le curseur depuis les milieux de
                    //     barres COURANTS (_lastM1/_lastM2, tout juste calculés) via les
                    //     trajectoires de la calibration croix. Remplace l'ancien centre
                    //     Double-T + bilinéaire à coins, qui rebondissait à la perte de
                    //     LED. Chaque barre seule peut produire l'estimation complète.
                    if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                    {
                        // [V59-FIX-8e/28] EN/FR: refresh the degraded-mode anchors on full 4-LED frames.
                        if (count == 4)
                        {
                            _twbLastTopMid = _lastM1;
                            _twbLastBotMid = _lastM2;
                        }

                        // [V59-FIX-30] Only map through homography if calibrated and NOT calibrating!
                        if (_calibrateForm == null && _twbHomography != null)
                        {
                            Point2F twbCursor;
                            // [V59-FIX-21] EN: Flip X to match _twbCalCenter used during homography training!
                            //     FR: Flipper X pour correspondre aux _twbCalCenter utilisés lors de la calibration !
                            Point2F flippedCenter = new Point2F(1.0f - absoluteCenter.X, absoluteCenter.Y);
                            Point2F flippedM1 = new Point2F(1.0f - _lastM1.X, _lastM1.Y);
                            Point2F flippedM2 = new Point2F(1.0f - _lastM2.X, _lastM2.Y);
                            if (TryMapTwoBar(flippedCenter, flippedM1, flippedM2, true, true, out twbCursor))
                            {
                                _twbMapped = true;
                                // [V59-FIX-25] EN: Keep _lastValidCenter as the physical camera center (absoluteCenter).
                                //     FR: Conserver _lastValidCenter comme centre caméra physique (absoluteCenter).
                                _lastValidCenter = absoluteCenter;
                                _wasUsingRelativeTracking = false;
                                _framesSinceTransition = 0;
                                hasSensor = true;
                                return twbCursor;
                            }
                            else
                            {
                                // [V59-FIX-29] Off-screen detected!
                                _twbMapped = false;
                                hasSensor = false;
                                return twbCursor;
                            }
                        }
                    }

                    // Update max observed diagonal for distance compensation
                    if (count == 4)
                    {
                        var ptsList = currentPoints.Values.ToList();
                        float maxDist = 0;
                        for (int i = 0; i < ptsList.Count; i++)
                            for (int j = i + 1; j < ptsList.Count; j++)
                            {
                                float d = GetDistance(ptsList[i], ptsList[j]);
                                if (d > maxDist) maxDist = d;
                            }
                        if (maxDist > _maxObservedDiagonal) _maxObservedDiagonal = maxDist;
                    }

                    _lastValidCenter = absoluteCenter;
                    _wasUsingRelativeTracking = false;
                    _framesSinceTransition = 0;
                    hasSensor = true;
                    return absoluteCenter; // Early return!
                }
            }

            // Update Max Diagonal and Dimensions if 4 points
            if (count == 4)
            {
                var pts = currentPoints.Values.ToList();
                // Check diagonals (0-2, 0-3, etc.) - find max distance
                float maxDist = 0;
                for(int i=0; i<pts.Count; i++)
                    for(int j=i+1; j<pts.Count; j++)
                    {
                        float d = GetDistance(pts[i], pts[j]);
                        if (d > maxDist) maxDist = d;
                    }
                if (maxDist > _maxObservedDiagonal) _maxObservedDiagonal = maxDist;

                if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // Calculate Centroid (EN/FR: Calculer le centroïde)
                    float cx = 0, cy = 0;
                    foreach (var p in pts) { cx += p.X; cy += p.Y; }
                    cx /= 4; cy /= 4;

                    // v10: Robust Angle-Based Identification
                    var sortedByAngle = currentPoints.OrderBy(kvp => Math.Atan2(kvp.Value.Y - cy, kvp.Value.X - cx)).ToList();
                    
                    // Order: index 0: BR (~ -135), index 1: BL (~ -45), index 2: TL (~ +45), index 3: TR (~ +135)
                    _idBottomRight = sortedByAngle[0].Key;
                    _idBottomLeft = sortedByAngle[1].Key;
                    _idTopLeft = sortedByAngle[2].Key;
                    _idTopRight = sortedByAngle[3].Key;

                    // v10: Scalable Dimension & Ratio Learning
                    float wTop = GetDistance(currentPoints[_idTopLeft], currentPoints[_idTopRight]);
                    float wBottom = GetDistance(currentPoints[_idBottomLeft], currentPoints[_idBottomRight]);
                    float hLeft = GetDistance(currentPoints[_idTopLeft], currentPoints[_idBottomLeft]);
                    float hRight = GetDistance(currentPoints[_idTopRight], currentPoints[_idBottomRight]);

                    _observedWidth = (wTop + wBottom) / 2.0f;
                    _observedHeight = (hLeft + hRight) / 2.0f;
                    // _refRatio completely removed in v16

                    // SimpleLogger.Instance.Info(...)
                }

                // NOTE: _lastIRSpan clobbering removed (v5) - We now exclusively use GetLayoutAwareSpan(ir)
                // calculated at the beginning of GetScaledPosition for consistency across all modes.
                // (EN/FR: Écrasement de _lastIRSpan supprimé - Utilisation exclusive de GetLayoutAwareSpan(ir))
            }

            // ... (Rest of the file) ...

            // 1-Point tracking falls back to relative movement.
            if (count == 4)
            {
                // ... (Homography Logic) ...
            }
            
            if (useAbsolute)
            {
                relativePosition = absoluteCenter;
                // Update last valid center (already in normalized coords)
                _lastValidCenter = absoluteCenter;
                _wasUsingRelativeTracking = false;
                _framesSinceTransition = 0;
                hasSensor = true;
                
            }
            else if (count > 0)
            {
                // ...
            }
            else
            {
                // No points visible
            }


            if (count == 4)
            {
                var pts = currentPoints.Values.ToList();
                float cx = 0, cy = 0;
                foreach (var p in pts) { cx += p.X; cy += p.Y; }
                cx /= 4; cy /= 4;

                if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                {
                    // Projective Center (Intersection of Diagonals)
                    var sortedPts = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                    Point2F p1 = sortedPts[0]; Point2F p2 = sortedPts[2]; Point2F p3 = sortedPts[1]; Point2F p4 = sortedPts[3];
                    Point2F? intersection = GetLineIntersection(p1, p2, p3, p4);

                    if (intersection.HasValue) absoluteCenter = intersection.Value;
                    else absoluteCenter = new Point2F(cx, cy);
                    
                    useAbsolute = true;
                }
                else
                {
                    absoluteCenter = new Point2F(cx, cy);
                    useAbsolute = true;
                }
            }
            else if (count == 3)
            {
                var pts = currentPoints.Values.ToList();
                
                if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                {
                    // Gun4IR (Diamond): Robust 3-Point Logic
                    // (EN/FR: Gun4IR (Diamant) : Logique 3 points robuste)
                    // Instead of "Longest Diagonal" (which fails at angles), we identify points.
                    
                    // 1. Calculate Centroid
                    float cx = 0, cy = 0;
                    foreach (var p in pts) { cx += p.X; cy += p.Y; }
                    cx /= pts.Count;
                    cy /= pts.Count;

                    // 2. Identify Points based on relative position to centroid
                    // Top: Y < cy, X ~ cx
                    // Bottom: Y > cy, X ~ cx
                    // Left: X > cx, Y ~ cy (Camera Coords: Left is High X)
                    // Right: X < cx, Y ~ cy (Camera Coords: Right is Low X)
                    

                    
                    // Sort by Angle to help identification? Or just Quadrants?
                    // Let's use simple X/Y checks relative to centroid.
                    // But with 3 points, the centroid is shifted.
                    
                    // Better approach: Sort by Y to find Top/Bottom candidates.
                    var sortedByY = pts.OrderBy(p => p.Y).ToList();
                    var sortedByX = pts.OrderBy(p => p.X).ToList();
                    
                    // Heuristic: 
                    // The point with lowest Y is likely Top.
                    // The point with highest Y is likely Bottom.
                    // The point with lowest X is likely Right.
                    // The point with highest X is likely Left.
                    
                    // Check if we have a Vertical Pair (Top/Bottom)
                    // If the distance between Lowest Y and Highest Y is significant
                    // AND they have similar X values (relative to the width of the set).
                    
                    Point2F pTop = sortedByY[0];
                    Point2F pBottom = sortedByY[sortedByY.Count - 1];
                    
                    Point2F pRight = sortedByX[0];
                    Point2F pLeft = sortedByX[sortedByX.Count - 1];
                    
                    // Check Vertical Diagonal (Top-Bottom)
                    // They should be the same points as pTop and pBottom
                    // And their X difference should be small compared to Y difference?
                    // No, at an angle, X diff might be large.
                    
                    // Let's rely on the fact that in a Diamond, Top and Bottom are "Opposite".
                    // If we have 3 points, we either have (Top, Bottom, Left) or (Top, Bottom, Right) or (Left, Right, Top) or (Left, Right, Bottom).
                    // In the first two cases, Top and Bottom are present.
                    // In the last two cases, Left and Right are present.
                    
                    // We prefer the pair that is "More Opposite".
                    float distVert = GetDistance(pTop, pBottom);
                    float distHorz = GetDistance(pLeft, pRight);
                    
                    if (distVert > distHorz)
                    {
                        // Vertical Diagonal is longer -> Use Top/Bottom
                        absoluteCenter.X = (pTop.X + pBottom.X) / 2;
                        absoluteCenter.Y = (pTop.Y + pBottom.Y) / 2;
                    }
                    else
                    {
                        // Horizontal Diagonal is longer -> Use Left/Right
                        absoluteCenter.X = (pLeft.X + pRight.X) / 2;
                        absoluteCenter.Y = (pLeft.Y + pRight.Y) / 2;
                    }
                    
                    useAbsolute = true;
                }
            }
            else if (count == 2)
            {
                // Keep IDs (Key) and Positions (Value)
                var pts = currentPoints.ToList(); 
                float dist = GetDistance(pts[0].Value, pts[1].Value);

                if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                {
                    // Gun4IR (Diamond): Check for Vertical or Horizontal pairs
                    // (EN/FR: Gun4IR (Diamant) : Vérifier paires Verticales ou Horizontales)
                    
                    bool isVertical = false;
                    bool isHorizontal = false;
                    
                    // Use observed dimensions to identify the pair
                    if (_observedHeight > 0 && Math.Abs(dist - _observedHeight) < _observedHeight * 0.2f)
                    {
                        isVertical = true; // Likely Top-Bottom
                    }
                    else if (_observedWidth > 0 && Math.Abs(dist - _observedWidth) < _observedWidth * 0.2f)
                    {
                        isHorizontal = true; // Likely Left-Right
                    }
                    else
                    {
                        // Fallback to Angle Check if dimensions not learned yet
                        float dx = Math.Abs(pts[1].Value.X - pts[0].Value.X);
                        float dy = Math.Abs(pts[1].Value.Y - pts[0].Value.Y);
                        
                        // Vertical: Small dx, Large dy
                        if (dy > dx * 2) isVertical = true;
                        
                        // Horizontal: Small dy, Large dx
                        if (dx > dy * 2) isHorizontal = true;
                    }

                    if (isVertical || isHorizontal)
                    {
                        // Midpoint is the center
                        absoluteCenter.X = (pts[0].Value.X + pts[1].Value.X) / 2;
                        absoluteCenter.Y = (pts[0].Value.Y + pts[1].Value.Y) / 2;
                        useAbsolute = true;
                    }
                }
                else
                {
                    // WiimoteBar or others: Use diagonal detection (Legacy)
                    bool isDiagonal = false;
                    
                    if (_maxObservedDiagonal > 0)
                    {
                        float threshold = _wasUsingRelativeTracking ? 0.75f : 0.80f;
                        if (dist > _maxObservedDiagonal * threshold)
                        {
                            float dx = Math.Abs(pts[1].Value.X - pts[0].Value.X);
                            float dy = Math.Abs(pts[1].Value.Y - pts[0].Value.Y);
                            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                            if ((angle >= 30 && angle <= 60) || (angle >= 120 && angle <= 150))
                            {
                                isDiagonal = true;
                            }
                        }
                    }

                    if (isDiagonal)
                    {
                        absoluteCenter.X = (pts[0].Value.X + pts[1].Value.X) / 2;
                        absoluteCenter.Y = (pts[0].Value.Y + pts[1].Value.Y) / 2;
                        useAbsolute = true;
                    }
                }

            }
            else
            {
                // 0 or 1 point: Relative tracking only
                useAbsolute = false;
            }

            if (useAbsolute)
            {
                relativePosition = absoluteCenter;
                // Update last valid center (already in normalized coords)
                _lastValidCenter = absoluteCenter;
                _wasUsingRelativeTracking = false;
                _framesSinceTransition = 0;
                hasSensor = true;
            }
            else if (count > 0)
            {
                // CRITICAL: FourCorners (2-Bar) should NOT use relative tracking
                // It causes drift and teleportation. Better to disable cursor.
                // (EN/FR: CRITIQUE : FourCorners (2-Bar) ne doit PAS utiliser le tracking relatif)
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // Permissive Calibration: Allow 1+ points if calibrating
                    // (EN/FR: Calibration permissive : Autoriser 1+ points si en calibration)
                    if (_calibrateForm != null)
                    {
                        if (currentPoints.Count == 1 && _observedWidth > 0 && _observedHeight > 0)
                        {
                            // Smart Single-Point Calibration using Learned IDs
                            // (EN/FR: Calibration intelligente à 1 point via IDs appris)
                            var kvp = currentPoints.First();
                            int id = kvp.Key;
                            Point2F p = kvp.Value;
                            
                            float halfW = _observedWidth / 2.0f;
                            float halfH = _observedHeight / 2.0f;
                            
                            bool handled = false;

                            if (id == _idTopLeft)
                            {
                                absoluteCenter.X = p.X + halfW;
                                absoluteCenter.Y = p.Y - halfH; // Top: Subtract H to move Center Up (towards 0)
                                handled = true;
                            }
                            else if (id == _idTopRight)
                            {
                                absoluteCenter.X = p.X - halfW;
                                absoluteCenter.Y = p.Y - halfH; // Top: Subtract H
                                handled = true;
                            }
                            else if (id == _idBottomRight)
                            {
                                absoluteCenter.X = p.X - halfW;
                                absoluteCenter.Y = p.Y + halfH; // Bottom: Add H to move Center Down (towards 1)
                                handled = true;
                            }
                            else if (id == _idBottomLeft)
                            {
                                absoluteCenter.X = p.X + halfW;
                                absoluteCenter.Y = p.Y + halfH; // Bottom: Add H
                                handled = true;
                            }

                            if (handled)
                            {
                                SimpleLogger.Instance.Info(string.Format("[Calib-1Pt] ID:{0} Raw:({1:F3},{2:F3}) Dims:({3:F3}x{4:F3}) -> Center:({5:F3},{6:F3})", id, p.X, p.Y, _observedWidth, _observedHeight, absoluteCenter.X, absoluteCenter.Y));
                                useAbsolute = true;
                                hasSensor = true;
                            }
                            else
                            {
                                // Fallback to centroid if ID unknown
                                SimpleLogger.Instance.Info(string.Format("[Calib-1Pt] ID:{0} Unknown! Fallback to Raw. Dims:({1:F3}x{2:F3})", id, _observedWidth, _observedHeight));
                                
                                // Visual Snap: If we don't know dimensions/ID, snap cursor to target corner
                                // so user can click. Data saved will be RAW (_lastRawPoint).
                                // (EN/FR: Snap visuel : Si dims/ID inconnus, coller curseur au coin cible)
                                if (_calibrateForm != null)
                                {
                                    // Get current step from form (0=TL, 1=TR, 2=BR, 3=BL)
                                    // We can't access private _currentCalibrationStep, but we can infer from _gun4irPoints count
                                    int step = 0;
                                    for(int i=0; i<4; i++) if (_gun4irPoints[i].HasValue) step++;
                                    
                                    if (step == 0) { absoluteCenter = new Point2F(0, 0); } // TL
                                    else if (step == 1) { absoluteCenter = new Point2F(1, 0); } // TR
                                    else if (step == 2) { absoluteCenter = new Point2F(1, 1); } // BR
                                    else if (step == 3) { absoluteCenter = new Point2F(0, 1); } // BL
                                    else { absoluteCenter = p; }
                                }
                                else
                                {
                                    absoluteCenter = p;
                                }

                                useAbsolute = true;
                                hasSensor = true;
                            }
                        }
                        else
                        {
                            // Use centroid of visible points (Fallback for >1 points or unknown dims)
                            float cx = 0, cy = 0;
                            int c = 0;
                            foreach(var kvp in currentPoints)
                            {
                                cx += kvp.Value.X;
                                cy += kvp.Value.Y;
                                c++;
                            }
                            if (c > 0)
                            {
                                absoluteCenter.X = cx / c;
                                absoluteCenter.Y = cy / c;
                                useAbsolute = true;
                                hasSensor = true; 
                            }
                        }
                    }
                    else
                    {
                        // 0 or 1 point for 2-Bar = not enough data -> disable cursor
                        useAbsolute = false;
                        hasSensor = false;
                    }
                }
                else
                {
                    // Relative Tracking for other layouts (WiimoteBar, Gun4IR)
                    // Apply average delta of visible points to last known center
                    float totalDeltaX = 0;
                    float totalDeltaY = 0;
                    int trackedPoints = 0;

                    foreach (var kvp in currentPoints)
                    {
                        if (_lastFramePoints.ContainsKey(kvp.Key))
                        {
                            Point2F prev = _lastFramePoints[kvp.Key];
                            totalDeltaX += kvp.Value.X - prev.X;
                            totalDeltaY += kvp.Value.Y - prev.Y;
                            trackedPoints++;
                        }
                    }

                    if (trackedPoints > 0)
                    {
                        relativePosition.X = _lastValidCenter.X + (totalDeltaX / trackedPoints);
                        relativePosition.Y = _lastValidCenter.Y + (totalDeltaY / trackedPoints);
                        
                        // Update last valid center for next frame continuity
                        _lastValidCenter = relativePosition;
                        _wasUsingRelativeTracking = true;
                        _framesSinceTransition++;
                        hasSensor = true;
                    }
                    else
                    {
                        // No tracked points (first frame in relative mode or all IDs changed)
                        // Use average of current visible points as new baseline
                        float sumX = 0, sumY = 0;
                        foreach (var pt in currentPoints.Values)
                        {
                            sumX += pt.X;
                            sumY += pt.Y;
                        }
                        relativePosition.X = sumX / currentPoints.Count;
                        relativePosition.Y = sumY / currentPoints.Count;
                        
                        _lastValidCenter = relativePosition;
                        _wasUsingRelativeTracking = true;
                        _framesSinceTransition = 0;
                        hasSensor = true;
                    }
                }
            }
            else
            {
                hasSensor = false;
            }

            // Apply exponential smoothing to reduce jitter
            // Stronger smoothing right after mode transition, lighter during stable tracking
            float smoothFactor = 0.3f; // Default: 30% new, 70% old
            if (_framesSinceTransition < 5)
            {
                // Increase smoothing for first few frames after transition
                smoothFactor = 0.15f; // 15% new, 85% old
            }
            
            relativePosition.X = relativePosition.X * smoothFactor + _lastSmoothedCenter.X * (1.0f - smoothFactor);
            relativePosition.Y = relativePosition.Y * smoothFactor + _lastSmoothedCenter.Y * (1.0f - smoothFactor);
            _lastSmoothedCenter = relativePosition;

            // Update history
            _lastFramePoints = currentPoints;

            return relativePosition;
        }

        // Apply distance compensation to a mapped position (manual calibration modes ONLY)
        // When the player moves closer/farther from the screen after calibration, the IR repere
        // appears larger/smaller. We scale the cursor position around the center to compensate.
        // (EN/FR: Appliquer compensation de distance à une position mappée (modes calibration manuelle UNIQUEMENT)
        // Quand le joueur s'approche/s'éloigne après calibration, le repère IR paraît plus grand/petit.
        // On redimensionne la position du curseur autour du centre pour compenser.)
        private Point2F ApplyDistanceCompensation(Point2F mappedPos, float currentSpan)
        {
            // Guard: need valid calibration reference and current measurement
            // (EN/FR: Garde : il faut une référence de calibration valide et une mesure courante)
            if (_calibrationIRSpan <= 0f || currentSpan <= 0f)
                return mappedPos;

            float distanceFactor = currentSpan / _calibrationIRSpan;

            // Only apply when change is above threshold to prevent noise amplification
            // (EN/FR: Appliquer uniquement si le changement dépasse le seuil pour éviter d'amplifier le bruit)
            if (Math.Abs(distanceFactor - 1.0f) < DistanceCompensationThreshold)
                return mappedPos;

            // Scale around center (0.5, 0.5): closer = larger span = compress; farther = smaller span = expand
            // (EN/FR: Mise à l'échelle autour du centre (0.5, 0.5) : plus proche = repère plus grand = compression; plus loin = expansion)
            float cx = mappedPos.X - 0.5f;
            float cy = mappedPos.Y - 0.5f;

            Point2F compensated = new Point2F(
                cx / distanceFactor + 0.5f,
                cy / distanceFactor + 0.5f
            );

            SimpleLogger.Instance.Info(string.Format(
                "[P{0}] DistComp: CalibSpan={1:F3} CurSpan={2:F3} Factor={3:F3} ({4:F3},{5:F3})->({6:F3},{7:F3})",
                _playerIndex, _calibrationIRSpan, currentSpan, distanceFactor,
                mappedPos.X, mappedPos.Y, compensated.X, compensated.Y));

            return compensated;
        }

        private Point2F NormalizePoint(Point2F p, float factor)
        {
            if (factor <= 0.001f) return p;
            return new Point2F(0.5f + (p.X - 0.5f) / factor, 0.5f + (p.Y - 0.5f) / factor);
        }

        /// <summary>
        /// Robustly calculates the "scale" of the IR constellation using mean distance to centroid.
        /// (EN/FR: Calcule de manière robuste l'échelle de la constellation IR via la distance moyenne au centroïde.)
        /// </summary>
        private float GetLayoutAwareSpan(IRState ir)
        {
            List<Point2F> pts = new List<Point2F>();
            if (ir.IRSensor0.Found) pts.Add((Point2F)ir.IRSensor0.RawPosition);
            if (ir.IRSensor1.Found) pts.Add((Point2F)ir.IRSensor1.RawPosition);
            if (ir.IRSensor2.Found) pts.Add((Point2F)ir.IRSensor2.RawPosition);
            if (ir.IRSensor3.Found) pts.Add((Point2F)ir.IRSensor3.RawPosition);

            if (pts.Count < 2) return 0f;

            // Centroid (average position)
            float cx = pts.Average(p => p.X);
            float cy = pts.Average(p => p.Y);
            Point2F centroid = new Point2F(cx, cy);

            // Mean distance of all points to their common centroid
            float totalDist = pts.Sum(p => GetDistance(p, centroid));
            return (totalDist / pts.Count) / 1024f; // Normalize to 0-1 range
        }

        private float GetDistance(Point2F p1, Point2F p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        // Calculate angle at p1 (between p1-p2 and p1-p3) in degrees
        private float GetAngle(Point2F p1, Point2F p2, Point2F p3)
        {
            float a = GetDistance(p2, p3);
            float b = GetDistance(p1, p3);
            float c = GetDistance(p1, p2);
            
            // Law of Cosines: a^2 = b^2 + c^2 - 2bc cos(A)
            // cos(A) = (b^2 + c^2 - a^2) / (2bc)
            float val = (b*b + c*c - a*a) / (2*b*c);
            
            // Clamp to -1..1 to avoid NaN
            if (val < -1) val = -1;
            if (val > 1) val = 1;
            
            return (float)(Math.Acos(val) * 180.0 / Math.PI);
        }



        // Compute 3x3 Homography Matrix from 4 point correspondences
        // Returns float[8] representing the matrix (h33 = 1.0)
        // [V59-FIX-8d] EN: Least-squares homography from N (>=4) point correspondences
        //     (src = camera space, dst = screen space — same convention as
        //     ComputeHomography). With 5 points (the TWB cross: center + 4 edges) the
        //     fit averages the aim noise and the CENTER click contributes to the
        //     mapping exactly like the edge clicks — the initial BlancoM principle.
        //     FR: Homographie aux MOINDRES CARRÉS depuis N (>=4) correspondences
        //     (src = espace caméra, dst = espace écran — même convention que
        //     ComputeHomography). Avec 5 points (la croix TWB : centre + 4 bords),
        //     l'ajustement moyenne le bruit de visée et le clic CENTRE contribue au
        //     mapping exactement comme les clics de bord — le principe initial BlancoM.
        private float[] ComputeHomographyLeastSquares(Point2F[] src, Point2F[] dst)
        {
            int n = src.Length;
            if (n < 4) return null;

            // Normal equations (A^T A) h = A^T b — A is (2n x 8), b is (2n)
            // Row pair per correspondence (u = dst.X, v = dst.Y):
            //   [x, y, 1, 0, 0, 0, -u*x, -u*y] | u
            //   [0, 0, 0, x, y, 1, -v*x, -v*y] | v
            float[,] ata = new float[8, 8];
            float[] atb = new float[8];

            for (int i = 0; i < n; i++)
            {
                float x = src[i].X, y = src[i].Y;
                float u = dst[i].X, v = dst[i].Y;
                float[] r1 = { x, y, 1f, 0f, 0f, 0f, -u * x, -u * y };
                float[] r2 = { 0f, 0f, 0f, x, y, 1f, -v * x, -v * y };
                AccumulateNormalRow(ata, atb, r1, u);
                AccumulateNormalRow(ata, atb, r2, v);
            }

            // Augmented 8x9 system + Gaussian elimination with partial pivoting
            float[,] aug = new float[8, 9];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++) aug[i, j] = ata[i, j];
                aug[i, 8] = atb[i];
            }
            for (int col = 0; col < 8; col++)
            {
                int piv = col;
                for (int r = col + 1; r < 8; r++)
                    if (Math.Abs(aug[r, col]) > Math.Abs(aug[piv, col])) piv = r;
                if (Math.Abs(aug[piv, col]) < 1e-10f) return null; // singular / degenerate clicks
                if (piv != col)
                {
                    for (int c = 0; c < 9; c++)
                    {
                        float tmp = aug[piv, c]; aug[piv, c] = aug[col, c]; aug[col, c] = tmp;
                    }
                }
                for (int r = col + 1; r < 8; r++)
                {
                    float f = aug[r, col] / aug[col, col];
                    if (f == 0f) continue;
                    for (int c = col; c < 9; c++) aug[r, c] -= f * aug[col, c];
                }
            }
            float[] h = new float[8];
            for (int i = 7; i >= 0; i--)
            {
                float s = aug[i, 8];
                for (int j = i + 1; j < 8; j++) s -= aug[i, j] * h[j];
                h[i] = s / aug[i, i];
            }
            return h;
        }

        // [V59-FIX-8d] EN/FR: Accumulates one DLT row into the normal equations.
        private static void AccumulateNormalRow(float[,] ata, float[] atb, float[] row, float b)
        {
            for (int i = 0; i < 8; i++)
            {
                atb[i] += row[i] * b;
                for (int j = 0; j < 8; j++) ata[i, j] += row[i] * row[j];
            }
        }

        private float[] ComputeHomography(Point2F[] src, Point2F[] dst)
        {
            // Build the 8x8 linear system
            float[][] P = new float[8][];
            for(int i=0; i<8; i++) P[i] = new float[9];

            for (int i = 0; i < 4; i++)
            {
                float x = src[i].X;
                float y = src[i].Y;
                float X = dst[i].X;
                float Y = dst[i].Y;

                // Equation 1 for point i: h11*x + h12*y + h13 - h31*x*X - h32*y*X = X
                P[2*i][0] = x; P[2*i][1] = y; P[2*i][2] = 1;
                P[2*i][3] = 0; P[2*i][4] = 0; P[2*i][5] = 0;
                P[2*i][6] = -x*X; P[2*i][7] = -y*X; P[2*i][8] = X;

                // Equation 2 for point i: h21*x + h22*y + h23 - h31*x*Y - h32*y*Y = Y
                P[2*i+1][0] = 0; P[2*i+1][1] = 0; P[2*i+1][2] = 0;
                P[2*i+1][3] = x; P[2*i+1][4] = y; P[2*i+1][5] = 1;
                P[2*i+1][6] = -x*Y; P[2*i+1][7] = -y*Y; P[2*i+1][8] = Y;
            }

            // Gaussian Elimination
            for (int i = 0; i < 8; i++)
            {
                // Pivot
                int maxRow = i;
                for (int k = i + 1; k < 8; k++)
                    if (Math.Abs(P[k][i]) > Math.Abs(P[maxRow][i])) maxRow = k;

                // Swap
                float[] temp = P[i]; P[i] = P[maxRow]; P[maxRow] = temp;

                // Check for singularity
                if (Math.Abs(P[i][i]) < 0.0000001f) continue;

                // Eliminate
                for (int k = i + 1; k < 8; k++)
                {
                    float factor = P[k][i] / P[i][i];
                    for (int j = i; j < 9; j++) P[k][j] -= factor * P[i][j];
                }
            }

            // Back substitution
            float[] h = new float[8];
            for (int i = 7; i >= 0; i--)
            {
                float sum = 0;
                for (int j = i + 1; j < 8; j++) sum += P[i][j] * h[j];
                h[i] = (P[i][8] - sum) / P[i][i];
            }

            return h;
        }

        // TPS Coefficients Structure
        private struct TPSCoefficients
        {
            public float[] w; // Weights for radial basis functions
            public float a0, a1, a2; // Affine part
            public List<Point2F> controlPoints;
        }

        // Compute TPS Coefficients (Solves (L | P) * (w | a) = (v | 0))
        private TPSCoefficients ComputeTPS(List<Point2F> src, List<float> dstValues)
        {
            int n = src.Count;
            int dim = n + 3;
            
            // Build Matrix L (N x N)
            // K_ij = U(|Pi - Pj|)
            float[][] K = new float[n][];
            for(int i=0; i<n; i++) K[i] = new float[n];

            for(int i=0; i<n; i++)
            {
                for(int j=0; j<n; j++)
                {
                    if (i == j) K[i][j] = 0; // U(0) = 0
                    else
                    {
                        float dist = GetDistance(src[i], src[j]);
                        // U(r) = r^2 * log(r)
                        // Add small epsilon to avoid log(0) if duplicate points
                        if (dist < 0.0001f) K[i][j] = 0;
                        else K[i][j] = (dist * dist) * (float)Math.Log(dist);
                    }
                }
            }

            // Build Matrix P (N x 3) -> [1, x, y]
            // And construct the full system Matrix A (N+3 x N+3)
            float[][] A = new float[dim][];
            for(int i=0; i<dim; i++) A[i] = new float[dim];

            // Fill Top-Left (K)
            for(int i=0; i<n; i++)
                for(int j=0; j<n; j++)
                    A[i][j] = K[i][j];

            // Fill Top-Right (P) and Bottom-Left (Pt)
            for(int i=0; i<n; i++)
            {
                A[i][n] = 1;   A[n][i] = 1;
                A[i][n+1] = src[i].X; A[n+1][i] = src[i].X;
                A[i][n+2] = src[i].Y; A[n+2][i] = src[i].Y;
            }

            // Bottom-Right is 0 (3x3)

            // Build RHS Vector B (N+3)
            float[] B = new float[dim];
            for(int i=0; i<n; i++) B[i] = dstValues[i];
            // Last 3 elements are 0

            // Solve A * X = B using Gaussian Elimination
            float[] X = SolveLinearSystem(A, B, dim);

            TPSCoefficients coeffs = new TPSCoefficients();
            coeffs.w = new float[n];
            for(int i=0; i<n; i++) coeffs.w[i] = X[i];
            coeffs.a0 = X[n];
            coeffs.a1 = X[n+1];
            coeffs.a2 = X[n+2];
            coeffs.controlPoints = src;

            return coeffs;
        }

        private float EvaluateTPS(TPSCoefficients coeffs, Point2F p)
        {
            float result = coeffs.a0 + coeffs.a1 * p.X + coeffs.a2 * p.Y;
            for(int i=0; i<coeffs.controlPoints.Count; i++)
            {
                float dist = GetDistance(p, coeffs.controlPoints[i]);
                float u = 0;
                if (dist > 0.0001f) u = (dist * dist) * (float)Math.Log(dist);
                result += coeffs.w[i] * u;
            }
            return result;
        }

        private float[] SolveLinearSystem(float[][] A, float[] B, int n)
        {
            // Copy A and B to avoid modifying originals if reused (though here we create fresh)
            float[][] M = new float[n][];
            for(int i=0; i<n; i++) M[i] = (float[])A[i].Clone();
            float[] V = (float[])B.Clone();

            // Gaussian Elimination
            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                    if (Math.Abs(M[k][i]) > Math.Abs(M[maxRow][i])) maxRow = k;

                float[] temp = M[i]; M[i] = M[maxRow]; M[maxRow] = temp;
                float tempV = V[i]; V[i] = V[maxRow]; V[maxRow] = tempV;

                if (Math.Abs(M[i][i]) < 0.0000001f) continue;

                for (int k = i + 1; k < n; k++)
                {
                    float factor = M[k][i] / M[i][i];
                    for (int j = i; j < n; j++) M[k][j] -= factor * M[i][j];
                    V[k] -= factor * V[i];
                }
            }

            float[] X = new float[n];
            for (int i = n - 1; i >= 0; i--)
            {
                float sum = 0;
                for (int j = i + 1; j < n; j++) sum += M[i][j] * X[j];
                X[i] = (V[i] - sum) / M[i][i];
            }
            return X;
        }
        // Helper: Get Intersection of two lines (p1-p2 and p3-p4)
        private Point2F? GetLineIntersection(Point2F p1, Point2F p2, Point2F p3, Point2F p4)
        {
            float A1 = p2.Y - p1.Y;
            float B1 = p1.X - p2.X;
            float C1 = A1 * p1.X + B1 * p1.Y;

            float A2 = p4.Y - p3.Y;
            float B2 = p3.X - p4.X;
            float C2 = A2 * p3.X + B2 * p3.Y;

            float det = A1 * B2 - A2 * B1;

            if (Math.Abs(det) < 0.0001f) return null; // Parallel

            float x = (B2 * C1 - B1 * C2) / det;
            float y = (A1 * C2 - A2 * C1) / det;

            return new Point2F(x, y);
        }

        private float InterpolateX(float rawX, float rawY, Point2F TL, Point2F TR, Point2F BL, Point2F BR)
        {
            float pctLeft = (BL.Y - TL.Y == 0) ? 0 : (rawY - TL.Y) / (BL.Y - TL.Y);
            float leftX = TL.X + pctLeft * (BL.X - TL.X);

            float pctRight = (BR.Y - TR.Y == 0) ? 0 : (rawY - TR.Y) / (BR.Y - TR.Y);
            float rightX = TR.X + pctRight * (BR.X - TR.X);

            if (Math.Abs(rightX - leftX) < 0.0001f) return rawX;
            float val = (rawX - leftX) / (rightX - leftX);
            return float.IsNaN(val) || float.IsInfinity(val) ? rawX : val;
        }

        private float InterpolateY(float rawX, float rawY, Point2F TL, Point2F TR, Point2F BL, Point2F BR)
        {
            float pctTop = (TR.X - TL.X == 0) ? 0 : (rawX - TL.X) / (TR.X - TL.X);
            float topY = TL.Y + pctTop * (TR.Y - TL.Y);

            float pctBot = (BR.X - BL.X == 0) ? 0 : (rawX - BL.X) / (BR.X - BL.X);
            float botY = BL.Y + pctBot * (BR.Y - BL.Y);

            if (Math.Abs(botY - topY) < 0.0001f) return rawY;
            float val = (rawY - topY) / (botY - topY);
            return float.IsNaN(val) || float.IsInfinity(val) ? rawY : val;
        }

        // ── [V59] Wiimote Bar PnP: experimental hybrid tracking methods ──────────

        // [V59-FIX-5] EN: SHARED 2-LED roll compensation. Rolling the gun rotates the
        //     whole camera image around the image center, so the LED midpoint ORBITS
        //     and the cursor drifts DIAGONALLY (X and Y) — the old PnP X-only patch
        //     could not absorb it (user-verified: >10cm drift in both modes). Here we
        //     measure the roll from the LED-line angle (atan2, normalized to the
        //     "pointing right" convention so the sensor 0/1 swap when aiming left/right
        //     of the bar does not flip the sign), smooth it, gate it (deadband 2-6°)
        //     and un-rotate the midpoint around the image center — the pipeline then
        //     behaves as if the gun were never tilted.
        //     FR: Compensation de roulis PARTAGÉE pour 2 LEDs. Pencher le gun fait
        //     tourner toute l'image caméra autour du centre de l'image — le milieu LED
        //     ORBITE et le curseur dérive en DIAGONALE (X et Y) ; l'ancien patch PnP
        //     sur X seul ne pouvait pas l'absorber (vérifié user : >10 cm de dérive
        //     dans les deux modes). Ici on mesure le roulis via l'angle de la ligne LED
        //     (atan2, normalisé en convention « pointant à droite » pour que
        //     l'inversion des sensors 0/1 en visant à gauche/droite de la barre ne
        //     retourne pas le signe), on le lisse, on le borne (zone morte 2-6°) et on
        //     dé-roule le milieu autour du centre de l'image — le pipeline se comporte
        //     comme si le gun n'était jamais penché.
        private float _ledRollSmoothedRad = 0f;

        private void UnrotateLedMidpointRoll(ref Point2F pos, WiimoteLib.DataTypes.IRState ir)
        {
            if (ir.IRSensor0.Found && ir.IRSensor1.Found)
            {
                float dx = ir.IRSensor1.Position.X - ir.IRSensor0.Position.X;
                float dy = ir.IRSensor1.Position.Y - ir.IRSensor0.Position.Y;
                if (dx * dx + dy * dy >= 0.000025f)
                {
                    float rollRad = (float)Math.Atan2(dy, dx);
                    if (rollRad > Math.PI / 2f) rollRad -= (float)Math.PI;
                    else if (rollRad < -Math.PI / 2f) rollRad += (float)Math.PI;
                    _ledRollSmoothedRad += (rollRad - _ledRollSmoothedRad) * 0.20f;
                }
            }

            // [V59-FIX-22] EN: Keep un-rotating pos using _ledRollSmoothedRad even on 1-LED frames
            //     so the cursor does NOT suffer an angular jump at the 2->1 LED transition!
            //     FR: Continuer de dé-rouler pos avec _ledRollSmoothedRad même sur les frames 1-LED
            //     pour éviter tout saut angulaire à la transition 2->1 LED !
            float rollDeg = Math.Abs(_ledRollSmoothedRad) * (180f / (float)Math.PI);
            float gate = Math.Max(0f, Math.Min(1f, (rollDeg - 2f) / 4f));
            if (gate <= 0f) return;

            float ang = -_ledRollSmoothedRad * gate;
            float cos = (float)Math.Cos(ang);
            float sin = (float)Math.Sin(ang);
            float ox = pos.X - 0.5f;
            float oy = pos.Y - 0.5f;
            pos.X = 0.5f + ox * cos - oy * sin;
            pos.Y = 0.5f + ox * sin + oy * cos;
        }

        // [V59-FIX-8] EN: TwoWiimoteBars cross-map — estimates the cursor (sx, sy) from
        //     the current bar midpoints using the 5 cross-calibration samples:
        //     sy from each bar's Y trajectory (top/bottom/center click samples,
        //     piecewise linear, both bars averaged), sx from the horizontal offset
        //     versus that trajectory normalized by the left/right click half-span.
        //     Each bar can provide the FULL estimate alone — losing a whole bar
        //     (both LEDs) still tracks, which kills the old bounce-on-LED-loss.
        //     FR: Croise-map TwoWiimoteBars — estime le curseur (sx, sy) depuis les
        //     milieux de barres courants avec les 5 échantillons de calibration croix :
        //     sy depuis la trajectoire Y de chaque barre (échantillons haut/bas/centre,
        //     linéaire par morceaux, moyenne des deux barres), sx = écart horizontal vs
        //     cette trajectoire normalisé par la demi-étendue des clics gauche/droite.
        //     Chaque barre peut fournir SEULE l'estimation complète — perdre une barre
        //     entière (2 LEDs) suit quand même le tracking, ce qui tue les rebonds
        //     d'avant sur perte de LED.
            // [V59-FIX-9b] EN: Live TWB hybrid state for the IR visualizer display.
        //     FR: État hybride TWB en direct pour l'affichage du visualiseur IR.
        public float TwbLastSepRatio { get { return _twbSepSmoothedRatio; } }
        public float TwbCalPairSep { get { return _twbCalPairSep; } }

        // [V59-FIX-14] EN: Live TWB distance (meters) — computed from the current bar
        //     pair separation and the DECLARED geometry, WITHOUT needing calibration.
        //     Works before, during and after calibration. Returns 0 when < 2 LEDs.
        //     FR: Distance TWB en direct (mètres) — calculée depuis la séparation
        //     courante des barres et la géométrie DÉCLARÉE, SANS nécessiter de
        //     calibration. Fonctionne avant, pendant et après calibration.
        //     Renvoie 0 si < 2 LEDs.
        public float TwbLastDistanceM { get { return _twbLastDistanceMLive; } }
        private float _twbLastDistanceMLive = 0f;

        // [V59-FIX-8e] EN: TwoWiimoteBars DEGRADED tracking (1 or 2 LEDs visible at the
        //     FOV edge). Approximates continuously instead of bouncing (the same
        //     philosophy as the single-bar 1-LED extrapolation — slightly less precise,
        //     may drift a little until both pairs come back, but NEVER snaps):
        //     - 2 LEDs, one TOP + one BOTTOM (vertical separation comparable to the
        //       calibrated pair separation): each bar's midpoint is reconstructed as
        //       the visible LED shifted by half the measured within-bar LED span,
        //       toward the side opposite the last valid midpoint.
        //     - 2 LEDs of the SAME bar (intact pair): that bar uses the pair average;
        //       the other bar stays frozen at its last valid midpoint.
        //     - 1 LED: reconstructed on its bar; the other bar frozen.
        //     FR: Tracking DÉGRADÉ TwoWiimoteBars (1-2 LEDs visibles à la butée FOV).
        //     Approxime en continu au lieu de rebondir (même philosophie que
        //     l'extrapolation 1-LED de la barre seule — un peu moins précis, peut
        //     dériver légèrement jusqu'au retour des deux paires, mais ne JAMAIS
        //     sauter) : 2 LEDs haut+bas (séparation verticale comparable à la
        //     séparation calibrée) → chaque milieu reconstruit = LED visible décalée
        //     d'une demi-portée LED intra-barre mesurée, côté opposé au dernier milieu
        //     valide ; 2 LEDs d'une MÊME barre (paire intacte) → cette barre suit la
        //     moyenne, l'autre reste figée ; 1 LED → reconstruite sur sa barre,
        //     l'autre figée.
        // [V59-FIX-28] EN: TwoWiimoteBars Quad Trapezoidal Degraded Tracking (1-3 LEDs visible).
        //     Designed specifically for perspective trapezoids (e.g. arcade cabinets with backward-tilted screens).
        //     Matches visible points to the persistent 4-point Quad slots (TL, TR, BR, BL).
        //     - 3 LEDs: Reconstructs the 1 missing LED along the intact bar's unit vector using that bar's own span.
        //     - 2 LEDs: Reconstructs missing points along the calibrated edge vectors.
        //     - 1 LED: Continuous delta tracking.
        //     - FREEZES the Hybrid PnP distance ratio in degraded mode so that reconstructed points
        //       NEVER cause ratio spikes or cursor shrinkage!
        //     FR: Tracking Quad Trapézoïdal Dégradé TwoWiimoteBars (1-3 LEDs visibles).
        //     Conçu pour les trapèzes en perspective (bornes arcade à écran incliné).
        //     Reconstruction mathématique exacte à 0 résidu, gel du ratio PnP pour 0 sursaut.
        private bool TryMapTwoBarDegraded(List<Point2F> pts, out Point2F cursor)
        {
            cursor = new Point2F(0.5f, 0.5f);
            if (!_twbHaveQuad || pts == null || pts.Count < 2 || pts.Count > 3 || _twbHomography == null) return false;

            Point2F curTL = _twbTL, curTR = _twbTR, curBR = _twbBR, curBL = _twbBL;
            Point2F mTop = _twbLastTopMid;
            Point2F mBot = _twbLastBotMid;

            if (pts.Count == 3)
            {
                // [V59-FIX-29] 3 points visible: 
                // Determine the intact horizontal bar using purely internal relative geometry
                // Among 3 points, find the pair with smallest Y difference (most horizontal)
                int bestI = 0, bestJ = 1;
                float minDy = Math.Abs(pts[0].Y - pts[1].Y);
                float dy02 = Math.Abs(pts[0].Y - pts[2].Y);
                if (dy02 < minDy) { minDy = dy02; bestI = 0; bestJ = 2; }
                float dy12 = Math.Abs(pts[1].Y - pts[2].Y);
                if (dy12 < minDy) { minDy = dy12; bestI = 1; bestJ = 2; }

                Point2F bA = pts[bestI], bB = pts[bestJ];
                if (bA.X > bB.X) { Point2F tp = bA; bA = bB; bB = tp; } // bA is Left, bB is Right

                // The 3rd point (single on the opposite bar)
                int singleIdx = 3 - (bestI + bestJ); // 0+1+2 = 3
                Point2F pSingle = pts[singleIdx];

                float barMidY = (bA.Y + bB.Y) * 0.5f;
                float barMidX = (bA.X + bB.X) * 0.5f;

                if (barMidY < pSingle.Y)
                {
                    // Intact bar is TOP (TL, TR); single point is on BOTTOM (BL or BR)
                    curTL = bA; curTR = bB;
                    float span = (_twbBotSpan > 0.005f) ? _twbBotSpan : (_twbTopSpan > 0.005f ? _twbTopSpan : 0.20f);
                    Point2F vTopNorm = new Point2F(curTR.X - curTL.X, curTR.Y - curTL.Y);
                    float lTop = (float)Math.Sqrt(vTopNorm.X * vTopNorm.X + vTopNorm.Y * vTopNorm.Y);
                    Point2F uRight = lTop > 0.005f ? new Point2F(vTopNorm.X / lTop, vTopNorm.Y / lTop) : new Point2F(1f, 0f);

                    if (pSingle.X < barMidX)
                    {
                        // Single point is BL
                        curBL = pSingle;
                        curBR = new Point2F(curBL.X + span * uRight.X, curBL.Y + span * uRight.Y);
                    }
                    else
                    {
                        // Single point is BR
                        curBR = pSingle;
                        curBL = new Point2F(curBR.X - span * uRight.X, curBR.Y - span * uRight.Y);
                    }
                }
                else
                {
                    // Intact bar is BOTTOM (BL, BR); single point is on TOP (TL or TR)
                    curBL = bA; curBR = bB;
                    float span = (_twbTopSpan > 0.005f) ? _twbTopSpan : (_twbBotSpan > 0.005f ? _twbBotSpan : 0.20f);
                    Point2F vBotNorm = new Point2F(curBR.X - curBL.X, curBR.Y - curBL.Y);
                    float lBot = (float)Math.Sqrt(vBotNorm.X * vBotNorm.X + vBotNorm.Y * vBotNorm.Y);
                    Point2F uRight = lBot > 0.005f ? new Point2F(vBotNorm.X / lBot, vBotNorm.Y / lBot) : new Point2F(1f, 0f);

                    if (pSingle.X < barMidX)
                    {
                        // Single point is TL
                        curTL = pSingle;
                        curTR = new Point2F(curTL.X + span * uRight.X, curTL.Y + span * uRight.Y);
                    }
                    else
                    {
                        // Single point is TR
                        curTR = pSingle;
                        curTL = new Point2F(curTR.X - span * uRight.X, curTR.Y - span * uRight.Y);
                    }
                }
                mTop = new Point2F((curTL.X + curTR.X) * 0.5f, (curTL.Y + curTR.Y) * 0.5f);
                mBot = new Point2F((curBL.X + curBR.X) * 0.5f, (curBL.Y + curBR.Y) * 0.5f);
            }
            else // pts.Count == 2
            {
                // [V59-FIX-29] 2 points visible: determine horizontal vs vertical orientation
                Point2F pA = pts[0], pB = pts[1];
                if (pA.X > pB.X) { Point2F tp = pA; pA = pB; pB = tp; }
                float dx = pB.X - pA.X;
                float dy = Math.Abs(pB.Y - pA.Y);

                if (dx > dy * 1.2f)
                {
                    // Horizontal bar (Top bar or Bottom bar)
                    float avgY = (pA.Y + pB.Y) * 0.5f;
                    float lastTopY = (_twbTL.Y + _twbTR.Y) * 0.5f;
                    float lastBotY = (_twbBL.Y + _twbBR.Y) * 0.5f;

                    if (Math.Abs(avgY - lastTopY) <= Math.Abs(avgY - lastBotY))
                    {
                        // Top bar intact
                        curTL = pA; curTR = pB;
                        curBL = new Point2F(curTL.X + _twbVLeft.X, curTL.Y + _twbVLeft.Y);
                        curBR = new Point2F(curTR.X + _twbVRight.X, curTR.Y + _twbVRight.Y);
                    }
                    else
                    {
                        // Bottom bar intact
                        curBL = pA; curBR = pB;
                        curTL = new Point2F(curBL.X - _twbVLeft.X, curBL.Y - _twbVLeft.Y);
                        curTR = new Point2F(curBR.X - _twbVRight.X, curBR.Y - _twbVRight.Y);
                    }
                }
                else
                {
                    // Lateral vertical edge (Left edge or Right edge)
                    Point2F pTop = (pA.Y < pB.Y) ? pA : pB;
                    Point2F pBot = (pA.Y < pB.Y) ? pB : pA;
                    float avgX = (pA.X + pB.X) * 0.5f;
                    float lastLeftX = (_twbTL.X + _twbBL.X) * 0.5f;
                    float lastRightX = (_twbTR.X + _twbBR.X) * 0.5f;

                    if (Math.Abs(avgX - lastLeftX) <= Math.Abs(avgX - lastRightX))
                    {
                        // Left edge intact (TL, BL)
                        curTL = pTop; curBL = pBot;
                        curTR = new Point2F(curTL.X + _twbVTop.X, curTL.Y + _twbVTop.Y);
                        curBR = new Point2F(curBL.X + _twbVBot.X, curBL.Y + _twbVBot.Y);
                    }
                    else
                    {
                        // Right edge intact (TR, BR)
                        curTR = pTop; curBR = pBot;
                        curTL = new Point2F(curTR.X - _twbVTop.X, curTR.Y - _twbVTop.Y);
                        curBL = new Point2F(curBR.X - _twbVBot.X, curBR.Y - _twbVBot.Y);
                    }
                }
                mTop = new Point2F((curTL.X + curTR.X) * 0.5f, (curTL.Y + curTR.Y) * 0.5f);
                mBot = new Point2F((curBL.X + curBR.X) * 0.5f, (curBL.Y + curBR.Y) * 0.5f);
            }

            // Update persistent positions
            _twbTL = curTL;
            _twbTR = curTR;
            _twbBR = curBR;
            _twbBL = curBL;
            _lastM1 = mTop;
            _lastM2 = mBot;
            _twbLastTopMid = mTop;
            _twbLastBotMid = mBot;

            // [V59-FIX-28] Keep live distance updated even in degraded mode
            float curBarSpan = (_twbTopSpan > 0.005f && _twbBotSpan > 0.005f) 
                ? (_twbTopSpan + _twbBotSpan) * 0.5f 
                : (_twbTopSpan > 0.005f ? _twbTopSpan : _twbBotSpan);
            if (curBarSpan > 0.005f)
            {
                GetPnpGeometry(out float spacingMM, out float focalNorm);
                _twbLastDistanceMLive = (spacingMM / 1000f) * focalNorm / curBarSpan;
            }

            Point2F twbCenter = new Point2F((mTop.X + mBot.X) * 0.5f, (mTop.Y + mBot.Y) * 0.5f);
            Point2F flippedCenter = new Point2F(1.0f - twbCenter.X, twbCenter.Y);
            Point2F flippedTop = new Point2F(1.0f - mTop.X, mTop.Y);
            Point2F flippedBot = new Point2F(1.0f - mBot.X, mBot.Y);

            // In degraded mode, pass topValid:false, botValid:false so that
            // Hybrid PnP distance scaling ratio is NOT recalculated from extrapolated points!
            return TryMapTwoBar(flippedCenter, flippedTop, flippedBot, false, false, out cursor);
        }

        private bool TryMapTwoBar(Point2F twbCenter, Point2F mTop, Point2F mBot, bool topValid, bool botValid, out Point2F cursor)
        {
            cursor = new Point2F(0.5f, 0.5f);
            if (_twbHomography == null) return false;

            float hw = _twbHomography[6] * twbCenter.X + _twbHomography[7] * twbCenter.Y + 1.0f;
            // Protection stricte anti-division par zéro uniquement
            if (Math.Abs(hw) < 0.0001f) return false;

            float rawScreenX = (_twbHomography[0] * twbCenter.X + _twbHomography[1] * twbCenter.Y + _twbHomography[2]) / hw;
            float rawScreenY = (_twbHomography[3] * twbCenter.X + _twbHomography[4] * twbCenter.Y + _twbHomography[5]) / hw;

            // [V59-FIX-29] Strict off-screen boundary detection:
            // When extrapolation is off, the off-screen threshold is 5% past the screen border [-0.05, 1.05].
            // When extrapolation is on, allow 15% past the screen border [-0.15, 1.15].
            float margin = (Options.Instance.WiimoteBarPnpEdgeExtrapolation) ? 0.15f : 0.05f;
            if (rawScreenX < -margin || rawScreenX > 1.0f + margin ||
                rawScreenY < -margin || rawScreenY > 1.0f + margin)
            {
                // Aim is physically off-screen! Return false so hasSensor becomes false and offscreen reload triggers!
                cursor = new Point2F(rawScreenX, rawScreenY);
                return false;
            }

            // Inside valid screen area: clamp smoothly to the active margin
            float clampMin = -margin;
            float clampMax = 1.0f + margin;
            cursor = new Point2F(Math.Max(clampMin, Math.Min(clampMax, rawScreenX)),
                                 Math.Max(clampMin, Math.Min(clampMax, rawScreenY)));

            // [V59-FIX-9/27/28] EN: Hybrid PnP distance correction for BOTH X and Y
            // Only update the live separation ratio when BOTH bars are genuinely observed (topValid && botValid).
            // In degraded mode, use the frozen _twbSepSmoothedRatio to maintain exact scale continuity without spikes!
            if (Options.Instance.TwoBarHybridPnp && _twbCalPairSep > 0.01f)
            {
                if (topValid && botValid)
                {
                    float sepNow = GetDistance(mTop, mBot);
                    if (sepNow > 0.01f)
                    {
                        float sepRatio = sepNow / _twbCalPairSep;
                        _twbSepSmoothedRatio += (sepRatio - _twbSepSmoothedRatio) * 0.10f;
                        if (Environment.TickCount - _twbLastSepLogMs > 2000 &&
                            Math.Abs(sepRatio - _twbLastLoggedSepRatio) > 0.05f)
                        {
                            _twbLastLoggedSepRatio = sepRatio;
                            _twbLastSepLogMs = Environment.TickCount;
                            SimpleLogger.Instance.Info(string.Format(
                                "[V59] TWB hybrid: sep={0:F3} cal={1:F3} ratio={2:F2} sr={3:F2}",
                                sepNow, _twbCalPairSep, sepRatio, _twbSepSmoothedRatio));
                        }
                    }
                }

                // Apply distance scale (both in full 4-LED and degraded mode)
                float sepGate = Math.Max(0f, Math.Min(1f, (Math.Abs(_twbSepSmoothedRatio - 1f) - 0.05f) / 0.05f));
                if (sepGate > 0f)
                {
                    float srEff = 1f + (_twbSepSmoothedRatio - 1f) * sepGate;
                    cursor.X = 0.5f + (cursor.X - 0.5f) / srEff;
                    cursor.Y = 0.5f + (cursor.Y - 0.5f) / srEff;

                    // Re-check off-screen boundary after distance scale
                    if (cursor.X < -margin || cursor.X > 1.0f + margin ||
                        cursor.Y < -margin || cursor.Y > 1.0f + margin)
                    {
                        return false; // Off-screen!
                    }

                    cursor.X = Math.Max(clampMin, Math.Min(clampMax, cursor.X));
                    cursor.Y = Math.Max(clampMin, Math.Min(clampMax, cursor.Y));
                }
            }
            return true;
        }

        // [V59-FIX-8] EN: Local helper for TryMapTwoBar (bottom-bar half-span).
        //     FR: Helper local de TryMapTwoBar (demi-étendue de la barre basse).
        private float halfSpanB()
        {
            return Math.Abs(_twbCalBotMid[4].Value.X - _twbCalBotMid[3].Value.X) / 2f;
        }

        // [V59-FIX-3] EN: Median of the two-click sample buffer (per-axis positional
        //     median). Falls back to the instantaneous raw point when empty.
        //     FR: Médiane du tampon d'échantillons des 2 clics (médiane positionnelle
        //     par axe). Repli sur le point brut instantané si vide.
        private Point2F PnpMedianSample()
        {
            int n = _pnpClickSamples.Count;
            if (n == 0) return _lastRawPoint;
            float[] xs = new float[n];
            float[] ys = new float[n];
            for (int i = 0; i < n; i++)
            {
                xs[i] = _pnpClickSamples[i].X;
                ys[i] = _pnpClickSamples[i].Y;
            }
            Array.Sort(xs);
            Array.Sort(ys);
            return new Point2F(xs[n / 2], ys[n / 2]);
        }

        // [V59-FIX-3] EN: Number of PnP Hybrid points already captured (0-4).
        //     FR: Nombre de points PnP Hybrid déjà capturés (0-4).
        private int PnpCapturedCount()
        {
            int c = 0;
            if (_topLeftPt.HasValue) c++;
            if (_topRightPt.HasValue) c++;
            if (_bottomRightPt.HasValue) c++;
            if (_bottomLeftPt.HasValue) c++;
            return c;
        }

        // [V59-FIXES] EN: Full separation between the two sensor bar LEDs in
        //     NORMALIZED camera units (0-1). Returns 0 when fewer than 2 LEDs are visible.
        //     FR: Séparation COMPLÈTE entre les 2 LEDs de la barre en unités caméra
        //     NORMALISÉES (0-1). Renvoie 0 si moins de 2 LEDs sont visibles.
        private float GetPnpLedSeparation(WiimoteLib.DataTypes.IRState ir)
        {
            if (!ir.IRSensor0.Found || !ir.IRSensor1.Found) return 0f;
            float dx = ir.IRSensor1.Position.X - ir.IRSensor0.Position.X;
            float dy = ir.IRSensor1.Position.Y - ir.IRSensor0.Position.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // EN/FR: Estimate the gun-to-screen distance (meters) from the FULL LED
        //     separation in normalized units: distance = spacing_m × focal / separation.
        //     Uses the calibrated focal when available, else the normalized factory
        //     focal (~1.207 — 45° horizontal FOV). Same helper for calibration AND
        //     tracking so the ratio between both is physically consistent.
        //     FR: Estime la distance gun-écran (mètres) depuis la séparation COMPLÈTE
        //     des LEDs en unités normalisées : distance = espacement_m × focale / séparation.
        //     Utilise la focale calibrée si disponible, sinon la focale usine normalisée
        //     (~1,207 — FOV horizontal 45°). Même helper pour calibration ET tracking
        //     afin que le ratio entre les deux soit physiquement cohérent.
        // [V59-FIX-10] EN: Declared-geometry accessor — when the user declared the
        //     physical setup, the spacing and the focal come from the declaration
        //     (focal_norm = 0.5 / tan(FOV/2), normalized image basis) and the distance
        //     is METRIC; otherwise the factory values (approximation).
        //     FR: Accesseur géométrie déclarée — si l'utilisateur a déclaré son
        //     installation, l'écartement et la focale viennent de la déclaration
        //     (focale_norm = 0,5 / tan(FOV/2), base image normalisée) et la distance
        //     est MÉTRIQUE ; sinon les valeurs usine (approximation).
        private void GetPnpGeometry(out float spacingMM, out float focalNorm)
        {
            if (Options.Instance.PnpDeclareGeometry)
            {
                // Mode MANUEL : géométrie déclarée par l'utilisateur (mesures manuelles avec/sans fisheye)
                spacingMM = Math.Max(20f, Options.Instance.PnpLedSpacingMM);
                float fovDeg = Math.Max(0.1f, Math.Min(250f, Options.Instance.PnpLensFovDeg));
                focalNorm = 0.5f / (float)Math.Tan(fovDeg * (float)Math.PI / 360f);
            }
            else
            {
                // Mode AUTOMATIQUE (par défaut, case non cochée) : géométrie usine standard Wiimote
                spacingMM = PnpLedSpacingMM;
                focalNorm = PnpFactoryFocalNorm;
            }
        }

        // [V59-FIX-10] EN: Precision forecast (user request): the linear size of ONE
        //     camera raw pixel at the current distance, in millimeters — the expected
        //     cursor granularity at that range (declared FOV when enabled, 45° else).
        //     FR: Prévision de précision (demande user) : taille linéaire d'UN pixel
        //     brut caméra à la distance courante, en millimètres — granularité
        //     attendue du curseur à cette portée (FOV déclaré si activé, 45° sinon).
        public float PnpPrecisionMm
        {
            get
            {
                float d = _pnpLastDistanceM;
                if (d <= 0f) return 0f;
                float fovDeg = 45f;
                if (Options.Instance.PnpDeclareGeometry)
                    fovDeg = Math.Max(0.1f, Math.Min(250f, Options.Instance.PnpLensFovDeg));
                return 2f * d * (float)Math.Tan(fovDeg * (float)Math.PI / 360f) / 1024f * 1000f;
            }
        }

        private float PnpEstimateDistance(float separation)
        {
            if (separation < 0.005f) return 0f; // EN/FR: Invalid separation — return 0 (unknown)
            GetPnpGeometry(out float spacingMM, out float focalNorm);
            return (spacingMM / 1000f) * focalNorm / separation;
        }

        // EN: Current live PnP distance estimate for the IR visualizer (0 = unknown).
        //     FR: Estimation de distance PnP en direct pour le visualiseur IR (0 = inconnue).
        public float PnpLastDistanceM { get { return _pnpLastDistanceM; } }

        // EN: Apply PnP 3D corrections on top of the bilinear base mapping.
        //     FR: Applique les corrections 3D PnP par-dessus le mapping bilinéaire de base.
        private void ApplyPnpCorrections(ref Point2F pos, WiimoteLib.DataTypes.IRState ir)
        {
            // [V59-FIX-21] Guard: Both LEDs must be visible for PnP 3D corrections.
            if (!(ir.IRSensor0.Found && ir.IRSensor1.Found)) return;
            if (_pnpFocalX <= 0 || _pnpRefDistance <= 0) return; // EN/FR: Not calibrated

            // EN/FR: 1. Current distance — shared per-frame estimate (same helper and
            //     focal as the calibration path). The old code recomputed it from a
            //     differently-defined span, making the ratio ~2x off at all times.
            //     FR: Distance courante — estimation par image partagée (même helper et
            //     focale que le chemin de calibration). L'ancien code la recalculait
            //     depuis un span défini différemment, décalant le ratio de ~2x en
            //     permanence.
            float dx = ir.IRSensor1.Position.X - ir.IRSensor0.Position.X;
            float dy = ir.IRSensor1.Position.Y - ir.IRSensor0.Position.Y;
            float separation = (float)Math.Sqrt(dx * dx + dy * dy);
            _pnpLastSeparation = separation;
            if (separation < 0.005f) return; // EN/FR: Separation too small (normalized units) — no correction

            float currentDist = _pnpLastDistanceM;
            if (currentDist < 0.1f) return; // EN/FR: Nonsensical distance — skip

            // EN/FR: 2. Distance ratio — how far the player moved from the calibration position
            float distRatio = currentDist / _pnpRefDistance;

            // EN/FR: 3. Smooth the ratio to prevent jitter (low-pass filter)
            const float ratioSmoothing = 0.10f;
            _pnpSmoothedDistRatio += (distRatio - _pnpSmoothedDistRatio) * ratioSmoothing;

            // [V59-FIX-6] EN: Distance/ratio diagnostic log (user request). One line when
            //     the ratio changes >5% since the last logged value, throttled to one
            //     line per 2s — compact enough for long sessions, enough to correlate
            //     with the in-game experience. Also logs the shared roll angle.
            //     FR: Log diagnostic distance/ratio (demande user). Une ligne quand le
            //     ratio change de >5 % depuis la dernière valeur loggée, limitée à une
            //     ligne / 2 s — assez compact pour de longues sessions, assez pour
            //     corréler au ressenti en jeu. Logge aussi l'angle de roulis partagé.
            if (Environment.TickCount - _pnpLastDistLogMs > 2000)
            {
                if (Math.Abs(distRatio - _pnpLastLoggedDistRatio) > 0.05f)
                {
                    _pnpLastLoggedDistRatio = distRatio;
                    _pnpLastDistLogMs = Environment.TickCount;
                    float diagGate = Math.Max(0f, Math.Min(1f, (Math.Abs(_pnpSmoothedDistRatio - 1f) - 0.05f) / 0.05f));
                    float diagRollDeg = _ledRollSmoothedRad * (180f / (float)Math.PI);
                    SimpleLogger.Instance.Info(string.Format(
                        "[V59] PnP dist: cur={0:F2}m ref={1:F2}m ratio={2:F2} sr={3:F2} gate={4:F2} roll={5:F1}°",
                        currentDist, _pnpRefDistance, distRatio, _pnpSmoothedDistRatio, diagGate, diagRollDeg));
                }
            }

            // [V59-FIX-5] EN/FR: The old ROLL compensation block was REMOVED here:
            //     rolling is now corrected by UnrotateLedMidpointRoll() BEFORE the
            //     bilinear mapping, SHARED with the standard WiimoteBar mode (X AND Y,
            //     user decision for an honest comparison). Keeping this X-only
            //     empirical patch would double-correct the roll.

            // EN/FR: 5. Distance compensation — scale the offset from screen center
            //     When the player moves closer/farther, the bilinear mapping stays at the
            //     calibration distance scale. The correction scales the offset from center
            //     proportionally to the distance change (only if the change exceeds noise).
            //     FR: Compensation de distance — met à l'échelle l'offset par rapport au centre.
            //     Quand le joueur s'approche/s'éloigne, le mapping bilinéaire reste à
            //     l'échelle de la distance de calibration. La correction met à l'échelle
            //     l'offset par rapport au centre proportionnellement au changement de
            //     distance (seulement si le changement dépasse le bruit).
            // [V59-FIX-4] EN: Distance compensation with a SMOOTH DEADBAND. The previous
            //     hard 3% gate made the correction pop on/off whenever the ±2% IR noise
            //     crossed it — felt as cursor "jumps" at the screen edges. Now the
            //     correction is ZERO below 5% deviation and ramps smoothly to full
            //     amplitude at 10% — at a fixed distance the mode behaves exactly like
            //     the clean bilinear baseline and only engages on REAL moves.
            //     FR: Compensation distance avec ZONE MORTE PROGRESSIVE. L'ancien seuil
            //     dur à 3 % faisait clignoter la correction quand le bruit IR de ±2 %
            //     le traversait — ressenti en « sursauts » du curseur aux bords.
            //     Désormais la correction est NULLE sous 5 % d'écart et monte
            //     progressivement à pleine amplitude à 10 % — à distance fixe le mode
            //     se comporte exactement comme le bilinéaire pur et ne s'enclenche
            //     que sur de VRAIS déplacements.
            {
                float sr = _pnpSmoothedDistRatio;
                float gate = Math.Max(0f, Math.Min(1f, (Math.Abs(sr - 1f) - 0.05f) / 0.05f));
                if (gate > 0f)
                {
                    float srEff = 1f + (sr - 1f) * gate;
                    // Scale the offset from center (0.5, 0.5) by the gated distance ratio
                    // (EN/FR: Mettre à l'échelle l'offset par rapport au centre (0.5, 0.5))
                    float offX = pos.X - 0.5f;
                    float offY = pos.Y - 0.5f;
                    pos.X = 0.5f + offX * srEff;
                    pos.Y = 0.5f + offY * srEff;
                }
            }

            // EN/FR: 7. IR offset correction — shift crosshair from IR camera to gun barrel
            //     The IR camera is physically offset from the barrel (~30mm typically).
            //     At close range this causes a visible vertical misalignment.
            //     FR: Correction offset IR — décale le crosshair du capteur IR vers le canon.
            //     Le capteur IR est physiquement décalé du canon (~30mm typiquement).
            //     À courte distance cela provoque un désalignement vertical visible.
            if (_pnpIROffsetM > 0 && currentDist > 0.05f)
            {
                float angularCorrection = _pnpIROffsetM / currentDist;
                pos.Y -= angularCorrection * 0.5f; // EN/FR: Scale to normalized coords (screen height ≈ 0.5 → half)
            }

            // [V59-FIX-22] EN: Clamp respects edge extrapolation opt-in.
            //     FR: Le clamp respecte l'option d'extrapolation de bord.
            float clampPnpMin = Options.Instance.WiimoteBarPnpEdgeExtrapolation ? -0.50f : -0.05f;
            float clampPnpMax = Options.Instance.WiimoteBarPnpEdgeExtrapolation ? 1.50f : 1.05f;
            if (pos.X < clampPnpMin) pos.X = clampPnpMin;
            if (pos.X > clampPnpMax) pos.X = clampPnpMax;
            if (pos.Y < clampPnpMin) pos.Y = clampPnpMin;
            if (pos.Y > clampPnpMax) pos.Y = clampPnpMax;
        }
    }
}
