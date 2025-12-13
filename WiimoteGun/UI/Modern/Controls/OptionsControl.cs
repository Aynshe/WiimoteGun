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
            btnTabGeneral.Click += (s, e) => SwitchTab(btnTabGeneral, 0);
            btnTabKeyboard.Click += (s, e) => SwitchTab(btnTabKeyboard, 1);
            btnTabDetection.Click += (s, e) => SwitchTab(btnTabDetection, 2);
            btnTabGestures.Click += (s, e) => SwitchTab(btnTabGestures, 3);
            btnTabEmulators.Click += (s, e) => SwitchTab(btnTabEmulators, 4);
            btnTabPlayers.Click += (s, e) => SwitchTab(btnTabPlayers, 5);

            // Button Actions
            btnApply.Click += BtnApplyOptions_Click;
            btnReset.Click += (s, e) => LoadOptionsFromInstance();
            
            // Hover Effects for Sidebar
            SetupHoverEffect(btnTabGeneral);
            SetupHoverEffect(btnTabKeyboard);
            SetupHoverEffect(btnTabDetection);
            SetupHoverEffect(btnTabGestures);
            SetupHoverEffect(btnTabEmulators);
            SetupHoverEffect(btnTabPlayers);
            SetupHoverEffect(btnTabEmulators);
            SetupHoverEffect(btnTabPlayers);

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);
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
                else if (btn == btnTabKeyboard) index = 1;
                else if (btn == btnTabDetection) index = 2;
                else if (btn == btnTabGestures) index = 3;
                else if (btn == btnTabEmulators) index = 4;
                else if (btn == btnTabPlayers) index = 5;

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
            btnTabKeyboard.BackColor = dark;
            btnTabDetection.BackColor = dark;
            btnTabGestures.BackColor = dark;
            btnTabEmulators.BackColor = dark;
            btnTabPlayers.BackColor = dark;
            
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

            // Keyboard
            optUseSharedKeyboard.Checked = Options.Instance.UseSharedKeyboard;

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

            // Players
            optEnable4Players.Checked = Options.Instance.Enable4Players;
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

                // Keyboard
                Options.Instance.UseSharedKeyboard = optUseSharedKeyboard.Checked;

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

                // Players
                Options.Instance.Enable4Players = optEnable4Players.Checked;

                // Save
                Options.Instance.Save();
                
                SimpleLogger.Instance.Info("Options saved. Restarting...");
                MessageBox.Show("Options saved. Application will restart.", "Restart", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Restart Logic
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wiimotegun.exe",
                    Arguments = "-refresh",
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
    }
}
