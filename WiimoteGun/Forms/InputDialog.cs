using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.Forms
{
    public partial class InputDialog : Form
    {
        private TextBox textBox;
        public string InputText => textBox.Text;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            this.Text = title;
            this.Controls.OfType<Label>().First().Text = prompt;
            this.textBox = this.Controls.OfType<TextBox>().First();
        }
    }
}
