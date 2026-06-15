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
            AppendColoredTitle("⚠️ DRIVER & SERVICE INSTALLATION REQ. / INSTALLATION PILOTES ET SERVICE REQUISE\n", Color.FromArgb(220, 50, 50));
            AppendText("Before using Wiimote4Guns, you MUST install the Wiimote4Guns Drivers AND the Wiimote4Guns Service:\n");
            AppendText("Avant d'utiliser Wiimote4Guns, vous DEVEZ installer les pilotes Wiimote4Guns ET le Service Wiimote4Guns :\n\n");
            
            AppendText("If you just installed drivers, you MUST restart Windows:\n");
            AppendText("Si vous venez d'installer les pilotes, vous DEVEZ redémarrer Windows :\n\n");
            
            AppendText("• Restart your PC / Redémarrez votre PC\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // WIIMOTE COMPATIBILITY
            AppendColoredTitle("⚠️ WIIMOTE COMPATIBILITY / COMPATIBILITÉ WIIMOTE\n", Color.FromArgb(255, 140, 0));
            
            AppendBoldText("📶 Bluetooth Mode:\n");
            AppendText("• ONLY early generation Wiimotes (pre-2011)\n");
            AppendText("• Uniquement Wiimotes première génération (avant 2011)\n");
            AppendText("• Serial NOT ending in Z-C4, Z-C6, C-C4\n\n");
            
            AppendBoldText("🎮 DolphinBar Mayflash Mode 4:\n");
            AppendText("• Works with ALL Wiimotes (new/old/clones)\n");
            AppendText("• Fonctionne avec TOUTES les Wiimotes\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // CONNECTING WIIMOTES
            AppendColoredTitle("🎮 CONNECTING WIIMOTES / CONNECTER LES WIIMOTES\n", Color.FromArgb(0, 150, 100));
            
            AppendBoldText("Connection / Connexion:\n");
            AppendText("• Press Red SYNC button or 1 + 2 buttons\n");
            AppendText("• Appuyez sur le bouton rouge SYNC ou les boutons 1 + 2\n\n");

            AppendBoldText("If Bluetooth connection fails / Si la connexion Bluetooth échoue :\n");
            AppendText("1. Bluetooth Manager -> Add Device / Ajouter appareil\n");
            AppendText("2. Press 1+2 repeatedly / Appuyez plusieurs fois sur 1+2\n");
            AppendText("3. Click 'Nintendo RVL-CNT-01' (or 'Input device/Saisie')\n");
            AppendText("4. CANCEL PIN code request (Do not enter anything!)\n");
            AppendText("   ANNULEZ la demande de code PIN (Ne rien saisir !)\n");
            AppendText("5. Keep pressing 1+2 until connected\n");
            AppendText("   Continuez d'appuyer sur 1+2 jusqu'à connexion\n\n");

            AppendBoldText("Reset:\n");
            AppendText("• Long press SYNC resets DolphinBar hardware link\n");
            AppendText("• Appui long SYNC réinitialise le lien matériel DolphinBar\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // CONTROLS
            AppendColoredTitle("🎯 CONTROLS / CONTRÔLES\n", Color.FromArgb(100, 100, 150));
            AppendText("• HOME - toggle modes (Mouse/Keyboard/Disabled)\n");
            AppendText("• HOME (long press) - calibrate / calibrer\n");
            AppendText("• HOME + Plus - open overlay menu / ouvrir menu overlay\n");
            AppendText("• OFF-SCREEN + Minus + Plus (3s) - Manually Disable Virtual Device / Désactiver Périphérique Virtuel Manuellement\n");
            AppendText("• Right-click tray icon - settings / paramètres\n\n");
            
            /* 
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            
            // TROUBLESHOOTING
            AppendColoredTitle("🛠️ TROUBLESHOOTING / DÉPANNAGE\n", Color.FromArgb(200, 50, 200));
            AppendBoldText("Fix Virtual Device Bugs / Corriger Bugs Périphériques Virtuels:\n");
            AppendText("If you need to disable a Player (will only reactivate on next connection, reboot needed between disable/enable):\n");
            AppendText("Si vous devez désactiver un Joueur (réactivation à la prochaine connexion, redémarrage nécessaire entre désactivation et activation) :\n");
            AppendText("• Use the Manual Disable hotkey (Off-screen Minus+Plus 3s)\n");
            AppendText("• Utilisez le raccourci Désactivation Manuelle (Hors-écran Moins+Plus 3s)\n\n");
            
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");
            */
            
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
