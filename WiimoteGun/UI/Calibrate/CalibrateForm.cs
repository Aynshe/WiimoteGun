using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using WiimoteGun.Common.Win32;
using WiimoteLib.Geometry;

namespace WiimoteGun.UI.Calibrate
{
    class CalibrateForm : Form
    {
        private Screen _screen;
        private LEDLayoutType _ledLayout;
        private int _currentCalibrationStep; // Current LED being calibrated (0-N)
        private int _totalCalibrationSteps;  // Total number of LEDs to calibrate

        // [V59] EN: PnP mode — display the gun-to-screen distance and reject points
        //     that are too far from the reference distance (Lichtknarre-like validation).
        //     FR: Mode PnP — afficher la distance gun-écran et rejeter les points
        //     trop éloignés de la distance de référence (validation à la Lichtknarre).
        private float _lastDisplayedDistance = -1f;  // Last displayed distance (m), -1 = unknown
        // [V59-FIX-2] EN: 12% tolerance (was 5% — the plan value). Aiming at CORNERS
        //     physically changes the estimated distance vs the reference: slant range
        //     (+2-4%) and perspective foreshortening of the apparent LED separation at
        //     the FOV edge (up to ~10%) — user logs showed dozens of unfair rejections.
        //     FR: Tolérance 12 % (avant 5 % — valeur du plan). Viser les COINS change
        //     physiquement la distance estimée vs la référence : distance slante
        //     (+2-4 %) et raccourcissement perspectif de la séparation apparente des
        //     LEDs au bord du FOV (jusqu'à ~10 %) — les logs user montraient des
        //     dizaines de refus injustifiés.
        private const float DistanceTolerance = 0.12f;
        private int _distanceRejectCount = 0;           // Consecutive rejections (anti-deadlock at 3)
        private float _referenceDistance = -1f;          // Reference from first accepted point

        // 4-Point calibration tracking (EN/FR: Suivi calibration 4-points)
        private static Point2F? mTopLeft;      // Point 1
        private static Point2F? mTopRight;     // Point 2
        private static Point2F? mBottomRight;  // Point 3
        private static Point2F? mBottomLeft;   // Point 4

        // [V59] PnP 5th point (center) — stored as static like the 4 corners
        //     FR: PnP 5e point (centre) — stocké en statique comme les 4 coins
        private static Point2F? mCenter;      // Point 5

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
                // [V59-FIX-4] WiimoteBarPnP: 4 corners ONLY (the 5th center point was
                // removed — user decision)
                case LEDLayoutType.WiimoteBarPnP:
                    _totalCalibrationSteps = 4;
                    break;
                // TwoWiimoteBar & FourCorners fall here (5 steps)
                default:
                    _totalCalibrationSteps = 5; // Gun4IR & 4Corners: 5 steps with center
                    break;
            }
            _currentCalibrationStep = 0;
            _lastDisplayedDistance = -1f;
            _distanceRejectCount = 0;
            _referenceDistance = -1f;

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

            // [V59-FIXES] EN: 0.7 opacity (was 0.8) — slightly more see-through so the
            //     IR visualizer data (dots + enlarged coordinate/distance text) stays
            //     readable through the overlay. User-confirmed: applies to ALL layouts.
            //     FR: Opacité 0.7 (avant 0.8) — un peu plus transparent pour que les
            //     données du visualiseur IR (points + texte coordonnées/distance agrandi)
            //     restent lisibles au travers. Confirmé user : pour TOUS les layouts.
            Opacity = 0.7;
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
            // [V59-FIX-3b] EN: Compact semi-transparent top panel instead of the full
            //     top-third strip — measured text bounds + padding, horizontally
            //     centered. Minimizes the area masking the IR visualizer behind and
            //     keeps the layout coherent (user: texts were hiding each other).
            //     FR: Panneau haut semi-transparent COMPACT au lieu de la bande sur le
            //     tiers supérieur complet — texte mesuré + padding, centré
            //     horizontalement. Minimise la zone masquant le visualiseur IR derrière
            //     et garde une mise en page cohérente (user : les textes se cachaient
            //     les uns les autres).
            EnsureCalibFonts();
            string instructions = GetCalibrationInstructions();
            // [V59-FIX-3c] EN: Panel starts BELOW the top sensor bar + its label
            //     (user: the panel was masking the bar and its "TOP" text).
            //     FR: Le panneau démarre SOUS la barre capteur haute + son libellé
            //     (user : le panneau masquait la barre et son texte « TOP »).
            int panelY = 30 + (int)_calibMarkerFont.Height + 45;
            var measureRect = new Rectangle(0, panelY, this.ClientSize.Width, this.ClientSize.Height / 3);
            var textSize = TextRenderer.MeasureText(e.Graphics, instructions, _calibInstructionsFont,
                new Size(measureRect.Width - 64, measureRect.Height), TextFormatFlags.WordBreak);
            int panelW = Math.Min(measureRect.Width - 32, textSize.Width + 56);
            int panelH = textSize.Height + 28;
            int panelX = (this.ClientSize.Width - panelW) / 2;
            using (var panelBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(panelBrush, panelX, measureRect.Y, panelW, panelH);
            }
            var textRect = new Rectangle(panelX + 28, measureRect.Y + 14, panelW - 56, panelH - 28);
            TextRenderer.DrawText(e.Graphics, instructions, _calibInstructionsFont,
                textRect, System.Drawing.Color.White, System.Drawing.Color.Transparent,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
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
                    // [V59-FIX-8] EN/FR: CROSS calibration (user spec) — Center → Top
                    //     → Bottom → Left → Right. The top/bottom clicks tag the center
                    //     of each LED bar; left/right tag the horizontal extent.
                    //     FR: Calibration CROIX (spec user) — Centre → Haut → Bas →
                    //     Gauche → Droite. Les clics haut/bas taguent le centre de chaque
                    //     barre LED ; gauche/droite donnent l'extension horizontale.
                    switch (_currentCalibrationStep)
                    {
                        case 0: return new Point(centerX, centerY);                // Center
                        case 1: return new Point(centerX, margin);                 // Top-center
                        case 2: return new Point(centerX, h - margin);             // Bottom-center
                        case 3: return new Point(margin, centerY);                 // Left-center
                        case 4: return new Point(w - margin, centerY);             // Right-center
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

                // [V59-FIX-4] EN: PnP Hybrid 4-point: same order as the capture code
                //     (TL→TR→BR→BL) — validates on the 4th point.
                //     FR: PnP Hybrid 4 points : même ordre que le code de capture
                //     (HG→HD→BD→BG) — validation au 4e point.
                case LEDLayoutType.WiimoteBarPnP:
                    if (!mTopLeft.HasValue)
                        return new Point(margin, margin);              // Step 1: Top-Left
                    else if (!mTopRight.HasValue)
                        return new Point(w - margin, margin);          // Step 2: Top-Right
                    else if (!mBottomRight.HasValue)
                        return new Point(w - margin, h - margin);       // Step 3: Bottom-Right
                    else if (!mBottomLeft.HasValue)
                        return new Point(margin, h - margin);           // Step 4: Bottom-Left
                    // All 4 captured — the calibration validates on the 4th point
                    // (EN/FR: 4 capturés — la calibration valide au 4e point)
                    break;
            }

            return new Point(w / 2, h / 2);
        }

        private void DrawLEDGuides(Graphics g)
        {
            using (Pen ledPen = new Pen(Color.Cyan, 3))
            using (Pen guidePen = new Pen(Color.Yellow, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                // [V59-FIX-3] EN/FR: Resolution-aware marker font (was fixed 12pt bold).
                EnsureCalibFonts();
                Font font = _calibMarkerFont;
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
                        // [V59-FIX-3b] EN/FR: Measured + centered label, font-aware offset.
                        var topLbl = g.MeasureString("SENSOR BAR (TOP)", font);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, w / 2 - topLbl.Width / 2, topY + 8);
                        
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
                        // Draw bottom sensor bar (same width as top, mirrored at bottom)
                        // (EN/FR: Dessiner la barre capteur BAS - même largeur que le haut, symétrique en bas)
                        int botY = h - margin;
                        g.DrawLine(ledPen, barStart, botY, barEnd, botY);
                        // [V59-FIX-3b] EN/FR: Measured + centered label, font-aware offset.
                        var botLbl = g.MeasureString("SENSOR BAR (BOTTOM)", font);
                        g.DrawString("SENSOR BAR (BOTTOM)", font, Brushes.Cyan, w / 2 - botLbl.Width / 2, botY - botLbl.Height - 8);

                        // Draw "OR" label at screen center to show that TOP and BOTTOM are both compatible
                        // (EN/FR: Afficher "OR / OU" au centre pour montrer que TOP et BOTTOM sont tous deux compatibles)
                        using (var orFont = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 22, System.Drawing.FontStyle.Bold))
                        {
                            string orText = "OR";
                            var orSize = g.MeasureString(orText, orFont);
                            // Draw a subtle dark background behind the text for readability
                            // (EN/FR: Dessiner un fond sombre derrière le texte pour lisibilité)
                            var orRect = new RectangleF(
                                w / 2 - orSize.Width / 2 - 10, centerY - orSize.Height / 2 - 5,
                                orSize.Width + 20, orSize.Height + 10);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), orRect);
                            g.DrawString(orText, orFont, Brushes.Yellow, w / 2 - orSize.Width / 2, centerY - orSize.Height / 2);
                        }

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

                        // Draw Top Sensor Bar (Left and Right LED from Top bar)
                        Point topBarStart = new Point(twoBarCenterX, margin);
                        Point topBarEnd = new Point(twoBarCenterX + twoBarWidth, margin);
                        g.DrawLine(ledPen, topBarStart, topBarEnd);
                        // [V59-FIX-3b] EN/FR: Measured + centered label.
                        var tTopLbl = g.MeasureString("SENSOR BAR (TOP)", font);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, centerX - tTopLbl.Width / 2, margin + 5);

                        // Draw Bottom Sensor Bar (Left and Right LED from Bottom bar)
                        Point botBarStart = new Point(twoBarCenterX, h - margin);
                        Point botBarEnd = new Point(twoBarCenterX + twoBarWidth, h - margin);
                        g.DrawLine(ledPen, botBarStart, botBarEnd);
                        // [V59-FIX-3b] EN/FR: Measured + centered label, font-aware offset.
                        var tBotLbl = g.MeasureString("SENSOR BAR (BOTTOM)", font);
                        g.DrawString("SENSOR BAR (BOTTOM)", font, Brushes.Cyan, centerX - tBotLbl.Width / 2, h - margin - tBotLbl.Height - 5);

                        // Draw calibration targets — [V59-FIX-8] CROSS (user spec):
                        // Center → Top-center → Bottom-center → Left-center → Right-center
                        // (EN/FR: Cibles de calibration — CROIX (spec user) : Centre →
                        // Haut-centre → Bas-centre → Gauche-centre → Droite-centre)
                        Point twoBarTop = new Point(centerX, margin);
                        Point twoBarBottom = new Point(centerX, h - margin);
                        Point twoBarLeft = new Point(margin, centerY);
                        Point twoBarRight = new Point(w - margin, centerY);

                        // Draw Center Marker (Step 0)
                        DrawLEDMarker(g, new Point(centerX, centerY), "CENTER", _currentCalibrationStep == 0);
                        DrawLEDMarker(g, twoBarTop, "TOP", _currentCalibrationStep == 1);
                        DrawLEDMarker(g, twoBarBottom, "BOTTOM", _currentCalibrationStep == 2);
                        DrawLEDMarker(g, twoBarLeft, "LEFT", _currentCalibrationStep == 3);
                        DrawLEDMarker(g, twoBarRight, "RIGHT", _currentCalibrationStep == 4);

                        var tTitleLbl = g.MeasureString("2 WIIMOTE BARS - CROSS CALIBRATION", font);
                        g.DrawString("2 WIIMOTE BARS - CROSS CALIBRATION", font, Brushes.Cyan, centerX - tTitleLbl.Width / 2, centerY + 80);
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

                    // [V59-FIX-4] EN: PnP Hybrid — same visual base as WiimoteBar
                    //     (user request: reuse the base mode target points). 4 corners
                    //     ONLY — the 5th (CENTER) point was removed (user decision).
                    //     Active indicators follow the step counter (0=TL..3=BL).
                    //     FR: PnP Hybrid — même base visuelle que WiimoteBar (demande
                    //     user : reprendre les points de visée du mode de base).
                    //     4 coins SEULEMENT — le 5e point (CENTRE) a été retiré
                    //     (décision user). Les indicateurs suivent le compteur (0=HG..3=BG).
                    case LEDLayoutType.WiimoteBarPnP:
                        // Sensor bar references, top and bottom (same as WiimoteBar)
                        // (EN/FR: Barres capteur haut et bas, identiques à WiimoteBar)
                        int pnpBarWidth = (int)(w * 0.25f);
                        int pnpBarStart = (w - pnpBarWidth) / 2;
                        int pnpBarEnd = pnpBarStart + pnpBarWidth;
                        int pnpTopY = margin;
                        g.DrawLine(ledPen, pnpBarStart, pnpTopY, pnpBarEnd, pnpTopY);
                        // [V59-FIX-3b] EN/FR: Measured + centered label, font-aware offset.
                        var pnpTopLbl = g.MeasureString("SENSOR BAR (TOP)", font);
                        g.DrawString("SENSOR BAR (TOP)", font, Brushes.Cyan, w / 2 - pnpTopLbl.Width / 2, pnpTopY + 8);

                        int pnpBotY = h - margin;
                        g.DrawLine(ledPen, pnpBarStart, pnpBotY, pnpBarEnd, pnpBotY);
                        // [V59-FIX-3b] EN/FR: Measured + centered label, kept INSIDE the screen.
                        var pnpBotLbl = g.MeasureString("SENSOR BAR (BOTTOM)", font);
                        g.DrawString("SENSOR BAR (BOTTOM)", font, Brushes.Cyan, w / 2 - pnpBotLbl.Width / 2, pnpBotY - pnpBotLbl.Height - 8);

                        // 4 corner markers (steps 0-3)
                        // (EN/FR: 4 marqueurs coins (étapes 0-3))
                        Point pnpTopLeftPoint = new Point(margin, margin);
                        Point pnpTopRightPoint = new Point(w - margin, margin);
                        Point pnpBottomRightPoint = new Point(w - margin, h - margin);
                        Point pnpBottomLeftPoint = new Point(margin, h - margin);

                        DrawLEDMarker(g, pnpTopLeftPoint, "TL", _currentCalibrationStep == 0);
                        DrawLEDMarker(g, pnpTopRightPoint, "TR", _currentCalibrationStep == 1);
                        DrawLEDMarker(g, pnpBottomRightPoint, "BR", _currentCalibrationStep == 2);
                        DrawLEDMarker(g, pnpBottomLeftPoint, "BL", _currentCalibrationStep == 3);

                        // "OR" label at screen center — both TOP and BOTTOM bar
                        // placements stay compatible (same as WiimoteBar).
                        // (EN/FR: Libellé « OR » au centre — les barres HAUT et BAS
                        // restent compatibles, comme WiimoteBar.)
                        {
                            // [V59-FIX-3] EN/FR: Use the shared resolution-aware marker font.
                            string pnpOrText = "OR";
                            var pnpOrSize = g.MeasureString(pnpOrText, _calibMarkerFont);
                            var pnpOrRect = new RectangleF(
                                w / 2 - pnpOrSize.Width / 2 - 10, centerY - pnpOrSize.Height / 2 - 5,
                                pnpOrSize.Width + 20, pnpOrSize.Height + 10);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), pnpOrRect);
                            g.DrawString(pnpOrText, _calibMarkerFont, Brushes.Yellow, w / 2 - pnpOrSize.Width / 2, centerY - pnpOrSize.Height / 2);
                        }

                        var pnpTitleLbl = g.MeasureString(string.Format("PnP HYBRID CALIBRATION ({0}/{1})", _currentCalibrationStep + 1, _totalCalibrationSteps), font);
                        g.DrawString(string.Format("PnP HYBRID CALIBRATION ({0}/{1})", _currentCalibrationStep + 1, _totalCalibrationSteps), font, Brushes.Cyan, centerX - pnpTitleLbl.Width / 2, centerY + 80);
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

            // [V59-FIX-3] EN/FR: Resolution-aware marker label (was fixed 12pt bold).
            // [V59-FIX-3b] EN/FR: Font-aware vertical offset (was fixed -22).
            var labelSize = g.MeasureString(label, _calibMarkerFont);
            g.DrawString(label, _calibMarkerFont, Brushes.Yellow,
                position.X - labelSize.Width / 2, position.Y - size / 2 - _calibMarkerFont.Height - 6);
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
                        return string.Format("Calibrating Gun4IR ({0}/{1})\r\n\r\nAim at {2} and press A or B", _currentCalibrationStep + 1, _totalCalibrationSteps, diamondSteps[_currentCalibrationStep]);
                    break;

                case LEDLayoutType.TwoWiimoteBar:
                    // [V59-FIX-8] EN/FR: Cross order (user spec) — Center, Top, Bottom,
                    //     Left, Right. The top/bottom clicks tag each LED bar's center.
                    string[] twoBarSteps = { "CENTER / CENTRE", "TOP-CENTER / HAUT-CENTRE (barre haute)",
                                             "BOTTOM-CENTER / BAS-CENTRE (barre basse)",
                                             "LEFT-CENTER / GAUCHE-CENTRE", "RIGHT-CENTER / DROITE-CENTRE" };
                    if (_currentCalibrationStep < twoBarSteps.Length)
                    {
                        return string.Format("Calibrating 2 Wiimote Bars ({0}/5)\r\n\r\nAim at {1} and press A or B",
                            _currentCalibrationStep + 1, twoBarSteps[_currentCalibrationStep]);
                    }
                    break;

                case LEDLayoutType.FourCorners:
                    string[] fcSteps = { "CENTER", "TOP-LEFT", "TOP-RIGHT", "BOTTOM-RIGHT", "BOTTOM-LEFT" };
                    if (_currentCalibrationStep < fcSteps.Length)
                    {
                        return string.Format("Calibrating 4-Corners ({0}/{1})\r\n\r\nAim at {2} and press A or B", _currentCalibrationStep + 1, _totalCalibrationSteps, fcSteps[_currentCalibrationStep]);
                    }
                    break;

                // [V59-FIX-4] EN: PnP Hybrid 4-point calibration — 4 corners with
                //     distance display. Validates on the 4th point.
                //     FR: Calibration PnP Hybrid 4 points — 4 coins avec affichage
                //     distance. Validation au 4e point.
                case LEDLayoutType.WiimoteBarPnP:
                    string[] pnpSteps = { "TOP-LEFT / HAUT-GAUCHE", "TOP-RIGHT / HAUT-DROIT",
                                           "BOTTOM-RIGHT / BAS-DROIT", "BOTTOM-LEFT / BAS-GAUCHE" };
                    if (_currentCalibrationStep < pnpSteps.Length)
                    {
                        string dist = _lastDisplayedDistance > 0
                            ? string.Format("\r\n[dist: {0:F2} m]", _lastDisplayedDistance)
                            : "";
                        string warn = _distanceRejectCount > 0
                            ? "\r\n! Distance mismatch (>12%) — hold the same stance (trial " + _distanceRejectCount + "/3)"
                            : "";
                        // [V59-FIX-3] EN/FR: Two-click capture mention + confirm prompt
                        //     between the two clicks (median capture).
                        //     Mention des 2 clics par point + invite de confirmation
                        //     entre les 2 clics (capture médiane).
                        string click = _pnpConfirmMode
                            ? "\r\n>> CLICK AGAIN to CONFIRM / CLIQUEZ A NOUVEAU pour CONFIRMER <<"
                            : "\r\nAim and click TWICE on each target / Visez et cliquez DEUX FOIS sur chaque cible";
                        return string.Format("Calibrating PnP HYBRID ({0}/{1})\r\n\r\nAim at {2} and press A or B{3}{4}{5}",
                            _currentCalibrationStep + 1, _totalCalibrationSteps, pnpSteps[_currentCalibrationStep], click, dist, warn);
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
                // [V59] EN: Reset distance validation state for the new point
                //     FR: Réinitialiser l'état de validation de distance pour le nouveau point
                _distanceRejectCount = 0;
                Invalidate();
            }
        }

        /// <summary>
        /// [V59] EN: Update the gun-to-screen distance display (PnP mode only).
        ///     Called from ScreenPositionCalculator on each frame during calibration.
        ///     FR: Mettre à jour l'affichage de la distance gun-écran (mode PnP uniquement).
        ///     Appelé depuis ScreenPositionCalculator à chaque frame pendant la calibration.
        /// </summary>
        public void UpdateDistance(float distanceM)
        {
            if (Math.Abs(_lastDisplayedDistance - distanceM) > 0.01f)
            {
                _lastDisplayedDistance = distanceM;
                if (!this.IsDisposed && !this.Disposing)
                {
                    try { this.Invalidate(); } catch { }
                }
            }
        }

        /// <summary>
        /// [V59] EN: Validate the distance for the current calibration point (PnP mode).
        ///     Returns true if the point can be accepted, false if the user is at the
        ///     wrong distance (distance mismatch with the previous point).
        ///     After 3 consecutive rejections, the point is accepted with a warning
        ///     (anti-deadlock).
        ///     FR: Valider la distance pour le point de calibration courant (mode PnP).
        ///     Renvoie true si le point peut être accepté, false si l'utilisateur est à
        ///     la mauvaise distance (écart avec le point précédent). Après 3 refus
        ///     consécutifs, le point est accepté avec un avertissement (anti-blocage).
        /// </summary>
        public bool ValidateDistance(float distanceM)
        {
            // EN/FR: First accepted point sets the reference
            if (_referenceDistance < 0)
            {
                _referenceDistance = distanceM;
                _distanceRejectCount = 0;
                return true;
            }

            // EN/FR: Check tolerance (12% of reference — see DistanceTolerance rationale above)
            if (Math.Abs(distanceM - _referenceDistance) / _referenceDistance <= DistanceTolerance)
            {
                _distanceRejectCount = 0;
                return true;
            }

            // EN/FR: Distance mismatch — reject (up to 3 times, then accept with warning)
            _distanceRejectCount++;
            if (_distanceRejectCount >= 3)
            {
                _distanceRejectCount = 0;
                return true; // EN/FR: Anti-deadlock — accept after 3 tries
            }
            return false;
        }

        /// <summary>
        /// [V59-FIXES] EN: Current consecutive distance-rejection count (PnP logging).
        ///     FR: Nombre actuel de refus de distance consécutifs (logs PnP).
        /// </summary>
        public int DistanceRejectCount { get { return _distanceRejectCount; } }

        // [V59-FIX-3] EN: PnP two-click capture — when true, the instructions show
        //     "CLICK AGAIN to CONFIRM" between the two clicks of a point.
        //     FR: Capture PnP en 2 clics — si vrai, les instructions affichent
        //     « CLIQUEZ A NOUVEAU pour CONFIRMER » entre les 2 clics d'un point.
        private bool _pnpConfirmMode = false;
        public void SetPnpConfirmMode(bool armed)
        {
            if (_pnpConfirmMode != armed)
            {
                _pnpConfirmMode = armed;
                Invalidate();
            }
        }

        // [V59-FIX-3] EN: Resolution-aware fonts for the painted calibration texts.
        //     The previous fixed 12-14pt fonts were unreadable on big high-resolution
        //     screens (user request). Sizes scale with the form (fullscreen = screen)
        //     height and refresh lazily, like the IR visualizer's EnsureFonts.
        //     FR: Polices adaptées à la résolution pour les textes peints de la
        //     calibration. Les polices fixes 12-14 pt précédentes étaient illisibles
        //     sur grand écran haute résolution (demande user). Les tailles suivent la
        //     hauteur du form (plein écran = écran) et se rafraîchissent paresseusement,
        //     comme le EnsureFonts du visualiseur IR.
        private Font _calibInstructionsFont;
        private Font _calibMarkerFont;
        private float _calibFontSize;

        private void EnsureCalibFonts()
        {
            int h = this.Height;
            if (h <= 0) h = Screen.PrimaryScreen.Bounds.Height;
            // [V59-FIX-3b] EN/FR: HALVED sizes (user: "abusively big, texts masking the
            // IR visualizer behind"). ~16pt/1080p, ~22pt/1440p, clamped 14-28.
            float target = Math.Max(14f, Math.Min(28f, h / 65f));
            // Markers: ~12pt/1080p, ~16pt/1440p, clamped 11-18.
            float marker = Math.Max(11f, Math.Min(18f, h / 90f));
            if (_calibInstructionsFont == null || Math.Abs(_calibFontSize - target) > 0.5f)
            {
                if (_calibInstructionsFont != null) _calibInstructionsFont.Dispose();
                if (_calibMarkerFont != null) _calibMarkerFont.Dispose();
                _calibFontSize = target;
                _calibInstructionsFont = new System.Drawing.Font(
                    System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, target, System.Drawing.FontStyle.Bold);
                _calibMarkerFont = new System.Drawing.Font(
                    System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, marker, System.Drawing.FontStyle.Bold);
            }
        }

        // [V59-FIX-3] EN/FR: Dispose the cached calibration fonts.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_calibInstructionsFont != null) { _calibInstructionsFont.Dispose(); _calibInstructionsFont = null; }
                if (_calibMarkerFont != null) { _calibMarkerFont.Dispose(); _calibMarkerFont = null; }
            }
            base.Dispose(disposing);
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
            // [V59-FIX-4] PnP Hybrid: 4 corners (the 5th center point was removed)
            get
            {
                if (_ledLayout == LEDLayoutType.WiimoteBarPnP)
                    return mTopLeft.HasValue && mTopRight.HasValue && mBottomRight.HasValue &&
                           mBottomLeft.HasValue;
                return mTopLeft.HasValue && mTopRight.HasValue && mBottomRight.HasValue;
            }
        }

        // [V59] EN: Capture the center point (PnP mode, 5th calibration point).
        //     FR: Capturer le point centre (mode PnP, 5e point de calibration).
        public void CaptureCenter(Point2F center)
        {
            mCenter = center;
            Invalidate();
        }

        // [V59] EN: Reset the center point (used when calibration restarts).
        //     FR: Réinitialiser le point centre (utilisé quand la calibration redémarre).
        public void ResetCenter()
        {
            mCenter = null;
        }

        // [V59] EN: Reset all static calibration points (4 corners + center).
        //     FR: Réinitialiser tous les points de calibration statiques (4 coins + centre).
        public void ResetAllPoints()
        {
            mTopLeft = null;
            mTopRight = null;
            mBottomRight = null;
            mBottomLeft = null;
            mCenter = null;
            Invalidate();
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
                    string.Format("Capture_Calib_{0}", timestamp));
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
                log.AppendLine(string.Format("Timestamp: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
                log.AppendLine(string.Format("LED Layout: {0}", _ledLayout));
                log.AppendLine(string.Format("Step: {0}/{1}", _currentCalibrationStep + 1, _totalCalibrationSteps));
                log.AppendLine();

                // Log Wiimote States
                var wiimotes = WiimoteLib.WiimoteManager.ConnectedWiimotes;
                for (int i = 0; i < wiimotes.Length; i++)
                {
                    var wm = wiimotes[i];
                    log.AppendLine(string.Format("--- Wiimote {0} ({1}) ---", i + 1, wm.DevicePath));
                    var ir = wm.WiimoteState.IRState;
                    log.AppendLine(string.Format("IR Mode: {0}, Sensitivity: {1}", ir.Mode, ir.Sensitivity));
                    for (int p = 0; p < 4; p++)
                    {
                        if (ir[p].Found)
                            log.AppendLine(string.Format("  Point {0}: ({1}, {2}) Size: {3}", p, ir[p].Position.X, ir[p].Position.Y, ir[p].Size));
                    }
                }

                string logPath = System.IO.Path.Combine(debugFolder, "calibration_log.txt");
                System.IO.File.WriteAllText(logPath, log.ToString());

                // Visual feedback (Flash opacity)
                this.Opacity = 0.4;
                var t = new Timer();
                t.Interval = 100;
                // [V59-FIXES] Restore the global 0.7 calibration opacity
                t.Tick += (s, args) => { this.Opacity = 0.7; t.Stop(); t.Dispose(); };
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Debug Capture Failed: " + ex.Message);
            }
        }
    }

}
