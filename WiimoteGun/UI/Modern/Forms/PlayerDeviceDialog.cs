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
    public partial class PlayerDeviceDialog : Form
    {
        private int _playerIndex;
        
        private Dictionary<string, string> _mouseDevices; // Display Name -> Hardware ID
        private Dictionary<string, string> _keyboardDevices; // Display Name -> Hardware ID
        
        public PlayerDeviceDialog(int playerIndex)
        {
            _playerIndex = playerIndex;
            InitializeComponent();
            
            // Set dynamic text that depends on _playerIndex
            this.Text = string.Format("Device Selection - Player {0}", _playerIndex);
            this.lblTitle.Text = string.Format("🎮 Player {0} Device Configuration", _playerIndex);
            
            // Set FlatAppearance border sizes (Designer doesn't support this)
            btnApply.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;
            
            LoadDevices();
        }
        
        // Event handler for cancel button
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        
        private void LoadDevices()
        {
            _mouseDevices = new Dictionary<string, string>();
            _keyboardDevices = new Dictionary<string, string>();
            
            // Load Mouse Devices (EN/FR: Charger périphériques souris)
            cmbMouse.Items.Add("None (Auto)");
            _mouseDevices["None (Auto)"] = "";
            
            // 1. Force Add VMulti Mouse Option (Offline Support)
            // (EN/FR: Forcer l'ajout de l'option Souris VMulti - Support Offline)
            // Since VMulti mice are created dynamically, they might not exist when this dialog is opened.
            // We blindly add the correct ID so the user can select it in advance.
            
             string[] vMultiSuffixes = { "vmultia", "vmultib", "vmultic", "vmultid" };
             if (_playerIndex >= 1 && _playerIndex <= 4)
             {
                 string suffix = vMultiSuffixes[_playerIndex - 1];
                 string vid = (_playerIndex == 1) ? "001F" : (_playerIndex == 2) ? "002F" : (_playerIndex == 3) ? "003F" : "004F";
                 
                 // Exact ID format expected by the driver/registry
                 string vmultiId = $"HID\\{suffix}&Col03HID\\VID_{vid}&UP:0001_U:0002HID_DEVICE_SYSTEM_MOUSEHID_DEVICE_UP:0001_U:0002HID_DEVICE";
                 
                 string displayName = $"VMulti Mouse [Player {_playerIndex}] (Virtual/Offline)";
                 
                 // Only add if not already present (should not be, but safety check)
                 if (!_mouseDevices.ContainsKey(displayName))
                 {
                     _mouseDevices[displayName] = vmultiId;
                     cmbMouse.Items.Add(displayName);
                     SimpleLogger.Instance.Info($"[DIALOG] Force-added VMulti Mouse option for P{_playerIndex}");
                 }
             }

            // Enumerate mouse devices (legacy enumeration) (EN/FR: Énumérer périphériques souris)
            try
            {
                var context = WiimoteGun.Interception.InterceptionDriver.interception_create_context();
                if (context != IntPtr.Zero)
                {
                    for (int i = 11; i <= 20; i++)
                    {
                        bool isMouse = WiimoteGun.Interception.InterceptionDriver.interception_is_mouse(i) != 0;
                        
                        // Get hardware ID (EN/FR: Récupérer ID matériel)
                        byte[] buffer = new byte[1000];
                        uint result = WiimoteGun.Interception.InterceptionDriver.interception_get_hardware_id(context, i, buffer, (uint)buffer.Length);
                        string hardwareId = "";

                        if (result > 0)
                        {
                            // Convert byte array to string (EN/FR: Convertir tableau bytes en string)
                            int byteCount = Math.Min((int)result * 2, buffer.Length);
                            hardwareId = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount);
                                
                            // Remove NULL characters (EN/FR: Supprimer caractères NULL)
                            hardwareId = hardwareId.Replace("\0", "").Trim();
                        }

                        // Accept if it's a mouse OR if we detected a VMulti ID (even if isMouse is false)
                        if (isMouse || (!string.IsNullOrEmpty(hardwareId) && hardwareId.IndexOf("vmulti", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            if (!string.IsNullOrEmpty(hardwareId))
                            {
                                // Check for duplicates (SetupAPI might have already added this)
                                // (EN/FR: Vérifier doublons - SetupAPI peut déjà l'avoir ajouté)
                                bool exists = _mouseDevices.ContainsValue(hardwareId) || 
                                              _mouseDevices.Values.Any(v => DeviceHelper.IsHardwareIdMatch(v, hardwareId));
                                              
                                if (exists)
                                {
                                    SimpleLogger.Instance.Info($"[DIALOG] Skipping duplicate for {hardwareId}");
                                    continue;
                                }

                                // Extract VID/PID for friendly name and unique identification (EN/FR: Extraire VID/PID pour nom et identification)
                                var vidPidResult = DeviceHelper.ExtractVidPid(hardwareId);
                                string vid = vidPidResult.Vid;
                                string pid = vidPidResult.Pid;
                                    
                                string displayName;
                                    
                                if (vid != null)
                                {
                                    string vidPidKey = pid != null ? string.Format("VID_{0}&PID_{1}", vid, pid) : string.Format("VID_{0}", vid);
                                        
                                    // Get friendly name (EN/FR: Récupérer nom commercial)
                                    // Pass hardwareId for robust VMulti detection
                                    string friendlyName = DeviceHelper.GetDeviceFriendlyName(vidPidKey, hardwareId);
                                        
                                    if (!string.IsNullOrEmpty(friendlyName))
                                    {
                                        displayName = string.Format("{0} (Device {1})", friendlyName, i);
                                    }
                                    else
                                    {
                                        displayName = string.Format("Mouse {0} ({1})", i, vidPidKey);
                                    }
                                }
                                else
                                {
                                    displayName = string.Format("Mouse {0} (Unknown)", i);
                                }
                                    
                                _mouseDevices[displayName] = hardwareId;
                                cmbMouse.Items.Add(displayName);
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
            
            // Force Add VMulti Keyboard Option (EN/FR: Forcer l'ajout de l'option Clavier VMulti)
            // Same logic as mouse: VMulti keyboards are virtual and might not be enumerable
            try
            {
                if (_playerIndex >= 1 && _playerIndex <= 4)
                {
                    string suffix = vMultiSuffixes[_playerIndex - 1];
                    string vid = (_playerIndex == 1) ? "001F" : (_playerIndex == 2) ? "002F" : (_playerIndex == 3) ? "003F" : "004F";
                    
                    // Keyboard hardware ID format (Col07 for keyboard)
                    string vmultiKeyboardId = string.Format("HID\\{0}&Col07HID\\VID_{1}&UP:0001_U:0006HID_DEVICE", suffix, vid);
                    string displayName = string.Format("VMulti Keyboard [Player {0}] (Virtual)", _playerIndex);
                    
                    if (!_keyboardDevices.ContainsKey(displayName))
                    {
                        _keyboardDevices[displayName] = vmultiKeyboardId;
                        cmbKeyboard.Items.Add(displayName);
                        SimpleLogger.Instance.Info(string.Format("[DIALOG] Added VMulti Keyboard option for P{0}", _playerIndex));
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to add VMulti keyboard: {0}", ex.Message));
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
                    string displayName = string.Format("Unknown Device ({0})", currentMouseId);
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
                    string displayName = string.Format("Unknown Device ({0})", currentKeyboardId);
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
                    Text = string.Format("🔒 Auto-locked to VMulti Player {0}", _playerIndex),
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
            
            SimpleLogger.Instance.Info(string.Format("Device preferences saved for Player {0}", _playerIndex));
            
            this.DialogResult = DialogResult.OK;
        }
    }
}
