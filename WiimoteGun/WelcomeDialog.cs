using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun
{
    public class WelcomeDialog : Form
    {
        private TextBox textBox;
        private Button btnOK;

        public WelcomeDialog()
        {
            InitializeComponent();
            PopulateContent();
            
            // Remove text selection and focus on OK button (EN/FR: Enlever la sélection et focus sur le bouton OK)
            textBox.SelectionLength = 0;
            btnOK.Focus();
        }

        private void InitializeComponent()
        {
            this.textBox = new TextBox();
            this.btnOK = new Button();
            this.SuspendLayout();
            
            // textBox
            this.textBox.BorderStyle = BorderStyle.None;
            this.textBox.Location = new Point(12, 12);
            this.textBox.Multiline = true;
            this.textBox.Name = "textBox";
            this.textBox.ReadOnly = true;
            this.textBox.ScrollBars = ScrollBars.Vertical;
            this.textBox.Size = new Size(760, 500);
            this.textBox.TabIndex = 0;
            this.textBox.TabStop = false;
            this.textBox.Font = new Font("Segoe UI", 9F);
            this.textBox.BackColor = SystemColors.Window;
            
            // btnOK
            this.btnOK.Location = new Point(350, 525);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            this.btnOK.DialogResult = DialogResult.OK;
            
            // WelcomeDialog
            this.AcceptButton = this.btnOK;
            this.ClientSize = new Size(784, 567);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.textBox);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WelcomeDialog";
            this.ShowIcon = true;
            this.ShowInTaskbar = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Wiimote2Guns - First Launch / Premier Lancement";
            this.Font = SystemFonts.MessageBoxFont;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void PopulateContent()
        {
            textBox.Text = 
                "═══════════════════════════════════════════════════════════════════════════════════════════════\r\n" +
                "                                    Welcome to Wiimote2Guns v2.0!\r\n" +
                "                                  Bienvenue dans Wiimote2Guns v2.0 !\r\n" +
                "═══════════════════════════════════════════════════════════════════════════════════════════════\r\n\r\n" +

                "╔═════════════════════════════════════════════════════════════════════════════════════════════╗\r\n" +
                "║                      IMPORTANT - DRIVER INSTALLATION REQUIRED                               ║\r\n" +
                "║                      IMPORTANT - INSTALLATION DU DRIVER REQUISE                             ║\r\n" +
                "╚═════════════════════════════════════════════════════════════════════════════════════════════╝\r\n\r\n" +

                "Before using Wiimote2Guns, you MUST install the Interception driver:\r\n" +
                "Avant d'utiliser Wiimote2Guns, vous DEVEZ installer le driver Interception :\r\n\r\n" +

                "► METHOD 1 (Recommended / Recommandée):\r\n" +
                "   1. Right-click the Wiimote2Guns tray icon / Clic droit sur l'icône dans la barre des tâches\r\n" +
                "   2. Click 'Options'\r\n" +
                "   3. Click 'Install Drivers' / Cliquez sur 'Install Drivers'\r\n" +
                "   4. Restart your PC / Redémarrez votre PC\r\n\r\n" +

                "► METHOD 2 (If button doesn't work / Si le bouton ne fonctionne pas):\r\n" +
                "   1. Navigate to: WiimoteGunDriver\\command line installer\\\r\n" +
                "   2. Right-click 'install-interception.exe' → Run as Administrator\r\n" +
                "      Clic droit sur 'install-interception.exe' → Exécuter en tant qu'administrateur\r\n" +
                "   3. Restart your PC / Redémarrez votre PC\r\n\r\n" +

                "───────────────────────────────────────────────────────────────────────────────────────────────\r\n\r\n" +

                "╔═════════════════════════════════════════════════════════════════════════════════════════════╗\r\n" +
                "║              CONNECTING MULTIPLE WIIMOTES / CONNECTER PLUSIEURS WIIMOTES                    ║\r\n" +
                "╚═════════════════════════════════════════════════════════════════════════════════════════════╝\r\n\r\n" +

                "► For 2 Players (stable / stable):\r\n" +
                "   • Press 1+2 on each Wiimote to connect them\r\n" +
                "   • They will be auto-assigned as Player 1 and Player 2\r\n\r\n" +
                "   • Appuyez sur 1+2 sur chaque Wiimote pour les connecter\r\n" +
                "   • Elles seront automatiquement assignées en Joueur 1 et Joueur 2\r\n\r\n" +

                "► For 3-4 Players (experimental / expérimental):\r\n" +
                "   • Enable '4 Players Mode' in Options / Activez 'Mode 4 Joueurs' dans les Options\r\n" +
                "   • You need 4 physical keyboards + 4 physical mice\r\n" +
                "   • Each player gets a unique virtual keyboard/mouse pair\r\n\r\n" +
                "   • Vous avez besoin de 4 claviers physiques + 4 souris physiques\r\n" +
                "   • Chaque joueur obtient une paire clavier/souris virtuelle unique\r\n\r\n" +

                "───────────────────────────────────────────────────────────────────────────────────────────────\r\n\r\n" +

                "╔═════════════════════════════════════════════════════════════════════════════════════════════╗\r\n" +
                "║                          CONTROLS / CONTRÔLES                                               ║\r\n" +
                "╚═════════════════════════════════════════════════════════════════════════════════════════════╝\r\n\r\n" +

                "   • Press HOME to toggle modes (Mouse/Keyboard/Disabled)\r\n" +
                "   • Long press HOME to calibrate / Appui long sur HOME pour calibrer\r\n" +
                "   • Right-click tray icon for settings / Clic droit sur l'icône pour les paramètres\r\n\r\n" +

                "═══════════════════════════════════════════════════════════════════════════════════════════════\r\n" +
                "                                 Enjoy / Amusez-vous bien !\r\n" +
                "═══════════════════════════════════════════════════════════════════════════════════════════════";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
