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
        }

        public ModalInputDialog(string title, string prompt) : this()
        {
            this.Text = title;
            _promptLabel.Text = prompt;
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
    }
}
