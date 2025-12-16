using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun.Controls
{
    public partial class MappingControl : UserControl
    {
        private void ShowVirtualKeyboard(TextBox target)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(target);
            keyboard.StartPosition = FormStartPosition.Manual;
            
            Point screenPos = target.PointToScreen(new Point(0, target.Height));
            keyboard.Location = new Point(
                screenPos.X + (target.Width - keyboard.Width) / 2,
                screenPos.Y + 5 
            );
            
            var screen = Screen.FromControl(this);
            if (keyboard.Bottom > screen.WorkingArea.Bottom)
            {
                 keyboard.Top = target.PointToScreen(Point.Empty).Y - keyboard.Height - 5;
            }

            keyboard.ShowDialog(this.FindForm());
        }

        // State (EN/FR: État)
        private string _currentExecutable;
        private string _currentExecutablePath;
        private int _currentPlayer = 1;
        private bool _isAssignMode = false;
        private string _waitingForButton = null;
        private int _waitingForPlayer = 1;
        private ButtonAction _originalMapping = null;
        private System.Threading.Timer _assignCountdownTimer;
        private int _assignCountdownSeconds = 8;
        private bool _updatingCheckbox = false;

        // Colors (EN/FR: Couleurs)
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);

        public MappingControl()
        {
            InitializeComponent();
            if (txtProfileName != null) txtProfileName.Click += (s, e) => ShowVirtualKeyboard(txtProfileName);
            
            // Set FlatAppearance properties (Designer doesn't support all)
            // (EN/FR: Définir propriétés d'apparence)
            btnSelectExe.FlatAppearance.BorderSize = 0;
            btnNewFolder.FlatAppearance.BorderSize = 0;
            btnDeleteProfile.FlatAppearance.BorderSize = 0;
            btnAssignMode.FlatAppearance.BorderSize = 0;
            btnHotkeys.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.BorderSize = 0;
            btnLoad.FlatAppearance.BorderSize = 0;
            btnConfirmAssign.FlatAppearance.BorderSize = 0;
            btnCancelAssign.FlatAppearance.BorderSize = 0;
            

            
            // Initialize UI (EN/FR: Initialiser UI)
            LoadProfileUI();
            LoadProfileUI();
            LoadCurrentMappings();

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler BackRequested;

        // Public methods (EN/FR: Méthodes publiques)
        public void LoadData()
        {
            LoadProfileUI();
            LoadCurrentMappings();
        }

        public void SetCurrentGame(string exeName, string exePath = null)
        {
            _currentExecutable = exeName;
            _currentExecutablePath = exePath ?? "";
            
            lblCurrentGame.Text = $"Current Game: {_currentExecutable ?? "None"}";
            UpdateCurrentGameLabel();
            UpdateAutoLoadCheckbox();
        }

        // Profile UI management (EN/FR: Gestion UI des profils)
        private void LoadProfileUI()
        {
            comboBoxSubfolders.Items.Clear();
            comboBoxSubfolders.Items.Add("(Root)");
            
            var subfolders = RemapProfileManager.GetSubfolders();
            foreach (var folder in subfolders)
            {
                comboBoxSubfolders.Items.Add(folder);
            }
            
            comboBoxSubfolders.SelectedIndex = 0;
            RefreshProfileList();
        }

        private void RefreshProfileList()
        {
            comboBoxProfiles.Items.Clear();
            
            string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
            if (selectedFolder == "(Root)")
                selectedFolder = null;
            
            var profiles = RemapProfileManager.GetProfilesInFolder(selectedFolder);
            foreach (var profile in profiles)
            {
                comboBoxProfiles.Items.Add(profile);
            }
            
            if (comboBoxProfiles.Items.Count > 0)
                comboBoxProfiles.SelectedIndex = 0;
        }

        private void UpdateCurrentGameLabel()
        {
            if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
            {
                string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                if (!string.IsNullOrEmpty(mappedProfile))
                {
                    lblLinkedExe.Text = $"Linked EXE: {_currentExecutable} → {mappedProfile}";
                    lblLinkedExe.ForeColor = ColorAccent;
                }
                else
                {
                    lblLinkedExe.Text = $"Linked EXE: {_currentExecutable} (not mapped)";
                    lblLinkedExe.ForeColor = Color.Gray;
                }
            }
            else
            {
                lblLinkedExe.Text = "Linked EXE: None";
                lblLinkedExe.ForeColor = Color.Gray;
            }
        }

        private void UpdateAutoLoadCheckbox()
        {
            _updatingCheckbox = true;
            
            if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
            {
                string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                string currentProfile = Program.GetActiveRemapProfile();
                
                chkAutoLoad.Checked = !string.IsNullOrEmpty(mappedProfile) && 
                                      !string.IsNullOrEmpty(currentProfile) &&
                                      mappedProfile.Equals(currentProfile, StringComparison.OrdinalIgnoreCase);
                chkAutoLoad.Enabled = true;
            }
            else
            {
                chkAutoLoad.Checked = false;
                chkAutoLoad.Enabled = false;
            }
            
            _updatingCheckbox = false;
        }

        // Load and display current mappings for selected player (EN/FR: Charger et afficher mappings joueur sélectionné)
        private void LoadCurrentMappings()
        {
            panelMappingDisplay.Controls.Clear();
            
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);
            LoadPlayerMappings(panelMappingDisplay, mappings);
        }

        // Display player mappings in 2 columns (Wiimote / Nunchuk) - from ProfileOverlay
        // (EN/FR: Afficher mappings joueur en 2 colonnes)
        private void LoadPlayerMappings(Panel panel, PlayerMappings mappings)
        {
            panel.Controls.Clear();
            
            int panelWidth = panel.Width;
            int yPos = 15;
            int labelWidth = 95;
            int valueWidth = 120;
            int spacing = 22;
            int columnSpacing = 35;
            
            int column1Width = labelWidth + valueWidth;
            int column2Width = labelWidth + valueWidth;
            int totalWidth = column1Width + columnSpacing + column2Width;
            int startX = (panelWidth - totalWidth) / 2;
            
            // Column 1: Wiimote (EN/FR: Colonne 1: Wiimote)
            int col1X = startX;
            Label lblWiimote = new Label
            {
                Text = "━━ Wiimote ━━",
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(col1X, yPos),
                Size = new Size(column1Width, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblWiimote);
            yPos += spacing + 5;
            
            Action<string, ButtonAction> AddWiimoteRow = (buttonName, mapping) =>
            {
                Label lblButton = new Label
                {
                    Text = buttonName + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(col1X, yPos),
                    Size = new Size(labelWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                Label lblMapping = new Label
                {
                    Text = GetMappingDisplay(mapping),
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Location = new Point(col1X + labelWidth + 5, yPos),
                    Size = new Size(valueWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                panel.Controls.Add(lblButton);
                panel.Controls.Add(lblMapping);
                yPos += spacing;
            };
            
            AddWiimoteRow("A Button", mappings.WiiA);
            AddWiimoteRow("B Button", mappings.WiiB);
            AddWiimoteRow("One", mappings.WiiOne);
            AddWiimoteRow("Two", mappings.WiiTwo);
            AddWiimoteRow("Plus", mappings.WiiPlus);
            AddWiimoteRow("Minus", mappings.WiiMinus);
            AddWiimoteRow("D-Pad Up", mappings.WiiUp);
            AddWiimoteRow("D-Pad Down", mappings.WiiDown);
            AddWiimoteRow("D-Pad Left", mappings.WiiLeft);
            AddWiimoteRow("D-Pad Right", mappings.WiiRight);
            
            // Column 2: Nunchuk (EN/FR: Colonne 2: Nunchuk)
            int col2X = col1X + column1Width + columnSpacing;
            yPos = 15;
            
            Label lblNunchuk = new Label
            {
                Text = "━━ Nunchuk ━━",
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(col2X, yPos),
                Size = new Size(column2Width, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblNunchuk);
            yPos += spacing + 5;
            
            Action<string, ButtonAction> AddNunchukRow = (buttonName, mapping) =>
            {
                Label lblButton = new Label
                {
                    Text = buttonName + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(col2X, yPos),
                    Size = new Size(labelWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                Label lblMapping = new Label
                {
                    Text = GetMappingDisplay(mapping),
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Location = new Point(col2X + labelWidth + 5, yPos),
                    Size = new Size(valueWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                panel.Controls.Add(lblButton);
                panel.Controls.Add(lblMapping);
                yPos += spacing;
            };
            
            if (mappings.NunC != null && mappings.NunZ != null)
            {
                AddNunchukRow("C Button", mappings.NunC);
                AddNunchukRow("Z Button", mappings.NunZ);
                AddNunchukRow("Stick Up", mappings.NunUp);
                AddNunchukRow("Stick Down", mappings.NunDown);
                AddNunchukRow("Stick Left", mappings.NunLeft);
                AddNunchukRow("Stick Right", mappings.NunRight);
            }
            else
            {
                Label lblNoNunchuk = new Label
                {
                    Text = "(Not configured)",
                    ForeColor = Color.FromArgb(128, 128, 128),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                    Location = new Point(col2X, yPos),
                    Size = new Size(column2Width, 18)
                };
                panel.Controls.Add(lblNoNunchuk);
            }
        }

        private string GetMappingDisplay(ButtonAction mapping)
        {
            if (mapping == null) return "None";
            
            if (mapping.Special != SpecialAction.None)
            {
                switch (mapping.Special)
                {
                    case SpecialAction.LeftMouse: return "🖱 Left Click";
                    case SpecialAction.RightMouse: return "🖱 Right Click";
                    case SpecialAction.MiddleMouse: return "🖱 Middle Click";
                    default: return mapping.Special.ToString();
                }
            }
            
            if (mapping.Key != Keys.None)
            {
                return $"⌨ {mapping.Key}";
            }
            
            return "None";
        }

        // ============================================
        // Assign Mode System (EN/FR: Système mode assignation)
        // ============================================

        private void BtnAssignMode_Click(object sender, EventArgs e)
        {
            if (_isAssignMode)
            {
                ExitAssignMode();
                return;
            }
            
            EnterAssignMode();
        }

        private void EnterAssignMode()
        {
            _isAssignMode = true;
            
            btnAssignMode.Text = "✖ Cancel Assign";
            btnAssignMode.BackColor = Color.FromArgb(180, 0, 0);
            
            lblAssignStatus.Text = $"⏱ Press any Wiimote/Nunchuk button\n({_assignCountdownSeconds}s)";
            lblAssignStatus.ForeColor = Color.Orange;
            lblAssignStatus.Visible = true;
            lblAssignStatus.BringToFront();
            
            LockWiimoteInputs(true);
            
            _assignCountdownSeconds = 8;
            _assignCountdownTimer = new System.Threading.Timer(OnAssignCountdownTick, null, 1000, 1000);
            
            SimpleLogger.Instance?.Info("Entered button assignment mode");
        }

        private void ExitAssignMode()
        {
            _isAssignMode = false;
            
            if (_assignCountdownTimer != null)
            {
                _assignCountdownTimer.Dispose();
                _assignCountdownTimer = null;
            }
            
            LockWiimoteInputs(false);
            
            btnAssignMode.Text = "🔷 Assign Button";
            btnAssignMode.BackColor = ColorAccent;
            lblAssignStatus.Visible = false;
            comboActionSelector.Visible = false;
            btnConfirmAssign.Visible = false;
            btnCancelAssign.Visible = false;
            
            _waitingForButton = null;
            _waitingForPlayer = 1;
            _originalMapping = null;
            
            SimpleLogger.Instance?.Info("Exited button assignment mode");
        }

        private void OnAssignCountdownTick(object state)
        {
            _assignCountdownSeconds--;
            
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnAssignCountdownTick(state)));
                return;
            }
            
            if (_assignCountdownSeconds <= 0)
            {
                ExitAssignMode();
                // Timeout notification
                return;
            }
            
            lblAssignStatus.Text = $"⏱ Press any Wiimote/Nunchuk button\n({_assignCountdownSeconds}s)";
            
            if (_assignCountdownSeconds <= 3)
                lblAssignStatus.ForeColor = Color.Red;
        }

        private void LockWiimoteInputs(bool locked)
        {
            WiiMoteController.SetInputLock(locked);
            
            if (locked)
            {
                WiiMoteController.ButtonPressed += OnWiimoteButtonPressed;
            }
            else
            {
                WiiMoteController.ButtonPressed -= OnWiimoteButtonPressed;
            }
        }

        // Tab player selection handler (EN/FR: Gestionnaire sélection joueur par tab)
        private void TabControlPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlPlayers == null) return;
            
            _currentPlayer = tabControlPlayers.SelectedIndex + 1;
            LoadCurrentMappings();
            
            // Update gyro checkbox for new player
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);
            if (mappings != null)
            {
                chkEnableGyro.Checked = mappings.EnableGyroAiming;
            }
        }

        private void OnWiimoteButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnWiimoteButtonPressed(sender, e)));
                return;
            }
            
            SimpleLogger.Instance?.Info($"Button detected: {e.ButtonName} from P{e.PlayerIndex}");
            
            if (_assignCountdownTimer != null)
            {
                _assignCountdownTimer.Dispose();
                _assignCountdownTimer = null;
            }
            
            _waitingForButton = e.ButtonName;
            _waitingForPlayer = e.PlayerIndex;
            _currentPlayer = e.PlayerIndex; // Switch to this player
            
            _originalMapping = GetCurrentMapping(e.PlayerIndex, e.ButtonName);
            
            ShowActionSelector(e.ButtonName);
        }

        private ButtonAction GetCurrentMapping(int playerIndex, string buttonName)
        {
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            
            switch (buttonName)
            {
                case "WiiA": return mappings.WiiA;
                case "WiiB": return mappings.WiiB;
                case "WiiOne": return mappings.WiiOne;
                case "WiiTwo": return mappings.WiiTwo;
                case "WiiPlus": return mappings.WiiPlus;
                case "WiiMinus": return mappings.WiiMinus;
                case "WiiUp": return mappings.WiiUp;
                case "WiiDown": return mappings.WiiDown;
                case "WiiLeft": return mappings.WiiLeft;
                case "WiiRight": return mappings.WiiRight;
                case "NunchukC": return mappings.NunC;
                case "NunchukZ": return mappings.NunZ;
                case "NunUp": return mappings.NunUp;
                case "NunDown": return mappings.NunDown;
                case "NunLeft": return mappings.NunLeft;
                case "NunRight": return mappings.NunRight;
                default: return new ButtonAction();
            }
        }

        private void ShowActionSelector(string buttonName)
        {
            comboActionSelector.Items.Clear();
            comboActionSelector.Items.Add("None");
            comboActionSelector.Items.Add("Mouse Left Click");
            comboActionSelector.Items.Add("Mouse Right Click");
            comboActionSelector.Items.Add("Mouse Middle Click");
            comboActionSelector.Items.Add("Keyboard Key...");
            
            string currentActionText = GetActionDisplayText(_originalMapping);
            int index = comboActionSelector.Items.IndexOf(currentActionText);
            if (index >= 0)
                comboActionSelector.SelectedIndex = index;
            else
                comboActionSelector.SelectedIndex = 0;
            
            lblAssignStatus.Text = $"Select action for {buttonName}:";
            lblAssignStatus.ForeColor = Color.LightGreen;
            
            comboActionSelector.Visible = true;
            btnConfirmAssign.Visible = true;
            btnCancelAssign.Visible = true;
            
            comboActionSelector.BringToFront();
            btnConfirmAssign.BringToFront();
            btnCancelAssign.BringToFront();
        }

        private string GetActionDisplayText(ButtonAction action)
        {
            if (action == null) return "None";
            
            if (action.Special != SpecialAction.None)
            {
                switch (action.Special)
                {
                    case SpecialAction.LeftMouse: return "Mouse Left Click";
                    case SpecialAction.RightMouse: return "Mouse Right Click";
                    case SpecialAction.MiddleMouse: return "Mouse Middle Click";
                    default: return action.Special.ToString();
                }
            }
            
            if (action.Key != Keys.None)
            {
                return $"Key: {action.Key}";
            }
            
            return "None";
        }

        private void BtnConfirmAssign_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_waitingForButton) || comboActionSelector.SelectedIndex < 0)
            {
                ExitAssignMode();
                return;
            }
            
            string selectedAction = comboActionSelector.SelectedItem.ToString();
            ButtonAction newAction = CreateButtonActionFromSelection(selectedAction);
            
            if (newAction == null)
            {
                SimpleLogger.Instance?.Info("User cancelled action selection");
                return;
            }
            
            ApplyMapping(_waitingForPlayer, _waitingForButton, newAction);
            Options.Instance.Save();
            
            LoadCurrentMappings();
            
            SimpleLogger.Instance?.Info($"Assigned {selectedAction} to {_waitingForButton} for P{_waitingForPlayer}");
            // Show success message
            
            ExitAssignMode();
        }

        private ButtonAction CreateButtonActionFromSelection(string selection)
        {
            switch (selection)
            {
                case "None":
                    return new ButtonAction();
                case "Mouse Left Click":
                    return new ButtonAction(SpecialAction.LeftMouse);
                case "Mouse Right Click":
                    return new ButtonAction(SpecialAction.RightMouse);
                case "Mouse Middle Click":
                    return new ButtonAction(SpecialAction.MiddleMouse);
                case "Keyboard Key...":
                    using (KeySelectorDialog keyDialog = new KeySelectorDialog())
                    {
                        if (keyDialog.ShowDialog(this.FindForm()) == DialogResult.OK && keyDialog.SelectedKey != Keys.None)
                        {
                            return new ButtonAction(keyDialog.SelectedKey);
                        }
                    }
                    return null;
                default:
                    return null;
            }
        }

        private void ApplyMapping(int playerIndex, string buttonName, ButtonAction action)
        {
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            
            switch (buttonName)
            {
                case "WiiA": mappings.WiiA = action; break;
                case "WiiB": mappings.WiiB = action; break;
                case "WiiOne": mappings.WiiOne = action; break;
                case "WiiTwo": mappings.WiiTwo = action; break;
                case "WiiPlus": mappings.WiiPlus = action; break;
                case "WiiMinus": mappings.WiiMinus = action; break;
                case "WiiUp": mappings.WiiUp = action; break;
                case "WiiDown": mappings.WiiDown = action; break;
                case "WiiLeft": mappings.WiiLeft = action; break;
                case "WiiRight": mappings.WiiRight = action; break;
                case "NunchukC": mappings.NunC = action; break;
                case "NunchukZ": mappings.NunZ = action; break;
                case "NunUp": mappings.NunUp = action; break;
                case "NunDown": mappings.NunDown = action; break;
                case "NunLeft": mappings.NunLeft = action; break;
                case "NunRight": mappings.NunRight = action; break;
            }
        }

        private void BtnCancelAssign_Click(object sender, EventArgs e)
        {
            SimpleLogger.Instance?.Info($"Cancelled assignment for {_waitingForButton}");
            ExitAssignMode();
            // Cancel notification
        }

        // Handle dynamic keyboard mapping (EN/FR: Gérer mapping clavier dynamique)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Only active if we are in assignment mode AND waiting for user input
            if (_isAssignMode && !string.IsNullOrEmpty(_waitingForButton) && comboActionSelector.Visible)
            {
                Keys code = keyData & Keys.KeyCode;

                // Allow navigation keys to control the menu (EN/FR: Permettre touches navigation menu)
                if (code == Keys.Enter || code == Keys.Escape || code == Keys.Up || code == Keys.Down || code == Keys.Tab)
                {
                    return base.ProcessCmdKey(ref msg, keyData);
                }
                
                // Exclude modifiers alone
                if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu || code == Keys.Alt)
                {
                    return base.ProcessCmdKey(ref msg, keyData);
                }

                // Dynamic Assign! (EN/FR: Assignation dynamique !)
                ButtonAction newAction = new ButtonAction(keyData);
                
                ApplyMapping(_waitingForPlayer, _waitingForButton, newAction);
                Options.Instance.Save();
                
                LoadCurrentMappings(); 
                
                SimpleLogger.Instance?.Info($"[Dynamic] Assigned Key {keyData} to {_waitingForButton} for P{_waitingForPlayer}");
                
                ExitAssignMode();
                return true; // Key handled
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ============================================
        // Event Handlers (EN/FR: Gestionnaires d'événements)
        // ============================================

        private void BtnSelectExe_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executables (*.exe)|*.exe";
                ofd.Title = "Select Game/Application Executable";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SetCurrentGame(Path.GetFileName(ofd.FileName), ofd.FileName);
                    // Selected notification
                }
            }
        }

        private void BtnNewFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new ModalInputDialog("New Folder", "Folder Name:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string folderName = dialog.InputValue;
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        try
                        {
                            string remapDir = RemapProfileManager.GetRemapDirectory();
                            string newFolderPath = Path.Combine(remapDir, folderName);
                            Directory.CreateDirectory(newFolderPath);
                            
                            LoadProfileUI();
                            comboBoxSubfolders.SelectedItem = folderName;
                            MessageBox.Show($"Folder '{folderName}' created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to create folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                MessageBox.Show("Please select a profile to delete", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show($"Delete '{selectedProfile}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                    string subfolder = selectedFolder == "(Root)" ? "" : selectedFolder;
                    string profilePath = RemapProfileManager.GetProfilePath(selectedProfile, subfolder);
                    
                    if (File.Exists(profilePath))
                    {
                        File.Delete(profilePath);
                        RefreshProfileList();
                        MessageBox.Show($"Profile '{selectedProfile}' deleted", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnHotkeys_Click(object sender, EventArgs e)
        {
            using (var dialog = new HotkeyEditorDialog(_currentPlayer))
            {
                if (dialog.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    HotkeyManager.SetProfile(_currentPlayer, dialog.HotkeyProfile);
                    SimpleLogger.Instance?.Info($"Hotkeys updated for Player {_currentPlayer}");
                    MessageBox.Show($"Hotkeys saved for Player {_currentPlayer}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ComboBoxSubfolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshProfileList();
        }

        private void ComboBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedProfile))
            {
                string profileNameWithoutExt = selectedProfile.EndsWith(".remap", StringComparison.OrdinalIgnoreCase)
                    ? selectedProfile.Substring(0, selectedProfile.Length - 6)
                    : selectedProfile;
                
                txtProfileName.Text = profileNameWithoutExt;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string profileName = txtProfileName.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter profile name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var profile = new RemapProfile
                {
                    ProfileName = profileName,
                    P1Mappings = new PlayerMappings(),
                    P2Mappings = new PlayerMappings(),
                    P3Mappings = new PlayerMappings(),
                    P4Mappings = new PlayerMappings()
                };
                
                profile.P1Mappings.CopyFrom(Options.Instance.P1Mappings);
                profile.P2Mappings.CopyFrom(Options.Instance.P2Mappings);
                profile.P3Mappings.CopyFrom(Options.Instance.P3Mappings);
                profile.P4Mappings.CopyFrom(Options.Instance.P4Mappings);
                
                // Save Hotkeys (EN/FR: Sauvegarder les hotkeys)
                // Use a copy to avoid reference issues (EN/FR: Utiliser une copie)
                profile.P1Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(1).Hotkeys.Select(h => h.Clone()));
                profile.P2Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(2).Hotkeys.Select(h => h.Clone()));
                profile.P3Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(3).Hotkeys.Select(h => h.Clone()));
                profile.P4Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(4).Hotkeys.Select(h => h.Clone()));
                profile.UseSharedHotkeys = Options.Instance.UseSharedHotkeys;
                
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                string subfolder = selectedFolder == "(Root)" ? null : selectedFolder;
                
                bool success = RemapProfileManager.SaveProfile(profileName, subfolder, profile);
                
                if (success)
                {
                    RefreshProfileList();
                    MessageBox.Show($"Profile '{profileName}' saved", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                MessageBox.Show("Please select a profile", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                string relativePath = selectedFolder == "(Root)" ? selectedProfile : Path.Combine(selectedFolder, selectedProfile);
                
                var profile = RemapProfileManager.LoadProfile(relativePath);
                
                if (profile != null)
                {
                    if (profile.P1Mappings != null) Options.Instance.P1Mappings.CopyFrom(profile.P1Mappings);
                    if (profile.P2Mappings != null) Options.Instance.P2Mappings.CopyFrom(profile.P2Mappings);
                    if (profile.P3Mappings != null) Options.Instance.P3Mappings.CopyFrom(profile.P3Mappings);
                    if (profile.P4Mappings != null) Options.Instance.P4Mappings.CopyFrom(profile.P4Mappings);
                    
                    // Load Hotkeys (EN/FR: Charger les hotkeys)
                    // CRITICAL: Always release current hotkeys, even if profile has none (Global fix)
                    // (EN/FR: Toujours relâcher hotkeys actuelles, même si profil n'en a pas)
                    
                    var p1Lines = profile.P1Hotkeys ?? new List<Hotkey>();
                    var p1Prof = new HotkeyProfile(1);
                    p1Prof.Hotkeys = new List<Hotkey>(p1Lines);
                    HotkeyManager.SetProfile(1, p1Prof);

                    var p2Lines = profile.P2Hotkeys ?? new List<Hotkey>();
                    var p2Prof = new HotkeyProfile(2);
                    p2Prof.Hotkeys = new List<Hotkey>(p2Lines);
                    HotkeyManager.SetProfile(2, p2Prof);
                    
                    var p3Lines = profile.P3Hotkeys ?? new List<Hotkey>();
                    var p3Prof = new HotkeyProfile(3);
                    p3Prof.Hotkeys = new List<Hotkey>(p3Lines);
                    HotkeyManager.SetProfile(3, p3Prof);
                    
                    var p4Lines = profile.P4Hotkeys ?? new List<Hotkey>();
                    var p4Prof = new HotkeyProfile(4);
                    p4Prof.Hotkeys = new List<Hotkey>(p4Lines);
                    HotkeyManager.SetProfile(4, p4Prof);
                    
                    Options.Instance.UseSharedHotkeys = profile.UseSharedHotkeys;
                    
                    // Force clear active modifier states to prevent stuck keys
                    HotkeyManager.ClearActiveState();
                    
                    Options.Instance.Save();
                    Program.LoadRemapProfileHot(relativePath, true);
                    
                    LoadCurrentMappings();
                    UpdateAutoLoadCheckbox();
                    
                    MessageBox.Show($"Profile '{profile.ProfileName}' loaded", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to load profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkAutoLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingCheckbox || !chkAutoLoad.Enabled) return;
            
            try
            {
                if (chkAutoLoad.Checked)
                {
                    if (!string.IsNullOrEmpty(_currentExecutablePath) && !string.IsNullOrEmpty(_currentExecutable))
                    {
                        string currentProfile = Program.GetActiveRemapProfile();
                        if (!string.IsNullOrEmpty(currentProfile))
                        {
                            GameProfileMappingManager.AddMapping(_currentExecutable, currentProfile, _currentExecutablePath);
                            // Auto-load enabled
                            UpdateCurrentGameLabel();
                        }
                        else
                        {
                            _updatingCheckbox = true;
                            chkAutoLoad.Checked = false;
                            _updatingCheckbox = false;
                            MessageBox.Show("No active profile", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(_currentExecutable))
                    {
                        GameProfileMappingManager.RemoveMapping(_currentExecutable);
                        // Auto-load disabled
                        UpdateCurrentGameLabel();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance?.Error($"ChkAutoLoad error: {ex.Message}");
                _updatingCheckbox = false;
            }
        }

        private void ChkEnableGyro_CheckedChanged(object sender, EventArgs e)
        {
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);
            if (mappings != null)
            {
                mappings.EnableGyroAiming = chkEnableGyro.Checked;
                SimpleLogger.Instance?.Info($"Gyro {(chkEnableGyro.Checked ? "enabled" : "disabled")} for P{_currentPlayer}");
                // Gyro toggle notification
            }
        }
    }
}
