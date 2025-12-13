using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun
{
    public partial class WelcomeDialog : Form
    {
        public WelcomeDialog()
        {
            InitializeComponent();
            PopulateContent();
            
            // Clean up FlatAppearance border size (moved from InitializeComponent)
            btnOK.FlatAppearance.BorderSize = 0;
            
            // Remove text selection and focus on OK button
            richTextBox.SelectionLength = 0;
            btnOK.Focus();

            // Paint title
            this.Paint += (s, e) =>
            {
                using (var font = new Font("Segoe UI", 18F, FontStyle.Bold))
                {
                    var titleSize = e.Graphics.MeasureString("Wiimote4Guns", font);
                    e.Graphics.DrawString("Wiimote4Guns", font, Brushes.White, 
                        (this.ClientSize.Width - titleSize.Width) / 2, 15);
                }
            };
        }

        private void PopulateContent()
        {
            richTextBox.Clear();
            
            // DRIVER INSTALLATION
            AppendColoredTitle("⚠️ DRIVER INSTALLATION REQUIRED / INSTALLATION DU DRIVER REQUISE\n", Color.FromArgb(220, 50, 50));
            AppendText("Before using Wiimote4Guns, you MUST install the Interception driver:\n");
            AppendText("Avant d'utiliser Wiimote4Guns, vous DEVEZ installer le driver Interception :\n\n");
            
            AppendText("1. Right-click tray icon → Options → Install Drivers\n");
            AppendText("   Clic droit icône → Options → Install Drivers\n\n");
            
            AppendText("2. Choose player count / Choisissez le nombre de joueurs:\n");
            AppendText("   • 'Add 1 Player' - single player / un joueur\n");
            AppendText("   • 'Add 2 Player' - two players / deux joueurs\n\n");
            
            AppendText("3. Restart your PC / Redémarrez votre PC\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // WIIMOTE COMPATIBILITY
            AppendColoredTitle("⚠️ WIIMOTE COMPATIBILITY / COMPATIBILITÉ WIIMOTE\n", Color.FromArgb(255, 140, 0));
            
            AppendBoldText("📶 Bluetooth Mode:\n");
            AppendText("• ONLY early generation Wiimotes (pre-2011)\n");
            AppendText("• Uniquement Wiimotes première génération (avant 2011)\n");
            AppendText("• Serial NOT ending in Z-C4, Z-C6, C-C4\n\n");
            
            AppendBoldText("🎮 DolphinBar Mayflash Mode 4 (RECOMMENDED):\n");
            AppendText("• Works with ALL Wiimotes (new/old/clones)\n");
            AppendText("• Fonctionne avec TOUTES les Wiimotes\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // CONNECTING WIIMOTES
            AppendColoredTitle("🎮 CONNECTING WIIMOTES / CONNECTER LES WIIMOTES\n", Color.FromArgb(0, 150, 100));
            
            AppendBoldText("Driver Installation / Installation des Pilotes:\n");
            AppendText("• Install Virtual Driver according to the number of players desired\n");
            AppendText("• Installez le pilote virtuel selon le nombre de joueurs souhaité\n\n");

            AppendBoldText("No Driver / Sans Pilote:\n");
            AppendText("• Physical Mouse/Keyboard will be used for one player\n");
            AppendText("• Souris/Clavier physique utilisé pour un joueur\n\n");
            
            AppendBoldText("Multi-Player Setup / Configuration Multijoueur:\n");
            AppendText("• Enable '4 Players Mode' in Options for >2 players\n");
            AppendText("• Activez 'Mode 4 Joueurs' dans les Options pour >2 joueurs\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // CONTROLS
            AppendColoredTitle("🎯 CONTROLS / CONTRÔLES\n", Color.FromArgb(100, 100, 150));
            AppendText("• HOME - toggle modes (Mouse/Keyboard/Disabled)\n");
            AppendText("• HOME (long press) - calibrate / calibrer\n");
            AppendText("• HOME + Plus - open overlay menu / ouvrir menu overlay\n");
            AppendText("• Right-click tray icon - settings / paramètres\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // Footer
            AppendColoredTitle("Enjoy! / Amusez-vous bien ! 🎮", Color.FromArgb(0, 120, 215));
        }

        private void AppendColoredTitle(string text, Color color)
        {
            int start = richTextBox.TextLength;
            richTextBox.AppendText(text);
            richTextBox.Select(start, text.Length);
            richTextBox.SelectionColor = color;
            richTextBox.SelectionFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            richTextBox.SelectionLength = 0;
        }

        private void AppendBoldText(string text)
        {
            int start = richTextBox.TextLength;
            richTextBox.AppendText(text);
            richTextBox.Select(start, text.Length);
            richTextBox.SelectionFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            richTextBox.SelectionLength = 0;
        }

        private void AppendText(string text)
        {
            richTextBox.AppendText(text);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
