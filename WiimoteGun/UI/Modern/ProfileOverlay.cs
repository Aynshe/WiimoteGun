using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WiimoteGun.Controls;
using System.Diagnostics;

namespace WiimoteGun.Forms
{
    public partial class ProfileOverlay : Form
    {
        // P/Invoke for window activation behavior
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int WS_EX_NOACTIVATE = 0x08000000;

        // Controls
        private HomeControl homeControl;
        private AssignControl assignControl;
        private OptionsControl optionsControl;
        private MappingControl mappingControl;
        private GamePadMappingControl gamePadMappingControl;
        private IRControl irControl;
        
        // Custom Title Bar
        private Panel pnlTitleBar;
        private Label lblTitleBarText;
        private Button btnTitleMinimize;
        private Button btnTitleClose;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // State
        private bool _windowedMode;
        private System.Windows.Forms.Timer _gameDetectTimer;
        private string _currentExecutable = "";
        private string _currentExecutablePath = null; // [V47] Full path of the foreground exe (EN/FR: Chemin complet de l'exe au premier plan)
        
        // Dynamic Sizing Constants (EN/FR: Constantes de redimensionnement dynamique)
        private const int COMPACT_WIDTH = 600;
        private const int EXTENDED_WIDTH = 800;
        private const int CONTENT_HEIGHT = 780;
        
        public bool IsWindowedMode { get { return _windowedMode; } }

        public bool IsEditing
        {
            get
            {
                // Return true if any "editing" control is visible
                return (mappingControl != null && mappingControl.Visible) ||
                       (assignControl != null && assignControl.Visible) ||
                       (optionsControl != null && optionsControl.Visible) ||
                       (gamePadMappingControl != null && gamePadMappingControl.Visible);
            }
        }

        public ProfileOverlay(bool windowedMode)
        {
            _windowedMode = windowedMode;
            InitializeComponent();
            SetupModernUI();
            InitializeControls();

            // [V57n] EN: Apply the global UI zoom to the WHOLE interface (this form plus
            //     every page created above) at creation time - bounds first, then fonts,
            //     then screen clamping. No-op at 100%.
            //     FR: Applique le zoom UI global à TOUTE l'interface (ce form plus toutes
            //     les pages créées ci-dessus) au moment de la création - bornes d'abord,
            //     puis polices, puis bornage écran. No-op à 100 %.
            WiimoteGun.UI.UiScaler.ApplyForm(this);

            // [V57o] EN: Initial page show AFTER the zoom is applied: ShowPage sizes the
            //     form and the active page with S()-scaled metrics, so it must run once
            //     the scaling is done - never before (double-scale).
            //     FR: Affichage initial APRÈS l'application du zoom : ShowPage dimensionne
            //     le form et la page active avec des métriques S() scalées, donc il doit
            //     tourner une fois le scale fait - jamais avant (double-scale).
            ShowPage("Home");

            // Game Detection Timer
            _gameDetectTimer = new System.Windows.Forms.Timer();
            _gameDetectTimer.Interval = 1000;
            _gameDetectTimer.Tick += (s, e) => DetectCurrentGame();
            _gameDetectTimer.Start();
        }

        private void InitializeControls()
        {
            int topOffset = _windowedMode ? 32 : 0;
            // (EN/FR: La taille sera ajustée dynamiquement dans ShowPage)
            
            // Home
            homeControl = new HomeControl { Visible = true };
            homeControl.OptionsClicked += (s, e) => ShowPage("Options");
            homeControl.MappingClicked += (s, e) => ShowPage("Mapping");
            homeControl.AssignClicked += (s, e) => ShowPage("Assign");
            homeControl.IRVizClicked += (s, e) => ShowPage("IRViz");
            this.Controls.Add(homeControl);

            // Assign
            assignControl = new AssignControl { Visible = false };
            this.Controls.Add(assignControl);

            // Options
            optionsControl = new OptionsControl { Visible = false };
            this.Controls.Add(optionsControl);
            
            // Mapping
            mappingControl = new MappingControl { Visible = false };
            this.Controls.Add(mappingControl);

            // IR
            irControl = new IRControl { Visible = false };
            this.Controls.Add(irControl);

            // GamePad Mapping
            gamePadMappingControl = new GamePadMappingControl { Visible = false };
            this.Controls.Add(gamePadMappingControl);

            // Wire Internal Back Events
            assignControl.BackRequested += (s, e) => ShowPage("Home");
            optionsControl.BackRequested += (s, e) => ShowPage("Home");
            mappingControl.BackRequested += (s, e) => ShowPage("Home");
            mappingControl.GamePadMappingRequested += (s, e) => ShowPage("GamePadMapping");
            irControl.BackRequested += (s, e) => ShowPage("Home");
            gamePadMappingControl.BackRequested += (s, e) => ShowPage("Mapping");

            // [V57p] EN: Disable the mouse wheel on every ComboBox/UpDown of ALL pages
            //     (Designer content): scrolling a page must never silently change a
            //     selection or a value. Runtime rows are covered by the suppressor calls
            //     at the end of each page population.
            //     FR: Désactive la molette sur chaque ComboBox/UpDown de TOUTES les pages
            //     (contenu Designer) : défiler une page ne doit plus jamais changer une
            //     sélection ou une valeur en silence. Les lignes runtime sont couvertes
            //     par les appels du suppresseur en fin de chaque population de page.
            WiimoteGun.UI.WheelSuppressor.ApplyTo(this);

            // [V57o] EN: The initial ShowPage("Home") MOVED to the constructor, AFTER
            //     UiScaler.ApplyForm: running it here (before the zoom is applied) made
            //     ApplyForm re-scale the S()-sized form a second time - at launch the
            //     frame was too wide/off-center until the user returned to the home page.
            //     FR: Le ShowPage("Home") initial DÉPLACÉ vers le constructeur, APRÈS
            //     UiScaler.ApplyForm : l'exécuter ici (avant l'application du zoom) faisait
            //     re-scaler par ApplyForm la taille S() posée une seconde fois - au
            //     lancement, le cadre était trop large/décentré jusqu'au retour de
            //     l'utilisateur à la page d'accueil.
        }

        /* 
        // Global button removed in favor of internal buttons per user request
        private Button btnBackToHome; 
        */


        private void ShowPage(string page)
        {
            // [V57n] EN: Every hardcoded metric below is scaled by the global UI zoom so a
            //     page switch can never revert the window/contents to the unscaled
            //     designer sizes. FR: Chaque métrique codée en dur ci-dessous est scalée
            //     par le zoom UI global pour qu'un changement de page ne ramène JAMAIS la
            //     fenêtre/les contenus aux tailles Designer non scalées.
            // Determine Target Width (EN/FR: Déterminer la largeur cible)
            int targetWidth = (page == "GamePadMapping") ? WiimoteGun.UI.UiScaler.S(EXTENDED_WIDTH) : WiimoteGun.UI.UiScaler.S(COMPACT_WIDTH);
            int targetHeight = WiimoteGun.UI.UiScaler.S(840);

            // Resize Form (EN/FR: Redimensionner le formulaire)
            if (this.Width != targetWidth)
            {
                // Store current center to maintain positioning (Optional, otherwise it jumps)
                Point center = new Point(this.Left + this.Width / 2, this.Top + this.Height / 2);

                this.Width = targetWidth;
                this.Height = targetHeight;

                // Re-center maintaining the same center point (EN/FR: Recentrer en maintenant le même point central)
                this.Left = center.X - targetWidth / 2;
                this.Top = center.Y - targetHeight / 2;

                // Update rounded corners for Overlay mode (EN/FR: Mettre à jour les coins arrondis)
                if (!_windowedMode)
                {
                    GraphicsPath path = new GraphicsPath();
                    int radius = WiimoteGun.UI.UiScaler.S(12);
                    Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
                    path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
                    path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
                    path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();
                    this.Region = new Region(path);
                }

                // Update Title Bar width if exists
                if (pnlTitleBar != null)
                {
                    pnlTitleBar.Width = this.Width;
                }
            }

            // Hide all
            homeControl.Visible = false;
            assignControl.Visible = false;
            optionsControl.Visible = false;
            mappingControl.Visible = false;
            irControl.Visible = false;
            gamePadMappingControl.Visible = false;
            
            // Unload data if needed
            assignControl.UnloadData();
            irControl.UnloadData();

            // Setup common control sizing and position (EN/FR: Configurer taille et position commune)
            int topOffset = _windowedMode ? 32 : 0;
            Size contentSize = new Size(targetWidth - WiimoteGun.UI.UiScaler.S(40), WiimoteGun.UI.UiScaler.S(CONTENT_HEIGHT));
            Point contentLoc = new Point((this.Width - contentSize.Width) / 2, WiimoteGun.UI.UiScaler.S(30) + topOffset);

            UserControl activeControl = null;

            // [V57o2] EN: Data loads run AFTER the page is sized/positioned (see the
            //     activeControl block below): the runtime row layouts read the panel
            //     width, so populating before sizing computed the mouse-mapping layout
            //     against the wrong width - it only looked right after switching player
            //     tabs (which re-populated at the final width).
            //     FR: Les chargements de données s'exécutent APRÈS dimensionnement/
            //     positionnement de la page (voir le bloc activeControl ci-dessous) : les
            //     layouts de lignes runtime lisent la largeur du panneau, donc peupler
            //     avant le dimensionnement calculait le layout du mapping souris sur une
            //     mauvaise largeur - il n'était correct qu'après un changement
            //     d'onglet joueur (qui repeuplait à la largeur finale).
            System.Action loadData = null;

            switch (page)
            {
                case "Home":
                    activeControl = homeControl;
                    break;
                case "Options":
                    activeControl = optionsControl;
                    loadData = () => optionsControl.LoadOptionsFromInstance();
                    break;
                case "Mapping":
                    activeControl = mappingControl;
                    // [V47] Pass exe + PATH, falling back to the tracked game so the
                    //     association is fully automatic while in game.
                    //     (EN/FR: Passer exe + CHEMIN, avec repli sur le jeu suivi pour que
                    //     l'association soit entièrement automatique en jeu.)
                    string mapExe = !string.IsNullOrEmpty(_currentExecutable) ? _currentExecutable : Program.LastDetectedGameName;
                    string mapPath = !string.IsNullOrEmpty(_currentExecutablePath) ? _currentExecutablePath : Program.LastDetectedGamePath;
                    loadData = () => mappingControl.SetCurrentGame(mapExe, mapPath);
                    break;
                case "Assign":
                    activeControl = assignControl;
                    loadData = () => assignControl.LoadData();
                    break;
                case "IRViz":
                    activeControl = irControl;
                    loadData = () => irControl.LoadData();
                    break;
                case "GamePadMapping":
                    activeControl = gamePadMappingControl;
                    // [V47] Pass exe + PATH, falling back to the tracked game so the
                    //     association is fully automatic while in game.
                    //     (EN/FR: Passer exe + CHEMIN, avec repli sur le jeu suivi pour que
                    //     l'association soit entièrement automatique en jeu.)
                    string gpExe = !string.IsNullOrEmpty(_currentExecutable) ? _currentExecutable : Program.LastDetectedGameName;
                    string gpPath = !string.IsNullOrEmpty(_currentExecutablePath) ? _currentExecutablePath : Program.LastDetectedGamePath;
                    loadData = () =>
                    {
                        gamePadMappingControl.SetCurrentGame(gpExe, gpPath);
                        gamePadMappingControl.LoadData();
                    };
                    break;
            }

            if (activeControl != null)
            {
                // [V57o2] EN: Size/position/visibility FIRST, data load SECOND, idempotent
                //     font zoom THIRD: the load creates the runtime rows against the
                //     FINAL scaled width, then ApplyFonts picks up their fresh fonts
                //     (already-scaled fonts are skipped by the registry).
                //     FR: Dimensionnement/position/visibilité D'ABORD, chargement ENSUITE,
                //     zoom idempotent des polices EN TROISIÈME : le chargement crée les
                //     lignes runtime sur la largeur scalée FINALE, puis ApplyFonts prend
                //     en compte leurs polices fraîches (les polices déjà scalées sont
                //     sautées par le registre).
                activeControl.Size = contentSize;
                activeControl.Location = contentLoc;
                activeControl.Visible = true;

                if (loadData != null)
                {
                    try { loadData(); }
                    catch (Exception ex) { SimpleLogger.Instance.Error("ShowPage load failed: " + ex.Message); }
                }

                WiimoteGun.UI.UiScaler.ApplyFonts(activeControl);

                // [V57o3] EN/FR: Diagnostics - target vs actual, one line per page switch.
                SimpleLogger.Instance.Debug(string.Format(
                    "[V57n] ShowPage({0}): zoom={1}% windowed={2} target={3}x{4} form={5}x{6} page={7}x{8}@{9},{10}",
                    page, WiimoteGun.Options.Instance.UiScalePercent, _windowedMode,
                    targetWidth, targetHeight, this.Width, this.Height,
                    activeControl.Width, activeControl.Height, activeControl.Left, activeControl.Top));
            }
        }

        private void SetupModernUI()
        {
            this.BackColor = Color.FromArgb(20, 20, 20); // Dark background
            this.DoubleBuffered = true;
            
             if (!_windowedMode)
            {
                // [V57o2] EN: Same as the windowed branch below: with the Designer's
                //     AutoScaleMode.Font, changing the form's root font inside
                //     UiScaler.ApplyForm makes WinForms RE-SCALE every child by the font
                //     ratio on top of our own scale (double-scale: overlay not centered,
                //     back buttons out of view - the first HOME+PLUS open was broken
                //     while the systray windowed interface, which sets None here, was
                //     correct). None before ApplyForm = our UiScaler owns ALL scaling.
                //     FR: Comme la branche fenêtrée ci-dessous : avec l'AutoScaleMode.Font
                //     du Designer, changer la police racine du form dans
                //     UiScaler.ApplyForm fait RE-SCALER WinForms tous les enfants selon le
                //     ratio de police PAR-DESSUS notre propre scale (double-scale :
                //     overlay non centré, boutons retour hors champ - la première
                //     ouverture HOME+PLUS était cassée alors que l'interface fenêtrée du
                //     systray, qui met None ici, était correcte). None avant ApplyForm =
                //     notre UiScaler possède TOUT le scaling.
                this.AutoScaleMode = AutoScaleMode.None;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Size = new Size(800, 840);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.TopMost = true;
                this.ShowInTaskbar = false;
                this.Opacity = 0.95;

                GraphicsPath path = new GraphicsPath();
                int radius = 12;
                Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
                path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
                path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
                path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
            else
            {
                this.AutoScaleMode = AutoScaleMode.None;
                this.ClientSize = new Size(800, 840);
                this.FormBorderStyle = FormBorderStyle.None;
                this.StartPosition = FormStartPosition.CenterScreen;
                SetupCustomTitleBar();
            }
        }

        private void SetupCustomTitleBar()
        {
             pnlTitleBar = new Panel
            {
                Size = new Size(this.Width, 32),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(45, 45, 48),
                Dock = DockStyle.Top
            };
            
            lblTitleBarText = new Label
            {
                Text = "Wiimote4Guns - Overlay",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            
            btnTitleClose = new Button
            {
                Text = "✕",
                Size = new Size(45, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F)
            };
            btnTitleClose.FlatAppearance.BorderSize = 0;
            btnTitleClose.Click += (s, e) => this.Close();
            btnTitleClose.MouseEnter += (s, e) => btnTitleClose.BackColor = Color.Red;
            btnTitleClose.MouseLeave += (s, e) => btnTitleClose.BackColor = Color.Transparent;

            btnTitleMinimize = new Button
            {
                Text = "—",
                Size = new Size(45, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F)
            };
            btnTitleMinimize.FlatAppearance.BorderSize = 0;
            btnTitleMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnTitleMinimize.MouseEnter += (s, e) => btnTitleMinimize.BackColor = Color.FromArgb(60, 60, 60);
            btnTitleMinimize.MouseLeave += (s, e) => btnTitleMinimize.BackColor = Color.Transparent;

            pnlTitleBar.Controls.Add(lblTitleBarText);
            pnlTitleBar.Controls.Add(btnTitleMinimize);
            pnlTitleBar.Controls.Add(btnTitleClose);

            pnlTitleBar.MouseDown += (s,e) => { if(e.Button == MouseButtons.Left) { _isDragging = true; _dragStartPoint = e.Location; } };
            pnlTitleBar.MouseUp += (s,e) => _isDragging = false;
            pnlTitleBar.MouseMove += (s,e) => 
            {
                if (_isDragging)
                {
                    Point p = PointToScreen(e.Location);
                    this.Location = new Point(p.X - _dragStartPoint.X, p.Y - _dragStartPoint.Y);
                }
            };

            this.Controls.Add(pnlTitleBar);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (!_windowedMode)
                {
                    cp.ExStyle |= WS_EX_NOACTIVATE;
                }
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return !_windowedMode; } }

        private void DetectCurrentGame()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle == IntPtr.Zero) return;

                // Check if it's us
                if (handle == this.Handle) return;

                uint processId;
                GetWindowThreadProcessId(handle, out processId);
                Process p = Process.GetProcessById((int)processId);

                string processName = p.ProcessName.ToLower();
                // Ignore shell and self (overlay)
                // (EN/FR: Ignorer shell et soi-même (overlay))
                if (processName == "explorer" || processName == "searchhost" || processName == "wiimotegun") return;

                if (processName + ".exe" != _currentExecutable)
                {
                    _currentExecutable = processName + ".exe";

                    // [V47] Capture the full path too: linking needs it for strict matching
                    // (EN/FR: Capturer aussi le chemin complet : le lien en a besoin pour
                    // la correspondance stricte)
                    try { _currentExecutablePath = p.MainModule?.FileName; } catch { _currentExecutablePath = null; }

                    // Update Mapping Control
                    if (mappingControl != null)
                        mappingControl.SetCurrentGame(_currentExecutable, _currentExecutablePath);
                    // Update GamePad Mapping Control (EN/FR: Mettre à jour l'UI GamePad)
                    if (gamePadMappingControl != null && gamePadMappingControl.Visible)
                        gamePadMappingControl.SetCurrentGame(_currentExecutable, _currentExecutablePath);
                }
            }
            catch {}
        }

        // Additional overrides/events needed by designer
        private void ProfileOverlay_Load(object sender, EventArgs e)
        {
            // Initial load logic if any
        }
    }
}
