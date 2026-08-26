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
            btnOpenSetupWizard.FlatAppearance.BorderSize = 0;

            // Set version string dynamically (EN/FR: Définir la version dynamiquement)
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                lblVersion.Text = string.Format("v{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
            catch
            {
                lblVersion.Text = "v2.3.5.3";
            }
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
        private void BtnOpenSetupWizard_Click(object sender, EventArgs e)
        {
            // EN/FR: Open Setup Wizard when button is clicked (Ouvrir l'assistant de configuration lors du clic)
            using (var wizard = new WiimoteGun.Forms.SetupWizard())
            {
                wizard.ShowDialog();
            }
        }

        private void BtnSetup_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(80, 80, 80); // Brighter gray on hover
            }
        }

        private void BtnSetup_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(60, 60, 60); // Original gray
            }
        }
    }
}
