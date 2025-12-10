using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using WiimoteGun.Common.Win32;
using WiimoteLib.Geometry;

namespace WiimoteGun
{
    class CalibrateForm : Form
    {
        private Screen _screen;
        private LEDLayoutType _ledLayout;
        private int _currentCalibrationStep; // Current LED being calibrated (0-N)
        private int _totalCalibrationSteps;  // Total number of LEDs to calibrate

        // 3-Point calibration tracking (EN/FR: Suivi calibration 3-points)
        private static Point2F? mTopLeft;      // Point 1
        private static Point2F? mTopRight;     // Point 2
        private static Point2F? mBottomRight;  // Point 3
        private static Point2F? mBottomLeft;   // Point 4 (EN/FR: Point 4)

        // Cancel event - raised when user presses ESC to cancel calibration
        // (EN/FR: Événement d'annulation - déclenché quand l'utilisateur appuie sur ESC)
        public event EventHandler CalibrationCancelled;

        public CalibrateForm(int screenIndex, LEDLayoutType ledLayout)
        {
            _ledLayout = ledLayout;
            
            // Determine number of calibration steps based on layout
            // (EN/FR: Déterminer le nombre d'étapes de calibration selon le layout)
            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    _totalCalibrationSteps = 4;  // 4-POINT CALIBRATION (EN/FR: CALIBRATION 4-POINTS)
                    break;
                case LEDLayoutType.TwoWiimoteBar:
                    _totalCalibrationSteps = 4;  // 4-CORNER CALIBRATION - NO CENTER (EN/FR: CALIBRATION 4-COINS - SANS CENTRE)
                    break;
                default:
                    _totalCalibrationSteps = 5; // Gun4IR & 4Corners: 5 steps with center
                    break;
            }
            _currentCalibrationStep = 0;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            this.KeyPreview = true; // Enable form to receive key events (EN/FR: Activer réception touches)
            this.KeyDown += CalibrateForm_KeyDown;

            if (screenIndex > 0)
                _screen = Screen.AllScreens.Skip(screenIndex).FirstOrDefault();

            if (_screen == null)
                _screen = Screen.PrimaryScreen;

            var bounds = _screen.Bounds;

            Opacity = 0;
            BackColor = System.Drawing.Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.Dpi;
            ShowInTaskbar = false;            
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true; // Force window to stay on top (EN/FR: Forcer fenêtre au premier plan)
            Text = null;
            Width = bounds.Width;
            Height = bounds.Height;
            Location = new System.Drawing.Point(bounds.Left, bounds.Top);

            StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Aggressive topmost and focus enforcement (EN/FR: Application agressive du premier plan et focus)
            User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.SHOWWINDOW);
            
            // Disable all other windows to ensure exclusivity (EN/FR: Désactiver autres fenêtres pour garantir exclusivité)
            // This makes it modal-like behavior
            Focus();
            Activate();
            BringToFront();
            
            User32.SetForegroundWindow(Handle);
            User32.SetActiveWindow(Handle);
            
            // Force input focus (EN/FR: Forcer focus d'entrée)
            this.Select();

            Opacity = 0.8;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Re-enforce topmost every time window is activated (EN/FR: Re-forcer premier plan à chaque activation)
            User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014) // WM_ERASEBKGND
            {
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Note: Gun crosshair removed - using LED markers instead
            // (EN/FR: Note : Viseur pistolet retiré - utilise marqueurs LED maintenant)

            // Draw LED guides based on layout type (EN/FR: Dessiner les guides LED selon le type)
            DrawLEDGuides(e.Graphics);

            // Draw instructions (EN/FR: Dessiner les instructions)
            var rect = this.ClientRectangle;
            rect.Height /= 3;

            using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 14))
            {
                string instructions = GetCalibrationInstructions();
                TextRenderer.DrawText(e.Graphics, instructions, font,
                    rect, System.Drawing.Color.White, System.Drawing.Color.Transparent,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        private Point GetTargetPosition()
        {
            int w = this.Width;
            int h = this.Height;
            int margin = 30;
            int centerX = w / 2;
            int centerY = h / 2;

            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    // 4-POINT POSITIONS: Top-Left → Top-Right → Bottom-Right → Bottom-Left
                    // (EN/FR: POSITIONS 4-POINTS : Haut-Gauche → Haut-Droit → Bas-Droit → Bas-Gauche)
                    if (!mTopLeft.HasValue)
                        return new Point(margin, margin);              // Step 1: Top-Left
                    else if (!mTopRight.HasValue)
                        return new Point(w - margin, margin);          // Step 2: Top-Right
                    else if (!mBottomRight.HasValue)
                        return new Point(w - margin, h - margin);      // Step 3: Bottom-Right
                    else
                        return new Point(margin, h - margin);          // Step 4: Bottom-Left

                case LEDLayoutType.Gun4IRDiamond:
                    // 5 steps: Center, Top, Right, Bottom, Left
                    switch (_currentCalibrationStep)
                    {
                        case 0: return new Point(centerX, centerY);            // Center
                        case 1: return new Point(centerX, margin);             // Top
                        case 2: return new Point(w - margin, centerY);         // Right
                        case 3: return new Point(centerX, h - margin);         // Bottom
                        case 4: return new Point(margin, centerY);             // Left
                    }
                    break;

                case LEDLayoutType.TwoWiimoteBar:
                    // 4 corners: TL → TR → BR → BL (NO CENTER)
                    // (EN/FR: 4 coins : HG → HD → BD → BG - SANS CENTRE)
                    switch (_currentCalibrationStep)
                    {
                        case 0: return new Point(margin, margin);                  // Top-left
                        case 1: return new Point(w - margin, margin);              // Top-right
                        case 2: return new Point(w - margin, h - margin);          // Bottom-right
                        case 3: return new Point(margin, h - margin);              // Bottom-left
                    }
                    break;

                case LEDLayoutType.FourCorners:
                    // 5 positions: Center + 4 corners (EN/FR: 5 positions : Centre + 4 coins)
                    switch (_currentCalibrationStep)
                    {
                        case 0: return new Point(centerX, centerY);                // Center
                        case 1: return new Point(margin, margin);                  // Top-left
                        case 2: return new Point(w - margin, margin);              // Top-right
                        case 3: return new Point(w - margin, h - margin);          // Bottom-right
                        case 4: return new Point(margin, h - margin);              // Bottom-left
                    }
                    break;
            }

            return new Point(w / 2, h / 2);
        }

        private void DrawLEDGuides(Graphics g)
        {
            using (Pen ledPen = new Pen(Color.Cyan, 3))
            using (Pen guidePen = new Pen(Color.Yellow, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 12, System.Drawing.FontStyle.Bold))
            {
                int w = this.Width;
                int h = this.Height;
                int margin = 30;
                int centerX = w / 2;
                int centerY = h / 2;

                switch (_ledLayout)
                {
                    case LEDLayoutType.WiimoteBar:
                        // Wiimote Sensor Bar - 2 Point Calibration (Center + Top-Left)
                        // (EN/FR: Barre Wiimote - Calibration 2 points (Centre + Haut-Gauche))
                        // Step 0 = Center, Step 1 = Top-Left corner
                        
                        // Show sensor bar for reference (visual guide only)
                        int barWidth = (int)(w * 0.25f); 
                        int barStart = (w - barWidth) / 2;
                        int barEnd = barStart + barWidth;
                        int topY = margin;
                        g.DrawLine(ledPen, barStart, topY, barEnd, topY);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, w / 2 - 70, topY + 20);
                        
                        // Calibration points - 4 CORNERS (EN/FR: Points de calibration - 4 COINS)
                        Point topLeftPoint = new Point(margin, margin);
                        Point topRightPoint = new Point(w - margin, margin);
                        Point bottomRightPoint = new Point(w - margin, h - margin);
                        Point bottomLeftPoint = new Point(margin, h - margin);
                        
                        // Draw 4 calibration points with active indicator (EN/FR: Dessiner 4 points avec indicateur actif)
                        DrawLEDMarker(g, topLeftPoint, "TL", _currentCalibrationStep == 0);
                        DrawLEDMarker(g, topRightPoint, "TR", _currentCalibrationStep == 1);
                        
                        // Flexible Step 3 (Permissive Mode)
                        if (Options.Instance.PermissiveWiimoteBarCalibration && _currentCalibrationStep == 2)
                        {
                            // Both Bottom points active (Green)
                            DrawLEDMarker(g, bottomRightPoint, "BR", true);
                            DrawLEDMarker(g, bottomLeftPoint, "BL", true);
                        }
                        else
                        {
                            DrawLEDMarker(g, bottomRightPoint, "BR", _currentCalibrationStep == 2);
                            DrawLEDMarker(g, bottomLeftPoint, "BL", _currentCalibrationStep == 3);
                        }

                        // Bottom warning
                        int botY = h - margin;
                        string warningMsg = "BOTTOM SENSOR BAR NOT SUPPORTED";
                        var warningSize = g.MeasureString(warningMsg, font);
                        g.DrawString(warningMsg, font, Brushes.Red, w / 2 - warningSize.Width / 2, botY - 40);
                        break;

                    case LEDLayoutType.Gun4IRDiamond:
                        // Gun4IR: 5-Point Calibration with markers only (no crosshair)
                        // (EN/FR: Gun4IR : Calibration 5 points avec marqueurs uniquement (pas de croix))
                        // Step 0: Center, 1: Top, 2: Right, 3: Bottom, 4: Left
                        
                        // Define Target Positions (Edges)
                        Point tCenter = new Point(centerX, centerY);
                        Point tTop = new Point(centerX, 0 + margin);
                        Point tRight = new Point(w - margin, centerY);
                        Point tBottom = new Point(centerX, h - margin);
                        Point tLeft = new Point(0 + margin, centerY);

                        // Draw all markers with active indicator
                        DrawLEDMarker(g, tCenter, "CENTER", _currentCalibrationStep == 0);
                        DrawLEDMarker(g, tTop, "TOP", _currentCalibrationStep == 1);
                        DrawLEDMarker(g, tRight, "RIGHT", _currentCalibrationStep == 2);
                        DrawLEDMarker(g, tBottom, "BOTTOM", _currentCalibrationStep == 3);
                        DrawLEDMarker(g, tLeft, "LEFT", _currentCalibrationStep == 4);

                        g.DrawString($"GUN4IR CALIBRATION ({_currentCalibrationStep + 1}/5)", font, Brushes.Cyan, centerX - 100, centerY + 80);
                        break;


                    case LEDLayoutType.TwoWiimoteBar:
                        // 2 Wiimote Sensor Bars (Top/Bottom) - Horizontal bars
                        // (EN/FR: 2 Barres Wiimote (Haut/Bas) - Barres horizontales)
                        
                        int twoBarWidth = (int)(w * 0.4f); // 40% of screen width
                        int twoBarCenterX = centerX - twoBarWidth / 2;
                        
                        Point[] twoBarCorners = new Point[]
                        {
                            new Point(margin, margin),                  // Top-left
                            new Point(w - margin, margin),              // Top-right
                            new Point(w - margin, h - margin),          // Bottom-right
                            new Point(margin, h - margin)               // Bottom-left
                        };

                        // Draw Top Sensor Bar (Left and Right LED from Top bar)
                        Point topBarStart = new Point(twoBarCenterX, margin);
                        Point topBarEnd = new Point(twoBarCenterX + twoBarWidth, margin);
                        g.DrawLine(ledPen, topBarStart, topBarEnd);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, centerX - 70, margin + 5);

                        // Draw Bottom Sensor Bar (Left and Right LED from Bottom bar)
                        Point botBarStart = new Point(twoBarCenterX, h - margin);
                        Point botBarEnd = new Point(twoBarCenterX + twoBarWidth, h - margin);
                        g.DrawLine(ledPen, botBarStart, botBarEnd);
                        g.DrawString("SENSOR BAR (BOTTOM)", font, Brushes.Cyan, centerX - 85, h - margin - 25);

                        // Draw calibration target corners - 4-CORNER CALIBRATION (NO CENTER)
                        // (EN/FR: Dessiner coins cibles de calibration - CALIBRATION 4-COINS (SANS CENTRE))
                        string[] twoBarLabels = { "TL", "TR", "BR", "BL" };
                        
                        // Draw 4 corner markers (steps 0-3, NO center point)
                        // (EN/FR: Dessiner 4 marqueurs de coin - étapes 0-3, PAS de point central)
                        for (int i = 0; i < twoBarCorners.Length; i++)
                        {
                            bool isCurrent = (_currentCalibrationStep == i);
                            DrawLEDMarker(g, twoBarCorners[i], twoBarLabels[i], isCurrent);
                        }

                        g.DrawString("2 WIIMOTE BARS (TOP/BOTTOM) - 4 CORNERS", font, Brushes.Cyan, centerX - 155, centerY + 80);
                        break;

                    case LEDLayoutType.FourCorners:
                        // 4 Individual LEDs at screen corners
                        // (EN/FR: 4 LEDs individuelles aux coins de l'écran)
                        Point[] fourCorners = new Point[]
                        {
                            new Point(margin, margin),                  // Top-left
                            new Point(w - margin, margin),              // Top-right
                            new Point(w - margin, h - margin),          // Bottom-right
                            new Point(margin, h - margin)               // Bottom-left
                        };

                        // Draw rectangle outline (EN/FR: Dessiner contour rectangle)
                        g.DrawRectangle(guidePen, margin, margin, w - 2 * margin, h - 2 * margin);

                        // Draw corner LED markers (EN/FR: Dessiner marqueurs LED coins)
                        string[] fourCornerLabels = { "TL", "TR", "BR", "BL" };
                        
                        // Draw Center Marker (Step 0)
                        DrawLEDMarker(g, new Point(centerX, centerY), "CENTER", _currentCalibrationStep == 0);

                        for (int i = 0; i < fourCorners.Length; i++)
                        {
                            // Corners are steps 1-4
                            bool isCurrent = (_currentCalibrationStep == (i + 1));
                            // Draw larger marker if current
                            DrawLEDMarker(g, fourCorners[i], fourCornerLabels[i], isCurrent);
                        }

                        g.DrawString("4 CORNER LEDS", font, Brushes.Cyan, centerX - 60, centerY + 80);
                        break;
                }
            }
        }

        private void DrawLEDMarker(Graphics g, Point position, string label, bool isActive)
        {
            // Draw LED marker with different color if it's the active calibration step
            // (EN/FR: Dessiner marqueur LED avec couleur différente si c'est l'étape active)
            Brush ledBrush = isActive ? Brushes.Lime : Brushes.Red;
            int size = isActive ? 42 : 30; // x3 larger (was 14 : 10)
            
            g.FillEllipse(ledBrush, position.X - size / 2, position.Y - size / 2, size, size);
            g.DrawEllipse(Pens.White, position.X - size / 2, position.Y - size / 2, size, size);
            
            using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 12, System.Drawing.FontStyle.Bold))
            {
                var labelSize = g.MeasureString(label, font);
                g.DrawString(label, font, Brushes.Yellow, position.X - labelSize.Width / 2, position.Y - size / 2 - 22);
            }
        }

        private string GetCalibrationInstructions()
        {
            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    // 4-POINT CALIBRATION INSTRUCTIONS (EN/FR: INSTRUCTIONS CALIBRATION 4-POINTS)
                    if (!mTopLeft.HasValue)
                        return "Point at TOP-LEFT corner and press A / Trigger\nVisez le coin HAUT-GAUCHE et appuyez sur A / Gâchette";
                    if (!mTopRight.HasValue)
                        return "Point at TOP-RIGHT corner and press A / Trigger\nVisez le coin HAUT-DROIT et appuyez sur A / Gâchette";
                    
                    if (!mBottomRight.HasValue && !mBottomLeft.HasValue)
                    {
                        if (Options.Instance.PermissiveWiimoteBarCalibration)
                            return "Point at BOTTOM-RIGHT or BOTTOM-LEFT corner\nVisez le coin BAS-DROIT ou BAS-GAUCHE";
                        else
                            return "Point at BOTTOM-RIGHT corner and press A / Trigger\nVisez le coin BAS-DROIT et appuyez sur A / Gâchette";
                    }
                    
                    return "Point at BOTTOM-LEFT corner and press A / Trigger\nVisez le coin BAS-GAUCHE et appuyez sur A / Gâchette";


                case LEDLayoutType.Gun4IRDiamond:
                    string[] diamondSteps = { "CENTER", "TOP EDGE", "RIGHT EDGE", "BOTTOM EDGE", "LEFT EDGE" };
                    if (_currentCalibrationStep < diamondSteps.Length)
                        return $"Calibrating Gun4IR ({_currentCalibrationStep + 1}/{_totalCalibrationSteps})\r\n\r\nAim at {diamondSteps[_currentCalibrationStep]} and press A or B";
                    break;

                case LEDLayoutType.TwoWiimoteBar:
                    // 4-CORNER CALIBRATION WITHOUT CENTER (EN/FR: CALIBRATION 4-COINS SANS CENTRE)
                    string[] twoBarSteps = { "TOP-LEFT / HAUT-GAUCHE", "TOP-RIGHT / HAUT-DROIT", 
                                             "BOTTOM-RIGHT / BAS-DROIT", "BOTTOM-LEFT / BAS-GAUCHE" };
                    if (_currentCalibrationStep < twoBarSteps.Length)
                        return $"Calibrating 2 Wiimote Bars ({_currentCalibrationStep + 1}/4)\\r\\n\\r\\nAim at {twoBarSteps[_currentCalibrationStep].Split('/')[0].Trim()}\\r\\nVisez {twoBarSteps[_currentCalibrationStep].Split('/')[1].Trim()} et appuyez sur A ou B";
                    break;

                case LEDLayoutType.FourCorners:
                    string[] fcSteps = { "CENTER", "TOP-LEFT", "TOP-RIGHT", "BOTTOM-RIGHT", "BOTTOM-LEFT" };
                    if (_currentCalibrationStep < fcSteps.Length)
                    {
                        return $"Calibrating 4-Corners ({_currentCalibrationStep + 1}/{_totalCalibrationSteps})\r\n\r\nAim at {fcSteps[_currentCalibrationStep]} and press A or B";
                    }
                    break;

                default:
                    return "Calibrating Wiimote\r\nAim and fire on targets";
            }
            return string.Empty;
        }

        public void SetStep(int step)
        {
            if (_currentCalibrationStep != step)
            {
                _currentCalibrationStep = step;
                Invalidate();
            }
        }

        // Update calibration state with 3 points (EN/FR: Mettre à jour l'état avec 3 points)
        public void UpdateState(Point2F pos, Point2F? topLeft, Point2F? topRight, Point2F? bottomRight, Point2F? bottomLeft)
        {
            if (mTopLeft == topLeft && mTopRight == topRight && mBottomRight == bottomRight && mBottomLeft == bottomLeft)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Point2F, Point2F?, Point2F?, Point2F?, Point2F?>(UpdateState), 
                    new object[] { pos, topLeft, topRight, bottomRight, bottomLeft });
                return;
            }

            mTopLeft = topLeft;
            mTopRight = topRight;
            mBottomRight = bottomRight;
            mBottomLeft = bottomLeft;
                        Invalidate();
        }

        public bool IsCalibrated
        {
            // WiimoteBar: 3 points, TwoWiimoteBar: 4 points (EN/FR: WiimoteBar 3 points, TwoWiimoteBar 4 points)
            get { return mTopLeft.HasValue && mTopRight.HasValue && mBottomRight.HasValue; }
        }

        // Keyboard event handling (EN/FR: Gestion des événements clavier)
        private void CalibrateForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // ESC key to cancel calibration (EN/FR: Touche ESC pour annuler la calibration)
                // Raise event to notify controller (EN/FR: Déclencher événement pour notifier contrôleur)
                SimpleLogger.Instance.Info("Calibration cancelled by user (ESC key)");
                CalibrationCancelled?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F12)
            {
                CaptureDebugSnapshot();
                e.Handled = true;
            }
        }

        private void CaptureDebugSnapshot()
        {
            try
            {
                // Create Debug folder with timestamp (EN/FR: Créer dossier Debug avec horodatage)
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string debugFolder = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location),
                    "Debug",
                    $"Capture_Calib_{timestamp}");
                System.IO.Directory.CreateDirectory(debugFolder);

                // 1. Capture screenshot (EN/FR: Capturer capture d'écran)
                Bitmap screenshot = new Bitmap(this.Width, this.Height);
                this.DrawToBitmap(screenshot, new Rectangle(0, 0, this.Width, this.Height));
                string imagePath = System.IO.Path.Combine(debugFolder, "calibration_screen.png");
                screenshot.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                screenshot.Dispose();

                // 2. Generate detailed log (EN/FR: Générer log détaillé)
                var log = new System.Text.StringBuilder();
                log.AppendLine("=== WiimoteGun Calibration Debug Snapshot ===");
                log.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                log.AppendLine($"LED Layout: {_ledLayout}");
                log.AppendLine($"Step: {_currentCalibrationStep + 1}/{_totalCalibrationSteps}");
                log.AppendLine();

                // Log Wiimote States
                var wiimotes = WiimoteLib.WiimoteManager.ConnectedWiimotes;
                for (int i = 0; i < wiimotes.Length; i++)
                {
                    var wm = wiimotes[i];
                    log.AppendLine($"--- Wiimote {i + 1} ({wm.DevicePath}) ---");
                    var ir = wm.WiimoteState.IRState;
                    log.AppendLine($"IR Mode: {ir.Mode}, Sensitivity: {ir.Sensitivity}");
                    for (int p = 0; p < 4; p++)
                    {
                        if (ir[p].Found)
                            log.AppendLine($"  Point {p}: ({ir[p].Position.X}, {ir[p].Position.Y}) Size: {ir[p].Size}");
                    }
                }

                string logPath = System.IO.Path.Combine(debugFolder, "calibration_log.txt");
                System.IO.File.WriteAllText(logPath, log.ToString());

                // Visual feedback (Flash opacity)
                this.Opacity = 0.4;
                var t = new Timer();
                t.Interval = 100;
                t.Tick += (s, args) => { this.Opacity = 0.8; t.Stop(); t.Dispose(); };
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Debug Capture Failed: " + ex.Message);
            }
        }
    }

}
