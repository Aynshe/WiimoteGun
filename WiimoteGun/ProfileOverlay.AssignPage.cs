using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WiimoteLib;

namespace WiimoteGun
{
    public partial class ProfileOverlay
    {
        private Panel panelAssign;
        private Panel panelAssignContent;
        private System.Windows.Forms.Timer _assignRefreshTimer;
        
        private void InitializeAssignPanel()
        {
            int topOffset = _windowedMode ? 32 : 0;
            panelAssign = new Panel
            {
                Name = "panelAssign",
                Size = new Size(560, 780 - topOffset),
                Location = new Point(20, 30 + topOffset),
                BackColor = Color.Transparent,
                Visible = false
            };
            
            Label lblTitle = new Label
            {
                Text = "📡 Assign Wiimotes",
                Location = new Point(10, 10),
                Size = new Size(540, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelAssign.Controls.Add(lblTitle);
            
            panelAssignContent = new Panel
            {
                Size = new Size(540, 690 - topOffset), // Adjusted to fit 4 players + Back button (EN/FR: Ajusté pour 4 joueurs + bouton Back)
                Location = new Point(10, 70),
                BackColor = Color.Transparent,
                AutoScroll = false // No scrolling needed (EN/FR: Pas de défilement nécessaire)
            };
            panelAssign.Controls.Add(panelAssignContent);
            
            /* Refresh button removed - auto-refresh handles updates (EN/FR: Bouton refresh supprimé - auto-refresh gère les mises à jour)
            Button btnRefresh = new Button
            {
                Text = "↻ Refresh List",
                Size = new Size(150, 40),
                Location = new Point(205, 770),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshAssignList();
            panelAssign.Controls.Add(btnRefresh);
            */
            
            this.Controls.Add(panelAssign);
            
            // Timer to auto-refresh battery levels (EN/FR: Timer pour rafraîchir niveaux batterie)
            _assignRefreshTimer = new System.Windows.Forms.Timer();
            _assignRefreshTimer.Interval = 5000; // 5 seconds - increased to reduce flickering
            _assignRefreshTimer.Tick += (s, e) => 
            {
                if (panelAssign.Visible) UpdateBatteryLevels();
            };
        }
        
        private void LoadAssignPage()
        {
            RefreshAssignList();
            _assignRefreshTimer.Start();
        }
        
        private void RefreshAssignList()
        {
            panelAssignContent.Controls.Clear();
            
            int yPos = 10;
            
            // Check if manager exists (EN/FR: Vérifier si manager existe)
            if (Program.WiiMoteManager == null)
            {
                Label lblError = new Label
                {
                    Text = "Wiimote Manager not initialized",
                    Location = new Point(10, yPos),
                    Size = new Size(500, 30),
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 10F)
                };
                panelAssignContent.Controls.Add(lblError);
                return;
            }
            
            var controllers = Program.WiiMoteManager.Controllers.ToList();
            
            // List 4 slots (EN/FR: Lister 4 slots)
            for (int i = 1; i <= 4; i++)
            {
                // Find controller for this player index (EN/FR: Trouver contrôleur pour cet index joueur)
                var controller = controllers.FirstOrDefault(c => c.PlayerIndex == i);
                
                Panel slotPanel = CreateWiimoteSlotPanel(i, controller, yPos);
                panelAssignContent.Controls.Add(slotPanel);
                yPos += 160; // Reduced to fit 4 players in windowed mode (EN/FR: Réduit pour rentrer 4 joueurs en mode fenêtré)
            }
        }
        
        /// <summary>
        /// Update only battery levels without recreating controls to avoid flickering
        /// FR: Mettre à jour seulement les niveaux de batterie sans recréer les contrôles pour éviter le clignotement
        /// </summary>
        private void UpdateBatteryLevels()
        {
            if (Program.WiiMoteManager == null || !panelAssignContent.Visible)
                return;
            
            var controllers = Program.WiiMoteManager.Controllers.ToList();
            
            // Update each player panel (EN/FR: Mettre à jour chaque panel joueur)
            foreach (Control control in panelAssignContent.Controls)
            {
                if (control is Panel playerPanel && playerPanel.Tag != null)
                {
                    int playerIndex = (int)playerPanel.Tag;
                    var controller = controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
                    
                    // Find and update battery label (EN/FR: Trouver et mettre à jour label batterie)
                    foreach (Control childControl in playerPanel.Controls)
                    {
                        if (childControl is Label lbl && lbl.Tag != null && lbl.Tag.ToString() == "Battery")
                        {
                            if (controller != null)
                            {
                                float battery = controller.Wiimote.WiimoteState.Status.Battery;
                                lbl.Text = $"🔋 {battery:F0}%";
                                lbl.ForeColor = battery < 20 ? Color.Red : Color.White;
                            }
                        }
                    }
                }
            }
        }
        
        private Panel CreateWiimoteSlotPanel(int playerIndex, WiiMoteController controller, int y)
        {
            Panel p = new Panel
            {
                Size = new Size(520, 160), // Increased height for rumble controls
                Location = new Point(10, y),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = playerIndex // Tag for UpdateBatteryLevels to identify player
            };
            
            // Make panel clickable to open device selection dialog (EN/FR: Rendre le panel cliquable pour ouvrir dialogue sélection)
            p.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
            
            // Player Label (EN/FR: Label Joueur)
            Label lblPlayer = new Label
            {
                Text = $"Player {playerIndex}",
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            lblPlayer.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
            p.Controls.Add(lblPlayer);
            
            if (controller != null)
            {
                // Connected state (EN/FR: État connecté)
                Label lblStatus = new Label
                {
                    Text = "✓ Connected",
                    Location = new Point(120, 15),
                    Size = new Size(150, 25),
                    ForeColor = Color.LightGreen,
                    Font = new Font("Segoe UI", 10F)
                };
                lblStatus.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(lblStatus);
                
                Label lblMac = new Label
                {
                    Text = $"MAC: {controller.Wiimote.Address}",
                    Location = new Point(120, 40),
                    Size = new Size(200, 20),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 9F)
                };
                lblMac.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(lblMac);
                
                // Battery (EN/FR: Batterie)
                float battery = controller.Wiimote.WiimoteState.Status.Battery;
                Label lblBattery = new Label
                {
                    Text = $"🔋 {battery:F0}%",
                    Location = new Point(120, 60),
                    Size = new Size(100, 20),
                    ForeColor = battery < 20 ? Color.Red : Color.White,
                    Font = new Font("Segoe UI", 9F),
                    Tag = "Battery" // Tag for UpdateBatteryLevels to identify battery label
                };
                lblBattery.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(lblBattery);
                
                // Identify Button (EN/FR: Bouton Identifier)
                Button btnIdentify = new Button
                {
                    Text = "📳 Identify",
                    Size = new Size(100, 35),
                    Location = new Point(400, 10),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F),
                    Cursor = Cursors.Hand
                };
                btnIdentify.FlatAppearance.BorderSize = 0;
                btnIdentify.Click += (s, e) => 
                {
                    try 
                    {
                        controller.Wiimote.SetRumble(true);
                        System.Threading.Thread.Sleep(300);
                        controller.Wiimote.SetRumble(false);
                    } catch {}
                };
                p.Controls.Add(btnIdentify);
                
                // Device Config Button (EN/FR: Bouton Config Périphériques)
                Button btnDevices = new Button
                {
                    Text = "⚙️ Devices",
                    Size = new Size(100, 35),
                    Location = new Point(400, 50),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F),
                    Cursor = Cursors.Hand
                };
                btnDevices.FlatAppearance.BorderSize = 0;
                btnDevices.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(btnDevices);
            }
            else
            {
                // Disconnected state (EN/FR: État déconnecté)
                Label lblStatus = new Label
                {
                    Text = "Waiting for connection...",
                    Location = new Point(120, 35),
                    Size = new Size(250, 25),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic)
                };
                lblStatus.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(lblStatus);
                
                // Device Config Button (EN/FR: Bouton Config Périphériques)
                Button btnDevices = new Button
                {
                    Text = "⚙️ Devices",
                    Size = new Size(100, 35),
                    Location = new Point(400, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F),
                    Cursor = Cursors.Hand
                };
                btnDevices.FlatAppearance.BorderSize = 0;
                btnDevices.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                p.Controls.Add(btnDevices);
            }
            
            // Rumble Settings Section (EN/FR: Section Paramètres Vibration)
            int rumbleY = 90;
            
            Label lblRumble = new Label
            {
                Text = "Rumble Settings:",
                Location = new Point(10, rumbleY),
                Size = new Size(120, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            p.Controls.Add(lblRumble);
            
            // Enable Rumble CheckBox (EN/FR: Case à cocher Activer Vibration)
            CheckBox chkEnableRumble = new CheckBox
            {
                Text = "Enable",
                Location = new Point(130, rumbleY),
                Size = new Size(70, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Checked = Options.Instance.GetEnableWeaponRumble(playerIndex)
            };
            chkEnableRumble.CheckedChanged += (s, e) =>
            {
                Options.Instance.SetEnableWeaponRumble(playerIndex, chkEnableRumble.Checked);
                Options.Instance.Save();
            };
            p.Controls.Add(chkEnableRumble);
            
            rumbleY += 25;
            
            // Intensity Label and TrackBar (EN/FR: Label et TrackBar Intensité)
            Label lblIntensity = new Label
            {
                Text = "Intensity:",
                Location = new Point(10, rumbleY),
                Size = new Size(60, 20),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            p.Controls.Add(lblIntensity);
            
            // Declare label value first (EN/FR: Déclarer le label de valeur d'abord)
            Label lblIntensityVal = new Label
            {
                Text = "",
                Location = new Point(255, rumbleY),
                Size = new Size(50, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F)
            };
            p.Controls.Add(lblIntensityVal);
            
            TrackBar trkIntensity = new TrackBar
            {
                Location = new Point(70, rumbleY - 5),
                Size = new Size(180, 30),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = Options.Instance.GetRumbleIntensity(playerIndex)
            };
            lblIntensityVal.Text = $"{trkIntensity.Value}%"; // Set initial value
            trkIntensity.ValueChanged += (s, e) =>
            {
                Options.Instance.SetRumbleIntensity(playerIndex, trkIntensity.Value);
                lblIntensityVal.Text = $"{trkIntensity.Value}%";
            };
            trkIntensity.MouseUp += (s, e) => Options.Instance.Save();
            p.Controls.Add(trkIntensity);
            
            // Duration Label and NumericUpDown (EN/FR: Label et NumericUpDown Durée)
            Label lblDuration = new Label
            {
                Text = "Duration:",
                Location = new Point(310, rumbleY),
                Size = new Size(60, 20),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            p.Controls.Add(lblDuration);
            
            NumericUpDown nudDuration = new NumericUpDown
            {
                Location = new Point(375, rumbleY - 3),
                Size = new Size(60, 20),
                Minimum = 50,
                Maximum = 1000,
                Increment = 50,
                Value = Options.Instance.GetRumbleDurationMs(playerIndex),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            nudDuration.ValueChanged += (s, e) =>
            {
                Options.Instance.SetRumbleDurationMs(playerIndex, (int)nudDuration.Value);
                Options.Instance.Save();
            };
            p.Controls.Add(nudDuration);
            
            Label lblMs = new Label
            {
                Text = "ms",
                Location = new Point(440, rumbleY),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            p.Controls.Add(lblMs);
            
            return p;
        }
        
        private void OpenDeviceSelectionDialog(int playerIndex)
        {
            try
            {
                using (PlayerDeviceDialog dialog = new PlayerDeviceDialog(playerIndex))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        SimpleLogger.Instance.Info($"Device configuration updated for Player {playerIndex}");
                        // Optionally refresh the assign list
                        RefreshAssignList();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to open device selection dialog: {ex.Message}");
            }
        }
    }
}
