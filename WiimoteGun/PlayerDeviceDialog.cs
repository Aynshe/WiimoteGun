using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun
{
    /// <summary>
    /// Modal dialog for selecting mouse and keyboard devices for a specific player
    /// FR: Dialogue modal pour sélectionner les périphériques souris et clavier pour un joueur spécifique
    /// </summary>
    public class PlayerDeviceDialog : Form
    {
        private int _playerIndex;
        private ComboBox cmbMouse;
        private ComboBox cmbKeyboard;
        private CheckBox chkLockMouse;
        private CheckBox chkLockKeyboard;
        private Label lblMouseInfo;
        private Label lblKeyboardInfo;
        
        private Dictionary<string, string> _mouseDevices; // Display Name -> Hardware ID
        private Dictionary<string, string> _keyboardDevices; // Display Name -> Hardware ID
        
        public PlayerDeviceDialog(int playerIndex)
        {
            _playerIndex = playerIndex;
            InitializeComponent();
            LoadDevices();
        }
        
        private void InitializeComponent()
        {
            // Form settings (EN/FR: Paramètres du formulaire)
            this.Text = $"Device Selection - Player {_playerIndex}";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            
            int yPos = 20;
            
            // Title (EN/FR: Titre)
            Label lblTitle = new Label
            {
                Text = $"🎮 Player {_playerIndex} Device Configuration",
                Location = new Point(20, yPos),
                Size = new Size(410, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);
            yPos += 50;
            
            // Mouse Section (EN/FR: Section Souris)
            Label lblMouseTitle = new Label
            {
                Text = "🖱️ Mouse Device:",
                Location = new Point(20, yPos),
                Size = new Size(150, 25),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            this.Controls.Add(lblMouseTitle);
            yPos += 30;
            
            cmbMouse = new ComboBox
            {
                Location = new Point(20, yPos),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(cmbMouse);
            
            chkLockMouse = new CheckBox
            {
                Text = "🔒",
                Location = new Point(370, yPos),
                Size = new Size(50, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F)
            };
            chkLockMouse.CheckedChanged += ChkLockMouse_CheckedChanged;
            cmbMouse.SelectedIndexChanged += CmbMouse_SelectedIndexChanged;
            this.Controls.Add(chkLockMouse);
            yPos += 30;
            
            lblMouseInfo = new Label
            {
                Text = "",
                Location = new Point(20, yPos),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
            this.Controls.Add(lblMouseInfo);
            yPos += 40;
            
            // Keyboard Section (EN/FR: Section Clavier)
            Label lblKeyboardTitle = new Label
            {
                Text = "⌨️ Keyboard Device:",
                Location = new Point(20, yPos),
                Size = new Size(180, 25),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            this.Controls.Add(lblKeyboardTitle);
            yPos += 30;
            
            cmbKeyboard = new ComboBox
            {
                Location = new Point(20, yPos),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(cmbKeyboard);
            
            chkLockKeyboard = new CheckBox
            {
                Text = "🔒",
                Location = new Point(370, yPos),
                Size = new Size(50, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F)
            };
            chkLockKeyboard.CheckedChanged += ChkLockKeyboard_CheckedChanged;
            cmbKeyboard.SelectedIndexChanged += CmbKeyboard_SelectedIndexChanged;
            this.Controls.Add(chkLockKeyboard);
            yPos += 30;
            
            lblKeyboardInfo = new Label
            {
                Text = "",
                Location = new Point(20, yPos),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
            this.Controls.Add(lblKeyboardInfo);
            yPos += 40;
            
            // Buttons (EN/FR: Boutons)
            Button btnApply = new Button
            {
                Text = "✓ Apply",
                Size = new Size(120, 35),
                Location = new Point(130, yPos),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;
            this.Controls.Add(btnApply);
            
            Button btnCancel = new Button
            {
                Text = "✗ Cancel",
                Size = new Size(120, 35),
                Location = new Point(260, yPos),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
        }
        
        private void LoadDevices()
        {
            _mouseDevices = new Dictionary<string, string>();
            _keyboardDevices = new Dictionary<string, string>();
            
            // Load Mouse Devices (EN/FR: Charger périphériques souris)
            cmbMouse.Items.Add("None (Auto)");
            _mouseDevices["None (Auto)"] = "";
            
            // Enumerate mouse devices via Interception (EN/FR: Énumérer périphériques souris via Interception)
            try
            {
                var context = WiimoteGun.Interception.InterceptionDriver.interception_create_context();
                if (context != IntPtr.Zero)
                {
                    for (int i = 11; i <= 20; i++)
                    {
                        if (WiimoteGun.Interception.InterceptionDriver.interception_is_mouse(i) != 0)
                        {
                            // Get hardware ID (EN/FR: Récupérer ID matériel)
                            byte[] buffer = new byte[1000];
                            uint result = WiimoteGun.Interception.InterceptionDriver.interception_get_hardware_id(context, i, buffer, (uint)buffer.Length);
                            
                            if (result > 0)
                            {
                                // Convert byte array to string (EN/FR: Convertir tableau bytes en string)
                                int byteCount = Math.Min((int)result * 2, buffer.Length);
                                string hardwareId = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount);
                                
                                // Remove NULL characters (EN/FR: Supprimer caractères NULL)
                                hardwareId = hardwareId.Replace("\0", "").Trim();
                                
                                if (!string.IsNullOrEmpty(hardwareId))
                                {
                                    // Extract VID/PID for friendly name and unique identification (EN/FR: Extraire VID/PID pour nom et identification)
                                    var vidPidResult = DeviceHelper.ExtractVidPid(hardwareId);
                                    string vid = vidPidResult.vid;
                                    string pid = vidPidResult.pid;
                                    
                                    string displayName;
                                    string identifierToSave;
                                    
                                    if (vid != null)
                                    {
                                        string vidPidKey = pid != null ? $"VID_{vid}&PID_{pid}" : $"VID_{vid}";
                                        
                                        // Get friendly name (EN/FR: Récupérer nom commercial)
                                        // Pass hardwareId for robust VMulti detection
                                        string friendlyName = DeviceHelper.GetDeviceFriendlyName(vidPidKey, hardwareId);
                                        
                                        if (!string.IsNullOrEmpty(friendlyName))
                                        {
                                            displayName = $"{friendlyName} (Device {i})";
                                        }
                                        else
                                        {
                                            displayName = $"Mouse {i} ({vidPidKey})";
                                        }
                                        
                                        // Save full hardware ID for unique identification (EN/FR: Sauvegarder ID matériel complet pour identification unique)
                                        identifierToSave = hardwareId;
                                    }
                                    else
                                    {
                                        displayName = $"Mouse {i} (Unknown)";
                                        identifierToSave = hardwareId;
                                    }
                                    
                                    _mouseDevices[displayName] = identifierToSave;
                                    cmbMouse.Items.Add(displayName);
                                }
                            }
                        }
                    }
                    WiimoteGun.Interception.InterceptionDriver.interception_destroy_context(context);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to enumerate mouse devices: {ex.Message}");
            }
            
            // Load Keyboard Devices (EN/FR: Charger périphériques clavier)
            cmbKeyboard.Items.Add("None (Auto)");
            _keyboardDevices["None (Auto)"] = "";
            
            try
            {
                var availableKeyboards = VirtualInterceptionKeyboard.GetAvailableKeyboardsWithNames();
                foreach (var kvp in availableKeyboards)
                {
                    int deviceId = kvp.Key;
                    string displayName = kvp.Value;
                    string hardwareId = VirtualInterceptionKeyboard.GetKeyboardHardwareId(deviceId);
                    
                    if (!string.IsNullOrEmpty(hardwareId))
                    {
                        _keyboardDevices[displayName] = hardwareId;
                        cmbKeyboard.Items.Add(displayName);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to enumerate keyboards: {ex.Message}");
            }
            
            // Set current values (EN/FR: Définir valeurs actuelles)
            string currentMouseId = Options.Instance.GetPreferredMouseId(_playerIndex);
            string currentKeyboardId = Options.Instance.GetPreferredKeyboardId(_playerIndex);
            
            // Select current mouse (EN/FR: Sélectionner souris actuelle)
            if (string.IsNullOrEmpty(currentMouseId))
            {
                cmbMouse.SelectedIndex = 0; // "None (Auto)"
                chkLockMouse.Checked = false;
            }
            else
            {
                var mouseEntry = _mouseDevices.FirstOrDefault(kv => kv.Value == currentMouseId);
                if (!string.IsNullOrEmpty(mouseEntry.Key))
                {
                    cmbMouse.SelectedItem = mouseEntry.Key;
                }
                else
                {
                    // Device not found, add it anyway
                    string displayName = $"Unknown Device ({currentMouseId})";
                    _mouseDevices[displayName] = currentMouseId;
                    cmbMouse.Items.Add(displayName);
                    cmbMouse.SelectedItem = displayName;
                }
                chkLockMouse.Checked = true;
            }
            
            // Select current keyboard (EN/FR: Sélectionner clavier actuel)
            if (string.IsNullOrEmpty(currentKeyboardId))
            {
                cmbKeyboard.SelectedIndex = 0; // "None (Auto)"
                chkLockKeyboard.Checked = false;
            }
            else
            {
                var keyboardEntry = _keyboardDevices.FirstOrDefault(kv => kv.Value == currentKeyboardId);
                if (!string.IsNullOrEmpty(keyboardEntry.Key))
                {
                    cmbKeyboard.SelectedItem = keyboardEntry.Key;
                }
                else
                {
                    // Device not found, add it anyway
                    string displayName = $"Unknown Device ({currentKeyboardId})";
                    _keyboardDevices[displayName] = currentKeyboardId;
                    cmbKeyboard.Items.Add(displayName);
                    cmbKeyboard.SelectedItem = displayName;
                }
                chkLockKeyboard.Checked = true;
            }
            
            // VMulti Auto-Lock: Disable controls for P1/P2 when option is enabled
            // (EN/FR: Auto-Lock VMulti : Désactiver contrôles P1/P2 si option activée)
            if (VMultiDeviceDetector.ShouldLockPlayerDevices(_playerIndex))
            {
                cmbMouse.Enabled = false;
                cmbKeyboard.Enabled = false;
                chkLockMouse.Enabled = false;
                chkLockKeyboard.Enabled = false;
                
                // Add info label to show why controls are locked (EN/FR: Ajouter label info pour expliquer verrouillage)
                Label lblLockInfo = new Label
                {
                    Text = $"🔒 Auto-locked to VMulti Player {_playerIndex}",
                    ForeColor = Color.Orange,
                    AutoSize = true,
                    Location = new Point(10, cmbKeyboard.Bottom + 10),
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic)
                };
                this.Controls.Add(lblLockInfo);
            }
            
            UpdateInfoLabels();
        }
        
        private void UpdateInfoLabels()
        {
            if (!chkLockMouse.Checked || cmbMouse.SelectedIndex == 0)
            {
                lblMouseInfo.Text = "ℹ️ Auto mode: System will assign mouse automatically";
            }
            else
            {
                lblMouseInfo.Text = "🔒 Device locked for this player";
            }
            
            // Update keyboard info label
            if (!chkLockKeyboard.Checked || cmbKeyboard.SelectedIndex == 0)
            {
                lblKeyboardInfo.Text = "ℹ️ Auto mode: System will assign keyboard automatically";
            }
            else
            {
                lblKeyboardInfo.Text = "🔒 Device locked for this player";
            }
        }
        
        private void ChkLockMouse_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkLockMouse.Checked)
            {
                cmbMouse.SelectedIndex = 0; // Reset to "None (Auto)"
            }
            UpdateInfoLabels();
        }
        
        private void ChkLockKeyboard_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkLockKeyboard.Checked)
            {
                cmbKeyboard.SelectedIndex = 0; // Reset to "None (Auto)"
            }
            UpdateInfoLabels();
        }
        
        private void CmbMouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInfoLabels();
        }
        
        private void CmbKeyboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInfoLabels();
        }
        
        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Save mouse selection (EN/FR: Sauvegarder sélection souris)
            string selectedMouse = cmbMouse.SelectedItem?.ToString();
            if (chkLockMouse.Checked && !string.IsNullOrEmpty(selectedMouse) && selectedMouse != "None (Auto)")
            {
                string hardwareId = _mouseDevices.ContainsKey(selectedMouse) ? _mouseDevices[selectedMouse] : "";
                Options.Instance.SetPreferredMouseId(_playerIndex, hardwareId);
            }
            else
            {
                Options.Instance.SetPreferredMouseId(_playerIndex, "");
            }
            
            // Save keyboard selection (EN/FR: Sauvegarder sélection clavier)
            string selectedKeyboard = cmbKeyboard.SelectedItem?.ToString();
            if (chkLockKeyboard.Checked && !string.IsNullOrEmpty(selectedKeyboard) && selectedKeyboard != "None (Auto)")
            {
                string hardwareId = _keyboardDevices.ContainsKey(selectedKeyboard) ? _keyboardDevices[selectedKeyboard] : "";
                Options.Instance.SetPreferredKeyboardId(_playerIndex, hardwareId);
            }
            else
            {
                Options.Instance.SetPreferredKeyboardId(_playerIndex, "");
            }
            
            // Save to file (EN/FR: Sauvegarder dans le fichier)
            Options.Instance.Save();
            
            SimpleLogger.Instance.Info($"Device preferences saved for Player {_playerIndex}");
            
            this.DialogResult = DialogResult.OK;
        }
    }
}
