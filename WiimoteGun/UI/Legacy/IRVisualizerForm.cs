using System;
using System.Drawing;
using System.Windows.Forms;
using WiimoteLib;
using WiimoteLib.DataTypes;
using WiimoteLib.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun.UI.Legacy
{
    public partial class IRVisualizerForm : Form
    {
        private Dictionary<string, WiimoteState> _lastStates = new Dictionary<string, WiimoteState>();
        private int _selectedPlayerIndex = 1;

        // [V59-FIXES] EN: Large, zoom- and resolution-aware fonts for the painted HUD
        //     texts (the previous SystemFonts.DefaultFont ~8pt was unreadable on big
        //     screens / from a distance — user request).
        //     FR: Polices larges, sensibles au zoom et à la résolution pour les textes
        //     HUD peints (l'ancienne SystemFonts.DefaultFont ~8pt était illisible sur
        //     grand écran / de loin — demande user).
        private Font _hudFont;
        private Font _pointFont;
        private float _hudFontSize;
        // [V59-FIX-8c] EN/FR: TWB cross overlay points (5 clicks, drawn as two axes).
        private readonly List<PointF> _twbOverlayPts = new List<PointF>();

        private void EnsureFonts()
        {
            int h = this.ClientSize.Height;
            if (h <= 0) h = Screen.PrimaryScreen.Bounds.Height;
            // [V59-FIX-3c] EN/FR: "un peu" smaller (user request) — ~17pt on 1080p,
            // ~22pt on 1440p, ~28pt on 4K (was ~24/32/40pt before).
            float target = Math.Max(12f, Math.Min(28f, h / 65f)) * WiimoteGun.UI.UiScaler.Factor;
            if (_hudFont == null || Math.Abs(_hudFontSize - target) > 0.5f)
            {
                if (_hudFont != null) _hudFont.Dispose();
                if (_pointFont != null) _pointFont.Dispose();
                _hudFontSize = target;
                _hudFont = new Font("Segoe UI", target, FontStyle.Bold);
                _pointFont = new Font("Segoe UI", Math.Max(11f, target * 0.75f), FontStyle.Bold);
            }
        }

        public IRVisualizerForm()
        {
            InitializeComponent();
            // [V58-scaling-fix] EN: This is a FULLSCREEN form managing its own bounds
            //     (Location + WindowState.Maximized set below). ApplyForm would scale
            //     and clamp the small Designer-set size BEFORE the fullscreen setup,
            //     breaking the maximization at zoom > 100%. ApplyFonts scales the
            //     fonts only - the fullscreen sizing handles the rest.
            //     FR: C'est un form PLEIN ÉCRAN qui gère ses propres bornes (Location +
            //     WindowState.Maximized ci-dessous). ApplyForm scalerait et clamperait
            //     la petite taille du Designer AVANT le passage plein écran, cassant
            //     la maximisation au zoom > 100 %. ApplyFonts ne scale que les polices
            //     - le dimensionnement plein écran fait le reste.
            WiimoteGun.UI.UiScaler.ApplyFonts(this);

            // Position on correct screen (EN/FR: Positionner sur le bon écran)
            int screenIndex = Options.Instance.MonitorId;
            Screen screen = Screen.PrimaryScreen;
            if (screenIndex >= 0 && screenIndex < Screen.AllScreens.Length)
            {
                screen = Screen.AllScreens[screenIndex];
            }

            this.StartPosition = FormStartPosition.Manual;
            var bounds = screen.Bounds;
            this.Location = new Point(bounds.Left, bounds.Top);
            
            // Full screen setup (EN/FR: Configuration plein écran)
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            this.FormClosing += IRVisualizerForm_FormClosing;
            this.Paint += IRVisualizerForm_Paint;
            this.KeyDown += IRVisualizerForm_KeyDown;
            
            // Attach Timer Event manually (not in designer for this)
            _updateTimer.Tick += _updateTimer_Tick;
            _updateTimer.Start();

            // Init Combo
            cmbPlayer.SelectedIndex = 0;
            
            UpdateOffsetLabel();
            
            // Default selection

            // Add Close Button for Wiimote usage (EN/FR: Ajouter bouton fermer pour usage Wiimote)
            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            btnClose.ForeColor = Color.White;
            btnClose.BackColor = Color.Red;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(60, 60);
            btnClose.Location = new Point(this.ClientSize.Width - 60, 0);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
            btnClose.BringToFront();
        }

        private void CmbPlayer_KeyDown(object sender, KeyEventArgs ke)
        {
            // Check if any Wiimote has Home pressed (EN/FR: Vérifier si une Wiimote a Home pressé)
            bool homePressed = false;
            var wiimotes = WiimoteManager.ConnectedWiimotes;
            foreach (var wm in wiimotes)
            {
                if (wm.WiimoteState != null && (wm.WiimoteState.Buttons.Home || wm.WiimoteState.Buttons.Minus))
                {
                    homePressed = true;
                    break;
                }
            }
            
            // Block D-pad keys if Home is pressed (EN/FR: Bloquer touches D-pad si Home pressé)
            if (homePressed && (ke.KeyCode == Keys.Up || ke.KeyCode == Keys.Down || ke.KeyCode == Keys.Left || ke.KeyCode == Keys.Right))
            {
                ke.Handled = true;
                ke.SuppressKeyPress = true;
            }
        }

        private void BtnSaveOffset_Click(object sender, EventArgs e) 
        {
            Options.Instance.Save();
            MessageBox.Show($"Offset saved for Player {_selectedPlayerIndex}!\n\nX: {Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex)}\nY: {Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex)}", 
                "Offset Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Debug capture with F12 key (EN/FR: Capture debug avec touche F12)
        private void IRVisualizerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
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
                    $"Capture_{timestamp}");
                System.IO.Directory.CreateDirectory(debugFolder);

                // 1. Capture screenshot (EN/FR: Capturer capture d'écran)
                Bitmap screenshot = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                this.DrawToBitmap(screenshot, new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height));
                string imagePath = System.IO.Path.Combine(debugFolder, "visualizer.png");
                screenshot.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                screenshot.Dispose();

                // 2. Generate detailed log (EN/FR: Générer log détaillé)
                var log = new System.Text.StringBuilder();
                log.AppendLine("=== WiimoteGun IR Visualizer Debug Snapshot ===");
                log.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                log.AppendLine($"LED Layout: {Options.Instance.LEDLayout}");
                log.AppendLine();

                var wiimotes = WiimoteManager.ConnectedWiimotes;
                for (int i = 0; i < wiimotes.Length; i++)
                {
                    var wm = wiimotes[i];
                    log.AppendLine($"--- Wiimote {i + 1} ({wm.DevicePath}) ---");

                    if (_lastStates.ContainsKey(wm.DevicePath))
                    {
                        var state = _lastStates[wm.DevicePath];
                        var ir = state.IRState;

                        log.AppendLine($"IR Mode: {ir.Mode}, Sensitivity: {ir.Sensitivity}");

                        // Log visible IR points
                        var effLayout = Options.Instance.GetEffectiveLedLayout();
                        bool isBarLayout = (effLayout == LEDLayoutType.WiimoteBar || effLayout == LEDLayoutType.WiimoteBarPnP);
                        int maxLeds = isBarLayout ? 2 : 4;

                        int visibleCount = 0;
                        for (int p = 0; p < maxLeds; p++)
                        {
                            if (ir[p].Found)
                            {
                                visibleCount++;
                                var rawPos = ir[p].RawPosition;
                                var normPos = ir[p].Position;
                                log.AppendLine($"  P{p + 1}: Raw({rawPos.X}, {rawPos.Y}) Norm({normPos.X:F4}, {normPos.Y:F4}) Size={ir[p].Size}");
                            }
                        }
                        // [V59-FIX-11/15] EN: The denominator follows the layout — 2 LEDs for
                        //     the Wiimote Bar modes (standard + PNP-Hybrid), 4 for the
                        //     4-LED layouts (user fix: "/4" was misleading on 2-LED bars).
                        //     FR: Le dénominateur suit le layout — 2 LEDs pour les modes
                        //     Wiimote Bar (standard + PNP-Hybrid), 4 pour les layouts
                        //     4 LEDs (correctif user : « /4 » trompeur sur les barres 2 LEDs).
                        log.AppendLine($"Visible Points: {visibleCount}/{maxLeds}");

                        // Log calculated center
                        var controller = Program.WiiMoteManager?.GetController(wm.ID);
                        if (controller != null && controller.Calculator != null)
                        {
                            var center = controller.Calculator.LastCalculatedCenter;
                            float normX = center.X / 1023f;
                            float normY = center.Y / 767f;
                            log.AppendLine($"Calculated Center: Raw({center.X:F2}, {center.Y:F2}) Norm({normX:F4}, {normY:F4})");
                            log.AppendLine($"Is Calibrated: {controller.Calculator.IsCalibrated}");
                        }
                        else
                        {
                            log.AppendLine("Calculator: Not available");
                        }
                    }
                    else
                    {
                        log.AppendLine("No state data available");
                    }
                    log.AppendLine();
                }

                string logPath = System.IO.Path.Combine(debugFolder, "debug_log.txt");
                System.IO.File.WriteAllText(logPath, log.ToString());

                // Show confirmation (EN/FR: Afficher confirmation)
                this.Text = $"Wiimote IR Visualizer - Captured to {debugFolder}";
                System.Threading.Timer resetTitle = null;
                resetTitle = new System.Threading.Timer(_ =>
                {
                    this.Invoke(new Action(() => this.Text = "Wiimote IR Visualizer"));
                    resetTitle?.Dispose();
                }, null, 2000, System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to capture debug snapshot: {ex.Message}",
                    "Capture Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void IRVisualizerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _updateTimer.Stop();
            // [V59-FIXES] Dispose the HUD fonts (EN/FR: Libérer les polices HUD)
            if (_hudFont != null) { _hudFont.Dispose(); _hudFont = null; }
            if (_pointFont != null) { _pointFont.Dispose(); _pointFont = null; }
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            // Poll current states and handle Wiimote input for offset adjustment (EN/FR: Récupérer états et gérer entrées Wiimote pour ajustement offset)
            var wiimotes = WiimoteManager.ConnectedWiimotes;
            foreach (var wm in wiimotes)
            {
                if (wm.WiimoteState != null)
                {
                    _lastStates[wm.DevicePath] = wm.WiimoteState;
                    
                    // Check for Home + D-pad combination for selected player (EN/FR: Vérifier combinaison Home + D-pad pour joueur sélectionné)
                    var controller = Program.WiiMoteManager?.GetController(wm.ID);
                    if (controller != null && controller.PlayerIndex == _selectedPlayerIndex)
                    {
                        var buttons = wm.WiimoteState.Buttons;
                        
                        // Home button OR Minus button must be held (EN/FR: Bouton Home OU Moins doit être maintenu)
                        // Minus is needed for DolphinBar where HOME changes mode
                        if (buttons.Home || buttons.Minus)
                        {
                            // Apply offset adjustments on D-pad press (EN/FR: Appliquer ajustements offset sur pression D-pad)
                            if (buttons.Left)
                            {
                                int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex);
                                Options.Instance.SetDynamicPerspectiveOffsetX(_selectedPlayerIndex, currentOffset - 1);
                                UpdateOffsetLabel();
                            }
                            else if (buttons.Right)
                            {
                                int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex);
                                Options.Instance.SetDynamicPerspectiveOffsetX(_selectedPlayerIndex, currentOffset + 1);
                                UpdateOffsetLabel();
                            }
                            
                            if (buttons.Up)
                            {
                                int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex);
                                Options.Instance.SetDynamicPerspectiveOffsetY(_selectedPlayerIndex, currentOffset - 1);
                                UpdateOffsetLabel();
                            }
                            else if (buttons.Down)
                            {
                                int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex);
                                Options.Instance.SetDynamicPerspectiveOffsetY(_selectedPlayerIndex, currentOffset + 1);
                                UpdateOffsetLabel();
                            }
                        }
                    }
                }
            }
            this.Invalidate();
        }

        private void CmbPlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedPlayerIndex = cmbPlayer.SelectedIndex + 1;
            UpdateOffsetLabel();
        }

        private void BtnOffsetLeft_Click(object sender, EventArgs e)
        {
            // Decrease offsetX by -5 for selected player (EN/FR: Diminuer offsetX de -5 pour le joueur sélectionné)
            int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex);
            Options.Instance.SetDynamicPerspectiveOffsetX(_selectedPlayerIndex, currentOffset - 5);
            UpdateOffsetLabel();
        }

        private void BtnOffsetRight_Click(object sender, EventArgs e)
        {
            // Increase offsetX by +5 for selected player (EN/FR: Augmenter offsetX de +5 pour le joueur sélectionné)
            int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex);
            Options.Instance.SetDynamicPerspectiveOffsetX(_selectedPlayerIndex, currentOffset + 5);
            UpdateOffsetLabel();
        }

        private void BtnOffsetUp_Click(object sender, EventArgs e)
        {
            // Decrease offsetY by -5 for selected player (up in screen = lower Y) (EN/FR: Diminuer offsetY de -5 pour le joueur sélectionné)
            int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex);
            Options.Instance.SetDynamicPerspectiveOffsetY(_selectedPlayerIndex, currentOffset - 5);
            UpdateOffsetLabel();
        }

        private void BtnOffsetDown_Click(object sender, EventArgs e)
        {
            // Increase offsetY by +5 for selected player (down in screen = higher Y) (EN/FR: Augmenter offsetY de +5 pour le joueur sélectionné)
            int currentOffset = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex);
            Options.Instance.SetDynamicPerspectiveOffsetY(_selectedPlayerIndex, currentOffset + 5);
            UpdateOffsetLabel();
        }

        private void UpdateOffsetLabel()
        {
            // Display X/Y offsets for selected player (EN/FR: Afficher offsets X/Y pour le joueur sélectionné)
            int offsetX = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex);
            int offsetY = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex);
            lblOffsetValue.Text = $"X: {offsetX:+0;-0;0}, Y: {offsetY:+0;-0;0}";
        }

        private void IRVisualizerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // [V59-FIXES] Refresh the large HUD fonts for the current resolution/zoom
            //     (EN/FR: Rafraîchir les grandes polices HUD pour la résolution/zoom courant)
            EnsureFonts();
            g.Clear(Color.Black);

            var wiimotes = WiimoteManager.ConnectedWiimotes;
            if (wiimotes.Length == 0)
            {
                g.DrawString("No Wiimotes Connected", _hudFont, Brushes.White, 10, 10);
                return;
            }

            // Sort wiimotes by PlayerIndex to display in order P1, P2, P3, P4
            var sortedWiimotes = wiimotes
                .Select(wm => new { 
                    Wiimote = wm, 
                    PlayerIndex = Program.WiiMoteManager?.GetController(wm.ID)?.PlayerIndex ?? 999 
                })
                .OrderBy(x => x.PlayerIndex)
                .Select(x => x.Wiimote)
                .ToArray();

            int count = sortedWiimotes.Length;
            // Available width per remote (fullscreen split)
            int availWidthPerRemote = this.ClientSize.Width / count;
            int availHeight = this.ClientSize.Height;

            for (int i = 0; i < count; i++)
            {
                var wm = sortedWiimotes[i];
                // Base X offset for this player's slot
                int slotOffsetX = i * availWidthPerRemote;
                
                // --- ASPECT RATIO LOGIC (4:3) ---
                // We want to fit a 4:3 rectangle inside availWidthPerRemote x availHeight
                float targetRatio = 1024f / 768f; // ~1.333
                
                float drawW = availWidthPerRemote;
                float drawH = drawW / targetRatio;
                
                if (drawH > availHeight)
                {
                    // Too tall, limit by height
                    drawH = availHeight;
                    drawW = drawH * targetRatio;
                }
                
                // Center the drawing rectangle within the slot
                float rectX = slotOffsetX + (availWidthPerRemote - drawW) / 2f;
                float rectY = (availHeight - drawH) / 2f;
                
                // Draw border for the "screen" area
                g.DrawRectangle(Pens.DarkGray, rectX, rectY, drawW, drawH);

                // Get player index
                var controller = Program.WiiMoteManager?.GetController(wm.ID);
                int playerNumber = controller != null ? controller.PlayerIndex : (i + 1);
                
                if (wm == null || string.IsNullOrEmpty(wm.DevicePath))
                {
                    g.DrawString($"Player {playerNumber}\nDisconnected", _hudFont, Brushes.Red, rectX + 10, rectY + 10);
                    continue;
                }
                
                // Draw separator (between slots)
                if (i > 0)
                    using (Pen p = new Pen(Color.FromArgb(30, 30, 30)))
                        g.DrawLine(p, slotOffsetX, 0, slotOffsetX, availHeight);

                // [V59-FIXES] EN: Large, resolution-aware HUD lines with running Y
                //     positions (previously ~8pt at fixed offsets — unreadable from a
                //     distance, user request).
                //     FR: Lignes HUD larges, adaptées à la résolution, positions Y
                //     défilantes (avant : ~8pt à offsets fixes — illisible de loin).
                float lineH = _hudFont.GetHeight(g) + 4f;
                float textY = rectY + 10;
                g.DrawString($"Player {playerNumber}", _hudFont, Brushes.White, rectX + 10, textY);
                textY += lineH;

                // [V59-FIX-11/15] EN/FR: the EFFECTIVE layout (the hybrid checkbox makes
                //     Wiimote Bar display as PNP-Hybrid).
                var effLayout = Options.Instance.GetEffectiveLedLayout();
                bool isBarLayout = (effLayout == LEDLayoutType.WiimoteBar || effLayout == LEDLayoutType.WiimoteBarPnP);
                int maxLeds = isBarLayout ? 2 : 4;

                string layoutName = GetLayoutName(effLayout);
                g.DrawString($"LED Layout: {layoutName}", _hudFont, Brushes.Cyan, rectX + 10, textY);
                textY += lineH;

                if (_lastStates.ContainsKey(wm.DevicePath))
                {
                    var state = _lastStates[wm.DevicePath];
                    var ir = state.IRState;

                    g.DrawString($"Mode: {ir.Mode}, Sens: {ir.Sensitivity}", _hudFont, Brushes.White, rectX + 10, textY);
                    textY += lineH;

                    // [V59-FIXES] EN: Live PnP gun-to-screen distance for this player's slot.
                    //     FR: Distance PnP gun-écran en direct pour le créneau de ce joueur.
                    // [V59-FIX-11] EN: uses the EFFECTIVE layout (Wiimote Bar + the
                    //     hybrid checkbox behaves as PNP-Hybrid).
                    if (effLayout == LEDLayoutType.WiimoteBarPnP &&
                        controller != null && controller.Calculator != null)
                    {
                        float pnpDist = controller.Calculator.PnpLastDistanceM;
                        if (pnpDist > 0f)
                        {
                            // [V59-FIX-10] EN: with the declared geometry, append the
                            //     precision forecast (±mm per camera pixel at this range).
                            //     FR: avec la géométrie déclarée, ajoute la prévision de
                            //     précision (±mm par pixel caméra à cette portée).
                            string prec = Options.Instance.PnpDeclareGeometry
                                ? string.Format(" ±{0:F1} mm", controller.Calculator.PnpPrecisionMm)
                                : "";
                            g.DrawString($"PnP dist: {pnpDist:F2} m{prec}", _hudFont, Brushes.Orange, rectX + 10, textY);
                            textY += lineH;
                        }
                    }

                    // [V59-FIX-14] EN: LIVE TWB distance (user request — must display
                    //     from the moment LEDs are visible, before AND after calibration).
                    //     Uses the declared geometry; shows "declare geometry" hint if off.
                    //     FR: Distance TWB EN DIRECT (demande user — doit s'afficher dès
                    //     que les LEDs sont visibles, avant ET après calibration).
                    //     Utilise la géométrie déclarée ; suggestion si non déclarée.
                    if (Options.Instance.LEDLayout == LEDLayoutType.TwoWiimoteBar &&
                        controller != null && controller.Calculator != null)
                    {
                        float twbDist = controller.Calculator.TwbLastDistanceM;
                        if (twbDist > 0f)
                        {
                            g.DrawString($"TWB dist: {twbDist:F2} m", _hudFont, Brushes.Orange, rectX + 10, textY);
                            textY += lineH;
                        }
                    }

                    // [V59-FIX-9b] EN: TWB hybrid separation ratio (user checkbox) —
                    //     visual feedback like the PnP dist line. The RATIO is shown
                    //     (no absolute meters: the bar-pair spacing is unknown for
                    //     BlancoM vs Wii bars — the ratio is what the correction uses).
                    //     FR: Ratio de séparation hybride TWB (case user) — retour
                    //     visuel comme la ligne PnP dist. On affiche le RATIO (pas de
                    //     mètres absolus : l'écartement des barres est inconnu pour
                    //     BlancoM vs barres Wii — le ratio est ce que la correction
                    //     utilise).
                    if (Options.Instance.LEDLayout == LEDLayoutType.TwoWiimoteBar &&
                        Options.Instance.TwoBarHybridPnp &&
                        controller != null && controller.Calculator != null &&
                        controller.Calculator.TwbCalPairSep > 0.01f)
                    {
                        g.DrawString(string.Format("TWB ratio: {0:F2}", controller.Calculator.TwbLastSepRatio),
                            _hudFont, Brushes.Orange, rectX + 10, textY);
                        textY += lineH;
                    }

                    // Draw IR dots scaled to drawW/drawH (limited to maxLeds for the layout)
                    for (int p = 0; p < maxLeds; p++)
                    {
                        var point = ir[p];
                        if (point.Found)
                        {
                            // Scale 0-1023 to 0-drawW (Mirrored axis for intuitive view)
                            float x = rectX + (1.0f - point.RawPosition.X / 1023f) * drawW;
                            float y = rectY + (1.0f - (point.RawPosition.Y / 767f)) * drawH; // [V59-FIX-13] Y FLIPPED — matches the physical world (bar below screen = bottom of visualizer)
                            
                            // Scale dot size relative to screen size (min 5px)
                            float scaleFactor = drawW / 1024f; 
                            int size = (int)(point.Size * scaleFactor) + 5;
                            
                            g.FillEllipse(Brushes.Red, x - size/2, y - size/2, size, size);
                            // [V59-FIXES/FIX-15] 1-based point label (P1..P2 for bar, P1..P4 for 4-LED layouts)
                            g.DrawString($"P{p + 1}", _pointFont, Brushes.Yellow, x + 8, y - _pointFont.GetHeight(g) / 2);
                        }
                    }

                    // Draw Calibration Overlay
                    if (chkShowCalibration.Checked)
                    {
                        var calController = Program.WiiMoteManager?.GetController(wm.ID);
                        if (calController != null && calController.Calculator != null)
                        {
                            var calPoints = calController.Calculator.GetCalibrationPoints();
                            if (calPoints != null)
                            {
                                List<PointF> polyPoints = new List<PointF>();
                                _twbOverlayPts.Clear(); // [V59-FIX-8c] TWB cross axes
                                for (int k = 0; k < calPoints.Length; k++)
                                {
                                    if (calPoints[k].HasValue)
                                    {
                                        float cx = rectX + (1.0f - calPoints[k].Value.X) * drawW;
                                        float cy = rectY + (1.0f - calPoints[k].Value.Y) * drawH; // [V59-FIX-13] Y FLIPPED
                                        
                                        g.DrawEllipse(Pens.Magenta, cx - 5, cy - 5, 10, 10);
                                        
                                        string label = $"C{k}";
                                        if (calPoints.Length == 5)
                                        {
                                            // [V59-FIX-8c] EN: TwoWiimoteBars cross — correct labels
                                            //     (capture order: C, TOP, BOTTOM, LEFT, RIGHT) and NO
                                            //     bowtie polygon: draw the two cross axes instead.
                                            //     The generic gun4ir order (Center/TL/TR/BR/BL) does
                                            //     not match the cross capture order — it drew a
                                            //     misleading bowtie (user report).
                                            //     FR: Croix TwoWiimoteBars — libellés corrects
                                            //     (ordre de capture : C, HAUT, BAS, GAUCHE, DROITE)
                                            //     et PAS de polygone papillon : on trace les deux
                                            //     axes de la croix. L'ordre générique gun4ir
                                            //     (Center/TL/TR/BR/BL) ne correspond pas à l'ordre
                                            //     de capture croix — il dessinait un papillon
                                            //     trompeur (rapport user).
                                            if (Options.Instance.LEDLayout == LEDLayoutType.TwoWiimoteBar)
                                            {
                                                if (k == 0) label = "C";
                                                else if (k == 1) label = "TOP";
                                                else if (k == 2) label = "BOT";
                                                else if (k == 3) label = "LEFT";
                                                else if (k == 4) label = "RIGHT";
                                                _twbOverlayPts.Add(new PointF(cx, cy));
                                                if (_twbOverlayPts.Count == 5)
                                                {
                                                    // vertical axis: TOP(1) ↔ BOTTOM(2); horizontal: LEFT(3) ↔ RIGHT(4)
                                                    using (Pen crossPen = new Pen(Color.Magenta, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                                                    {
                                                        g.DrawLine(crossPen, _twbOverlayPts[1], _twbOverlayPts[2]);
                                                        g.DrawLine(crossPen, _twbOverlayPts[3], _twbOverlayPts[4]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (k==0) label="Center";
                                                else if (k==1) label="TL";
                                                else if (k==2) label="TR";
                                                else if (k==3) label="BR";
                                                else if (k==4) label="BL";

                                                // Add corners (1-4) to polygon
                                                if (k > 0) polyPoints.Add(new PointF(cx, cy));
                                            }
                                        }
                                        else if (calPoints.Length == 4) // Wiimote bar
                                        {
                                            // [V59-FIX-4/11] EN: PnP Hybrid — label the 4 captured corners
                                            //     (uses the EFFECTIVE layout — the hybrid checkbox
                                            //     makes Wiimote Bar behave as PNP-Hybrid).
                                            if (Options.Instance.GetEffectiveLedLayout() == LEDLayoutType.WiimoteBarPnP)
                                            {
                                                if (k == 0) label = "TL";
                                                else if (k == 1) label = "TR";
                                                else if (k == 2) label = "BR";
                                                else if (k == 3) label = "BL";
                                            }
                                             polyPoints.Add(new PointF(cx, cy));
                                        }

                                        g.DrawString(label, _pointFont, Brushes.Magenta, cx + 8, cy - _pointFont.GetHeight(g) / 2);
                                    }
                                }

                                if (polyPoints.Count >= 3) // Draw polygon if enough points
                                {
                                    // Hacky sort for 4 corners to ensure correct winding?
                                    // Usually points are stored in order, 1=TL, 2=TR, 3=BR, 4=BL (or similar)
                                    // WiimoteBar: TL, TR, BR, BL
                                    g.DrawPolygon(Pens.Magenta, polyPoints.ToArray());
                                }
                            }
                        }
                    }

                    // Draw Virtual Center
                    var centerController = Program.WiiMoteManager?.GetController(wm.ID);
                    if (centerController != null && centerController.Calculator != null)
                    {
                        var center = centerController.Calculator.LastCalculatedCenter;
                        if (center.X != 0 && center.Y != 0)
                        {
                            float cx = rectX + (1.0f - center.X / 1023f) * drawW;
                            float cy = rectY + (1.0f - (center.Y / 767f)) * drawH; // [V59-FIX-13] Y FLIPPED
                            
                            // Triangulation lines: WiimoteBar horizontal bar, Double-T, or Gun4IR/FourCorners radial
                            if (effLayout == LEDLayoutType.TwoWiimoteBar)
                            {
                                // [V59-FIX-27] EN: BlancoM style: No stem, no connecting lines between LED points, no center cross.
                                //     The visualizer displays ONLY the detected IR LED points (and HUD text), exactly as BlancoM does.
                                //     FR: Style BlancoM : Ni tige, ni traits de liaison entre points LED, ni croix centrale.
                                //     Le visualiseur affiche UNIQUEMENT les points LED IR détectés (et le HUD), exactement comme BlancoM.
                            }
                            else if (isBarLayout)
                            {
                                // [V59-FIX-15] Wiimote Bar: 2 LEDs on a single horizontal sensor bar
                                // Connects P1 - center - P2 horizontally along the bar (NOT radial diamond perspective)
                                bool p1Found = ir[0].Found;
                                bool p2Found = ir[1].Found;

                                if (p1Found && p2Found)
                                {
                                    float p1X = rectX + (1.0f - ir[0].RawPosition.X / 1023f) * drawW;
                                    float p1Y = rectY + (1.0f - (ir[0].RawPosition.Y / 767f)) * drawH;
                                    float p2X = rectX + (1.0f - ir[1].RawPosition.X / 1023f) * drawW;
                                    float p2Y = rectY + (1.0f - (ir[1].RawPosition.Y / 767f)) * drawH;

                                    g.DrawLine(Pens.DarkCyan, p1X, p1Y, cx, cy);
                                    g.DrawLine(Pens.DarkCyan, cx, cy, p2X, p2Y);
                                }
                                else if (p1Found)
                                {
                                    float p1X = rectX + (1.0f - ir[0].RawPosition.X / 1023f) * drawW;
                                    float p1Y = rectY + (1.0f - (ir[0].RawPosition.Y / 767f)) * drawH;
                                    g.DrawLine(Pens.DarkCyan, p1X, p1Y, cx, cy);
                                }
                                else if (p2Found)
                                {
                                    float p2X = rectX + (1.0f - ir[1].RawPosition.X / 1023f) * drawW;
                                    float p2Y = rectY + (1.0f - (ir[1].RawPosition.Y / 767f)) * drawH;
                                    g.DrawLine(Pens.DarkCyan, p2X, p2Y, cx, cy);
                                }
                            }
                            else
                            {
                                // [V59-FIX-15] Gun4IRDiamond, FourCorners: Radial perspective lines from each LED to center
                                for (int p = 0; p < maxLeds; p++)
                                {
                                    var point = ir[p];
                                    if (point.Found)
                                    {
                                        float px = rectX + (1.0f - point.RawPosition.X / 1023f) * drawW;
                                        float py = rectY + (1.0f - (point.RawPosition.Y / 767f)) * drawH; // [V59-FIX-15] Y FLIPPED — attaches directly to red LED dot
                                        g.DrawLine(Pens.DarkCyan, px, py, cx, cy);
                                    }
                                }
                            }
                            if (effLayout != LEDLayoutType.TwoWiimoteBar)
                            {
                                int crossSize = 10;
                                g.DrawLine(Pens.Cyan, cx - crossSize, cy, cx + crossSize, cy);
                                g.DrawLine(Pens.Cyan, cx, cy - crossSize, cx, cy + crossSize);
                                g.DrawString("CENTER", _pointFont, Brushes.Cyan, cx + 5, cy + 5);
                            }

                            // [V59-FIXES] EN: Debug info continues under the header lines
                            //     with the large HUD font (was ~8pt at fixed offsets).
                            //     FR: Les infos debug continuent sous les lignes d'en-tête
                            //     avec la grande police HUD (avant ~8pt à offsets fixes).
                            int visibleCount = 0;
                            for (int p = 0; p < maxLeds; p++) if (ir[p].Found) visibleCount++;

                            // [V59-FIX-11/15] EN: denominator = maxLeds (2 for bar modes, 4 for 4-LED layouts).
                            g.DrawString($"Visible Points: {visibleCount}/{maxLeds}", _hudFont, Brushes.LightGreen, rectX + 10, textY);
                            textY += lineH;

                            g.DrawString($"Center (norm): ({center.X/1023f:F3}, {center.Y/767f:F3})", _hudFont, Brushes.Cyan, rectX + 10, textY);
                        }
                    }
                }
            }
        }
        
        // Get human-readable LED Layout name (EN/FR: Obtenir nom lisible du LED Layout)
        private string GetLayoutName(LEDLayoutType layout)
        {
            switch (layout)
            {
                case LEDLayoutType.WiimoteBar:
                    return "Wiimote Bar";
                case LEDLayoutType.Gun4IRDiamond:
                    return "Gun4IR Diamond";
                case LEDLayoutType.TwoWiimoteBar:
                    return "2-Wiimote Bar (Top/Bottom)";
                case LEDLayoutType.FourCorners:
                    return "4 Corner LEDs";
                case LEDLayoutType.WiimoteBarPnP:
                    return "Wiimote Bar - PNP-Hybrid (alpha Test)"; // [V59-FIX-4] renamed per user decision
                default:
                    return layout.ToString();
            }
        }
    }
}
