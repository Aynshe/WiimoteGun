using System;
using System.Drawing;
using System.Windows.Forms;
using WiimoteLib;
using WiimoteLib.DataTypes;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun.UI.Legacy
{
    public partial class IRVisualizerForm : Form
    {
        private Dictionary<string, WiimoteState> _lastStates = new Dictionary<string, WiimoteState>();
        private int _selectedPlayerIndex = 1;

        public IRVisualizerForm()
        {
            InitializeComponent();

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
                if (wm.WiimoteState != null && wm.WiimoteState.Buttons.Home)
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
                        int visibleCount = 0;
                        for (int p = 0; p < 4; p++)
                        {
                            if (ir[p].Found)
                            {
                                visibleCount++;
                                var rawPos = ir[p].RawPosition;
                                var normPos = ir[p].Position;
                                log.AppendLine($"  P{p}: Raw({rawPos.X}, {rawPos.Y}) Norm({normPos.X:F4}, {normPos.Y:F4}) Size={ir[p].Size}");
                            }
                        }
                        log.AppendLine($"Visible Points: {visibleCount}/4");

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
                        
                        // Home button must be held (EN/FR: Bouton Home doit être maintenu)
                        if (buttons.Home)
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
            g.Clear(Color.Black);

            var wiimotes = WiimoteManager.ConnectedWiimotes;
            if (wiimotes.Length == 0)
            {
                g.DrawString("No Wiimotes Connected", SystemFonts.DefaultFont, Brushes.White, 10, 10);
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
                    g.DrawString($"Player {playerNumber}\nDisconnected", SystemFonts.DefaultFont, Brushes.Red, rectX + 10, rectY + 10);
                    continue;
                }
                
                // Draw separator (between slots)
                if (i > 0)
                    using (Pen p = new Pen(Color.FromArgb(30, 30, 30)))
                        g.DrawLine(p, slotOffsetX, 0, slotOffsetX, availHeight);

                g.DrawString($"Player {playerNumber}", SystemFonts.DefaultFont, Brushes.White, rectX + 10, rectY + 10);
                
                string layoutName = GetLayoutName(Options.Instance.LEDLayout);
                g.DrawString($"LED Layout: {layoutName}", SystemFonts.DefaultFont, Brushes.Cyan, rectX + 10, rectY + 30);

                if (_lastStates.ContainsKey(wm.DevicePath))
                {
                    var state = _lastStates[wm.DevicePath];
                    var ir = state.IRState;

                    g.DrawString($"Mode: {ir.Mode}, Sens: {ir.Sensitivity}", SystemFonts.DefaultFont, Brushes.White, rectX + 10, rectY + 50);

                    // Draw IR dots scaled to drawW/drawH
                    for (int p = 0; p < 4; p++)
                    {
                        var point = ir[p];
                        if (point.Found)
                        {
                            // Scale 0-1023 to 0-drawW
                            float x = rectX + (point.RawPosition.X / 1023f) * drawW;
                            float y = rectY + (point.RawPosition.Y / 767f) * drawH;
                            
                            // Scale dot size relative to screen size (min 5px)
                            float scaleFactor = drawW / 1024f; 
                            int size = (int)(point.Size * scaleFactor) + 5;
                            
                            g.FillEllipse(Brushes.Red, x - size/2, y - size/2, size, size);
                            g.DrawString($"P{p}", SystemFonts.DefaultFont, Brushes.Yellow, x, y);
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
                                for (int k = 0; k < calPoints.Length; k++)
                                {
                                    if (calPoints[k].HasValue)
                                    {
                                        float cx = rectX + (calPoints[k].Value.X) * drawW;
                                        float cy = rectY + (calPoints[k].Value.Y) * drawH;
                                        
                                        g.DrawEllipse(Pens.Magenta, cx - 5, cy - 5, 10, 10);
                                        
                                        string label = $"C{k}";
                                        if (calPoints.Length == 5)
                                        {
                                            if (k==0) label="Center";
                                            else if (k==1) label="TL";
                                            else if (k==2) label="TR";
                                            else if (k==3) label="BR";
                                            else if (k==4) label="BL";
                                            
                                            // Add corners (1-4) to polygon
                                            if (k > 0) polyPoints.Add(new PointF(cx, cy));
                                        }
                                        else if (calPoints.Length == 4) // Wiimote bar
                                        {
                                             polyPoints.Add(new PointF(cx, cy));
                                        }

                                        g.DrawString(label, SystemFonts.DefaultFont, Brushes.Magenta, cx + 6, cy);
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
                            float cx = rectX + (center.X / 1023f) * drawW;
                            float cy = rectY + (center.Y / 767f) * drawH;
                            
                            // Triangulation lines
                            for (int p = 0; p < 4; p++)
                            {
                                var point = ir[p];
                                if (point.Found)
                                {
                                    float px = rectX + (point.RawPosition.X / 1023f) * drawW;
                                    float py = rectY + (point.RawPosition.Y / 767f) * drawH;
                                    g.DrawLine(Pens.DarkCyan, px, py, cx, cy);
                                }
                            }

                            int crossSize = 10;
                            g.DrawLine(Pens.Cyan, cx - crossSize, cy, cx + crossSize, cy);
                            g.DrawLine(Pens.Cyan, cx, cy - crossSize, cx, cy + crossSize);
                            g.DrawString("CENTER", SystemFonts.DefaultFont, Brushes.Cyan, cx + 5, cy + 5);
                            
                            // DEBUG Info
                            int textY = (int)rectY + 70;
                            int lineHeight = 15;
                            
                            int visibleCount = 0;
                            for (int p = 0; p < 4; p++) if (ir[p].Found) visibleCount++;
                            
                            g.DrawString($"Visible Points: {visibleCount}/4", SystemFonts.DefaultFont, Brushes.LightGreen, rectX + 10, textY);
                            textY += lineHeight;
                            
                            g.DrawString($"Center (norm): ({center.X/1023f:F3}, {center.Y/767f:F3})", SystemFonts.DefaultFont, Brushes.Cyan, rectX + 10, textY);
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
                default:
                    return layout.ToString();
            }
        }
    }
}
