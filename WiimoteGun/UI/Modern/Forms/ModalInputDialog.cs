using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Modal input dialog overlay for text input
    /// (EN/FR: Dialogue modal overlay pour saisie de texte)
    /// </summary>
    public partial class ModalInputDialog : Form
    {
        public string InputValue { get; private set; }
        public bool WasCancelled { get; private set; }
        
        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorPanel = Color.FromArgb(37, 37, 37);
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);
        private static readonly Color ColorBorder = Color.FromArgb(63, 63, 63);

        public ModalInputDialog()
        {
            InitializeComponent();
            
            // Set FlatAppearance border sizes (Designer doesn't support this)
            _okButton.FlatAppearance.BorderSize = 0;
            _cancelButton.FlatAppearance.BorderSize = 0;
            
            // Enable Virtual Keyboard on click (EN/FR: Activer Clavier Virtuel au clic)
            _inputTextBox.Click += (s, e) => ShowVirtualKeyboard();
        }

        public ModalInputDialog(string title, string prompt) : this()
        {
            this.Text = title;
            _promptLabel.Text = prompt;
        }

        public ModalInputDialog(string title, string prompt, string defaultValue) : this(title, prompt)
        {
            _inputTextBox.Text = defaultValue;
        }
        


        public string ShowDialog(Form owner, string prompt)
        {
            _promptLabel.Text = prompt;
            _inputTextBox.Text = "";
            WasCancelled = false;
            InputValue = null;
            
            // Custom size sizing removed as using standard dialog sizing
            // (EN/FR: Taille personnalisée supprimée car utilisation du dimensionnement de dialogue standard)
            
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


        private void ShowVirtualKeyboard()
        {
             VirtualKeyboard keyboard = new VirtualKeyboard(_inputTextBox);
             keyboard.StartPosition = FormStartPosition.Manual;
             
             // Position below the dialog (EN/FR: Positionner sous le dialogue)
             keyboard.Location = new Point(
                this.Location.X + (this.Width - keyboard.Width) / 2, 
                this.Location.Y + this.Height + 5
             );
             
             // Check screen bounds (EN/FR: Vérifier limites écran)
             var screen = Screen.FromControl(this);
             if (keyboard.Bottom > screen.WorkingArea.Bottom)
             {
                 // Place above if no space below (EN/FR: Placer au-dessus si manque place)
                 keyboard.Top = this.Top - keyboard.Height - 5;
             }

             keyboard.ShowDialog(this);
        }
    }
}
