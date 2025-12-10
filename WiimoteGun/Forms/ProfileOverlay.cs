using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Modern profile manager overlay with dark theme
    /// Opens without minimizing fullscreen games (EN/FR: Overlay gestionnaire de profils sans minimiser jeux plein écran)
    /// </summary>
    public partial class ProfileOverlay : Form
    {
        // Win32 API for no-activation (EN/FR: API Win32 pour non-activation)
        private const int WS_EX_NOACTIVATE = 0x08000000;
        
        // Win32 API for process detection (EN/FR: API Win32 pour détection processus)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, 
            [Out] System.Text.StringBuilder lpExeName, [In, Out] ref uint lpdwSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        
        // Magnetic pointer constants (EN/FR: Constantes pointer magnétique)
        private const int MAGNETIC_RADIUS = 50; // Distance d'attraction en pixels
        private const float MAGNETIC_STRENGTH = 0.35f; // Force d'attraction (0.0 - 1.0)
        private Point _lastCursorPos = Point.Empty;
        
        // Design colors (EN/FR: Couleurs du design)
        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26); // #1A1A1A opaque (Opacity property handles transparency)
        private static readonly Color ColorPanel = Color.FromArgb(37, 37, 37); // #252525
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204); // #007ACC
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224); // #E0E0E0
        private static readonly Color ColorBorder = Color.FromArgb(63, 63, 63); // #3F3F3F
        
        private string _currentExecutable = "";
        private string _currentExecutablePath = ""; // Full path (EN/FR: Chemin complet)

        private ToastNotification _toastNotification;
        private ModalInputDialog _modalInputDialog;
        private bool _updatingCheckbox = false; // Guard against recursive CheckedChanged (EN/FR: Garde contre récursion CheckedChanged)
        private bool _isProgrammaticUpdate = false; // Guard against recursive TextChanged (EN/FR: Garde contre récursion TextChanged)
        private Button btnDeleteProfile; // Button to delete profile (EN/FR: Bouton pour supprimer profil)
        
        // Button assignment mode fields (EN/FR: Champs mode assignation bouton)
        private bool _isAssignMode = false;
        private System.Threading.Timer _assignCountdownTimer;
        private int _assignCountdownSeconds = 8;
        private string _waitingForButton = null; // e.g. "WiiA", "WiiB", "NunchukC", etc.
        private int _waitingForPlayer = 1; // 1-4
        private ButtonAction _originalMapping; // For cancellation (EN/FR: Pour annulation)
        
        private bool _windowedMode = false; // Windowed mode flag (EN/FR: Drapeau mode fenêtré)
        
        public ProfileOverlay(bool windowedMode = false)
        {
            _windowedMode = windowedMode;
            
            InitializeComponent();
            
            // Explicitly set form size to ensure it's correct (EN/FR: Définir explicitement la taille pour s'assurer qu'elle est correcte)
            this.ClientSize = new Size(600, 840);
            
            // Adjust form properties based on mode (EN/FR: Ajuster propriétés formulaire selon mode)
            if (_windowedMode)
            {
                // Windowed mode: normal window behavior (EN/FR: Mode fenêtré : comportement normal)
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.TopMost = false;
                this.MinimizeBox = true;
                this.MaximizeBox = true;
                this.ShowInTaskbar = true;
            }
            else
            {
                // Fullscreen overlay mode: existing behavior (EN/FR: Mode overlay plein écran : comportement existant)
                this.FormBorderStyle = FormBorderStyle.None;
                this.TopMost = true;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                this.ShowInTaskbar = false;
            }
            
            SetupModernUI();
            
            // Initialize UI components (EN/FR: Initialiser composants UI)
            _toastNotification = new ToastNotification();
            this.Controls.Add(_toastNotification);
            _toastNotification.BringToFront();
            
            _modalInputDialog = new ModalInputDialog();
            
            // Setup event handlers (EN/FR: Configurer gestionnaires événements)
            this.KeyPreview = true;
            this.KeyDown += ProfileOverlay_KeyDown;
            this.Activated += ProfileOverlay_Activated; // Fix focus when returning to form (EN/FR: Corriger focus au retour sur formulaire)
            this.MouseClick += ProfileOverlay_MouseClick; // Right-click to go home (EN/FR: Clic droit pour retour accueil)
            this.MouseMove += ProfileOverlay_MouseMove; // Magnetic pointer effect (EN/FR: Effet pointer magnétique)
            
            // Wire up navigation events
            btnNavOptions.Click += (s, e) => ShowPage("Options");
            btnNavMapping.Click += (s, e) => ShowPage("Mapping");
            btnNavAssign.Click += (s, e) => ShowPage("Assign");
            btnNavIRViz.Click += (s, e) => ShowPage("IRViz");
            btnBackToHome.Click += (s, e) => ShowPage("Home");

            // Show home page by default (EN/FR: Afficher page d'accueil par défaut)
            ShowPage("Home");
            
            DetectCurrentGame();
        }

        private void DetectCurrentGame()
        {
            // Detect foreground window process (EN/FR: Détecter processus fenêtre premier plan)
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    uint processId;
                    GetWindowThreadProcessId(hwnd, out processId);
                    
                    // Try Method 1: MainModule.FileName (fastest, but fails for protected processes)
                    // (EN/FR: Essai Méthode 1: MainModule.FileName (rapide, mais échoue pour processus protégés))
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById((int)processId);
                        string exePath = process.MainModule.FileName;
                        string exeName = System.IO.Path.GetFileName(exePath);
                        
                        // Ignore WiimoteGun itself (EN/FR: Ignorer WiimoteGun lui-même)
                        if (exeName.Equals("WiimoteGun.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            SimpleLogger.Instance.Debug("Ignoring WiimoteGun.exe in detection");
                            return;
                        }
                        
                        _currentExecutablePath = exePath;
                        _currentExecutable = exeName;
                        SimpleLogger.Instance.Info($"Detected current game (Method 1): {_currentExecutable} [{_currentExecutablePath}]");
                        return; // Success, exit early
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Access denied - try alternative method
                        // (EN/FR: Accès refusé - essayer méthode alternative)
                        SimpleLogger.Instance.Info($"MainModule access denied for process {processId}, trying QueryFullProcessImageName...");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Process exited
                        SimpleLogger.Instance.Warning($"Process {processId} exited: {ex.Message}");
                        _currentExecutable = "Unknown";
                        _currentExecutablePath = null;
                        return;
                    }
                    
                    // Try Method 2: QueryFullProcessImageName (works with protected processes)
                    // (EN/FR: Essai Méthode 2: QueryFullProcessImageName (fonctionne avec processus protégés))
                    IntPtr hProcess = IntPtr.Zero;
                    try
                    {
                        hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                        if (hProcess != IntPtr.Zero)
                        {
                            System.Text.StringBuilder buffer = new System.Text.StringBuilder(1024);
                            uint size = (uint)buffer.Capacity;
                            
                            if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
                            {
                                string exePath = buffer.ToString();
                                string exeName = System.IO.Path.GetFileName(exePath);
                                
                                // Ignore WiimoteGun itself (EN/FR: Ignorer WiimoteGun lui-même)
                                if (exeName.Equals("WiimoteGun.exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    SimpleLogger.Instance.Debug("Ignoring WiimoteGun.exe in detection (Method 2)");
                                    return;
                                }
                                
                                _currentExecutablePath = exePath;
                                _currentExecutable = exeName;
                                SimpleLogger.Instance.Info($"Detected current game (Method 2): {_currentExecutable} [{_currentExecutablePath}]");
                                return; // Success
                            }
                            else
                            {
                                SimpleLogger.Instance.Warning($"QueryFullProcessImageName failed for process {processId}");
                            }
                        }
                        else
                        {
                            SimpleLogger.Instance.Warning($"OpenProcess failed for process {processId}");
                        }
                    }
                    finally
                    {
                        if (hProcess != IntPtr.Zero)
                            CloseHandle(hProcess);
                    }
                    
                    // Both methods failed
                    // (EN/FR: Les deux méthodes ont échoué)
                    _currentExecutable = "Unknown (Detection Failed)";
                    _currentExecutablePath = null;
                }
                else
                {
                    _currentExecutable = "Unknown";
                    _currentExecutablePath = null;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to detect current game: {ex.Message}");
                _currentExecutable = "Unknown";
                _currentExecutablePath = null;
            }
        }

        private void UpdateCurrentGameLabel()
        {
            lblCurrentGame.Text = $"Current Game: {_currentExecutable}";
            
            // Update linked exe label based on currently loaded profile
            // (EN/FR: Mettre à jour label exe lié basé sur profil chargé)
            string currentProfile = Program.GetActiveRemapProfile();
            if (!string.IsNullOrEmpty(currentProfile))
            {
                string linkedExe = GameProfileMappingManager.GetExecutableForProfile(currentProfile);
                if (!string.IsNullOrEmpty(linkedExe))
                {
                    lblLinkedExe.Text = $"Linked to: {linkedExe}";
                    lblLinkedExe.ForeColor = ColorAccent;
                }
                else
                {
                    lblLinkedExe.Text = "Not linked to any executable";
                    lblLinkedExe.ForeColor = Color.Gray;
                }
            }
            else
            {
                lblLinkedExe.Text = "";
            }
        }

        private void LoadProfiles()
        {
            try
            {
                comboBoxProfiles.Items.Clear();
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString() ?? "[Root]";
                string subfolder = selectedFolder == "[Root]" ? "" : selectedFolder;
                
                var profiles = RemapProfileManager.GetProfilesInFolder(subfolder);
                foreach (var profile in profiles)
                {
                    comboBoxProfiles.Items.Add(profile);
                }
                
                // After loading profile, update UI
                UpdateCurrentGameLabel();
                
                // Update Auto-load checkbox state
                // Only check if the CURRENT game is mapped to the CURRENT profile
                // (EN/FR: Cocher seulement si jeu ACTUEL est mappé au profil ACTUEL)
                if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
                {
                    string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                    string currentProfile = Program.GetActiveRemapProfile();
                    
                    // Check if mapping exists AND matches current profile
                    _updatingCheckbox = true;
                    if (!string.IsNullOrEmpty(mappedProfile) && 
                        !string.IsNullOrEmpty(currentProfile) && 
                        mappedProfile.Equals(currentProfile, StringComparison.OrdinalIgnoreCase))
                    {
                        chkAutoLoad.Checked = true;
                    }
                    else
                    {
                        chkAutoLoad.Checked = false;
                    }
                    chkAutoLoad.Enabled = true;
                    _updatingCheckbox = false;
                }
                else
                {
                    _updatingCheckbox = true;
                    chkAutoLoad.Checked = false;
                    chkAutoLoad.Enabled = false; // Disable if no game detected
                    _updatingCheckbox = false;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load profiles: {ex.Message}");
            }
        }

        private void ProfileOverlay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Hide();
                e.Handled = true;
            }
        }
        
        private void ProfileOverlay_Activated(object sender, EventArgs e)
        {
            // Focus the form to capture keyboard input (EN/FR: Focaliser le formulaire pour capturer entrée clavier)
            this.Focus();
            this.Activate();
        }
        
        private void ProfileOverlay_MouseClick(object sender, MouseEventArgs e)
        {
            // Right-click to return to home page (EN/FR: Clic droit pour retourner à la page d'accueil)
            if (e.Button == MouseButtons.Right)
            {
                ShowPage("Home");
            }
        }
        
        private void ProfileOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            // Magnetic pointer effect - attract cursor to nearby interactive controls
            // (EN/FR: Effet pointer magnétique - attirer curseur vers contrôles interactifs proches)
            
            // Get current cursor position in form coordinates
            Point cursorPos = e.Location;
            
            // Find the closest interactive control within magnetic radius
            Control closestControl = null;
            float closestDistance = float.MaxValue;
            Point closestCenter = Point.Empty;
            
            // Search all controls recursively (EN/FR: Chercher tous contrôles récursivement)
            foreach (Control ctrl in GetAllControls(this))
            {
                // Only target interactive controls (buttons, checkboxes, comboboxes)
                // (EN/FR: Cibler uniquement contrôles interactifs)
                if (!IsInteractiveControl(ctrl) || !ctrl.Visible || !ctrl.Enabled)
                    continue;
                
                // Get control center in form coordinates
                Point controlCenter = ctrl.Parent.PointToScreen(new Point(
                    ctrl.Location.X + ctrl.Width / 2,
                    ctrl.Location.Y + ctrl.Height / 2
                ));
                controlCenter = this.PointToClient(controlCenter);
                
                // Calculate distance from cursor to control center
                float distance = (float)Math.Sqrt(
                    Math.Pow(cursorPos.X - controlCenter.X, 2) +
                    Math.Pow(cursorPos.Y - controlCenter.Y, 2)
                );
                
                // Check if within magnetic radius and closer than previous
                if (distance < MAGNETIC_RADIUS && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestControl = ctrl;
                    closestCenter = controlCenter;
                }
            }
            
            // Apply magnetic attraction if a control was found
            if (closestControl != null)
            {
                // Calculate attraction vector (EN/FR: Calculer vecteur d'attraction)
                float deltaX = closestCenter.X - cursorPos.X;
                float deltaY = closestCenter.Y - cursorPos.Y;
                
                // Apply magnetic strength with smooth interpolation (lerp)
                // (EN/FR: Appliquer force magnétique avec interpolation douce)
                int attractedX = cursorPos.X + (int)(deltaX * MAGNETIC_STRENGTH);
                int attractedY = cursorPos.Y + (int)(deltaY * MAGNETIC_STRENGTH);
                
                // Convert to screen coordinates and move cursor
                // (EN/FR: Convertir en coordonnées écran et déplacer curseur)
                Point screenPos = this.PointToScreen(new Point(attractedX, attractedY));
                
                // Only move cursor if the new position is different enough (prevent jitter)
                // (EN/FR: Déplacer seulement si nouvelle position assez différente pour éviter tremblement)
                if (Math.Abs(screenPos.X - Cursor.Position.X) > 1 || 
                    Math.Abs(screenPos.Y - Cursor.Position.Y) > 1)
                {
                    Cursor.Position = screenPos;
                }
            }
        }
        
        /// <summary>
        /// Get all controls recursively (EN/FR: Récupérer tous contrôles récursivement)
        /// </summary>
        private IEnumerable<Control> GetAllControls(Control container)
        {
            foreach (Control ctrl in container.Controls)
            {
                yield return ctrl;
                
                // Recursively get children (EN/FR: Récupérer enfants récursivement)
                foreach (Control childCtrl in GetAllControls(ctrl))
                {
                    yield return childCtrl;
                }
            }
        }
        
        /// <summary>
        /// Check if control is interactive (EN/FR: Vérifier si contrôle est interactif)
        /// </summary>
        private bool IsInteractiveControl(Control ctrl)
        {
            return ctrl is Button || 
                   ctrl is CheckBox || 
                   ctrl is ComboBox || 
                   ctrl is RadioButton ||
                   ctrl is NumericUpDown ||
                   ctrl is TrackBar;
        }

        /// <summary>
        /// Prevent form from taking focus (EN/FR: Empêcher le formulaire de prendre le focus)
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Only apply NOACTIVATE in overlay mode (EN/FR: Appliquer NOACTIVATE seulement en mode overlay)
                if (!_windowedMode)
                {
                    cp.ExStyle |= WS_EX_NOACTIVATE; // Don't steal focus from game
                }
                return cp;
            }
        }
        
        protected override bool ShowWithoutActivation => !_windowedMode;
        
        public bool IsWindowedMode => _windowedMode;

        private void SetupModernUI()
        {
            // Form settings (EN/FR: Paramètres du formulaire)
            if (!_windowedMode)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.Size = new Size(600, 840); // Increased height for all 4 players with rumble settings (EN/FR: Hauteur augmentée pour 4 joueurs avec paramètres rumble)
            }
            else
            {
                // In windowed mode, set ClientSize to ensure content fits (EN/FR: En mode fenêtré, définir ClientSize pour assurer que le contenu rentre)
                this.AutoScaleMode = AutoScaleMode.None; // Disable auto-scaling (EN/FR: Désactiver mise à l'échelle auto)
                this.ClientSize = new Size(600, 840);
                this.MinimumSize = this.Size; // Prevent resizing smaller than content (EN/FR: Empêcher redimensionnement plus petit que contenu)
                this.FormBorderStyle = FormBorderStyle.None; // Custom border (EN/FR: Bordure personnalisée)
                SetupCustomTitleBar();
            }
            
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBackground;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.95;
            
            // Rounded corners (EN/FR: Coins arrondis)
            GraphicsPath path = new GraphicsPath();
            int radius = 12;
            Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            this.Region = new Region(path);
            
            // Set font (EN/FR: Définir police)
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        }
        
        // Custom Title Bar Fields
        private Panel pnlTitleBar;
        private Label lblTitleBarText;
        private Button btnTitleMinimize;
        private Button btnTitleClose;
        private Point _dragStartPoint;
        private bool _isDragging = false;

        private void SetupCustomTitleBar()
        {
            pnlTitleBar = new Panel
            {
                Size = new Size(this.Width, 32),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(45, 45, 48), // Dark gray
                Dock = DockStyle.Top
            };
            
            lblTitleBarText = new Label
            {
                Text = "WiimoteGun - Overlay",
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
            pnlTitleBar.Controls.Add(btnTitleClose); // Added last to be right-most (Dock.Right stack)
            
            // Re-order controls for Dock.Right: Last added is closest to edge? 
            // Actually Dock.Right stacks from right to left. 
            // So if I add Close then Minimize, Close is rightmost, Minimize is left of it.
            // Let's verify: 
            // pnlTitleBar.Controls.Add(btnTitleClose); -> Rightmost
            // pnlTitleBar.Controls.Add(btnTitleMinimize); -> Left of Close
            // My code above added Minimize then Close. So Minimize is Rightmost, Close is left of it.
            // Wait, Dock order is reverse of addition for standard collections? 
            // "The z-order of the controls determines how they are docked... The control at the top of the z-order is docked last."
            // Add() puts at end of collection (top of z-order?).
            // Let's just swap them to be safe or use BringToFront.
            // Correct order: Add Close (Rightmost), Add Minimize (Left of Close).
            
            pnlTitleBar.Controls.Clear();
            pnlTitleBar.Controls.Add(lblTitleBarText);
            pnlTitleBar.Controls.Add(btnTitleMinimize);
            pnlTitleBar.Controls.Add(btnTitleClose); 
            // With this order: Close is added last -> Top of Z-order -> Docked closest to edge?
            // Actually, let's just use explicit BringToFront if needed.
            // Standard behavior: First added is at edge.
            // Let's try: Add Close, Add Minimize. Close is at edge.
            
            pnlTitleBar.Controls.Clear();
            pnlTitleBar.Controls.Add(lblTitleBarText);
            pnlTitleBar.Controls.Add(btnTitleMinimize); // Left of Close
            pnlTitleBar.Controls.Add(btnTitleClose);    // Rightmost
            
            // Dragging
            pnlTitleBar.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.MouseMove += TitleBar_MouseMove;
            pnlTitleBar.MouseUp += TitleBar_MouseUp;
            lblTitleBarText.MouseDown += TitleBar_MouseDown;
            lblTitleBarText.MouseMove += TitleBar_MouseMove;
            lblTitleBarText.MouseUp += TitleBar_MouseUp;
            
            this.Controls.Add(pnlTitleBar);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point p = PointToScreen(e.Location);
                // We need to calculate the new form location
                // p is current screen mouse pos. _dragStartPoint is mouse pos relative to control (TitleBar).
                // But TitleBar is at 0,0 of form.
                // So Form.Location should be p - _dragStartPoint.
                
                // However, PointToScreen(e.Location) gives screen coords of mouse.
                // If I click at 10,10 in TitleBar.
                // Move mouse to screen 500,500.
                // Form TopLeft should be 500-10, 500-10.
                
                // There is a catch: PointToScreen uses the control's coordinate system.
                // If I move the form, the control moves, so PointToScreen result changes?
                // No, PointToScreen converts client point to screen point.
                
                // Standard drag logic:
                this.Location = new Point(this.Location.X + (e.X - _dragStartPoint.X), this.Location.Y + (e.Y - _dragStartPoint.Y));
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }
        
        private void ShowPage(string pageName)
        {
            // Hide all panels
            if (panelHome != null) panelHome.Visible = false;
            if (panelMapping != null) panelMapping.Visible = false;
            //if (panelOptions != null) panelOptions.Visible = false;
            //if (panelAssign != null) panelAssign.Visible = false;
            //if (panelIRViz != null) panelIRViz.Visible = false;
            
            // Show requested panel
            switch (pageName)
            {
                case "Home":
                    if (panelHome != null) panelHome.Visible = true;
                    if (btnBackToHome != null) btnBackToHome.Visible = false;
                    if (lblTitle != null) lblTitle.Visible = false; // Hide old title
                    // Hide Save/Load buttons on Home page (EN/FR: Cacher boutons Save/Load sur page Home)
                    if (btnSave != null) btnSave.Visible = false;
                    if (btnLoad != null) btnLoad.Visible = false;
                    break;
                    
                case "Mapping":
                    if (panelMapping != null) panelMapping.Visible = true;
                    if (btnBackToHome != null) btnBackToHome.Visible = true;
                    if (lblTitle != null) lblTitle.Visible = false; // Hide old title
                    // Show Save/Load buttons on Mapping page (EN/FR: Afficher boutons Save/Load sur page Mapping)
                    if (btnSave != null) btnSave.Visible = true;
                    if (btnLoad != null) btnLoad.Visible = true;
                    // Load mappings if needed
                    LoadCurrentMappings();
                    break;
                    
                    
                case "Options":
                    //if (panelOptions != null) panelOptions.Visible = true;
                    if (btnBackToHome != null) btnBackToHome.Visible = true;
                    if (lblTitle != null) lblTitle.Visible = false;
                    // Hide Save/Load buttons on Options page (EN/FR: Cacher boutons Save/Load sur page Options)
                    if (btnSave != null) btnSave.Visible = false;
                    if (btnLoad != null) btnLoad.Visible = false;
                    break;
                    
                case "Assign":
                    //if (panelAssign != null)
                    //{
                    //    panelAssign.Visible = true;
                    //    LoadAssignPage(); // Refresh list when showing page
                    //}
                    if (btnBackToHome != null) btnBackToHome.Visible = true;
                    if (lblTitle != null) lblTitle.Visible = false;
                    if (btnSave != null) btnSave.Visible = false;
                    if (btnLoad != null) btnLoad.Visible = false;
                    break;
                    
                case "IRViz":
                    //if (panelIRViz != null)
                    //{
                    //    panelIRViz.Visible = true;
                    //    LoadIRPage();
                    //}
                    if (btnBackToHome != null) btnBackToHome.Visible = true;
                    if (lblTitle != null) lblTitle.Visible = false;
                    // Hide Save/Load buttons on IR page (EN/FR: Cacher boutons Save/Load sur page IR)
                    if (btnSave != null) btnSave.Visible = false;
                    if (btnLoad != null) btnLoad.Visible = false;
                    break;
            }
            
            // Footer always visible
            if (lblFooter != null)
            {
                lblFooter.Visible = true;
                lblFooter.BringToFront();
            }
            
            // Manage Close button visibility - ONLY on Home page (EN/FR: Gérer visibilité bouton Close - UNIQUEMENT sur page Home)
            if (btnClose != null)
            {
                btnClose.Visible = (pageName == "Home");
                if (btnClose.Visible)
                    btnClose.BringToFront();
            }
            
            // Ensure Save/Load buttons are on top if visible
            if (btnSave != null && btnSave.Visible) btnSave.BringToFront();
            if (btnLoad != null && btnLoad.Visible) btnLoad.BringToFront();
        }

        /// <summary>
        /// Show overlay with fade-in animation (EN/FR: Afficher overlay avec animation)
        /// </summary>
        public new void Show()
        {
            // Refresh game detection when showing
            DetectCurrentGame();
            UpdateCurrentGameLabel();
            
            // Check if we have a mapping for this game
            if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
            {
                string profilePath = GameProfileMappingManager.GetProfileForGame(_currentExecutable);
                if (!string.IsNullOrEmpty(profilePath))
                {
                    chkAutoLoad.Checked = true;
                    // Pre-fill fields based on existing mapping if possible
                    string profileName = System.IO.Path.GetFileNameWithoutExtension(profilePath);
                    txtProfileName.Text = profileName;
                }
                else
                {
                    chkAutoLoad.Checked = false;
                }
            }

            this.Opacity = 0;
            base.Show();
            
            // Fade in animation (EN/FR: Animation fondu)
            Timer fadeTimer = new Timer();
            fadeTimer.Interval = 20;
            fadeTimer.Tick += (s, e) =>
            {
                if (this.Opacity < 0.95)
                    this.Opacity += 0.05;
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
            };
            fadeTimer.Start();
        }
        
        /// <summary>
        /// Hide overlay with fade-out animation (EN/FR: Masquer overlay avec animation)
        /// </summary>
        public new void Hide()
        {
            Timer fadeTimer = new Timer();
            fadeTimer.Interval = 20;
            fadeTimer.Tick += (s, e) =>
            {
                if (this.Opacity > 0)
                    this.Opacity -= 0.05;
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    base.Hide();
                }
            };
            fadeTimer.Start();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border (EN/FR: Dessiner bordure)
            using (Pen borderPen = new Pen(ColorBorder, 2))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                GraphicsPath path = new GraphicsPath();
                int radius = 12;
                Rectangle bounds = new Rectangle(1, 1, this.Width - 2, this.Height - 2);
                path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
                path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
                path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                e.Graphics.DrawPath(borderPen, path);
            }
        }
        
        // Win32 API (EN/FR: API Win32)
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        private void ProfileOverlay_Load(object sender, EventArgs e)
        {
            LoadSubfolders();
            LoadProfiles();
            LoadCurrentMappings();
            UpdateCurrentGameLabel();
            PresetActiveProfile(); // Preselect current active profile (EN/FR: Présélectionner profil actif actuel)
        }
        
        private void LoadSubfolders()
        {
            try
            {
                comboBoxSubfolders.Items.Clear();
                comboBoxSubfolders.Items.Add("[Root]");
                
                var subfolders = RemapProfileManager.GetSubfolders();
                foreach (var folder in subfolders)
                {
                    comboBoxSubfolders.Items.Add(folder);
                }
                
                comboBoxSubfolders.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load subfolders: {ex.Message}");
            }
        }
        
        private void comboBoxSubfolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Reload profiles when subfolder selection changes (EN/FR: Recharger profils lors changement sous-dossier)
            LoadProfiles();
        }
        
        private void PresetActiveProfile()
        {
            // Preset the currently active profile in UI fields (EN/FR: Présélectionner profil actif dans champs UI)
            try
            {
                string activeProfile = Program.GetActiveRemapProfile();
                if (string.IsNullOrEmpty(activeProfile))
                {
                    SimpleLogger.Instance.Info("No active profile to preset");
                    return;
                }
                
                SimpleLogger.Instance.Info($"Presetting active profile: {activeProfile}");
                
                // Parse subfolder and filename (EN/FR: Analyser sous-dossier et nom fichier)
                string subfolder = "";
                string filename = activeProfile;
                
                int lastSlash = activeProfile.LastIndexOf('/');
                if (lastSlash < 0)
                    lastSlash = activeProfile.LastIndexOf('\\');
                    
                if (lastSlash >= 0)
                {
                    subfolder = activeProfile.Substring(0, lastSlash);
                    filename = activeProfile.Substring(lastSlash + 1);
                }
                
                // Set subfolder (EN/FR: Définir sous-dossier)
                string subfolderDisplay = string.IsNullOrEmpty(subfolder) ? "[Root]" : subfolder;
                for (int i = 0; i < comboBoxSubfolders.Items.Count; i++)
                {
                    if (comboBoxSubfolders.Items[i].ToString() == subfolderDisplay)
                    {
                        comboBoxSubfolders.SelectedIndex = i;
                        break;
                    }
                }
                
                // Reload profiles for selected subfolder (EN/FR: Recharger profils pour sous-dossier sélectionné)
                LoadProfiles();
                
                // Set profile in comboBox (EN/FR: Définir profil dans comboBox)
                _isProgrammaticUpdate = true;
                for (int i = 0; i < comboBoxProfiles.Items.Count; i++)
                {
                    if (comboBoxProfiles.Items[i].ToString() == filename)
                    {
                        comboBoxProfiles.SelectedIndex = i;
                        break;
                    }
                }
                _isProgrammaticUpdate = false;
                
                // Set profile name in textbox (EN/FR: Définir nom profil dans champ texte)
                // Remove .remap extension (EN/FR: Retirer extension .remap)
                string profileNameWithoutExt = filename.Replace(".remap", "");
                _isProgrammaticUpdate = true;
                txtProfileName.Text = profileNameWithoutExt;
                _isProgrammaticUpdate = false;
                
                SimpleLogger.Instance.Info($"Preset complete: Subfolder='{subfolderDisplay}', Profile='{filename}'");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to preset active profile: {ex.Message}");
            }
        }
        

        
        private void LoadCurrentMappings()
        {
            // Load all 4 players (EN/FR: Charger les 4 joueurs)
            LoadPlayerMappings(panelP1Mappings, Options.Instance.P1Mappings);
            LoadPlayerMappings(panelP2Mappings, Options.Instance.P2Mappings);
            LoadPlayerMappings(panelP3Mappings, Options.Instance.P3Mappings);
            LoadPlayerMappings(panelP4Mappings, Options.Instance.P4Mappings);
            
            // Update auto-load checkbox based on current mapping (EN/FR: Mettre à jour checkbox auto-load selon mapping actuel)
            UpdateAutoLoadCheckbox();
        }
        
        private void UpdateAutoLoadCheckbox()
        {
            if (chkAutoLoad == null)
            {
                SimpleLogger.Instance.Debug("UpdateAutoLoadCheckbox: Skipped (chkAutoLoad is null)");
                return;
            }
                
            try
            {
                // Get current active profile (EN/FR: Obtenir profil actif actuel)
                string currentProfile = Program.GetActiveRemapProfile();
                if (!string.IsNullOrEmpty(currentProfile))
                {
                    // Check: Is the CURRENT executable mapped to the CURRENT profile?
                    // (EN/FR: Vérifier : Est-ce que l'exécutable ACTUEL est mappé vers le profil ACTUEL ?)
                    string mappedProfile = null;
                    bool hasMapping = false;
                    
                    if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
                    {
                        // Get which profile is mapped to the current executable (EN/FR: Obtenir quel profil est mappé à l'exécutable actuel)
                        mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                        
                        // Normalize paths for comparison (handle / vs \ differences) (EN/FR: Normaliser chemins pour comparaison)
                        string normalizedCurrent = currentProfile?.Replace('\\', '/');
                        string normalizedMapped = mappedProfile?.Replace('\\', '/');
                        
                        // Checkbox should be checked if the mapped profile matches the current profile
                        // (EN/FR: Checkbox doit être cochée si le profil mappé correspond au profil actuel)
                        hasMapping = !string.IsNullOrEmpty(normalizedMapped) && 
                                     normalizedCurrent.Equals(normalizedMapped, StringComparison.OrdinalIgnoreCase);
                    }
                    
                    SimpleLogger.Instance.Info($"UpdateAutoLoadCheckbox: currentProfile='{currentProfile}', _currentExecutable='{_currentExecutable}', mappedProfile='{mappedProfile}', hasMapping={hasMapping}");
                    
                    // Update checkbox without triggering event (EN/FR: Mettre à jour checkbox sans déclencher événement)
                    _updatingCheckbox = true;
                    chkAutoLoad.Checked = hasMapping;
                    
                    // Enable checkbox if we have a valid executable to link (EN/FR: Activer checkbox si on a un exécutable valide à lier)
                    bool isValidExe = !string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != " Unknown";
                    chkAutoLoad.Enabled = isValidExe;
                    
                    _updatingCheckbox = false;
                }
                else
                {
                    SimpleLogger.Instance.Debug("UpdateAutoLoadCheckbox: currentProfile is empty");
                    _updatingCheckbox = true;
                    chkAutoLoad.Checked = false;
                    chkAutoLoad.Enabled = false;
                    _updatingCheckbox = false;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error updating auto-load checkbox: {ex.Message}");
                _updatingCheckbox = false;
            }
        }
        
        private void LoadPlayerMappings(Panel panel, PlayerMappings mappings)
        {
            panel.Controls.Clear();
            
            // Panel width is 528px (EN/FR: Largeur du panel 528px)
            int panelWidth = 528;
            int yPos = 15;
            int labelWidth = 95;
            int valueWidth = 120;
            int spacing = 22;
            int columnSpacing = 35;
            
            // Calculate total width of both columns (EN/FR: Calculer largeur totale des deux colonnes)
            int column1Width = labelWidth + valueWidth;
            int column2Width = labelWidth + valueWidth;
            int totalWidth = column1Width + columnSpacing + column2Width;
            
            // Center both columns (EN/FR: Centrer les deux colonnes)
            int startX = (panelWidth - totalWidth) / 2;
            
            // Column 1: Wiimote (EN/FR: Colonne 1 : Wiimote)
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
            
            // Add Wiimote mappings (EN/FR: Ajouter mappings Wiimote)
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
            
            // Column 2: Nunchuk (EN/FR: Colonne 2 : Nunchuk)
            int col2X = col1X + column1Width + columnSpacing;
            yPos = 15; // Reset y position for second column
            
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
            
            // Add Nunchuk mappings (EN/FR: Ajouter mappings Nunchuk)
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
            if (mapping == null) return "Not mapped";
            
            if (mapping.Special != SpecialAction.None)
            {
                return mapping.Special.ToString();
            }
            else if (mapping.Key != System.Windows.Forms.Keys.None)
            {
                return mapping.Key.ToString();
            }
            
            return "Not mapped";
        }
        

        
        private void btnNewFolder_Click(object sender, EventArgs e)
        {
            string folderName = _modalInputDialog.ShowDialog(this, "Enter new subfolder name:");
            
            if (!_modalInputDialog.WasCancelled && !string.IsNullOrWhiteSpace(folderName))
            {
                try
                {
                    string remapDir = RemapProfileManager.GetRemapDirectory();
                    string newFolderPath = System.IO.Path.Combine(remapDir, folderName);
                    System.IO.Directory.CreateDirectory(newFolderPath);
                    
                    LoadSubfolders();
                    comboBoxSubfolders.SelectedItem = folderName;
                    
                    _toastNotification.Show($"✓ Subfolder '{folderName}' created", 2000);
                }
                catch (Exception ex)
                {
                    _toastNotification.Show($"✗ Failed to create folder: {ex.Message}", 3000);
                }
            }
        }
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            string profileName = txtProfileName.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                _toastNotification.Show("⚠ Please enter a profile name", 2000);
                return;
            }
            
            try
            {
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString() ?? "[Root]";
                string subfolder = selectedFolder == "[Root]" ? "" : selectedFolder;
                
                // Create profile from current mappings (EN/FR: Créer profil depuis mappings actuels)
                var profile = new RemapProfile
                {
                    ProfileName = profileName,
                    P1Mappings = Options.Instance.P1Mappings,
                    P2Mappings = Options.Instance.P2Mappings,
                    P3Mappings = Options.Instance.P3Mappings,
                    P4Mappings = Options.Instance.P4Mappings
                };
                
                bool success = RemapProfileManager.SaveProfile(profileName, subfolder, profile);
                
                if (success)
                {
                    // Save game mapping if auto-load is checked (EN/FR: Sauvegarder mapping jeu si auto-load coché)
                    if (chkAutoLoad.Checked && !string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown")
                    {
                        string profilePath = string.IsNullOrEmpty(subfolder) 
                            ? $"{profileName}.remap" 
                            : $"{subfolder}/{profileName}.remap";
                        // Pass full path for strict matching (EN/FR: Passer chemin complet pour correspondance stricte)
                        GameProfileMappingManager.AddMapping(_currentExecutable, profilePath, _currentExecutablePath);
                        SimpleLogger.Instance.Info($"Mapped {_currentExecutablePath} to {profilePath}");
                    }
                    else if (!chkAutoLoad.Checked && !string.IsNullOrEmpty(_currentExecutable))
                    {
                        // Remove mapping if unchecked
                        GameProfileMappingManager.RemoveMapping(_currentExecutable);
                    }
                    
                    // Set as active profile in Program (EN/FR: Définir comme profil actif dans Program)
                    string savedProfilePath = string.IsNullOrEmpty(subfolder) 
                        ? $"{profileName}.remap" 
                        : $"{subfolder}/{profileName}.remap";
                    Program.LoadRemapProfileHot(savedProfilePath, true);
                    
                    LoadProfiles();
                    
                    // Restore selection to the saved profile (EN/FR: Restaurer sélection au profil sauvegardé)
                    string expectedFileName = $"{profileName}.remap";
                    _isProgrammaticUpdate = true;
                    for (int i = 0; i < comboBoxProfiles.Items.Count; i++)
                    {
                        if (comboBoxProfiles.Items[i].ToString() == expectedFileName)
                        {
                            comboBoxProfiles.SelectedIndex = i;
                            break;
                        }
                    }
                    _isProgrammaticUpdate = false;
                    
                    _toastNotification.Show($"✓ Profile '{profileName}' saved", 2000);
                }
                else
                {
                    _toastNotification.Show("✗ Failed to save profile", 3000);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save profile: {ex.Message}");
                _toastNotification.Show($"✗ Error: {ex.Message}", 3000);
            }
        }
        
        private void btnLoad_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                _toastNotification.Show("⚠ Please select a profile to load", 2000);
                return;
            }
            
            try
            {
                string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString() ?? "[Root]";
                string relativePath;
                
                if (selectedFolder == "[Root]" || string.IsNullOrEmpty(selectedFolder))
                    relativePath = selectedProfile;
                else
                    relativePath = System.IO.Path.Combine(selectedFolder, selectedProfile);
                
                var profile = RemapProfileManager.LoadProfile(relativePath);
                
                if (profile != null)
                {
                    // Apply to running instance (EN/FR: Appliquer à l'instance en cours)
                    if (profile.P1Mappings != null)
                        Options.Instance.P1Mappings.CopyFrom(profile.P1Mappings);
                    if (profile.P2Mappings != null)
                        Options.Instance.P2Mappings.CopyFrom(profile.P2Mappings);
                    if (profile.P3Mappings != null)
                        Options.Instance.P3Mappings.CopyFrom(profile.P3Mappings);
                    if (profile.P4Mappings != null)
                        Options.Instance.P4Mappings.CopyFrom(profile.P4Mappings);
                    
                    Options.Instance.Save();
                    
                    // Set as active profile in Program (EN/FR: Définir comme profil actif dans Program)
                    Program.LoadRemapProfileHot(relativePath, true);
                    
                    // Fill profile name for easy re-save (EN/FR: Remplir nom profil pour re-save facile)
                    _isProgrammaticUpdate = true;
                    txtProfileName.Text = profile.ProfileName;
                    _isProgrammaticUpdate = false;
                    
                    // Signal manual profile change to prevent auto-reload (EN/FR: Signaler changement manuel)
                    Program.SetManualProfileOverride();
                    
                    _toastNotification.Show($"✓ Profile '{profile.ProfileName}' loaded", 2000);
                    
                    SimpleLogger.Instance.Info($"Manually loaded profile: {profile.ProfileName}. Updating UI...");
                    
                    LoadCurrentMappings(); // Refresh preview
                    panelMapping.Refresh(); // Force redraw (EN/FR: Forcer redessin)
                    
                    UpdateCurrentGameLabel(); // Update linked exe display (EN/FR: Mettre à jour affichage exe lié)
                    
                    // Force checkbox update explicitly and log result (EN/FR: Forcer mise à jour checkbox explicitement et logger résultat)
                    UpdateAutoLoadCheckbox();
                    SimpleLogger.Instance.Info($"UI update complete. Checkbox state: {chkAutoLoad.Checked}");
                    
                    // Update auto-load checkbox based on loaded profile - ALREADY DONE by UpdateAutoLoadCheckbox above
                    // (EN/FR: Mettre à jour case auto-load selon profil chargé - DÉJÀ FAIT par UpdateAutoLoadCheckbox ci-dessus)
                    // Redundant code removed (EN/FR: Code redondant supprimé)
                }
                else
                {
                    _toastNotification.Show("✗ Failed to load profile", 3000);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load profile: {ex.Message}");
                _toastNotification.Show($"✗ Error: {ex.Message}", 3000);
            }
        }
        
        private void chkAutoLoad_CheckedChanged(object sender, EventArgs e)
        {
            SimpleLogger.Instance.Info($"chkAutoLoad_CheckedChanged called: Checked={chkAutoLoad.Checked}, Enabled={chkAutoLoad.Enabled}, _updatingCheckbox={_updatingCheckbox}");
            
            // Guard against recursive calls (EN/FR: Garde contre appels récursifs)
            if (_updatingCheckbox)
            {
                SimpleLogger.Instance.Info("Skipping due to _updatingCheckbox flag");
                return;
            }
            
            // Handle auto-load toggle (EN/FR: Gérer activation/désactivation auto-load)
            if (!chkAutoLoad.Enabled)
            {
                SimpleLogger.Instance.Info("Skipping due to checkbox disabled");
                return; // Skip if disabled
            }
            
            try
            {
                if (chkAutoLoad.Checked)
                {
                    SimpleLogger.Instance.Info("Auto-load checked - attempting to create mapping");
                    // Create mapping (EN/FR: Créer mapping)
                    // Check if we have a valid executable path (from auto-detection OR manual selection)
                    // (EN/FR: Vérifier si chemin exécutable valide (détection auto OU sélection manuelle))
                    if (!string.IsNullOrEmpty(_currentExecutablePath) && !string.IsNullOrEmpty(_currentExecutable))
                    {
                        SimpleLogger.Instance.Info($"Valid exe detected: {_currentExecutable} [{_currentExecutablePath}]");
                        // Get currently active profile (EN/FR: Obtenir profil actif)
                        string currentProfile = Program.GetActiveRemapProfile();
                        SimpleLogger.Instance.Info($"Current active profile: {currentProfile}");
                        
                        if (!string.IsNullOrEmpty(currentProfile))
                        {
                            SimpleLogger.Instance.Info($"Adding mapping: {_currentExecutablePath} -> {currentProfile}");
                            GameProfileMappingManager.AddMapping(_currentExecutable, currentProfile, _currentExecutablePath);
                            _toastNotification.Show($"✓ Auto-load enabled for {_currentExecutable}", 2000);
                            UpdateCurrentGameLabel(); // Refresh linked exe display
                            SimpleLogger.Instance.Info("Mapping added successfully");
                        }
                        else
                        {
                            SimpleLogger.Instance.Warning("No active profile to link auto-load");
                            _toastNotification.Show("⚠ No profile is currently active", 2000);
                            _updatingCheckbox = true;
                            chkAutoLoad.Checked = false;
                            _updatingCheckbox = false;
                        }
                    }
                    else
                    {
                        SimpleLogger.Instance.Warning($"Cannot enable auto-load: exePath={_currentExecutablePath}, exe={_currentExecutable}");
                        _toastNotification.Show("⚠ Please select an executable first (click ... button)", 3000);
                        _updatingCheckbox = true;
                        chkAutoLoad.Checked = false;
                        _updatingCheckbox = false;
                    }
                }
                else
                {
                    SimpleLogger.Instance.Info("Auto-load unchecked - attempting to remove mapping");
                    // Remove mapping (EN/FR: Supprimer mapping)
                    if (!string.IsNullOrEmpty(_currentExecutable))
                    {
                        SimpleLogger.Instance.Info($"Removing mapping for {_currentExecutable}");
                        GameProfileMappingManager.RemoveMapping(_currentExecutable);
                        _toastNotification.Show($"✓ Auto-load disabled for {_currentExecutable}", 2000);
                        UpdateCurrentGameLabel(); // Refresh linked exe display
                        SimpleLogger.Instance.Info("Mapping removed successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Exception in chkAutoLoad_CheckedChanged: {ex.Message}\n{ex.StackTrace}");
                _toastNotification.Show($"✗ Error: {ex.Message}", 3000);
                // Ensure flag is reset even on exception (EN/FR: S'assurer que le flag est réinitialisé même en cas d'erreur)
                _updatingCheckbox = false;
            }
        }
        
        private void chkEnableGyro_CheckedChanged(object sender, EventArgs e)
        {
            // Get the currently selected player tab (EN/FR: Obtenir l'onglet joueur actuellement sélectionné)
            if (tabControlPlayers == null || tabControlPlayers.SelectedTab == null)
                return;
            
            // Determine which player is selected (1-4) (EN/FR: Déterminer quel joueur est sélectionné)
            int playerIndex = tabControlPlayers.SelectedIndex + 1; // SelectedIndex is 0-based
            
            // Get the mappings for the selected player (EN/FR: Obtenir les mappings pour le joueur sélectionné)
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            if (mappings == null)
                return;
            
            // Update the EnableGyroAiming property (EN/FR: Mettre à jour la propriété EnableGyroAiming)
            mappings.EnableGyroAiming = chkEnableGyro.Checked;
            
            SimpleLogger.Instance.Info($"Gyro Aiming {(chkEnableGyro.Checked ? "enabled" : "disabled")} for Player {playerIndex}");
            
            // Show notification (EN/FR: Afficher notification)
            if (chkEnableGyro.Checked)
                _toastNotification.Show($"🎯 Gyro Aiming enabled for Player {playerIndex}", 2000);
            else
                _toastNotification.Show($"Gyro Aiming disabled for Player {playerIndex}", 2000);
        }
        
        private void tabControlPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update gyro checkbox when switching tabs (EN/FR: Mettre à jour checkbox gyro lors du changement d'onglet)
            if (tabControlPlayers == null || tabControlPlayers.SelectedTab == null || chkEnableGyro == null)
                return;
            
            // Get the newly selected player (EN/FR: Obtenir le joueur nouvellement sélectionné)
            int playerIndex = tabControlPlayers.SelectedIndex + 1;
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            
            if (mappings != null)
            {
                // Update checkbox state to match the player's setting (EN/FR: Mettre à jour état checkbox pour correspondre au réglage du joueur)
                chkEnableGyro.Checked = mappings.EnableGyroAiming;
            }
        }
        
        private void btnSelectExe_Click(object sender, EventArgs e)
        {
            // Open file dialog to manually select executable (EN/FR: Ouvrir dialogue fichier pour sélection manuelle exécutable)
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Game/Application Executable";
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentExecutablePath = openFileDialog.FileName;
                        _currentExecutable = System.IO.Path.GetFileName(_currentExecutablePath);
                        
                        SimpleLogger.Instance.Info($"Manually selected executable: {_currentExecutable} [{_currentExecutablePath}]");
                        
                        // Update UI (EN/FR: Mettre à jour UI)
                        UpdateCurrentGameLabel();
                        
                        // Update checkbox state (EN/FR: Mettre à jour état case)
                        _updatingCheckbox = true;
                        if (!string.IsNullOrEmpty(_currentExecutable))
                        {
                            string mappedProfile = GameProfileMappingManager.GetProfileForGame(_currentExecutable, _currentExecutablePath);
                            string currentProfile = Program.GetActiveRemapProfile();
                            
                            if (!string.IsNullOrEmpty(mappedProfile) && 
                                !string.IsNullOrEmpty(currentProfile) && 
                                mappedProfile.Equals(currentProfile, StringComparison.OrdinalIgnoreCase))
                            {
                                chkAutoLoad.Checked = true;
                            }
                            else
                            {
                                chkAutoLoad.Checked = false;
                            }
                            chkAutoLoad.Enabled = true;
                        }
                        else
                        {
                            chkAutoLoad.Checked = false;
                            chkAutoLoad.Enabled = false;
                        }
                        _updatingCheckbox = false;
                        
                        _toastNotification.Show($"✓ Selected: {_currentExecutable}", 2000);
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error($"Failed to select executable: {ex.Message}");
                        _toastNotification.Show($"✗ Error: {ex.Message}", 3000);
                    }
                }
            }
        }
        
        /// <summary>
        /// Event handler: Profile Name text changed (EN/FR: Nom de profil modifié)
        /// </summary>
        private void txtProfileName_TextChanged(object sender, EventArgs e)
        {
            // Skip if this is a programmatic update (EN/FR: Ignorer si mise à jour programmatique)
            if (_isProgrammaticUpdate)
                return;
                
            // User manually changed profile name, clear the Load Profile selection (EN/FR: Utilisateur a modifié manuellement, effacer sélection)
            // This indicates intent to create a NEW profile rather than update existing (EN/FR: Indique intention de créer NOUVEAU profil)
            if (comboBoxProfiles != null && comboBoxProfiles.SelectedIndex != -1)
            {
                _isProgrammaticUpdate = true;
                comboBoxProfiles.SelectedIndex = -1;
                _isProgrammaticUpdate = false;
                SimpleLogger.Instance.Info($"Profile name manually changed to '{txtProfileName.Text}'. Cleared Load Profile selection.");
            }
        }
        
        /// <summary>
        /// Event handler: Show virtual keyboard when clicking profile name textbox
        /// (EN/FR: Afficher clavier virtuel au clic sur textbox nom de profil)
        /// </summary>
        private void txtProfileName_Click(object sender, EventArgs e)
        {
            ShowVirtualKeyboard(txtProfileName);
        }
        
        /// <summary>
        /// Show virtual keyboard for text input (EN/FR: Afficher clavier virtuel pour saisie texte)
        /// </summary>
        private void ShowVirtualKeyboard(TextBox targetTextBox)
        {
            if (targetTextBox == null) return;

            // Create and show virtual keyboard (EN/FR: Créer et afficher clavier virtuel)
            VirtualKeyboard keyboard = new VirtualKeyboard(targetTextBox);
            
            // Center on parent form (EN/FR: Centrer sur formulaire parent)
            keyboard.StartPosition = FormStartPosition.Manual;
            keyboard.Location = new Point(
                this.Location.X + (this.Width - keyboard.Width) / 2,
                this.Location.Y + (this.Height - keyboard.Height) / 2
            );
            
            keyboard.ShowDialog(this);
        }
        
        /// <summary>
        /// Event handler: Delete selected profile (EN/FR: Supprimer profil sélectionné)
        /// </summary>
        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            // Check if a profile is selected (EN/FR: Vérifier si profil sélectionné)
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                _toastNotification.Show("⚠ Please select a profile to delete", 2000);
                return;
            }
            
            // Show confirmation dialog (EN/FR: Afficher dialogue de confirmation)
            string message = $"Are you sure you want to delete the profile:\n\n{selectedProfile}\n\nThis action cannot be undone.";
            DialogResult result = MessageBox.Show(
                message, 
                "Delete Profile Confirmation", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2 // Default to "No" for safety (EN/FR: Par défaut "Non" pour sécurité)
            );
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Get the full path to delete (EN/FR: Obtenir chemin complet à supprimer)
                    string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString() ?? "[Root]";
                    string subfolder = selectedFolder == "[Root]" ? "" : selectedFolder;
                    string profilePath = RemapProfileManager.GetProfilePath(selectedProfile, subfolder);
                    
                    // Delete the file (EN/FR: Supprimer le fichier)
                    if (System.IO.File.Exists(profilePath))
                    {
                        System.IO.File.Delete(profilePath);
                        SimpleLogger.Instance.Info($"Deleted profile: {profilePath}");
                        
                        // If this was the active profile, clear it (EN/FR: Si profil actif, l'effacer)
                        string currentProfile = Program.GetActiveRemapProfile();
                        string deletedProfileRelative = string.IsNullOrEmpty(subfolder) ? selectedProfile : $"{subfolder}/{selectedProfile}";
                        
                        if (!string.IsNullOrEmpty(currentProfile) && 
                            currentProfile.Equals(deletedProfileRelative, StringComparison.OrdinalIgnoreCase))
                        {
                            // Clear active profile (EN/FR: Effacer profil actif)
                            Program.LoadRemapProfileHot("", false);
                            _isProgrammaticUpdate = true;
                            txtProfileName.Text = "";
                            _isProgrammaticUpdate = false;
                        }
                        
                        // Remove from game mapping if exists (EN/FR: Retirer du mapping jeu si existe)
                        GameProfileMappingManager.RemoveMappingByProfile(deletedProfileRelative);
                        
                        // Reload the list (EN/FR: Recharger la liste)
                        LoadProfiles();
                        
                        _toastNotification.Show($"✓ Profile '{selectedProfile}' deleted", 2000);
                    }
                    else
                    {
                        _toastNotification.Show($"✗ Profile file not found: {selectedProfile}", 3000);
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"Failed to delete profile: {ex.Message}");
                    _toastNotification.Show($"✗ Error: {ex.Message}", 3000);
                }
            }
        }
        
        // ============================================
        // Button Assignment Feature (EN/FR: Fonctionnalité assignation bouton)
        // ============================================
        
        private void btnAssignMode_Click(object sender, EventArgs e)
        {
            if (_isAssignMode)
            {
                // Already in assign mode, exit (EN/FR: Déjà en mode assign, sortir)
                ExitAssignMode();
                return;
            }
            
            EnterAssignMode();
        }
        
        /// <summary>
        /// Event handler: Open hotkey editor for current player
        /// (EN/FR: Gestionnaire d'événements : Ouvrir éditeur hotkeys pour joueur actuel)
        /// </summary>
        private void btnHotkeys_Click(object sender, EventArgs e)
        {
            // Get current player index from selected tab (1-based)
            int playerIndex = tabControlPlayers.SelectedIndex + 1;
            
            // Open hotkey editor dialog
            using (var dialog = new HotkeyEditorDialog(playerIndex))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Update hotkey profile in manager
                    HotkeyManager.SetProfile(playerIndex, dialog.HotkeyProfile);
                    
                    SimpleLogger.Instance.Info($"Hotkeys updated for Player {playerIndex}");
                    _toastNotification.Show($"✓ Hotkeys saved for Player {playerIndex}", 2000);
                }
            }
        }
        
        private void EnterAssignMode()
        {
            _isAssignMode = true;
            
            // Update UI (EN/FR: Mettre à jour UI)
            btnAssignMode.Text = "✖ Cancel Assign";
            btnAssignMode.BackColor = Color.FromArgb(180, 0, 0); // Red
           
            // Center popup overlay on visible tab area (EN/FR: Centrer overlay popup sur zone tabs visible)
            // Visible area is approximately from Y=250 to Y=600 (tabs area)
            int visibleTop = 250;
            int visibleHeight = 350;
            int centerY = visibleTop + (visibleHeight - lblAssignStatus.Height) / 2;
            
            lblAssignStatus.Location = new Point(
                (this.ClientSize.Width - lblAssignStatus.Width) / 2,
                centerY
            );
            lblAssignStatus.Text = $"⏱ Press any Wiimote/Nunchuk button\n({_assignCountdownSeconds}s)";
            lblAssignStatus.ForeColor = Color.Orange;
            lblAssignStatus.Visible = true;
            lblAssignStatus.BringToFront(); // Ensure it's on top (EN/FR: S'assurer qu'il est au dessus)
            
            // Lock inputs on all connected controllers (EN/FR: Verrouiller inputs sur tous contrôleurs connectés)
            LockWiimoteInputs(true);
            
            // Start countdown timer (EN/FR: Démarrer timer countdown)
            _assignCountdownSeconds = 8;
            _assignCountdownTimer = new System.Threading.Timer(OnAssignCountdownTick, null, 1000, 1000);
            
            SimpleLogger.Instance.Info("Entered button assignment mode");
        }
        
        private void ExitAssignMode()
        {
            _isAssignMode = false;
            
            // Stop countdown timer (EN/FR: Arrêter timer countdown)
            if (_assignCountdownTimer != null)
            {
                _assignCountdownTimer.Dispose();
                _assignCountdownTimer = null;
            }
            
            // Unlock inputs (EN/FR: Déverrouiller inputs)
            LockWiimoteInputs(false);
            
            // Reset UI (EN/FR: Réinitialiser UI)
            btnAssignMode.Text = "✏️ Assign Button";
            btnAssignMode.BackColor = ColorAccent;
            lblAssignStatus.Visible = false;
            comboActionSelector.Visible = false;
            btnConfirmAssign.Visible = false;
            btnCancelAssign.Visible = false;
            
            // Reset state (EN/FR: Réinitialiser état)
            _waitingForButton = null;
            _waitingForPlayer = 1;
            _originalMapping = null;
            
            SimpleLogger.Instance.Info("Exited button assignment mode");
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
                // Timeout, exit assign mode (EN/FR: Délai expiré, sortir mode assign)
                ExitAssignMode();
                _toastNotification.Show("⚠ Assignment timeout", 2000);
                return;
            }
            
            
            // Update label with countdown (EN/FR: Mettre à jour label avec compte à rebours)
            lblAssignStatus.Text = $"⏱ Press any Wiimote/Nunchuk button\n({_assignCountdownSeconds}s)";
            
            // Change color when < 3 seconds (EN/FR: Changer couleur quand < 3 secondes)
            if (_assignCountdownSeconds <= 3)
                lblAssignStatus.ForeColor = Color.Red;
        }
        
        private void LockWiimoteInputs(bool locked)
        {
            // Lock/unlock inputs via WiiMoteController (EN/FR: Verrouiller/déverrouiller inputs via WiiMoteController)
            WiiMoteController.SetInputLock(locked);
            
            if (locked)
            {
                // Subscribe to button press event (EN/FR: S'abonner à événement pression bouton)
                WiiMoteController.ButtonPressed += OnWiimoteButtonPressed;
            }
            else
            {
                // Unsubscribe from event (EN/FR: Se désabonner de l'événement)
                WiiMoteController.ButtonPressed -= OnWiimoteButtonPressed;
            }
        }
        
        private void OnWiimoteButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Called when a button is pressed while in assign mode (EN/FR: Appelé quand un bouton est pressé en mode assignation)
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnWiimoteButtonPressed(sender, e)));
                return;
            }
            
            SimpleLogger.Instance.Info($"Button detected in assign mode: {e.ButtonName} from P{e.PlayerIndex}");
            
            // Stop countdown timer (EN/FR: Arrêter timer countdown)
            if (_assignCountdownTimer != null)
            {
                _assignCountdownTimer.Dispose();
                _assignCountdownTimer = null;
            }
            
            // Store which button we're assigning (EN/FR: Stocker quel bouton on assigne)
            _waitingForButton = e.ButtonName;
            _waitingForPlayer = e.PlayerIndex;
            
            // Get current mapping for this button to allow cancellation (EN/FR: Obtenir mapping actuel pour permettre annulation)
            _originalMapping = GetCurrentMapping(e.PlayerIndex, e.ButtonName);
            
            // Show action selector (EN/FR: Afficher sélecteur d'action)
            ShowActionSelector(e.ButtonName);
        }
        
        private ButtonAction GetCurrentMapping(int playerIndex, string buttonName)
        {
            // Get player mappings (EN/FR: Obtenir mappings joueur)
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            
            // Return the current mapping for this button (EN/FR: Retourner le mapping actuel pour ce bouton)
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
                default: return new ButtonAction(); // Empty action
            }
        }
        
        private void ShowActionSelector(string buttonName)
        {
            // Fill action selector with available actions (EN/FR: Remplir sélecteur avec actions disponibles)
            comboActionSelector.Items.Clear();
            comboActionSelector.Items.Add("None");
            comboActionSelector.Items.Add("Mouse Left Click");
            comboActionSelector.Items.Add("Mouse Right Click");
            comboActionSelector.Items.Add("Mouse Middle Click");
            comboActionSelector.Items.Add("Keyboard Key...");
            
            // Select current mapping if any (EN/FR: Sélectionner mapping actuel si existe)
            string currentActionText = GetActionDisplayText(_originalMapping);
            int index = comboActionSelector.Items.IndexOf(currentActionText);
            if (index >= 0)
                comboActionSelector.SelectedIndex = index;
            else
                comboActionSelector.SelectedIndex = 0;
            
            // Position selector near button label and show (EN/FR: Positionner sélecteur près libellé bouton et afficher)
            lblAssignStatus.Text = $"Select action for {buttonName}:";
            lblAssignStatus.ForeColor = Color.LightGreen;
            
            comboActionSelector.Visible = true;
            btnConfirmAssign.Visible = true;
            btnCancelAssign.Visible = true;
            
            // Bring to front to ensure visibility (EN/FR: Mettre au premier plan pour assurer visibilité)
            comboActionSelector.BringToFront();
            btnConfirmAssign.BringToFront();
            btnCancelAssign.BringToFront();
        }
        
        private string GetActionDisplayText(ButtonAction action)
        {
            if (action == null) return "None";
            
            // Check if it's a special action (EN/FR: Vérifier si action spéciale)
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
            
            // Check if it's a keyboard key (EN/FR: Vérifier si touche clavier)
            if (action.Key != Keys.None)
            {
                return $"Key: {action.Key}";
            }
            
            return "None";
        }
        
        private void btnConfirmAssign_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_waitingForButton) || comboActionSelector.SelectedIndex < 0)
            {
                ExitAssignMode();
                return;
            }
            
            // Parse selected action (EN/FR: Analyser action sélectionnée)
            string selectedAction = comboActionSelector.SelectedItem.ToString();
            ButtonAction newAction = CreateButtonActionFromSelection(selectedAction);
            
            if (newAction == null)
            {
                // User cancelled the dialog, stay in assign mode (EN/FR: Utilisateur a annulé, rester en mode assign)
                SimpleLogger.Instance.Info("User cancelled action selection");
                return; // Don't exit assign mode (EN/FR: Ne pas sortir du mode assign)
            }
            
            // Apply the new mapping (EN/FR: Appliquer le nouveau mapping)
            ApplyMapping(_waitingForPlayer, _waitingForButton, newAction);
            
            // Save Options (EN/FR: Sauvegarder Options)
            Options.Instance.Save();
            
            // Reload mapping display (EN/FR: Recharger affichage mappings)
            LoadCurrentMappings();
            
            SimpleLogger.Instance.Info($"Assigned {selectedAction} to {_waitingForButton} for P{_waitingForPlayer}");
            _toastNotification.Show($"✓ {_waitingForButton} → {selectedAction}", 2500);
            
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
                    // Open dialog to select key (EN/FR: Ouvrir dialogue pour sélectionner touche)
                    using (KeySelectorDialog keyDialog = new KeySelectorDialog())
                    {
                        if (keyDialog.ShowDialog(this) == DialogResult.OK && keyDialog.SelectedKey != Keys.None)
                        {
                            return new ButtonAction(keyDialog.SelectedKey);
                        }
                    }
                    return null;
                case "Gamepad Button...":
                    // TODO: Open dialog to select gamepad button (EN/FR: Ouvrir dialogue pour sélectionner bouton manette)
                    _toastNotification.Show("⚠ Gamepad button selection not yet implemented", 3000);
                    return null;
                default:
                    return null;
            }
        }
        
        private void ApplyMapping(int playerIndex, string buttonName, ButtonAction action)
        {
            // Get player mappings (EN/FR: Obtenir mappings joueur)
            PlayerMappings mappings = Options.Instance.GetMappingsForPlayer(playerIndex);
            
            // Apply the mapping (EN/FR: Appliquer le mapping)
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
        
        private void btnCancelAssign_Click(object sender, EventArgs e)
        {
            // Restore original mapping if any (EN/FR: Restaurer mapping original si existe)
            if (_originalMapping != null && !string.IsNullOrEmpty(_waitingForButton))
            {
                // TODO: Restore original mapping (EN/FR: Restaurer mapping original)
                SimpleLogger.Instance.Info($"Cancelled assignment for {_waitingForButton}");
            }
            
            ExitAssignMode();
            _toastNotification.Show("✖ Assignment cancelled", 2000);
        }
        
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
