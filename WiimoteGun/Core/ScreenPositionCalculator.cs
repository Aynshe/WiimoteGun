using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using WiimoteLib.Geometry;
using WiimoteLib.DataTypes;
using WiimoteGun.UI.Calibrate;

namespace WiimoteGun
{
    class ScreenPositionCalculator
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
        
        // Dynamic Offset Memory for smooth 1-point tracking (EN/FR: Mémoire offset dynamique pour suivi fluide 1 point)
        // Stored by Quadrant (TL, TR, BL, BR) to be robust against ID swaps
        private Point2F? _offsetTL;
        private Point2F? _offsetTR;
        private Point2F? _offsetBL;
        private Point2F? _offsetBR;
        
        // OLD: private Dictionary<int, Point2F> _dynamicOffsets = new Dictionary<int, Point2F>();

        private Point2F _lastRawPoint; // Raw IR point for calibration (EN/FR: Point IR brut pour calibration))

        // Tracking robustness (EN/FR: Robustesse du tracking)
        private Point2F _lastValidCenter = new Point2F(0.5f, 0.5f); // Default center in normalized coords (0-1)
        private Point2F _lastSmoothedCenter = new Point2F(0.5f, 0.5f); // Smoothed output in normalized coords
        private float _maxObservedDiagonal = 0f;
        private float _observedHeight = 0f; // Learned height of the rectangle (EN/FR: Hauteur apprise du rectangle)
        private float _observedWidth = 0f;  // Learned width of the rectangle (EN/FR: Largeur apprise du rectangle)
        private Dictionary<int, Point2F> _lastFramePoints = new Dictionary<int, Point2F>();
        private bool _wasUsingRelativeTracking = false;
        private int _framesSinceTransition = 0;

        private CalibrateForm _calibrateForm;

        public bool IsCalibrating { get { return _calibrateForm != null; } }
        public bool IsCalibrated 
        { 
            get
            {
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                {
                    // TwoWiimoteBar: Only 4 corners (indices 0-3), NO center
                    return _gun4irPoints != null &&
                           _gun4irPoints[0].HasValue && _gun4irPoints[1].HasValue &&
                           _gun4irPoints[2].HasValue && _gun4irPoints[3].HasValue;
                }
                else if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // Gun4IR/FourCorners: 5 points including center (all indices)
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

        // Expose the calculated center for visualization (EN/FR: Exposer le centre calculé pour la visualisation)
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

        public bool UseDynamicPerspective { get; set; } = false;

        public void Calibrate()
        {
            if (_calibrateForm != null)
                return;

            // Gun4IR / 4 Corners: Ask user for mode (Dynamic vs Standard)
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners)
            {
                // Show full-screen mode selection form (EN/FR: Afficher le formulaire plein écran de sélection)
                Program.PostToUIThread(() =>
                {
                    string modeName = _ledLayout == LEDLayoutType.Gun4IRDiamond ? "Gun4IR" : "4 Corners";
                    
                    using (var selectionForm = new CalibrationModeSelectionForm(Options.Instance.MonitorId, modeName))
                    {
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
                            Program.Notify($"{modeName} Dynamic Mode Enabled (P{_playerIndex})");
                            // No need to show calibration form
                        }
                        else
                        {
                            // Standard Mode
                            UseDynamicPerspective = false;
                            Options.Instance.SetUseDynamicPerspective(_playerIndex, false);
                            StartCalibrationForm();
                        }
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
            ResetCalibration();
            // Pass LED layout to calibration form (EN/FR: Passer le layout LED au formulaire de calibration)
            // Use current MonitorId from options (EN/FR: Utiliser le MonitorId actuel des options)
            _calibrateForm = new CalibrateForm(Options.Instance.MonitorId, _ledLayout);
            
            // Handle ESC key cancellation (EN/FR: Gérer l'annulation avec touche ESC)
            _calibrateForm.CalibrationCancelled += (s, e) =>
            {
                SimpleLogger.Instance.Info($"Calibration cancelled by user for Player {_playerIndex}");
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
                SimpleLogger.Instance.Info($"Permissive mode: Extrapolated Bottom-Left point from TL.X + BR.Y for Player {_playerIndex}");
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

            var frm = _calibrateForm;
            _calibrateForm = null;

            Program.PostToUIThread(() => { frm.Dispose(); });
        }

        public void ResetCalibration()
        {
            _topLeftPt = null;
            _topRightPt = null;
            _bottomRightPt = null;
            _bottomLeftPt = null; // NEW: Reset 4th point (EN/FR: Réinitialiser 4e point)
            for(int i=0; i<5; i++) _gun4irPoints[i] = null; // Reset Gun4IR points
        }

        public Point2F? GetScaledPosition(WiimoteLib.DataTypes.IRState ir, WiimoteLib.DataTypes.ButtonState buttons, WiimoteLib.DataTypes.ButtonState lastState)
        {
            Point2F relativePosition = new Point2F();
            bool hasSensor = true;

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
                else if (_ledLayout == LEDLayoutType.TwoWiimoteBar)
                {
                    // TwoWiimoteBar: 4 CORNERS ONLY (NO CENTER) - TL → TR → BR → BL
                    // (EN/FR: TwoWiimoteBar : 4 COINS SEULEMENT (SANS CENTRE) - HG → HD → BD → BG)
                    for (int i = 0; i < 4; i++)  // Changed from 5 to 4
                    {
                        if (!_gun4irPoints[i].HasValue)
                        {
                            _gun4irPoints[i] = _lastRawPoint;
                            break;
                        }
                    }
                }
                else // Gun4IR / FourCorners with center (5 points)
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

                // Wiimote Bar Logic - 4-POINT HOMOGRAPHY MAPPING (EN/FR: Logique WiimoteBar - MAPPING HOMOGRAPHIE 4-POINTS)
                if (_ledLayout == LEDLayoutType.WiimoteBar)
                {
                    if (_topLeftPt.HasValue && _topRightPt.HasValue && _bottomRightPt.HasValue && _bottomLeftPt.HasValue)
                    {
                        // Use 4-corner homography mapping (same as TwoWiimoteBar)
                        // (EN/FR: Utiliser mapping homographie 4 coins - identique à TwoWiimoteBar)
                        Point2F[] src = new Point2F[4];
                        Point2F[] dst = new Point2F[4];

                        src[0] = _topLeftPt.Value;     // TL
                        src[1] = _topRightPt.Value;    // TR
                        src[2] = _bottomRightPt.Value; // BR
                        src[3] = _bottomLeftPt.Value;  // BL

                        dst[0] = new Point2F(0.0f, 0.0f); // TL
                        dst[1] = new Point2F(1.0f, 0.0f); // TR
                        dst[2] = new Point2F(1.0f, 1.0f); // BR
                        dst[3] = new Point2F(0.0f, 1.0f); // BL

                        // Compute Homography Matrix (EN/FR: Calculer matrice homographie)
                        float[] H = ComputeHomography(src, dst);

                        // Apply Homography to Current Position (EN/FR: Appliquer homographie à position actuelle)
                        float x = relativePosition.X;
                        float y = relativePosition.Y;
                        float w = H[6] * x + H[7] * y + 1.0f;

                        if (Math.Abs(w) > 0.0001f)
                        {
                            relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                            relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                        }

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

                                // Compute Homography Matrix
                                float[] H = ComputeHomography(src, dst);

                                // Apply Homography to Current Position (Midpoint)
                                float x = relativePosition.X;
                                float y = relativePosition.Y;
                                float w = H[6] * x + H[7] * y + 1.0f;

                                if (Math.Abs(w) > 0.0001f)
                                {
                                    relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                                    relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                                }
                            }
                        }
                        else // TwoWiimoteBar / FourCorners: Use indices 0-3 (TL, TR, BR, BL)
                        {
                            if (_gun4irPoints[0].HasValue && _gun4irPoints[1].HasValue && 
                                _gun4irPoints[2].HasValue && _gun4irPoints[3].HasValue)
                            {
                                Point2F[] src = new Point2F[4];
                                Point2F[] dst = new Point2F[4];

                                src[0] = _gun4irPoints[0].Value; // TL
                                src[1] = _gun4irPoints[1].Value; // TR
                                src[2] = _gun4irPoints[2].Value; // BR
                                src[3] = _gun4irPoints[3].Value; // BL

                                dst[0] = new Point2F(0.0f, 0.0f); // TL
                                dst[1] = new Point2F(1.0f, 0.0f); // TR
                                dst[2] = new Point2F(1.0f, 1.0f); // BR
                                dst[3] = new Point2F(0.0f, 1.0f); // BL

                                // Compute Homography Matrix
                                float[] H = ComputeHomography(src, dst);

                                // Apply Homography to Current Position (Midpoint)
                                float x = relativePosition.X;
                                float y = relativePosition.Y;
                                float w = H[6] * x + H[7] * y + 1.0f;

                                if (Math.Abs(w) > 0.0001f)
                                {
                                    relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                                    relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                                }
                            }
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

                // Learn dimensions for 2-Bar reconstruction (EN/FR: Apprendre dimensions pour reconstruction 2-Bar)
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // Sort by Y to separate Top/Bottom
                    var sortedByY = currentPoints.OrderBy(kvp => kvp.Value.Y).ToList();
                    // Top 2 are sortedByY[0] and [1]
                    // Bottom 2 are sortedByY[2] and [3]
                    
                    float topY = (sortedByY[0].Value.Y + sortedByY[1].Value.Y) / 2;
                    float bottomY = (sortedByY[2].Value.Y + sortedByY[3].Value.Y) / 2;
                    _observedHeight = Math.Abs(bottomY - topY);

                    // Sort Top 2 by X to get TL/TR
                    // CAMERA VIEW: X increases from Right to Left (Mirror)
                    // So Lowest X is Top-Right, Highest X is Top-Left
                    var topPts = new[] { sortedByY[0], sortedByY[1] }.OrderBy(kvp => kvp.Value.X).ToList();
                    _idTopRight = topPts[0].Key; // Low X
                    _idTopLeft = topPts[1].Key;  // High X

                    // Sort Bottom 2 by X to get BL/BR
                    var bottomPts = new[] { sortedByY[2], sortedByY[3] }.OrderBy(kvp => kvp.Value.X).ToList();
                    _idBottomRight = bottomPts[0].Key; // Low X
                    _idBottomLeft = bottomPts[1].Key;  // High X

                    SimpleLogger.Instance.Info($"[P{_playerIndex}] Learned IDs: TL={_idTopLeft} TR={_idTopRight} BL={_idBottomLeft} BR={_idBottomRight} Dims:{_observedWidth:F3}x{_observedHeight:F3}");

                    // Sort by X to separate Left/Right (for width calculation)
                    var sortedByX = currentPoints.Values.OrderBy(p => p.X).ToList();
                    float leftX = (sortedByX[0].X + sortedByX[1].X) / 2;
                    float rightX = (sortedByX[2].X + sortedByX[3].X) / 2;
                    _observedWidth = Math.Abs(rightX - leftX);
                }
            }

            // ... (Rest of the file) ...

            // ---------------------------------------------------------
            // 1 POINT VISIBLE (EN/FR: 1 POINT VISIBLE)
            // ---------------------------------------------------------
            if (count == 1)
            {
                // We need to know WHICH point it is to apply offset
                // (EN/FR: On doit savoir QUEL point c'est pour appliquer l'offset)
                var ptList = currentPoints.ToList();
                int id = ptList[0].Key;
                var p = ptList[0].Value;

                // Default to center if unknown (EN/FR: Défaut au centre si inconnu)
                float finalX = p.X;
                float finalY = p.Y;

                // Apply Geometric Offset based on ID (EN/FR: Appliquer décalage géométrique selon ID)
                // Logic:
                // TL (Top-Left)     -> We are aiming Top-Left. Camera sees it at Bottom-Right relative to center.
                //                      To get Center, we must move LEFT (-W/2) and DOWN (+H/2)? 
                //                      WAIT. Let's think in Screen Coordinates (0,0 is Top-Left).
                //                      If we see TL LED, the "Screen Center" is to the RIGHT (+W/2) and DOWN (+H/2) of that LED.
                
                // CORRECTION:
                // The "Point" p is the LED position in Camera Normalized Coords (0-1).
                // Camera 0,0 is Top-Left.
                // If we point at TL of screen, the TL LED is in the CENTER of the camera (0.5, 0.5).
                // If we point at CENTER of screen, the TL LED moves to the Top-Right of camera image? No.
                
                // Let's use the standard "Offset from Center" logic.
                // Center = Point + Offset
                
                // 1-Point Tracking Logic (FourCorners)
                // (EN/FR: Logique de suivi à 1 point)

                // 1-Point Tracking Logic (FourCorners)
                // (EN/FR: Logique de suivi à 1 point)

                // Use Quadrant-Based Dynamic Offsets (Robust against ID swaps)
                // (EN/FR: Offsets dynamiques basés sur quadrants (Robuste contre échange ID))
                
                Point2F? selectedOffset = null;
                
                // Determine Corner based on Camera Quadrant
                // Camera 0,0 is Top-Left.
                // TL LED appears in Bottom-Right (High X, High Y)
                // TR LED appears in Bottom-Left (Low X, High Y)
                // BL LED appears in Top-Right (High X, Low Y)
                // BR LED appears in Top-Left (Low X, Low Y)
                
                if (p.X > 0.5f && p.Y > 0.5f)
                {
                    // Bottom-Right of Camera -> Top-Left LED
                    selectedOffset = _offsetTL;
                    // Fallback Heuristic if no offset
                    if (selectedOffset == null) 
                    {
                        float halfW = _observedWidth / 2f; if (halfW < 0.01f) halfW = 0.2f;
                        float halfH = _observedHeight / 2f; if (halfH < 0.01f) halfH = 0.2f;
                        selectedOffset = new Point2F(-halfW, -halfH);
                    }
                }
                else if (p.X <= 0.5f && p.Y > 0.5f)
                {
                    // Bottom-Left of Camera -> Top-Right LED
                    selectedOffset = _offsetTR;
                    if (selectedOffset == null) 
                    {
                        float halfW = _observedWidth / 2f; if (halfW < 0.01f) halfW = 0.2f;
                        float halfH = _observedHeight / 2f; if (halfH < 0.01f) halfH = 0.2f;
                        selectedOffset = new Point2F(halfW, -halfH);
                    }
                }
                else if (p.X > 0.5f && p.Y <= 0.5f)
                {
                    // Top-Right of Camera -> Bottom-Left LED
                    selectedOffset = _offsetBL;
                    if (selectedOffset == null) 
                    {
                        float halfW = _observedWidth / 2f; if (halfW < 0.01f) halfW = 0.2f;
                        float halfH = _observedHeight / 2f; if (halfH < 0.01f) halfH = 0.2f;
                        selectedOffset = new Point2F(-halfW, halfH);
                    }
                }
                else // p.X <= 0.5f && p.Y <= 0.5f
                {
                    // Top-Left of Camera -> Bottom-Right LED
                    selectedOffset = _offsetBR;
                    if (selectedOffset == null) 
                    {
                        float halfW = _observedWidth / 2f; if (halfW < 0.01f) halfW = 0.2f;
                        float halfH = _observedHeight / 2f; if (halfH < 0.01f) halfH = 0.2f;
                        selectedOffset = new Point2F(halfW, halfH);
                    }
                }

                if (selectedOffset.HasValue)
                {
                    absoluteCenter.X = p.X + selectedOffset.Value.X;
                    absoluteCenter.Y = p.Y + selectedOffset.Value.Y;
                    useAbsolute = true;
                }
            }

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
                
                // Update Quadrant Offsets when 4 points are visible (Sorted & Identified)
                // (EN/FR: Mettre à jour offsets quadrants quand 4 points visibles (Triés & Identifiés))
                if (count == 4 && (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners))
                {
                    // We need the sorted points from the Homography block.
                    // Since we are outside that block, we need to re-sort or capture them.
                    // Actually, let's just use the current points and sort them here for offset update.
                    var pts = currentPoints.Values.ToList();
                    float cx = 0, cy = 0;
                    foreach (var pt in pts) { cx += pt.X; cy += pt.Y; }
                    cx /= 4; cy /= 4;
                    
                    // Sort by Angle to identify TL, TR, BR, BL
                    // 0=TL, 1=TR, 2=BR, 3=BL (Clockwise from Top-Left?)
                    // Wait, standard sort is:
                    // TL (Low X, Low Y in Screen) -> But here we are in Camera Coords.
                    // Camera: TL LED is Bottom-Right (High X, High Y).
                    // TR LED is Bottom-Left (Low X, High Y).
                    // BR LED is Top-Left (Low X, Low Y).
                    // BL LED is Top-Right (High X, Low Y).
                    
                    // Let's use the same sort as Homography:
                    // sortedPts = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                    // Angles (Y is Down):
                    // BR (Low X, Low Y) -> (-,-) -> -135 deg
                    // TR (Low X, High Y) -> (-,+) -> +135 deg
                    // TL (High X, High Y) -> (+,+) -> +45 deg
                    // BL (High X, Low Y) -> (+,-) -> -45 deg
                    
                    // Order: -135 (BR), -45 (BL), +45 (TL), +135 (TR)
                    // Index 0: BR
                    // Index 1: BL
                    // Index 2: TL
                    // Index 3: TR
                    
                    var sortedPts = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                    
                    // Update Offsets: Offset = Center - Point
                    _offsetBR = new Point2F(absoluteCenter.X - sortedPts[0].X, absoluteCenter.Y - sortedPts[0].Y);
                    _offsetBL = new Point2F(absoluteCenter.X - sortedPts[1].X, absoluteCenter.Y - sortedPts[1].Y);
                    _offsetTL = new Point2F(absoluteCenter.X - sortedPts[2].X, absoluteCenter.Y - sortedPts[2].Y);
                    _offsetTR = new Point2F(absoluteCenter.X - sortedPts[3].X, absoluteCenter.Y - sortedPts[3].Y);
                }
            }
            else if (count > 0)
            {
                // ...
            }
            else
            {
                // No points visible -> Clear Offsets? 
                // No, keep them for a bit? 
                // Better to clear to avoid stale data if user moves significantly.
                _offsetTL = null;
                _offsetTR = null;
                _offsetBL = null;
                _offsetBR = null;
            }


            if (count == 4)
            {
                var pts = currentPoints.Values.ToList();
                float cx = 0, cy = 0;
                foreach (var p in pts) { cx += p.X; cy += p.Y; }
                cx /= 4; cy /= 4;

                // Debug Log (disabled by default, uncomment to enable)
                // SimpleLogger.Instance.Info($"[4pt] Centroid: ({cx:F3}, {cy:F3}). Layout: {_ledLayout}");

                // CRITICAL: Different tracking for Diamond vs Rectangle layouts
                if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
                {
                    // Gun4IR (Diamond): Use Intersection of Diagonals (Projective Center)
                    // This is the only point invariant under perspective distortion for a diamond.
                    
                    // Sort points to identify opposing pairs
                    var sortedPts = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                    
                    // Diamond: 0=Top, 1=Right, 2=Bottom, 3=Left -> Intersect (0,2) and (1,3)
                    Point2F p1 = sortedPts[0];
                    Point2F p2 = sortedPts[2];
                    Point2F p3 = sortedPts[1];
                    Point2F p4 = sortedPts[3];

                    // Intersect the two lines
                    Point2F? intersection = GetLineIntersection(p1, p2, p3, p4);

                    if (intersection.HasValue)
                    {
                        absoluteCenter = intersection.Value;
                        useAbsolute = true;
                    }
                    else
                    {
                        // Fallback to centroid if parallel (unlikely)
                        absoluteCenter.X = cx;
                        absoluteCenter.Y = cy;
                        useAbsolute = true;
                    }
                }
                else if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // 2-Bar / Rectangle: Use Centroid (Average)
                    // For rectangular layouts, centroid is stable and geometrically valid.
                    absoluteCenter.X = cx;
                    absoluteCenter.Y = cy;
                    useAbsolute = true;
                }
                else
                {
                    // Fallback to average
                    absoluteCenter.X = cx;
                    absoluteCenter.Y = cy;
                    useAbsolute = true;
                }
            }
            else if (count == 3)
            {
                var pts = currentPoints.Values.ToList();
                
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // 2-Bar / Rectangle: Robust Reconstruction
                    // (EN/FR: 2-Bar / Rectangle : Reconstruction robuste)
                    
                    // OPTIMIZATION: Use Max Distance to find Diagonal
                    // (EN/FR: OPTIMISATION : Utiliser la distance max pour trouver la diagonale)
                    // The two points furthest apart are the diagonal (A and C).
                    // The third point is the corner B (opposite to missing D).
                    // Center = Midpoint of Diagonal (A+C)/2.
                    
                    float d01 = GetDistance(pts[0], pts[1]);
                    float d12 = GetDistance(pts[1], pts[2]);
                    float d02 = GetDistance(pts[0], pts[2]);
                    
                    Point2F diag1, diag2;
                    
                    if (d01 > d12 && d01 > d02)
                    {
                        // P0 and P1 are diagonal
                        diag1 = pts[0]; diag2 = pts[1];
                    }
                    else if (d12 > d01 && d12 > d02)
                    {
                        // P1 and P2 are diagonal
                        diag1 = pts[1]; diag2 = pts[2];
                    }
                    else
                    {
                        // P0 and P2 are diagonal
                        diag1 = pts[0]; diag2 = pts[2];
                    }
                    
                    // Center is midpoint of diagonal
                    absoluteCenter.X = (diag1.X + diag2.X) / 2.0f;
                    absoluteCenter.Y = (diag1.Y + diag2.Y) / 2.0f;
                    useAbsolute = true;                
                    // Update Dimensions (Width/Height) from 3 points
                    // (EN/FR: Mettre à jour dimensions depuis 3 points)
                    // Determine which adjacent point forms Width vs Height
                    // We need to identify the 'opposite' point (the one not part of the diagonal)
                    Point2F opposite;
                    if ((diag1.X == pts[0].X && diag1.Y == pts[0].Y && diag2.X == pts[1].X && diag2.Y == pts[1].Y) ||
                        (diag1.X == pts[1].X && diag1.Y == pts[1].Y && diag2.X == pts[0].X && diag2.Y == pts[0].Y))
                    {
                        opposite = pts[2];
                    }
                    else if ((diag1.X == pts[1].X && diag1.Y == pts[1].Y && diag2.X == pts[2].X && diag2.Y == pts[2].Y) ||
                             (diag1.X == pts[2].X && diag1.Y == pts[2].Y && diag2.X == pts[1].X && diag2.Y == pts[1].Y))
                    {
                        opposite = pts[0];
                    }
                    else
                    {
                        opposite = pts[1];
                    }

                    Point2F adj1 = diag1; // One of the diagonal points is adjacent to 'opposite'
                    Point2F adj2 = diag2; // The other diagonal point is also adjacent to 'opposite'
                    
                    float dist1 = GetDistance(opposite, adj1);
                    float dist2 = GetDistance(opposite, adj2);

                    float dx1 = Math.Abs(opposite.X - adj1.X);
                    float dy1 = Math.Abs(opposite.Y - adj1.Y);
                    
                    float dx2 = Math.Abs(opposite.X - adj2.X);
                    float dy2 = Math.Abs(opposite.Y - adj2.Y);
                    
                    // Heuristic: Width is horizontal (dx > dy), Height is vertical (dy > dx)
                    if (dx1 > dy1) _observedWidth = dist1;
                    else _observedHeight = dist1;
                    
                    if (dx2 > dy2) _observedWidth = dist2;
                    else _observedHeight = dist2;
                }
                else
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

                // CRITICAL: FourCorners (2-Bar) should ALWAYS use absolute tracking
                if (_ledLayout == LEDLayoutType.TwoWiimoteBar || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // 2-Bar / Rectangle: 2-Point Reconstruction
                    // (EN/FR: 2-Bar / Rectangle : Reconstruction 2 points)
                    
                    // Calculate Midpoint
                    Point2F mid = new Point2F((pts[0].Value.X + pts[1].Value.X) / 2, (pts[0].Value.Y + pts[1].Value.Y) / 2);
                    
                    float dx = pts[1].Value.X - pts[0].Value.X;
                    float dy = pts[1].Value.Y - pts[0].Value.Y;
                    
                    // Determine orientation: Horizontal or Vertical?
                    bool isHorizontal = Math.Abs(dx) > Math.Abs(dy);
                    
                    if (isHorizontal)
                    {
                        // HORIZONTAL BAR (Top or Bottom)
                        bool isTopBar;

                        // Check IDs if available
                        int id1 = pts[0].Key;
                        int id2 = pts[1].Key;
                        
                        if (_idTopLeft != -1 && 
                           ((id1 == _idTopLeft && id2 == _idTopRight) || (id1 == _idTopRight && id2 == _idTopLeft)))
                        {
                            isTopBar = true;
                            // SimpleLogger.Instance.Info($"[P{_playerIndex}] 2-Point ID Match: Top Bar");
                        }
                        else if (_idBottomLeft != -1 && 
                                ((id1 == _idBottomLeft && id2 == _idBottomRight) || (id1 == _idBottomRight && id2 == _idBottomLeft)))
                        {
                            isTopBar = false;
                            // SimpleLogger.Instance.Info($"[P{_playerIndex}] 2-Point ID Match: Bottom Bar");
                        }
                        else
                        {
                            // Fallback Heuristic: If Y > 0.5 (Bottom of image), we are pointing UP -> Top Bar is visible
                            isTopBar = mid.Y > 0.5f;
                            SimpleLogger.Instance.Info($"[P{_playerIndex}] 2-Point Fallback: TopBar={isTopBar} (MidY={mid.Y:F2}) IDs:({id1},{id2}) KnownTL:{_idTopLeft}");
                        }
                        
                        if (_observedHeight > 0)
                        {
                            // Calculate perpendicular vector for offset
                            float len = (float)Math.Sqrt(dx*dx + dy*dy);
                            if (len > 0.001f)
                            {
                                float ndx = dx / len;
                                float ndy = dy / len;
                                
                                // Normal vector (rotated 90 deg) pointing "Down" (Y+)
                                // If bar is horizontal (dx=1, dy=0), Normal should be (0, 1)
                                // (-dy, dx) -> (0, 1) Correct.
                                float nx = -ndy;
                                float ny = ndx;
                                
                                // Ensure Normal points Down (Y+)
                                if (ny < 0) { nx = -nx; ny = -ny; }
                                
                                // Offset distance (Half Height)
                                float offset = _observedHeight / 2.0f;
                                
                                if (isTopBar)
                                {
                                    // If Top Bar, Center is BELOW (Y+)
                                    absoluteCenter.X = mid.X + nx * offset;
                                    absoluteCenter.Y = mid.Y + ny * offset;
                                }
                                else
                                {
                                    // If Bottom Bar, Center is ABOVE (Y-)
                                    absoluteCenter.X = mid.X - nx * offset;
                                    absoluteCenter.Y = mid.Y - ny * offset;
                                }
                            }
                            else { absoluteCenter = mid; }
                        }
                        else { absoluteCenter = mid; }
                    }
                    else
                    {
                        // VERTICAL BAR (Left or Right)
                        bool isLeftBar;

                        // ID Check removed to prevent "Bounce" due to swapped IDs.
                        // Geometric Heuristic is robust for edge tracking.
                        // (EN/FR: Vérification ID supprimée pour éviter le "Rebond". L'heuristique géométrique est robuste.)
                        
                        // Fallback Heuristic: If X > 0.5 (Right of image), we are pointing LEFT -> Left Bar is visible
                        isLeftBar = mid.X > 0.5f;
                        
                        /* 
                        // OLD ID LOGIC (Caused Bounce if IDs swapped)
                        if (_idTopLeft != -1 && 
                           ((id1 == _idTopLeft && id2 == _idBottomLeft) || (id1 == _idBottomLeft && id2 == _idTopLeft)))
                        {
                            isLeftBar = true;
                        }
                        else if (_idTopRight != -1 && 
                                ((id1 == _idTopRight && id2 == _idBottomRight) || (id1 == _idBottomRight && id2 == _idTopRight)))
                        {
                            isLeftBar = false;
                        }
                        else
                        {
                            isLeftBar = mid.X > 0.5f;
                        }
                        */
                        
                        if (_observedWidth > 0)
                        {
                             // Calculate perpendicular vector for offset
                            float len = (float)Math.Sqrt(dx*dx + dy*dy);
                            if (len > 0.001f)
                            {
                                float ndx = dx / len;
                                float ndy = dy / len;
                                
                                // Normal vector (rotated 90 deg) pointing "Right" (X+)
                                // If bar is vertical (dx=0, dy=1), Normal should be (1, 0)
                                // (dy, -dx) -> (1, 0) Correct.
                                float nx = ndy;
                                float ny = -ndx;
                                
                                // Ensure Normal points Right (X+)
                                if (nx < 0) { nx = -nx; ny = -ny; }
                                
                                // Offset distance (Half Width)
                                float offset = _observedWidth / 2.0f;
                                
                                if (isLeftBar)
                                {
                                    // If Left Bar, Center is to the RIGHT (X+)
                                    absoluteCenter.X = mid.X + nx * offset;
                                    absoluteCenter.Y = mid.Y + ny * offset;
                                }
                                else
                                {
                                    // If Right Bar, Center is to the LEFT (X-)
                                    absoluteCenter.X = mid.X - nx * offset;
                                    absoluteCenter.Y = mid.Y - ny * offset;
                                }
                            }
                            else { absoluteCenter = mid; }
                        }
                        else { absoluteCenter = mid; }
                    }
                    
                    useAbsolute = true;
                }
                else if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
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
                                SimpleLogger.Instance.Info($"[Calib-1Pt] ID:{id} Raw:({p.X:F3},{p.Y:F3}) Dims:({_observedWidth:F3}x{_observedHeight:F3}) -> Center:({absoluteCenter.X:F3},{absoluteCenter.Y:F3})");
                                useAbsolute = true;
                                hasSensor = true;
                            }
                            else
                            {
                                // Fallback to centroid if ID unknown
                                SimpleLogger.Instance.Info($"[Calib-1Pt] ID:{id} Unknown! Fallback to Raw. Dims:({_observedWidth:F3}x{_observedHeight:F3})");
                                
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
    }
}
