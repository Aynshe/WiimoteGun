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

                // Bind Lock Button
                if (GetCtrl("btnLock") is Button btnLock)
                {
                    btnLock.Click += (s, e) =>
                    {
                        bool isLocked = Options.Instance.GetLockedSlot(playerIndex);
                        Options.Instance.SetLockedSlot(playerIndex, !isLocked);
                        Options.Instance.Save();
                        UpdateUI();
                    };
                }

                // Swap Up button (only for P2, P3, P4)
                // (EN/FR: Bouton swap vers le haut, uniquement pour P2, P3, P4)
                if (playerIndex > 1)
                {
                    if (GetCtrl("btnSwapUp") is Button btnSwapUp)
                    {
                        int targetPlayer = playerIndex - 1;
                        btnSwapUp.Click += (s, e) => PerformSwap(playerIndex, targetPlayer, btnSwapUp, "▲");
                    }
                }

                // Swap Down button (only for P1, P2, P3)
                // (EN/FR: Bouton swap vers le bas, uniquement pour P1, P2, P3)
                if (playerIndex < 4)
                {
                    if (GetCtrl("btnSwapDown") is Button btnSwapDown)
                    {
                        int targetPlayer = playerIndex + 1;
                        btnSwapDown.Click += (s, e) => PerformSwap(playerIndex, targetPlayer, btnSwapDown, "▼");
                    }
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
                Control GetCtrl(string prefix) => panel.Controls[prefix + playerIndex];
                
                var controller = controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
                bool isConnected = controller != null && controller.Wiimote.IsConnected;
                bool isLocked = Options.Instance.GetLockedSlot(playerIndex);

                // Update Lock Button
                if (GetCtrl("btnLock") is Button btnLock)
                {
                    if (isLocked)
                    {
                        btnLock.Text = "🔒 Locked";
                        btnLock.BackColor = Color.FromArgb(180, 40, 40); // Red
                    }
                    else
                    {
                        btnLock.Text = "🔓 Unlock";
                        btnLock.BackColor = Color.FromArgb(60, 60, 60); // Gray
                    }
                }

                Label lblStatus = GetCtrl("lblStatus") as Label;
                Label lblMac = GetCtrl("lblMac") as Label;
                Label lblBattery = GetCtrl("lblBattery") as Label;
                Button btnIdentify = GetCtrl("btnIdentify") as Button;
                
                if (isConnected)
                {
                    lblStatus.Text = "Connected";
                    lblStatus.ForeColor = Color.LightGreen;
                    lblMac.Text = "MAC: " + controller.Wiimote.Address.ToString();
                    
                    float batteryLevel = controller.Wiimote.WiimoteState.Status.Battery;
                    lblBattery.Text = $"🔋 {batteryLevel:F1}%";
                    
                    if (batteryLevel < 20) lblBattery.ForeColor = Color.Red;
                    else if (batteryLevel < 50) lblBattery.ForeColor = Color.Orange;
                    else lblBattery.ForeColor = Color.White;
                    
                    btnIdentify.Enabled = true;

                    // Update Swap Buttons State
                    // (EN/FR: Mettre à jour l'état des boutons swap)
                    if (playerIndex > 1)
                    {
                        if (GetCtrl("btnSwapUp") is Button btnSwapUp)
                        {
                            int targetP = playerIndex - 1;
                            bool targetLocked = Options.Instance.GetLockedSlot(targetP);
                            // Enable only if target not locked (EN/FR: Activer seulement si cible non verrouillée)
                            btnSwapUp.Enabled = !targetLocked;
                        }
                    }
                    if (playerIndex < 4)
                    {
                        if (GetCtrl("btnSwapDown") is Button btnSwapDown)
                        {
                            int targetP = playerIndex + 1;
                            bool targetLocked = Options.Instance.GetLockedSlot(targetP);
                            // Enable only if target not locked
                            btnSwapDown.Enabled = !targetLocked;
                        }
                    }
                }
                else
                {
                    if (isLocked)
                    {
                        lblStatus.Text = "🔒 LOCKED (Reserved)";
                        lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                    }
                    else
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

                    // Disable swap buttons if not connected
                    // (EN/FR: Désactiver boutons swap si non connecté)
                    if (playerIndex > 1)
                    {
                        Button btnSwapUp = GetCtrl("btnSwapUp") as Button;
                        if (btnSwapUp != null) btnSwapUp.Enabled = false;
                    }
                    if (playerIndex < 4)
                    {
                        Button btnSwapDown = GetCtrl("btnSwapDown") as Button;
                        if (btnSwapDown != null) btnSwapDown.Enabled = false;
                    }
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

        /// <summary>
        /// EN: Swap a Wiimote between two player slots, with visual feedback on the button.
        /// FR: Échanger une Wiimote entre deux slots joueur, avec retour visuel sur le bouton.
        /// </summary>
        /// <param name="fromPlayer">Source player index</param>
        /// <param name="toPlayer">Target player index</param>
        /// <param name="swapButton">The button that triggered the swap</param>
        /// <param name="arrow">Arrow symbol for button text restoration (▲ or ▼)</param>
        private void PerformSwap(int fromPlayer, int toPlayer, System.Windows.Forms.Button swapButton, string arrow)
        {
            if (Program.WiiMoteManager == null) return;

            // Disable button during swap to prevent double-click
            // (EN/FR: Désactiver le bouton pendant le swap pour éviter double-clic)
            swapButton.Enabled = false;
            swapButton.Text = "⏳ Swapping...";

            SimpleLogger.Instance.Info($"[UI] Swap requested: P{fromPlayer} → P{toPlayer}");

            Program.WiiMoteManager.SwapPlayerSlot(fromPlayer, toPlayer, success =>
            {
                // Callback runs on UI thread (EN/FR: Callback exécuté sur thread UI)
                if (success)
                {
                    SimpleLogger.Instance.Info($"[UI] Swap P{fromPlayer} → P{toPlayer} completed successfully");
                }
                else
                {
                    SimpleLogger.Instance.Warning($"[UI] Swap P{fromPlayer} → P{toPlayer} failed");
                }

                // Restore button text (EN/FR: Restaurer le texte du bouton)
                swapButton.Text = $"{arrow} P{toPlayer}";
                UpdateUI();
            });
        }
    }
}
