using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V50] System picker (EmulationStation systems = <RetroBat>\roms\ folders),
    /// used for ADVANCED associations outside a game session.
    /// FR: [V50] Sélecteur de système (systèmes ES = dossiers de <RetroBat>\roms\),
    /// utilisé pour les associations AVANCÉES hors session de jeu.
    /// </summary>
    internal partial class EsSystemPickerForm : Form
    {
        // [V51] Full list + live prefix filter (EN/FR: Liste complète + filtre préfixe live)
        private System.Collections.Generic.List<string> _allSystems =
            new System.Collections.Generic.List<string>();
        private string _preselect;

        private EsSystemPickerForm()
        {
            InitializeComponent();
            BuildLogic();
        }

        private void BuildLogic()
        {
            btnOK.BackColor = Color.FromArgb(0, 122, 204);
            btnCancel.BackColor = Color.FromArgb(60, 60, 60);

            btnOK.Click += (s, e) =>
            {
                if (lstSystems.SelectedItem != null)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            lstSystems.DoubleClick += (s, e) =>
            {
                if (lstSystems.SelectedItem != null)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            // [V51] Live prefix search: typing "p" shows systems starting with "p",
            // "ps" shows those starting with "ps", etc. (case-insensitive).
            // (EN/FR: Recherche par préfixe live : taper "p" affiche les systèmes
            // commençant par "p", "ps" ceux commençant par "ps", etc. (casse ignorée).)
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                if (e.KeyCode == Keys.Enter && lstSystems.SelectedItem != null)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    e.Handled = true;
                }
            };
        }

        /// <summary>
        /// EN: [V51] Rebuild the visible list from the search text (prefix, case-insensitive).
        /// FR: [V51] Reconstruit la liste visible depuis le texte recherché (préfixe, casse ignorée).
        /// </summary>
        private void ApplyFilter()
        {
            string filter = txtSearch.Text.Trim();

            lstSystems.BeginUpdate();
            lstSystems.Items.Clear();
            foreach (string sys in _allSystems)
            {
                if (string.IsNullOrEmpty(filter) ||
                    sys.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    lstSystems.Items.Add(sys);
                }
            }
            lstSystems.EndUpdate();

            // Keep the preselection while it still matches the filter
            // (EN/FR: Garder la présélection tant qu'elle matche le filtre)
            if (!string.IsNullOrEmpty(_preselect))
            {
                int idx = lstSystems.Items.IndexOf(_preselect);
                if (idx >= 0) { lstSystems.SelectedIndex = idx; return; }
            }
            if (lstSystems.Items.Count > 0)
                lstSystems.SelectedIndex = 0;
        }

        /// <summary>
        /// EN: Show the system picker. Returns the selected system name, or null if cancelled.
        /// FR: Affiche le sélecteur de système. Retourne le nom sélectionné, ou null si annulé.
        /// </summary>
        public static string Show(IWin32Window owner, string title, string preselect = null)
        {
            var systems = EsScriptIntegration.GetEsSystemNames();
            if (systems.Count == 0)
            {
                MessageBox.Show(owner, "No system found in <RetroBat>\\roms\\.", "Systems", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using (var form = new EsSystemPickerForm())
            {
                if (!string.IsNullOrEmpty(title)) form.lblTitle.Text = title;

                form._preselect = preselect;
                form._allSystems = systems;
                form.ApplyFilter();
                form.ActiveControl = form.txtSearch; // [V51] Search focused on open

                return form.ShowDialog(owner) == DialogResult.OK
                    ? form.lstSystems.SelectedItem?.ToString()
                    : null;
            }
        }
    }
}
