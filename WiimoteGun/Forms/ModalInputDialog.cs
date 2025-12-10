using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Modal input dialog overlay for text input
    /// (EN/FR: Dialogue modal overlay pour saisie de texte)
    /// </summary>
    public class ModalInputDialog : Form
    {
        private Panel _backgroundPanel;
        private Panel _dialogPanel;
        private Label _promptLabel;
        private TextBox _inputTextBox;
        private Button _okButton;
        private Button _cancelButton;
        
        public string InputValue { get; private set; }
        public bool WasCancelled { get; private set; }
        
        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorPanel = Color.FromArgb(37, 37, 37);
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);
        private static readonly Color ColorBorder = Color.FromArgb(63, 63, 63);

        public ModalInputDialog()
        {
            // Form settings (EN/FR: Paramètres du formulaire)
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.85;
            this.Dock = DockStyle.Fill;
            
            // Background panel (EN/FR: Panneau d'arrière-plan)
            _backgroundPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            _backgroundPanel.Click += (s, e) => Cancel();
            
            // Dialog panel (EN/FR: Panneau de dialogue)
            _dialogPanel = new Panel
            {
                Size = new Size(350, 160),
                BackColor = ColorPanel
            };
            
            // Prompt label (EN/FR: Label de demande)
            _promptLabel = new Label
            {
                Text = "Enter folder name:",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(20, 20),
                Size = new Size(310, 25)
            };
            
            // Input textbox (EN/FR: Zone de texte)
            _inputTextBox = new TextBox
            {
                BackColor = ColorBackground,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 55),
                Size = new Size(310, 25),
                BorderStyle = BorderStyle.FixedSingle
            };
            _inputTextBox.KeyDown += InputTextBox_KeyDown;
            
            // OK button (EN/FR: Bouton OK)
            _okButton = new Button
            {
                Text = "OK",
                BackColor = ColorAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(170, 100),
                Size = new Size(75, 35)
            };
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Click += OkButton_Click;
            
            // Cancel button (EN/FR: Bouton Annuler)
            _cancelButton = new Button
            {
                Text = "Cancel",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(255, 100),
                Size = new Size(75, 35)
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Click += CancelButton_Click;
            
            _dialogPanel.Controls.Add(_promptLabel);
            _dialogPanel.Controls.Add(_inputTextBox);
            _dialogPanel.Controls.Add(_okButton);
            _dialogPanel.Controls.Add(_cancelButton);
            
            _backgroundPanel.Controls.Add(_dialogPanel);
            this.Controls.Add(_backgroundPanel);
        }

        public string ShowDialog(Form owner, string prompt)
        {
            _promptLabel.Text = prompt;
            _inputTextBox.Text = "";
            WasCancelled = false;
            InputValue = null;
            
            // Set size and position to match owner (EN/FR: Définir taille et position pour correspondre au propriétaire)
            if (owner != null)
            {
                this.Size = owner.Size;
                this.Location = owner.Location;
                
                // Center dialog panel (EN/FR: Centrer panneau dialogue)
                _dialogPanel.Location = new Point(
                    (this.Width - _dialogPanel.Width) / 2,
                    (this.Height - _dialogPanel.Height) / 2
                );
            }
            
            this.ShowDialog(owner);
            
            return InputValue;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Accept();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Cancel();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Accept();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Cancel();
        }

        private void Accept()
        {
            InputValue = _inputTextBox.Text.Trim();
            WasCancelled = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Cancel()
        {
            InputValue = null;
            WasCancelled = true;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _inputTextBox.Focus();
            _inputTextBox.SelectAll();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backgroundPanel?.Dispose();
                _dialogPanel?.Dispose();
                _promptLabel?.Dispose();
                _inputTextBox?.Dispose();
                _okButton?.Dispose();
                _cancelButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
