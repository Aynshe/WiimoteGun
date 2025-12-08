using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun
{
    public class WelcomeDialog : Form
    {
        private RichTextBox richTextBox;
        private Button btnOK;

        public WelcomeDialog()
        {
            InitializeComponent();
            PopulateContent();
            
            // Remove text selection and focus on OK button
            richTextBox.SelectionLength = 0;
            btnOK.Focus();
        }

        private void InitializeComponent()
        {
            this.richTextBox = new RichTextBox();
            this.btnOK = new Button();
            this.SuspendLayout();
            
            // richTextBox
            this.richTextBox.BorderStyle = BorderStyle.None;
            this.richTextBox.Location = new Point(12, 60);
            this.richTextBox.Multiline = true;
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.richTextBox.Size = new Size(776, 530);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.TabStop = false;
            this.richTextBox.Font = new Font("Segoe UI", 9.5F);
            this.richTextBox.BackColor = SystemColors.Window;
            
            // btnOK
            this.btnOK.Location = new Point(355, 600);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(90, 35);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.BackColor = Color.FromArgb(0, 120, 215);
            this.btnOK.ForeColor = Color.White;
            this.btnOK.FlatStyle = FlatStyle.Flat;
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.Cursor = Cursors.Hand;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            this.btnOK.DialogResult = DialogResult.OK;
            
            // WelcomeDialog
            this.AcceptButton = this.btnOK;
            this.ClientSize = new Size(800, 650);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.richTextBox);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WelcomeDialog";
            this.ShowIcon = true;
            this.ShowInTaskbar = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Wiimote4Guns - First Launch";
            this.Font = SystemFonts.MessageBoxFont;
            this.BackColor = Color.FromArgb(0, 120, 215);
            this.ResumeLayout(false);
            this.PerformLayout();
            
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
