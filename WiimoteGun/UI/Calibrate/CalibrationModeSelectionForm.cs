using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI.Calibrate
{
    /// <summary>
    /// Full-screen form for selecting between Dynamic Mode and Standard Calibration
    /// (EN/FR: Formulaire plein écran pour choisir entre Mode Dynamique et Calibration Standard)
    /// </summary>
    public partial class CalibrationModeSelectionForm : Form
    {
        private bool _selectionMade = false;
        
        public bool UseDynamicMode { get; private set; }
        public bool SelectionMade { get { return _selectionMade; } }

        public CalibrationModeSelectionForm(int monitorId, string modeName)
        {
            InitializeComponent();
            
            // Set dynamic text
            _lblTitle.Text = string.Format("CHOOSE {0} TRACKING MODE", modeName.ToUpper());
            this.Text = string.Format("{0} - Calibration Mode Selection", modeName);
            
            // Set FlatAppearance border sizes (Designer doesn't support this)
            _btnDynamic.FlatAppearance.BorderSize = 3;
            _btnDynamic.FlatAppearance.BorderColor = Color.White;
            _btnStandard.FlatAppearance.BorderSize = 3;
            _btnStandard.FlatAppearance.BorderColor = Color.White;
            
            PositionOnMonitor(monitorId);
        }
        
        // Event handlers (EN/FR: Gestionnaires d'événements)
        private void CalibrationModeSelectionForm_Shown(object sender, EventArgs e)
        {
            this.Activate();
            this.Focus();
            this.BringToFront();
        }
        
        private void CalibrationModeSelectionForm_Load(object sender, EventArgs e)
        {
            PositionButtons();
        }
        
        private void CalibrationModeSelectionForm_Resize(object sender, EventArgs e)
        {
            PositionButtons();
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
