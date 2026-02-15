using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using WiimoteGun;
using WiimoteGun.Common;

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
        }

        private void BrowseFolder(TextBox targetTextBox, string description)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = description;
                fbd.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(targetTextBox.Text) && System.IO.Directory.Exists(targetTextBox.Text))
                    fbd.SelectedPath = targetTextBox.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
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
            activeBtn.BackColor = active;
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

            // Log Level
            optLogLevel.SelectedItem = Options.Instance.LoggingLevel.ToString();



            // Detection
            optDetectDolphin.Checked = Options.Instance.DetectDolphinbar;
            optDetectBluetooth.Checked = Options.Instance.DetectBlueTooth;

            // Gestures
            optEnableOffScreenReload.Checked = Options.Instance.EnableOffScreenReload;
            optOffScreenReloadAuto.Checked = Options.Instance.OffScreenReloadAuto;
            optEnableShakeReload.Checked = Options.Instance.EnableShakeReload;
            optShakeSensitivity.Value = Options.Instance.ShakeSensitivity;
            optShakeFromNunchuk.Checked = Options.Instance.ShakeFromNunchuk;
            optEnableGrenadeGesture.Checked = Options.Instance.EnableGrenadeGesture;

            // Emulators
            optRestartOnDolphin.Checked = Options.Instance.RestartOnDolphin;
            optRestartOnCemu.Checked = Options.Instance.RestartOnCemu;

            // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
            optEnableIRSmoothing.Checked = Options.Instance.EnableIRSmoothing;
            optIRSmoothingStrength.Value = Math.Min(Math.Max(Options.Instance.IRSmoothingStrength, optIRSmoothingStrength.Minimum), optIRSmoothingStrength.Maximum);
            optUseHighPerfTimers.Checked = Options.Instance.UseHighPerfTimers;
            optEnableHomographyCache.Checked = Options.Instance.EnableHomographyCache;
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

                // Save Log Level (EN/FR: Sauvegarder niveau de log)
                if (optLogLevel.SelectedItem != null)
                {
                    Options.Instance.LoggingLevel = (LogLevel)Enum.Parse(typeof(LogLevel), optLogLevel.SelectedItem.ToString());
                    SimpleLogger.Instance.Threshold = Options.Instance.LoggingLevel;
                }



                // Detection
                Options.Instance.DetectDolphinbar = optDetectDolphin.Checked;
                Options.Instance.DetectBlueTooth = optDetectBluetooth.Checked;

                // Gestures
                Options.Instance.EnableOffScreenReload = optEnableOffScreenReload.Checked;
                Options.Instance.OffScreenReloadAuto = optOffScreenReloadAuto.Checked;
                Options.Instance.EnableShakeReload = optEnableShakeReload.Checked;
                Options.Instance.ShakeSensitivity = optShakeSensitivity.Value;
                Options.Instance.ShakeFromNunchuk = optShakeFromNunchuk.Checked;
                Options.Instance.EnableGrenadeGesture = optEnableGrenadeGesture.Checked;

                // Emulators
                Options.Instance.RestartOnDolphin = optRestartOnDolphin.Checked;
                Options.Instance.RestartOnCemu = optRestartOnCemu.Checked;

                // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
                Options.Instance.EnableIRSmoothing = optEnableIRSmoothing.Checked;
                Options.Instance.IRSmoothingStrength = (int)optIRSmoothingStrength.Value;
                Options.Instance.UseHighPerfTimers = optUseHighPerfTimers.Checked;
                Options.Instance.EnableHomographyCache = optEnableHomographyCache.Checked;
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
                MessageBox.Show("Options saved. Application will restart.", "Restart", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Restart Logic
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wiimotegun.exe",
                    Arguments = "-restart",
                    UseShellExecute = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error saving options: {ex.Message}");
                MessageBox.Show($"Error saving options: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (Form form = new Form())
            {
                form.Text = "GamePad Mapping Configuration";
                form.Size = new Size(580, 820);
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowIcon = false;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Color.FromArgb(20, 20, 20);

                var control = new GamePadMappingControl();
                control.Dock = DockStyle.Fill;
                if (control.btnBack != null) control.btnBack.Visible = false;
                control.BackRequested += (s, args) => form.Close();
                
                form.Controls.Add(control);
                form.ShowDialog(this);
            }
        }
    }
}
