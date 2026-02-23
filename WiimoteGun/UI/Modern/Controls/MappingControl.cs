using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun.Controls
{
    public partial class MappingControl : UserControl
    {
        private VirtualKeyboard _activeKeyboard;

        private void ShowVirtualKeyboard(TextBox target)
        {
            if (_activeKeyboard != null && !_activeKeyboard.IsDisposed)
            {
                // V25p: Do NOT call Focus() here, it steals focus from the TextBox if user re-clicks it.
                // (EN/FR: NE PAS appeler Focus() ici, cela vole le focus du TextBox si l'utilisateur reclique.)
                return;
            }

            // V25p: Force activation of the parent form (ProfileOverlay)
            // This is critical for physical keyboard input to work in Overlay mode (which is non-activating by default).
            // (EN/FR: Forcer l'activation du formulaire parent pour permettre la saisie physique en mode Overlay.)
            this.FindForm()?.Activate();

            _activeKeyboard = new VirtualKeyboard(target);
            _activeKeyboard.StartPosition = FormStartPosition.Manual;
            
            Point screenPos = target.PointToScreen(new Point(0, target.Height));
            _activeKeyboard.Location = new Point(
                screenPos.X + (target.Width - _activeKeyboard.Width) / 2,
                screenPos.Y + 5 
            );
            
            var screen = Screen.FromControl(this);
            if (_activeKeyboard.Bottom > screen.WorkingArea.Bottom)
            {
                 _activeKeyboard.Top = target.PointToScreen(Point.Empty).Y - _activeKeyboard.Height - 5;
            }

            _activeKeyboard.FormClosed += (s, e) => _activeKeyboard = null;
            _activeKeyboard.Show();

            // V25p: Explicitly return focus to the target TextBox after showing the OSK.
            // (EN/FR: Rendre explicitement le focus au TextBox après avoir affiché le clavier.)
            target.Focus();
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
            btnGamePadMapping.FlatAppearance.BorderSize = 0;
            btnConfirmAssign.FlatAppearance.BorderSize = 0;
            btnCancelAssign.FlatAppearance.BorderSize = 0;
            

            
            // Initialize UI (EN/FR: Initialiser UI)
            LoadProfileUI();
            LoadProfileUI();
            LoadCurrentMappings();

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            // Conditional visibility (EN/FR: Visibilité conditionnelle)
            if (btnGamePadMapping != null)
                btnGamePadMapping.Visible = Options.Instance.EnableGamePadSwapMode;
        }

        public event EventHandler BackRequested;
        public event EventHandler GamePadMappingRequested;

        // Public methods (EN/FR: Méthodes publiques)
        public void LoadData()
        {
            LoadProfileUI();
            LoadCurrentMappings();

            // Masquer le bouton de mappage GamePad si l'option n'est pas activée (EN/FR: Hide GamePad mapping button if option not enabled)
            if (btnGamePadMapping != null)
                btnGamePadMapping.Visible = Options.Instance.EnableGamePadSwapMode;
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
                // V27: Hide Gamepad folder from Mouse UI (EN/FR: Cacher le dossier Gamepad de l'interface Souris)
                if (folder.Equals("Gamepad", StringComparison.OrdinalIgnoreCase)) continue;
                
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
            string statusText = "";
            bool hasLink = false;

            // 1. Status of Current Game (EN/FR: Statut du jeu actuel)
            if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
            {
                string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                if (!string.IsNullOrEmpty(mappedProfile))
                {
                    statusText = $"Current Game '{_currentExecutable}' -> '{mappedProfile}'";
                    hasLink = true;
                }
                else
                {
                    statusText = $"Current Game '{_currentExecutable}' (not mapped)";
                }
            }
            else
            {
                statusText = "No Game Detected";
            }

            // 2. Status of Loaded Profile (EN/FR: Statut du profil chargé)
            string currentProfile = Program.GetActiveRemapProfile();
            if (!string.IsNullOrEmpty(currentProfile))
            {
                // We use the relative path (e.g. "fps/gear5.remap")
                string linkedExe = GameProfileMappingManager.GetExecutableForProfile(currentProfile);
                if (!string.IsNullOrEmpty(linkedExe))
                {
                    // Append info (EN/FR: Ajouter info)
                    statusText += $" | Profile linked to: {linkedExe}";
                    hasLink = true;
                }
            }

            lblLinkedExe.Text = statusText;
            lblLinkedExe.ForeColor = hasLink ? ColorAccent : Color.Gray;
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
                                      mappedProfile.Replace('\\', '/').Equals(currentProfile.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
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
            panel.AutoScroll = true; // EN/FR: Activer le défilement vertical
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
                yPos += spacing;
            }

            int motionY = Math.Max(yPos, 15 + (10 * spacing)) + 35; // EN/FR: Augmenté de 15 à 35 pour aérer
            
            int totalMotionColumns = 2; // EN/FR: Passé de 3 à 2 colonnes pour éviter le texte tronqué
            int motionColumnWidth = totalWidth / totalMotionColumns;
            
            Action<string, int, int> AddMotionHeaderRow = (title, rowY, colIdx) =>
            {
                Label lblHeader = new Label
                {
                    Text = title,
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 9.0F, FontStyle.Bold),
                    Location = new Point(startX + (colIdx * motionColumnWidth), rowY),
                    Size = new Size(motionColumnWidth, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panel.Controls.Add(lblHeader);
            };

            Action<string, string, ButtonAction, int, int> AddMotionActionRow = (displayName, internalName, mapping, rowY, colIdx) =>
            {
                int colX = startX + (colIdx * motionColumnWidth);
                int localLabelWidth = 100; // EN/FR: Plus large
                
                Label lblButton = new Label
                {
                    Text = displayName + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.0F),
                    Location = new Point(colX, rowY),
                    Size = new Size(localLabelWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                Label lblMapping = new Label
                {
                    Text = GetMappingDisplay(mapping),
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 8.0F, FontStyle.Bold),
                    Location = new Point(colX + localLabelWidth + 2, rowY),
                    Size = new Size(motionColumnWidth - localLabelWidth - 5, 18),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };

                lblMapping.Click += (s, e) => {
                    _isAssignMode = true; 
                    _waitingForButton = internalName;
                    _waitingForPlayer = _currentPlayer;
                    _originalMapping = GetCurrentMapping(_currentPlayer, internalName);
                    ShowActionSelector(internalName);
                };
                
                panel.Controls.Add(lblButton);
                panel.Controls.Add(lblMapping);
            };

            // Wiimote Motions
            AddMotionHeaderRow("━ Wiimote ━ (Experimental)", motionY, 0);
            AddMotionActionRow("Up", "AccelWiimoteUp", mappings.AccelWiimoteUp, motionY + spacing * 1, 0);
            AddMotionActionRow("Down", "AccelWiimoteDown", mappings.AccelWiimoteDown, motionY + spacing * 2, 0);
            AddMotionActionRow("Left", "AccelWiimoteLeft", mappings.AccelWiimoteLeft, motionY + spacing * 3, 0);
            AddMotionActionRow("Right", "AccelWiimoteRight", mappings.AccelWiimoteRight, motionY + spacing * 4, 0);
            AddMotionActionRow("Shake", "AccelWiimoteShake", mappings.AccelWiimoteShake, motionY + spacing * 5, 0);

            // Nunchuk Motions
            AddMotionHeaderRow("━ Nunchuk ━ (Experimental)", motionY, 1);
            AddMotionActionRow("Up", "AccelNunchukUp", mappings.AccelNunchukUp, motionY + spacing * 1, 1);
            AddMotionActionRow("Down", "AccelNunchukDown", mappings.AccelNunchukDown, motionY + spacing * 2, 1);
            AddMotionActionRow("Left", "AccelNunchukLeft", mappings.AccelNunchukLeft, motionY + spacing * 3, 1);
            AddMotionActionRow("Right", "AccelNunchukRight", mappings.AccelNunchukRight, motionY + spacing * 4, 1);
            AddMotionActionRow("Shake", "AccelNunchukShake", mappings.AccelNunchukShake, motionY + spacing * 5, 1);

            // Motion Plus (Lower row, 2 columns spans)
            int secondRowY = motionY + spacing * 7;
            AddMotionHeaderRow("━━━━ Gyroscope (Motion Plus) (Experimental) ━━━━", secondRowY, 0);
            ((Label)panel.Controls[panel.Controls.Count-1]).Width = totalWidth; // Center span

            AddMotionActionRow("Tilt Up", "GyroMotionPlusUp", mappings.GyroMotionPlusUp, secondRowY + spacing * 1, 0);
            AddMotionActionRow("Tilt Down", "GyroMotionPlusDown", mappings.GyroMotionPlusDown, secondRowY + spacing * 2, 0);
            AddMotionActionRow("Tilt Left", "GyroMotionPlusLeft", mappings.GyroMotionPlusLeft, secondRowY + spacing * 3, 0);
            AddMotionActionRow("Tilt Right", "GyroMotionPlusRight", mappings.GyroMotionPlusRight, secondRowY + spacing * 1, 1);
            AddMotionActionRow("Roll Left", "GyroMotionPlusRollLeft", mappings.GyroMotionPlusRollLeft, secondRowY + spacing * 2, 1);
            AddMotionActionRow("Roll Right", "GyroMotionPlusRollRight", mappings.GyroMotionPlusRollRight, secondRowY + spacing * 3, 1);

            // Sensitivity Settings (EN/FR: Réglages de sensibilité)
            int sensY = secondRowY + spacing * 6; // EN/FR: Augmenté pour aérer (5 -> 6)
            
            Action<string, float, Action<float>, int, bool> AddSensControl = (labelText, currentVal, setter, colIdx, isDeadzone) =>
            {
                int colX = startX + (colIdx * motionColumnWidth);
                Label lbl = new Label {
                    Text = labelText + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.0F),
                    Location = new Point(colX, sensY),
                    Size = new Size(110, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                NumericUpDown num = new NumericUpDown {
                    Minimum = 0, Maximum = 100, // EN/FR: Max 100.0 (unifié avec GamePad)
                    Value = (decimal)currentVal,
                    Location = new Point(colX + 115, sensY),
                    Size = new Size(50, 18),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    DecimalPlaces = 2,
                    Increment = 0.1m
                };
                num.ValueChanged += (s, e) => {
                    setter((float)num.Value);
                    Options.Instance.Save();
                };
                panel.Controls.Add(lbl);
                panel.Controls.Add(num);
            };

            AddSensControl("Wiimote Accel", mappings.AccelWiimoteSensitivity, (v) => mappings.AccelWiimoteSensitivity = v, 0, false);
            AddSensControl("Nunchuk Accel", mappings.AccelNunchukSensitivity, (v) => mappings.AccelNunchukSensitivity = v, 1, false);
            sensY += spacing;
            AddSensControl("Wiimote DZ (G)", mappings.AccelWiimoteDeadzone, (v) => mappings.AccelWiimoteDeadzone = v, 0, true);
            AddSensControl("Nunchuk DZ (G)", mappings.AccelNunchukDeadzone, (v) => mappings.AccelNunchukDeadzone = v, 1, true);
            sensY += spacing;
            AddSensControl("Wii Shake (G)", mappings.AccelWiimoteShakeDeadzone, (v) => mappings.AccelWiimoteShakeDeadzone = v, 0, true);
            AddSensControl("Nun Shake (G)", mappings.AccelNunchukShakeDeadzone, (v) => mappings.AccelNunchukShakeDeadzone = v, 1, true);
            sensY += spacing;
            // EN: Shake oscillation count control (integer, not scaled by 100)
            // FR: Contrôle du nombre d'oscillations shake (entier, pas mis à l'échelle par 100)
            {
                int colX = startX;
                Label lblShakeCount = new Label {
                    Text = "Shake Count:",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.0F),
                    Location = new Point(colX, sensY),
                    Size = new Size(110, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                NumericUpDown numShakeCount = new NumericUpDown {
                    Minimum = 2, Maximum = 10,
                    Value = mappings.ShakeOscillationRequired,
                    Location = new Point(colX + 115, sensY),
                    Size = new Size(50, 18),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    DecimalPlaces = 0
                };
                numShakeCount.ValueChanged += (s, e2) => {
                    mappings.ShakeOscillationRequired = (int)numShakeCount.Value;
                    Options.Instance.Save();
                };
                panel.Controls.Add(lblShakeCount);
                panel.Controls.Add(numShakeCount);
            }
            sensY += spacing;
            AddSensControl("Gyro Sens.", mappings.GyroSensitivity, (v) => mappings.GyroSensitivity = v, 0, false);
            AddSensControl("Gyro Deadzone", mappings.GyroDeadzone, (v) => mappings.GyroDeadzone = v, 1, true);

            // 3D Visualizer Button (EN/FR: Bouton Visualiseur 3D)
            Button btnViz = new Button {
                Text = "3D",
                Location = new Point(startX + 180, sensY),
                Size = new Size(35, 24),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.0F, FontStyle.Bold),
                ForeColor = Color.Gold,
                Cursor = Cursors.Hand
            };
            btnViz.FlatAppearance.BorderSize = 0;
            btnViz.Click += (s, e) => Open3DVisualizer();
            
            ToolTip tt = new ToolTip();
            tt.SetToolTip(btnViz, "Open 3D Visualizer (Calibration tool)");
            panel.Controls.Add(btnViz);
        }

        private void Open3DVisualizer()
        {
            try {
                var formType = Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == "GyroVisualizerForm");
                
                if (formType != null)
                {
                    Form form = (Form)Activator.CreateInstance(formType);
                    form.Show(this.FindForm()); // EN/FR: Utiliser FindForm() comme propriétaire pour rester au premier plan (Use FindForm() as owner)
                }
                else 
                {
                    MessageBox.Show(this.FindForm(), "GyroVisualizerForm not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch (Exception ex) {
                MessageBox.Show(this.FindForm(), "Error opening visualizer: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                case "AccelWiimoteUp": return mappings.AccelWiimoteUp;
                case "AccelWiimoteDown": return mappings.AccelWiimoteDown;
                case "AccelWiimoteLeft": return mappings.AccelWiimoteLeft;
                case "AccelWiimoteRight": return mappings.AccelWiimoteRight;
                case "AccelWiimoteShake": return mappings.AccelWiimoteShake;

                case "AccelNunchukUp": return mappings.AccelNunchukUp;
                case "AccelNunchukDown": return mappings.AccelNunchukDown;
                case "AccelNunchukLeft": return mappings.AccelNunchukLeft;
                case "AccelNunchukRight": return mappings.AccelNunchukRight;
                case "AccelNunchukShake": return mappings.AccelNunchukShake;

                case "GyroMotionPlusUp": return mappings.GyroMotionPlusUp;
                case "GyroMotionPlusDown": return mappings.GyroMotionPlusDown;
                case "GyroMotionPlusLeft": return mappings.GyroMotionPlusLeft;
                case "GyroMotionPlusRight": return mappings.GyroMotionPlusRight;
                case "GyroMotionPlusRollLeft": return mappings.GyroMotionPlusRollLeft;
                case "GyroMotionPlusRollRight": return mappings.GyroMotionPlusRollRight;
                
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
                
                case "AccelWiimoteUp": mappings.AccelWiimoteUp = action; break;
                case "AccelWiimoteDown": mappings.AccelWiimoteDown = action; break;
                case "AccelWiimoteLeft": mappings.AccelWiimoteLeft = action; break;
                case "AccelWiimoteRight": mappings.AccelWiimoteRight = action; break;
                case "AccelWiimoteShake": mappings.AccelWiimoteShake = action; break;

                case "AccelNunchukUp": mappings.AccelNunchukUp = action; break;
                case "AccelNunchukDown": mappings.AccelNunchukDown = action; break;
                case "AccelNunchukLeft": mappings.AccelNunchukLeft = action; break;
                case "AccelNunchukRight": mappings.AccelNunchukRight = action; break;
                case "AccelNunchukShake": mappings.AccelNunchukShake = action; break;

                case "GyroMotionPlusUp": mappings.GyroMotionPlusUp = action; break;
                case "GyroMotionPlusDown": mappings.GyroMotionPlusDown = action; break;
                case "GyroMotionPlusLeft": mappings.GyroMotionPlusLeft = action; break;
                case "GyroMotionPlusRight": mappings.GyroMotionPlusRight = action; break;
                case "GyroMotionPlusRollLeft": mappings.GyroMotionPlusRollLeft = action; break;
                case "GyroMotionPlusRollRight": mappings.GyroMotionPlusRollRight = action; break;
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
                if (ofd.ShowDialog(this.FindForm()) == DialogResult.OK)
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
                if (dialog.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    string folderName = dialog.InputValue;
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        // V27: Prevent creating 'Gamepad' folder in Mouse UI (reserved for Gamepad profiles)
                        // (EN/FR: Empêcher création dossier 'Gamepad' en UI Souris - réservé aux profils Gamepad)
                        if (folderName.Equals("Gamepad", StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(this.FindForm(), 
                                "The name 'Gamepad' is reserved for system Gamepad profiles.\nPlease choose another name.", 
                                "Reserved Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        try
                        {
                            string remapDir = RemapProfileManager.GetRemapDirectory();
                            string newFolderPath = Path.Combine(remapDir, folderName);
                            Directory.CreateDirectory(newFolderPath);
                            
                            LoadProfileUI();
                            comboBoxSubfolders.SelectedItem = folderName;
                            MessageBox.Show(this.FindForm(), $"Folder '{folderName}' created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this.FindForm(), $"Failed to create folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this.FindForm(), "Please select a profile to delete", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(this.FindForm(), $"Delete '{selectedProfile}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                    string subfolder = selectedFolder == "(Root)" ? "" : selectedFolder;
                    string profilePath = RemapProfileManager.GetProfilePath(selectedProfile, subfolder);
                    
                    if (File.Exists(profilePath))
                    {
                        // V25l/m: Cleanup mappings using RELATIVE path (as stored in JSON)
                        // (EN/FR: Nettoyer mappings avec chemin RELATIF comme stocké dans JSON)
                        string relativePathCleanup = string.IsNullOrEmpty(subfolder) ? selectedProfile : Path.Combine(subfolder, selectedProfile);
                        GameProfileMappingManager.RemoveMappingByProfile(relativePathCleanup);

                        File.Delete(profilePath);
                        RefreshProfileList();
                        MessageBox.Show(this.FindForm(), $"Profile '{selectedProfile}' deleted", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this.FindForm(), $"Failed to delete: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(this.FindForm(), $"Hotkeys saved for Player {_currentPlayer}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        private void BtnGamePadMapping_Click(object sender, EventArgs e)
        {
            GamePadMappingRequested?.Invoke(this, EventArgs.Empty);
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
                MessageBox.Show(this.FindForm(), "Please enter profile name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                string subfolder = selectedFolder == "(Root)" ? null : selectedFolder;
                
                bool success = RemapProfileManager.SaveProfile(profileName, subfolder, profile);
                
                if (success)
                {
                    // FIX V25: Also save the Game Mapping (JSON) if Auto-Load is checked OR user confirms
                    // (EN/FR: Sauver aussi le mapping jeu (JSON) si Auto-Load coché OU utilisateur confirme)
                    
                    bool hasExe = !string.IsNullOrEmpty(_currentExecutable) && !string.IsNullOrEmpty(_currentExecutablePath);
                    bool shouldSaveMapping = false;

                    if (hasExe)
                    {
                        if (chkAutoLoad.Checked)
                        {
                            shouldSaveMapping = true;
                        }
                        else
                        {
                            // If user manually selected an EXE but forgot to check Auto-Load, ask them.
                            // (EN/FR: Si utilisateur a sélectionné manuellement un EXE mais oublié de cocher Auto-Load, demander.)
                            var result = MessageBox.Show(this.FindForm(),
                                $"Do you want to link this profile to '{_currentExecutable}' for auto-loading?", 
                                "Link Executable?", 
                                MessageBoxButtons.YesNo, 
                                MessageBoxIcon.Question);
                                
                            if (result == DialogResult.Yes)
                            {
                                chkAutoLoad.Checked = true;
                                shouldSaveMapping = true;
                            }
                        }
                    }

                    if (shouldSaveMapping)
                    {
                        string savedProfilePath = string.IsNullOrEmpty(subfolder) ? profileName + ".remap" : Path.Combine(subfolder, profileName + ".remap");
                        GameProfileMappingManager.AddMapping(_currentExecutable, savedProfilePath, _currentExecutablePath);
                        UpdateCurrentGameLabel(); // Refresh label to show link
                    }

                    RefreshProfileList();
                    MessageBox.Show(this.FindForm(), $"Profile '{profileName}' saved", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this.FindForm(), "Failed to save profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.FindForm(), $"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                MessageBox.Show(this.FindForm(), "Please select a profile", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    
                    // Force clear active modifier states to prevent stuck keys
                    HotkeyManager.ClearActiveState();
                    
                    Options.Instance.Save();
                    Program.LoadRemapProfileHot(relativePath, true);
                    
                    LoadCurrentMappings();
                    UpdateAutoLoadCheckbox();
                    UpdateCurrentGameLabel(); // V25m: Force UI refresh of "Linked EXE" status
                    
                    MessageBox.Show(this.FindForm(), $"Profile '{profile.ProfileName}' loaded", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this.FindForm(), "Failed to load profile", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.FindForm(), $"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            MessageBox.Show(this.FindForm(), "No active profile", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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


    }
}
