using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WiimoteLib;

namespace WiimoteGun
{
    public partial class ProfileOverlay
    {
        private Panel panelIRViz;
        private PictureBox pbIRCanvas;
        private ComboBox cbIRPlayerSelect;
        private NumericUpDown nudIROffsetX, nudIROffsetY;
        private System.Windows.Forms.Timer _irRefreshTimer;
        private int _selectedIRPlayer = 1;
        
        private void InitializeIRPanel()
        {
            int topOffset = _windowedMode ? 32 : 0;
            panelIRViz = new Panel
            {
                Name = "panelIRViz",
                Size = new Size(560, 670), // Adjusted for button (EN/FR: Ajusté pour bouton)
                Location = new Point(20, 30 + topOffset),
                BackColor = Color.Transparent,
                Visible = false
            };
            
            Label lblTitle = new Label
            {
                Text = "🎯 IR Visualizer & Calibration",
                Location = new Point(10, 10),
                Size = new Size(540, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelIRViz.Controls.Add(lblTitle);
            
            // Player Selection (EN/FR: Sélection Joueur)
            Label lblPlayer = new Label
            {
                Text = "Select Player:",
                Location = new Point(20, 60),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };
            panelIRViz.Controls.Add(lblPlayer);
            
            cbIRPlayerSelect = new ComboBox
            {
                Location = new Point(130, 60),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            cbIRPlayerSelect.Items.AddRange(new object[] { "Player 1", "Player 2", "Player 3", "Player 4" });
            cbIRPlayerSelect.SelectedIndex = 0;
            cbIRPlayerSelect.SelectedIndexChanged += (s, e) => 
            {
                _selectedIRPlayer = cbIRPlayerSelect.SelectedIndex + 1;
                LoadCalibrationValues();
            };
            panelIRViz.Controls.Add(cbIRPlayerSelect);
            
            // IR Canvas (EN/FR: Zone de dessin IR)
            pbIRCanvas = new PictureBox
            {
                Location = new Point(20, 100),
                Size = new Size(520, 390), // 4:3 aspect ratio approx
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            pbIRCanvas.Paint += PbIRCanvas_Paint;
            panelIRViz.Controls.Add(pbIRCanvas);
            
            // Calibration Controls (EN/FR: Contrôles Calibration)
            GroupBox gbCalib = new GroupBox
            {
                Text = "Manual Calibration (Offset)",
                Location = new Point(20, 500),
                Size = new Size(520, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            
            Label lblX = new Label { Text = "Offset X:", Location = new Point(20, 30), Size = new Size(60, 20), ForeColor = Color.White };
            nudIROffsetX = new NumericUpDown { Location = new Point(90, 30), Size = new Size(80, 25), Minimum = -500, Maximum = 500, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            
            Label lblY = new Label { Text = "Offset Y:", Location = new Point(200, 30), Size = new Size(60, 20), ForeColor = Color.White };
            nudIROffsetY = new NumericUpDown { Location = new Point(270, 30), Size = new Size(80, 25), Minimum = -500, Maximum = 500, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            
            Button btnSaveCalib = new Button
            {
                Text = "💾 Save Offset",
                Location = new Point(380, 25),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSaveCalib.FlatAppearance.BorderSize = 0;
            btnSaveCalib.Click += (s, e) => SaveCalibration();
            
            gbCalib.Controls.Add(lblX);
            gbCalib.Controls.Add(nudIROffsetX);
            gbCalib.Controls.Add(lblY);
            gbCalib.Controls.Add(nudIROffsetY);
            gbCalib.Controls.Add(btnSaveCalib);
            
            Label lblHelp = new Label
            {
                Text = "Adjust offsets if the crosshair is not aligned with your aim.",
                Location = new Point(20, 70),
                Size = new Size(480, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            gbCalib.Controls.Add(lblHelp);
            
            // Info label for Wiimote controls (EN/FR: Label info pour contrôles Wiimote)
            Label lblWiimoteHelp = new Label
            {
                Text = "💡 Hold HOME + D-Pad to adjust offset in real-time",
                Location = new Point(20, 610),
                Size = new Size(520, 20),
                ForeColor = Color.LightBlue,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            panelIRViz.Controls.Add(lblWiimoteHelp);
            
            panelIRViz.Controls.Add(gbCalib);
            
            // Button to open 3D Gyro Visualizer (EN/FR: Bouton pour ouvrir visualiseur 3D gyro)
            Button btnOpenGyroViz = new Button
            {
                Text = "🎯 Open 3D Gyro Visualizer",
                Location = new Point(20, 610),
                Size = new Size(520, 45),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnOpenGyroViz.FlatAppearance.BorderSize = 0;
            btnOpenGyroViz.Click += (s, e) =>
            {
                GyroVisualizerForm gyroForm = new GyroVisualizerForm();
                gyroForm.ShowDialog(this);
            };
            btnOpenGyroViz.Visible = Options.Instance.EnableDevGestures;
            panelIRViz.Controls.Add(btnOpenGyroViz);
            
            this.Controls.Add(panelIRViz);
            
            // Timer for animation and Wiimote input (EN/FR: Timer pour animation et saisie Wiimote)
            _irRefreshTimer = new System.Windows.Forms.Timer();
            _irRefreshTimer.Interval = 33; // ~30 FPS
            _irRefreshTimer.Tick += (s, e) => 
            {
                if (panelIRViz.Visible)
                {
                    pbIRCanvas.Invalidate();
                    
                    // Check for Wiimote Home + D-pad input for offset adjustment (EN/FR: Vérifier entrées Wiimote Home + D-pad pour ajustement)
                    if (Program.WiiMoteManager != null)
                    {
                        var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedIRPlayer);
                        if (controller != null && controller.Wiimote != null && controller.Wiimote.WiimoteState != null)
                        {
                            var buttons = controller.Wiimote.WiimoteState.Buttons;
                            
                            // Home button must be held (EN/FR: Bouton Home doit être maintenu)
                            if (buttons.Home)
                            {
                                bool offsetChanged = false;
                                
                                // Adjust X offset with Left/Right (EN/FR: Ajuster offset X avec gauche/droite)
                                if (buttons.Left)
                                {
                                    nudIROffsetX.Value = Math.Max(nudIROffsetX.Minimum, nudIROffsetX.Value - 1);
                                    offsetChanged = true;
                                }
                                else if (buttons.Right)
                                {
                                    nudIROffsetX.Value = Math.Min(nudIROffsetX.Maximum, nudIROffsetX.Value + 1);
                                    offsetChanged = true;
                                }
                                
                                // Adjust Y offset with Up/Down (EN/FR: Ajuster offset Y avec haut/bas)
                                if (buttons.Up)
                                {
                                    nudIROffsetY.Value = Math.Max(nudIROffsetY.Minimum, nudIROffsetY.Value - 1);
                                    offsetChanged = true;
                                }
                                else if (buttons.Down)
                                {
                                    nudIROffsetY.Value = Math.Min(nudIROffsetY.Maximum, nudIROffsetY.Value + 1);
                                    offsetChanged = true;
                                }
                                
                                // Apply offset changes in real-time for visual feedback (EN/FR: Appliquer changements en temps réel pour retour visuel)
                                if (offsetChanged)
                                {
                                    Options.Instance.SetDynamicPerspectiveOffsetX(_selectedIRPlayer, (int)nudIROffsetX.Value);
                                    Options.Instance.SetDynamicPerspectiveOffsetY(_selectedIRPlayer, (int)nudIROffsetY.Value);
                                }
                            }
                        }
                    }
                }
            };
        }
        
        private void LoadIRPage()
        {
            LoadCalibrationValues();
            _irRefreshTimer.Start();
        }
        
        private void LoadCalibrationValues()
        {
            // Load DynamicPerspectiveOffset values for the selected player (EN/FR: Charger valeurs DynamicPerspectiveOffset pour joueur sélectionné)
            int offsetX = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedIRPlayer);
            int offsetY = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedIRPlayer);
            
            nudIROffsetX.Value = offsetX;
            nudIROffsetY.Value = offsetY;
        }
        
        private void SaveCalibration()
        {
            // Save DynamicPerspectiveOffset values (EN/FR: Sauvegarder valeurs DynamicPerspectiveOffset)
            Options.Instance.SetDynamicPerspectiveOffsetX(_selectedIRPlayer, (int)nudIROffsetX.Value);
            Options.Instance.SetDynamicPerspectiveOffsetY(_selectedIRPlayer, (int)nudIROffsetY.Value);
                
            Options.Instance.Save();
            _toastNotification.Show($"✓ P{_selectedIRPlayer} Offset Saved", 2000);
        }
        
        private void PbIRCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw sensor bar reference (EN/FR: Dessiner référence sensor bar)
            int midX = pbIRCanvas.Width / 2;
            int midY = pbIRCanvas.Height / 2;
            
            g.DrawLine(Pens.DarkGray, midX, 0, midX, pbIRCanvas.Height);
            g.DrawLine(Pens.DarkGray, 0, midY, pbIRCanvas.Width, midY);
            
            if (Program.WiiMoteManager == null) return;
            
            var controller = Program.WiiMoteManager.Controllers.FirstOrDefault(c => c.PlayerIndex == _selectedIRPlayer);
            
            if (controller != null)
            {
                // Draw IR dots (EN/FR: Dessiner points IR)
                var irState = controller.Wiimote.WiimoteState.IRState;
                
                // IR coordinates are 0-1023 for X and Y
                float scaleX = (float)pbIRCanvas.Width / 1024f;
                float scaleY = (float)pbIRCanvas.Height / 768f; // IR sensor is 1024x768
                
                for (int i = 0; i < 4; i++)
                {
                    if (irState[i].Found)
                    {
                        float x = irState[i].RawPosition.X * scaleX;
                        float y = irState[i].RawPosition.Y * scaleY;
                        
                        // Invert X because sensor sees mirrored image (EN/FR: Inverser X car capteur voit image miroir)
                        x = pbIRCanvas.Width - x;
                        
                        int size = irState[i].Size + 5; // Size depends on distance/intensity
                        
                        g.FillEllipse(Brushes.White, x - size/2, y - size/2, size, size);
                        g.DrawString((i+1).ToString(), this.Font, Brushes.Yellow, x + 5, y + 5);
                    }
                }
                
                // Draw computed aim point if available (EN/FR: Dessiner point de visée calculé si disponible)
                // Note: This requires accessing the computed coordinates from the controller
                // For now, just showing raw IR dots is useful enough for calibration
            }
            else
            {
                string msg = $"Player {_selectedIRPlayer} not connected";
                SizeF size = g.MeasureString(msg, this.Font);
                g.DrawString(msg, this.Font, Brushes.Red, midX - size.Width/2, midY - size.Height/2);
            }
        }
    }
}
