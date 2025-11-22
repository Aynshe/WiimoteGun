using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;
using WiimoteLib;

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
            chk4Players.DataBindings.Add("Checked", Options.Instance, "Enable4Players");
            chkSharedKeyboard.DataBindings.Add("Checked", Options.Instance, "UseSharedKeyboard");
            
            if (Options.Instance.DetectBlueTooth && Options.Instance.DetectDolphinbar)
                rbBoth.Checked = true;
            else if (Options.Instance.DetectDolphinbar)
                rbDolphinbar.Checked = true;
            else
                rbBlueTooth.Checked = true;

            trackBar1.SetRange(0, 5);
            trackBar1.Value = Options.Instance.IRSensitivity;
            
            chk4Players.Checked = Options.Instance.Enable4Players;

            // LED Layout ComboBox (EN/FR: ComboBox disposition LED)
            cboLEDLayout.SelectedIndex = (int)Options.Instance.LEDLayout;
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
            Options.Instance.MonitorId = (int) numericUpDown1.Value;
            Options.Instance.IRSensitivity = trackBar1.Value;
            Options.Instance.Enable4Players = chk4Players.Checked;
            Options.Instance.LEDLayout = (LEDLayoutType)cboLEDLayout.SelectedIndex; // Save LED layout (EN/FR: Sauvegarder layout LED)
            Options.Instance.Save();

            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/installDrivers"))
            {
                MessageBox.Show("Driver installation process started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            if (RunAsAdmin("/uninstallDrivers"))
            {
                MessageBox.Show("Driver uninstallation process started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
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

        private void btn4PlayersInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "4-Player Mode (Experimental)\n\n" +
                "This mode allows up to 4 Wiimotes to connect simultaneously.\n\n" +
                "IMPORTANT REQUIREMENTS:\n" +
                "• You need 4 physical keyboard devices (USB keyboards or dongles)\n" +
                "• You need 4 physical mouse devices (USB mice)\n" +
                "• Each player will be assigned a unique keyboard/mouse pair\n\n" +
                "NOTE: This feature is experimental and may be unstable with some\n" +
                "Bluetooth adapters. If you experience connection issues, disable\n" +
                "this option to limit to 2 players (stable mode).\n\n" +
                "Current detected devices:\n" +
                "- Keyboards: Check WiimoteGun.log for 'Assigned Keyboard Device ID'\n" +
                "- Mice: Interception drivers should detect all connected mice",
                "4-Player Mode Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
