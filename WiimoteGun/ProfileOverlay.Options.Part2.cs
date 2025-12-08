        // Global controls for option binding (EN/FR: Contrôles globaux pour liaison options)
        private ComboBox optMouseMode, optLEDLayout;
        private NumericUpDown optMonitorId, optIRSensitivity;
        private CheckBox optShowNotifications, optUseSharedKeyboard, optDetectDolphin, optDetectBluetooth;
        private CheckBox optEnableOffScreenReload, optOffScreenReloadAuto, optEnableShakeReload, optShakeFromNunchuk;
        private CheckBox optEnableGrenadeGesture, optRestartOnDolphin, optRestartOnCemu, optEnable4Players;
        private TrackBar optShakeSensitivity;
        
        private void LoadGeneralOptions()
        {
            _optionsYOffset = 10;
            AddSectionTitle("🎮 General Settings");
            
            AddOptionLabel("Mouse Mode:");
            optMouseMode = AddOptionComboBox(new[] { "SendInput", "RawInput" }, 
                Options.Instance.CurrentMouseMode.ToString());
            
            AddOptionLabel("Monitor ID:");
            optMonitorId = AddOptionNumeric(Options.Instance.MonitorId, 0, 9);
            
            AddOptionLabel("LED Layout Type:");
            optLEDLayout = AddOptionComboBox(new[] { "Wiimote Bar", "Gun4IR Diamond", "Two Wiimote Bars", "Four Corners" },
                GetLEDLayoutName(Options.Instance.LedLayoutType));
            
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
            
            AddOptionLabel("Shake Reload:");
            optEnableShakeReload = AddOptionCheckBox("", Options.Instance.EnableShakeReload);
            
            AddOptionLabel("Shake Sensitivity:");
            optShakeSensitivity = AddOptionTrackBar(Options.Instance.ShakeSensitivity, 0, 2);
            
            AddOptionLabel("Shake from Nunchuk:");
            optShakeFromNunchuk = AddOptionCheckBox("", Options.Instance.ShakeFromNunchuk);
            
            AddOptionLabel("Grenade Gesture:");
            optEnableGrenadeGesture = AddOptionCheckBox("", Options.Instance.EnableGrenadeGesture);
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
                Text = "Leave empty for automatic assignment. Format: AA:BB:CC:DD:EE:FF",
                Location = new Point(10, _optionsYOffset),
                Size = new Size(370, 25),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8F)
            };
            panelOptionsContent.Controls.Add(macInfo);
            _optionsYOffset += 35;
            
            for (int i = 1; i <= 4; i++)
            {
                AddOptionLabel($"Player {i} MAC:");
                TextBox txtMac = new TextBox
                {
                    Location = new Point(200, _optionsYOffset),
                    Size = new Size(180, 25),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 9F),
                    Text = i == 1 ? Options.Instance.PreferredMacP1 :
                           i == 2 ? Options.Instance.PreferredMacP2 :
                           i == 3 ? Options.Instance.PreferredMacP3 :
                           Options.Instance.PreferredMacP4
                };
                txtMac.Tag = $"P{i}MAC";
                panelOptionsContent.Controls.Add(txtMac);
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
                Options.Instance.CurrentMouseMode = (MouseMode)Enum.Parse(typeof(MouseMode), optMouseMode.SelectedItem.ToString());
            if (optMonitorId != null)
                Options.Instance.MonitorId = (int)optMonitorId.Value;
            if (optLEDLayout != null)
                Options.Instance.LedLayoutType = GetLEDLayoutFromName(optLEDLayout.SelectedItem.ToString());
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
            
            // MAC addresses
            foreach (Control ctrl in panelOptionsContent.Controls)
            {
                if (ctrl is TextBox txt && txt.Tag != null && txt.Tag.ToString().EndsWith("MAC"))
                {
                    switch (txt.Tag.ToString())
                    {
                        case "P1MAC": Options.Instance.PreferredMacP1 = txt.Text; break;
                        case "P2MAC": Options.Instance.PreferredMacP2 = txt.Text; break;
                        case "P3MAC": Options.Instance.PreferredMacP3 = txt.Text; break;
                        case "P4MAC": Options.Instance.PreferredMacP4 = txt.Text; break;
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
                    Arguments = "-restart",
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
