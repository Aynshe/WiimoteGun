using System;
using System.Drawing;
using System.Windows.Forms;
using WiimoteLib;
using WiimoteLib.DataTypes;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun
{
    public class IRVisualizerForm : Form
    {
        private Timer _updateTimer;
        private Dictionary<string, WiimoteState> _lastStates = new Dictionary<string, WiimoteState>();
        private CheckBox chkShowCalibration;
        private ComboBox cmbPlayer;
        private Button btnOffsetLeft;
        private Button btnOffsetRight;
        private Button btnOffsetUp;
        private Button btnOffsetDown;
        private Label lblOffsetValue;
        private int _selectedPlayerIndex = 1;

        public IRVisualizerForm()
        {
            this.Text = "Wiimote IR Visualizer";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            // Position on correct screen (EN/FR: Positionner sur le bon écran)
            int screenIndex = Options.Instance.MonitorId;
            Screen screen = Screen.PrimaryScreen;
            if (screenIndex >= 0 && screenIndex < Screen.AllScreens.Length)
            {
                screen = Screen.AllScreens[screenIndex];
            }

            this.StartPosition = FormStartPosition.Manual;
            var bounds = screen.Bounds;
            this.Location = new Point(
                bounds.Left + (bounds.Width - this.Width) / 2,
                bounds.Top + (bounds.Height - this.Height) / 2
            );

            this.FormClosing += IRVisualizerForm_FormClosing;
            this.Paint += IRVisualizerForm_Paint;
            this.KeyDown += IRVisualizerForm_KeyDown;
            this.KeyPreview = true; // Enable form to receive key events

            _updateTimer = new Timer();
            _updateTimer.Interval = 33; // ~30 FPS
            _updateTimer.Tick += _updateTimer_Tick;
            _updateTimer.Start();

            // Add CheckBox for Calibration Overlay (EN/FR: Ajouter CheckBox pour overlay calibration)
            chkShowCalibration = new CheckBox();
            chkShowCalibration.Text = "Show Calibration";
            chkShowCalibration.ForeColor = Color.White;
            chkShowCalibration.BackColor = Color.Transparent;
            chkShowCalibration.AutoSize = true;
            chkShowCalibration.Location = new Point(10, this.ClientSize.Height - 30);
            chkShowCalibration.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(chkShowCalibration);

            // Add Per-Player X/Y Offset Controls (EN/FR: Ajouter contrôles d'offset X/Y par joueur)
            int offsetControlsX = 180;
            int offsetControlsY = this.ClientSize.Height - 90;
            
            // Player selector ComboBox
            cmbPlayer = new ComboBox();
            cmbPlayer.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPlayer.Items.AddRange(new object[] { "Player 1", "Player 2", "Player 3", "Player 4" });
            cmbPlayer.SelectedIndex = 0;
            cmbPlayer.Location = new Point(offsetControlsX + 160, offsetControlsY + 20);
            cmbPlayer.Size = new Size(100, 25);
            cmbPlayer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cmbPlayer.SelectedIndexChanged += CmbPlayer_SelectedIndexChanged;
            
            // Prevent ComboBox from handling D-pad when Home is held for offset adjustment (EN/FR: Empêcher ComboBox de gérer D-pad quand Home maintenu)
            cmbPlayer.KeyDown += (s, ke) =>
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
            };
            
            this.Controls.Add(cmbPlayer);
            
            // Button LEFT (←) - decrease offsetX by -5
            btnOffsetLeft = new Button();
            btnOffsetLeft.Text = "←";
            btnOffsetLeft.Size = new Size(35, 30);
            btnOffsetLeft.Location = new Point(offsetControlsX, offsetControlsY + 20);
            btnOffsetLeft.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOffsetLeft.Click += BtnOffsetLeft_Click;
            this.Controls.Add(btnOffsetLeft);
            
            // Button RIGHT (→) - increase offsetX by +5
            btnOffsetRight = new Button();
            btnOffsetRight.Text = "→";
            btnOffsetRight.Size = new Size(35, 30);
            btnOffsetRight.Location = new Point(offsetControlsX + 40, offsetControlsY + 20);
            btnOffsetRight.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOffsetRight.Click += BtnOffsetRight_Click;
            this.Controls.Add(btnOffsetRight);
            
            // Button UP (▲) - decrease offsetY by -5 (up in screen = lower Y coord)
            btnOffsetUp = new Button();
            btnOffsetUp.Text = "▲";
            btnOffsetUp.Size = new Size(35, 30);
            btnOffsetUp.Location = new Point(offsetControlsX + 80, offsetControlsY + 20);
            btnOffsetUp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOffsetUp.Click += BtnOffsetUp_Click;
            this.Controls.Add(btnOffsetUp);
            
            // Button DOWN (▼) - increase offsetY by +5 (down in screen = higher Y coord)
            btnOffsetDown = new Button();
            btnOffsetDown.Text = "▼";
            btnOffsetDown.Size = new Size(35, 30);
            btnOffsetDown.Location = new Point(offsetControlsX + 120, offsetControlsY + 20);
            btnOffsetDown.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOffsetDown.Click += BtnOffsetDown_Click;
            this.Controls.Add(btnOffsetDown);
            
            // Label showing current X/Y offset values for selected player
            lblOffsetValue = new Label();
            lblOffsetValue.Text = "X: 0, Y: 0";
            lblOffsetValue.ForeColor = Color.Orange;
            lblOffsetValue.BackColor = Color.Transparent;
            lblOffsetValue.AutoSize = true;
            lblOffsetValue.Location = new Point(offsetControlsX + 270, offsetControlsY + 25);
            lblOffsetValue.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(lblOffsetValue);
            
            // Save Offset button (EN/FR: Bouton Sauvegarder Offset)
            Button btnSaveOffset = new Button();
            btnSaveOffset.Text = "💾 Save Offset";
            btnSaveOffset.Size = new Size(100, 30);
            btnSaveOffset.Location = new Point(offsetControlsX + 400, offsetControlsY + 20);
            btnSaveOffset.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveOffset.BackColor = Color.FromArgb(0, 122, 204);
            btnSaveOffset.ForeColor = Color.White;
            btnSaveOffset.FlatStyle = FlatStyle.Flat;
            btnSaveOffset.FlatAppearance.BorderSize = 0;
            btnSaveOffset.Click += (s, e) => 
            {
                Options.Instance.Save();
                MessageBox.Show($"Offset saved for Player {_selectedPlayerIndex}!\n\nX: {Options.Instance.GetDynamicPerspectiveOffsetX(_selectedPlayerIndex)}\nY: {Options.Instance.GetDynamicPerspectiveOffsetY(_selectedPlayerIndex)}", 
                    "Offset Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            this.Controls.Add(btnSaveOffset);
            
            // Info label for Wiimote controls (EN/FR: Label info pour contrôles Wiimote)
            Label lblWiimoteInfo = new Label();
            lblWiimoteInfo.Text = "💡 Hold HOME + D-Pad to adjust offset in real-time";
            lblWiimoteInfo.ForeColor = Color.LightGray;
            lblWiimoteInfo.BackColor = Color.Transparent;
            lblWiimoteInfo.AutoSize = true;
            lblWiimoteInfo.Location = new Point(offsetControlsX, offsetControlsY + 55);
            lblWiimoteInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblWiimoteInfo.Font = new Font(this.Font.FontFamily, 8F, FontStyle.Italic);
            this.Controls.Add(lblWiimoteInfo);
            
            UpdateOffsetLabel();
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
            // (EN/FR: Trier les wiimotes par PlayerIndex pour afficher dans l'ordre P1, P2, P3, P4)
            var sortedWiimotes = wiimotes
                .Select(wm => new { 
                    Wiimote = wm, 
                    PlayerIndex = Program.WiiMoteManager?.GetController(wm.ID)?.PlayerIndex ?? 999 
                })
                .OrderBy(x => x.PlayerIndex)
                .Select(x => x.Wiimote)
                .ToArray();

            int count = sortedWiimotes.Length;
            int widthPerRemote = this.ClientSize.Width / count;
            int height = this.ClientSize.Height;

            for (int i = 0; i < count; i++)
            {
                var wm = sortedWiimotes[i];
                int offsetX = i * widthPerRemote;
                
                // Get player index from controller (EN/FR: Récupérer index joueur depuis contrôleur)
                var controller = Program.WiiMoteManager?.GetController(wm.ID);
                int playerNumber = controller != null ? controller.PlayerIndex : (i + 1);
                
                // Safety check: Skip if wiimote or its properties are null (EN/FR: Vérification sécurité : ignorer si wiimote null)
                if (wm == null || string.IsNullOrEmpty(wm.DevicePath))
                {
                    g.DrawString($"Player {playerNumber}", SystemFonts.DefaultFont, Brushes.White, offsetX + 10, 10);
                    g.DrawString("Disconnected", SystemFonts.DefaultFont, Brushes.Red, offsetX + 10, 30);
                    continue;
                }
                
                
                // Draw separator
                if (i > 0)
                    g.DrawLine(Pens.Gray, offsetX, 0, offsetX, height);

                g.DrawString($"Player {playerNumber}", SystemFonts.DefaultFont, Brushes.White, offsetX + 10, 10);
                
                // Display LED Layout mode (EN/FR: Afficher mode LED Layout)
                string layoutName = GetLayoutName(Options.Instance.LEDLayout);
                g.DrawString($"LED Layout: {layoutName}", SystemFonts.DefaultFont, Brushes.Cyan, offsetX + 10, 30);

                if (_lastStates.ContainsKey(wm.DevicePath))
                {
                    var state = _lastStates[wm.DevicePath];
                    var ir = state.IRState;

                    g.DrawString($"Mode: {ir.Mode}, Sens: {ir.Sensitivity}", SystemFonts.DefaultFont, Brushes.White, offsetX + 10, 50);

                    // Draw IR dots
                    // IR coordinates are 0-1023 for X, 0-767 for Y
                    // Map to current view area
                    
                    for (int p = 0; p < 4; p++)
                    {
                        var point = ir[p];
                        if (point.Found)
                        {
                            float x = offsetX + (point.RawPosition.X / 1023f) * widthPerRemote;
                            float y = (point.RawPosition.Y / 767f) * height;
                            
                            int size = point.Size + 5;
                            g.FillEllipse(Brushes.Red, x - size/2, y - size/2, size, size);
                            g.DrawString($"P{p}", SystemFonts.DefaultFont, Brushes.Yellow, x, y);
                        }
                    }

                    // Draw Calibration Overlay if checked (EN/FR: Dessiner overlay calibration si coché)
                    if (chkShowCalibration.Checked)
                    {
                        var calController = Program.WiiMoteManager?.GetController(wm.ID);
                        if (calController != null && calController.Calculator != null)
                        {
                            var calPoints = calController.Calculator.GetCalibrationPoints();
                            if (calPoints != null)
                            {
                                // Draw Points
                                List<PointF> polyPoints = new List<PointF>();
                                
                                for (int k = 0; k < calPoints.Length; k++)
                                {
                                    if (calPoints[k].HasValue)
                                    {
                                        // Calibration points are normalized 0-1
                                        // Need to invert X because IR camera is mirrored? 
                                        // Wait, _gun4irPoints stores RAW IR coords which are 0-1 normalized.
                                        // In ScreenPositionCalculator: _lastRawPoint.X = 1.0f - _lastRawPoint.X;
                                        // So they are already inverted relative to camera raw?
                                        // Let's assume they are 0-1 normalized relative to the SCREEN (Left-Right).
                                        // But here we visualize the CAMERA view.
                                        // Camera View: 0,0 is Top-Left of SENSOR.
                                        // If stored points are "Screen Relative", we might need to flip X to match Camera View.
                                        // Let's try direct mapping first.
                                        
                                        float cx = offsetX + (calPoints[k].Value.X) * widthPerRemote;
                                        float cy = (calPoints[k].Value.Y) * height;
                                        
                                        // Draw Hollow Magenta Circle
                                        g.DrawEllipse(Pens.Magenta, cx - 5, cy - 5, 10, 10);
                                        
                                        // Label
                                        string label = $"C{k}";
                                        if (calPoints.Length == 5) // Gun4IR/4Corners
                                        {
                                            if (k==0) label="Center";
                                            else if (k==1) label="TL";
                                            else if (k==2) label="TR";
                                            else if (k==3) label="BR";
                                            else if (k==4) label="BL";
                                            
                                            if (k > 0) polyPoints.Add(new PointF(cx, cy));
                                        }
                                        g.DrawString(label, SystemFonts.DefaultFont, Brushes.Magenta, cx + 6, cy);
                                    }
                                }

                                // Draw Polygon connecting corners (TL-TR-BR-BL-TL)
                                if (polyPoints.Count == 4)
                                {
                                    g.DrawPolygon(Pens.Magenta, polyPoints.ToArray());
                                }
                            }
                        }
                    }

                    // Draw Virtual Center (Blue Cross) if available
                    // (EN/FR: Dessiner le centre virtuel (Croix bleue) si disponible)
                    var centerController = Program.WiiMoteManager?.GetController(wm.ID);
                    if (centerController != null && centerController.Calculator != null)
                    {
                        var center = centerController.Calculator.LastCalculatedCenter;
                        if (center.X != 0 && center.Y != 0) // Check if valid
                        {
                            float cx = offsetX + (center.X / 1023f) * widthPerRemote;
                            float cy = (center.Y / 767f) * height;
                            
                            // Draw triangulation lines from each visible point to center
                            // (EN/FR: Dessiner les lignes de triangulation de chaque point visible vers le centre)
                            for (int p = 0; p < 4; p++)
                            {
                                var point = ir[p];
                                if (point.Found)
                                {
                                    float px = offsetX + (point.RawPosition.X / 1023f) * widthPerRemote;
                                    float py = (point.RawPosition.Y / 767f) * height;
                                    g.DrawLine(Pens.DarkCyan, px, py, cx, cy);
                                }
                            }

                            int crossSize = 10;
                            g.DrawLine(Pens.Cyan, cx - crossSize, cy, cx + crossSize, cy);
                            g.DrawLine(Pens.Cyan, cx, cy - crossSize, cx, cy + crossSize);
                            g.DrawString("CENTER", SystemFonts.DefaultFont, Brushes.Cyan, cx + 5, cy + 5);
                            
                            // DEBUG: Display numeric coordinates
                            // (EN/FR: DEBUG: Afficher les coordonnées numériques)
                            int textY = 70; // Was 50, moved down to avoid overlap with Mode info
                            int lineHeight = 15;
                            
                            // Count visible points
                            int visibleCount = 0;
                            for (int p = 0; p < 4; p++)
                                if (ir[p].Found) visibleCount++;
                            
                            g.DrawString($"Visible Points: {visibleCount}/4", SystemFonts.DefaultFont, Brushes.LightGreen, offsetX + 10, textY);
                            textY += lineHeight;
                            
                            // Display raw IR positions
                            for (int p = 0; p < 4; p++)
                            {
                                if (ir[p].Found)
                                {
                                    var rawPos = ir[p].RawPosition;
                                    g.DrawString($"P{p}: ({rawPos.X}, {rawPos.Y})", 
                                        SystemFonts.DefaultFont, Brushes.Yellow, offsetX + 10, textY);
                                    textY += lineHeight;
                                }
                            }
                            
                            // Display calculated center (raw pixels)
                            g.DrawString($"Center (raw): ({center.X:F1}, {center.Y:F1})", 
                                SystemFonts.DefaultFont, Brushes.Cyan, offsetX + 10, textY);
                            textY += lineHeight;
                            
                            // Display normalized center
                            float normX = center.X / 1023f;
                            float normY = center.Y / 767f;
                            g.DrawString($"Center (norm): ({normX:F3}, {normY:F3})", 
                                SystemFonts.DefaultFont, Brushes.Cyan, offsetX + 10, textY);
                            textY += lineHeight;
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
