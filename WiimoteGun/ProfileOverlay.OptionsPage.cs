using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace WiimoteGun
{
    public partial class ProfileOverlay
    {
        // Options page controls (EN/FR: Contrôles page Options)
        private Panel panelOptions;
        private Panel panelOptionsSidebar;
        private Panel panelOptionsContent;
        private string _currentOptionsCategory = "General";
        
        // Global controls for option binding (EN/FR: Contrôles globaux pour liaison options)
        private ComboBox optMouseMode, optLEDLayout;
        private NumericUpDown optMonitorId, optIRSensitivity;
        private CheckBox optShowNotifications, optUseSharedKeyboard, optDetectDolphin, optDetectBluetooth;
        private CheckBox optEnableOffScreenReload, optOffScreenReloadAuto, optEnableShakeReload, optShakeFromNunchuk;
        private CheckBox optEnableGrenadeGesture, optRestartOnDolphin, optRestartOnCemu, optEnable4Players, optPermissiveCalibration;
        private TrackBar optShakeSensitivity;
        
        /// <summary>
        /// Initialize Options panel with 2-column layout (EN/FR: Initialiser panel Options avec layout 2 colonnes)
        /// </summary>
        private void InitializeOptionsPanel()
        {
            // Main options panel
            int topOffset = _windowedMode ? 32 : 0;
            panelOptions = new Panel
            {
                Name = "panelOptions",
                Size = new Size(560, 660),
                Location = new Point(20, 30 + topOffset),
                BackColor = Color.Transparent,
                Visible = false
            };
            
            // Sidebar (left column) for categories (EN/FR: Barre latérale (colonne gauche) pour catégories)
            panelOptionsSidebar = new Panel
            {
                Size = new Size(150, 660),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None
            };
            
            // Create category buttons (EN/FR: Créer boutons catégories)
            // Create category buttons (EN/FR: Créer boutons catégories)
            string[] categories = { "General", "Keyboard", "Detection", "Gestures", "Emulators", "Players" };
            string[] icons = { "⚙️", "⌨️", "📡", "🤌", "🎮", "👥" };
            
            int yPos = 10;
            for (int i = 0; i < categories.Length; i++)
            {
                Button btnCategory = CreateCategoryButton(categories[i], icons[i], yPos);
                btnCategory.Click += (s, e) => ShowOptionsCategory(((Button)s).Tag.ToString());
                panelOptionsSidebar.Controls.Add(btnCategory);
                yPos += 50;
            }
            
            // Content panel (right column - scrollable) (EN/FR: Panel contenu (colonne droite - scrollable))
            panelOptionsContent = new Panel
            {
                Size = new Size(400, 600),
                Location = new Point(155, 5),
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            
            // Apply and Reset buttons at bottom (EN/FR: Boutons Apply et Reset en bas)
            Button btnApply = new Button
            {
                Text = "💾 Apply & Restart",
                Size = new Size(180, 40),
                Location = new Point(155, 615),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApplyOptions_Click;
            
            Button btnReset = new Button
            {
                Text = "↺ Reset",
                Size = new Size(100, 40),
                Location = new Point(345, 615),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) => LoadOptionsFromInstance();
            
            // Add all to main panel
            panelOptions.Controls.Add(panelOptionsSidebar);
            panelOptions.Controls.Add(panelOptionsContent);
            panelOptions.Controls.Add(btnApply);
            panelOptions.Controls.Add(btnReset);
            
            this.Controls.Add(panelOptions);
            panelOptions.BringToFront();
            
            // Load default category (EN/FR: Charger catégorie par défaut)
            ShowOptionsCategory("General");
        }
        
        private Button CreateCategoryButton(string name, string icon, int y)
        {
            var btn = new Button
            {
                Text = $"{icon} {name}",
                Tag = name,
                Size = new Size(145, 45),
                Location = new Point(2, y),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            
            // Hover effect
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(60, 60, 60);
            btn.MouseLeave += (s, e) => btn.BackColor = (btn.Tag.ToString() == _currentOptionsCategory) 
                ? Color.FromArgb(0, 122, 204) 
                : Color.FromArgb(40, 40, 40);
            
            return btn;
        }
        
        private void ShowOptionsCategory(string category)
        {
            _currentOptionsCategory = category;
            
            // Update sidebar button colors (EN/FR: Mettre à jour couleurs boutons sidebar)
            foreach (Control ctrl in panelOptionsSidebar.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = (btn.Tag.ToString() == category) 
                        ? Color.FromArgb(0, 122, 204) 
                        : Color.FromArgb(40, 40, 40);
                }
            }
            
            // Clear content panel (EN/FR: Vider panel contenu)
            panelOptionsContent.Controls.Clear();
            
            // Load category content (EN/FR: Charger contenu catégorie)
            switch (category)
            {
                case "General": LoadGeneralOptions(); break;
                // Calibration removed (done via IR Visualizer)
                case "Keyboard": LoadKeyboardOptions(); break;
                case "Detection": LoadDetectionOptions(); break;
                case "Gestures": LoadGesturesOptions(); break;
                case "Emulators": LoadEmulatorsOptions(); break;
                case "Players": LoadPlayersOptions(); break;
            }
        }
        
        private int _optionsYOffset;
        
        private void AddOptionLabel(string text)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(10, _optionsYOffset),
                Size = new Size(180, 25),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelOptionsContent.Controls.Add(lbl);
        }
        
        private ComboBox AddOptionComboBox(string[] items, string selectedValue = null)
        {
            ComboBox combo = new ComboBox
            {
                Location = new Point(200, _optionsYOffset),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            combo.Items.AddRange(items);
            if (selectedValue != null && combo.Items.Contains(selectedValue))
                combo.SelectedItem = selectedValue;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
            
            panelOptionsContent.Controls.Add(combo);
            _optionsYOffset += 35;
            return combo;
        }
        
        private CheckBox AddOptionCheckBox(string text, bool isChecked)
        {
            CheckBox chk = new CheckBox
            {
                Text = text,
                Location = new Point(200, _optionsYOffset),
                Size = new Size(180, 25),
                Checked = isChecked,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9F)
            };
            panelOptionsContent.Controls.Add(chk);
            _optionsYOffset += 35;
            return chk;
        }
        
        private NumericUpDown AddOptionNumeric(decimal value, decimal min, decimal max)
        {
            NumericUpDown numeric = new NumericUpDown
            {
                Location = new Point(200, _optionsYOffset),
                Size = new Size(100, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9F)
            };
            panelOptionsContent.Controls.Add(numeric);
            _optionsYOffset += 35;
            return numeric;
        }
        
        private TrackBar AddOptionTrackBar(int value, int min, int max)
        {
            TrackBar track = new TrackBar
            {
                Location = new Point(200, _optionsYOffset - 5),
                Size = new Size(180, 45),
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.BottomRight
            };
            panelOptionsContent.Controls.Add(track);
            
            Label lblValue = new Label
            {
                Text = value.ToString(),
                Location = new Point(250, _optionsYOffset + 25),
                Size = new Size(30, 20),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.TopCenter
            };
            panelOptionsContent.Controls.Add(lblValue);
            track.ValueChanged += (s, e) => lblValue.Text = track.Value.ToString();
            
            _optionsYOffset += 50;
            return track;
        }
        
        private void AddSectionTitle(string title)
        {
            if (_optionsYOffset > 10) _optionsYOffset += 10; // Spacing
            
            Label lbl = new Label
            {
                Text = title,
                Location = new Point(5, _optionsYOffset),
                Size = new Size(380, 25),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelOptionsContent.Controls.Add(lbl);
            _optionsYOffset += 35;
        }
        
        // Category implementations (EN/FR: Implémentations catégories)
        
        private void LoadGeneralOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("⚙️ General Settings");
            
            AddOptionLabel("Mouse Mode:");
            optMouseMode = AddOptionComboBox(new[] { "SendInput", "RawInput" }, 
                Options.Instance.DefaultMouseMode.ToString());
            
            AddOptionLabel("Monitor ID:");
            optMonitorId = AddOptionNumeric(Options.Instance.MonitorId, 0, 9);
            
            AddOptionLabel("LED Layout Type:");
            optLEDLayout = AddOptionComboBox(new[] { "Wiimote Bar", "Gun4IR Diamond", "Two Wiimote Bars", "Four Corners" },
                GetLEDLayoutName(Options.Instance.LEDLayout));
            
            // Permissive Calibration (only for Wiimote Bar)
            // Moved to new line to avoid truncation/overlap (EN/FR: Déplacé sur nouvelle ligne)
            _optionsYOffset += 5; // Extra spacing
            // optPermissiveCalibration = AddOptionCheckBox("Permissive Calibration (Large Screens)", Options.Instance.PermissiveWiimoteBarCalibration);
            // Fix layout: Move to left to avoid horizontal scrolling (EN/FR: Déplacer à gauche pour éviter défilement horizontal)
            // optPermissiveCalibration.Location = new Point(20, optPermissiveCalibration.Location.Y);
            
            // Visibility logic
            optLEDLayout.SelectedIndexChanged += (s, e) => 
            {
                bool isWiimoteBar = optLEDLayout.SelectedIndex == 0;
                // optPermissiveCalibration.Visible = isWiimoteBar;
            };
            // Initial visibility
            // optPermissiveCalibration.Visible = optLEDLayout.SelectedIndex == 0;
            
            AddOptionLabel("IR Sensitivity:");
            optIRSensitivity = AddOptionNumeric(Options.Instance.IRSensitivity, 1, 5);
            
            AddOptionLabel("Show Notifications:");
            optShowNotifications = AddOptionCheckBox("", Options.Instance.ShowNotifications);
        }
        
        private void LoadCalibrationOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("🎯 Calibration per Player");
            
            Label info = new Label
            {
                Text = "Auto-calibrate will be triggered on next Wiimote connection.\nManual calibration can be done via IR Visualizer.",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 40),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            panelOptionsContent.Controls.Add(info);
            _optionsYOffset += 50;
            
            // Note: Auto-calibrate is triggered by reconnection, not a toggle
            AddOptionLabel("Calibration is per-player");
            Label note = new Label
            {
                Text = "Use IR Visualizer page for manual calibration",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 25),
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            panelOptionsContent.Controls.Add(note);
            _optionsYOffset += 35;
        }
        
        private void LoadKeyboardOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("⌨️ Keyboard Settings");
            
            AddOptionLabel("Use Shared Keyboard:");
            optUseSharedKeyboard = AddOptionCheckBox("", Options.Instance.UseSharedKeyboard);
            
            Label info = new Label
            {
                Text = "When enabled, all players share the same keyboard.\nWhen disabled, each player needs a separate keyboard (Interception driver required).",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 50),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            panelOptionsContent.Controls.Add(info);
            _optionsYOffset += 60;
        }
        
        private void LoadDetectionOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("📡 Wiimote Detection");
            
            AddOptionLabel("Detect Dolphinbar:");
            optDetectDolphin = AddOptionCheckBox("", Options.Instance.DetectDolphinbar);
            
            AddOptionLabel("Detect Bluetooth:");
            optDetectBluetooth = AddOptionCheckBox("", Options.Instance.DetectBlueTooth);
            
            Label info = new Label
            {
                Text = "These options control which connection types are scanned for Wiimotes.\nRestart required after changing.",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 40),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            panelOptionsContent.Controls.Add(info);
            _optionsYOffset += 50;
        }
        
        private void LoadGesturesOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("🤌 Gesture Settings");
            
            AddOptionLabel("Off-Screen Reload:");
            optEnableOffScreenReload = AddOptionCheckBox("", Options.Instance.EnableOffScreenReload);
            
            AddOptionLabel("Auto Off-Screen:");
            optOffScreenReloadAuto = AddOptionCheckBox("", Options.Instance.OffScreenReloadAuto);
            
            // Experimental gestures (hidden by default)
            if (Options.Instance.EnableDevGestures)
            {
                AddSectionTitle("🧪 Experimental Gestures");
                
                AddOptionLabel("Shake Reload:");
                optEnableShakeReload = AddOptionCheckBox("", Options.Instance.EnableShakeReload);
                
                AddOptionLabel("Shake Sensitivity:");
                optShakeSensitivity = AddOptionTrackBar(Options.Instance.ShakeSensitivity, 0, 2);
                
                AddOptionLabel("Shake from Nunchuk:");
                optShakeFromNunchuk = AddOptionCheckBox("", Options.Instance.ShakeFromNunchuk);
                
                AddOptionLabel("Grenade Gesture:");
                optEnableGrenadeGesture = AddOptionCheckBox("", Options.Instance.EnableGrenadeGesture);
            }
        }
        
        private void LoadEmulatorsOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("🎮 Emulator Settings");
            
            AddOptionLabel("Restart on Dolphin:");
            optRestartOnDolphin = AddOptionCheckBox("", Options.Instance.RestartOnDolphin);
            
            AddOptionLabel("Restart on Cemu:");
            optRestartOnCemu = AddOptionCheckBox("", Options.Instance.RestartOnCemu);
            
            Label info = new Label
            {
                Text = "When enabled, WiimoteGun will automatically restart when the emulator is launched.",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 40),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            panelOptionsContent.Controls.Add(info);
            _optionsYOffset += 50;
        }
        
        private void LoadPlayersOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("👥 Player Settings");
            
            AddOptionLabel("Enable 4 Players:");
            optEnable4Players = AddOptionCheckBox("", Options.Instance.Enable4Players);
            
            Label info = new Label
            {
                Text = "When enabled, allows up to 4 Wiimotes to be used simultaneously.\nRequires restart to take effect.",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 40),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8.5F)
            };
            panelOptionsContent.Controls.Add(info);
            _optionsYOffset += 50;
            
            AddSectionTitle("Preferred Wiimote MAC Addresses");
            Label macInfo = new Label
            {
                Text = "Select a Wiimote from the dropdown and check the lock box to assign it permanently.",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 25),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8F)
            };
            panelOptionsContent.Controls.Add(macInfo);
            _optionsYOffset += 35;
            
            // Get list of connected Wiimotes (EN/FR: Obtenir liste des Wiimotes connectés)
            var connectedMacs = new System.Collections.Generic.List<string>();
            connectedMacs.Add("None (Auto)");
            
            try
            {
                if (Program.WiiMoteManager != null && Program.WiiMoteManager.Controllers != null)
                {
                    foreach (var controller in Program.WiiMoteManager.Controllers)
                    {
                        if (controller != null && controller.Wiimote != null)
                        {
                            // Try to get MAC address from Bluetooth or unique identifier
                            string mac = null;
                            
                            // Try BluetoothAddress first (for Bluetooth Wiimotes)
                            if (controller.Wiimote.Address != null && !controller.Wiimote.Address.IsInvalid)
                            {
                                mac = controller.Wiimote.Address.ToString();
                            }
                            // Fallback to UniqueId (works for both Bluetooth and DolphinBar)
                            else if (!string.IsNullOrEmpty(controller.Wiimote.UniqueId))
                            {
                                mac = controller.Wiimote.UniqueId;
                            }
                            
                            // Add to list if we got a valid identifier
                            if (!string.IsNullOrEmpty(mac) && !connectedMacs.Contains(mac))
                            {
                                connectedMacs.Add(mac);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to enumerate connected Wiimotes for MAC selection: {ex.Message}");
            }
            
            // Create ComboBox + CheckBox for each player (EN/FR: Créer ComboBox + CheckBox pour chaque joueur)
            for (int i = 1; i <= 4; i++)
            {
                AddOptionLabel($"Player {i} MAC:");
                
                ComboBox cmbMac = new ComboBox
                {
                    Location = new Point(200, _optionsYOffset),
                    Size = new Size(130, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = ColorText,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F),
                    Tag = $"P{i}MAC"
                };
                cmbMac.Items.AddRange(connectedMacs.ToArray());
                
                // Set selected value from Options (EN/FR: Définir valeur sélectionnée depuis Options)
                string currentMac = i == 1 ? Options.Instance.PreferredMacP1 :
                                   i == 2 ? Options.Instance.PreferredMacP2 :
                                   i == 3 ? Options.Instance.PreferredMacP3 :
                                   Options.Instance.PreferredMacP4;
                                   
                if (string.IsNullOrEmpty(currentMac))
                    cmbMac.SelectedIndex = 0; // "None (Auto)"
                else if (cmbMac.Items.Contains(currentMac))
                    cmbMac.SelectedItem = currentMac;
                else
                {
                    // MAC not in list (Wiimote disconnected), still show it
                    cmbMac.Items.Add(currentMac);
                    cmbMac.SelectedItem = currentMac;
                }
                
                panelOptionsContent.Controls.Add(cmbMac);
                
                // Lock CheckBox (EN/FR: CheckBox de verrouillage)
                CheckBox chkLock = new CheckBox
                {
                    Text = "🔒",
                    Location = new Point(335, _optionsYOffset),
                    Size = new Size(45, 25),
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 10F),
                    Checked = !string.IsNullOrEmpty(currentMac),
                    Tag = $"P{i}LOCK"
                };
                
                // When lock is checked, ensure no other player has this MAC locked (EN/FR: Quand verrouillé, s'assurer qu'aucun autre joueur n'a cette MAC verrouillée)
                chkLock.CheckedChanged += (s, e) =>
                {
                    if (!chkLock.Checked)
                    {
                        cmbMac.SelectedIndex = 0; // "None (Auto)"
                    }
                    else
                    {
                        // If locking, check if this MAC is already assigned to another player
                        string selectedMac = cmbMac.SelectedItem?.ToString();
                        if (!string.IsNullOrEmpty(selectedMac) && selectedMac != "None (Auto)")
                        {
                            // Find if any other player has this MAC locked
                            foreach (Control ctrl in panelOptionsContent.Controls)
                            {
                                if (ctrl is CheckBox otherLock && otherLock != chkLock && otherLock.Tag != null && otherLock.Tag.ToString().EndsWith("LOCK"))
                                {
                                    if (otherLock.Checked)
                                    {
                                        // Find the corresponding ComboBox
                                        string otherPlayerTag = otherLock.Tag.ToString().Replace("LOCK", "MAC");
                                        foreach (Control ctrl2 in panelOptionsContent.Controls)
                                        {
                                            if (ctrl2 is ComboBox otherCmb && otherCmb.Tag != null && otherCmb.Tag.ToString() == otherPlayerTag)
                                            {
                                                if (otherCmb.SelectedItem?.ToString() == selectedMac)
                                                {
                                                    // Same MAC found on another player - unlock it
                                                    otherLock.Checked = false;
                                                    SimpleLogger.Instance.Info($"Auto-unlocked {otherPlayerTag.Replace("MAC", "")} because {chkLock.Tag.ToString().Replace("LOCK", "")} selected the same Wiimote");
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                
                // When ComboBox selection changes while locked, check for conflicts (EN/FR: Quand sélection change alors que verrouillé, vérifier conflits)
                cmbMac.SelectedIndexChanged += (s, e) =>
                {
                    if (chkLock.Checked)
                    {
                        string selectedMac = cmbMac.SelectedItem?.ToString();
                        if (!string.IsNullOrEmpty(selectedMac) && selectedMac != "None (Auto)")
                        {
                            // Check if another player has this MAC locked
                            foreach (Control ctrl in panelOptionsContent.Controls)
                            {
                                if (ctrl is CheckBox otherLock && otherLock != chkLock && otherLock.Tag != null && otherLock.Tag.ToString().EndsWith("LOCK"))
                                {
                                    if (otherLock.Checked)
                                    {
                                        string otherPlayerTag = otherLock.Tag.ToString().Replace("LOCK", "MAC");
                                        foreach (Control ctrl2 in panelOptionsContent.Controls)
                                        {
                                            if (ctrl2 is ComboBox otherCmb && otherCmb.Tag != null && otherCmb.Tag.ToString() == otherPlayerTag)
                                            {
                                                if (otherCmb.SelectedItem?.ToString() == selectedMac)
                                                {
                                                    // Same MAC found - unlock the other player
                                                    otherLock.Checked = false;
                                                    SimpleLogger.Instance.Info($"Auto-unlocked {otherPlayerTag.Replace("MAC", "")} because {cmbMac.Tag.ToString().Replace("MAC", "")} selected the same Wiimote");
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                
                panelOptionsContent.Controls.Add(chkLock);
                
                _optionsYOffset += 35;
            }
        }
        
        private string GetLEDLayoutName(LEDLayoutType type)
        {
            switch (type)
            {
                case LEDLayoutType.WiimoteBar: return "Wiimote Bar";
                case LEDLayoutType.Gun4IRDiamond: return "Gun4IR Diamond";
                case LEDLayoutType.TwoWiimoteBar: return "Two Wiimote Bars";
                case LEDLayoutType.FourCorners: return "Four Corners";
                default: return "Wiimote Bar";
            }
        }
        
        private LEDLayoutType GetLEDLayoutFromName(string name)
        {
            switch (name)
            {
                case "Gun4IR Diamond": return LEDLayoutType.Gun4IRDiamond;
                case "Two Wiimote Bars": return LEDLayoutType.TwoWiimoteBar;
                case "Four Corners": return LEDLayoutType.FourCorners;
                default: return LEDLayoutType.WiimoteBar;
            }
        }
        
        private void SaveOptionsToInstance()
        {
            // General
            if (optMouseMode != null)
                Options.Instance.DefaultMouseMode = (MouseMode)Enum.Parse(typeof(MouseMode), optMouseMode.SelectedItem.ToString());
            if (optMonitorId != null)
                Options.Instance.MonitorId = (int)optMonitorId.Value;
            if (optLEDLayout != null)
                Options.Instance.LEDLayout = GetLEDLayoutFromName(optLEDLayout.SelectedItem.ToString());
            if (optPermissiveCalibration != null)
                // Options.Instance.PermissiveWiimoteBarCalibration = optPermissiveCalibration.Checked;
            if (optIRSensitivity != null)
                Options.Instance.IRSensitivity = (int)optIRSensitivity.Value;
            if (optShowNotifications != null)
                Options.Instance.ShowNotifications = optShowNotifications.Checked;
            
            // Keyboard
            if (optUseSharedKeyboard != null)
                Options.Instance.UseSharedKeyboard = optUseSharedKeyboard.Checked;
            
            // Detection
            if (optDetectDolphin != null)
                Options.Instance.DetectDolphinbar = optDetectDolphin.Checked;
            if (optDetectBluetooth != null)
                Options.Instance.DetectBlueTooth = optDetectBluetooth.Checked;
            
            // Gestures
            if (optEnableOffScreenReload != null)
                Options.Instance.EnableOffScreenReload = optEnableOffScreenReload.Checked;
            if (optOffScreenReloadAuto != null)
                Options.Instance.OffScreenReloadAuto = optOffScreenReloadAuto.Checked;
            if (optEnableShakeReload != null)
                Options.Instance.EnableShakeReload = optEnableShakeReload.Checked;
            if (optShakeSensitivity != null)
                Options.Instance.ShakeSensitivity = optShakeSensitivity.Value;
            if (optShakeFromNunchuk != null)
                Options.Instance.ShakeFromNunchuk = optShakeFromNunchuk.Checked;
            if (optEnableGrenadeGesture != null)
                Options.Instance.EnableGrenadeGesture = optEnableGrenadeGesture.Checked;
            
            
            // Emulators
            if (optRestartOnDolphin != null)
                Options.Instance.RestartOnDolphin = optRestartOnDolphin.Checked;
            if (optRestartOnCemu != null)
                Options.Instance.RestartOnCemu = optRestartOnCemu.Checked;
            
            // Players
            if (optEnable4Players != null)
                Options.Instance.Enable4Players = optEnable4Players.Checked;
            
            // MAC addresses - only save if locked (EN/FR: Adresses MAC - sauvegarder seulement si verrouillé)
            for (int i = 1; i <= 4; i++)
            {
                ComboBox cmbMac = null;
                CheckBox chkLock = null;
                
                // Find ComboBox and CheckBox for this player
                foreach (Control ctrl in panelOptionsContent.Controls)
                {
                    if (ctrl is ComboBox cmb && cmb.Tag != null && cmb.Tag.ToString() == $"P{i}MAC")
                        cmbMac = cmb;
                    if (ctrl is CheckBox chk && chk.Tag != null && chk.Tag.ToString() == $"P{i}LOCK")
                        chkLock = chk;
                }
                
                if (cmbMac != null && chkLock != null)
                {
                    string macValue = "";
                    
                    // If locked and not "None (Auto)", save the MAC address
                    if (chkLock.Checked && cmbMac.SelectedItem != null)
                    {
                        string selected = cmbMac.SelectedItem.ToString();
                        if (selected != "None (Auto)")
                            macValue = selected;
                    }
                    
                    // Save to Options
                    switch (i)
                    {
                        case 1: Options.Instance.PreferredMacP1 = macValue; break;
                        case 2: Options.Instance.PreferredMacP2 = macValue; break;
                        case 3: Options.Instance.PreferredMacP3 = macValue; break;
                        case 4: Options.Instance.PreferredMacP4 = macValue; break;
                    }
                }
            }
            
            // Save to file
            Options.Instance.Save();
        }
        
        private void LoadOptionsFromInstance()
        {
            // Reload current category to reset values
            ShowOptionsCategory(_currentOptionsCategory);
        }
        
        private void BtnApplyOptions_Click(object sender, EventArgs e)
        {
            // Save all options (EN/FR: Sauvegarder toutes les options)
            SaveOptionsToInstance();
            
            SimpleLogger.Instance.Info("Options saved. Executing refresh command...");
            
            // Show confirmation (EN/FR: Afficher confirmation)
            _toastNotification.Show("✓ Options saved. Restarting...", 2000);
            
            // Execute "wiimotegun -refresh" to restart (EN/FR: Exécuter "wiimotegun -refresh" pour redémarrer)
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wiimotegun.exe",
                    Arguments = "-refresh",
                    UseShellExecute = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                Process.Start(psi);
                
                // Close this instance (EN/FR: Fermer cette instance)
                Application.Exit();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to restart: {ex.Message}");
                _toastNotification.Show("⚠ Failed to restart. Please restart manually.", 3000);
            }
        }
    }
}
