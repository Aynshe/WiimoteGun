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

        // [V34] Transient non-blocking toast (EN/FR: Bandeau transitoire non bloquant)
        private TransientToast _toast;

        // [V48] Association currently displayed for the loaded/selected profile.
        // The checkboxes reflect THIS association (per-profile), not only the foreground exe.
        // (EN/FR: Association actuellement affichée pour le profil chargé/sélectionné.
        // Les cases reflètent CETTE association (par profil), pas seulement l'exe au premier plan.)
        private GameProfileMapping _linkedMapping;

        // [V50] Remembered manual selection (advanced association) - avoids double prompts
        // (EN/FR: Sélection manuelle mémorisée (association avancée) - évite les demandes en double)
        private string _manualGameName;
        private bool _manualIsFolder;
        private string _manualSystemName;

        // [V42] Emulator link: profile bound per GAME (from EmulationStation)
        // [V45] chkIsEmulator / chkIsFolder are Designer controls now (visible/editable in VS)
        // (EN/FR: Lien émulateur : profil lié par JEU. Les cases sont des contrôles Designer.)
        private ToolTip _esToolTips = new ToolTip();

        private void WireEmulatorCheckboxes()
        {
            if (chkIsEmulator == null || chkIsFolder == null) return;

            _esToolTips.SetToolTip(chkIsEmulator,
                "Check when the selected EXE is an EMULATOR.\r\n" +
                "WiimoteGun will then use the GAME (file or folder) launched by this emulator\r\n" +
                "to link and auto-load this profile for that specific game.\r\n" +
                "When no game is running, you will be asked to select the game file/folder in advance.");

            _esToolTips.SetToolTip(chkIsFolder,
                "Check when the game is launched through a FOLDER with extension (.pc/.game/.win...).\r\n" +
                "The folder name will be used as the game identity for linking and auto-load.");

            chkIsEmulator.CheckedChanged += (s, e) =>
            {
                chkIsFolder.Enabled = chkIsEmulator.Checked;
                if (!chkIsEmulator.Checked) chkIsFolder.Checked = false;
                UpdateCurrentGameLabel();
            };
            chkIsFolder.CheckedChanged += (s, e) =>
            {
                // [V50] Changing the folder expectation resets the remembered selection
                // (EN/FR: Changer l'attente dossier réinitialise la sélection mémorisée)
                _manualGameName = null;
                _manualSystemName = null;
                UpdateCurrentGameLabel();
            };

            // [V50] System name: auto-checked during an ES game-start when Emulator is on;
            // manually checked outside a session, it UNLOCKS the advanced association
            // (system -> game file/folder) without a game in progress.
            // (EN/FR: System name : auto-cochée pendant un game-start ES quand Emulator est
            // coché ; cochée manuellement hors session, elle DÉVERROUILLE l'association
            // avancée (système -> fichier/dossier jeu) sans jeu en cours.)
            _esToolTips.SetToolTip(chkIsSystem,
                "Advanced association outside a game session: pick the system first, then the game file/folder.\r\n" +
                "Automatically checked during an ES game-start when 'Emulator' is checked.");
            chkIsSystem.CheckedChanged += (s, e) =>
            {
                if (_updatingCheckbox) return;

                if (chkIsSystem.Checked)
                {
                    // [V55l] If no system is remembered and not in active ES session, prompt user with EsSystemPickerForm
                    if (string.IsNullOrEmpty(_manualSystemName) && !EsScriptIntegration.HasCurrentGame)
                    {
                        string defaultSys = DetectSystemFromPath(_currentExecutablePath ?? _linkedMapping?.ExecutablePath) ?? EsScriptIntegration.LastSystem;
                        string picked = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(), "Select the game system", defaultSys);
                        if (!string.IsNullOrEmpty(picked))
                        {
                            _manualSystemName = picked;
                            UpdateExistingMappingSystem(picked);
                        }
                        else
                        {
                            // User cancelled picker -> uncheck without re-triggering handler
                            _updatingCheckbox = true;
                            chkIsSystem.Checked = false;
                            _updatingCheckbox = false;
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(_manualSystemName))
                    {
                        UpdateExistingMappingSystem(_manualSystemName);
                    }
                }
                else
                {
                    _manualGameName = null; // [V50] Reset remembered selection
                    _manualSystemName = null;
                    UpdateExistingMappingSystem(null);
                }
                UpdateAutoLoadCheckbox();
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
        /// EN: [V55l] Update the system name on an existing mapping for the active profile.
        /// FR: [V55l] Met à jour le nom de système sur le mapping existant du profil actif.
        /// </summary>
        private void UpdateExistingMappingSystem(string systemName)
        {
            string currentProfile = Program.GetActiveRemapProfile();
            if (string.IsNullOrEmpty(currentProfile) || GameProfileMappingManager.IsRootDefaultProfilePath(currentProfile))
                return;

            var existing = ResolveLinkedMapping(currentProfile);
            if (existing != null)
            {
                GameProfileMappingManager.AddMapping(existing.ExecutableName, existing.ProfilePath, existing.ExecutablePath, null,
                    existing.IsEmulator, existing.GameName, existing.GameIsFolder, systemName);
                UpdateCurrentGameLabel();
                _toast?.Show(string.IsNullOrEmpty(systemName)
                    ? $"System removed from association: {existing.ExecutableName}"
                    : $"System updated: {existing.ExecutableName} [{systemName}]");
            }
            else if (!string.IsNullOrEmpty(_currentExecutable) && chkAutoLoad != null && chkAutoLoad.Checked)
            {
                GameProfileMappingManager.AddMapping(_currentExecutable, currentProfile, _currentExecutablePath, null,
                    chkIsEmulator?.Checked == true, null, false, systemName);
                UpdateCurrentGameLabel();
            }
        }

        /// <summary>
        /// EN: [V50] Resolve the association to create (game name, folder flag, system name).
        /// In an ES game session: fully automatic. Outside: advanced flow with MEMORY
        /// (no double prompt) - system picker then game file/folder, remembered for reuse.
        /// FR: [V50] Résout l'association à créer (nom de jeu, dossier, nom de système).
        /// En session de jeu ES : entièrement automatique. Hors : flux avancé avec MÉMOIRE
        /// (pas de double demande) - sélecteur de système puis fichier/dossier, mémorisé.
        /// </summary>
        private bool TryResolveAssociationForLink(out string gameName, out bool isFolder, out string systemName)
        {
            gameName = null;
            isFolder = false;
            systemName = null;

            if (chkIsEmulator == null || !chkIsEmulator.Checked)
            {
                // [V55l] Direct executable link (non-emulator):
                // 1) ES game session: system comes automatically from scripts
                if (EsScriptIntegration.HasCurrentGame)
                {
                    systemName = EsScriptIntegration.LastSystem;
                    if (chkIsSystem != null && !string.IsNullOrEmpty(systemName))
                    {
                        _updatingCheckbox = true;
                        chkIsSystem.Checked = true;
                        _updatingCheckbox = false;
                    }
                    return true;
                }

                // 2) Outside session: if system already remembered, reuse it
                if (!string.IsNullOrEmpty(_manualSystemName))
                {
                    systemName = _manualSystemName;
                    if (chkIsSystem != null)
                    {
                        _updatingCheckbox = true;
                        chkIsSystem.Checked = true;
                        _updatingCheckbox = false;
                    }
                    return true;
                }

                // 3) Outside session: prompt user with system picker (for EsProfileTileDialog filter)
                string defaultSys = DetectSystemFromPath(_currentExecutablePath) ?? EsScriptIntegration.LastSystem;
                string picked = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(),
                    "Select the game system", defaultSys);
                if (!string.IsNullOrEmpty(picked))
                {
                    systemName = picked;
                    _manualSystemName = picked;
                    if (chkIsSystem != null)
                    {
                        _updatingCheckbox = true;
                        chkIsSystem.Checked = true;
                        _updatingCheckbox = false;
                    }
                }
                else
                {
                    // User canceled system picker: allow link without system
                    if (chkIsSystem != null)
                    {
                        _updatingCheckbox = true;
                        chkIsSystem.Checked = false;
                        _updatingCheckbox = false;
                    }
                }
                return true;
            }

            bool folderExpected = chkIsFolder != null && chkIsFolder.Checked;

            // 1) ES game session: everything comes from the scripts (auto)
            // (EN/FR: Session de jeu ES : tout vient des scripts (auto))
            if (EsScriptIntegration.HasCurrentGame)
            {
                if (chkIsSystem != null && chkIsEmulator.Checked) chkIsSystem.Checked = true; // auto-check
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

            // 2) Outside a session: advanced association with MEMORY
            // (EN/FR: Hors session : association avancée avec MÉMOIRE)
            if (!string.IsNullOrEmpty(_manualGameName))
            {
                // Reuse the remembered selection - no double prompt
                // (EN/FR: Réutiliser la sélection mémorisée - pas de double demande)
                gameName = _manualGameName;
                isFolder = _manualIsFolder;
                systemName = _manualSystemName;
                return true;
            }

            // 2a) System first (EN/FR: Le système d'abord)
            systemName = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(),
                "Select the game system", EsScriptIntegration.LastSystem);
            if (string.IsNullOrEmpty(systemName)) return false;

            // 2b) Then the game file/folder (EN/FR: Puis le fichier/dossier du jeu)
            if (!EsScriptIntegration.TryResolveGameName(folderExpected, this.FindForm(), out gameName, out isFolder))
                return false;

            // 2c) Remember (EN/FR: Mémoriser)
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

            if (EsScriptIntegration.HasCurrentGame && folderExpected)
            {
                MessageBox.Show(this.FindForm(),
                    "The game received from EmulationStation is a FILE, not a folder.\r\nUncheck 'This is a folder' or launch a folder-based game first.",
                    "Not a folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return false;
        }

        /// <summary>EN: [V44] Game name for REMOVAL (never prompts). FR: [V44] Nom de jeu pour le RETRAIT (jamais de dialogue).</summary>
        private string GetEsGameNameForRemoval()
        {
            if (chkIsEmulator == null || !chkIsEmulator.Checked || !EsScriptIntegration.HasCurrentGame)
                return null;
            return (chkIsFolder != null && chkIsFolder.Checked) ? EsScriptIntegration.LastGameNameRaw : EsScriptIntegration.LastGameName;
        }

        // Colors (EN/FR: Couleurs)
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);

        public MappingControl()
        {
            InitializeComponent();
            _toast = new TransientToast(this);
            if (txtProfileName != null) txtProfileName.Click += (s, e) => ShowVirtualKeyboard(txtProfileName);
            WireEmulatorCheckboxes();
            
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
            // [V47] Never wipe an already-detected game with an empty update
            // (EN/FR: Ne jamais écraser un jeu déjà détecté par une mise à jour vide)
            if (string.IsNullOrEmpty(exeName) && !string.IsNullOrEmpty(_currentExecutable))
            {
                UpdateCurrentGameLabel();
                UpdateAutoLoadCheckbox();
                return;
            }

            _currentExecutable = exeName;
            _currentExecutablePath = string.IsNullOrEmpty(exePath) ? null : exePath;
            
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

        private void RefreshProfileList(string profileToSelect = null)
        {
            // [V41] Remember the current selection so a refresh (e.g. after a Save) does not
            // silently jump to the first profile and overwrite txtProfileName — the next
            // Save would then write to the WRONG profile.
            // (EN/FR: Mémoriser la sélection courante pour qu'un refresh (ex: après un Save)
            // ne saute pas silencieusement au premier profil et n'écrase txtProfileName —
            // le Save suivant écrirait alors le MAUVAIS profil.)
            string previousSelection = comboBoxProfiles.SelectedItem?.ToString();

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
            {
                // Selection priority (EN/FR: Priorité de sélection) :
                // 1. Explicit target (e.g. profile just saved)
                // 2. Previously selected profile (if still in the list)
                // 3. Active/loaded profile (if present in this folder)
                // 4. First profile (last resort)
                string target = profileToSelect;
                if (!string.IsNullOrEmpty(target) && !target.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                    target += ".remap";

                int idx = -1;
                if (!string.IsNullOrEmpty(target))
                    idx = comboBoxProfiles.FindStringExact(target);

                if (idx == -1 && !string.IsNullOrEmpty(previousSelection))
                    idx = comboBoxProfiles.FindStringExact(previousSelection);

                if (idx == -1)
                {
                    string activeProfile = Program.GetActiveRemapProfile();
                    if (!string.IsNullOrEmpty(activeProfile))
                    {
                        string activeName = Path.GetFileName(activeProfile.Replace('\\', '/'));
                        if (!string.IsNullOrEmpty(activeName))
                            idx = comboBoxProfiles.FindStringExact(activeName);
                    }
                }

                comboBoxProfiles.SelectedIndex = idx != -1 ? idx : 0;
            }
        }

        private void UpdateCurrentGameLabel()
        {
            string statusText = "";
            bool hasLink = false;
            GameProfileMapping linkMap = null;

            // [V51] Unified display: when the ACTIVE profile carries an association,
            // show ONLY that association (no more double segment
            // "Current Game ... (not mapped) | Profile linked to ...").
            // (EN/FR: Affichage unifié : quand le profil ACTIF porte une association,
            // n'afficher QUE cette association (plus de double segment).)
            string currentProfile = Program.GetActiveRemapProfile();
            if (!string.IsNullOrEmpty(currentProfile))
                linkMap = ResolveLinkedMapping(currentProfile);

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
                // 1. Status of Current Game only when the profile has no association
                // (EN/FR: Statut du jeu actuel seulement si le profil n'a pas d'association)
                if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
                {
                    string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath,
                        EsScriptIntegration.LastGameName, EsScriptIntegration.LastGameNameRaw); // [V46] both name variants
                    if (!string.IsNullOrEmpty(mappedProfile))
                    {
                        statusText = $"Current Game '{_currentExecutable}' -> '{mappedProfile}'";
                        hasLink = true;
                        linkMap = GameProfileMappingManager.GetBestMapping(_currentExecutable, _currentExecutablePath,
                            EsScriptIntegration.LastGameName, EsScriptIntegration.LastGameNameRaw);
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
            }

            lblLinkedExe.Text = statusText;

            // [V46+V50+V51] Same colors on both mapping pages: amber when the link
            // carries a game, accent blue when linked without game, gray otherwise.
            // (EN/FR: Mêmes couleurs sur les 2 pages : ambre si le lien porte un jeu,
            // bleu accent si lié sans jeu, gris sinon.)
            bool gameScoped = linkMap != null && !string.IsNullOrEmpty(linkMap.GameName);
            lblLinkedExe.ForeColor = !hasLink ? Color.Gray
                : (gameScoped ? Color.FromArgb(255, 170, 40) : ColorAccent);
            _esToolTips.SetToolTip(lblLinkedExe, gameScoped && linkMap != null
                ? $"Linked game: {linkMap.GameName}" + (linkMap.GameIsFolder ? " (folder)" : "") +
                  (linkMap.IsEmulator ? $" | Emulator: {linkMap.ExecutableName}" : "")
                : "");
        }

        /// <summary>
        /// EN: [V48] Resolve the association of the ACTIVE profile (per-profile sync):
        /// the current-exe mapping when it matches, otherwise the JSON association of the
        /// profile itself (e.g. an emulator/game link while browsing the frontend).
        /// FR: [V48] Résout l'association du profil ACTIF (synchronisation par profil) :
        /// le mapping de l'exe courant s'il correspond, sinon l'association JSON du profil
        /// lui-même (ex: lien émulateur/jeu pendant qu'on navigue le frontend).
        /// </summary>
        private GameProfileMapping ResolveLinkedMapping(string currentProfile)
        {
            if (string.IsNullOrEmpty(currentProfile)) return null;

            // 1. Mapping of the current exe (with BOTH ES game name variants)
            var byExe = GameProfileMappingManager.GetBestMapping(_currentExecutable, _currentExecutablePath,
                EsScriptIntegration.LastGameName, EsScriptIntegration.LastGameNameRaw);
            if (byExe != null && SameProfilePath(byExe.ProfilePath, currentProfile))
                return byExe;

            // 2. Association carried by the profile itself (any exe)
            return GameProfileMappingManager.GetMappingByProfilePath(currentProfile, false);
        }

        private static bool SameProfilePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return a.Replace('\\', '/').Equals(b.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateAutoLoadCheckbox()
        {
            _updatingCheckbox = true;
             
            try
            {
                string currentProfile = Program.GetActiveRemapProfile();

                // [V52b] The ROOT default.remap can be EDITED but NEVER associated: it is
                // the base fallback mapping. Auto-Load stays grayed and unchecked for it.
                // (EN/FR: Le default.remap RACINE peut être MODIFIÉ mais JAMAIS associé :
                // c'est le mapping de base. Auto-Load reste grisé et décoché pour lui.)
                if (GameProfileMappingManager.IsRootDefaultProfilePath(currentProfile))
                {
                    _linkedMapping = null;
                    chkAutoLoad.Checked = false;
                    chkAutoLoad.Enabled = false;
                    _esToolTips.SetToolTip(chkAutoLoad,
                        "The root default.remap is the base mapping: it cannot be associated to an executable.");
                    return;
                }

                _linkedMapping = ResolveLinkedMapping(currentProfile);

                chkAutoLoad.Checked = _linkedMapping != null &&
                                     SameProfilePath(_linkedMapping.ProfilePath, currentProfile);

                // [V50] Auto-load is DISABLED (grayed) outside an ES game session, UNLESS:
                // - the profile already carries an association (so it can be unchecked), or
                // - the advanced 'System name' flow is armed (chkIsSystem checked).
                // game-end ends the session capability.
                // (EN/FR: Auto-load est DÉSACTIVÉ (grisé) hors session de jeu ES, SAUF si :
                // - le profil porte déjà une association (pour pouvoir la décocher), ou
                // - le flux avancé 'System name' est armé (chkIsSystem coché).
                // Le game-end met fin à cette capacité.)
                bool sessionActive = EsScriptIntegration.HasCurrentGame;
                bool advancedArmed = chkIsSystem != null && chkIsSystem.Checked;
                bool hasAssociation = _linkedMapping != null;
                chkAutoLoad.Enabled = (sessionActive || advancedArmed || hasAssociation);
                _esToolTips.SetToolTip(chkAutoLoad,
                    !chkAutoLoad.Enabled
                        ? "Requires an active game (ES game-start) - or check 'System name' for an advanced association"
                        : (sessionActive ? "Auto-Load for the current game session" : "Auto-Load (existing association or advanced mode)"));

                // [V46+V48+V51c] Sync 'Emulator' and 'This is a folder' ONLY when the
                // association actually carries an emulator link (or when there is NO
                // association, which clears them). A PLAIN exe association must NOT touch
                // the boxes: the user may be building the advanced association right now —
                // checking 'System name' after 'Emulator' previously RESET 'Emulator' from
                // the bare JSON link (IsEmulator=false).
                // (EN/FR: Synchroniser 'Emulator'/'This is a folder' SEULEMENT si
                // l'association porte un lien émulateur (ou s'il n'y a AUCUNE association,
                // ce qui les nettoie). Une association d'exe SIMPLE ne touche PAS les
                // cases : cocher 'System name' après 'Emulator' réinitialisait avant
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
            finally
            {
                _updatingCheckbox = false;
            }
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
            int labelWidth = 95;
            int valueWidth = 120;
            int spacing = 22;
            int columnSpacing = 35;
            
            int column1Width = labelWidth + valueWidth;
            int column2Width = labelWidth + valueWidth;
            int totalWidth = column1Width + columnSpacing + column2Width;
            int startX = (panelWidth - totalWidth) / 2;

            // ===== [V55] Off-Screen Reload / TC Cover section (per profile) — TOP of page =====
            // (EN/FR: Section Rechargement hors-écran / Planque TC — en début de page)
            int tcY = 12;
            Label lblOsHeader = new Label
            {
                Text = "━━ Off-Screen Reload / TC Cover (per profile) ━━",
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.0F, FontStyle.Bold),
                Location = new Point(startX, tcY),
                Size = new Size(totalWidth, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblOsHeader);
            tcY += 24;

            PlayerMappings osMappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);
            ToolTip ttOs = new ToolTip();

            // 1) Off-Screen Reload (per-profile override of the global option)
            //    (EN/FR: Override par profil de l'option globale - déverrouillé et compact [V55g])
            CheckBox chkOsReload = new CheckBox
            {
                Text = "Off-Screen Reload",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(startX, tcY),
                Size = new Size(130, 20),
                UseVisualStyleBackColor = true
            };

            bool isDefault = IsDefaultProfileActive();
            if (isDefault)
            {
                chkOsReload.Checked = Options.Instance.EnableOffScreenReload;
            }
            else if (osMappings != null)
            {
                chkOsReload.Checked = osMappings.OffScreenReloadOverride == 1 ||
                                      (osMappings.OffScreenReloadOverride == -1 && Options.Instance.EnableOffScreenReload);
            }
            else
            {
                chkOsReload.Checked = Options.Instance.EnableOffScreenReload;
            }

            // EN/FR: Always unlocked and accessible (Toujours accessible et déverrouillé)
            chkOsReload.Enabled = true;

            chkOsReload.CheckedChanged += (s, e) =>
            {
                if (IsDefaultProfileActive())
                {
                    Options.Instance.EnableOffScreenReload = chkOsReload.Checked;
                }
                if (osMappings != null)
                {
                    osMappings.OffScreenReloadOverride = chkOsReload.Checked ? 1 : 0;
                }
            };

            ttOs.SetToolTip(chkOsReload,
                "Off-screen reload ON/OFF for this profile.\r\n" +
                "Aim off-screen and pull the trigger to send the reload action (right-click).\r\n" +
                "FR : Reload hors-écran ON/OFF pour ce profil.\r\n" +
                "Viser hors écran et presser la gâchette envoie l'action recharge (clic droit).");
            panel.Controls.Add(chkOsReload);

            // [V55] ComboBox Trigger/Auto next to Off-Screen Reload checkbox
            // (EN/FR: ComboBox Trigger/Auto rapprochée immédiatement de la case Off-Screen Reload [V55g])
            string[] osAutoNames = { "Trigger", "Auto" };
            ComboBox cboOsAuto = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 138, tcY - 1),
                Size = new Size(85, 22),
                Enabled = chkOsReload.Checked
            };
            cboOsAuto.Items.AddRange(osAutoNames);

            int osAutoIdx = 0; // default Trigger
            if (osMappings != null)
            {
                if (osMappings.OffScreenAutoOverride == 1) osAutoIdx = 1;
                else if (osMappings.OffScreenAutoOverride == 0) osAutoIdx = 0;
                else osAutoIdx = Options.Instance.OffScreenReloadAuto ? 1 : 0; // follow global
            }
            else
            {
                osAutoIdx = Options.Instance.OffScreenReloadAuto ? 1 : 0;
            }
            cboOsAuto.SelectedIndex = osAutoIdx;

            cboOsAuto.SelectedIndexChanged += (s, e) =>
            {
                if (IsDefaultProfileActive())
                {
                    Options.Instance.OffScreenReloadAuto = (cboOsAuto.SelectedIndex == 1);
                }
                if (osMappings != null)
                {
                    osMappings.OffScreenAutoOverride = cboOsAuto.SelectedIndex; // 0=Trigger, 1=Auto
                }
            };
            chkOsReload.CheckedChanged += (s, e) =>
            {
                cboOsAuto.Enabled = chkOsReload.Checked;
            };

            ttOs.SetToolTip(cboOsAuto,
                "Trigger = press the fire button off-screen to reload once (manual).\r\n" +
                "Auto    = one reload is sent automatically when going off-screen.\r\n" +
                "FR : Trigger = presser le bouton de tir hors écran pour recharger (manuel).\r\n" +
                "Auto    = une recharge automatique est envoyée à la sortie de l'écran.");
            panel.Controls.Add(cboOsAuto);
            tcY += 26;

            // [V55y] Reload Rumble override (per-profile override of the global option)
            // (EN/FR: Override par profil de la vibration rechargement)
            CheckBox chkReloadRumble = new CheckBox
            {
                Text = "Reload Rumble",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(startX, tcY),
                Size = new Size(130, 20),
                UseVisualStyleBackColor = true
            };

            if (IsDefaultProfileActive())
            {
                chkReloadRumble.Checked = Options.Instance.ReloadRumbleEnabled;
            }
            else if (osMappings != null)
            {
                chkReloadRumble.Checked = osMappings.ReloadRumbleOverride == 1 ||
                                          (osMappings.ReloadRumbleOverride == -1 && Options.Instance.ReloadRumbleEnabled);
            }
            else
            {
                chkReloadRumble.Checked = Options.Instance.ReloadRumbleEnabled;
            }

            chkReloadRumble.CheckedChanged += (s, e) =>
            {
                if (IsDefaultProfileActive())
                {
                    Options.Instance.ReloadRumbleEnabled = chkReloadRumble.Checked;
                }
                if (osMappings != null)
                {
                    osMappings.ReloadRumbleOverride = chkReloadRumble.Checked ? 1 : 0;
                }
            };

            ttOs.SetToolTip(chkReloadRumble,
                "Reload rumble ON/OFF for this profile (overrides Options > Gestures).\r\n" +
                "Plays a rumble pattern on every reload (off-screen auto/trigger, physical\r\n" +
                "reload button, shake reload, GamePad off-screen reloads).\r\n" +
                "FR : Vibration recharge ON/OFF pour ce profil (surcharge Options > Gestures).\r\n" +
                "Joue un motif de vibration à chaque recharge (auto/gâchette hors écran,\r\n" +
                "bouton reload physique, shake, recharges GamePad hors écran).");
            panel.Controls.Add(chkReloadRumble);

            // [V55y] Reload rumble STYLE combo (EN/FR: Combo style de vibration recharge)
            string[] rrStyleNames = { "Default", "Ratchet", "Short", "Long", "Custom" };
            ComboBox cboRrStyle = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 138, tcY - 1),
                Size = new Size(105, 22)
            };
            cboRrStyle.Items.AddRange(rrStyleNames);

            int rrStyleIdx;
            if (osMappings != null && osMappings.ReloadRumbleStyleOverride >= 0)
            {
                rrStyleIdx = osMappings.ReloadRumbleStyleOverride + 1; // 0..2 -> 1..3
            }
            else
            {
                rrStyleIdx = 0; // Default = follow global
            }
            cboRrStyle.SelectedIndex = rrStyleIdx;

            cboRrStyle.SelectedIndexChanged += (s, e) =>
            {
                if (osMappings != null)
                {
                    osMappings.ReloadRumbleStyleOverride = cboRrStyle.SelectedIndex - 1; // -1 = Default/global
                }
                if (IsDefaultProfileActive() && cboRrStyle.SelectedIndex > 0)
                {
                    Options.Instance.ReloadRumbleStyle = cboRrStyle.SelectedIndex - 1;
                }
            };
            ttOs.SetToolTip(cboRrStyle,
                "Rumble style for this profile: Default = follow Options > Gestures.\r\n" +
                "Custom = the tic pattern defined in Options > Gestures (Ticks / ON / OFF).\r\n" +
                "FR : Style de vibration pour ce profil : Default = suivre Options > Gestures.\r\n" +
                "Custom = le motif de tics défini dans Options > Gestures (Ticks / ON / OFF).");
            panel.Controls.Add(cboRrStyle);
            tcY += 26;

            // [V55y] Reload rumble INTENSITY combo (EN/FR: Combo intensité de vibration recharge)
            Label lblRrIntensity = new Label
            {
                Text = "Rumble Intensity:",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.0F),
                Location = new Point(startX, tcY + 2),
                Size = new Size(130, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            int[] rrIntensityValues = { -1, 20, 40, 60, 80, 100 };
            ComboBox cboRrIntensity = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 138, tcY),
                Size = new Size(105, 22)
            };
            cboRrIntensity.Items.AddRange(new object[] { "Default", "20%", "40%", "60%", "80%", "100%" });

            int rrIntensityIdx = 0; // Default = follow global
            if (osMappings != null && osMappings.ReloadRumbleIntensityOverride >= 0)
            {
                int v = osMappings.ReloadRumbleIntensityOverride;
                rrIntensityIdx = Array.IndexOf(rrIntensityValues, v >= 0 ? ((v / 20) * 20) : v);
                if (rrIntensityIdx < 0) rrIntensityIdx = 3; // snap to 60% bucket
            }
            cboRrIntensity.SelectedIndex = rrIntensityIdx;

            cboRrIntensity.SelectedIndexChanged += (s, e) =>
            {
                if (osMappings != null)
                {
                    osMappings.ReloadRumbleIntensityOverride = rrIntensityValues[cboRrIntensity.SelectedIndex >= 0 ? cboRrIntensity.SelectedIndex : 0];
                }
            };
            ttOs.SetToolTip(lblRrIntensity,
                "Rumble intensity for this profile: Default = follow Options > Gestures.\r\n" +
                "FR : Intensité de vibration pour ce profil : Default = suivre Options > Gestures.");
            ttOs.SetToolTip(cboRrIntensity,
                "Rumble intensity for this profile: Default = follow Options > Gestures.\r\n" +
                "FR : Intensité de vibration pour ce profil : Default = suivre Options > Gestures.");

            panel.Controls.Add(lblRrIntensity);
            panel.Controls.Add(cboRrIntensity);
            tcY += 26;

            // 2) TC Cover auto-reload (Time Crisis)
            //    (EN/FR: Planque TC auto-rechargement (Time Crisis))
            CheckBox chkTcCover = new CheckBox
            {
                Text = "TC Cover Auto-Reload (Time Crisis)",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(startX, tcY),
                Size = new Size(280, 20),
                UseVisualStyleBackColor = true,
                Checked = osMappings != null && osMappings.TCCoverReload
            };
            if (osMappings != null)
            {
                chkTcCover.CheckedChanged += (s, e) => { osMappings.TCCoverReload = chkTcCover.Checked; };
            }
            ttOs.SetToolTip(chkTcCover,
                "Time Crisis cover mode (CORRECTED [V55]):\r\n" +
                "Aiming ON-screen HOLDS the cover/planque input (= exit cover, shoot).\r\n" +
                "Aiming OFF-screen RELEASES it (= return to cover/planque).\r\n" +
                "This is the OPPOSITE of standard reload: in TC games you hold the pedal\r\n" +
                "to aim and shoot, then release to hide again.\r\n" +
                "Inhibits global Off-Screen Reload and Auto Off-Screen for this profile.\r\n" +
                "FR : Mode planque Time Crisis (CORRIGÉ [V55]) :\r\n" +
                "Viser l'ÉCRAN MAINTIENT l'entrée planque (= sortie planque, tirer).\r\n" +
                "Viser HORS écran RELÂCHE (= retour en planque).\r\n" +
                "C'est l'INVERSE du reload classique : dans TC on maintient la pédale pour\r\n" +
                "viser/tirer puis on relâche pour se cacher. Inhibe Off-Screen Reload global.");
            panel.Controls.Add(chkTcCover);
            tcY += 26;

            // 3) TC reload button dropdown (forced physical button)
            //    (EN/FR: Liste déroulante du bouton TC (bouton physique forcé))
            string[] tcNames = { "Auto (Right-Click)", "A Button", "B Button", "1 Button", "2 Button", "+ (Plus)", "- (Minus)", "C Button", "Z Button" };
            string[] tcIds = { "auto", "WiiA", "WiiB", "WiiOne", "WiiTwo", "WiiPlus", "WiiMinus", "NunC", "NunZ" };

            Label lblTcBtn = new Label
            {
                Text = "TC Reload Button:",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.0F),
                Location = new Point(startX, tcY + 2),
                Size = new Size(120, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            ComboBox cboTcBtn = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 125, tcY),
                Size = new Size(180, 22)
            };
            foreach (string n in tcNames) cboTcBtn.Items.Add(n);
            int tcIdx = Array.IndexOf(tcIds, string.IsNullOrEmpty(osMappings?.TCCoverButton) ? "auto" : osMappings.TCCoverButton);
            if (tcIdx < 0) tcIdx = 0;
            cboTcBtn.SelectedIndex = tcIdx;
            if (osMappings != null)
            {
                cboTcBtn.SelectedIndexChanged += (s, e) =>
                {
                    int sel = cboTcBtn.SelectedIndex >= 0 ? cboTcBtn.SelectedIndex : 0;
                    osMappings.TCCoverButton = tcIds[sel];
                };
            }
            ttOs.SetToolTip(cboTcBtn,
                "Physical Wiimote/Nunchuk button used by the TC cover action.\r\n" +
                "Auto = right-click mapping (existing behavior). Lets you override the\r\n" +
                "reload button for games that use a different one.\r\n" +
                "FR : Bouton physique Wiimote/Nunchuk utilisé par la planque TC.\r\n" +
                "Auto = mapping clic droit (comportement existant). Permet de contourner\r\n" +
                "un changement de bouton pour certains jeux.");

            panel.Controls.Add(lblTcBtn);
            panel.Controls.Add(cboTcBtn);
            tcY += 28;

            // 4) [V55] TC Bi-Pedal (TC4, TC5 2-pedal games)
            //    (EN/FR: Pédale TC bi-directionnelle (jeux à 2 pédales))
            CheckBox chkTcBiPedal = new CheckBox
            {
                Text = "TC Bi-Pedal (2 Pedals - Time Crisis 4/5)",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(startX, tcY),
                Size = new Size(300, 20),
                UseVisualStyleBackColor = true,
                Checked = osMappings != null && osMappings.TCBiPedal
            };
            if (osMappings != null)
            {
                chkTcBiPedal.CheckedChanged += (s, e) => { osMappings.TCBiPedal = chkTcBiPedal.Checked; };
            }
            ttOs.SetToolTip(chkTcBiPedal,
                "TC BI-DIRECTIONAL PEDAL [V55] (2-Pedal TC games):\r\n" +
                "Select two buttons for Left and Right pedal.\r\n" +
                "Pressing Left or Right while aiming ON-screen holds that direction.\r\n" +
                "Aiming OFF-screen releases without changing state. Pressing the other pedal switches.\r\n" +
                "FR : Pédale bi-directionnelle TC [V55] (jeux TC à 2 pédales) :\r\n" +
                "Choisir deux boutons pour la pédale gauche et droite.\r\n" +
                "Appuyer en visant l'écran maintient la direction. Hors écran relâche.");
            panel.Controls.Add(chkTcBiPedal);
            tcY += 28; // EN/FR: Espace aéré pour ne pas tronquer les ComboBox du dessous

            // 5) [V55] Two DropDowns for Pedal Left and Pedal Right
            //    (EN/FR: Deux listes déroulantes pour Pédale Gauche et Pédale Droite)
            string[] tcPedalNames = { "D-Pad Left", "D-Pad Right", "Auto (Right-Click)", "A Button", "B Button", "1 Button", "2 Button", "+ (Plus)", "- (Minus)", "D-Pad Up", "D-Pad Down", "C Button", "Z Button" };
            string[] tcPedalIds = { "WiiLeft", "WiiRight", "auto", "WiiA", "WiiB", "WiiOne", "WiiTwo", "WiiPlus", "WiiMinus", "WiiUp", "WiiDown", "NunC", "NunZ" };

            Label lblPedalLeft = new Label
            {
                Text = "Pedal Left:",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.0F),
                Location = new Point(startX, tcY + 2),
                Size = new Size(68, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            ComboBox cboPedalLeft = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 70, tcY),
                Size = new Size(130, 22),
                Enabled = chkTcBiPedal.Checked
            };
            foreach (string n in tcPedalNames) cboPedalLeft.Items.Add(n);
            int pLeftIdx = Array.IndexOf(tcPedalIds, string.IsNullOrEmpty(osMappings?.TCBiPedalLeftButton) ? "WiiLeft" : osMappings.TCBiPedalLeftButton);
            if (pLeftIdx < 0) pLeftIdx = 0; // Default: D-Pad Left
            cboPedalLeft.SelectedIndex = pLeftIdx;
            if (osMappings != null)
            {
                cboPedalLeft.SelectedIndexChanged += (s, e) =>
                {
                    int sel = cboPedalLeft.SelectedIndex >= 0 ? cboPedalLeft.SelectedIndex : 0;
                    osMappings.TCBiPedalLeftButton = tcPedalIds[sel];
                };
            }

            Label lblPedalRight = new Label
            {
                Text = "Pedal Right:",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8.0F),
                Location = new Point(startX + 215, tcY + 2),
                Size = new Size(72, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            ComboBox cboPedalRight = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Location = new Point(startX + 290, tcY),
                Size = new Size(130, 22),
                Enabled = chkTcBiPedal.Checked
            };
            foreach (string n in tcPedalNames) cboPedalRight.Items.Add(n);
            int pRightIdx = Array.IndexOf(tcPedalIds, string.IsNullOrEmpty(osMappings?.TCBiPedalRightButton) ? "WiiRight" : osMappings.TCBiPedalRightButton);
            if (pRightIdx < 0) pRightIdx = 1; // Default: D-Pad Right
            cboPedalRight.SelectedIndex = pRightIdx;
            if (osMappings != null)
            {
                cboPedalRight.SelectedIndexChanged += (s, e) =>
                {
                    int sel = cboPedalRight.SelectedIndex >= 0 ? cboPedalRight.SelectedIndex : 1;
                    osMappings.TCBiPedalRightButton = tcPedalIds[sel];
                };
            }

            chkTcBiPedal.CheckedChanged += (s, e) =>
            {
                cboPedalLeft.Enabled = chkTcBiPedal.Checked;
                cboPedalRight.Enabled = chkTcBiPedal.Checked;
            };

            panel.Controls.Add(lblPedalLeft);
            panel.Controls.Add(cboPedalLeft);
            panel.Controls.Add(lblPedalRight);
            panel.Controls.Add(cboPedalRight);
            tcY += 32; // EN/FR: Marge aérée avant le début des colonnes de boutons

            int baseButtonsY = tcY;

            // Column 1: Wiimote (EN/FR: Colonne 1: Wiimote)
            int col1X = startX;
            int col1Y = baseButtonsY;
            Label lblWiimote = new Label
            {
                Text = "━━ Wiimote ━━",
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(col1X, col1Y),
                Size = new Size(column1Width, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblWiimote);
            col1Y += spacing + 5;
            
            Action<string, ButtonAction> AddWiimoteRow = (buttonName, mapping) =>
            {
                Label lblButton = new Label
                {
                    Text = buttonName + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(col1X, col1Y),
                    Size = new Size(labelWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                Label lblMapping = new Label
                {
                    Text = GetMappingDisplay(mapping),
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Location = new Point(col1X + labelWidth + 5, col1Y),
                    Size = new Size(valueWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                panel.Controls.Add(lblButton);
                panel.Controls.Add(lblMapping);
                col1Y += spacing;
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
            int col2Y = baseButtonsY;
            
            Label lblNunchuk = new Label
            {
                Text = "━━ Nunchuk ━━",
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(col2X, col2Y),
                Size = new Size(column2Width, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblNunchuk);
            col2Y += spacing + 5;
            
            Action<string, ButtonAction> AddNunchukRow = (buttonName, mapping) =>
            {
                Label lblButton = new Label
                {
                    Text = buttonName + ":",
                    ForeColor = ColorText,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(col2X, col2Y),
                    Size = new Size(labelWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                Label lblMapping = new Label
                {
                    Text = GetMappingDisplay(mapping),
                    ForeColor = ColorAccent,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Location = new Point(col2X + labelWidth + 5, col2Y),
                    Size = new Size(valueWidth, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                panel.Controls.Add(lblButton);
                panel.Controls.Add(lblMapping);
                col2Y += spacing;
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
                    Location = new Point(col2X, col2Y),
                    Size = new Size(column2Width, 18)
                };
                panel.Controls.Add(lblNoNunchuk);
                col2Y += spacing;
            }

            int motionY = Math.Max(col1Y, col2Y) + 25; // EN/FR: Aéré proprement sous les deux colonnes
            
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
            
            // EN/FR: Bottom margin spacer to ensure comfortable scrolling
            panel.Controls.Add(new Label { Location = new Point(startX, sensY + spacing + 25), Size = new Size(totalWidth, 15) });
        }

        /// <summary>
        /// EN: [V54] True when the ACTIVE profile is the root default.remap — its
        /// Off-Screen Reload state always follows Options > Gestures (global).
        /// FR: [V54] True si le profil ACTIF est le default.remap racine — son état
        /// Off-Screen Reload suit toujours Options > Gestures (global).
        /// </summary>
        private static bool IsDefaultProfileActive()
        {
            string active = Program.GetActiveRemapProfile();
            if (string.IsNullOrEmpty(active)) return false;
            return active.Replace('\\', '/').Equals("default.remap", StringComparison.OrdinalIgnoreCase);
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
            
            string text = "";
            if (mapping.Special != SpecialAction.None)
            {
                switch (mapping.Special)
                {
                    case SpecialAction.LeftMouse: text = "🖱 Left Click"; break;
                    case SpecialAction.RightMouse: text = "🖱 Right Click"; break;
                    case SpecialAction.MiddleMouse: text = "🖱 Middle Click"; break;
                    default: text = mapping.Special.ToString(); break;
                }
            }
            else if (mapping.Key != Keys.None)
            {
                text = $"⌨ {mapping}";
            }
            else
            {
                text = "None";
            }

            // Double the ampersand so it renders correctly in WinForms Label (UseMnemonic fallback)
            // (EN/FR: Doubler l'esperluette pour affichage correct dans le Label WinForms)
            return text.Replace("&", "&&");
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

                    // [V51] Manual exe selection = explicit intent: make Auto-Load available
                    // and CHECKED directly (creates the link immediately) — same logic as
                    // the GamePad page (user request overriding the V35 no-auto-check).
                    // [V52b] Never for the ROOT default.remap (base mapping, not associable).
                    // (EN/FR: Sélection manuelle d'un exe = intention explicite : rendre
                    // Auto-Load disponible et COCHÉ directement (crée le lien immédiatement)
                    // — même logique que la page GamePad (demande utilisateur, remplace V35).
                    // [V52b] Jamais pour le default.remap RACINE (mapping de base).)
                    string activeProfile = Program.GetActiveRemapProfile();
                    if (chkAutoLoad != null &&
                        !string.IsNullOrEmpty(activeProfile) &&
                        !GameProfileMappingManager.IsRootDefaultProfilePath(activeProfile))
                    {
                        chkAutoLoad.Enabled = true;
                        if (!chkAutoLoad.Checked)
                        {
                            _updatingCheckbox = false;
                            chkAutoLoad.Checked = true; // Fires the handler -> AddMapping
                        }
                        else
                        {
                            // [V55l] If chkAutoLoad was ALREADY checked, resolve association and update link
                            string gameName; bool gameIsFolder; string systemName;
                            if (TryResolveAssociationForLink(out gameName, out gameIsFolder, out systemName))
                            {
                                GameProfileMappingManager.AddMapping(_currentExecutable, activeProfile, ofd.FileName, null,
                                    chkIsEmulator?.Checked == true, gameName, gameIsFolder, systemName);
                                UpdateCurrentGameLabel();
                            }
                        }
                    }
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
                            _toast.Show($"Folder '{folderName}' created!");
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
                        _toast.Show($"Profile '{selectedProfile}' deleted");
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
                    _toast.Show($"Hotkeys saved for Player {_currentPlayer}");
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

                // [V54] The ROOT default.remap always FOLLOWS Options > Gestures: store the
                // inherit state (-1) so the global checkboxes govern the default profile.
                // Custom profiles keep their own explicit choice.
                // (EN/FR: Le default.remap racine SUIT toujours Options > Gestures :
                // stocker l'état héritage (-1) pour que les cases globales gouvernent le
                // profil par défaut. Les profils custom gardent leur choix explicite.)
                if (string.IsNullOrEmpty(subfolder) && profileName.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    profile.P1Mappings.OffScreenReloadOverride = -1;
                    profile.P2Mappings.OffScreenReloadOverride = -1;
                    profile.P3Mappings.OffScreenReloadOverride = -1;
                    profile.P4Mappings.OffScreenReloadOverride = -1;

                    // [V55y] Reset reload rumble overrides on the default profile
                    // (EN/FR: Réinitialiser les overrides vibration recharge sur le profil par défaut)
                    profile.P1Mappings.ReloadRumbleOverride = -1;
                    profile.P2Mappings.ReloadRumbleOverride = -1;
                    profile.P3Mappings.ReloadRumbleOverride = -1;
                    profile.P4Mappings.ReloadRumbleOverride = -1;
                    profile.P1Mappings.ReloadRumbleIntensityOverride = -1;
                    profile.P2Mappings.ReloadRumbleIntensityOverride = -1;
                    profile.P3Mappings.ReloadRumbleIntensityOverride = -1;
                    profile.P4Mappings.ReloadRumbleIntensityOverride = -1;
                    profile.P1Mappings.ReloadRumbleStyleOverride = -1;
                    profile.P2Mappings.ReloadRumbleStyleOverride = -1;
                    profile.P3Mappings.ReloadRumbleStyleOverride = -1;
                    profile.P4Mappings.ReloadRumbleStyleOverride = -1;
                    Options.Instance.Save();
                }

                bool success = RemapProfileManager.SaveProfile(profileName, subfolder, profile);
                
                if (success)
                {
                    // [V48] The profile keeps its existing association (emulator/game):
                    // do NOT re-link it to the foreground exe — the V37 replacement would
                    // destroy the previous JSON association for this profile.
                    // (EN/FR: Le profil CONSERVE son association existante (émulateur/jeu) :
                    // ne pas le re-lier à l'exe au premier plan — le remplacement V37
                    // détruirait l'association JSON précédente de ce profil.)
                    bool hasExe = !string.IsNullOrEmpty(_currentExecutable);

                    // [V52b] The root default.remap is never associable: skip every link
                    // creation (and the "Link Executable?" prompt) for it.
                    // (EN/FR: Le default.remap racine n'est jamais associable : sauter
                    // toute création de lien (et le prompt "Link Executable?") pour lui.)
                    string savedProfileRelPath = string.IsNullOrEmpty(subfolder)
                        ? profileName + ".remap"
                        : Path.Combine(subfolder, profileName + ".remap");
                    bool savedIsRootDefault = GameProfileMappingManager.IsRootDefaultProfilePath(savedProfileRelPath);

                    if (_linkedMapping != null &&
                        SameProfilePath(_linkedMapping.ProfilePath, string.IsNullOrEmpty(subfolder) ? profileName + ".remap" : Path.Combine(subfolder, profileName + ".remap")))
                    {
                        // [V51b] Emulator is checked but the kept association carries NO game:
                        // the link was created at the manual exe selection (V51 auto-check)
                        // BEFORE the Emulator/System/Folder boxes were set. Resolve the
                        // advanced association NOW (system picker + game file/folder) instead
                        // of silently keeping a game-less link.
                        // (EN/FR: Emulator coché mais l'association conservée ne porte AUCUN
                        // jeu : le lien a été créé à la sélection manuelle de l'exe (auto-coche
                        // V51) AVANT les cases Emulator/System/Folder. Résoudre l'association
                        // avancée MAINTENANT (système + fichier/dossier) au lieu de conserver
                        // silencieusement un lien sans jeu.)
                        if (chkIsEmulator != null && chkIsEmulator.Checked && string.IsNullOrEmpty(_linkedMapping.GameName))
                        {
                            string esGameKept; bool esGameKeptIsFolder; string esSystemKept;
                            if (TryResolveAssociationForLink(out esGameKept, out esGameKeptIsFolder, out esSystemKept) &&
                                !string.IsNullOrEmpty(esGameKept))
                            {
                                string keptProfilePath = string.IsNullOrEmpty(subfolder)
                                    ? profileName + ".remap"
                                    : Path.Combine(subfolder, profileName + ".remap");
                                GameProfileMappingManager.AddMapping(_currentExecutable, keptProfilePath, _currentExecutablePath, null,
                                    true, esGameKept, esGameKeptIsFolder, esSystemKept);
                                _toast.Show($"Association updated: {_currentExecutable} [game: {esGameKept}{(esGameKeptIsFolder ? ", folder" : "")}]");
                            }
                            else
                            {
                                _toast.Show($"Association kept: {_linkedMapping.ExecutableName}");
                            }
                        }
                        else if (chkIsSystem != null && chkIsSystem.Checked &&
                                 (string.IsNullOrEmpty(_linkedMapping.SystemName) || (_manualSystemName != null && _manualSystemName != _linkedMapping.SystemName)))
                        {
                            // [V55l] Direct exe with System name checked: prompt/apply system
                            string sys = _manualSystemName ?? _linkedMapping.SystemName;
                            if (string.IsNullOrEmpty(sys))
                            {
                                string defaultSys = DetectSystemFromPath(_currentExecutablePath ?? _linkedMapping.ExecutablePath) ?? EsScriptIntegration.LastSystem;
                                sys = UI.Modern.Forms.EsSystemPickerForm.Show(this.FindForm(), "Select the game system", defaultSys);
                                if (!string.IsNullOrEmpty(sys)) _manualSystemName = sys;
                            }

                            if (!string.IsNullOrEmpty(sys))
                            {
                                string keptProfilePath = string.IsNullOrEmpty(subfolder)
                                    ? profileName + ".remap"
                                    : Path.Combine(subfolder, profileName + ".remap");
                                GameProfileMappingManager.AddMapping(_linkedMapping.ExecutableName, keptProfilePath, _linkedMapping.ExecutablePath ?? _currentExecutablePath, null,
                                    _linkedMapping.IsEmulator, _linkedMapping.GameName, _linkedMapping.GameIsFolder, sys);
                                _toast.Show($"Association updated: {_linkedMapping.ExecutableName} [{sys}]");
                            }
                            else
                            {
                                _toast.Show($"Association kept: {_linkedMapping.ExecutableName}");
                            }
                        }
                        else
                        {
                            _toast.Show($"Association kept: {_linkedMapping.ExecutableName}" +
                                (string.IsNullOrEmpty(_linkedMapping.GameName) ? "" : $" [game: {_linkedMapping.GameName}]") +
                                (string.IsNullOrEmpty(_linkedMapping.SystemName) ? "" : $" [{_linkedMapping.SystemName}]"));
                        }
                        UpdateCurrentGameLabel();
                    }
                    else
                    {
                        // FIX V25: Also save the Game Mapping (JSON) if Auto-Load is checked OR user confirms
                        // (EN/FR: Sauver aussi le mapping jeu (JSON) si Auto-Load coché OU utilisateur confirme)

                        bool shouldSaveMapping = false;

                        if (hasExe && !savedIsRootDefault) // [V52b] No link for the root default
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
                            // [V42-V50] Association resolution (session auto / advanced with memory)
                            // (EN/FR: Résolution d'association (session auto / avancé avec mémoire))
                            string esGame; bool esGameIsFolder; string esSystem;
                            if (!TryResolveAssociationForLink(out esGame, out esGameIsFolder, out esSystem))
                            {
                                // Canceled/invalid: skip the JSON link (EN/FR: Annulé/invalide : ignorer le lien JSON)
                            }
                            else
                            {
                                GameProfileMappingManager.AddMapping(_currentExecutable, savedProfilePath, _currentExecutablePath, null,
                                    chkIsEmulator?.Checked == true, esGame, esGameIsFolder, esSystem);
                            }
                            UpdateCurrentGameLabel(); // Refresh label to show link
                        }
                    }

                    // [V41] Re-select the SAVED profile (not the first of the list) so the
                    // name box keeps pointing at the profile being edited.
                    // (EN/FR: Re-sélectionner le profil SAUVEGARDÉ (pas le premier de la
                    // liste) pour que la zone de nom continue de désigner le profil édité.)
                    RefreshProfileList(profileName);
                    _toast.Show($"Profile '{profileName}' saved");
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

                    _toast.Show($"Profile '{profile.ProfileName}' loaded");
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
                    // [V47] Path is OPTIONAL: the foreground hook may know the exe name only.
                    // The game name comes automatically from the ES game-start while in game.
                    // (EN/FR: Le chemin est OPTIONNEL : le hook peut ne connaître que le nom
                    // de l'exe. Le nom du jeu vient automatiquement du game-start ES en jeu.)
                    if (!string.IsNullOrEmpty(_currentExecutable))
                    {
                        string exePathForLink = string.IsNullOrEmpty(_currentExecutablePath) ? null : _currentExecutablePath;
                        string currentProfile = Program.GetActiveRemapProfile();
                        if (!string.IsNullOrEmpty(currentProfile))
                        {
                            // [V52b] The root default.remap is never associable
                            // (EN/FR: Le default.remap racine n'est jamais associable)
                            if (GameProfileMappingManager.IsRootDefaultProfilePath(currentProfile))
                            {
                                _updatingCheckbox = true;
                                chkAutoLoad.Checked = false;
                                _updatingCheckbox = false;
                                return;
                            }

                            // [V42-V50] Association resolution: automatic during an ES game
                            // session, advanced flow with memory outside.
                            // (EN/FR: Résolution d'association : automatique en session de
                            // jeu ES, flux avancé avec mémoire hors session.)
                            string gameName; bool gameIsFolder; string systemName;
                            if (!TryResolveAssociationForLink(out gameName, out gameIsFolder, out systemName))
                            {
                                // Canceled/invalid: don't create a broken link
                                // (EN/FR: Annulé/invalide : ne pas créer un lien cassé)
                                _updatingCheckbox = true;
                                chkAutoLoad.Checked = false;
                                _updatingCheckbox = false;
                            }
                            else
                            {
                                GameProfileMappingManager.AddMapping(_currentExecutable, currentProfile, exePathForLink, null,
                                    chkIsEmulator?.Checked == true, gameName, gameIsFolder, systemName);
                                // Auto-load enabled
                                UpdateCurrentGameLabel();
                            }
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
                    // [V48] Unchecking removes THE DISPLAYED ASSOCIATION (its exe + game),
                    // not the foreground exe: a profile linked to an emulator+game can be
                    // unlinked while browsing the frontend.
                    // (EN/FR: Le décochage supprime L'ASSOCIATION AFFICHÉE (son exe + jeu),
                    // pas l'exe au premier plan : un profil lié à un émulateur+jeu peut être
                    // dissocié pendant qu'on navigue le frontend.)
                    if (_linkedMapping != null)
                    {
                        GameProfileMappingManager.RemoveProfileLinkForExecutable(
                            _linkedMapping.ExecutableName, _linkedMapping.ExecutablePath, _linkedMapping.GameName);
                        SimpleLogger.Instance.Info($"[V48] Removed association of profile from '{_linkedMapping.ExecutableName}'" +
                            (string.IsNullOrEmpty(_linkedMapping.GameName) ? "" : $" [game: {_linkedMapping.GameName}]"));
                        _linkedMapping = null;
                    }
                    else if (!string.IsNullOrEmpty(_currentExecutable))
                    {
                        GameProfileMappingManager.RemoveProfileLinkForExecutable(_currentExecutable, _currentExecutablePath, GetEsGameNameForRemoval());
                    }
                    // Auto-load disabled
                    UpdateCurrentGameLabel();
                    UpdateAutoLoadCheckbox();
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
