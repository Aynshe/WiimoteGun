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

            // Load Gun4IR or 4-Corners calibration
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
            {
                _gun4irPoints = Options.Instance.GetGun4IRCalibration(playerIndex);
            }
            else if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
            {
                _gun4irPoints = Options.Instance.GetTwoWiimoteBarCalibration(playerIndex);
                
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

        public bool IsCalibrating { get { return _calibrateForm != null; } }
        /// <summary>
        /// True when the auto/manual mode selection form is open (EN/FR: Vrai quand le formulaire de sélection auto/manuel est ouvert)
        /// </summary>
        public bool IsSelectingMode { get { return _modeSelectionForm != null; } }
        public bool IsCalibrated 
        { 
            get
            {
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
            if (_ledLayout == LEDLayoutType.WiimoteBar)
            {
                // Return 4 points for WiimoteBar (EN/FR: Retourner 4 points pour WiimoteBar)
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
            _calibrateForm = new CalibrateForm(Options.Instance.MonitorId, _ledLayout);
            
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
                _ledLayout == LEDLayoutType.WiimoteBar &&
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
            if (Options.Instance.EnableDistanceCompensation && _ledLayout == LEDLayoutType.WiimoteBar && !IsCalibrating && !IsSelectingMode && !UseDynamicPerspective && _calibrationIRSpan > 0f)
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
            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    relativePosition = CalculateWiimoteBarPosition(ir, ref hasSensor);
                    break;

                case LEDLayoutType.Gun4IRDiamond:
                case LEDLayoutType.TwoWiimoteBar:
                case LEDLayoutType.FourCorners:
                    // Both use multi-LED averaging (EN/FR: Les deux utilisent moyenne multi-LED)
                    relativePosition = CalculateMultiLEDPosition(ir, ref hasSensor);
                    break;

                default:
                    relativePosition = CalculateWiimoteBarPosition(ir, ref hasSensor);
                    break;
            }

            if (hasSensor)
            {
                _firstSensorPos = ir.IRSensor0.Position;
                _secondSensorPos = ir.IRSensor1.Position;
                _midSensorPos = relativePosition;

                // Store raw position for calibration (before visual offsets)
                // (EN/FR: Stocker position brute pour calibration (avant offsets visuels))
                _lastRawPoint = relativePosition;
                _lastRawPoint.X = 1.0f - _lastRawPoint.X;

                relativePosition.X = 1.0f - relativePosition.X;

            // Calibration Step Logic - 3-POINT SYSTEM (EN/FR: Logique de calibration - SYSTÈME 3-POINTS)
            if (_calibrateForm != null && ((buttons.A && !lastState.A) || (buttons.B && !lastState.B)))
            {
                // Capture current IR span sample if valid (EN/FR: Capturer l'échantillon de taille IR actuel si valide)
                if (_lastIRSpan > 0f) _calibrationSpanSamples.Add(_lastIRSpan);

                if (_ledLayout == LEDLayoutType.WiimoteBar)
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
                else // Gun4IR / FourCorners / TwoWiimoteBar with center (5 points)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!_gun4irPoints[i].HasValue)
                        {
                            _gun4irPoints[i] = _lastRawPoint;
                            break;
                        }
                    }
                }
            }

                // Wiimote Bar Logic - 4-POINT BILINEAR MAPPING (EN/FR: Logique WiimoteBar - MAPPING BILINÉAIRE 4-POINTS)
                if (_ledLayout == LEDLayoutType.WiimoteBar)
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

                        // Clamp with limited extrapolation (±5%) for edge cases
                        // (EN/FR: Limiter avec extrapolation limitée (±5%) pour cas limites)
                        relativePosition.X = Math.Max(-0.05f, Math.Min(1.05f, relativePosition.X));
                        relativePosition.Y = Math.Max(-0.05f, Math.Min(1.05f, relativePosition.Y));
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
                            // v18 Double-T: Do not use Homography perspective warping or bounding box.
                            // Use pure 2D Bilinear Trapezoid mapping to strictly obey the calibration edges without 3D perspective distortion.
                            // (EN/FR: Double-T : Utiliser mapping trapézoïdal bilinéaire absolu 2D pour respecter les bords sans distorsion 3D)
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
                if (_ledLayout == LEDLayoutType.WiimoteBar)
                {
                    // 3-POINT SYSTEM: Count how many points are set (EN/FR: Compter combien de points sont définis)
                    if (_topLeftPt.HasValue) currentStep++;
                    if (_topRightPt.HasValue) currentStep++;
                    if (_bottomRightPt.HasValue) currentStep++;
                }
                else
                {
                    for(int i=0; i<5; i++) if (_gun4irPoints[i].HasValue) currentStep = i + 1;
                }
                _calibrateForm.SetStep(currentStep);
                // UpdateState: Pass 3 calibration points (EN/FR: Passer les 3 points de calibration)
                _calibrateForm.UpdateState(relativePosition, _topLeftPt, _topRightPt, _bottomRightPt, _bottomLeftPt);

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
                relativePosition = ir.Midpoint;
            }
            else if (ir.IRSensor0.Found)
            {
                // Standard extrapolation (normalized points provide compensation automatically)
                relativePosition.X = _midSensorPos.X + (ir.IRSensor0.Position.X - _firstSensorPos.X);
                relativePosition.Y = _midSensorPos.Y + (ir.IRSensor0.Position.Y - _firstSensorPos.Y);
            }
            else if (ir.IRSensor1.Found)
            {
                relativePosition.X = _midSensorPos.X + (ir.IRSensor1.Position.X - _secondSensorPos.X);
                relativePosition.Y = _midSensorPos.Y + (ir.IRSensor1.Position.Y - _secondSensorPos.Y);
            }
            else
            {
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
                // Find the two horizontal bars by pairing closest Y coordinates
                var sorted = pts.OrderBy(p => p.Y).ToList();
                Point2F m1 = new Point2F((sorted[0].X + sorted[1].X) / 2.0f, (sorted[0].Y + sorted[1].Y) / 2.0f);
                Point2F m2 = new Point2F((sorted[2].X + sorted[3].X) / 2.0f, (sorted[2].Y + sorted[3].Y) / 2.0f);

                float w1 = GetDistance(sorted[0], sorted[1]);
                float w2 = GetDistance(sorted[2], sorted[3]);
                float avgW = (w1 + w2) / 2.0f;
                float dist = GetDistance(m1, m2);

                if (avgW > 0.001f)
                {
                    _doubleTRatio = dist / avgW;
                }

                _lastM1 = m1;
                _lastM2 = m2;
                _doubleTInitialized = true;

                center = new Point2F((m1.X + m2.X) / 2.0f, (m1.Y + m2.Y) / 2.0f);
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
                if (TryCalculateDoubleTCenter(currentPoints.Values.ToList(), out absoluteCenter))
                {
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

            if (rightX == leftX) return 0.5f;
            return (rawX - leftX) / (rightX - leftX);
        }

        private float InterpolateY(float rawX, float rawY, Point2F TL, Point2F TR, Point2F BL, Point2F BR)
        {
            float pctTop = (TR.X - TL.X == 0) ? 0 : (rawX - TL.X) / (TR.X - TL.X);
            float topY = TL.Y + pctTop * (TR.Y - TL.Y);

            float pctBot = (BR.X - BL.X == 0) ? 0 : (rawX - BL.X) / (BR.X - BL.X);
            float botY = BL.Y + pctBot * (BR.Y - BL.Y);

            if (botY == topY) return 0.5f;
            return (rawY - topY) / (botY - topY);
        }
    }
}
