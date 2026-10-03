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
                lblVersion.Text = "v3.5.6.1";
            }

            // [V56e] Poll the update check result and refresh the indicator
            // (EN/FR: Sondage du résultat de la vérification de mise à jour et rafraîchissement du voyant)
            _updateStateTimer = new Timer { Interval = 2000 };
            _updateStateTimer.Tick += (s, e) => RefreshUpdateIndicator();
            _updateStateTimer.Start();
            RefreshUpdateIndicator();
        }

        // [V56e] Update indicator state polling (EN/FR: Sondage d'état du voyant de mise à jour)
        private Timer _updateStateTimer;

        /// <summary>
        /// EN: [V56e/V56f] Refresh the update indicator: RED dot + "Update available: vX"
        /// when a newer release exists, GREEN dot + "Up to date" otherwise, ORANGE dot +
        /// "Offline" when the check could not reach GitHub (no internet). Gray "Checking..."
        /// while the check is still running. Stops polling once a definitive state is reached.
        /// FR: [V56e/V56f] Rafraîchit le voyant : point ROUGE + « Update available: vX » si
        /// une release plus récente existe, point VERT + « Up to date » sinon, point
        /// ORANGE + « Offline » si la vérification n'a pas pu joindre GitHub (pas
        /// d'internet). Gris « Checking... » pendant la vérification. Arrête le sondage
        /// dès qu'un état définitif est atteint.
        /// </summary>
        private void RefreshUpdateIndicator()
        {
            if (!WiimoteGun.Core.AppUpdateChecker.HasChecked) return; // Still checking (gray)

            if (WiimoteGun.Core.AppUpdateChecker.Offline)
            {
                // [V56f] Offline: orange dot, distinct from a real "up to date"
                // (EN/FR: Hors ligne : point orange, distinct d'un vrai « up to date »)
                lblUpdateDot.BackColor = Color.FromArgb(230, 126, 34);   // ORANGE
                lblUpdateStatusText.ForeColor = Color.FromArgb(255, 190, 120);
                lblUpdateStatusText.Text = "Offline";
                tsmiUpdateNow.Enabled = false;
            }
            else if (WiimoteGun.Core.AppUpdateChecker.UpdateAvailable)
            {
                lblUpdateDot.BackColor = Color.FromArgb(231, 76, 60);   // RED
                lblUpdateStatusText.ForeColor = Color.FromArgb(255, 120, 110);
                lblUpdateStatusText.Text = string.Format("Update available: v{0}", WiimoteGun.Core.AppUpdateChecker.LatestVersion);
                tsmiUpdateNow.Enabled = true;
            }
            else
            {
                lblUpdateDot.BackColor = Color.FromArgb(46, 204, 113);   // GREEN
                lblUpdateStatusText.ForeColor = Color.FromArgb(180, 230, 180);
                lblUpdateStatusText.Text = "Up to date";
                tsmiUpdateNow.Enabled = false;
            }

            _updateStateTimer.Stop();
        }

        /// <summary>
        /// EN: [V56e] The indicator is clickable: opens the update menu (release page /
        /// update now) at the click position.
        /// FR: [V56e] Le voyant est cliquable : ouvre le menu de mise à jour (page des
        /// releases / mise à jour directe) à la position du clic.
        /// </summary>
        private void UpdateStatus_Click(object sender, EventArgs e)
        {
            cmsUpdate.Show(pnlUpdateStatus, new Point(0, pnlUpdateStatus.Height + 2));
        }

        private void TsmiOpenReleasePage_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(WiimoteGun.Core.AppUpdateChecker.ReleaseUrl);
            }
            catch { }
        }

        private void TsmiUpdateNow_Click(object sender, EventArgs e)
        {
            if (!WiimoteGun.Core.AppUpdateChecker.UpdateAvailable) return;

            string latest = WiimoteGun.Core.AppUpdateChecker.LatestVersion;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            string msg = string.Format(
                "Update Wiimote4Guns now?\r\n\r\n" +
                "Current: v{0}\r\n" +
                "New: v{1}\r\n\r\n" +
                "The app will close, download and install the update,\r\n" +
                "then restart automatically (current user account).\r\n" +
                "Service files will be staged for the usual admin update.\r\n\r\n" +
                "Mettre à jour Wiimote4Guns maintenant ?\r\n" +
                "L'app va se fermer, télécharger et installer la mise à jour,\r\n" +
                "puis redémarrer automatiquement (compte utilisateur en cours).",
                version, latest);

            if (MessageBox.Show(this, msg, "Wiimote4Guns Update",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            string error;
            if (WiimoteGun.Services.AppUpdateLauncher.Launch(WiimoteGun.Core.AppUpdateChecker.DownloadUrl, out error))
            {
                Program.ExitApplicationForUpdate(); // Unregister from the service, then exit
            }
            else
            {
                MessageBox.Show(this, "Could not start the update:\r\n" + error, "Wiimote4Guns Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
