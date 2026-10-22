using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V62-Output] Modal dialog to configure the per-player output-driven rumble
    /// (enable + fire/damage force). Modal so it never takes space in the mapping table.
    /// FR: [V62-Output] Modale de réglage des vibrations pilotées par outputs pour un
    /// joueur (activation + force tir/dégâts). Modale afin de ne jamais prendre de place
    /// dans le tableau de mapping.
    /// </summary>
    public partial class OutputRumbleProfileForm : Form
    {
        private int _player = 1;
        private readonly ToolTip ttDur = new ToolTip(); // [V62-OUTPUT-DUR] length tooltips

        public OutputRumbleProfileForm()
        {
            InitializeComponent();

            // EN/FR: show the player in the title + live values on the trackbars.
            lblTitle.Text = "Output rumble - Player " + _player;

            trkFire.ValueChanged += (s, e) => lblFireValue.Text = trkFire.Value + "%";
            trkDamage.ValueChanged += (s, e) => lblDamageValue.Text = trkDamage.Value + "%";
            chkEnabled.CheckedChanged += (s, e) =>
            {
                trkFire.Enabled = chkEnabled.Checked;
                trkDamage.Enabled = chkEnabled.Checked;
                numFireDur.Enabled = chkEnabled.Checked;
                numDamageDur.Enabled = chkEnabled.Checked;
            };

            // [V62-OUTPUT-DUR] EN/FR: bilingual tooltips of the two length controls (ms).
            ttDur.SetToolTip(numFireDur,
                "LENGTH OF THE FIRE RUMBLE (ms):\r\n" +
                "Raise it when the weapon feels weak (semi-automatic / single-shot weapons often\r\n" +
                "rumble too briefly to be felt). The force % is then applied on this length.\r\n" +
                "\r\n" +
                "DUREE DE LA VIBRATION DE TIR (ms) :\r\n" +
                "L'augmenter quand l'arme parait faible (armes semi-automatiques / coup par coup\r\n" +
                "vibrent souvent trop brièvement pour être ressenties). La force % s'applique sur\r\n" +
                "cette durée.");
            ttDur.SetToolTip(numDamageDur,
                "LENGTH OF THE DAMAGE RUMBLE (ms):\r\n" +
                "How long an impact is felt (kept long by default so a hit is clearly perceptible).\r\n" +
                "\r\n" +
                "DUREE DE LA VIBRATION DEGATS (ms) :\r\n" +
                "Durée pendant laquelle un impact est ressenti (longue par défaut pour qu'il soit\r\n" +
                "nettement perceptible).");
        }

        /// <summary>EN: fill the dialog. FR: pré-remplir la modale.</summary>
        public void LoadState(int player, ref bool enabled, ref int fire, ref int damage, ref int fireDur, ref int damageDur)
        {
            _player = player <= 0 ? 1 : player;
            lblTitle.Text = "Output rumble - Player " + _player;

            chkEnabled.Checked = enabled;
            trkFire.Value = Math.Min(trkFire.Maximum, Math.Max(trkFire.Minimum, fire));
            trkDamage.Value = Math.Min(trkDamage.Maximum, Math.Max(trkDamage.Minimum, damage));
            lblFireValue.Text = trkFire.Value + "%";
            lblDamageValue.Text = trkDamage.Value + "%";
            numFireDur.Value = Math.Min(numFireDur.Maximum, Math.Max(numFireDur.Minimum, fireDur));
            numDamageDur.Value = Math.Min(numDamageDur.Maximum, Math.Max(numDamageDur.Minimum, damageDur));
            numFireDur.Enabled = chkEnabled.Checked;
            numDamageDur.Enabled = chkEnabled.Checked;
        }

        /// <summary>EN: read the dialog back. FR: relire la modale.</summary>
        public void StoreState(out bool enabled, out int fire, out int damage, out int fireDur, out int damageDur)
        {
            enabled = chkEnabled.Checked;
            fire = trkFire.Value;
            damage = trkDamage.Value;
            fireDur = (int)numFireDur.Value;
            damageDur = (int)numDamageDur.Value;
        }
    }
}
