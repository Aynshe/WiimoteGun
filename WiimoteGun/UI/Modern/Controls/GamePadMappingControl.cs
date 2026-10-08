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
        private bool _selectedExeIsManual = false; // [V35] True when chosen via "Select Exe" (pins the selection)
        private bool _isUpdatingAutoLoad = false;
        private bool _dataSourcesInitialized = false;
        private ToolTip _toolTip = new ToolTip();
        
        // Dictionary to map ComboBox index/item to GamePadButton
        private List<GamePadButtonItem> _gamePadButtons;
        private List<GamePadAxisItem> _gamePadAxes;
        private List<GamePadMotionModeItem> _motionModes;

        public event EventHandler BackRequested;

        private Label lblIRAxisValue;
        private Label lblNunchukAxisValue;

        // [V33+V34] Transient non-blocking toast (EN/FR: Bandeau transitoire non bloquant)
        private TransientToast _toast;

        // [V48] Association currently displayed for the selected profile (per-profile sync)
        // (EN/FR: Association actuellement affichée pour le profil sélectionné (synchro par profil))
        private GameProfileMapping _linkedMapping;

        // [V50] Remembered manual selection (advanced association) - avoids double prompts
        // (EN/FR: Sélection manuelle mémorisée (association avancée) - évite les demandes en double)
        private string _manualGameName;
        private bool _manualIsFolder;
        private string _manualSystemName;

        // [V42] Emulator link: profile bound per GAME (from EmulationStation)
        // [V45] chkIsEmulator / chkIsFolder are Designer controls now (visible/editable in VS)
        // (EN/FR: Lien émulateur : profil lié par JEU. Les cases sont des contrôles Designer.)

        public GamePadMappingControl()
        {
            InitializeComponent();
            // [V57o3] EN: UiScaler owns ALL scaling on this page: with the Designer's AutoScaleMode.Font,
            //     changing the page's root font (ApplyFonts) made WinForms RE-SCALE the children
            //     on top of our own Scale (double-scale). None disables that interference.
            //     FR: UiScaler possede TOUT le scaling de cette page : avec l'AutoScaleMode.Font
            //     du Designer, le changement de police racine (ApplyFonts) faisait RE-SCALER les
            //     enfants par WinForms PAR-DESSUS notre Scale (double-scale). None supprime cette
            //     interference.
            this.AutoScaleMode = AutoScaleMode.None;
            InitializeModernAxes();
            InitializeDataSources();
            _toast = new TransientToast(this);
            WireEmulatorCheckboxes();
        }

        private void WireEmulatorCheckboxes()
        {
            if (chkIsEmulator == null || chkIsFolder == null) return;

            _toolTip.SetToolTip(chkIsEmulator,
                "Check when the selected EXE is an EMULATOR.\r\n" +
                "WiimoteGun will then use the GAME (file or folder) launched by this emulator\r\n" +
                "to link and auto-load this profile for that specific game.\r\n" +
                "When no game is running, check 'System name' for an advanced association.");

            _toolTip.SetToolTip(chkIsFolder,
                "Check when the game is launched through a FOLDER with extension (.pc/.game/.win...).\r\n" +
                "The folder name will be used as the game identity for linking and auto-load.");

            chkIsEmulator.CheckedChanged += (s, e) =>
            {
                chkIsFolder.Enabled = chkIsEmulator.Checked;
                if (!chkIsEmulator.Checked) chkIsFolder.Checked = false;
                UpdateStatusLabels();
            };

            // [V50] System name + memory reset
            _toolTip.SetToolTip(chkIsSystem,
                "Advanced association outside a game session: pick the system first, then the game file/folder.\r\n" +
                "Automatically checked during an ES game-start when 'Emulator' is checked.");
            chkIsSystem.CheckedChanged += (s, e) =>
            {
                if (_isUpdatingAutoLoad) return;

                if (chkIsSystem.Checked)
                {
                    // [V55l] If no system is remembered and not in active ES session, prompt user with EsSystemPickerForm
                    if (string.IsNullOrEmpty(_manualSystemName) && !EsScriptIntegration.HasCurrentGame)
                    {
                        string defaultSys = DetectSystemFromPath(_selectedExePath ?? _linkedMapping?.ExecutablePath) ?? EsScriptIntegration.LastSystem;
                        string picked = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(), "Select the game system", defaultSys);
                        if (!string.IsNullOrEmpty(picked))
                        {
                            _manualSystemName = picked;
                            UpdateExistingGamePadMappingSystem(picked);
                        }
                        else
                        {
                            // User cancelled picker -> uncheck without re-triggering handler
                            _isUpdatingAutoLoad = true;
                            chkIsSystem.Checked = false;
                            _isUpdatingAutoLoad = false;
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(_manualSystemName))
                    {
                        UpdateExistingGamePadMappingSystem(_manualSystemName);
                    }
                }
                else
                {
                    _manualGameName = null;
                    _manualSystemName = null;
                    UpdateExistingGamePadMappingSystem(null);
                }
                CheckAutoLoadStatus(null);
            };
            chkIsFolder.CheckedChanged += (s, e) =>
            {
                _manualGameName = null; // [V50] Reset remembered selection
                _manualSystemName = null;
            };
        }

        /// <summary>
        /// EN: [V55l] Try detecting the system name from the executable path (e.g. \roms\<system>\...).
        /// FR: [V55l] Tente de détecter le nom de système depuis le chemin de l'exe (ex: \roms\<system>\...).
        /// </summary>
        private static string DetectSystemFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                string norm = path.Replace('/', '\\');
                int romsIdx = norm.IndexOf(@"\roms\", StringComparison.OrdinalIgnoreCase);
                if (romsIdx >= 0)
                {
                    string sub = norm.Substring(romsIdx + 6);
                    int nextSlash = sub.IndexOf('\\');
                    if (nextSlash > 0)
                    {
                        return sub.Substring(0, nextSlash).Trim();
                    }
                    return sub.Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: [V55l] Update the system name on an existing mapping for the selected GamePad profile.
        /// FR: [V55l] Met à jour le nom de système sur le mapping existant du profil GamePad sélectionné.
        /// </summary>
        private void UpdateExistingGamePadMappingSystem(string systemName)
        {
            string subfolder = cboSubfolders.SelectedItem?.ToString();
            if (subfolder == "[Root]") subfolder = "";
            string profileName = cboProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(profileName)) return;
            string rel = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);
            if (GameProfileMappingManager.IsRootDefaultProfilePath(rel)) return;

            string relNorm = rel.Replace('\\', '/');
            var existing = GameProfileMappingManager.GetMappingByProfilePath(relNorm, true);
            if (existing != null)
            {
                GameProfileMappingManager.AddMapping(existing.ExecutableName, null, existing.ExecutablePath, relNorm,
                    existing.IsEmulator, existing.GameName, existing.GameIsFolder, systemName);
                UpdateStatusLabels(relNorm);
                ShowToast(string.IsNullOrEmpty(systemName)
                    ? $"System removed from association: {existing.ExecutableName}"
                    : $"System updated: {existing.ExecutableName} [{systemName}]");
            }
            else if (!string.IsNullOrEmpty(_selectedExeName) && chkAutoLoad != null && chkAutoLoad.Checked)
            {
                GameProfileMappingManager.AddMapping(_selectedExeName, null, _selectedExePath, relNorm,
                    chkIsEmulator?.Checked == true, null, false, systemName);
                UpdateStatusLabels(relNorm);
            }
        }

        /// <summary>
        /// EN: [V50] Resolve the association to create (game name, folder flag, system name).
        /// In an ES game session: automatic. Outside: advanced flow with MEMORY.
        /// FR: [V50] Résout l'association à créer. En session de jeu ES : automatique.
        /// Hors : flux avancé avec MÉMOIRE.
        /// </summary>
        private bool TryResolveAssociationForLink(out string gameName, out bool isFolder, out string systemName)
        {
            gameName = null;
            isFolder = false;
            systemName = null;

            if (chkIsEmulator == null || !chkIsEmulator.Checked)
            {
                // [V55l] Direct executable link (non-emulator):
                if (EsScriptIntegration.HasCurrentGame)
                {
                    systemName = EsScriptIntegration.LastSystem;
                    if (chkIsSystem != null && !string.IsNullOrEmpty(systemName))
                    {
                        _isUpdatingAutoLoad = true;
                        chkIsSystem.Checked = true;
                        _isUpdatingAutoLoad = false;
                    }
                    return true;
                }

                if (!string.IsNullOrEmpty(_manualSystemName))
                {
                    systemName = _manualSystemName;
                    if (chkIsSystem != null)
                    {
                        _isUpdatingAutoLoad = true;
                        chkIsSystem.Checked = true;
                        _isUpdatingAutoLoad = false;
                    }
                    return true;
                }

                string defaultSys = DetectSystemFromPath(_selectedExePath) ?? EsScriptIntegration.LastSystem;
                string picked = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(),
                    "Select the game system", defaultSys);
                if (!string.IsNullOrEmpty(picked))
                {
                    systemName = picked;
                    _manualSystemName = picked;
                    if (chkIsSystem != null)
                    {
                        _isUpdatingAutoLoad = true;
                        chkIsSystem.Checked = true;
                        _isUpdatingAutoLoad = false;
                    }
                }
                else
                {
                    if (chkIsSystem != null)
                    {
                        _isUpdatingAutoLoad = true;
                        chkIsSystem.Checked = false;
                        _isUpdatingAutoLoad = false;
                    }
                }
                return true;
            }

            bool folderExpected = chkIsFolder != null && chkIsFolder.Checked;

            // 1) ES game session: automatic (EN/FR: Session de jeu ES : automatique)
            if (EsScriptIntegration.HasCurrentGame)
            {
                if (chkIsSystem != null) chkIsSystem.Checked = true; // auto-check
                if (folderExpected)
                {
                    if (!EsScriptIntegration.LastGameIsFolder)
                    {
                        MessageBox.Show(this.FindForm(),
                            "The game received from EmulationStation is a FILE, not a folder.\r\nUncheck 'This is a folder' or launch a folder-based game first.",
                            "Not a folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    gameName = EsScriptIntegration.LastGameNameRaw;
                    isFolder = true;
                }
                else
                {
                    gameName = EsScriptIntegration.LastGameName;
                }
                return !string.IsNullOrEmpty(gameName);
            }

            // 2) Outside a session: advanced with memory (EN/FR: Hors session : avancé avec mémoire)
            if (!string.IsNullOrEmpty(_manualGameName))
            {
                gameName = _manualGameName;
                isFolder = _manualIsFolder;
                systemName = _manualSystemName;
                return true;
            }

            systemName = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(),
                "Select the game system", EsScriptIntegration.LastSystem);
            if (string.IsNullOrEmpty(systemName)) return false;

            if (!EsScriptIntegration.TryResolveGameName(folderExpected, this.FindForm(), out gameName, out isFolder))
                return false;

            _manualGameName = gameName;
            _manualIsFolder = isFolder;
            _manualSystemName = systemName;
            SimpleLogger.Instance.Info($"[ES Advanced] Remembered manual association: system='{systemName}', game='{gameName}' (folder: {isFolder})");
            return true;
        }

        /// <summary>
        /// EN: [V44] Resolve the game to bind. Game running -> ES value (automatic);
        /// no game running -> manual selection in advance (file or folder).
        /// FR: [V44] Résout le jeu à lier. Jeu en cours -> valeur ES (automatique) ;
        /// pas de jeu en cours -> sélection manuelle à l'avance (fichier ou dossier).
        /// </summary>
        private bool TryResolveGameNameForLink(out string gameName, out bool isFolder)
        {
            gameName = null;
            isFolder = false;

            if (chkIsEmulator == null || !chkIsEmulator.Checked) return true; // Not an emulator link

            bool folderExpected = chkIsFolder != null && chkIsFolder.Checked;

            if (EsScriptIntegration.TryResolveGameName(folderExpected, this.FindForm(), out gameName, out isFolder))
                return true;

            // ES value is a file while a folder was expected (EN/FR: La valeur ES est un fichier alors qu'un dossier était attendu)
            if (EsScriptIntegration.HasCurrentGame && folderExpected)
            {
                MessageBox.Show(this.FindForm(),
                    "The game received from EmulationStation is a FILE, not a folder.\r\nUncheck 'This is a folder' or launch a folder-based game first.",
                    "Not a folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return false; // Canceled or invalid
        }

        /// <summary>EN: [V44] Game name for REMOVAL (never prompts). FR: [V44] Nom de jeu pour le RETRAIT (jamais de dialogue).</summary>
        private string GetEsGameNameForRemoval()
        {
            if (chkIsEmulator == null || !chkIsEmulator.Checked || !EsScriptIntegration.HasCurrentGame)
                return null;
            return (chkIsFolder != null && chkIsFolder.Checked) ? EsScriptIntegration.LastGameNameRaw : EsScriptIntegration.LastGameName;
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

        // ==========================================================================================
        // [V33+V34] TRANSIENT TOAST (EN/FR: BANDEAU TRANSITOIRE)
        // Delegates to the shared TransientToast helper (bottom-center, 2.5s, click to dismiss).
        // (EN/FR: Délègue au helper partagé TransientToast (bas-centre, 2,5s, clic pour fermer).)
        // ==========================================================================================
        private void ShowToast(string message, bool isError = false)
        {
            _toast?.Show(message, isError);
        }

        private void HideToast()
        {
            _toast?.Hide();
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

        public void SetCurrentGame(string exeName, string exePath = null)
        {
            // [V47] Never wipe an already-selected exe with an empty update
            // (EN/FR: Ne jamais écraser un exe déjà sélectionné par une mise à jour vide)
            if (string.IsNullOrEmpty(exeName) && !string.IsNullOrEmpty(_selectedExeName))
            {
                UpdateStatusLabels();
                return;
            }

            if (string.IsNullOrEmpty(exeName))
            {
                UpdateStatusLabels();
                return;
            }

            // [V35] Follow the foreground executable UNLESS the user manually selected one.
            // Previously the control stayed stuck on the FIRST auto-detected exe: when the
            // foreground game changed, checking Auto-load re-linked the PREVIOUS exe and it
            // re-entered the JSON (ghost link).
            // (EN/FR: Suivre l'exécutable au premier plan SAUF sélection manuelle.
            // Avant, le contrôle restait bloqué sur le PREMIER exe détecté : quand le jeu
            // au premier plan changeait, cocher Auto-load reliait l'exe PRÉCÉDENT qui
            // réintégrait le json (lien fantôme).)
            if (!_selectedExeIsManual && !string.Equals(_selectedExeName, exeName, StringComparison.OrdinalIgnoreCase))
            {
                _selectedExeName = exeName;
                _selectedExePath = string.IsNullOrEmpty(exePath) ? null : exePath; // [V47] Path from the foreground hook
                CheckAutoLoadStatus(null);
            }
            else if (_selectedExeName == null)
            {
                _selectedExeName = exeName;
                _selectedExePath = string.IsNullOrEmpty(exePath) ? null : exePath; // [V47]
                CheckAutoLoadStatus(null);
            }
            else
            {
                // [V47] Same exe but the hook now provides its full path: keep it
                // (EN/FR: Même exe mais le hook fournit maintenant son chemin complet : le garder)
                if (!_selectedExeIsManual && string.IsNullOrEmpty(_selectedExePath) && !string.IsNullOrEmpty(exePath))
                    _selectedExePath = exePath;
                UpdateStatusLabels();
            }
        }

        private void UpdateStatusLabels(string profilePathHint = null)
        {
            try
            {
                string statusText = "";
                bool hasLink = false;
                GameProfileMapping linkMap = null;

                // 1. Selected profile path (hint or combos)
                // (EN/FR: Chemin du profil sélectionné (indice ou combos))
                string currentProfile = profilePathHint;
                if (currentProfile == null)
                {
                    string subfolder = cboSubfolders.SelectedItem?.ToString();
                    if (subfolder == "[Root]") subfolder = "";
                    string profileName = cboProfiles.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(profileName))
                        currentProfile = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);
                }

                // [V51] Unified display (same as the mouse page): when the selected
                // profile carries an association, show ONLY that association — no more
                // double segment "App: ... (not linked) | Profile linked to ...".
                // (EN/FR: Affichage unifié (identique à la page souris) : quand le profil
                // sélectionné porte une association, n'afficher QUE cette association.)
                if (!string.IsNullOrEmpty(currentProfile))
                {
                    linkMap = GameProfileMappingManager.GetMappingByProfilePath(
                        currentProfile.Replace('\\', '/'), true);
                }

                if (linkMap != null)
                {
                    statusText = $"Profile linked to: {linkMap.ExecutableName}";
                    if (!string.IsNullOrEmpty(linkMap.GameName))
                        statusText += $" [game: {linkMap.GameName}{(linkMap.GameIsFolder ? ", folder" : "")}]";
                    else if (!string.IsNullOrEmpty(linkMap.SystemName))
                        statusText += $" [{linkMap.SystemName}]";
                    hasLink = true;
                }
                else
                {
                    // 2. Current app status only when the profile has no association
                    // (EN/FR: Statut de l'application courante seulement si le profil
                    // n'a pas d'association)
                    string currentExe = _selectedExeName ?? Program.LastDetectedGameName;
                    string currentExePath = _selectedExePath ?? Program.LastDetectedGamePath;

                    if (!string.IsNullOrEmpty(currentExe))
                    {
                        var byExe = GameProfileMappingManager.GetBestMapping(currentExe, currentExePath,
                            EsScriptIntegration.LastGameName, EsScriptIntegration.LastGameNameRaw);
                        string mappedProfile = byExe?.GamePadProfilePath;
                        if (!string.IsNullOrEmpty(mappedProfile))
                        {
                            // Same wording as the mouse page (EN/FR: Même libellé que la page souris)
                            statusText = $"Current Game '{currentExe}' -> '{Path.GetFileName(mappedProfile)}'";
                            hasLink = true;
                            linkMap = byExe;
                        }
                        else
                        {
                            statusText = $"Current Game '{currentExe}' (not mapped)";
                        }
                    }
                    else
                    {
                        statusText = "No Game Detected";
                    }
                }

                lblDetectedApp.Text = statusText;

                // [V51] Same colors as the mouse page: amber when the link carries a
                // game, accent blue when linked without game, gray otherwise.
                // (EN/FR: Mêmes couleurs que la page souris : ambre si le lien porte
                // un jeu, bleu accent si lié sans jeu, gris sinon.)
                bool gameScoped = linkMap != null && !string.IsNullOrEmpty(linkMap.GameName);
                lblDetectedApp.ForeColor = !hasLink ? Color.Gray
                    : (gameScoped ? Color.FromArgb(255, 170, 40) : Color.FromArgb(0, 122, 204));

                _toolTip.SetToolTip(lblDetectedApp,
                    gameScoped && linkMap != null
                        ? $"Linked game: {linkMap.GameName}" + (linkMap.GameIsFolder ? " (folder)" : "") +
                          (linkMap.IsEmulator ? $" | Emulator: {linkMap.ExecutableName}" : "")
                        : "");
            }
            catch { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // [V31] Full data load every time the control is shown: without this, the
            // subfolders/profiles combos stay empty when the page is reopened.
            // (EN/FR: Chargement complet à chaque affichage : sans cela, les combos
            // sous-dossiers/profils restent vides à la réouverture de la page.)
            LoadData();
        }

        private void InitializeDataSources()
        {
            // [V31] Guard against double event wiring (constructor + OnLoad)
            // (EN/FR: Garde contre le double câblage d'événements (constructeur + OnLoad))
            if (_dataSourcesInitialized) return;
            _dataSourcesInitialized = true;

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

            // [V31] Clarify btnApply scope: it saves current mappings to settings.cfg (global
            // config, incl. IR calibration), it does NOT write default.remap or custom profiles
            // (those are written by "Save Profile").
            // (EN/FR: Précise la portée de btnApply : sauvegarde les mappings actuels dans
            // settings.cfg (config globale, incl. calibration IR), n'écrit NI default.remap
            // NI les profils custom (écrits via "Save Profile").)
            _toolTip.SetToolTip(btnApply, "Saves current mappings to settings.cfg (global config, incl. IR calibration).\nUse 'Save Profile' to write default.remap (Root) or custom profiles (subfolders).");

            // Initial Load
            // [V57o4] EN: Constructor population - rows built unscaled; the ProfileOverlay's
            //     ApplyForm scales them ONCE with the page tree (no ScaleChildren here, it
            //     would double-scale before ApplyForm).
            //     FR: Population du constructeur - lignes construites non scalées ; l'
            //     ApplyForm du ProfileOverlay les scale UNE FOIS avec l'arbre de la page
            //     (pas de ScaleChildren ici, il doublerait le scale avant ApplyForm).
            LoadCurrentMappings(initial: true);
        }

        public void LoadData()
        {
            // [V31] Ensure the GamePad default.remap fallback exists before listing profiles
            // (EN/FR: Garantir le profil de repli default.remap GamePad avant de lister les profils)
            try
            {
                RemapProfileManager.EnsureDefaultGamePadProfile();
            }
            catch { }

            // [V56d] Recreate the tooltip component on every rebuild: a ToolTip created once
            // at construction (overlay path: before any handle exists) and surviving many
            // row handle create/destroy cycles (LoadData clears and rebuilds the rows on
            // every page open) becomes unreliable — tooltips "tend not to show" via the
            // gamepad icon page, while the Configure... dialog (fresh instance) always
            // works. The mouse Mapping page never had the issue because it builds its rows
            // with a FRESH ToolTip per rebuild — same pattern applied here now.
            // (EN/FR: Recrée le composant tooltip à chaque reconstruction : un ToolTip
            // créé une seule fois à la construction (chemin overlay : avant tout handle) et
            // survivant à de nombreux cycles de destruction/recréation des handles de
            // lignes (LoadData vide et reconstruit les lignes à chaque ouverture de page)
            // devient INFIABLE — les info-bulles « ont tendance à ne pas
            // s'afficher » via la page icône manette, alors que le dialogue Configure...
            // (instance fraîche) fonctionne toujours. La page mapping Souris n'a jamais eu
            // le problème car elle construit ses lignes avec un ToolTip NEUF par
            // reconstruction — même motif appliqué ici.)
            RecreateToolTip();

            LoadSubfolders();
            LoadCurrentMappings();
            UpdateStatusLabels();
        }

        /// <summary>
        /// EN: [V56d] Dispose the shared tooltip and create a fresh one, then re-register
        /// the STATIC control tooltips (the row/dynamic ones — lblDetectedApp,
        /// chkAutoLoad — are re-registered by UpdateStatusLabels / CheckAutoLoadStatus).
        /// FR: [V56d] Dispose le tooltip partagé, en crée un neuf, puis ré-enregistre les
        /// info-bulles STATIques (celles des lignes/dynamiques — lblDetectedApp,
        /// chkAutoLoad — sont ré-enregistrées par UpdateStatusLabels / CheckAutoLoadStatus).
        /// </summary>
        private void RecreateToolTip()
        {
            try
            {
                ToolTip old = _toolTip;

                _toolTip = new ToolTip
                {
                    InitialDelay = 300,
                    AutoPopDelay = 8000,
                    ReshowDelay = 100
                };

                if (old != null)
                {
                    try { old.RemoveAll(); } catch { }
                    old.Dispose();
                }

                // EN/FR: [V56d] Re-register the static tooltips (moved from the constructor
                // path so they survive the tooltip recreation)
                _toolTip.SetToolTip(chkIsEmulator,
                    "Check when the selected EXE is an EMULATOR.\r\n" +
                    "WiimoteGun will then use the GAME (file or folder) launched by this emulator\r\n" +
                    "to link and auto-load this profile for that specific game.\r\n" +
                    "When no game is running, check 'System name' for an advanced association.");

                _toolTip.SetToolTip(chkIsFolder,
                    "Check when the game is launched through a FOLDER with extension (.pc/.game/.win...).\r\n" +
                    "The folder name will be used as the game identity for linking and auto-load.");

                _toolTip.SetToolTip(chkIsSystem,
                    "Advanced association outside a game session: pick the system first, then the game file/folder.\r\n" +
                    "Automatically checked during an ES game-start when 'Emulator' is checked.");

                if (btnApply != null)
                {
                    _toolTip.SetToolTip(btnApply,
                        "Saves current mappings to settings.cfg (global config, incl. IR calibration).\n" +
                        "Use 'Save Profile' to write default.remap (Root) or custom profiles (subfolders).");
                }

                if (chkAutoLoad != null)
                {
                    // Refreshed later by CheckAutoLoadStatus with the live status
                    // (EN/FR: Rafraîchi ensuite par CheckAutoLoadStatus avec l'état réel)
                    _toolTip.SetToolTip(chkAutoLoad, "No game detected or selected");
                }
            }
            catch { }
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
                    ShowToast($"Profile loaded: {profileName}");

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
                      ShowToast("Default profile saved to Root (default.remap)");
                      CboSubfolders_SelectedIndexChanged(null, null); // Refresh
                      UpgradeSavedAssociationIfNeeded("default.remap"); // [V51c]
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
                        
                        ShowToast($"Profile saved: {savedName}");

                        // [V51c] Same logic as the mouse page Save: resolve the advanced
                        // association now if Emulator is checked but the saved profile's
                        // link carries no game yet.
                        // (EN/FR: Même logique que le Save page souris : résoudre
                        // l'association avancée maintenant si Emulator est coché mais que
                        // le lien du profil sauvegardé ne porte pas encore de jeu.)
                        UpgradeSavedAssociationIfNeeded(string.IsNullOrEmpty(subfolder) ? savedName : Path.Combine(subfolder, savedName));
                    }
                    else
                    {
                        MessageBox.Show(this.FindForm(), "Failed to save profile. See logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// EN: [V51c] GamePad commit point (mirror of the mouse page Save fix V51b):
        /// after saving a profile, if Emulator is checked (and Auto-Load carries a link)
        /// but the saved profile's association has NO game yet — e.g. the bare link was
        /// created at the manual exe selection BEFORE the boxes were checked — resolve the
        /// advanced association NOW (system picker + game file/folder) and update the link.
        /// FR: [V51c] Point de validation GamePad (miroir du fix Save souris V51b) :
        /// après la sauvegarde d'un profil, si Emulator est coché (et qu'Auto-Load porte
        /// un lien) mais que l'association du profil n'a PAS encore de jeu — ex. lien nu
        /// créé à la sélection manuelle d'exe AVANT le cochage des cases — résoudre
        /// l'association avancée MAINTENANT (système + jeu fichier/dossier).
        /// </summary>
        private void UpgradeSavedAssociationIfNeeded(string savedRelativePath)
        {
            try
            {
                if (chkAutoLoad == null || !chkAutoLoad.Checked) return; // A link must exist or be wanted
                if (string.IsNullOrEmpty(savedRelativePath)) return;
                // [V52b] The root default.remap is never associable
                // (EN/FR: Le default.remap racine n'est jamais associable)
                if (GameProfileMappingManager.IsRootDefaultProfilePath(savedRelativePath)) return;

                string rel = savedRelativePath.Replace('\\', '/');
                var m = GameProfileMappingManager.GetMappingByProfilePath(rel, true);

                if (chkIsEmulator != null && chkIsEmulator.Checked)
                {
                    if (m != null && !string.IsNullOrEmpty(m.GameName)) return; // Association already complete

                    string esGame; bool esGameIsFolder; string esSystem;
                    if (!TryResolveAssociationForLink(out esGame, out esGameIsFolder, out esSystem)) return;
                    if (string.IsNullOrEmpty(esGame)) return; // Not an emulator link / canceled

                    string exeName = m != null ? m.ExecutableName : _selectedExeName;
                    string exePath = m != null ? m.ExecutablePath : _selectedExePath;
                    if (string.IsNullOrEmpty(exeName)) return;

                    GameProfileMappingManager.AddMapping(exeName, null, exePath, savedRelativePath,
                        true, esGame, esGameIsFolder, esSystem);
                    ShowToast($"Association updated: {exeName} [game: {esGame}{(esGameIsFolder ? ", folder" : "")}]");
                    UpdateStatusLabels(savedRelativePath);
                }
                else if (chkIsSystem != null && chkIsSystem.Checked &&
                         (m == null || string.IsNullOrEmpty(m.SystemName) || (_manualSystemName != null && _manualSystemName != m.SystemName)))
                {
                    // [V55l] Direct exe with System name checked: prompt/apply system
                    string sys = _manualSystemName ?? m?.SystemName;
                    if (string.IsNullOrEmpty(sys))
                    {
                        string defaultSys = DetectSystemFromPath(_selectedExePath ?? m?.ExecutablePath) ?? EsScriptIntegration.LastSystem;
                        sys = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(), "Select the game system", defaultSys);
                        if (!string.IsNullOrEmpty(sys)) _manualSystemName = sys;
                    }

                    if (!string.IsNullOrEmpty(sys))
                    {
                        string exeName = m != null ? m.ExecutableName : _selectedExeName;
                        string exePath = m != null ? m.ExecutablePath : _selectedExePath;
                        if (string.IsNullOrEmpty(exeName)) return;

                        GameProfileMappingManager.AddMapping(exeName, null, exePath, savedRelativePath,
                            false, m?.GameName, m?.GameIsFolder ?? false, sys);
                        ShowToast($"Association updated: {exeName} [{sys}]");
                        UpdateStatusLabels(savedRelativePath);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[V51c] GamePad association upgrade failed: {ex.Message}");
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
                        
                        ShowToast($"Profile deleted: {profileName}");
                        
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

        private static bool SameProfilePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return a.Replace('\\', '/').Equals(b.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        // [V50] True when Auto-load creation is allowed (game session or advanced armed)
        // (EN/FR: True si la création Auto-load est permise (session de jeu ou mode avancé armé))
        private bool _gameSessionForAutoLoad;

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

            // [V50] Grayed outside an ES game session, unless an association exists on the
            // selected profile (so it can be unchecked) or the advanced 'System name' is armed.
            // (EN/FR: Grisé hors session de jeu ES, sauf si une association existe sur le
            // profil sélectionné (pour pouvoir la décocher) ou si 'System name' avancé est armé.)
            bool sessionActive = EsScriptIntegration.HasCurrentGame;
            bool advancedArmed = chkIsSystem != null && chkIsSystem.Checked;
            _gameSessionForAutoLoad = sessionActive || advancedArmed;

            _toolTip.SetToolTip(chkAutoLoad, _gameSessionForAutoLoad
                ? $"Auto-Load for {lastGame}"
                : "Requires an active game (ES game-start) - or check 'System name' for an advanced association");

            if (profilePath == null) // Provide fallback via selected combos
            {
                string subfolder = cboSubfolders.SelectedItem?.ToString();
                if (subfolder == "[Root]") subfolder = "";
                string profileName = cboProfiles.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(profileName))
                    profilePath = string.IsNullOrEmpty(subfolder) ? profileName : Path.Combine(subfolder, profileName);
            }

            // Check if currently linked
            // [V48] Per-profile sync: the current-exe mapping when it matches the selected
            // profile, otherwise the JSON association of the profile itself (e.g. an
            // emulator/game link while browsing the frontend).
            // (EN/FR: Synchro par profil : le mapping de l'exe courant s'il correspond au
            // profil sélectionné, sinon l'association JSON du profil lui-même (ex: lien
            // émulateur/jeu pendant qu'on navigue le frontend).)
            string currentProfileRelPath = profilePath?.Replace('\\', '/');

            // [V52b] The ROOT default.remap can be EDITED but NEVER associated: it is the
            // base fallback mapping. Auto-Load stays grayed and unchecked for it.
            // (EN/FR: Le default.remap RACINE peut être MODIFIÉ mais JAMAIS associé :
            // c'est le mapping de base. Auto-Load reste grisé et décoché pour lui.)
            if (GameProfileMappingManager.IsRootDefaultProfilePath(currentProfileRelPath))
            {
                _isUpdatingAutoLoad = true;
                try
                {
                    chkAutoLoad.Checked = false;
                    chkAutoLoad.Enabled = false;
                    _toolTip.SetToolTip(chkAutoLoad,
                        "The root default.remap is the base mapping: it cannot be associated to an executable.");
                }
                catch { }
                _isUpdatingAutoLoad = false;
                UpdateStatusLabels(profilePath);
                return;
            }

            var byExe = GameProfileMappingManager.GetBestMapping(lastGame, lastGamePath,
                EsScriptIntegration.LastGameName, EsScriptIntegration.LastGameNameRaw);

            if (byExe != null && SameProfilePath(byExe.GamePadProfilePath, currentProfileRelPath))
            {
                _linkedMapping = byExe;
            }
            else
            {
                _linkedMapping = string.IsNullOrEmpty(currentProfileRelPath)
                    ? null
                    : GameProfileMappingManager.GetMappingByProfilePath(currentProfileRelPath, true);
            }
            string linkedProfileRelPath = _linkedMapping?.GamePadProfilePath?.Replace('\\', '/');

            // [V32] Reflect the REAL link state in the checkbox (guarded so the change
            // does not fire the handler and remove the link we just detected).
            // [V46] Also sync 'Emulator' / 'This is a folder' from the JSON mapping.
            // (EN/FR: Refléter l'état RÉEL du lien dans la case à cocher (protégé).
            // Synchronise aussi 'Emulator' / 'This is a folder' depuis le mapping JSON.)
            _isUpdatingAutoLoad = true;
            try
            {
                chkAutoLoad.Checked = currentProfileRelPath != null &&
                                      string.Equals(currentProfileRelPath, linkedProfileRelPath, StringComparison.OrdinalIgnoreCase);

                // [V50] Enabled only during a session / advanced armed / existing association
                // (EN/FR: Activé seulement en session / avancé armé / association existante)
                chkAutoLoad.Enabled = _gameSessionForAutoLoad || _linkedMapping != null;

                // [V51c] Sync the Emulator/Folder/System boxes ONLY when the association
                // actually carries an emulator link (or when there is NO association at all,
                // which clears them). A PLAIN exe association must NOT touch the boxes:
                // the user may be building the advanced association right now — checking
                // 'System name' after 'Emulator' previously RESET 'Emulator' from the bare
                // JSON link (IsEmulator=false).
                // (EN/FR: Synchroniser Emulator/Folder/System SEULEMENT si l'association
                // porte réellement un lien émulateur (ou s'il n'y a AUCUNE association, ce
                // qui les nettoie). Une association d'exe SIMPLE ne doit PAS toucher les
                // cases : l'utilisateur peut être en train de construire l'association
                // avancée — cocher 'System name' après 'Emulator' réinitialisait avant
                // 'Emulator' depuis le lien nu du json (IsEmulator=false).)
                if (_linkedMapping != null)
                {
                    if (_linkedMapping.IsEmulator)
                    {
                        chkIsEmulator.Checked = true;
                        chkIsFolder.Enabled = true;
                        chkIsFolder.Checked = _linkedMapping.GameIsFolder;
                    }
                    if (chkIsSystem != null)
                        chkIsSystem.Checked = !string.IsNullOrEmpty(_linkedMapping.SystemName);
                }
                else if (_linkedMapping == null)
                {
                    chkIsEmulator.Checked = false;
                    chkIsFolder.Enabled = false;
                    chkIsFolder.Checked = false;
                    if (chkIsSystem != null)
                        chkIsSystem.Checked = false;
                }
                // else: plain exe association -> leave the user's boxes untouched
            }
            catch { }
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

            // [V52b] The root default.remap is never associable
            // (EN/FR: Le default.remap racine n'est jamais associable)
            if (GameProfileMappingManager.IsRootDefaultProfilePath(relativePath))
            {
                _isUpdatingAutoLoad = true;
                chkAutoLoad.Checked = false;
                _isUpdatingAutoLoad = false;
                return;
            }

            if (chkAutoLoad.Checked)
            {
                // [V42-V50] Association resolution: automatic in session, advanced with memory outside
                // (EN/FR: Résolution d'association : automatique en session, avancé avec mémoire hors)
                string esGame; bool esGameIsFolder; string esSystem;
                if (!TryResolveAssociationForLink(out esGame, out esGameIsFolder, out esSystem))
                {
                    // Canceled/invalid: don't create a broken link
                    // (EN/FR: Annulé/invalide : ne pas créer un lien cassé)
                    _isUpdatingAutoLoad = true;
                    chkAutoLoad.Checked = false;
                    _isUpdatingAutoLoad = false;
                }
                else
                {
                    GameProfileMappingManager.AddMapping(lastGame, null, lastGamePath, relativePath,
                        chkIsEmulator?.Checked == true, esGame, esGameIsFolder, esSystem); // Only update GamePad link
                }
            }
            else
            {
                // [V48] Unchecking removes THE DISPLAYED ASSOCIATION (its exe + game),
                // not the foreground exe: a profile linked to an emulator+game can be
                // unlinked while browsing the frontend.
                // (EN/FR: Le décochage supprime L'ASSOCIATION AFFICHÉE (son exe + jeu),
                // pas l'exe au premier plan : un profil lié à un émulateur+jeu peut être
                // dissocié pendant qu'on navigue le frontend.)
                if (_linkedMapping != null)
                {
                    GameProfileMappingManager.RemoveGamePadProfileLinkForExecutable(
                        _linkedMapping.ExecutableName, _linkedMapping.ExecutablePath, _linkedMapping.GameName);
                    SimpleLogger.Instance.Info($"[V48] Removed GamePad association of profile from '{_linkedMapping.ExecutableName}'" +
                        (string.IsNullOrEmpty(_linkedMapping.GameName) ? "" : $" [game: {_linkedMapping.GameName}]"));
                    _linkedMapping = null;
                }
                else
                {
                    GameProfileMappingManager.RemoveGamePadProfileLinkForExecutable(lastGame, lastGamePath, GetEsGameNameForRemoval());
                }
                CheckAutoLoadStatus(relativePath);
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
                    _selectedExeIsManual = true; // [V35] Pin the manual selection (auto-detection no longer overrides)

                    // [V51] Manual exe selection = explicit intent: make Auto-Load available
                    // and CHECKED directly (creates the link immediately) — same logic as
                    // the mouse page (user request overriding the V35 no-auto-check).
                    // (EN/FR: Sélection manuelle d'un exe = intention explicite : rendre
                    // Auto-Load disponible et COCHÉ directement (crée le lien immédiatement)
                    // — même logique que la page souris (demande utilisateur, remplace V35).)
                    CheckAutoLoadStatus(null);

                    if (chkAutoLoad != null && cboProfiles.SelectedItem != null)
                    {
                        string selSubfolder = cboSubfolders.SelectedItem?.ToString();
                        if (selSubfolder == "[Root]") selSubfolder = "";
                        string selProfileName = cboProfiles.SelectedItem.ToString();
                        string selRelPath = string.IsNullOrEmpty(selSubfolder) ? selProfileName : Path.Combine(selSubfolder, selProfileName);

                        // [V52b] Never enable/check Auto-Load for the root default.remap
                        // (EN/FR: Jamais activer/cocher Auto-Load pour le default.remap racine)
                        if (GameProfileMappingManager.IsRootDefaultProfilePath(selRelPath))
                        {
                            UpdateStatusLabels();
                            return;
                        }

                        chkAutoLoad.Enabled = true; // Manual selection unlocks the checkbox
                        if (!chkAutoLoad.Checked)
                        {
                            _isUpdatingAutoLoad = false;
                            chkAutoLoad.Checked = true; // Fires the handler -> AddMapping
                        }
                        else
                        {
                            // [V55l] If chkAutoLoad was ALREADY checked, resolve association and update link
                            string gameName; bool gameIsFolder; string systemName;
                            if (TryResolveAssociationForLink(out gameName, out gameIsFolder, out systemName))
                            {
                                GameProfileMappingManager.AddMapping(_selectedExeName, null, _selectedExePath, selRelPath,
                                    chkIsEmulator?.Checked == true, gameName, gameIsFolder, systemName);
                            }
                        }
                        UpdateStatusLabels();
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

        // [V57o4] EN: initial=true = CONSTRUCTOR call (rows scaled once by the overlay's
        //     ApplyForm - no ScaleChildren); initial=false = RUNTIME call (rows scaled by
        //     ScaleChildren below). FR: initial=true = appel du CONSTRUCTEUR (lignes
        //     scalées une fois par l'ApplyForm de l'overlay - pas de ScaleChildren) ;
        //     initial=false = appel RUNTIME (lignes scalées par le ScaleChildren ci-dessous).
        private void LoadCurrentMappings(bool initial = false)
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
            // [V57g] EN: Label reflects the active backend - XInput means ViGEmBus in
            //     RawInput (VMulti) mode and the HIDMaestro XUSB companion (no ViGEmBus
            //     dependency at all) in RawInput (UMDF2) mode. The setter routes through
            //     THE shared swap function (Program.SetGamePadOutputApi), the same one
            //     the tile modal's SWICTH button uses.
            //     FR: Le libellé reflète le backend actif - XInput signifie ViGEmBus en
            //     mode RawInput (VMulti) et le companion XUSB HIDMaestro (aucune
            //     dépendance ViGEmBus) en mode RawInput (UMDF2). Le setter passe par LA
            //     fonction de bascule partagée (Program.SetGamePadOutputApi), la même
            //     que le bouton SWICTH de la modale en tuiles.
            bool umdf2Mode = Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf;
            AddCheckBoxRow(
                "Use XInput (" + (umdf2Mode ? "UMDF2/XUSB" : "ViGEmBus") + ")",
                mappings.UseXInput,
                (val) =>
                {
                    mappings.UseXInput = val;
                    Program.SetGamePadOutputApi(new[] { _currentPlayer }, val, save: false);
                },
                "USE XINPUT:\r\n" +
                "ON = XInput gamepad (games/emulators reading XInput slots).\r\n" +
                (umdf2Mode
                    ? "UMDF2 mode: HIDMaestro Xbox 360 XUSB companion - no ViGEmBus needed.\r\n"
                    : "VMulti mode: ViGEmBus driver generates the pad (must be installed).\r\n") +
                "OFF = DirectInput gamepad (" + (umdf2Mode ? "HIDMaestro/vmulti-compatible" : "VMulti Col06") + ").\r\n" +
                "La même fonction que le bouton [SWITCH] de la modale (appui long PLUS).");

            // [V56] Physical FIRE button: drives the trigger (weapon) rumble in GamePad mode
            // (EN/FR: Bouton physique de TIR : pilote la vibration de gâchette (arme) en mode GamePad)
            AddPhysicalButtonRow(
                "Fire Button",
                mappings.FireButton,
                "WiiB",
                (val) => mappings.FireButton = val,
                "FIRE BUTTON (GamePad rumble source):\r\n" +
                "The physical Wiimote/Nunchuk button you use to SHOOT. Its ON-screen press\r\n" +
                "triggers the weapon rumble (same feature as the mouse-mode trigger rumble,\r\n" +
                "gated by the per-player rumble option in Assign). Default = B.\r\n" +
                "FR : BOUTON DE TIR (source de vibration GamePad) :\r\n" +
                "Le bouton physique Wiimote/Nunchuk que vous utilisez pour TIRER. Son appui\r\n" +
                "À l'écran déclenche la vibration d'arme (même fonction que la vibration de\r\n" +
                "gâchette en mode souris, gated sur l'option de vibration par joueur). Défaut = B.");

            // [V55] Off-Screen Reload GamePad mode (EN/FR: Reload hors-écran mode GamePad - avant TC Cover car non lié à TC)
            AddDropDownRow(
                "Off-Screen Reload",
                new[] { "Off", "Trigger", "Auto" },
                mappings.OffScreenReloadMode,
                (val) => mappings.OffScreenReloadMode = val,
                "OFF-SCREEN RELOAD (GamePad):\r\n" +
                "Off    = disabled.\r\n" +
                "Trigger = aim off-screen and press the fire button to reload once (A/B).\r\n" +
                "Auto    = one automatic reload is sent when going off-screen (no button needed).\r\n" +
                "Only 1 reload per off-screen session; aim back at the screen to re-arm.\r\n" +
                "FR : RELOAD HORS-ÉCRAN (GamePad) :\r\n" +
                "Off     = désactivé.\r\n" +
                "Trigger = viser hors écran + presser tir (A/B) pour recharger une fois.\r\n" +
                "Auto    = une recharge auto envoyée à la sortie de l'écran (sans bouton).\r\n" +
                "1 seule recharge par session ; viser l'écran pour réarmer.");

            // [V56] Physical RELOAD button (inside the Off-Screen Reload section):
            // its press triggers the reload rumble, on-screen and off-screen,
            // gated by the global Reload Rumble option + per-profile override.
            // (EN/FR: Bouton physique de RECHARGE (dans la section Off-Screen Reload) :
            // son appui déclenche la vibration de recharge, à l'écran comme hors écran,
            // gated sur l'option globale Reload Rumble + l'override par profil.)
            AddPhysicalButtonRow(
                "Reload Button",
                mappings.OffScreenReloadButton,
                "Wii2",
                (val) => mappings.OffScreenReloadButton = val,
                "RELOAD BUTTON (GamePad rumble source):\r\n" +
                "The physical Wiimote/Nunchuk button you use to RELOAD. Its press triggers\r\n" +
                "the reload rumble, on-screen AND off-screen, whether Off-Screen Reload is\r\n" +
                "enabled or not (gated by Options > Gestures > Reload Rumble + the per-profile\r\n" +
                "Reload Rumble override). Default = 2.\r\n" +
                "FR : BOUTON DE RECHARGE (source de vibration GamePad) :\r\n" +
                "Le bouton physique Wiimote/Nunchuk que vous utilisez pour RECHARGER. Son appui\r\n" +
                "déclenche la vibration de recharge, À l'écran comme HORS écran, que le\r\n" +
                "Off-Screen Reload soit activé ou non (gated sur Options > Gestures > Reload\r\n" +
                "Rumble + l'override par profil). Défaut = 2.");

            // [V55y] Reload rumble overrides (GamePad side) (EN/FR: Overrides vibration recharge - côté GamePad)
            AddCheckBoxRow(
                "Reload Rumble",
                mappings.ReloadRumbleOverride == 1 || (mappings.ReloadRumbleOverride == -1 && Options.Instance.ReloadRumbleEnabled),
                (val) => mappings.ReloadRumbleOverride = val ? 1 : 0,
                "RELOAD RUMBLE (GamePad profile):\r\n" +
                "ON/OFF override of Options > Gestures > Reload Rumble for this profile.\r\n" +
                "Plays the rumble pattern on every GamePad off-screen reload (Trigger/Auto).\r\n" +
                "FR : VIBRATION RECHARGE (profil GamePad) :\r\n" +
                "Override ON/OFF de Options > Gestures > Vibration recharge pour ce profil.\r\n" +
                "Joue le motif de vibration à chaque recharge hors écran GamePad (Trigger/Auto).");
            AddDropDownRow(
                "Reload Rumble Style",
                new[] { "Default", "Ratchet", "Short", "Long", "Custom" },
                (mappings.ReloadRumbleStyleOverride >= 0 ? mappings.ReloadRumbleStyleOverride + 1 : 0),
                (val) => mappings.ReloadRumbleStyleOverride = val - 1,
                "Rumble style override: Default = follow Options > Gestures.\r\n" +
                "Custom = the tic pattern defined in Options > Gestures (Ticks / ON / OFF).\r\n" +
                "FR : Override du style de vibration : Default = suivre Options > Gestures.\r\n" +
                "Custom = le motif de tics défini dans Options > Gestures (Ticks / ON / OFF).");
            AddDropDownRow(
                "Reload Rumble Intensity",
                new[] { "Default", "20%", "40%", "60%", "80%", "100%" },
                (mappings.ReloadRumbleIntensityOverride >= 0
                    ? Array.IndexOf(new[] { 20, 40, 60, 80, 100 }, ((mappings.ReloadRumbleIntensityOverride / 20) * 20)) + 1
                    : 0),
                (val) => mappings.ReloadRumbleIntensityOverride = (val <= 0 ? -1 : val * 20),
                "Rumble intensity override: Default = follow Options > Gestures.\r\n" +
                "FR : Override de l'intensité de vibration : Default = suivre Options > Gestures.");

            // [V54] TC Cover auto-reload (Time Crisis) — per profile, same zone as XInput
            // (EN/FR: Planque TC auto-rechargement (Time Crisis) — par profil, même zone que XInput)
            AddCheckBoxRow("TC Cover Auto-Reload", mappings.TCCoverReload, (val) => mappings.TCCoverReload = val,
                "Time Crisis cover mode (CORRECTED [V55]): Aiming ON-screen HOLDS the TC button (exit cover). Aiming OFF-screen RELEASES it (enter cover).\r\n" +
                "Aiming OFF-screen RELEASES it (= return to cover/planque).\r\n" +
                "In TC games you hold the pedal to aim and shoot, then release to hide.\r\n" +
                "FR : Mode planque Time Crisis (CORRIGE [V55]) : viser l'ECRAN MAINTIENT ; viser HORS écran MAINTIENT l'entrée recharge/planque ;\r\n" +
                "viser à nouveau l'écran la relâche. En mode GamePad, maintient le bouton gamepad mappé.\r\n" +
                "Bouton Auto = le bouton wiimote mappé clic droit côté souris (repli B).");
            AddTcButtonRow("TC Reload Button", mappings.TCCoverButton, (val) => mappings.TCCoverButton = val,
                "Physical Wiimote/Nunchuk button held by the TC cover (A, B, 1, 2, +, -, C, Z).\r\n" +
                "Auto = the button mapped to right-click on the mouse side, applied to the GamePad\r\n" +
                "side (fallback B). Lets you override the reload button for games that use another.\r\n" +
                "FR : Bouton physique Wiimote/Nunchuk maintenu par la planque TC (A, B, 1, 2, +, -, C, Z).\r\n" +
                "Auto = le bouton mappé clic droit côté souris, appliqué côté GamePad (repli B).\r\n" +
                "Permet de contourner un changement de bouton pour certains jeux.");

            // [V55] TC Bi-directional Pedal (EN/FR: Pedale TC bi-directionnelle)
            AddCheckBoxRow("TC Bi-Pedal (DPad L/R)", mappings.TCBiPedal, (val) => mappings.TCBiPedal = val,
                "TC BI-DIRECTIONAL PEDAL [V55] (TC3/TC4/TC5): DPad Left/Right = two TC pedals.\r\n" +
                "Press L or R while ON-screen: holds that pedal direction.\r\n" +
                "Go OFF-screen: releases. Press opposite side: switches.\r\n" +
                "Incompatible with TC Cover mode.\r\n" +
                "FR : DPad G/D = deux pedales TC independantes. A l'ecran -> maintient direction.\r\n" +
                "Hors ecran -> relache. Cote oppose -> bascule. Incompatible avec TC Cover.");
            AddTcBiPedalButtonRow("TC Pedal Left", mappings.TCBiPedalLeftButton, "WiiLeft", (val) => mappings.TCBiPedalLeftButton = val, "Physical Wiimote/Nunchuk button for LEFT TC pedal (default: D-Pad Left).\r\nFR : Bouton physique Wiimote/Nunchuk pour la pedale TC GAUCHE (defaut: D-Pad Gauche).");
            AddTcBiPedalButtonRow("TC Pedal Right", mappings.TCBiPedalRightButton, "WiiRight", (val) => mappings.TCBiPedalRightButton = val, "Physical Wiimote/Nunchuk button for RIGHT TC pedal (default: D-Pad Right).\r\nFR : Bouton physique Wiimote/Nunchuk pour la pedale TC DROITE (defaut: D-Pad Droit).");

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
            AddMappingRow("NunJoyUp", "Joystick Up", mappings.NunchukUp, (val) => mappings.NunchukUp = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukUpHybrid, (val) => mappings.NunchukUpHybrid = val);
            AddMappingRow("NunJoyDown", "Joystick Down", mappings.NunchukDown, (val) => mappings.NunchukDown = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukDownHybrid, (val) => mappings.NunchukDownHybrid = val);
            AddMappingRow("NunJoyLeft", "Joystick Left", mappings.NunchukLeft, (val) => mappings.NunchukLeft = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukLeftHybrid, (val) => mappings.NunchukLeftHybrid = val);
            AddMappingRow("NunJoyRight", "Joystick Right", mappings.NunchukRight, (val) => mappings.NunchukRight = val, mappings.HybridTriggerButton, (val) => { mappings.HybridTriggerButton = val; LoadCurrentMappings(); }, mappings.NunchukRightHybrid, (val) => mappings.NunchukRightHybrid = val);

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

            // [V57o4] EN: RUNTIME populations only (tab/player switch, profile load):
            //     the rows above were created with UNSCALED geometry - scale their bounds
            //     now so they match the zoomed fonts; the flow panel re-positions them.
            //     Constructor call (initial=true) skips this: ApplyForm scales the whole
            //     page tree exactly once.
            //     FR: Populations RUNTIME uniquement (changement d'onglet/joueur,
            //     chargement de profil) : les lignes ci-dessus ont été créées avec une
            //     géométrie NON scalée - scale leurs bornes maintenant pour qu'elles
            //     correspondent aux polices zoomées ; le panneau flow les repositionne.
            //     L'appel du constructeur (initial=true) saute ceci : ApplyForm scale tout
            //     l'arbre de la page exactement une fois.
            if (!initial)
            {
                WiimoteGun.UI.UiScaler.ScaleChildren(flowLayoutPanelButtons);
            }

            // [V57o5] EN: Re-apply the font zoom to the rows on EVERY population (see
            //     MappingControl.LoadCurrentMappings): fresh unscaled fonts must be
            //     scaled right here - idempotent via the UiScaler font registry.
            //     FR: Ré-applique le zoom des polices aux lignes à CHAQUE population (voir
            //     MappingControl.LoadCurrentMappings) : les polices fraîches non scalées
            //     doivent l'être ici même - idempotent via le registre de polices d'UiScaler.
            WiimoteGun.UI.UiScaler.ApplyFonts(flowLayoutPanelButtons);

            // [V57p] EN: Suppress the mouse wheel on the freshly built rows' combo/UpDown
            //     controls (idempotent - see WheelSuppressor): a page scroll must never
            //     change a mapping selection silently.
            //     FR: Supprime la molette sur les ComboBox/UpDown des lignes fraîchement
            //     construites (idempotent - voir WheelSuppressor) : défiler la page ne
            //     doit plus jamais changer une sélection de mapping en silence.
            WiimoteGun.UI.WheelSuppressor.ApplyTo(flowLayoutPanelButtons);
        }

        private void Open3DVisualizer()
        {
            try {
                var formType = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == "GyroVisualizerForm");
                
                if (formType != null)
                {
                    Form form = (Form)Activator.CreateInstance(formType);
                    form.Show(this.FindForm()); // EN/FR: Assurer le focus au premier plan (Ensure foreground focus)
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
        // [V55] TC Bi-Pedal button choices (EN/FR: Choix des boutons TC Bi-Pedal)
        private static readonly string[] TcBiPedalDisplayNames = { "D-Pad Left", "D-Pad Right", "Auto (Right-Click)", "A Button", "B Button", "1 Button", "2 Button", "+ (Plus)", "- (Minus)", "D-Pad Up", "D-Pad Down", "C Button", "Z Button" };
        private static readonly string[] TcBiPedalIds = { "WiiLeft", "WiiRight", "auto", "WiiA", "WiiB", "WiiOne", "WiiTwo", "WiiPlus", "WiiMinus", "WiiUp", "WiiDown", "NunC", "NunZ" };

        /// <summary>
        /// EN: [V55] Row with a dropdown for choosing the physical button for TC Left or Right pedal.
        /// FR: [V55] Ligne avec liste deroulante pour choisir le bouton physique pour la pedale TC Gauche ou Droite.
        /// </summary>
        private void AddTcBiPedalButtonRow(string labelText, string currentId, string defaultId, Action<string> setter, string tooltip = null)
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

            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Color.FromArgb(50, 50, 50);
            cbo.ForeColor = Color.White;
            cbo.Size = new Size(200, 25);
            cbo.Location = new Point(170, 2);
            foreach (string n in TcBiPedalDisplayNames) cbo.Items.Add(n);

            string effectiveId = string.IsNullOrEmpty(currentId) ? defaultId : currentId;
            if (effectiveId.Equals("DPadLeft", StringComparison.OrdinalIgnoreCase)) effectiveId = "WiiLeft";
            if (effectiveId.Equals("DPadRight", StringComparison.OrdinalIgnoreCase)) effectiveId = "WiiRight";
            int idx = Array.IndexOf(TcBiPedalIds, effectiveId);
            if (idx < 0) idx = Array.IndexOf(TcBiPedalIds, defaultId);
            if (idx < 0) idx = 0;
            cbo.SelectedIndex = idx;
            cbo.SelectedIndexChanged += (s, e) =>
            {
                int sel = cbo.SelectedIndex >= 0 ? cbo.SelectedIndex : 0;
                setter(TcBiPedalIds[sel]);
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                _toolTip.SetToolTip(lbl, tooltip);
                _toolTip.SetToolTip(cbo, tooltip);
            }

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
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

        private void AddCheckBoxRow(string labelText, bool currentValue, Action<bool> setter, string tooltip = null)
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
            chk.UseVisualStyleBackColor = true;
            chk.Size = new Size(200, 25);
            chk.Location = new Point(170, 2);
            chk.ForeColor = Color.White;

            chk.CheckedChanged += (s, e) => setter(chk.Checked);

            // [V54] Optional tooltip on both the label and the checkbox
            // (EN/FR: Bulle optionnelle sur le libellé et la case)
            if (!string.IsNullOrEmpty(tooltip))
            {
                _toolTip.SetToolTip(lbl, tooltip);
                _toolTip.SetToolTip(chk, tooltip);
            }

            row.Controls.Add(lbl);
            row.Controls.Add(chk);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        // [V54] TC reload button choices (EN/FR: Choix du bouton TC)
        private static readonly string[] TcButtonDisplayNames = { "Auto (Right-Click)", "A Button", "B Button", "1 Button", "2 Button", "+ (Plus)", "- (Minus)", "C Button", "Z Button" };
        private static readonly string[] TcButtonIds = { "auto", "WiiA", "WiiB", "WiiOne", "WiiTwo", "WiiPlus", "WiiMinus", "NunC", "NunZ" };

        /// <summary>
        /// EN: [V54] Row with a dropdown forcing the physical Wiimote/Nunchuk button used
        /// by the TC cover auto-reload ("auto" = right-click mapping).
        /// FR: [V54] Ligne avec une liste déroulante forçant le bouton physique
        /// Wiimote/Nunchuk utilisé par la planque TC ("auto" = mapping clic droit).
        /// </summary>
        private void AddTcButtonRow(string labelText, string currentId, Action<string> setter, string tooltip = null)
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

            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Color.FromArgb(50, 50, 50);
            cbo.ForeColor = Color.White;
            cbo.Size = new Size(200, 25);
            cbo.Location = new Point(170, 2);
            foreach (string n in TcButtonDisplayNames) cbo.Items.Add(n);

            int idx = Array.IndexOf(TcButtonIds, string.IsNullOrEmpty(currentId) ? "auto" : currentId);
            if (idx < 0) idx = 0;
            cbo.SelectedIndex = idx;
            cbo.SelectedIndexChanged += (s, e) =>
            {
                int sel = cbo.SelectedIndex >= 0 ? cbo.SelectedIndex : 0;
                setter(TcButtonIds[sel]);
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                _toolTip.SetToolTip(lbl, tooltip);
                _toolTip.SetToolTip(cbo, tooltip);
            }

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        // [V56] Physical rumble-source button choices (no "auto": a real physical button)
        // (EN/FR: Choix de boutons physiques sources de vibration (pas d'« auto » : un vrai bouton physique))
        private static readonly string[] PhysButtonDisplayNames = { "A Button", "B Button", "1 Button", "2 Button", "+ (Plus)", "- (Minus)", "C Button", "Z Button" };
        private static readonly string[] PhysButtonIds = { "WiiA", "WiiB", "Wii1", "Wii2", "WiiPlus", "WiiMinus", "NunchukC", "NunchukZ" };

        /// <summary>
        /// EN: [V56] Row with a dropdown selecting the physical Wiimote/Nunchuk button used
        /// as a rumble source (fire rumble / reload rumble) in GamePad mode.
        /// FR: [V56] Ligne avec une liste déroulante sélectionnant le bouton physique
        /// Wiimote/Nunchuk utilisé comme source de vibration (tir / recharge) en mode GamePad.
        /// </summary>
        private void AddPhysicalButtonRow(string labelText, string currentId, string defaultId, Action<string> setter, string tooltip = null)
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

            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Color.FromArgb(50, 50, 50);
            cbo.ForeColor = Color.White;
            cbo.Size = new Size(200, 25);
            cbo.Location = new Point(170, 2);
            foreach (string n in PhysButtonDisplayNames) cbo.Items.Add(n);

            int idx = Array.IndexOf(PhysButtonIds, string.IsNullOrEmpty(currentId) ? defaultId : currentId);
            if (idx < 0) idx = Array.IndexOf(PhysButtonIds, defaultId);
            cbo.SelectedIndex = idx;
            cbo.SelectedIndexChanged += (s, e) =>
            {
                int sel = cbo.SelectedIndex >= 0 ? cbo.SelectedIndex : 0;
                setter(PhysButtonIds[sel]);
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                _toolTip.SetToolTip(lbl, tooltip);
                _toolTip.SetToolTip(cbo, tooltip);
            }

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
            flowLayoutPanelButtons.Controls.Add(row);
        }

        /// <summary>
        /// EN: [V55] Generic drop-down row for selecting an int index from a list of display names.
        /// FR: [V55] Ligne générique avec liste déroulante pour choisir un entier parmi des libellés.
        /// </summary>
        private void AddDropDownRow(string labelText, string[] displayNames, int currentIndex, Action<int> setter, string tooltip = null)
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

            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Color.FromArgb(50, 50, 50);
            cbo.ForeColor = Color.White;
            cbo.Size = new Size(200, 25);
            cbo.Location = new Point(170, 2);
            foreach (string n in displayNames) cbo.Items.Add(n);

            int idx = currentIndex;
            if (idx < 0 || idx >= displayNames.Length) idx = 0;
            cbo.SelectedIndex = idx;
            cbo.SelectedIndexChanged += (s, e) =>
            {
                int sel = cbo.SelectedIndex >= 0 ? cbo.SelectedIndex : 0;
                setter(sel);
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                _toolTip.SetToolTip(lbl, tooltip);
                _toolTip.SetToolTip(cbo, tooltip);
            }

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
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
                // IR Calibration update (EN/FR: Mise à jour calibrage IR)
                mappings.IRLinearity = (float)numLinearity.Value;
                mappings.IROverscan = (float)numOverscan.Value;
            }

            // Buttons are updated in real-time via setter delegates in AddMappingRow
            // So we just need to save options
            Options.Instance.Save();

            // [V57g] EN: Route the "Use XInput" change through THE shared swap function
            //     (same one as the tile modal's button) - centralizes the API-change
            //     trace; the device swap itself is performed by the single runtime
            //     detection (GamePadOutputApiMismatch -> ReinitGamepadOutput).
            //     FR: Faire passer le changement « Use XInput » par LA fonction de
            //     bascule partagée (la même que le bouton de la modale en tuiles) -
            //     centralise la trace du changement d'API ; l'échange de device lui-même
            //     est fait par la détection runtime unique (GamePadOutputApiMismatch ->
            //     ReinitGamepadOutput).
            if (mappings != null)
            {
                Program.SetGamePadOutputApi(new[] { _currentPlayer }, mappings.UseXInput, save: false);
            }

            // [V31+V33] Non-blocking confirmation: mappings are in settings.cfg, profiles
            // are written by "Save Profile".
            // (EN/FR: Confirmation non bloquante : mappings dans settings.cfg, profils
            // écrits via "Save Profile".)
            ShowToast($"Saved to settings.cfg (Player {_currentPlayer}: mappings + IR calibration)");
            
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
