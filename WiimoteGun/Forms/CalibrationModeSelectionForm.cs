using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Full-screen form for selecting between Dynamic Mode and Standard Calibration
    /// (EN/FR: Formulaire plein écran pour choisir entre Mode Dynamique et Calibration Standard)
    /// </summary>
    public class CalibrationModeSelectionForm : Form
    {
        private Button _btnDynamic;
        private Button _btnStandard;
        private Label _lblTitle;
        private Label _lblSubtitle;
        private bool _selectionMade = false;
        
        public bool UseDynamicMode { get; private set; }
        public bool SelectionMade { get { return _selectionMade; } }

        public CalibrationModeSelectionForm(int monitorId, string modeName)
        {
            InitializeComponent(modeName);
            PositionOnMonitor(monitorId);
        }

        private void InitializeComponent(string modeName)
        {
            // Form setup (EN/FR: Configuration du formulaire)
            this.Text = $"{modeName} - Calibration Mode Selection";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.StartPosition = FormStartPosition.Manual;
            this.KeyPreview = true;
            this.TopMost = true; // Force to foreground (EN/FR: Forcer au premier plan)
            this.KeyDown += CalibrationModeSelectionForm_KeyDown;
            
            // Ensure form activates and focuses when shown (EN/FR: S'assurer que le formulaire s'active au premier plan)
            this.Shown += (s, e) => 
            { 
                this.Activate();
                this.Focus();
                this.BringToFront();
            };

            // Title Label (EN/FR: Label titre)
            _lblTitle = new Label();
            _lblTitle.Text = $"CHOOSE {modeName.ToUpper()} TRACKING MODE";
            _lblTitle.Font = new Font("Arial", 36, FontStyle.Bold);
            _lblTitle.ForeColor = Color.White;
            _lblTitle.AutoSize = false;
            _lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            _lblTitle.Dock = DockStyle.Top;
            _lblTitle.Height = 100;
            this.Controls.Add(_lblTitle);

            // Subtitle Label (EN/FR: Sous-titre)
            _lblSubtitle = new Label();
            _lblSubtitle.Text = "Select your preferred tracking method";
            _lblSubtitle.Font = new Font("Arial", 20, FontStyle.Regular);
            _lblSubtitle.ForeColor = Color.LightGray;
            _lblSubtitle.AutoSize = false;
            _lblSubtitle.TextAlign = ContentAlignment.TopCenter;
            _lblSubtitle.Dock = DockStyle.Top;
            _lblSubtitle.Height = 60;
            this.Controls.Add(_lblSubtitle);

            // Container Panel for Buttons (EN/FR: Panneau conteneur pour les boutons)
            Panel buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.BackColor = Color.Black;

            // Dynamic Mode Button (EN/FR: Bouton Mode Dynamique)
            _btnDynamic = new Button();
            _btnDynamic.Text = "DYNAMIC MODE\n(AUTO)\n\n✓ No calibration needed\n✓ Fixes staircase effect\n✓ Absolute perspective";
            _btnDynamic.Font = new Font("Arial", 24, FontStyle.Bold);
            _btnDynamic.BackColor = Color.FromArgb(0, 120, 215); // Windows Blue
            _btnDynamic.ForeColor = Color.White;
            _btnDynamic.FlatStyle = FlatStyle.Flat;
            _btnDynamic.FlatAppearance.BorderSize = 3;
            _btnDynamic.FlatAppearance.BorderColor = Color.White;
            _btnDynamic.Cursor = Cursors.Hand;
            _btnDynamic.Click += BtnDynamic_Click;
            _btnDynamic.MouseEnter += Btn_MouseEnter;
            _btnDynamic.MouseLeave += Btn_MouseLeave;

            // Standard Calibration Button (EN/FR: Bouton Calibration Standard)
            _btnStandard = new Button();
            _btnStandard.Text = "STANDARD CALIBRATION\n(MANUAL)\n\n• Classic 5-point calibration\n• Manual precision\n• Use if Dynamic is inaccurate";
            _btnStandard.Font = new Font("Arial", 24, FontStyle.Bold);
            _btnStandard.BackColor = Color.FromArgb(80, 80, 80); // Dark Gray
            _btnStandard.ForeColor = Color.White;
            _btnStandard.FlatStyle = FlatStyle.Flat;
            _btnStandard.FlatAppearance.BorderSize = 3;
            _btnStandard.FlatAppearance.BorderColor = Color.White;
            _btnStandard.Cursor = Cursors.Hand;
            _btnStandard.Click += BtnStandard_Click;
            _btnStandard.MouseEnter += Btn_MouseEnter;
            _btnStandard.MouseLeave += Btn_MouseLeave;

            buttonPanel.Controls.Add(_btnDynamic);
            buttonPanel.Controls.Add(_btnStandard);
            this.Controls.Add(buttonPanel);

            // Position buttons when form loads (EN/FR: Positionner les boutons au chargement)
            this.Load += (s, e) => PositionButtons();
            this.Resize += (s, e) => PositionButtons();
        }

        private void PositionButtons()
        {
            // Calculate button size and position (EN/FR: Calculer taille et position des boutons)
            int buttonWidth = Math.Min(600, this.ClientSize.Width / 2 - 100);
            int buttonHeight = Math.Min(400, this.ClientSize.Height - 200);
            int spacing = 50;
            
            int totalWidth = buttonWidth * 2 + spacing;
            int startX = (this.ClientSize.Width - totalWidth) / 2;
            int startY = (this.ClientSize.Height - buttonHeight) / 2 + 80; // +80 for title offset

            _btnDynamic.SetBounds(startX, startY, buttonWidth, buttonHeight);
            _btnStandard.SetBounds(startX + buttonWidth + spacing, startY, buttonWidth, buttonHeight);
        }

        private void PositionOnMonitor(int monitorId)
        {
            // Position form on specified monitor (EN/FR: Positionner le formulaire sur l'écran spécifié)
            if (monitorId >= 0 && monitorId < Screen.AllScreens.Length)
            {
                Screen targetScreen = Screen.AllScreens[monitorId];
                this.Location = targetScreen.Bounds.Location;
                this.Size = targetScreen.Bounds.Size;
            }
            else
            {
                // Default to primary screen (EN/FR: Écran principal par défaut)
                this.Location = Screen.PrimaryScreen.Bounds.Location;
                this.Size = Screen.PrimaryScreen.Bounds.Size;
            }
        }

        private void BtnDynamic_Click(object sender, EventArgs e)
        {
            UseDynamicMode = true;
            _selectionMade = true;
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        private void BtnStandard_Click(object sender, EventArgs e)
        {
            UseDynamicMode = false;
            _selectionMade = true;
            this.DialogResult = DialogResult.No;
            this.Close();
        }

        private void Btn_MouseEnter(object sender, EventArgs e)
        {
            // Hover effect: Brighten button (EN/FR: Effet survol : éclaircir le bouton)
            Button btn = sender as Button;
            if (btn != null)
            {
                btn.FlatAppearance.BorderColor = Color.Yellow;
                btn.FlatAppearance.BorderSize = 5;
            }
        }

        private void Btn_MouseLeave(object sender, EventArgs e)
        {
            // Reset hover effect (EN/FR: Réinitialiser l'effet survol)
            Button btn = sender as Button;
            if (btn != null)
            {
                btn.FlatAppearance.BorderColor = Color.White;
                btn.FlatAppearance.BorderSize = 3;
            }
        }

        private void CalibrationModeSelectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow ESC to cancel (EN/FR: Permettre ESC pour annuler)
            if (e.KeyCode == Keys.Escape)
            {
                _selectionMade = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            // 1 key = Dynamic, 2 key = Standard (EN/FR: Touche 1 = Dynamique, 2 = Standard)
            else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                BtnDynamic_Click(null, null);
            }
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                BtnStandard_Click(null, null);
            }
        }
    }
}
