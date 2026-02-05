using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;
using WiimoteLib;
using WiimoteGun.Controls;

namespace WiimoteGun
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();
            Font = SystemFonts.MessageBoxFont;

            numericUpDown1.Minimum = 0;  
            numericUpDown1.Maximum = Screen.AllScreens.Length - 1;
            numericUpDown1.DataBindings.Add("Value", Options.Instance, "MonitorId");
            
            cbStartWithWindows.DataBindings.Add("Checked", Options.Instance, "StartWithWindows");
            chkNotifications.DataBindings.Add("Checked", Options.Instance, "ShowNotifications");
            chkEnableGamePadSwap.DataBindings.Add("Checked", Options.Instance, "EnableGamePadSwapMode");
            chkPersistentGamePads.DataBindings.Add("Checked", Options.Instance, "PersistentGamePads");


            
            if (Options.Instance.DetectBlueTooth && Options.Instance.DetectDolphinbar)
                rbBoth.Checked = true;
            else if (Options.Instance.DetectDolphinbar)
                rbDolphinbar.Checked = true;
            else
                rbBlueTooth.Checked = true;

            trackBar1.SetRange(0, 5);
            trackBar1.Value = Options.Instance.IRSensitivity;
            


            // LED Layout ComboBox (EN/FR: ComboBox disposition LED)
            // LED Layout ComboBox (EN/FR: ComboBox disposition LED)
            cboLEDLayout.SelectedIndex = (int)Options.Instance.LEDLayout;
            cboLEDLayout.SelectedIndexChanged += CboLEDLayout_SelectedIndexChanged;
            
            // Permissive Calibration Binding
            // chkPermissiveCalibration.DataBindings.Add("Checked", Options.Instance, "PermissiveWiimoteBarCalibration");
            chkPermissiveCalibration.Visible = false; // Hide from UI as requested
            UpdatePermissiveCalibrationVisibility();

            // Gesture Controls Binding
            chkOffScreenReload.Checked = Options.Instance.EnableOffScreenReload;
            cboOffScreenMode.SelectedIndex = Options.Instance.OffScreenReloadAuto ? 1 : 0;
            chkGrenadeGesture.Checked = Options.Instance.EnableGrenadeGesture;
            
            if (Options.Instance.GrenadeFromNunchuk)
                rbGrenadeNunchuk.Checked = true;
            else
                rbGrenadeWiimote.Checked = true;
            chkEnableShake.DataBindings.Add("Checked", Options.Instance, "EnableShakeReload");
            
            if (Options.Instance.ShakeFromNunchuk)
                rbShakeNunchuk.Checked = true;
            else
                rbShakeWiimote.Checked = true;

            cboShakeSensitivity.SelectedIndex = Options.Instance.ShakeSensitivity;
            
            // Mouse Mode RadioButtons (EN/FR: Boutons radio Mode Souris)
            if (Options.Instance.DefaultMouseMode == MouseMode.SendInput)
            {
                rbMouseSendInput.Checked = true;
                lblMouseModeWarning.Visible = true;
            }
            else
            {
                rbMouseRawInput.Checked = true;
                lblMouseModeWarning.Visible = false;
            }

            // Hide dev gesture controls if not enabled (EN/FR: Cacher contrôles dev si non activé)
            bool showDevGestures = Options.Instance.EnableDevGestures;
            grpShakeReload.Visible = showDevGestures;
            chkGrenadeGesture.Visible = showDevGestures;
            rbGrenadeWiimote.Visible = showDevGestures;
            rbGrenadeNunchuk.Visible = showDevGestures;
            
            // Adjust group box height if dev gestures are hidden (EN/FR: Ajuster hauteur group box si gestes dev cachés)
            if (!showDevGestures)
            {
                grpGestures.Height = 50; // Only show Off-Screen Reload
            }
            
            // VMulti Auto-Lock binding (EN/FR: Liaison Auto-Lock VMulti)
            chkAutoLockVMulti.DataBindings.Add("Checked", Options.Instance, "AutoLockVMultiDevices");
            

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Options.Instance.DetectDolphinbar = rbBoth.Checked || rbDolphinbar.Checked;
            Options.Instance.DetectBlueTooth = rbBoth.Checked || rbBlueTooth.Checked;
            Options.Instance.StartWithWindows = cbStartWithWindows.Checked;
            Options.Instance.ShowNotifications = chkNotifications.Checked;
            Options.Instance.EnableGamePadSwapMode = chkEnableGamePadSwap.Checked;
            Options.Instance.PersistentGamePads = chkPersistentGamePads.Checked;
            Options.Instance.MonitorId = (int) numericUpDown1.Value;
            Options.Instance.IRSensitivity = trackBar1.Value;

            Options.Instance.LEDLayout = (LEDLayoutType)cboLEDLayout.SelectedIndex; // Save LED layout (EN/FR: Sauvegarder layout LED)
            // Options.Instance.PermissiveWiimoteBarCalibration = chkPermissiveCalibration.Checked;
            
            // Save Gesture Settings
            Options.Instance.EnableOffScreenReload = chkOffScreenReload.Checked;
            Options.Instance.OffScreenReloadAuto = cboOffScreenMode.SelectedIndex == 1;
            Options.Instance.EnableGrenadeGesture = chkGrenadeGesture.Checked;
            Options.Instance.GrenadeFromNunchuk = rbGrenadeNunchuk.Checked;
            Options.Instance.EnableShakeReload = chkEnableShake.Checked;
            Options.Instance.ShakeFromNunchuk = rbShakeNunchuk.Checked;
            Options.Instance.ShakeSensitivity = cboShakeSensitivity.SelectedIndex;
            
            // Save Mouse Mode (EN/FR: Sauvegarder Mode Souris)
            Options.Instance.DefaultMouseMode = rbMouseSendInput.Checked ? MouseMode.SendInput : MouseMode.RawInput;


            
            Options.Instance.Save();

            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;

            // Update IR Sensitivity immediately (EN/FR: Mettre à jour sensibilité IR immédiatement)
            if (Program.WiiMoteManager != null)
            {
                Program.WiiMoteManager.UpdateIRSensitivity();
            }

            DialogResult = DialogResult.OK;
            Close();
        }







        private bool RunAsAdmin(string args)
        {
            if (IsAdministrator())
            {
                // Already running as admin, this shouldn't happen from the UI
                return false;
            }

            try
            {
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                var startInfo = new ProcessStartInfo(exeName, args)
                {
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error trying to elevate privileges: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }



        private void rbMouseSendInput_CheckedChanged(object sender, EventArgs e)
        {
            // Show warning when SendInput is selected (EN/FR: Afficher avertissement si SendInput sélectionné)
            lblMouseModeWarning.Visible = rbMouseSendInput.Checked;
        }

        /// <summary>
        /// Install Player 1 (vmultia) - virtual1 driver (EN/FR: Installer Player 1)
        /// </summary>
        private void btnInstallPlayer1_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/installPlayer1"))
            {
                MessageBox.Show("Player 1 (vmultia) installation started.\\n\\nPlease wait for the elevated process to complete.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Install Player 2 (vmultib) - virtual2 driver (EN/FR: Installer Player 2)
        /// </summary>
        private void btnInstallPlayer2_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/installPlayer2"))
            {
                MessageBox.Show("Player 2 (vmultib) installation started.\\n\\nPlease wait for the elevated process to complete.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Install Player 3 (vmultic) - virtual3 driver (EN/FR: Installer Player 3)
        /// </summary>
        private void btnInstallPlayer3_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/installPlayer3"))
            {
                MessageBox.Show("Player 3 (vmultic) installation started.\\n\\nPlease wait for the elevated process to complete.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Install Player 4 (vmultid) - virtual4 driver (EN/FR: Installer Player 4)
        /// </summary>
        private void btnInstallPlayer4_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/installPlayer4"))
            {
                MessageBox.Show("Player 4 (vmultid) installation started.\\n\\nPlease wait for the elevated process to complete.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Uninstall Player 1 (EN/FR: Désinstaller Player 1)
        /// </summary>
        private void btnUninstallPlayer1_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/uninstallPlayer1"))
            {
                MessageBox.Show("Player 1 uninstallation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Uninstall Player 2 (EN/FR: Désinstaller Player 2)
        /// </summary>
        private void btnUninstallPlayer2_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/uninstallPlayer2"))
            {
                MessageBox.Show("Player 2 uninstallation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Uninstall Player 3 (EN/FR: Désinstaller Player 3)
        /// </summary>
        private void btnUninstallPlayer3_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/uninstallPlayer3"))
            {
                MessageBox.Show("Player 3 uninstallation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Uninstall Player 4 (EN/FR: Désinstaller Player 4)
        /// </summary>
        private void btnUninstallPlayer4_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/uninstallPlayer4"))
            {
                MessageBox.Show("Player 4 uninstallation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }



        /// <summary>
        /// Run PowerShell cleanup script for VMulti driver (EN/FR: Lancer script PowerShell nettoyage)
        /// </summary>
        private void RunCleanupScript(string driverName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string scriptPath = System.IO.Path.Combine(baseDir, "WiimoteGunDriver", "Scripts", string.Format("Disable-{0}-Unused.ps1", driverName));

                if (!System.IO.File.Exists(scriptPath))
                {
                    SimpleLogger.Instance.Warning($"Cleanup script not found: {scriptPath}");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true, // Show PowerShell window for user confirmation
                    Verb = "runas" // Run as admin
                };

                Process.Start(psi);
                SimpleLogger.Instance.Info($"Launched cleanup script: {scriptPath}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error running cleanup script: {ex.Message}");
            }
        }
        private void CboLEDLayout_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePermissiveCalibrationVisibility();
        }

        private void UpdatePermissiveCalibrationVisibility()
        {
            // Only show permissive calibration for WiimoteBar layout (Index 0)
            // (EN/FR: Afficher calibration permissive uniquement pour layout WiimoteBar)
            bool isWiimoteBar = cboLEDLayout.SelectedIndex == 0;
            // chkPermissiveCalibration.Visible = isWiimoteBar;
            chkPermissiveCalibration.Visible = false; // Always hidden as requested
        }

        private void btnConfigureGamePad_Click(object sender, EventArgs e)
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

                var control = new WiimoteGun.Controls.GamePadMappingControl();
                control.Dock = DockStyle.Fill;
                if (control.btnBack != null) control.btnBack.Visible = false; 
                control.BackRequested += (s, args) => form.Close();
                
                form.Controls.Add(control);
                form.ShowDialog(this);
            }
        }
    }
}
