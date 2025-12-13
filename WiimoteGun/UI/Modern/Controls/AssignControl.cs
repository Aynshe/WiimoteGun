using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WiimoteLib;

namespace WiimoteGun.Controls
{
    public partial class AssignControl : UserControl
    {
        private System.Windows.Forms.Timer _assignRefreshTimer;
        private Panel[] _playerPanels;
        
        public AssignControl()
        {
            InitializeComponent();
            
            // Initialize panel array for easy iteration (EN/FR: Tableau panneaux pour itération)
            _playerPanels = new Panel[] { panelPlayer1, panelPlayer2, panelPlayer3, panelPlayer4 };
            
            // Bind events for static controls (EN/FR: Lier événements contrôles statiques)
            BindEvents();
            
            // Initialize timer
            _assignRefreshTimer = new System.Windows.Forms.Timer();
            _assignRefreshTimer.Interval = 2000; // 2s refresh is enough for battery
            _assignRefreshTimer.Tick += (s, e) => 
            {
                if (this.Visible) UpdateUI();
            };
        }

        public event EventHandler BackRequested;

        private void BindEvents()
        {
            if (btnBack != null)
            {
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);
            }

            for (int i = 0; i < 4; i++)
            {
                int playerIndex = i + 1;
                Panel panel = _playerPanels[i];
                
                // Helper to get control by name
                Control GetCtrl(string prefix) => panel.Controls[prefix + playerIndex];

                // Bind Buttons
                if (GetCtrl("btnDevices") is Button btnDevices)
                {
                    btnDevices.Click += (s, e) => OpenDeviceSelectionDialog(playerIndex);
                }
                
                if (GetCtrl("btnIdentify") is Button btnIdentify)
                {
                    btnIdentify.Click += (s, e) => IdentifyWiimote(playerIndex);
                }
                
                // Bind Rumble Settings
                if (GetCtrl("chkRumble") is CheckBox chkRumble)
                {
                    chkRumble.Checked = Options.Instance.GetEnableWeaponRumble(playerIndex);
                    chkRumble.CheckedChanged += (s, e) =>
                    {
                        Options.Instance.SetEnableWeaponRumble(playerIndex, chkRumble.Checked);
                        Options.Instance.Save();
                    };
                }
                
                if (GetCtrl("trkIntensity") is TrackBar trkIntensity)
                {
                    trkIntensity.Value = Options.Instance.GetRumbleIntensity(playerIndex);
                    Label lblVal = GetCtrl("lblIntensityVal") as Label;
                    if (lblVal != null) lblVal.Text = $"{trkIntensity.Value}%";
                    
                    trkIntensity.ValueChanged += (s, e) =>
                    {
                        Options.Instance.SetRumbleIntensity(playerIndex, trkIntensity.Value);
                        if (lblVal != null) lblVal.Text = $"{trkIntensity.Value}%";
                    };
                    trkIntensity.MouseUp += (s, e) => Options.Instance.Save();
                }
                
                if (GetCtrl("nudDuration") is NumericUpDown nudDuration)
                {
                    nudDuration.Value = Options.Instance.GetRumbleDurationMs(playerIndex);
                    nudDuration.ValueChanged += (s, e) =>
                    {
                        Options.Instance.SetRumbleDurationMs(playerIndex, (int)nudDuration.Value);
                        Options.Instance.Save();
                    };
                }
            }
        }

        public void LoadData()
        {
            UpdateUI();
            _assignRefreshTimer.Start();
        }
        
        public void UnloadData()
        {
            _assignRefreshTimer.Stop();
        }

        private void UpdateUI()
        {
            if (Program.WiiMoteManager == null) return;
            
            var controllers = Program.WiiMoteManager.Controllers.ToList();
            
            for (int i = 0; i < 4; i++)
            {
                int playerIndex = i + 1;
                Panel panel = _playerPanels[i];
                var controller = controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
                
                Control GetCtrl(string prefix) => panel.Controls[prefix + playerIndex];
                
                Label lblStatus = GetCtrl("lblStatus") as Label;
                Label lblMac = GetCtrl("lblMac") as Label;
                Label lblBattery = GetCtrl("lblBattery") as Label;
                Button btnIdentify = GetCtrl("btnIdentify") as Button;
                
                if (controller != null)
                {
                    if (lblStatus != null)
                    {
                        lblStatus.Text = "✓ Connected";
                        lblStatus.ForeColor = Color.LightGreen;
                    }
                    
                    if (lblMac != null) lblMac.Text = $"MAC: {controller.Wiimote.Address}";
                    
                    if (lblBattery != null)
                    {
                        float battery = controller.Wiimote.WiimoteState.Status.Battery;
                        lblBattery.Text = $"🔋 {battery:F0}%";
                        lblBattery.ForeColor = battery < 20 ? Color.Red : Color.White;
                    }

                    if (btnIdentify != null) btnIdentify.Enabled = true;
                }
                else
                {
                    if (lblStatus != null)
                    {
                        lblStatus.Text = "Waiting for connection...";
                        lblStatus.ForeColor = Color.Gray;
                    }
                    
                    if (lblMac != null) lblMac.Text = "MAC: --:--:--:--:--:--";
                    
                    if (lblBattery != null)
                    {
                        lblBattery.Text = "🔋 --%";
                        lblBattery.ForeColor = Color.Gray;
                    }
                    
                    if (btnIdentify != null) btnIdentify.Enabled = false;
                }
            }
        }

        private void IdentifyWiimote(int playerIndex)
        {
            var controller = Program.WiiMoteManager?.Controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
            if (controller != null)
            {
                try 
                {
                    controller.Wiimote.SetRumble(true);
                    System.Threading.Thread.Sleep(300);
                    controller.Wiimote.SetRumble(false);
                } catch { }
            }
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
                        UpdateUI();
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
