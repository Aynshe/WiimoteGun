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
            WiimoteGun.UI.UiScaler.ApplyForm(this); // [V57n] UI zoom (bounds, fonts, screen clamp)
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
            AppendColoredTitle("⚠️ SERVICE INSTALLATION REQUIRED / INSTALLATION SERVICE REQUISE\n", Color.FromArgb(220, 50, 50));
            AppendText("Before using Wiimote4Guns, you MUST install the Wiimote4Guns Service (Setup Wizard):\n");
            AppendText("Avant d'utiliser Wiimote4Guns, vous DEVEZ installer le Service Wiimote4Guns (Setup Wizard) :\n\n");

            AppendBoldText("New driver: RawInput (UMDF2) / Nouveau pilote : RawInput (UMDF2)\n");
            AppendText("• Installed silently by the service (no test mode, no UAC, no reboot)\n");
            AppendText("• Installé silencieusement par le service (sans test mode, sans UAC, sans redémarrage)\n");
            AppendText("• Requires the free .NET Runtime 10 x64 (the app checks and proposes the download)\n");
            AppendText("• Nécessite le Runtime .NET 10 x64 gratuit (l'app vérifie et propose le téléchargement)\n");
            AppendText("• The legacy vmulti driver is OPTIONAL (Windows 10 / Win11 below 26H02 only)\n");
            AppendText("• Le pilote vmulti (legacy) est OPTIONNEL (Windows 10 / Win11 antérieur à 26H02)\n\n");

            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // WIIMOTE COMPATIBILITY
            AppendColoredTitle("📶 WIIMOTE COMPATIBILITY / COMPATIBILITÉ WIIMOTE\n", Color.FromArgb(255, 140, 0));

            AppendBoldText("Bluetooth Mode:\n");
            AppendText("• Wiimote v1 (pre-2011): press 1 + 2 buttons\n");
            AppendText("• Wiimote v1 (avant 2011) : appuyez sur les boutons 1 + 2\n");
            AppendText("• Wiimote v2 (2011+): use the RED SYNC button (under the battery cover)\n");
            AppendText("• Wiimote v2 (2011+) : utilisez le bouton SYNC ROUGE (sous le capot batterie)\n");
            AppendText("  Nunchuk NOT hot-pluggable: plug it BEFORE pairing / Nunchuk NON\n");
            AppendText("  branchable à chaud : branchez-le AVANT l'appairage\n\n");

            AppendBoldText("DolphinBar Mayflash Mode 4:\n");
            AppendText("• Works with ALL Wiimotes (new/old/clones)\n");
            AppendText("• Fonctionne avec TOUTES les Wiimotes\n\n");

            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // CONNECTING WIIMOTES
            AppendColoredTitle("🎮 CONNECTING WIIMOTES / CONNECTER LES WIIMOTES\n", Color.FromArgb(0, 150, 100));

            AppendText("• Press 1+2 (v1) or Red SYNC (v2) on each Wiimote\n");
            AppendText("• Appuyez sur 1+2 (v1) ou SYNC rouge (v2) sur chaque Wiimote\n");
            AppendText("• Auto-assigned to the next available player slot (P1-P4)\n");
            AppendText("• Assignation automatique au prochain slot joueur libre (P1-P4)\n\n");

            AppendBoldText("If Bluetooth fails / Si Bluetooth échoue :\n");
            AppendText("1. Bluetooth Manager -> Add Device / Ajouter appareil\n");
            AppendText("2. Press 1+2 repeatedly (v1) or SYNC (v2)\n");
            AppendText("2. Appuyez plusieurs fois sur 1+2 (v1) ou SYNC (v2)\n");
            AppendText("3. Click 'Nintendo RVL-CNT-01' (or 'Input device/Saisie')\n");
            AppendText("4. CANCEL PIN code request (Do not enter anything!)\n");
            AppendText("   ANNULEZ la demande de code PIN (Ne rien saisir !)\n\n");

            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // CONTROLS
            AppendColoredTitle("🎯 CONTROLS / CONTRÔLES\n", Color.FromArgb(100, 100, 150));
            AppendText("• HOME - cycle modes: Mouse → Mouse 4:3 → GamePad → Disabled\n");
            AppendText("• HOME - modes : Souris → Souris 4:3 → GamePad → Désactivé\n");
            AppendText("• HOME (long press) - calibrate / calibrer\n");
            AppendText("• HOME + PLUS - open Wiimote4Guns interface / ouvrir l'interface Wiimote4Guns\n");
            AppendText("• PLUS (long press ~4s) - tile modal: load profiles on the fly,\n");
            AppendText("  swap GamePad DInput/XInput, check batteries\n");
            AppendText("• PLUS (appui long ~4s) - modale tuiles : profils à la volée,\n");
            AppendText("  bascule GamePad DInput/XInput, état batteries\n");
            AppendText("• OFF-SCREEN + Minus + Plus (3s) - disable virtual device\n");
            AppendText("• Hors-écran + Moins + Plus (3s) - désactiver périphérique virtuel\n");
            AppendText("• Right-click tray icon - settings / clic droit icône systray - paramètres\n\n");

            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // REMAP PROFILES
            AppendColoredTitle("📁 REMAP PROFILES / PROFILS REMAP\n", Color.FromArgb(200, 50, 200));
            AppendBoldText("RetroBat game detection / Détection des jeux RetroBat :\n");
            AppendText("• Games launched from RetroBat are detected automatically\n");
            AppendText("• Les jeux lancés depuis RetroBat sont détectés automatiquement\n");
            AppendText("• Associate a button remap profile per game (Wiimote/Nunchuk)\n");
            AppendText("• Associez un profil de remap de boutons par jeu (Wiimote/Nunchuk)\n");
            AppendText("• The profile loads automatically when the game starts\n");
            AppendText("• Le profil se charge automatiquement au lancement du jeu\n");
            AppendText("• Switch profiles on the fly: long-press PLUS → tile modal\n");
            AppendText("• Changez de profil à la volée : appui long PLUS → modale tuiles\n\n");

            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // GAMEPAD MODE
            AppendColoredTitle("🎮 GAMEPAD MODE / MODE MANETTE\n", Color.FromArgb(0, 120, 215));
            AppendText("• Each Wiimote becomes a virtual gamepad: DirectInput or XInput\n");
            AppendText("• Chaque Wiimote devient une manette virtuelle : DirectInput ou XInput\n");
            AppendText("• IR aiming on the right stick, Nunchuk joystick on the left stick\n");
            AppendText("• Visée IR sur le stick droit, joystick nunchuk sur le stick gauche\n");
            AppendText("• DuckStation, PCSX2 and Dolphin profiles updated automatically\n");
            AppendText("• Profils DuckStation, PCSX2 et Dolphin mis à jour automatiquement\n");
            AppendText("• EmulationStation detects the gamepads automatically\n");
            AppendText("• EmulationStation détecte les manettes automatiquement\n\n");

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
