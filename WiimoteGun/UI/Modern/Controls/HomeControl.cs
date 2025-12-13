using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.Controls
{
    public partial class HomeControl : UserControl
    {
        public event EventHandler OptionsClicked;
        public event EventHandler MappingClicked;
        public event EventHandler AssignClicked;
        public event EventHandler IRVizClicked;

        public HomeControl()
        {
            InitializeComponent();
            
            // Set FlatAppearance properties (Designer doesn't support BorderSize = 0)
            btnNavOptions.FlatAppearance.BorderSize = 0;
            btnNavMapping.FlatAppearance.BorderSize = 0;
            btnNavAssign.FlatAppearance.BorderSize = 0;
            btnNavIRViz.FlatAppearance.BorderSize = 0;
        }

        // Button click event handlers (EN/FR: Gestionnaires de clic de boutons)
        private void BtnNavOptions_Click(object sender, EventArgs e)
        {
            OptionsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNavMapping_Click(object sender, EventArgs e)
        {
            MappingClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNavAssign_Click(object sender, EventArgs e)
        {
            AssignClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNavIRViz_Click(object sender, EventArgs e)
        {
            IRVizClicked?.Invoke(this, EventArgs.Empty);
        }

        // Mouse hover effects (EN/FR: Effets de survol souris)
        private void Btn_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(28, 151, 234); // Lighter blue on hover
            }
        }

        private void Btn_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(0, 122, 204); // Original blue
            }
        }
    }
}
