using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun.Controls
{
    public partial class GamePadMappingControl : UserControl
    {
        private int _currentPlayer = 1;
        private string _selectedExeName = null;
        private string _selectedExePath = null;
        private bool _isUpdatingAutoLoad = false;
        private ToolTip _toolTip = new ToolTip();
        
        // Dictionary to map ComboBox index/item to GamePadButton
        private List<GamePadButtonItem> _gamePadButtons;
        private List<GamePadAxisItem> _gamePadAxes;
        private List<GamePadMotionModeItem> _motionModes;

        public event EventHandler BackRequested;

        private Label lblIRAxisValue;
        private Label lblNunchukAxisValue;

        public GamePadMappingControl()
        {
            InitializeComponent();
            InitializeModernAxes();
            InitializeDataSources();
        }

        /// <summary>
        /// Sets the currently detected game executable (EN: auto-detected from foreground app).
        /// (FR: Définit l'exécutable du jeu détecté automatiquement en arrière-plan.)
        /// </summary>
        private void InitializeModernAxes()
        {
            // Create labels to replace ComboBoxes in grpAxes
            lblIRAxisValue = CreateModernSelectorLabel(new Point(200, 25), new Size(200, 24));
            lblNunchukAxisValue = CreateModernSelectorLabel(new Point(200, 60), new Size(200, 24));

            grpAxes.Controls.Add(lblIRAxisValue);
            grpAxes.Controls.Add(lblNunchukAxisValue);

            lblIRAxisValue.Click += (s, e) => ShowAxisMenu(lblIRAxisValue, (val) => {
                var mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
                if (mappings != null) mappings.IRSensorAxis = val;
            });

            lblNunchukAxisValue.Click += (s, e) => ShowAxisMenu(lblNunchukAxisValue, (val) => {
                var mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
                if (mappings != null) mappings.NunchukJoystickAxis = val;
            });
        }

        private Label CreateModernSelectorLabel(Point loc, Size size)
        {
            return new Label
            {
                Location = loc,
                Size = size,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.FromArgb(0, 122, 204),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
        }

        private void ShowAxisMenu(Label lbl, Action<GamePadAxis> setter)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (var axis in _gamePadAxes)
            {
                var axisItem = axis;
                ToolStripMenuItem item = new ToolStripMenuItem(axisItem.Name);
                item.Click += (s, e) => {
                    setter(axisItem.Value);
                    lbl.Text = axisItem.Name;
                };
                menu.Items.Add(item);
            }
            menu.Show(lbl, new Point(0, lbl.Height));
        }

        private void UpdateAxisLabel(Label lbl, GamePadAxis axis)
        {
            lbl.Text = GetAxisName(axis);
        }

        public void SetCurrentGame(string exeName)
        {
            if (string.IsNullOrEmpty(exeName))
            {
                UpdateStatusLabels();
                return;
            }
            
            // Pre-fill selected exe if none was manually chosen (EN/FR: Pré-remplir si aucune sélection manuelle)
            if (_selectedExeName == null)
            {
                _selectedExeName = exeName;
                CheckAutoLoadStatus(null);
            }
            else
            {
                UpdateStatusLabels();
            }
        }

        private void UpdateStatusLabels(string profilePathHint = null)
        {
            try
            {
                string statusText = "";
                bool hasLink = false;
                Color colorActive = Color.FromArgb(140, 200, 255); // Light blue
                Color colorInactive = Color.Gray;

                // 1. App Status (detected or manual)
                string currentExe = _selectedExeName ?? Program.LastDetectedGameName;
                string currentExePath = _selectedExePath ?? Program.LastDetectedGamePath;

                if (!string.IsNullOrEmpty(currentExe))
                {
                    string mappedProfile = GameProfileMappingManager.GetGamePadProfileForGame(currentExe, currentExePath);
                    if (!string.IsNullOrEmpty(mappedProfile))
                    {
                        statusText = $"App: {currentExe} -> {Path.GetFileName(mappedProfile)}";
                        hasLink = true;
                    }
                    else
                    {
                        statusText = $"App: {currentExe} (not linked)";
                    }
                }
                else
                {
                    statusText = "App: (none)";
                }

                // 2. Status of currently selected profile in UI
                string currentProfile = profilePathHint;
                if (currentProfile == null)
                {
                    string subfolder = cboSubfolders.SelectedItem?.ToString();
                    if (subfolder == "[Root]") subfolder = "";
                    string profileName = cboProfiles.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(profileName))
                        currentProfile = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);
                }

                if (!string.IsNullOrEmpty(currentProfile))
                {
                    string linkedExe = GameProfileMappingManager.GetExecutableForGamePadProfile(currentProfile);
                    if (!string.IsNullOrEmpty(linkedExe))
                    {
                        statusText += $" | Profile linked to: {linkedExe}";
                        hasLink = true;
                    }
                }

                lblDetectedApp.Text = statusText;
                lblDetectedApp.ForeColor = hasLink ? colorActive : colorInactive;
            }
            catch { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeDataSources(); // Ensure data is loaded when control is shown
        }

        private void InitializeDataSources()
        {
            // Initialize GamePad Buttons list for ComboBoxes
            _gamePadButtons = new List<GamePadButtonItem>
            {
                new GamePadButtonItem("None", GamePadButton.None),
                new GamePadButtonItem("Button A (1)", GamePadButton.Button1),
                new GamePadButtonItem("Button B (2)", GamePadButton.Button2),
                new GamePadButtonItem("Button X (3)", GamePadButton.Button3),
                new GamePadButtonItem("Button Y (4)", GamePadButton.Button4),
                new GamePadButtonItem("Left Bumper (LB)", GamePadButton.Button5),
                new GamePadButtonItem("Right Bumper (RB)", GamePadButton.Button6),
                new GamePadButtonItem("Left Trigger (Button)", GamePadButton.Button7),
                new GamePadButtonItem("Right Trigger (Button)", GamePadButton.Button8),
                new GamePadButtonItem("Back / Select", GamePadButton.Button9),
                new GamePadButtonItem("Start", GamePadButton.Button10),
                new GamePadButtonItem("Left Stick Click", GamePadButton.Button11),
                new GamePadButtonItem("Right Stick Click", GamePadButton.Button12),
                new GamePadButtonItem("D-Pad Up", GamePadButton.DPadUp),
                new GamePadButtonItem("D-Pad Down", GamePadButton.DPadDown),
                new GamePadButtonItem("D-Pad Left", GamePadButton.DPadLeft),
                new GamePadButtonItem("D-Pad Right", GamePadButton.DPadRight)
            };

            // Initialize Axes list
            _gamePadAxes = new List<GamePadAxisItem>
            {
                new GamePadAxisItem("None", GamePadAxis.None),
                new GamePadAxisItem("Left Stick (X/Y)", GamePadAxis.LeftStick),
                new GamePadAxisItem("Right Stick (Rx/Ry)", GamePadAxis.RightStick),
                new GamePadAxisItem("Digital D-Pad (Up/Down/Left/Right)", GamePadAxis.Dpad)
            };

            // Initialize Motion Modes list
            _motionModes = new List<GamePadMotionModeItem>
            {
                new GamePadMotionModeItem("Disabled", GamePadMotionMode.None),
                new GamePadMotionModeItem("Gyroscope -> Right Stick", GamePadMotionMode.GyroToRightStick),
                new GamePadMotionModeItem("Accelerometer -> Right Stick", GamePadMotionMode.AccToRightStick),
                new GamePadMotionModeItem("Gyroscope -> Left Stick", GamePadMotionMode.GyroToLeftStick),
                new GamePadMotionModeItem("Accelerometer -> Left Stick", GamePadMotionMode.AccToLeftStick),
                new GamePadMotionModeItem("Accelerometer -> Throttle", GamePadMotionMode.AccToThrottle),
                new GamePadMotionModeItem("Nunchuk Accel -> Right Stick", GamePadMotionMode.AccNunchukToRightStick),
                new GamePadMotionModeItem("Nunchuk Accel -> Left Stick", GamePadMotionMode.AccNunchukToLeftStick),
                new GamePadMotionModeItem("Nunchuk Accel -> Throttle", GamePadMotionMode.AccNunchukToThrottle)
            };

            // Populate Axe ComboBoxes
            cboIRAxis.DisplayMember = "Name";
            cboIRAxis.ValueMember = "Value";
            cboIRAxis.DataSource = new List<GamePadAxisItem>(_gamePadAxes);

            cboNunchukAxis.DisplayMember = "Name";
            cboNunchukAxis.ValueMember = "Value";
            cboNunchukAxis.DataSource = new List<GamePadAxisItem>(_gamePadAxes);

            // Back button handler
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            // Link profile list events
            cboProfiles.SelectedIndexChanged += (s, e) => UpdateStatusLabels();

            // Initial Load
            LoadCurrentMappings();
        }

        public void LoadData()
        {
            LoadSubfolders();
            LoadCurrentMappings();
            UpdateStatusLabels();
        }


        // =================================================================================================
        // PROFILE MANAGEMENT UI (EN/FR: UI GESTION PROFILS)
        // =================================================================================================

        // =================================================================================================
        // PROFILE MANAGEMENT UI (EN/FR: UI GESTION PROFILS)
        // =================================================================================================





        private void LoadSubfolders()
        {
            cboSubfolders.Items.Clear();
            cboSubfolders.Items.Add("[Root]");
            
            var folders = RemapProfileManager.GetGamePadSubfolders();
            foreach (var folder in folders)
            {
                cboSubfolders.Items.Add(folder);
            }
            cboSubfolders.SelectedIndex = 0; // Default to Root
        }

        private void CboSubfolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            string subfolder = cboSubfolders.SelectedItem.ToString();
            if (subfolder == "[Root]") subfolder = "";
            
            cboProfiles.Items.Clear();
            var profiles = RemapProfileManager.GetGamePadProfilesInFolder(subfolder);
            foreach (var p in profiles)
            {
                cboProfiles.Items.Add(p);
            }

            if (cboProfiles.Items.Count > 0)
            {
                // Try to select default.remap if present
                int defaultIdx = cboProfiles.FindStringExact("default.remap");
                cboProfiles.SelectedIndex = defaultIdx != -1 ? defaultIdx : 0;
            }
            
            UpdateStatusLabels();
        }


        private void BtnLoadProfile_Click(object sender, EventArgs e)
        {
            if (cboProfiles.SelectedItem == null && string.IsNullOrEmpty(cboProfiles.Text)) return;
            
            string subfolder = cboSubfolders.SelectedItem.ToString();
            if (subfolder == "[Root]") subfolder = "";
            
            string profileName = cboProfiles.SelectedItem != null ? cboProfiles.SelectedItem.ToString() : cboProfiles.Text;
            if (!profileName.EndsWith(".remap")) profileName += ".remap"; // Handle typed name
            
            string relativePath = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);

            try
            {
                var profile = RemapProfileManager.LoadGamePadProfile(relativePath);
                if (profile != null)
                {
                    // Apply to current options memory
                    // (EN/FR: Appliquer à la mémoire des options actuelle)
                    Options.Instance.P1GamePadMappings = profile.P1Mappings;
                    Options.Instance.P2GamePadMappings = profile.P2Mappings;
                    Options.Instance.P3GamePadMappings = profile.P3Mappings;
                    Options.Instance.P4GamePadMappings = profile.P4Mappings;
                    
                    // Refresh UI
                    LoadCurrentMappings();
                    
                    // Verify Auto-Load status
                    CheckAutoLoadStatus(relativePath);
                    
                    // Save to config (EN/FR: Sauvegarder dans la configuration)
                    Options.Instance.Save();
                    
                    UpdateStatusLabels(relativePath);
                    MessageBox.Show(this.FindForm(), $"Loaded Profile: {profileName}", "Profile Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.FindForm(), $"Error loading profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            string subfolder = cboSubfolders.SelectedItem?.ToString();
            if (subfolder == "[Root]") subfolder = "";

            // Feature: Silent Save for Root (EN/FR: Sauvegarde silencieuse pour Root)
            // If saving to Root, skip dialog and force "default.remap"
            if (string.IsNullOrEmpty(subfolder))
            {
                 GamePadProfile defaultProfile = new GamePadProfile
                 {
                     ProfileName = "Default",
                     P1Mappings = Options.Instance.P1GamePadMappings.Clone(),
                     P2Mappings = Options.Instance.P2GamePadMappings.Clone(),
                     P3Mappings = Options.Instance.P3GamePadMappings.Clone(),
                     P4Mappings = Options.Instance.P4GamePadMappings.Clone()
                 };

                 if (RemapProfileManager.SaveGamePadProfile("default", "", defaultProfile))
                 {
                     MessageBox.Show(this.FindForm(), "Default Profile saved successfully to Root.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                     CboSubfolders_SelectedIndexChanged(null, null); // Refresh
                 }
                 else
                 {
                     MessageBox.Show(this.FindForm(), "Failed to save default profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 }
                 return;
            }

            // Normal flow for subfolders (EN/FR: Flux normal pour sous-dossiers)
            string defaultName = cboProfiles.Text;
            if (string.IsNullOrEmpty(defaultName) && cboProfiles.SelectedItem != null)
                defaultName = cboProfiles.SelectedItem.ToString();
                
            using (var input = new ModalInputDialog("Save Profile", "Enter profile name:", defaultName))
            {
                if (input.ShowDialog(this.FindForm()) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.InputValue))
                {
                    // Create object from current Options
                    GamePadProfile profile = new GamePadProfile
                    {
                        ProfileName = input.InputValue,
                        P1Mappings = Options.Instance.P1GamePadMappings.Clone(),
                        P2Mappings = Options.Instance.P2GamePadMappings.Clone(),
                        P3Mappings = Options.Instance.P3GamePadMappings.Clone(),
                        P4Mappings = Options.Instance.P4GamePadMappings.Clone()
                    };
                    
                    if (RemapProfileManager.SaveGamePadProfile(input.InputValue, subfolder, profile))
                    {
                        CboSubfolders_SelectedIndexChanged(null, null); // Refresh list
                        
                        string savedName = input.InputValue.EndsWith(".remap") ? input.InputValue : input.InputValue + ".remap";
                        int idx = cboProfiles.FindStringExact(savedName);
                        if (idx != -1) cboProfiles.SelectedIndex = idx;
                        else cboProfiles.Text = savedName;
                        
                        MessageBox.Show(this.FindForm(), "Profile Saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(this.FindForm(), "Failed to save profile. See logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnNewFolder_Click(object sender, EventArgs e)
        {
             using (var input = new ModalInputDialog("New Folder", "Enter folder name:", ""))
             {
                 if (input.ShowDialog(this.FindForm()) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.InputValue))
                 {
                     string path = Path.Combine(RemapProfileManager.GetGamePadRemapDirectory(), input.InputValue);
                     if (!Directory.Exists(path))
                     {
                         Directory.CreateDirectory(path);
                         LoadSubfolders();
                         // Select new folder
                         int idx = cboSubfolders.FindStringExact(input.InputValue);
                         if (idx != -1) cboSubfolders.SelectedIndex = idx;
                     }
                 }
             }
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            string subfolder = cboSubfolders.SelectedItem?.ToString();
            if (subfolder == "[Root]") subfolder = "";
            
            string path = RemapProfileManager.GetGamePadRemapDirectory();
            if (!string.IsNullOrEmpty(subfolder))
                path = Path.Combine(path, subfolder);
                
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start(path);
            }
            else
            {
                MessageBox.Show(this.FindForm(), $"Directory not found: {path}");
            }
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            string profileName = cboProfiles.Text;
            if (string.IsNullOrEmpty(profileName) && cboProfiles.SelectedItem != null)
                profileName = cboProfiles.SelectedItem.ToString();

            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show(this.FindForm(), "Please select a profile to delete.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!profileName.EndsWith(".remap")) profileName += ".remap";

            var result = MessageBox.Show(this.FindForm(), $"Delete GamePad profile '{profileName}'?\nThis will also remove it from any linked games.", 
                                         "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string subfolder = cboSubfolders.SelectedItem?.ToString();
                    if (subfolder == "[Root]") subfolder = "";
                    
                    // Construct path
                    string remapDir = RemapProfileManager.GetGamePadRemapDirectory();
                    string fullPath = string.IsNullOrEmpty(subfolder) 
                        ? Path.Combine(remapDir, profileName) 
                        : Path.Combine(remapDir, subfolder, profileName);

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        
                        // Clean up JSON links (EN/FR: Nettoyer liens JSON)
                        string relativePath = string.IsNullOrEmpty(subfolder) 
                            ? profileName 
                            : Path.Combine(subfolder, profileName);
                            
                        GameProfileMappingManager.RemoveGamePadProfileLink(relativePath);
                        
                        MessageBox.Show(this.FindForm(), "Profile deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Refresh
                        CboSubfolders_SelectedIndexChanged(null, null);
                    }
                    else
                    {
                        MessageBox.Show(this.FindForm(), $"File not found: {fullPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this.FindForm(), $"Error deleting profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SimpleLogger.Instance.Error($"Failed to delete GamePad profile: {ex.Message}");
                }
            }
        }

        private void CheckAutoLoadStatus(string profilePath)
        {
            string lastGame = _selectedExeName ?? Program.LastDetectedGameName;
            string lastGamePath = _selectedExePath ?? Program.LastDetectedGamePath;
            
            if (string.IsNullOrEmpty(lastGame))
            {
                chkAutoLoad.Enabled = false;
                chkAutoLoad.Text = "Auto-Load";
                _toolTip.SetToolTip(chkAutoLoad, "No game detected or selected");
                return;
            }

            chkAutoLoad.Enabled = true;
            chkAutoLoad.Text = "Auto-Load";
            _toolTip.SetToolTip(chkAutoLoad, $"Auto-Load for {lastGame}");

            if (profilePath == null) // Provide fallback via selected combos
            {
                string subfolder = cboSubfolders.SelectedItem?.ToString();
                if (subfolder == "[Root]") subfolder = "";
                string profileName = cboProfiles.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(profileName))
                    profilePath = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);
            }

            // Check if currently linked
            string linkedProfile = GameProfileMappingManager.GetGamePadProfileForGame(lastGame, lastGamePath);
            
            // Normalize for comparison
            string currentProfileRelPath = profilePath?.Replace('\\', '/');
            string linkedProfileRelPath = linkedProfile?.Replace('\\', '/');

            // Avoid NPE
            _isUpdatingAutoLoad = false;
            
            UpdateStatusLabels(profilePath);
        }

        private void ChkAutoLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingAutoLoad) return;
            if (!chkAutoLoad.Enabled || cboProfiles.SelectedItem == null) return;
            
            string lastGame = _selectedExeName ?? Program.LastDetectedGameName;
            string lastGamePath = _selectedExePath ?? Program.LastDetectedGamePath;
            if (string.IsNullOrEmpty(lastGame)) return;

            string subfolder = cboSubfolders.SelectedItem.ToString();
            if (subfolder == "[Root]") subfolder = "";
            string profileName = cboProfiles.SelectedItem.ToString();
            string relativePath = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);

            if (chkAutoLoad.Checked)
            {
                GameProfileMappingManager.AddMapping(lastGame, null, lastGamePath, relativePath); // Only update GamePad link
            }
            else
            {
                GameProfileMappingManager.RemoveGamePadProfileLinkForExecutable(lastGame);
            }
            UpdateStatusLabels(relativePath);
        }

        private void BtnSelectExe_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executables (*.exe)|*.exe";
                ofd.Title = "Select Game/Application Executable for GamePad Profile";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _selectedExeName = Path.GetFileName(ofd.FileName);
                    _selectedExePath = ofd.FileName;
                    CheckAutoLoadStatus(null);
                    
                    if (!chkAutoLoad.Checked) {
                        try { chkAutoLoad.Checked = true; } catch { } // Optional auto-check
                    }
                }
            }
        }

        private void TabControlPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlPlayers == null) return;
            if (_gamePadButtons == null) return;
            _currentPlayer = tabControlPlayers.SelectedIndex + 1;
            LoadCurrentMappings();
        }

        private void LoadCurrentMappings()
        {
            // Designer Mode Support (EN/FR: Support Mode Designer)
            if (this.DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
            {
                // Create dummy controls for visual preview (EN/FR: Créer contrôles factices pour aperçu visuel)
                flowLayoutPanelButtons.Controls.Clear();
                AddSectionHeader("Designer Preview Header");
                AddMappingRow("Designer1", "Designer Button 1", GamePadButton.Button1, (val) => {}, "", (val) => {}, new ButtonAction(), (val) => {});
                AddMappingRow("Designer2", "Designer Button 2", GamePadButton.Button2, (val) => {}, "", (val) => {}, new ButtonAction(), (val) => {});
                return;
            }

            GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
            if (mappings == null) return;

            // Load Axes (Modern Labels)
            UpdateAxisLabel(lblIRAxisValue, mappings.IRSensorAxis);
            UpdateAxisLabel(lblNunchukAxisValue, mappings.NunchukJoystickAxis);

            // Hide old ComboBoxes (EN/FR: Masquer anciennes ComboBox)
            cboIRAxis.Visible = false;
            cboNunchukAxis.Visible = false;

            // Load IR Calibration values (EN/FR: Charger les valeurs de calibrage IR)
            numLinearity.Value = Math.Max(numLinearity.Minimum, Math.Min(numLinearity.Maximum, (decimal)mappings.IRLinearity));
            numOverscan.Value = Math.Max(numOverscan.Minimum, Math.Min(numOverscan.Maximum, (decimal)mappings.IROverscan));


            // ... rest of the method ...

            // EN/FR: Clear existing rows before header to avoid duplicates if re-called
            // (but header is added first, so we clear before everything)
            flowLayoutPanelButtons.Controls.Clear();
            flowLayoutPanelButtons.SuspendLayout();

            AddSectionHeader("Output Mode");
            AddCheckBoxRow("Use XInput (ViGEmBus)", mappings.UseXInput, (val) => mappings.UseXInput = val);
            AddCheckBoxRow("IR as Mouse in Hybrid Mode", mappings.IRHybridAsMouse, (val) => mappings.IRHybridAsMouse = val);
            AddCheckBoxRow("Hybrid Toggle (EN/FR: Bascule Hybride)", mappings.HybridToggle, (val) => mappings.HybridToggle = val);
            AddNumericRow("IR Anti-Deadzone (%)", (decimal)(mappings.IRAntiDeadzone * 100f), (val) => mappings.IRAntiDeadzone = (float)val / 100f);


            // Load Buttons (Re-create controls to ensure fresh state)

            AddSectionHeader("Wiimote Buttons");
            AddMappingRow("WiiA", "A Button", mappings.WiiA, (val) => mappings.WiiA = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiAHybrid, (val) => mappings.WiiAHybrid = val);
            AddMappingRow("WiiB", "B Button", mappings.WiiB, (val) => mappings.WiiB = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiBHybrid, (val) => mappings.WiiBHybrid = val);
            AddMappingRow("Wii1", "1 Button", mappings.Wii1, (val) => mappings.Wii1 = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.Wii1Hybrid, (val) => mappings.Wii1Hybrid = val);
            AddMappingRow("Wii2", "2 Button", mappings.Wii2, (val) => mappings.Wii2 = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.Wii2Hybrid, (val) => mappings.Wii2Hybrid = val);
            AddMappingRow("WiiPlus", "Plus (+)", mappings.WiiPlus, (val) => mappings.WiiPlus = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiPlusHybrid, (val) => mappings.WiiPlusHybrid = val);
            AddMappingRow("WiiMinus", "Minus (-)", mappings.WiiMinus, (val) => mappings.WiiMinus = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiMinusHybrid, (val) => mappings.WiiMinusHybrid = val);
            AddMappingRow("WiiUp", "D-Pad Up", mappings.WiiUp, (val) => mappings.WiiUp = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiUpHybrid, (val) => mappings.WiiUpHybrid = val);
            AddMappingRow("WiiDown", "D-Pad Down", mappings.WiiDown, (val) => mappings.WiiDown = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiDownHybrid, (val) => mappings.WiiDownHybrid = val);
            AddMappingRow("WiiLeft", "D-Pad Left", mappings.WiiLeft, (val) => mappings.WiiLeft = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiLeftHybrid, (val) => mappings.WiiLeftHybrid = val);
            AddMappingRow("WiiRight", "D-Pad Right", mappings.WiiRight, (val) => mappings.WiiRight = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.WiiRightHybrid, (val) => mappings.WiiRightHybrid = val);

            AddSectionHeader("Nunchuk Buttons");
            AddMappingRow("NunchukC", "C Button", mappings.NunchukC, (val) => mappings.NunchukC = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukCHybrid, (val) => mappings.NunchukCHybrid = val);
            AddMappingRow("NunchukZ", "Z Button", mappings.NunchukZ, (val) => mappings.NunchukZ = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukZHybrid, (val) => mappings.NunchukZHybrid = val);

            AddSectionHeader("Wiimote Motion (Gestures) -Experimental-", true);
            AddGesturalMappingRow("Move Up", mappings.AccelWiimoteUp, (val) => mappings.AccelWiimoteUp = val, mappings.AccelWiimoteUpHybrid, (val) => mappings.AccelWiimoteUpHybrid = val, (axis) => { mappings.AccelWiimoteDown.SetAxisIfNone(axis); mappings.AccelWiimoteLeft.SetAxisIfNone(axis); mappings.AccelWiimoteRight.SetAxisIfNone(axis); LoadCurrentMappings(); });
            AddGesturalMappingRow("Move Down", mappings.AccelWiimoteDown, (val) => mappings.AccelWiimoteDown = val, mappings.AccelWiimoteDownHybrid, (val) => mappings.AccelWiimoteDownHybrid = val);
            AddGesturalMappingRow("Move Left", mappings.AccelWiimoteLeft, (val) => mappings.AccelWiimoteLeft = val, mappings.AccelWiimoteLeftHybrid, (val) => mappings.AccelWiimoteLeftHybrid = val);
            AddGesturalMappingRow("Move Right", mappings.AccelWiimoteRight, (val) => mappings.AccelWiimoteRight = val, mappings.AccelWiimoteRightHybrid, (val) => mappings.AccelWiimoteRightHybrid = val);
            AddGesturalMappingRow("Shake", mappings.AccelWiimoteShake, (val) => mappings.AccelWiimoteShake = val, mappings.AccelWiimoteShakeHybrid, (val) => mappings.AccelWiimoteShakeHybrid = val);

            AddSectionHeader("Nunchuk Motion (Gestures) -Experimental-", true);
            AddGesturalMappingRow("Move Up", mappings.AccelNunchukUp, (val) => mappings.AccelNunchukUp = val, mappings.AccelNunchukUpHybrid, (val) => mappings.AccelNunchukUpHybrid = val, (axis) => { mappings.AccelNunchukDown.SetAxisIfNone(axis); mappings.AccelNunchukLeft.SetAxisIfNone(axis); mappings.AccelNunchukRight.SetAxisIfNone(axis); LoadCurrentMappings(); });
            AddGesturalMappingRow("Move Down", mappings.AccelNunchukDown, (val) => mappings.AccelNunchukDown = val, mappings.AccelNunchukDownHybrid, (val) => mappings.AccelNunchukDownHybrid = val);
            AddGesturalMappingRow("Move Left", mappings.AccelNunchukLeft, (val) => mappings.AccelNunchukLeft = val, mappings.AccelNunchukLeftHybrid, (val) => mappings.AccelNunchukLeftHybrid = val);
            AddGesturalMappingRow("Move Right", mappings.AccelNunchukRight, (val) => mappings.AccelNunchukRight = val, mappings.AccelNunchukRightHybrid, (val) => mappings.AccelNunchukRightHybrid = val);
            AddGesturalMappingRow("Shake", mappings.AccelNunchukShake, (val) => mappings.AccelNunchukShake = val, mappings.AccelNunchukShakeHybrid, (val) => mappings.AccelNunchukShakeHybrid = val);

            AddSectionHeader("Motion Plus (Gestures) -Experimental-", true);
            AddGesturalMappingRow("Tilt Up", mappings.GyroMotionPlusUp, (val) => mappings.GyroMotionPlusUp = val, mappings.GyroMotionPlusUpHybrid, (val) => mappings.GyroMotionPlusUpHybrid = val, (axis) => { mappings.GyroMotionPlusDown.SetAxisIfNone(axis); mappings.GyroMotionPlusLeft.SetAxisIfNone(axis); mappings.GyroMotionPlusRight.SetAxisIfNone(axis); LoadCurrentMappings(); });
            AddGesturalMappingRow("Tilt Down", mappings.GyroMotionPlusDown, (val) => mappings.GyroMotionPlusDown = val, mappings.GyroMotionPlusDownHybrid, (val) => mappings.GyroMotionPlusDownHybrid = val);
            AddGesturalMappingRow("Tilt Left", mappings.GyroMotionPlusLeft, (val) => mappings.GyroMotionPlusLeft = val, mappings.GyroMotionPlusLeftHybrid, (val) => mappings.GyroMotionPlusLeftHybrid = val);
            AddGesturalMappingRow("Tilt Right", mappings.GyroMotionPlusRight, (val) => mappings.GyroMotionPlusRight = val, mappings.GyroMotionPlusRightHybrid, (val) => mappings.GyroMotionPlusRightHybrid = val);
            AddGesturalMappingRow("Roll Left", mappings.GyroMotionPlusRollLeft, (val) => mappings.GyroMotionPlusRollLeft = val, mappings.GyroMotionPlusRollLeftHybrid, (val) => mappings.GyroMotionPlusRollLeftHybrid = val);
            AddGesturalMappingRow("Roll Right", mappings.GyroMotionPlusRollRight, (val) => mappings.GyroMotionPlusRollRight = val, mappings.GyroMotionPlusRollRightHybrid, (val) => mappings.GyroMotionPlusRollRightHybrid = val);
            
            AddSectionHeader("Motion Passthrough (Safe)");
            AddNumericRow("Wiimote Accel", (decimal)mappings.AccelWiimoteSensitivity, (val) => mappings.AccelWiimoteSensitivity = (float)val);
            AddNumericRow("Wiimote Deadzone (G)", (decimal)mappings.AccelWiimoteDeadzone, (val) => mappings.AccelWiimoteDeadzone = (float)val);
            AddNumericRow("Nunchuk Accel", (decimal)mappings.AccelNunchukSensitivity, (val) => mappings.AccelNunchukSensitivity = (float)val);
            AddNumericRow("Nunchuk Deadzone (G)", (decimal)mappings.AccelNunchukDeadzone, (val) => mappings.AccelNunchukDeadzone = (float)val);
            AddNumericRow("Wii Shake (G)", (decimal)mappings.AccelWiimoteShakeDeadzone, (val) => mappings.AccelWiimoteShakeDeadzone = (float)val);
            AddNumericRow("Nun Shake (G)", (decimal)mappings.AccelNunchukShakeDeadzone, (val) => mappings.AccelNunchukShakeDeadzone = (float)val);
            AddNumericRow("Shake Count", (decimal)mappings.ShakeOscillationRequired, (val) => mappings.ShakeOscillationRequired = (int)val);
            AddNumericRow("Gyro Sensitivity:", (decimal)mappings.GyroSensitivity, (val) => mappings.GyroSensitivity = (float)val);
            AddNumericRow("Gyro Deadzone:", (decimal)mappings.GyroDeadzone, (val) => mappings.GyroDeadzone = (float)val);

            flowLayoutPanelButtons.ResumeLayout();
        }

        private void Open3DVisualizer()
        {
            try {
                var formType = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == "GyroVisualizerForm");
                
                if (formType != null)
                {
                    Form form = (Form)Activator.CreateInstance(formType);
                    form.Show();
                }
                else 
                {
                    MessageBox.Show(this.FindForm(), "GyroVisualizerForm not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch (Exception ex) {
                MessageBox.Show(this.FindForm(), "Error opening visualizer: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private string GetTargetName(GamePadMotionAction action)
        {
            if (action.TargetType == GamePadMotionTargetType.Axis) return GetAxisName(action.TargetAxis);
            if (action.TargetType == GamePadMotionTargetType.Button) return GetGamePadButtonName(action.TargetButton);
            return "None";
        }

        private string GetAxisName(GamePadAxis axis)
        {
            var item = _gamePadAxes.FirstOrDefault(a => a.Value == axis);
            return item != null ? item.Name : "None";
        }

        private void SetAxisSelection(ComboBox cbo, GamePadAxis axis)
        {
            foreach (GamePadAxisItem item in cbo.Items)
            {
                if (item.Value == axis)
                {
                    cbo.SelectedItem = item;
                    return;
                }
            }
            if (cbo.Items.Count > 0)
                cbo.SelectedIndex = 0;
        }

        private void AddSectionHeader(string title, bool showVisualizer = false)
        {
            FlowLayoutPanel headerPanel = new FlowLayoutPanel();
            headerPanel.AutoSize = true;
            headerPanel.FlowDirection = FlowDirection.LeftToRight;
            headerPanel.Margin = new Padding(10, 25, 10, 5); // EN/FR: Augmenté (10 -> 25) pour aérer
            headerPanel.WrapContents = false;

            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Underline);
            lbl.ForeColor = Color.FromArgb(0, 122, 204); // Accent color
            lbl.AutoSize = true;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            headerPanel.Controls.Add(lbl);

            if (showVisualizer)
            {
                Button btnViz = new Button();
                btnViz.Text = "3D";
                btnViz.Size = new Size(35, 24);
                btnViz.FlatStyle = FlatStyle.Flat;
                btnViz.FlatAppearance.BorderSize = 0;
                btnViz.Font = new Font("Segoe UI", 8.0F, FontStyle.Bold);
                btnViz.ForeColor = Color.Gold;
                btnViz.Cursor = Cursors.Hand;
                btnViz.Margin = new Padding(5, 0, 0, 0);
                btnViz.Click += (s, e) => Open3DVisualizer();
                
                // Tooltip
                ToolTip tt = new ToolTip();
                tt.SetToolTip(btnViz, "Open 3D Visualizer (Calibration tool)");
                
                headerPanel.Controls.Add(btnViz);
            }

            flowLayoutPanelButtons.Controls.Add(headerPanel);
            flowLayoutPanelButtons.SetFlowBreak(headerPanel, true); // Force new line after
        }


        private void AddMappingRow(string buttonId, string labelText, GamePadButton currentValue, Action<GamePadButton> setter, 
                                   string currentHybridTrigger, Action<string> triggerSetter,
                                   ButtonAction currentHybridAction, Action<ButtonAction> actionSetter)
        {
            Panel row = new Panel();
            row.Size = new Size(680, 28);
            row.Margin = new Padding(0, 1, 0, 1);
            
            Label lbl = new Label();
            lbl.Text = labelText + ":";
            lbl.Font = new Font("Segoe UI", 9.5f);
            lbl.ForeColor = Color.White;
            lbl.AutoSize = false;
            lbl.Size = new Size(110, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(5, 2);
            
            Label lblMapped = new Label();
            lblMapped.Text = GetGamePadButtonName(currentValue);
            lblMapped.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            lblMapped.ForeColor = Color.FromArgb(0, 122, 204); // Accent color
            lblMapped.BackColor = Color.FromArgb(45, 45, 45);
            lblMapped.Size = new Size(160, 24);
            lblMapped.Location = new Point(120, 2);
            lblMapped.TextAlign = ContentAlignment.MiddleCenter;
            lblMapped.Cursor = Cursors.Hand;
            lblMapped.Click += (s, e) => {
                ContextMenuStrip menu = new ContextMenuStrip();
                foreach (var item in _gamePadButtons)
                {
                    var btnItem = item;
                    ToolStripMenuItem menuItem = new ToolStripMenuItem(btnItem.Name);
                    menuItem.Click += (s2, e2) => {
                        setter(btnItem.Value);
                        lblMapped.Text = btnItem.Name;
                    };
                    menu.Items.Add(menuItem);
                }
                menu.Show(lblMapped, new Point(0, lblMapped.Height));
            };

            // Hybrid Trigger Checkbox
            CheckBox chkTrigger = new CheckBox();
            chkTrigger.Text = "Set as Hybrid Trigger";
            chkTrigger.ForeColor = (currentHybridTrigger == buttonId) ? Color.White : Color.Gray;
            chkTrigger.BackColor = (currentHybridTrigger == buttonId) ? Color.FromArgb(180, 0, 0) : Color.Transparent;
            chkTrigger.Font = new Font("Segoe UI", 8.5f, (currentHybridTrigger == buttonId) ? FontStyle.Bold : FontStyle.Regular);
            chkTrigger.AutoSize = false;
            chkTrigger.Size = new Size(150, 25);
            chkTrigger.Location = new Point(290, 2);
            chkTrigger.Padding = new Padding(5, 0, 0, 0);
            chkTrigger.Checked = (currentHybridTrigger == buttonId);
            chkTrigger.CheckedChanged += (s, e) => { 
                if (chkTrigger.Checked && currentHybridTrigger != buttonId) { triggerSetter(buttonId); } 
                else if (!chkTrigger.Checked && currentHybridTrigger == buttonId) { triggerSetter(""); }
            };
            
            // Hybrid Action Button
            Button btnAction = new Button();
            btnAction.Text = "Hybrid: " + GetActionDisplayText(currentHybridAction);
            btnAction.ForeColor = Color.White;
            btnAction.BackColor = Color.FromArgb(60, 60, 60);
            btnAction.FlatStyle = FlatStyle.Flat;
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Size = new Size(200, 24);
            btnAction.Location = new Point(450, 2);
            btnAction.Click += (s, e) => {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("None", null, (s2, e2) => { actionSetter(new ButtonAction()); btnAction.Text = "Hybrid: None"; });
                menu.Items.Add("Mouse Left Click", null, (s2, e2) => { actionSetter(new ButtonAction(SpecialAction.LeftMouse)); btnAction.Text = "Hybrid: Mouse Left Click"; });
                menu.Items.Add("Mouse Right Click", null, (s2, e2) => { actionSetter(new ButtonAction(SpecialAction.RightMouse)); btnAction.Text = "Hybrid: Mouse Right Click"; });
                menu.Items.Add("Mouse Middle Click", null, (s2, e2) => { actionSetter(new ButtonAction(SpecialAction.MiddleMouse)); btnAction.Text = "Hybrid: Mouse Middle Click"; });
                menu.Items.Add("-");
                menu.Items.Add("Keyboard Key...", null, (s2, e2) => {
                    using (var keyDialog = new KeySelectorDialog())
                    {
                        if (keyDialog.ShowDialog(this.FindForm()) == DialogResult.OK && keyDialog.SelectedKey != Keys.None)
                        {
                            actionSetter(new ButtonAction(keyDialog.SelectedKey));
                            btnAction.Text = "Hybrid: Key " + keyDialog.SelectedKey.ToString();
                        }
                    }
                });
                menu.Show(btnAction, new Point(0, btnAction.Height));
            };
            
            row.Controls.Add(lbl);
            row.Controls.Add(lblMapped);
            row.Controls.Add(chkTrigger);
            row.Controls.Add(btnAction);
            
            flowLayoutPanelButtons.Controls.Add(row);
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
            if (action.Key != Keys.None) return $"Key {action.Key}";
            return "None";
        }

        private string GetGamePadButtonName(GamePadButton button)
        {
            var item = _gamePadButtons.FirstOrDefault(b => b.Value == button);
            return item != null ? item.Name : "None";
        }

        private void AddGesturalMappingRow(string labelText, GamePadMotionAction currentValue, Action<GamePadMotionAction> setter,
                                           ButtonAction hybridAction, Action<ButtonAction> hybridSetter, Action<GamePadAxis> onAxisAutoMap = null)

        {
            if (currentValue == null)
            {
                currentValue = new GamePadMotionAction();
                setter(currentValue);
            }

            Panel row = new Panel();
            row.Size = new Size(680, 28);
            row.Margin = new Padding(0, 1, 0, 1);

            Label lbl = new Label();
            lbl.Text = labelText + ":";
            lbl.Font = new Font("Segoe UI", 9.5f);
            lbl.ForeColor = Color.White;
            lbl.AutoSize = false;
            lbl.Size = new Size(110, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(5, 2);

            Label lblType = new Label();
            lblType.Text = currentValue.TargetType == GamePadMotionTargetType.None ? "None" : (currentValue.TargetType == GamePadMotionTargetType.Axis ? "To Axis" : "To Button");
            lblType.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            lblType.ForeColor = Color.White;
            lblType.BackColor = Color.FromArgb(45, 45, 45);
            lblType.Size = new Size(100, 24);
            lblType.Location = new Point(120, 2);
            lblType.TextAlign = ContentAlignment.MiddleCenter;
            lblType.Cursor = Cursors.Hand;

            Label lblTarget = new Label();
            lblTarget.Text = GetTargetName(currentValue);
            lblTarget.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            lblTarget.ForeColor = Color.FromArgb(0, 122, 204);
            lblTarget.BackColor = Color.FromArgb(45, 45, 45);
            lblTarget.Size = new Size(160, 24);
            lblTarget.Location = new Point(230, 2);
            lblTarget.TextAlign = ContentAlignment.MiddleCenter;
            lblTarget.Cursor = Cursors.Hand;
            lblTarget.Enabled = (currentValue.TargetType != GamePadMotionTargetType.None);

            lblType.Click += (s, e) => {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("None", null, (s2, e2) => {
                    currentValue.TargetType = GamePadMotionTargetType.None;
                    lblType.Text = "None";
                    lblTarget.Text = "None";
                    lblTarget.Enabled = false;
                });
                menu.Items.Add("To Axis", null, (s2, e2) => {
                    currentValue.TargetType = GamePadMotionTargetType.Axis;
                    lblType.Text = "To Axis";
                    lblTarget.Text = GetAxisName(currentValue.TargetAxis);
                    lblTarget.Enabled = true;
                });
                menu.Items.Add("To Button", null, (s2, e2) => {
                    currentValue.TargetType = GamePadMotionTargetType.Button;
                    lblType.Text = "To Button";
                    lblTarget.Text = GetGamePadButtonName(currentValue.TargetButton);
                    lblTarget.Enabled = true;
                });
                menu.Show(lblType, new Point(0, lblType.Height));
            };

            lblTarget.Click += (s, e) => {
                if (currentValue.TargetType == GamePadMotionTargetType.None) return;

                ContextMenuStrip menu = new ContextMenuStrip();
                if (currentValue.TargetType == GamePadMotionTargetType.Axis)
                {
                    foreach (var axis in _gamePadAxes)
                    {
                        var axisItem = axis;
                        ToolStripMenuItem item = new ToolStripMenuItem(axisItem.Name);
                        item.Click += (s2, e2) => {
                            currentValue.TargetAxis = axisItem.Value;
                            lblTarget.Text = axisItem.Name;
                            // Auto-mapping: if an axis is selected, apply to siblings in group
                            if (onAxisAutoMap != null) onAxisAutoMap(axisItem.Value);
                        };

                        menu.Items.Add(item);
                    }
                }
                else
                {
                    foreach (var btn in _gamePadButtons)
                    {
                        var btnItem = btn;
                        ToolStripMenuItem item = new ToolStripMenuItem(btnItem.Name);
                        item.Click += (s2, e2) => {
                            currentValue.TargetButton = btnItem.Value;
                            lblTarget.Text = btnItem.Name;
                        };
                        menu.Items.Add(item);
                    }
                }
                menu.Show(lblTarget, new Point(0, lblTarget.Height));
            };

            // Hybrid Action Button
            Button btnAction = new Button();
            btnAction.Text = "Hybrid: " + GetActionDisplayText(hybridAction);
            btnAction.ForeColor = Color.White;
            btnAction.BackColor = Color.FromArgb(60, 60, 60);
            btnAction.FlatStyle = FlatStyle.Flat;
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Size = new Size(200, 24);
            btnAction.Location = new Point(450, 2);
            btnAction.Click += (s, e) => {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("None", null, (s2, e2) => { hybridSetter(new ButtonAction()); btnAction.Text = "Hybrid: None"; });
                menu.Items.Add("Mouse Left Click", null, (s2, e2) => { hybridSetter(new ButtonAction(SpecialAction.LeftMouse)); btnAction.Text = "Hybrid: Mouse Left Click"; });
                menu.Items.Add("Mouse Right Click", null, (s2, e2) => { hybridSetter(new ButtonAction(SpecialAction.RightMouse)); btnAction.Text = "Hybrid: Mouse Right Click"; });
                menu.Items.Add("Mouse Middle Click", null, (s2, e2) => { hybridSetter(new ButtonAction(SpecialAction.MiddleMouse)); btnAction.Text = "Hybrid: Mouse Middle Click"; });
                menu.Items.Add("-");
                menu.Items.Add("Keyboard Key...", null, (s2, e2) => {
                    using (var keyDialog = new KeySelectorDialog())
                    {
                        if (keyDialog.ShowDialog(this.FindForm()) == DialogResult.OK && keyDialog.SelectedKey != Keys.None)
                        {
                            hybridSetter(new ButtonAction(keyDialog.SelectedKey));
                            btnAction.Text = "Hybrid: Key " + keyDialog.SelectedKey.ToString();
                        }
                    }
                });
                menu.Show(btnAction, new Point(0, btnAction.Height));
            };

            row.Controls.Add(lbl);
            row.Controls.Add(lblType);
            row.Controls.Add(lblTarget);
            row.Controls.Add(btnAction);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        private void SetButtonSelectionTarget(ComboBox cbo, GamePadButton button)
        {
            if (cbo.Items.Count == 0) return;
            foreach (GamePadButtonItem item in cbo.Items)
            {
                if (item.Value == button) { cbo.SelectedItem = item; return; }
            }
            cbo.SelectedIndex = 0;
        }

        private void SetAxisSelectionTarget(ComboBox cbo, GamePadAxis axis)
        {
            if (cbo.Items.Count == 0) return;
            foreach (GamePadAxisItem item in cbo.Items)
            {
                if (item.Value == axis) { cbo.SelectedItem = item; return; }
            }
            cbo.SelectedIndex = 0;
        }

        private void AddNumericRow(string labelText, decimal currentValue, Action<decimal> setter)
        {
            Panel row = new Panel();
            row.Size = new Size(400, 28);
            row.Margin = new Padding(1);
            
            Label lbl = new Label();
            lbl.Text = labelText + ":";
            lbl.Font = new Font("Segoe UI", 9.5f);
            lbl.ForeColor = Color.White;
            lbl.AutoSize = false;
            lbl.Size = new Size(150, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(10, 2);
            
            NumericUpDown num = new NumericUpDown();
            num.DecimalPlaces = 2;
            num.Increment = 0.1m;
            num.Minimum = 0.1m;
            num.Maximum = 100.0m;
            num.BackColor = Color.FromArgb(50, 50, 50);
            num.ForeColor = Color.White;
            num.Size = new Size(100, 25);
            num.Location = new Point(170, 2);
            num.Value = Math.Max(num.Minimum, Math.Min(num.Maximum, currentValue));
            
            num.ValueChanged += (s, e) => setter(num.Value);

            row.Controls.Add(lbl);
            row.Controls.Add(num);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        private void AddCheckBoxRow(string labelText, bool currentValue, Action<bool> setter)
        {
            Panel row = new Panel();
            row.Size = new Size(400, 28);
            row.Margin = new Padding(1);

            Label lbl = new Label();
            lbl.Text = labelText + ":";
            lbl.Font = new Font("Segoe UI", 9.5f);
            lbl.ForeColor = Color.White;
            lbl.AutoSize = false;
            lbl.Size = new Size(150, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(10, 2);

            CheckBox chk = new CheckBox();
            chk.Checked = currentValue;
            chk.FlatStyle = FlatStyle.Flat;
            chk.FlatAppearance.BorderSize = 1;
            chk.Size = new Size(200, 25);
            chk.Location = new Point(170, 2);
            chk.ForeColor = Color.White;
            
            chk.CheckedChanged += (s, e) => setter(chk.Checked);

            row.Controls.Add(lbl);
            row.Controls.Add(chk);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Axe settings update
            GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
            if (mappings != null)
            {
                if (cboIRAxis.SelectedItem is GamePadAxisItem irItem)
                    mappings.IRSensorAxis = irItem.Value;
                
                if (cboNunchukAxis.SelectedItem is GamePadAxisItem nunItem)
                    mappings.NunchukJoystickAxis = nunItem.Value;

                // IR Calibration update (EN/FR: Mise à jour calibrage IR)
                mappings.IRLinearity = (float)numLinearity.Value;
                mappings.IROverscan = (float)numOverscan.Value;
            }

            // Buttons are updated in real-time via setter delegates in AddMappingRow
            // So we just need to save options
            Options.Instance.Save();
            
            MessageBox.Show(this.FindForm(), $"GamePad Mappings for Player {_currentPlayer} saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // If we are currently running, maybe we need to reload mappings in the controller?
            // The controller reads Options.Instance directly usually, or we might need to trigger something.
            // For now assume direct read.
        }

        // Helper classes for ComboBox items
        private class GamePadButtonItem
        {
            public string Name { get; set; }
            public GamePadButton Value { get; set; }
            public GamePadButtonItem(string name, GamePadButton value) { Name = name; Value = value; }
        }

        private class GamePadAxisItem
        {
            public string Name { get; set; }
            public GamePadAxis Value { get; set; }
            public GamePadAxisItem(string name, GamePadAxis value) { Name = name; Value = value; }
        }

        private class GamePadMotionModeItem
        {
            public string Name { get; set; }
            public GamePadMotionMode Value { get; set; }
            public GamePadMotionModeItem(string name, GamePadMotionMode value) { Name = name; Value = value; }
        }
    }
}
