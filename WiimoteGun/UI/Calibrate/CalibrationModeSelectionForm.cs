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

        private string _modeName;

        // [V59] EN: Configurable button/subtitle texts for the PnP fisheye selection.
        //     When null, the original Gun4IR/4Corners labels are used.
        //     FR: Textes configurables des boutons/sous-titre pour la sélection fisheye PnP.
        //     Si null, les libellés originaux Gun4IR/4Corners sont utilisés.
        public string DynamicButtonText { get; set; }
        public string StandardButtonText { get; set; }
        public string SubtitleText { get; set; }

        public CalibrationModeSelectionForm(int monitorId, string modeName)
        {
            InitializeComponent();
            WiimoteGun.UI.UiScaler.ApplyForm(this); // [V57n] UI zoom (bounds, fonts, screen clamp)
            _modeName = modeName;

            // Set dynamic text
            _lblTitle.Text = string.Format("CHOOSE {0} TRACKING MODE", modeName.ToUpper());
            this.Text = string.Format("{0} - Calibration Mode Selection", modeName);

            // [V59] EN: Apply custom labels when provided (PnP fisheye mode)
            //     FR: Appliquer les libellés personnalisés si fournis (mode fisheye PnP)
            if (!string.IsNullOrEmpty(DynamicButtonText)) _btnDynamic.Text = DynamicButtonText;
            if (!string.IsNullOrEmpty(StandardButtonText)) _btnStandard.Text = StandardButtonText;
            if (!string.IsNullOrEmpty(SubtitleText)) _lblSubtitle.Text = SubtitleText;

            // Hide subtitle and standard button for 4 Corners (EN: Masquer sous-titre et bouton standard)
            // [V59-FIX-7] EN: Gun4IR Diamond joins the same pattern (user decision): the
            //     manual calibration option is REMOVED — calibration is AUTOMATIC only.
            //     Single centered button + subtitle explaining it.
            //     FR: Gun4IR Diamond rejoint le même schéma (décision user) : l'option
            //     de calibration manuelle est RETIRÉE — calibration AUTOMATIQUE
            //     uniquement. Bouton unique centré + sous-titre explicatif.
            if (_modeName == "4 Corners")
            {
                _lblSubtitle.Visible = false;
                _btnStandard.Visible = false;
            }
            else if (_modeName == "Gun4IR")
            {
                _btnDynamic.Text = "AUTOMATIC / AUTOMATIQUE";
                _lblSubtitle.Text = "Calibration is AUTOMATIC — no manual points needed.\r\nLa calibration est AUTOMATIQUE — aucun point manuel requis.";
                _btnStandard.Visible = false;
            }
            
            // Set FlatAppearance border sizes (Designer doesn't support this)
            _btnDynamic.FlatAppearance.BorderSize = 3;
            _btnDynamic.FlatAppearance.BorderColor = Color.White;
            _btnStandard.FlatAppearance.BorderSize = 3;
            _btnStandard.FlatAppearance.BorderColor = Color.White;
            
            PositionOnMonitor(monitorId);
        }
        
        /// <summary>
        /// [V59] EN: Constructor for the PnP fisheye selection — custom button labels
        ///     ("WITH FISHEYE / GRAND ANGLE" vs "WITHOUT LENS (STANDARD)") and subtitle.
        ///     FR: Constructeur pour la sélection fisheye PnP — libellés personnalisés
        ///     (« WITH FISHEYE / GRAND ANGLE » vs « WITHOUT LENS (STANDARD) ») et sous-titre.
        /// </summary>
        public CalibrationModeSelectionForm(int monitorId, string modeName,
            string dynamicText, string standardText, string subtitleText)
            : this(monitorId, modeName)
        {
            DynamicButtonText = dynamicText;
            StandardButtonText = standardText;
            SubtitleText = subtitleText;
            // Re-apply in case base constructor already ran with null values
            _btnDynamic.Text = dynamicText;
            _btnStandard.Text = standardText;
            _lblSubtitle.Text = subtitleText;
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
            
            // [V59-FIX-7] EN/FR: Single centered button for 4 Corners AND Gun4IR Diamond
            //     (automatic-calibration-only modes).
            if (_modeName == "4 Corners" || _modeName == "Gun4IR")
            {
                // Center the single Dynamic Button
                int startX = (this.ClientSize.Width - buttonWidth) / 2;
                int startY = (this.ClientSize.Height - buttonHeight) / 2 + 80;
                _btnDynamic.SetBounds(startX, startY, buttonWidth, buttonHeight);
            }
            else
            {
                int totalWidth = buttonWidth * 2 + spacing;
                int startX = (this.ClientSize.Width - totalWidth) / 2;
                int startY = (this.ClientSize.Height - buttonHeight) / 2 + 80; // +80 for title offset

                _btnDynamic.SetBounds(startX, startY, buttonWidth, buttonHeight);
                _btnStandard.SetBounds(startX + buttonWidth + spacing, startY, buttonWidth, buttonHeight);
            }
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
            // [V59-FIX-7] EN: The "2" shortcut only works when the Standard button is
            //     visible — on automatic-only modes (4 Corners, Gun4IR Diamond) the
            //     hidden option must stay unreachable by keyboard too.
            //     FR: Le raccourci « 2 » ne fonctionne que si le bouton Standard est
            //     visible — sur les modes automatiques uniquement (4 Corners, Gun4IR
            //     Diamond), l'option masquée doit rester inaccessible au clavier aussi.
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                if (_btnStandard.Visible) BtnStandard_Click(null, null);
            }
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                BtnStandard_Click(null, null);
            }
        }
    }
}
