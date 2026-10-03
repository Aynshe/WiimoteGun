using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V42-V44] Tile modal for quick profile switching, opened by a long-press on PLUS
    /// (any connected Wiimote). Mouse and GamePad tabs, FOLDER navigation (folders open,
    /// profiles load), keyboard and XInput gamepad navigation, and an XInput/DInput toggle
    /// used BEFORE launching a game. Profile auto-load never changes the GamePad API:
    /// only this manual toggle or a manually loaded profile can.
    /// FR: [V42-V44] Modale en tuiles pour le changement rapide de profil, ouverte par un
    /// appui long sur PLUS (n'importe quelle wiimote connectée). Onglets Souris et GamePad,
    /// navigation par DOSSIERS (les dossiers s'ouvrent, les profils se chargent), navigation
    /// clavier et manette XInput, et bascule XInput/DInput utilisée AVANT de lancer un jeu.
    /// L'auto-load des profils ne change JAMAIS l'API GamePad : seul ce bouton ou un profil
    /// chargé manuellement le peuvent.
    /// </summary>
    internal partial class EsProfileTileDialog : Form
    {
        private readonly Color _tileBack = Color.FromArgb(45, 45, 48);
        private readonly Color _folderBack = Color.FromArgb(52, 60, 78);
        private readonly Color _tileHover = Color.FromArgb(0, 122, 204);
        private readonly Color _accent = Color.FromArgb(0, 122, 204);
        private readonly Color _tabIdle = Color.FromArgb(45, 45, 48);

        private bool _gamePadTab;
        private string _currentFolder; // null = root (EN/FR: null = racine)
        private bool _showAll; // [V49] True = show ALL profiles/folders (system filter off)

        private static bool _isOpen = false;
        private static EsProfileTileDialog _openInstance;

        // [V52] Row-based navigation state: the WHOLE dialog is navigable — tabs row,
        // nav buttons row, tile grid rows, footer swap row. No more lost selection.
        // (EN/FR: État de navigation PAR LIGNES : TOUTE la modale est navigable — ligne
        // onglets, ligne boutons nav, lignes de tuiles, ligne bascule bas de page.)
        private System.Collections.Generic.List<System.Collections.Generic.List<Control>> _navRows =
            new System.Collections.Generic.List<System.Collections.Generic.List<Control>>();
        private int _navRow = 0;
        private int _navCol = 0;

        // XInput polling (EN/FR: Sondage XInput)
        private Timer _xinputTimer;
        private int _prevXinputButtons = 0;

        public EsProfileTileDialog()
        {
            InitializeComponent();

            // [V51] VS Designer support: NEVER run runtime logic at design time
            // (BuildLogic touches the disk, Options, and starts an XInput timer — an
            // exception there is what made the Designer show an empty white window).
            // Show a static PREVIEW instead, like the other pages of the app.
            // (EN/FR: Support Designer VS : ne JAMAIS exécuter la logique runtime au
            // design time (BuildLogic touche le disque, Options, démarre un timer XInput
            // — une exception là est ce qui affichait une fenêtre blanche vide dans le
            // Designer). Afficher un PRÉVISUEL statique, comme les autres pages de l'app.)
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                BuildDesignerPreview();
                return;
            }

            BuildLogic();
        }

        /// <summary>
        /// EN: [V51] Static preview for the VS Designer (no disk/Options/timer access):
        /// styled tabs + dummy folder/profile tiles so the form is visually editable.
        /// FR: [V51] Prévisuel statique pour le Designer VS (aucun accès disque/Options/
        /// timer) : onglets stylés + tuiles factices dossier/profil pour l'édition visuelle.
        /// </summary>
        private void BuildDesignerPreview()
        {
            lblDialogTitle.Text = "PROFILES   |   System: mastersystem   |   Game: Sonic the Hedgehog";
            lblCurrentPath.Text = "(root)";

            _btnTabStyle(btnTabMouse, true);
            _btnTabStyle(btnTabGamePad, false);

            btnShowAll.Text = "🌐 All profiles";
            btnShowAll.BackColor = Color.FromArgb(50, 60, 78);

            btnApiSwap.Text = "GamePad API: XInput  [SWITCH]";
            btnApiSwap.BackColor = Color.FromArgb(0, 100, 60);

            AddPreviewTile("📁 mastersystem", _folderBack, Color.FromArgb(200, 220, 255));
            AddPreviewTile("default", _tileBack, Color.White);
            AddPreviewTile("Sonic the Hedgehog", _tileBack, Color.White);
            AddPreviewTile("HOTD4SP", _tileBack, Color.White);
            AddPreviewTile("📁 dreamcast", _folderBack, Color.FromArgb(200, 220, 255));
            AddPreviewTile("Shenmue", _tileBack, Color.White);
            AddPreviewTile("Test Profile", _tileBack, Color.White);
        }

        private void AddPreviewTile(string text, Color back, Color fore)
        {
            var tile = new Button
            {
                Size = new Size(165, 60),
                Margin = new Padding(6),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter
            };
            tile.FlatAppearance.BorderSize = 1;
            tile.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            flowTiles.Controls.Add(tile);
        }

        /// <summary>EN: True when the modal is already open. FR: True si la modale est déjà ouverte.</summary>
        public static bool IsOpen { get { return _isOpen; } }

        /// <summary>
        /// EN: Show the tile modal on the screen currently aimed by Wiimotes (or primary).
        /// Takes focus so keyboard/gamepad navigation works immediately.
        /// FR: Affiche la modale en tuiles sur l'écran actuellement visé par les wiimotes
        /// (ou principal). Prend le focus pour que la navigation clavier/manette fonctionne.
        /// </summary>
        public static void ShowModal()
        {
            if (_isOpen) return;

            var dialog = new EsProfileTileDialog();
            _isOpen = true;
            _openInstance = dialog; // [V49] Remote control entry point (EN/FR: Point d'entrée télécommande)

            try
            {
                int idx = Program.LastActiveScreenIndex;
                Screen screen = (idx >= 0 && idx < Screen.AllScreens.Length) ? Screen.AllScreens[idx] : Screen.PrimaryScreen;
                int x = screen.WorkingArea.Left + (screen.WorkingArea.Width - dialog.Width) / 2;
                int y = screen.WorkingArea.Top + (screen.WorkingArea.Height - dialog.Height) / 2;
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(x, y);
            }
            catch { }

            dialog.Show();
            dialog.Activate(); // Focus for keyboard/gamepad navigation (EN/FR: Focus pour navigation clavier/manette)
        }

        private void BuildLogic()
        {
            // Title: system only — never a stale game name
            // (EN/FR: Titre : système uniquement — jamais un nom de jeu obsolète)
            UpdateTitle(null);

            btnDialogClose.BringToFront(); // [V46] Ensure the close cross is ABOVE the docked title label
            btnDialogClose.Click += (s, e) => Close();
            btnTabMouse.Click += (s, e) => SwitchTab(false);
            btnTabGamePad.Click += (s, e) => SwitchTab(true);
            btnNavHome.Click += (s, e) => { _currentFolder = null; PopulateTiles(); };
            btnNavBack.Click += (s, e) => NavigateBack();
            btnApiSwap.Click += BtnApiSwap_Click;

            // [V49] Show-All switch: system-filtered view <-> every profile and folder
            // (EN/FR: Bascule Tout afficher : vue filtrée par système <-> tous profils et dossiers)
            btnShowAll.BackColor = Color.FromArgb(50, 60, 78);
            btnShowAll.Click += (s, e) =>
            {
                _showAll = !_showAll;
                _currentFolder = null;
                UpdateShowAllButton();
                PopulateTiles();
            };
            UpdateShowAllButton();

            this.KeyDown += EsTileDialog_KeyDown;
            this.FormClosed += (s, e) => { StopXInputPolling(); _isOpen = false; _openInstance = null; };

            // [V52] Tiles ALWAYS wrap inside the frame (never overflow to the right);
            // the AutoScroll vertical scrollbar appears when the grid exceeds the
            // frame bottom and the flow rewraps at the reduced client width.
            // (EN/FR: Tuiles TOUJOURS wrappées dans le cadre (jamais de débordement à
            // droite) ; la barre de défilement verticale AutoScroll apparaît quand la
            // grille dépasse le cadre bas et le flux rewrappe sur la largeur réduite.)
            flowTiles.WrapContents = true;
            flowTiles.AutoScroll = true;

            // [V52] Header buttons are navigable too, with focus highlight
            // (EN/FR: Les boutons d'en-tête sont navigables aussi, avec surbrillance)
            WireNavigation(btnNavHome);
            WireNavigation(btnNavBack);
            WireNavigation(btnShowAll);
            WireNavigation(btnDialogClose);
            WireNavigation(btnTabMouse);
            WireNavigation(btnTabGamePad);
            WireNavigation(btnApiSwap);

            SwitchTab(false);
            // [V52] Initial focus on the first profile tile
            // (EN/FR: Focus initial sur la première tuile profil)
            if (_navRows.Count > 2) { _navRow = 2; _navCol = 0; FocusNavigable(); }
            StartXInputPolling();
        }

        private void UpdateShowAllButton()
        {
            btnShowAll.Text = _showAll ? "🎯 System view" : "🌐 All profiles";
            btnShowAll.BackColor = _showAll ? Color.FromArgb(0, 100, 60) : Color.FromArgb(50, 60, 78);
        }

        // ═════════════════════════════════════════════════════════════════
        // WIIMOTE REMOTE CONTROL (EN/FR: TÉLÉCOMMANDE WIIMOTE)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// EN: [V49] Wiimote [+] pressed (single click): activate the focused tile/button.
        /// FR: [V49] Appui [+] wiimote (un clic) : active la tuile/bouton focus.
        /// </summary>
        public static void NotifyWiimotePlus()
        {
            var f = _openInstance;
            if (f == null || f.IsDisposed) return;
            if (f.InvokeRequired) f.BeginInvoke(new Action(() => { try { f.ActivateFocused(); } catch { } }));
            else { try { f.ActivateFocused(); } catch { } }
        }

        /// <summary>
        /// EN: [V49] Wiimote [-] pressed: go back when inside a folder, close at root.
        /// FR: [V49] Appui [-] wiimote : retourne en arrière dans un sous-dossier, ferme à la racine.
        /// </summary>
        public static void NotifyWiimoteMinus()
        {
            var f = _openInstance;
            if (f == null || f.IsDisposed) return;
            if (f.InvokeRequired) f.BeginInvoke(new Action(() => { try { f.WiimoteBack(); } catch { } }));
            else { try { f.WiimoteBack(); } catch { } }
        }

        /// <summary>
        /// EN: [V52] Wiimote DPad navigation (physical buttons read in WiiMoteController,
        /// works identically in Mouse, XInput and DInput modes). dx/dy: -1, 0 or +1.
        /// FR: [V52] Navigation DPad wiimote (boutons physiques lus dans WiiMoteController,
        /// fonctionne à l'identique en modes Souris, XInput et DInput). dx/dy : -1, 0 ou +1.
        /// </summary>
        public static void NotifyWiimoteDirection(int dx, int dy)
        {
            var f = _openInstance;
            if (f == null || f.IsDisposed) return;

            Action move;
            if (dx == -1) move = f.MoveFocusLeft;
            else if (dx == 1) move = f.MoveFocusRight;
            else if (dy == -1) move = f.MoveFocusUp;
            else if (dy == 1) move = f.MoveFocusDown;
            else return;

            if (f.InvokeRequired) f.BeginInvoke(new Action(() => { try { move(); } catch { } }));
            else { try { move(); } catch { } }
        }

        private void WiimoteBack()
        {
            if (!string.IsNullOrEmpty(_currentFolder))
                NavigateBack();
            else
                Close();
        }

        /// <summary>
        /// EN: [V46] Dialog title: "PROFILES | System: X" plus the GameName dedicated to
        /// the focused profile tile (when the profile carries a game association).
        /// FR: [V46] Titre : "PROFILES | System : X" plus le GameName dédié à la tuile
        /// profil focus (quand le profil porte une association de jeu).
        /// </summary>
        private void UpdateTitle(string focusedProfileRelPath)
        {
            string sys = EsScriptIntegration.LastSystem;
            string game = null;

            if (!string.IsNullOrEmpty(focusedProfileRelPath))
            {
                var m = GameProfileMappingManager.GetMappingByProfilePath(focusedProfileRelPath, _gamePadTab);
                game = m?.GameName;
            }

            lblDialogTitle.Text = "PROFILES"
                + (string.IsNullOrEmpty(sys) ? "" : $"   |   System: {sys}")
                + (string.IsNullOrEmpty(game) ? "" : $"   |   Game: {game}");
        }

        // ═════════════════════════════════════════════════════════════════
        // TABS + TILES (EN/FR: ONGLETS + TUILES)
        // ═════════════════════════════════════════════════════════════════

        private void SwitchTab(bool gamePadTab)
        {
            _gamePadTab = gamePadTab;
            _currentFolder = null;

            _btnTabStyle(btnTabMouse, !gamePadTab);
            _btnTabStyle(btnTabGamePad, gamePadTab);

            // [V44] The XInput/DInput swap only belongs to the GamePad tab
            // (EN/FR: La bascule XInput/DInput n'appartient qu'à l'onglet GamePad)
            btnApiSwap.Visible = gamePadTab;
            lblApiSwapInfo.Visible = gamePadTab;

            UpdateApiSwapLabel();

            // [V44] Open directly on the ES system folder when it exists
            // (EN/FR: Ouvrir directement sur le dossier du système ES s'il existe)
            string system = EsScriptIntegration.LastSystem;
            if (!string.IsNullOrEmpty(system))
            {
                var folders = _gamePadTab ? RemapProfileManager.GetGamePadSubfolders()
                                          : RemapProfileManager.GetSubfolders().Where(f => !f.Equals("Gamepad", StringComparison.OrdinalIgnoreCase)).ToList();
                if (folders.Any(f => f.Equals(system, StringComparison.OrdinalIgnoreCase)))
                    _currentFolder = system;
            }

            PopulateTiles();
        }

        private void _btnTabStyle(Button b, bool active)
        {
            b.BackColor = active ? _accent : _tabIdle;
            b.ForeColor = active ? Color.White : Color.FromArgb(180, 180, 180);
        }

        private void NavigateBack()
        {
            if (string.IsNullOrEmpty(_currentFolder)) return;
            // Nested folders: go to parent folder, else root (EN/FR: Dossiers imbriqués : parent, sinon racine)
            int sep = _currentFolder.LastIndexOf('/');
            _currentFolder = sep > 0 ? _currentFolder.Substring(0, sep) : null;
            PopulateTiles();
        }

        private void PopulateTiles()
        {
            flowTiles.SuspendLayout();
            flowTiles.Controls.Clear();

            lblCurrentPath.Text = string.IsNullOrEmpty(_currentFolder) ? "(root)" : _currentFolder;

            try
            {
                string system = EsScriptIntegration.LastSystem;

                // [V49] Root default.remap ALWAYS first (mouse + GamePad, whatever the system)
                // (EN/FR: Le default.remap racine TOUJOURS en premier (souris + GamePad,
                // quel que soit le système))
                if (string.IsNullOrEmpty(_currentFolder))
                {
                    var rootProfiles = _gamePadTab
                        ? RemapProfileManager.GetGamePadProfilesInFolder(null)
                        : RemapProfileManager.GetProfilesInFolder(null);
                    foreach (string def in rootProfiles)
                    {
                        if (def.Equals("default.remap", StringComparison.OrdinalIgnoreCase))
                        {
                            AddProfileTile(def, null);
                            break;
                        }
                    }
                }

                // [V46+V49] STRICT system filtering at root: when an ES system is selected
                // (and the Show-All switch is OFF), only show the system folder and the
                // profiles ASSOCIATED to that system (via their mapping SystemName).
                // (EN/FR: FILTRAGE STRICT par système à la racine : quand un système ES est
                // sélectionné (et la bascule Tout afficher est DÉSACTIVÉE), n'afficher que
                // le dossier du système et les profils ASSOCIÉS à ce système.)
                if (string.IsNullOrEmpty(_currentFolder) && !string.IsNullOrEmpty(system) && !_showAll)
                {
                    var folders = _gamePadTab ? RemapProfileManager.GetGamePadSubfolders()
                                              : RemapProfileManager.GetSubfolders().Where(f => !f.Equals("Gamepad", StringComparison.OrdinalIgnoreCase)).ToList();

                    bool folderExists = folders.Any(f => f.Equals(system, StringComparison.OrdinalIgnoreCase));
                    if (folderExists)
                        AddFolderTile(system, system);

                    var systemProfiles = GameProfileMappingManager.GetProfilePathsForSystem(system, _gamePadTab);
                    foreach (string rel in systemProfiles)
                        AddSystemProfileTile(rel);

                    flowTiles.ResumeLayout();
                    RebuildNavigation();
                    return;
                }

                // Subfolders of the current location (EN/FR: Sous-dossiers de l'emplacement courant)
                var foldersAll = _gamePadTab ? RemapProfileManager.GetGamePadSubfolders()
                                           : RemapProfileManager.GetSubfolders().Where(f => !f.Equals("Gamepad", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (string folder in foldersAll)
                {
                    // At root, list top-level folders only; inside, list nested subfolders
                    // (EN/FR: À la racine, dossiers de premier niveau ; à l'intérieur, sous-dossiers imbriqués)
                    string rel = string.IsNullOrEmpty(_currentFolder) ? folder : null;
                    if (rel == null)
                    {
                        // Nested navigation: only folders starting with prefix (handled below)
                        continue;
                    }
                    AddFolderTile(rel, folder);
                }

                // Inside a folder: nested subfolders (EN/FR: Dans un dossier : sous-dossiers imbriqués)
                if (!string.IsNullOrEmpty(_currentFolder))
                {
                    foreach (string folder in foldersAll)
                    {
                        if (folder.StartsWith(_currentFolder + "/", StringComparison.OrdinalIgnoreCase))
                        {
                            string remainder = folder.Substring(_currentFolder.Length + 1);
                            if (remainder.Contains("/")) continue; // Deeper, reachable by steps (EN/FR: Plus profond, atteignable par étapes)
                            AddFolderTile(folder, remainder);
                        }
                    }
                }

                // Profiles of the current location (EN/FR: Profils de l'emplacement courant)
                // [V49] Skip root default.remap here: already added FIRST at the top
                // (EN/FR: Sauter le default.remap racine ici : déjà ajouté EN PREMIER en tête)
                string sub = _currentFolder != null && _currentFolder.Contains("/") ? _currentFolder.Replace('/', Path.DirectorySeparatorChar) : _currentFolder;
                if (_gamePadTab)
                {
                    foreach (string profile in RemapProfileManager.GetGamePadProfilesInFolder(sub))
                    {
                        if (string.IsNullOrEmpty(sub) && profile.Equals("default.remap", StringComparison.OrdinalIgnoreCase)) continue;
                        AddProfileTile(profile, sub);
                    }
                }
                else
                {
                    foreach (string profile in RemapProfileManager.GetProfilesInFolder(sub))
                    {
                        if (string.IsNullOrEmpty(sub) && profile.Equals("default.remap", StringComparison.OrdinalIgnoreCase)) continue;
                        AddProfileTile(profile, sub);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ES Tile] Failed to list profiles: {ex.Message}");
            }

            flowTiles.ResumeLayout();
            RebuildNavigation();
        }

        private void AddSystemProfileTile(string relPath)
        {
            // [V46] Profile associated to the current system via its mapping, from ANY folder
            // (EN/FR: Profil associé au système courant via son mapping, de N'IMPORTE QUEL dossier)
            string normalized = relPath.Replace('\\', '/');
            string fileName = Path.GetFileName(normalized);
            string subfolder = normalized.Contains("/")
                ? normalized.Substring(0, normalized.LastIndexOf('/'))
                : null;

            var tile = new Button
            {
                Size = new Size(165, 60),
                Margin = new Padding(6),
                FlatStyle = FlatStyle.Flat,
                BackColor = _tileBack,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = Path.GetFileNameWithoutExtension(fileName) + "\n" + (string.IsNullOrEmpty(subfolder) ? "(root)" : subfolder),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "profile:" + normalized
            };
            tile.FlatAppearance.BorderSize = 1;
            tile.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            tile.FlatAppearance.MouseOverBackColor = _tileHover;
            WireNavigation(tile);
            tile.Enter += (s, e) => UpdateTitle(normalized); // [V46] GameName in the title on focus

            tile.Click += (s, e) => LoadProfileAndClose(normalized);
            flowTiles.Controls.Add(tile);
        }

        private void LoadProfileAndClose(string relPath)
        {
            try
            {
                if (_gamePadTab)
                {
                    Program.LoadGamePadProfileHot(relPath, true); // Manual: may change the GamePad API
                    SimpleLogger.Instance.Info($"[ES Tile] GamePad profile loaded: {relPath}");
                }
                else
                {
                    Program.LoadRemapProfileHot(relPath, true);
                    SimpleLogger.Instance.Info($"[ES Tile] Mouse profile loaded: {relPath}");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ES Tile] Failed to load profile '{relPath}': {ex.Message}");
            }
            Close();
        }

        private void AddFolderTile(string relativePath, string displayName)
        {
            var tile = new Button
            {
                Size = new Size(165, 60),
                Margin = new Padding(6),
                FlatStyle = FlatStyle.Flat,
                BackColor = _folderBack,
                ForeColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = "📁 " + displayName,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "folder:" + relativePath
            };
            tile.FlatAppearance.BorderSize = 1;
            tile.FlatAppearance.BorderColor = Color.FromArgb(80, 90, 110);
            tile.FlatAppearance.MouseOverBackColor = _tileHover;
            WireNavigation(tile);
            tile.Enter += (s, e) => UpdateTitle(null); // [V46] Folder focus: title without game

            tile.Click += (s, e) =>
            {
                _currentFolder = relativePath;
                PopulateTiles();
            };
            flowTiles.Controls.Add(tile);
        }

        private void AddProfileTile(string profileName, string subfolder)
        {
            string relPath = string.IsNullOrEmpty(subfolder)
                ? profileName
                : (subfolder.Replace('\\', '/') + "/" + profileName);

            var tile = new Button
            {
                Size = new Size(165, 60),
                Margin = new Padding(6),
                FlatStyle = FlatStyle.Flat,
                BackColor = _tileBack,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = Path.GetFileNameWithoutExtension(profileName),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "profile:" + relPath
            };
            tile.FlatAppearance.BorderSize = 1;
            tile.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            tile.FlatAppearance.MouseOverBackColor = _tileHover;
            WireNavigation(tile);
            tile.Enter += (s, e) => UpdateTitle(relPath); // [V46] GameName in the title on focus

            tile.Click += (s, e) => LoadProfileAndClose(relPath);
            flowTiles.Controls.Add(tile);
        }

        // ═════════════════════════════════════════════════════════════════
        // XINPUT / DINPUT SWAP (EN/FR: BASCULE XINPUT/DINPUT)
        // ═════════════════════════════════════════════════════════════════

        private void UpdateApiSwapLabel()
        {
            bool useXInput = Options.Instance.P1GamePadMappings?.UseXInput ?? false;
            btnApiSwap.Text = "GamePad API: " + (useXInput ? "XInput" : "DInput (VMulti)") + "  [SWITCH]";
            btnApiSwap.BackColor = useXInput ? Color.FromArgb(0, 100, 60) : Color.FromArgb(90, 60, 20);
        }

        private void BtnApiSwap_Click(object sender, EventArgs e)
        {
            // Toggle XInput/DInput for ALL players (EN/FR: Bascule XInput/DInput pour TOUS les players)
            var all = new[]
            {
                Options.Instance.P1GamePadMappings,
                Options.Instance.P2GamePadMappings,
                Options.Instance.P3GamePadMappings,
                Options.Instance.P4GamePadMappings
            };
            bool newValue = !(all[0]?.UseXInput ?? false);
            foreach (var m in all)
            {
                if (m != null) m.UseXInput = newValue;
            }
            Options.Instance.Save();
            SimpleLogger.Instance.Info($"[ES Tile] GamePad API switched to {(newValue ? "XInput" : "DInput (VMulti)")} for all players");
            UpdateApiSwapLabel();
        }

        // ═════════════════════════════════════════════════════════════════
        // NAVIGATION: KEYBOARD + XINPUT GAMEPAD (EN/FR: NAVIGATION CLAVIER + MANETTE)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// EN: [V52] Focus highlight for every navigable control (accent border).
        /// FR: [V52] Surbrillance de focus pour tout contrôle navigable (bordure accent).
        /// </summary>
        private void WireNavigation(Control c)
        {
            c.Enter += (s, e) => { if (c is Button b) { b.FlatAppearance.BorderSize = 1; b.FlatAppearance.BorderColor = _accent; } };
            c.Leave += (s, e) =>
            {
                if (c is Button b)
                {
                    bool isTile = c.Parent == flowTiles; // evaluated at event time (EN/FR: évalué au moment de l'événement)
                    b.FlatAppearance.BorderSize = isTile ? 1 : 0;
                    b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
                }
            };
        }

        private void RebuildNavigation()
        {
            // [V52] Rows: [tabs] [nav buttons] [tile grid rows...] [swap footer].
            // The WHOLE dialog is navigable: Up from the tile grid reaches the tabs and
            // Home/Back/Show-All; Down from the last tile row reaches the API swap
            // (GamePad page) — vertical moves wrap bottom <-> top.
            // (EN/FR: Lignes : [onglets] [boutons nav] [lignes de tuiles...] [bascule bas].
            // TOUTE la modale est navigable : Haut depuis les tuiles atteint les onglets
            // et Home/Back/Tout afficher ; Bas depuis la dernière tuile atteint la
            // bascule API (page GamePad) — les déplacements verticaux wrappe bas <-> haut.)
            var rows = new System.Collections.Generic.List<System.Collections.Generic.List<Control>>();

            // Row 0: tabs (EN/FR: Ligne 0 : onglets)
            rows.Add(new System.Collections.Generic.List<Control> { btnTabMouse, btnTabGamePad });

            // Row 1: navigation buttons (EN/FR: Ligne 1 : boutons de navigation)
            rows.Add(new System.Collections.Generic.List<Control> { btnNavHome, btnNavBack, btnShowAll, btnDialogClose });

            // Tile rows, chunked by column count (EN/FR: Lignes de tuiles, découpées par colonnes)
            var tiles = flowTiles.Controls.Cast<Control>().Where(c => c is Button).ToList();
            int cols = TileColumns();
            for (int i = 0; i < tiles.Count; i += cols)
                rows.Add(tiles.GetRange(i, Math.Min(cols, tiles.Count - i)));

            // Footer row: API swap (GamePad tab only) (EN/FR: Ligne bas : bascule API)
            if (btnApiSwap.Visible)
                rows.Add(new System.Collections.Generic.List<Control> { btnApiSwap });

            _navRows = rows;
            ClampNavPosition();
        }

        /// <summary>EN: [V52] Number of tile columns from the flow layout. FR: [V52] Nombre de colonnes de tuiles.</summary>
        private int TileColumns()
        {
            // Tile step: 165 wide + 12 horizontal margins; panel padding 14 + 14.
            // (EN/FR: Pas des tuiles : 165 + 12 marges horizontales ; padding 14 + 14.)
            int usable = Math.Max(177, flowTiles.ClientSize.Width - 28);
            return Math.Max(1, usable / 177);
        }

        private void ClampNavPosition()
        {
            if (_navRows.Count == 0) { _navRow = 0; _navCol = 0; return; }
            if (_navRow < 0) _navRow = 0;
            if (_navRow >= _navRows.Count) _navRow = _navRows.Count - 1;
            int count = _navRows[_navRow].Count;
            _navCol = count == 0 ? 0 : Math.Max(0, Math.Min(count - 1, _navCol));
        }

        private void FocusNavigable()
        {
            if (_navRows.Count == 0) return;
            var row = _navRows[_navRow];
            if (row.Count == 0) return;
            var c = row[_navCol];
            c.Focus();
            // [V52] The scrollbar follows the focused tile
            // (EN/FR: La barre de défilement suit la tuile focus)
            flowTiles.ScrollControlIntoView(c);
        }

        private void MoveFocusLeft()
        {
            if (_navRows.Count == 0) return;
            var row = _navRows[_navRow];
            if (row.Count == 0) return;
            _navCol = (_navCol - 1 + row.Count) % row.Count;
            FocusNavigable();
        }

        private void MoveFocusRight()
        {
            if (_navRows.Count == 0) return;
            var row = _navRows[_navRow];
            if (row.Count == 0) return;
            _navCol = (_navCol + 1) % row.Count;
            FocusNavigable();
        }

        private void MoveFocusUp()
        {
            if (_navRows.Count == 0) return;
            _navRow = (_navRow - 1 + _navRows.Count) % _navRows.Count; // wrap bottom <-> top
            ClampNavPosition();
            FocusNavigable();
        }

        private void MoveFocusDown()
        {
            if (_navRows.Count == 0) return;
            _navRow = (_navRow + 1) % _navRows.Count; // wrap top <-> bottom
            ClampNavPosition();
            FocusNavigable();
        }

        private void ActivateFocused()
        {
            if (_navRows.Count == 0) return;
            var row = _navRows[_navRow];
            if (row.Count == 0) return;
            if (row[_navCol] is Button b) b.PerformClick();
        }

        private void EsTileDialog_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Keys.Left: MoveFocusLeft(); e.Handled = true; break;
                case Keys.Right: MoveFocusRight(); e.Handled = true; break;
                case Keys.Up: MoveFocusUp(); e.Handled = true; break;
                case Keys.Down: MoveFocusDown(); e.Handled = true; break;
                case Keys.Enter:
                    ActivateFocused();
                    e.Handled = true;
                    break;
            }
        }

        // ── XInput native polling (EN/FR: Sondage natif XInput) ──

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public short wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

        private const int XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        private const int XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        private const int XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        private const int XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        private const int XINPUT_GAMEPAD_A = 0x1000;
        private const int XINPUT_GAMEPAD_B = 0x2000;

        // [V52] Generic DInput buttons merged into the same edge detection
        // (EN/FR: Boutons DInput génériques fusionnés dans la même détection de front)
        private const int NAV_BTN_VALIDATE = 0x01000000;
        private const int NAV_BTN_BACK = 0x02000000;

        private void StartXInputPolling()
        {
            _xinputTimer = new Timer { Interval = 50 };
            _xinputTimer.Tick += (s, e) => PollControllers();
            _xinputTimer.Start();
        }

        private void StopXInputPolling()
        {
            if (_xinputTimer != null)
            {
                _xinputTimer.Stop();
                _xinputTimer.Dispose();
                _xinputTimer = null;
            }
        }

        /// <summary>
        /// EN: [V52] Poll every controller source (XInput gamepads + DInput joysticks),
        /// merge their buttons and navigate on edges.
        /// FR: [V52] Sondage de toutes les sources de contrôle (manettes XInput +
        /// joysticks DInput), fusion des boutons et navigation sur fronts.
        /// </summary>
        private void PollControllers()
        {
            try
            {
                int buttons = 0;

                // XInput gamepads (EN/FR: Manettes XInput)
                for (uint u = 0; u < 4; u++)
                {
                    var state = new XINPUT_STATE();
                    if (XInputGetState(u, ref state) == 0)
                    {
                        buttons |= (int)(ushort)state.Gamepad.wButtons;
                        // Left stick as directional too (EN/FR: Stick gauche aussi directionnel)
                        if (state.Gamepad.sThumbLX > 12000) buttons |= XINPUT_GAMEPAD_DPAD_RIGHT;
                        if (state.Gamepad.sThumbLX < -12000) buttons |= XINPUT_GAMEPAD_DPAD_LEFT;
                        if (state.Gamepad.sThumbLY > 12000) buttons |= XINPUT_GAMEPAD_DPAD_UP;
                        if (state.Gamepad.sThumbLY < -12000) buttons |= XINPUT_GAMEPAD_DPAD_DOWN;
                    }
                }

                // [V56b] DInput joysticks polling REMOVED — crash root cause.
                // The legacy winmm joystick API (joyGetPosEx) hammered at 20Hz over 16 IDs
                // forced the expensive registry/INF path for every phantom ID
                // (winmm!joyOpen -> dinput!JoyReg_GetConfig -> CM_Get_DevNode_Registry_Property
                // -> cfgmgr32!SpInf*) which corrupts the cfgmgr32 INF heap structures ->
                // deferred STATUS_HEAP_CORRUPTION (c0000374) process kill. Proof: user crash
                // dump 2026-10-01 16:33:21 (heap corruption detected inside
                // joyGetPosEx -> CM_Get_DevNode_Registry_PropertyW -> SpInfFreeInfFile),
                // 7 seconds after this modal opened.
                // Navigation stays fully covered by: the physical Wiimote (it is what
                // opens this modal — wired via NotifyWiimote*), XInput gamepads above,
                // and the VMulti gamepads driven by the Wiimote never double-navigate
                // (their DPad output is suppressed at the source while this modal is open).
                // (EN/FR: Sondage joysticks DInput SUPPRIMÉ — cause racine du crash.
                // L'API joystick winmm héritée (joyGetPosEx) martelée à 20Hz sur 16 IDs
                // forçait le chemin coûteux registre/INF pour chaque ID fantôme
                // (winmm!joyOpen -> dinput!JoyReg_GetConfig -> CM_Get_DevNode_Registry_Property
                // -> cfgmgr32!SpInf*) ce qui corrompt les structures INF du tas cfgmgr32 ->
                // STATUS_HEAP_CORRUPTION (c0000374) différé tuant le processus. Preuve :
                // dump du crash user 2026-10-01 16:33:21 (corruption détectée dans
                // joyGetPosEx -> CM_Get_DevNode_Registry_PropertyW -> SpInfFreeInfFile),
                // 7 secondes après l'ouverture de cette modale.
                // La navigation reste couverte par : la Wiimote physique (c'est elle qui
                // ouvre cette modale — câblée via NotifyWiimote*), les manettes XInput
                // ci-dessus, et les gamepads VMulti pilotés par la Wiimote ne doublonnent
                // jamais (leur DPad est supprimé à la source pendant que cette modale
                // est ouverte).)

                int pressed = buttons & ~_prevXinputButtons; // Edge detection (EN/FR: Détection de front)
                _prevXinputButtons = buttons;

                if ((pressed & XINPUT_GAMEPAD_DPAD_LEFT) != 0) MoveFocusLeft();
                if ((pressed & XINPUT_GAMEPAD_DPAD_RIGHT) != 0) MoveFocusRight();
                if ((pressed & XINPUT_GAMEPAD_DPAD_UP) != 0) MoveFocusUp();
                if ((pressed & XINPUT_GAMEPAD_DPAD_DOWN) != 0) MoveFocusDown();
                if ((pressed & (XINPUT_GAMEPAD_A | NAV_BTN_VALIDATE)) != 0) ActivateFocused();
                if ((pressed & (XINPUT_GAMEPAD_B | NAV_BTN_BACK)) != 0) Close();
            }
            catch { }
        }

    }
}
