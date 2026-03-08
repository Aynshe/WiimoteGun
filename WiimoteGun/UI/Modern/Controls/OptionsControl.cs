using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using WiimoteGun;
using WiimoteGun.Common;
using WiimoteGun.Core;

namespace WiimoteGun.Controls
{
    public partial class OptionsControl : UserControl
    {
        public OptionsControl()
        {
            InitializeComponent();
            BindEvents();
            LoadOptionsFromInstance();
            
            // Default tab
            SwitchTab(btnTabGeneral, 0);

            // Hide tabs at runtime (Developer Note: Tabs are visible in Designer for editing)
            this.tabsOptions.Appearance = TabAppearance.FlatButtons;
            this.tabsOptions.ItemSize = new Size(0, 1);
            this.tabsOptions.SizeMode = TabSizeMode.Fixed;
        }

        private void BindEvents()
        {
            // Tab Switching
            // Tab Switching
            btnTabGeneral.Click += (s, e) => SwitchTab(btnTabGeneral, 0);
            btnTabDetection.Click += (s, e) => SwitchTab(btnTabDetection, 1);
            btnTabGestures.Click += (s, e) => SwitchTab(btnTabGestures, 2);
            btnTabEmulators.Click += (s, e) => SwitchTab(btnTabEmulators, 3);


            // Button Actions
            btnApply.Click += BtnApplyOptions_Click;
            btnReset.Click += (s, e) => LoadOptionsFromInstance();
            
            // Hover Effects for Sidebar
            SetupHoverEffect(btnTabGeneral);

            SetupHoverEffect(btnTabDetection);
            SetupHoverEffect(btnTabGestures);
            SetupHoverEffect(btnTabEmulators);

            SetupHoverEffect(btnTabEmulators);

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            // Standalone Browsing Buttons
            btnBrowsePCSX2.Click += (s, e) => BrowseFolder(txtPCSX2Path, "Select PCSX2 Emulators Root Folder");
            btnBrowseDuckStation.Click += (s, e) => BrowseFolder(txtDuckStationPath, "Select DuckStation Emulators Root Folder");
            btnBrowseDolphin.Click += (s, e) => BrowseFolder(txtDolphinPath, "Select Dolphin Emulators Root Folder");
            btnBrowseCemu.Click += (s, e) => BrowseFolder(txtCemuPath, "Select Cemu Emulators Root Folder");
            
            // Standalone Toggle Logic
            optStandaloneMode.CheckedChanged += (s, e) => UpdateStandaloneUIState();

            // Mutual Exclusivity for Gestures (EN/FR: Exclusivité mutuelle pour les gestes)
            optShakeDevice.SelectedIndexChanged += (s, e) => EnsureGestureDeviceExclusivity(true);
            optGrenadeDevice.SelectedIndexChanged += (s, e) => EnsureGestureDeviceExclusivity(false);
        }

        private void EnsureGestureDeviceExclusivity(bool shakeChanged)
        {
            // Only enforce if both are enabled or we want to prevent same-device mapping
            // Index 0 = Wiimote, 1 = Nunchuk (EN/FR: Index 0 = Wiimote, 1 = Nunchuk)
            if (optShakeDevice.SelectedIndex == -1 || optGrenadeDevice.SelectedIndex == -1) return;

            if (optShakeDevice.SelectedIndex == optGrenadeDevice.SelectedIndex)
            {
                // If they match, swap the one that WASN'T just changed by the user (or the other one)
                // (EN/FR: Si identique, changer celui qui n'a pas été modifié par l'utilisateur)
                if (shakeChanged)
                    optGrenadeDevice.SelectedIndex = (optShakeDevice.SelectedIndex == 0) ? 1 : 0;
                else
                    optShakeDevice.SelectedIndex = (optGrenadeDevice.SelectedIndex == 0) ? 1 : 0;
            }
        }

        private void BrowseFolder(TextBox targetTextBox, string description)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = description;
                fbd.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(targetTextBox.Text) && System.IO.Directory.Exists(targetTextBox.Text))
                    fbd.SelectedPath = targetTextBox.Text;

                if (fbd.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    targetTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        public event EventHandler BackRequested;

        private void SetupHoverEffect(Button btn)
        {
            btn.MouseEnter += (s, e) => 
            {
                if (btn.BackColor != Color.FromArgb(0, 122, 204)) // Not selected
                    btn.BackColor = Color.FromArgb(60, 60, 60);
            };
            btn.MouseLeave += (s, e) => 
            {
                // If this is the active tab, keep blue, else dark
                int index = -1;
                if (btn == btnTabGeneral) index = 0;
                else if (btn == btnTabDetection) index = 1;
                else if (btn == btnTabGestures) index = 2;
                else if (btn == btnTabEmulators) index = 3;


                if (tabsOptions.SelectedIndex == index)
                    btn.BackColor = Color.FromArgb(0, 122, 204);
                else
                    btn.BackColor = Color.FromArgb(40, 40, 40);
            };
        }

        private void SwitchTab(Button activeBtn, int index)
        {
            tabsOptions.SelectedIndex = index;
            
            // Reset colors
            Color dark = Color.FromArgb(40, 40, 40);
            Color active = Color.FromArgb(0, 122, 204);
            
            btnTabGeneral.BackColor = dark;
            btnTabDetection.BackColor = dark;
            btnTabGestures.BackColor = dark;
            btnTabEmulators.BackColor = dark;

            
            // Set active
            // Set active
            activeBtn.BackColor = active;

            // EN: Refresh gesture lock state when switching to Gestures tab
            // FR: Rafraîchir l'état de verrouillage des gestes lors du passage à l'onglet Gestes
            if (index == 2)
                UpdateGestureLockState();
        }

        public void LoadOptionsFromInstance()
        {
            // General
            optMouseMode.SelectedItem = Options.Instance.DefaultMouseMode.ToString();
            optMonitorId.Value = Math.Min(Math.Max(Options.Instance.MonitorId, optMonitorId.Minimum), optMonitorId.Maximum);
            optLEDLayout.SelectedItem = GetLEDLayoutName(Options.Instance.LEDLayout);
            optIRSensitivity.Value = Math.Min(Math.Max(Options.Instance.IRSensitivity, optIRSensitivity.Minimum), optIRSensitivity.Maximum);
            optShowNotifications.Checked = Options.Instance.ShowNotifications;
            optEnableGamePadSwap.Checked = Options.Instance.EnableGamePadSwapMode;
            optPersistentGamePads.Checked = Options.Instance.PersistentGamePads;
            optEnableFPSMode.Checked = Options.Instance.EnableFPSMode;

            // Log Level
            optLogLevel.SelectedItem = Options.Instance.LoggingLevel.ToString();

            // Auto-Start
            optAutoStart.Items.Clear();
            optAutoStart.Items.Add("None");
            optAutoStart.Items.Add("Windows Startup");
            optAutoStart.Items.Add("RetroBat Startup");
            optAutoStart.SelectedIndex = (int)Options.Instance.AutoStart;


            // Detection
            optDetectDolphin.Checked = Options.Instance.DetectDolphinbar;
            optDetectBluetooth.Checked = Options.Instance.DetectBlueTooth;

            // Gestures (EN/FR: Gestes)
            optEnableOffScreenReload.Checked = Options.Instance.EnableOffScreenReload;
            optOffScreenReloadAuto.Checked = Options.Instance.OffScreenReloadAuto;
            optEnableShakeReload.Checked = Options.Instance.EnableShakeReload;
            
            // Map 0-3 index to Very Low, Low, Medium, High (EN/FR: Index 0-3 vers les niveaux de sensibilité)
            optShakeSensitivity.SelectedIndex = Math.Min(Math.Max(Options.Instance.ShakeSensitivity, 0), 3);
            
            // Map bool to index: 0=Wiimote, 1=Nunchuk (EN/FR: 0=Wiimote, 1=Nunchuk)
            optShakeDevice.SelectedIndex = Options.Instance.ShakeFromNunchuk ? 1 : 0;
            optGrenadeDevice.SelectedIndex = Options.Instance.GrenadeFromNunchuk ? 1 : 0;
            
            optEnableGrenadeGesture.Checked = Options.Instance.EnableGrenadeGesture;

            // Emulators
            optRestartOnDolphin.Checked = Options.Instance.RestartOnDolphin;
            optRestartOnCemu.Checked = Options.Instance.RestartOnCemu;

            // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
            optEnableIRSmoothing.Checked = Options.Instance.EnableIRSmoothing;
            optIRSmoothingStrength.Value = Math.Min(Math.Max(Options.Instance.IRSmoothingStrength, optIRSmoothingStrength.Minimum), optIRSmoothingStrength.Maximum);
            optUseHighPerfTimers.Checked = Options.Instance.UseHighPerfTimers;
            optEnableHomographyCache.Checked = Options.Instance.EnableHomographyCache;
            optEnableDistanceCompensation.Checked = Options.Instance.EnableDistanceCompensation;
            optUseIRExtrapolation.Checked = Options.Instance.UseIRExtrapolation;
            optIRExtrapolationStrength.Value = (decimal)Math.Min(Math.Max(Options.Instance.IRExtrapolationStrength, (float)optIRExtrapolationStrength.Minimum), (float)optIRExtrapolationStrength.Maximum);
            optEnableVirtualPolling.Checked = Options.Instance.EnableVirtualPolling;
            optVirtualPollingRate.Value = Math.Min(Math.Max(Options.Instance.VirtualPollingRate, (int)optVirtualPollingRate.Minimum), (int)optVirtualPollingRate.Maximum);

            // Standalone
            optStandaloneMode.Checked = Options.Instance.StandaloneMode;
            txtPCSX2Path.Text = Options.Instance.PCSX2Path;
            txtDuckStationPath.Text = Options.Instance.DuckStationPath;
            txtDolphinPath.Text = Options.Instance.DolphinPath;
            txtCemuPath.Text = Options.Instance.CemuPath;

            UpdateStandaloneUIState();

            // EN: Check if remap profiles have Shake mappings that override gesture settings
            // FR: Vérifier si les profils remap ont des mappings Shake qui priment sur les paramètres de gestes
            UpdateGestureLockState();
        }

        private void UpdateStandaloneUIState()
        {
            bool isStandalone = optStandaloneMode.Checked;

            // Enable/Disable controls
            txtPCSX2Path.Enabled = isStandalone;
            btnBrowsePCSX2.Enabled = isStandalone;
            txtDuckStationPath.Enabled = isStandalone;
            btnBrowseDuckStation.Enabled = isStandalone;
            txtDolphinPath.Enabled = isStandalone;
            btnBrowseDolphin.Enabled = isStandalone;
            txtCemuPath.Enabled = isStandalone;
            btnBrowseCemu.Enabled = isStandalone;

            // Update hint text if NOT standalone
            if (!isStandalone)
            {
                string autoHint = "(Auto-detected via RetroBat)";
                txtPCSX2Path.Text = autoHint;
                txtDuckStationPath.Text = autoHint;
                txtDolphinPath.Text = autoHint;
                txtCemuPath.Text = autoHint;
                
                txtPCSX2Path.ForeColor = Color.Gray;
                txtDuckStationPath.ForeColor = Color.Gray;
                txtDolphinPath.ForeColor = Color.Gray;
                txtCemuPath.ForeColor = Color.Gray;
            }
            else
            {
                // Restore actual paths from instance when re-enabled
                txtPCSX2Path.Text = Options.Instance.PCSX2Path;
                txtDuckStationPath.Text = Options.Instance.DuckStationPath;
                txtDolphinPath.Text = Options.Instance.DolphinPath;
                txtCemuPath.Text = Options.Instance.CemuPath;

                txtPCSX2Path.ForeColor = Color.White;
                txtDuckStationPath.ForeColor = Color.White;
                txtDolphinPath.ForeColor = Color.White;
                txtCemuPath.ForeColor = Color.White;
            }
        }

        /// <summary>
        /// EN: Checks active remap profiles for Shake mappings and locks/unlocks gesture settings accordingly.
        /// FR: Vérifie les profils remap actifs pour les mappings Shake et verrouille/déverrouille les paramètres de gestes.
        /// </summary>
        private void UpdateGestureLockState()
        {
            try
            {
                // EN: Check Mouse/Keyboard profile for P1 (FR: Vérifier le profil Souris/Clavier de P1)
                var mouseMappings = Options.Instance.P1Mappings;
                bool mouseShakeWiimoteMapped = mouseMappings != null && mouseMappings.AccelWiimoteShake != null &&
                    (mouseMappings.AccelWiimoteShake.Special != SpecialAction.None || mouseMappings.AccelWiimoteShake.Key != System.Windows.Forms.Keys.None);
                bool mouseShakeNunchukMapped = mouseMappings != null && mouseMappings.AccelNunchukShake != null &&
                    (mouseMappings.AccelNunchukShake.Special != SpecialAction.None || mouseMappings.AccelNunchukShake.Key != System.Windows.Forms.Keys.None);

                // EN: Check GamePad profile for P1 (FR: Vérifier le profil GamePad de P1)
                var padMappings = Options.Instance.P1GamePadMappings;
                bool padShakeWiimoteMapped = padMappings != null && padMappings.AccelWiimoteShake != null &&
                    padMappings.AccelWiimoteShake.TargetType != GamePadMotionTargetType.None;
                bool padShakeNunchukMapped = padMappings != null && padMappings.AccelNunchukShake != null &&
                    padMappings.AccelNunchukShake.TargetType != GamePadMotionTargetType.None;

                bool hasAnyShakeMapping = mouseShakeWiimoteMapped || mouseShakeNunchukMapped || padShakeWiimoteMapped || padShakeNunchukMapped;

                string lockReason = "";
                if (hasAnyShakeMapping)
                {
                    // EN: Build reason message (FR: Construire le message de raison)
                    if (mouseShakeWiimoteMapped || mouseShakeNunchukMapped)
                        lockReason += "Mouse/IR profile has Shake mapped. ";
                    if (padShakeWiimoteMapped || padShakeNunchukMapped)
                        lockReason += "GamePad profile has Shake mapped. ";
                    lockReason += "Remove the mapping to enable gestures.";
                }

                // EN: Lock/Unlock Shake Reload (FR: Verrouiller/Déverrouiller Shake Reload)
                optEnableShakeReload.Enabled = !hasAnyShakeMapping;
                if (hasAnyShakeMapping)
                {
                    optEnableShakeReload.Checked = false;
                    optEnableShakeReload.ForeColor = Color.Gray;
                    optEnableShakeReload.Text = "Shake Reload (Locked)";
                }
                else
                {
                    optEnableShakeReload.ForeColor = Color.White;
                    optEnableShakeReload.Text = "Shake Reload";
                }

                // EN: Lock/Unlock Grenade Gesture (FR: Verrouiller/Déverrouiller Grenade Gesture)
                optEnableGrenadeGesture.Enabled = !hasAnyShakeMapping;
                if (hasAnyShakeMapping)
                {
                    optEnableGrenadeGesture.Checked = false;
                    optEnableGrenadeGesture.ForeColor = Color.Gray;
                    optEnableGrenadeGesture.Text = "Grenade Gesture (Locked)";
                }
                else
                {
                    optEnableGrenadeGesture.ForeColor = Color.White;
                    optEnableGrenadeGesture.Text = "Grenade Gesture";
                }

                // EN: Show/Hide lock reason label (FR: Afficher/Masquer label de raison de verrouillage)
                if (lblGestureLockReason == null)
                {
                    lblGestureLockReason = new Label
                    {
                        Name = "lblGestureLockReason",
                        ForeColor = Color.FromArgb(255, 180, 0), // Orange warning color
                        Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                        AutoSize = false,
                        Size = new Size(340, 40),
                        Location = new Point(20, 310)
                    };
                    tabGestures.Controls.Add(lblGestureLockReason);
                }

                lblGestureLockReason.Text = hasAnyShakeMapping ? "⚠ " + lockReason : "";
                lblGestureLockReason.Visible = hasAnyShakeMapping;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("UpdateGestureLockState error: {0}", ex.Message));
            }
        }

        private Label lblGestureLockReason;

        private void BtnApplyOptions_Click(object sender, EventArgs e)
        {
            try 
            {
                // General
                if (optMouseMode.SelectedItem != null)
                    Options.Instance.DefaultMouseMode = (MouseMode)Enum.Parse(typeof(MouseMode), optMouseMode.SelectedItem.ToString());
                
                Options.Instance.MonitorId = (int)optMonitorId.Value;
                
                if (optLEDLayout.SelectedItem != null)
                    Options.Instance.LEDLayout = GetLEDLayoutFromName(optLEDLayout.SelectedItem.ToString());
                
                Options.Instance.IRSensitivity = (int)optIRSensitivity.Value;
                Options.Instance.ShowNotifications = optShowNotifications.Checked;
                Options.Instance.EnableGamePadSwapMode = optEnableGamePadSwap.Checked;
                Options.Instance.PersistentGamePads = optPersistentGamePads.Checked;
                Options.Instance.EnableFPSMode = optEnableFPSMode.Checked;

                // Save Log Level (EN/FR: Sauvegarder niveau de log)
                if (optLogLevel.SelectedItem != null)
                {
                    Options.Instance.LoggingLevel = (LogLevel)Enum.Parse(typeof(LogLevel), optLogLevel.SelectedItem.ToString());
                    SimpleLogger.Instance.Threshold = Options.Instance.LoggingLevel;
                }

                // Auto-Start
                Options.Instance.AutoStart = (AutoStartMode)optAutoStart.SelectedIndex;
                Options.Instance.ApplyAutoStart();



                // Detection
                Options.Instance.DetectDolphinbar = optDetectDolphin.Checked;
                Options.Instance.DetectBlueTooth = optDetectBluetooth.Checked;

                // Gestures (EN/FR: Gestes)
                Options.Instance.EnableOffScreenReload = optEnableOffScreenReload.Checked;
                Options.Instance.OffScreenReloadAuto = optOffScreenReloadAuto.Checked;
                Options.Instance.EnableShakeReload = optEnableShakeReload.Checked;
                
                if (optShakeSensitivity.SelectedIndex != -1)
                    Options.Instance.ShakeSensitivity = optShakeSensitivity.SelectedIndex;
                
                if (optShakeDevice.SelectedIndex != -1)
                    Options.Instance.ShakeFromNunchuk = (optShakeDevice.SelectedIndex == 1);

                if (optGrenadeDevice.SelectedIndex != -1)
                    Options.Instance.GrenadeFromNunchuk = (optGrenadeDevice.SelectedIndex == 1);
                
                Options.Instance.EnableGrenadeGesture = optEnableGrenadeGesture.Checked;

                // Emulators
                Options.Instance.RestartOnDolphin = optRestartOnDolphin.Checked;
                Options.Instance.RestartOnCemu = optRestartOnCemu.Checked;

                // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
                Options.Instance.EnableIRSmoothing = optEnableIRSmoothing.Checked;
                Options.Instance.IRSmoothingStrength = (int)optIRSmoothingStrength.Value;
                Options.Instance.UseHighPerfTimers = optUseHighPerfTimers.Checked;
                Options.Instance.EnableHomographyCache = optEnableHomographyCache.Checked;
                Options.Instance.EnableDistanceCompensation = optEnableDistanceCompensation.Checked;
                Options.Instance.UseIRExtrapolation = optUseIRExtrapolation.Checked;
                Options.Instance.IRExtrapolationStrength = (float)optIRExtrapolationStrength.Value;
                Options.Instance.EnableVirtualPolling = optEnableVirtualPolling.Checked;
                Options.Instance.VirtualPollingRate = (int)optVirtualPollingRate.Value;

                // Standalone - Only save paths if in standalone mode to avoid saving hints
                Options.Instance.StandaloneMode = optStandaloneMode.Checked;
                if (Options.Instance.StandaloneMode)
                {
                    Options.Instance.PCSX2Path = txtPCSX2Path.Text.Trim();
                    Options.Instance.DuckStationPath = txtDuckStationPath.Text.Trim();
                    Options.Instance.DolphinPath = txtDolphinPath.Text.Trim();
                    Options.Instance.CemuPath = txtCemuPath.Text.Trim();
                }


                // Save
                Options.Instance.Save();
                
                SimpleLogger.Instance.Info("Options saved. Restarting...");
                MessageBox.Show(this.FindForm(), "Options saved. Application will restart.", "Restart", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Restart Logic
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wiimotegun.exe",
                    Arguments = "-restart",
                    UseShellExecute = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                Process.Start(psi);
                Program.IsRestarting = true;
                Application.Exit();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error saving options: {ex.Message}");
                MessageBox.Show(this.FindForm(), $"Error saving options: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLEDLayoutName(LEDLayoutType type)
        {
            switch (type)
            {
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

        private void BtnConfigureGamePad_Click(object sender, EventArgs e)
        {
            using (Form form = new Form
            {
                Text = "GamePad Mapping Configuration",
                Size = new Size(580, 820),
                StartPosition = FormStartPosition.CenterParent,
                ShowIcon = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(20, 20, 20)
            })
            {

                var control = new GamePadMappingControl
                {
                    Dock = DockStyle.Fill
                };
                if (control.btnBack != null) control.btnBack.Visible = false;
                control.BackRequested += (s, args) => form.Close();
                
                form.Controls.Add(control);
                form.ShowDialog(this);
            }
        }


    }
}
