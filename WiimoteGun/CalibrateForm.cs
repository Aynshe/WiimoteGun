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

        private static Point2F? mCenter;
        private static Point2F? mTopLeft;

        public CalibrateForm(int screenIndex, LEDLayoutType ledLayout)
        {
            _ledLayout = ledLayout;
            
            // Determine number of calibration steps based on layout
            // (EN/FR: Déterminer le nombre d'étapes de calibration selon le layout)
            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    _totalCalibrationSteps = 2;
                    break;
                default:
                    _totalCalibrationSteps = 5; // Gun4IR & 4Corners now 5 steps
                    break;
            }
            _currentCalibrationStep = 0;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

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
            Text = null;
            Width = bounds.Width;
            Height = bounds.Height;
            Location = new System.Drawing.Point(bounds.Left, bounds.Top);

            StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE);
            User32.SetForegroundWindow(Handle);
            User32.SetActiveWindow(Handle);

            Opacity = 0.8;
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
            var gun = Properties.Resources.gun;

            // Draw gun at center or corner based on calibration step
            // (EN/FR: Dessiner le pistolet au centre ou au coin selon l'étape)
            Point gunPos = GetTargetPosition();
            e.Graphics.DrawImage(gun, gunPos.X - (gun.Width / 2), gunPos.Y - (gun.Height / 2));

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
                    // Center or top-left corner (EN/FR: Centre ou coin haut-gauche)
                    // Fix: Use mCenter.HasValue to determine step for Wiimote mode
                    if (!mCenter.HasValue)
                        return new Point(w / 2, h / 2);
                    else
                        return new Point(margin, margin);

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

                case LEDLayoutType.FourCorners:
                    // 4 corner positions (EN/FR: 4 positions aux coins)
                    switch (_currentCalibrationStep)
                    {
                        case 0: return new Point(margin, margin);                  // Top-left
                        case 1: return new Point(w - margin, margin);              // Top-right
                        case 2: return new Point(w - margin, h - margin);          // Bottom-right
                        case 3: return new Point(margin, h - margin);              // Bottom-left
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
                        // Horizontal LED bar at top AND bottom (EN/FR: Barre LED horizontale en haut ET en bas)
                        // Width ~25% of screen (approx 24cm on average TV)
                        int barWidth = (int)(w * 0.25f); 
                        int barStart = (w - barWidth) / 2;
                        int barEnd = barStart + barWidth;
                        
                        // Top Bar
                        int topY = margin;
                        g.DrawLine(ledPen, barStart, topY, barEnd, topY);
                        g.FillEllipse(Brushes.Red, barStart - 6, topY - 6, 12, 12);
                        g.FillEllipse(Brushes.Red, barEnd - 6, topY - 6, 12, 12);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, w / 2 - 70, topY + 20);

                        // OR label
                        g.DrawString("- OR -", font, Brushes.Yellow, w / 2 - 25, topY + 45);

                        // Bottom Bar
                        int botY = h - margin;
                        g.DrawLine(ledPen, barStart, botY, barEnd, botY);
                        g.FillEllipse(Brushes.Red, barStart - 6, botY - 6, 12, 12);
                        g.FillEllipse(Brushes.Red, barEnd - 6, botY - 6, 12, 12);
                        g.DrawString("SENSOR BAR (BOTTOM)", font, Brushes.Cyan, w / 2 - 85, botY - 40);
                        break;

                    case LEDLayoutType.Gun4IRDiamond:
                        // Gun4IR: Cross pattern + Edge Targets (EN/FR: Croix + Cibles bords)
                        // Step 0: Center, 1: Top, 2: Right, 3: Bottom, 4: Left
                        
                        // Draw Full Cross (EN/FR: Croix complète)
                        g.DrawLine(guidePen, centerX, 0, centerX, h); // Vertical
                        g.DrawLine(guidePen, 0, centerY, w, centerY); // Horizontal

                        // Define Target Positions (Edges)
                        Point tCenter = new Point(centerX, centerY);
                        Point tTop = new Point(centerX, 0 + margin);
                        Point tRight = new Point(w - margin, centerY);
                        Point tBottom = new Point(centerX, h - margin);
                        Point tLeft = new Point(0 + margin, centerY);

                        // Draw Markers based on step
                        // Only show current target or completed ones? User said "point tiré disparait"
                        // So we only highlight the CURRENT target.
                        
                        if (_currentCalibrationStep == 0) DrawLEDMarker(g, tCenter, "CENTER", true);
                        if (_currentCalibrationStep == 1) DrawLEDMarker(g, tTop, "TOP", true);
                        if (_currentCalibrationStep == 2) DrawLEDMarker(g, tRight, "RIGHT", true);
                        if (_currentCalibrationStep == 3) DrawLEDMarker(g, tBottom, "BOTTOM", true);
                        if (_currentCalibrationStep == 4) DrawLEDMarker(g, tLeft, "LEFT", true);

                        g.DrawString($"GUN4IR CALIBRATION ({_currentCalibrationStep + 1}/5)", font, Brushes.Cyan, centerX - 100, centerY - 100);
                        break;

                    case LEDLayoutType.FourCorners:
                        // 4 corners rectangle (EN/FR: Rectangle 4 coins)
                        Point[] corners = new Point[]
                        {
                            new Point(margin, margin),                  // Top-left
                            new Point(w - margin, margin),              // Top-right
                            new Point(w - margin, h - margin),          // Bottom-right
                            new Point(margin, h - margin)               // Bottom-left
                        };

                        // Draw rectangle outline (EN/FR: Dessiner contour rectangle)
                        g.DrawRectangle(guidePen, margin, margin, w - 2 * margin, h - 2 * margin);

                        // Draw corner LED markers (EN/FR: Dessiner marqueurs LED coins)
                        string[] cornerLabels = { "TL", "TR", "BR", "BL" };
                        for (int i = 0; i < corners.Length; i++)
                        {
                            DrawLEDMarker(g, corners[i], cornerLabels[i], _currentCalibrationStep == i);
                        }

                        g.DrawString("4 CORNERS", font, Brushes.Cyan, centerX - 45, margin - 25);
                        break;
                }
            }
        }

        private void DrawLEDMarker(Graphics g, Point position, string label, bool isActive)
        {
            // Draw LED marker with different color if it's the active calibration step
            // (EN/FR: Dessiner marqueur LED avec couleur différente si c'est l'étape active)
            Brush ledBrush = isActive ? Brushes.Lime : Brushes.Red;
            int size = isActive ? 14 : 10;
            
            g.FillEllipse(ledBrush, position.X - size / 2, position.Y - size / 2, size, size);
            g.DrawEllipse(Pens.White, position.X - size / 2, position.Y - size / 2, size, size);
            
            using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 9, System.Drawing.FontStyle.Bold))
            {
                var labelSize = g.MeasureString(label, font);
                g.DrawString(label, font, Brushes.Yellow, position.X - labelSize.Width / 2, position.Y - size / 2 - 18);
            }
        }

        private string GetCalibrationInstructions()
        {
            switch (_ledLayout)
            {
                case LEDLayoutType.WiimoteBar:
                    if (!mCenter.HasValue)
                        return "Calibrating Wiimote (LED Bar)\r\n\r\nAim at CENTER and press A or B";
                    else
                        return "Calibrating Wiimote (LED Bar)\r\n\r\nAim at TOP-LEFT CORNER and press A or B";

                case LEDLayoutType.Gun4IRDiamond:
                    string[] diamondSteps = { "CENTER", "TOP EDGE", "RIGHT EDGE", "BOTTOM EDGE", "LEFT EDGE" };
                    if (_currentCalibrationStep < diamondSteps.Length)
                        return $"Calibrating Gun4IR ({_currentCalibrationStep + 1}/{_totalCalibrationSteps})\r\n\r\nAim at {diamondSteps[_currentCalibrationStep]} and press A or B";
                    break;

                case LEDLayoutType.FourCorners:
                    string[] fcSteps = { "CENTER", "TOP EDGE", "RIGHT EDGE", "BOTTOM EDGE", "LEFT EDGE" };
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

        public void UpdateState(Point2F pos, Point2F? center, Point2F? topLeft)
        {
            if (mCenter == center && mTopLeft == topLeft)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Point2F, Point2F?, Point2F?>(UpdateState), new object[] { pos, center, topLeft });
                return;
            }

            mCenter = center;
            mTopLeft = topLeft;
            Invalidate();
        }

        public bool IsCalibrated
        {
            get { return mCenter.HasValue && mTopLeft.HasValue; }
        }
    }

}
