using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using WiimoteLib.Geometry;
using WiimoteLib.DataTypes;

namespace WiimoteGun
{
    class ScreenPositionCalculator
    {
        public ScreenPositionCalculator(int screenIndex, int playerIndex)
        {
            _screenIndex = screenIndex;
            _playerIndex = playerIndex;
            _ledLayout = Options.Instance.LEDLayout; // Load LED layout type (EN/FR: Charger le type de layout LED)

            // Load player-specific calibration (EN/FR: Charger calibration spécifique au joueur)
            var (top, left, centerX, centerY) = Options.Instance.GetCalibrationForPlayer(playerIndex);
            
            if (top != -1 && left != -1 && centerX != -1 && centerY != -1)
            {
                _topLeftPt = new Point2F(left, top);
                _centerPt = new Point2F(centerX, centerY);
            }

            // Load Gun4IR or 4-Corners calibration
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
            {
                _gun4irPoints = Options.Instance.GetGun4IRCalibration(playerIndex);
            }
            else if (_ledLayout == LEDLayoutType.FourCorners)
            {
                _gun4irPoints = Options.Instance.GetFourCornersCalibration(playerIndex);
            }
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
        private Point2F? _centerPt;
        private Point2F? _topLeftPt;
        
        // Gun4IR / 4 Corners Calibration Points (5 points: Center, Top, Right, Bottom, Left)
        private Point2F?[] _gun4irPoints = new Point2F?[5]; // 0=Center, 1=Top, 2=Right, 3=Bottom, 4=Left

        // Tracking robustness (EN/FR: Robustesse du tracking)
        private Point2F _lastValidRawCenter = new Point2F(512, 384); // Default center (1024x768 / 2)
        private Point2F _lastSmoothedCenter = new Point2F(512, 384); // Smoothed output
        private float _maxObservedDiagonal = 0f;
        private Dictionary<int, Point2F> _lastFramePoints = new Dictionary<int, Point2F>();
        private bool _wasUsingRelativeTracking = false;
        private int _framesSinceTransition = 0;

        private CalibrateForm _calibrateForm;

        public bool IsCalibrating { get { return _calibrateForm != null; } }
        public bool IsCalibrated 
        { 
            get 
            { 
                if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners)
                {
                    return _gun4irPoints != null && _gun4irPoints.All(p => p.HasValue);
                }
                return _centerPt.HasValue && _topLeftPt.HasValue; 
            } 
        }

        public void Calibrate()
        {
            if (_calibrateForm != null)
                return;

            ResetCalibration();

            Program.PostToUIThread(() =>
            {
                // Pass LED layout to calibration form (EN/FR: Passer le layout LED au formulaire de calibration)
                // Use current MonitorId from options (EN/FR: Utiliser le MonitorId actuel des options)
                _calibrateForm = new CalibrateForm(Options.Instance.MonitorId, _ledLayout);
                _calibrateForm.Show();
            });
        }

        public void EndCalibrate()
        {
            if (_calibrateForm == null)
                return;

            // Save player-specific calibration (EN/FR: Sauvegarder calibration spécifique au joueur)
            Options.Instance.SetCalibrationForPlayer(
                _playerIndex,
                _topLeftPt.HasValue ? _topLeftPt.Value.Y : -1,
                _topLeftPt.HasValue ? _topLeftPt.Value.X : -1,
                _centerPt.HasValue ? _centerPt.Value.X : -1,
                _centerPt.HasValue ? _centerPt.Value.Y : -1
            );

            // Save Gun4IR or 4-Corners calibration
            if (_ledLayout == LEDLayoutType.Gun4IRDiamond)
            {
                Options.Instance.SetGun4IRCalibration(_playerIndex, _gun4irPoints);
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
            _centerPt = null;
            _topLeftPt = null;
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

                relativePosition.X = 1.0f - relativePosition.X;

            // Calibration Step Logic
            if (_calibrateForm != null && ((buttons.A && !lastState.A) || (buttons.B && !lastState.B)))
            {
                if (_ledLayout == LEDLayoutType.WiimoteBar)
                {
                    if (!_centerPt.HasValue) _centerPt = relativePosition;
                    else if (!_topLeftPt.HasValue) _topLeftPt = relativePosition;
                }
                else // Gun4IR / 4Corners
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!_gun4irPoints[i].HasValue)
                        {
                            _gun4irPoints[i] = relativePosition;
                            break;
                        }
                    }
                }
            }

                // Wiimote Bar Logic (Existing)
                if (_ledLayout == LEDLayoutType.WiimoteBar)
                {
                    if (_topLeftPt.HasValue && _centerPt.HasValue)
                    {
                        // Safety check: Prevent division by zero
                        float deltaX = _centerPt.Value.X - _topLeftPt.Value.X;
                        float deltaY = _centerPt.Value.Y - _topLeftPt.Value.Y;

                        if (Math.Abs(deltaX) > 0.001f && Math.Abs(deltaY) > 0.001f)
                        {
                            relativePosition.X = (relativePosition.X - _topLeftPt.Value.X) / (deltaX * 2);
                            relativePosition.Y = (relativePosition.Y - _topLeftPt.Value.Y) / (deltaY * 2);
                        }
                    }
                }
                // Gun4IR / 4 Corners Logic (5-Point Interpolation)
                else if (_ledLayout == LEDLayoutType.Gun4IRDiamond || _ledLayout == LEDLayoutType.FourCorners)
                {
                    // We need 5 points: Center, Top, Right, Bottom, Left
                    // Stored in _gun4irPoints array: [0]=Center, [1]=Top, [2]=Right, [3]=Bottom, [4]=Left
                    
                    if (_gun4irPoints[0].HasValue && _gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                        _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                    {
                        Point2F center = _gun4irPoints[0].Value;
                        Point2F top = _gun4irPoints[1].Value;
                        Point2F right = _gun4irPoints[2].Value;
                        Point2F bottom = _gun4irPoints[3].Value;
                        Point2F left = _gun4irPoints[4].Value;

                        // Homography (Perspective Transform)
                        // We use the 4 outer points to compute a perspective transformation matrix.
                        // This avoids the "Tent Pole" effect (V-shape distortion) caused by the Center point
                        // in the triangulation method if the center is not perfectly aligned.
                        
                        // 4-Point Homography (Perspective Transform)
                        // CRITICAL: We use ONLY the 4 outer points (Top, Right, Bottom, Left).
                        // The Center point is IGNORED for mapping to avoid "Tent Pole" artifacts.
                        // The Center point only serves to identify the Gun4IR layout during calibration.
                        
                        // Check if we have the 4 outer calibration points
                        if (_gun4irPoints[1].HasValue && _gun4irPoints[2].HasValue && 
                            _gun4irPoints[3].HasValue && _gun4irPoints[4].HasValue)
                        {
                            // Source Points (Calibrated Sensor Coords)
                            Point2F[] src = new Point2F[4];
                            src[0] = _gun4irPoints[1].Value; // Top
                            src[1] = _gun4irPoints[2].Value; // Right
                            src[2] = _gun4irPoints[3].Value; // Bottom
                            src[3] = _gun4irPoints[4].Value; // Left

                            // Destination Points (Fixed Screen Coords 0-1)
                            Point2F[] dst = new Point2F[4];
                            dst[0] = new Point2F(0.5f, 0.0f); // Top -> Top Center
                            dst[1] = new Point2F(1.0f, 0.5f); // Right -> Right Center
                            dst[2] = new Point2F(0.5f, 1.0f); // Bottom -> Bottom Center
                            dst[3] = new Point2F(0.0f, 0.5f); // Left -> Left Center

                            // Compute Homography Matrix
                            float[] H = ComputeHomography(src, dst);

                            // Apply Homography to Current Position
                            float x = relativePosition.X;
                            float y = relativePosition.Y;
                            float w = H[6] * x + H[7] * y + 1.0f;

                            if (Math.Abs(w) > 0.0001f)
                            {
                                relativePosition.X = (H[0] * x + H[1] * y + H[2]) / w;
                                relativePosition.Y = (H[3] * x + H[4] * y + H[5]) / w;
                            }
                        }
                        else
                        {
                            // Fallback if not calibrated: Normalize Sensor Coords (0-1023) to Screen (0-1)
                            relativePosition.X /= 1024.0f;
                            relativePosition.Y /= 768.0f;
                        }
                    }
                    else
                    {
                        // Default / Raw fallback if not fully calibrated
                        // WiimoteLib returns normalized values (0.0 - 1.0), so NO division needed.
                        // (EN/FR: WiimoteLib retourne des valeurs normalisées, PAS de division nécessaire)
                    }
                }
            }

            if (_calibrateForm != null)
            {
                // Update UI Step
                int currentStep = 0;
                if (_ledLayout == LEDLayoutType.WiimoteBar)
                {
                    if (_centerPt.HasValue) currentStep = 1;
                }
                else
                {
                    for(int i=0; i<5; i++) if (_gun4irPoints[i].HasValue) currentStep = i + 1;
                }
                _calibrateForm.SetStep(currentStep);
                _calibrateForm.UpdateState(relativePosition, _centerPt, _topLeftPt);

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

            return relativePosition;
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

            // Update Max Diagonal if 4 points
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
            }

            if (count == 4)
            {
                // 4 Points: Use Intersection of Diagonals (Projective Center)
                // This is the only point invariant under perspective distortion.
                // Centroid (Average) and Midpoint of Longest Diagonal are NOT perspective-correct.
                
                var pts = currentPoints.Values.ToList();
                float cx = 0, cy = 0;
                foreach (var p in pts) { cx += p.X; cy += p.Y; }
                cx /= 4; cy /= 4;

                // Sort points to identify pairs: Top-Bottom and Left-Right
                // Top: Low Y, Bottom: High Y, Left: Low X, Right: High X (roughly)
                // Robust angular sort:
                var sortedPts = pts.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
                
                // Sorted order (-PI to PI): Left, Top, Right, Bottom (approx)
                // We need to find the "Top" point to align the sequence.
                // Top point has lowest Y (usually).
                Point2F pTop = pts.OrderBy(p => p.Y).First();
                Point2F pBottom = pts.OrderByDescending(p => p.Y).First();
                Point2F pLeft = pts.OrderBy(p => p.X).First();
                Point2F pRight = pts.OrderByDescending(p => p.X).First();

                // Intersect Line(Top, Bottom) with Line(Left, Right)
                Point2F? intersection = GetLineIntersection(pTop, pBottom, pLeft, pRight);

                if (intersection.HasValue)
                {
                    absoluteCenter = intersection.Value;
                    useAbsolute = true;
                }
                else
                {
                    // Fallback to average if parallel (unlikely for diamond)
                    absoluteCenter.X = cx;
                    absoluteCenter.Y = cy;
                    useAbsolute = true;
                }
            }
            else if (count == 3)
            {
                // 3 points: Use Midpoint of Longest Diagonal (Best approximation)
                var pts = currentPoints.Values.ToList();
                float maxDist = -1;
                Point2F p1 = new Point2F(), p2 = new Point2F();

                for (int i = 0; i < pts.Count; i++)
                {
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        float dist = GetDistance(pts[i], pts[j]);
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            p1 = pts[i];
                            p2 = pts[j];
                        }
                    }
                }
                absoluteCenter.X = (p1.X + p2.X) / 2;
                absoluteCenter.Y = (p1.Y + p2.Y) / 2;
                useAbsolute = true;
            }
            else if (count == 2)
            {
                var pts = currentPoints.Values.ToList();
                float dist = GetDistance(pts[0], pts[1]);

                // Improved diagonal detection: use distance + angle
                // Diagonals should be close to 45° (or 135°) and long enough
                bool isDiagonal = false;
                if (_maxObservedDiagonal > 0)
                {
                    // Distance check: >= 80% of max (with hysteresis)
                    float threshold = _wasUsingRelativeTracking ? 0.75f : 0.80f; // Lower threshold to exit relative mode
                    if (dist > _maxObservedDiagonal * threshold)
                    {
                        // Angle check: should be close to diagonal (30-60° or 120-150°)
                        float dx = Math.Abs(pts[1].X - pts[0].X);
                        float dy = Math.Abs(pts[1].Y - pts[0].Y);
                        float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                        if ((angle >= 30 && angle <= 60) || (angle >= 120 && angle <= 150))
                        {
                            isDiagonal = true;
                        }
                    }
                }

                if (isDiagonal)
                {
                    absoluteCenter.X = (pts[0].X + pts[1].X) / 2;
                    absoluteCenter.Y = (pts[0].Y + pts[1].Y) / 2;
                    useAbsolute = true;
                }
                else
                {
                    // It's a side or uncertain -> Fallback to relative tracking
                    useAbsolute = false;
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
                _lastValidRawCenter = absoluteCenter;
                _wasUsingRelativeTracking = false;
                _framesSinceTransition = 0;
                hasSensor = true;
            }
            else if (count > 0)
            {
                // Relative Tracking: Apply average delta of visible points to last known center
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
                    relativePosition.X = _lastValidRawCenter.X + (totalDeltaX / trackedPoints);
                    relativePosition.Y = _lastValidRawCenter.Y + (totalDeltaY / trackedPoints);
                    
                    // Update last valid center for next frame continuity
                    _lastValidRawCenter = relativePosition;
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
                    relativePosition.X = sumX / count;
                    relativePosition.Y = sumY / count;
                    _lastValidRawCenter = relativePosition;
                    _wasUsingRelativeTracking = true;
                    _framesSinceTransition = 0;
                    hasSensor = true;
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
