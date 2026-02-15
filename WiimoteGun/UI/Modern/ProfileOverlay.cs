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
        
        public bool IsWindowedMode { get { return _windowedMode; } }

        public ProfileOverlay(bool windowedMode)
        {
            _windowedMode = windowedMode;
            InitializeComponent();
            SetupModernUI();
            InitializeControls();
            
            // Game Detection Timer
            _gameDetectTimer = new System.Windows.Forms.Timer();
            _gameDetectTimer.Interval = 1000;
            _gameDetectTimer.Tick += (s, e) => DetectCurrentGame();
            _gameDetectTimer.Start();
        }

        private void InitializeControls()
        {
            int topOffset = _windowedMode ? 32 : 0;
            Size contentSize = new Size(560, 780); // Standardize size (increased for AssignControl)
            Point contentLoc = new Point((this.Width - contentSize.Width) / 2, 30 + topOffset);

            // Home
            homeControl = new HomeControl { Location = contentLoc, Visible = true };
            homeControl.OptionsClicked += (s, e) => ShowPage("Options");
            homeControl.MappingClicked += (s, e) => ShowPage("Mapping");
            homeControl.AssignClicked += (s, e) => ShowPage("Assign");
            homeControl.IRVizClicked += (s, e) => ShowPage("IRViz");
            this.Controls.Add(homeControl);

            // Assign
            assignControl = new AssignControl { Location = contentLoc, Visible = false };
            this.Controls.Add(assignControl);

            // Options
            optionsControl = new OptionsControl { Location = contentLoc, Visible = false };
            this.Controls.Add(optionsControl);
            
            // Mapping
            mappingControl = new MappingControl { Location = contentLoc, Visible = false };
            this.Controls.Add(mappingControl);

            // IR
            irControl = new IRControl { Location = contentLoc, Visible = false };
            this.Controls.Add(irControl);

            // GamePad Mapping
            gamePadMappingControl = new GamePadMappingControl { Location = contentLoc, Visible = false };
            this.Controls.Add(gamePadMappingControl);

            // Wire Internal Back Events
            assignControl.BackRequested += (s, e) => ShowPage("Home");
            optionsControl.BackRequested += (s, e) => ShowPage("Home");
            mappingControl.BackRequested += (s, e) => ShowPage("Home");
            mappingControl.GamePadMappingRequested += (s, e) => ShowPage("GamePadMapping");
            irControl.BackRequested += (s, e) => ShowPage("Home");
            gamePadMappingControl.BackRequested += (s, e) => ShowPage("Mapping");
        }

        /* 
        // Global button removed in favor of internal buttons per user request
        private Button btnBackToHome; 
        */


        private void ShowPage(string page)
        {
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

            // btnBackToHome logic removed

            switch (page)
            {
                case "Home":
                    homeControl.Visible = true;
                    break;
                case "Options":
                    optionsControl.Visible = true;
                    break;
                case "Mapping":
                    mappingControl.Visible = true;
                    // refresh mapping data if needed
                     mappingControl.SetCurrentGame(_currentExecutable);
                    break;
                case "Assign":
                    assignControl.Visible = true;
                    assignControl.LoadData();
                    break;
                case "IRViz":
                    irControl.Visible = true;
                    irControl.LoadData();
                    break;
                case "GamePadMapping":
                    gamePadMappingControl.Visible = true;
                    gamePadMappingControl.LoadData();
                    break;
            }
        }

        private void SetupModernUI()
        {
            this.BackColor = Color.FromArgb(20, 20, 20); // Dark background
            this.DoubleBuffered = true;
            
             if (!_windowedMode)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.Size = new Size(600, 840);
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
                this.ClientSize = new Size(600, 840);
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
                
                string processName = p.ProcessName;
                if (processName.ToLower() == "explorer" || processName.ToLower() == "searchhost") return; // Ignore shell

                if (processName + ".exe" != _currentExecutable)
                {
                    _currentExecutable = processName + ".exe";
                    // Update Mapping Control
                    if (mappingControl != null)
                        mappingControl.SetCurrentGame(_currentExecutable);
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
