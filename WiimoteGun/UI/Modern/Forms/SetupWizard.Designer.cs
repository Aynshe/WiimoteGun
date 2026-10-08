namespace WiimoteGun.Forms
{
    partial class SetupWizard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblServiceTitle = new System.Windows.Forms.Label();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.btnInstallService = new System.Windows.Forms.Button();
            this.btnUninstallService = new System.Windows.Forms.Button();
            this.lblVMultiTitle = new System.Windows.Forms.Label();
            this.lblVMultiStatus = new System.Windows.Forms.Label();
            this.chkUnlockVMulti = new System.Windows.Forms.CheckBox();
            this.btnInstallVMulti = new System.Windows.Forms.Button();
            this.btnUninstallVMulti = new System.Windows.Forms.Button();
            this.lblVMultiInfo = new System.Windows.Forms.Label();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnSkip = new System.Windows.Forms.Button();
            this.chkDontShowAgain = new System.Windows.Forms.CheckBox();
            this.btnReCheck = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(180, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "First Run Setup";
            // 
            // lblDescription
            // 
            this.lblDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDescription.ForeColor = System.Drawing.Color.LightGray;
            this.lblDescription.Location = new System.Drawing.Point(20, 65);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(610, 52);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "The Wiimote4Guns Service is REQUIRED. The recommended input mode is now 'RawInput (UMDF2)' - installed silently by the service, no test mode, no UAC. The legacy VMulti kernel driver is now OPTIONAL.\r\n\r\nLe service Wiimote4Guns est REQUIS. Le mode d'entrée recommandé est désormais « RawInput (UMDF2) » - installé silencieusement par le service, sans test mode, sans UAC. Le pilote vmulti (legacy) est désormais OPTIONNEL.";
            // 
            // 
            // lblServiceTitle
            // 
            this.lblServiceTitle.AutoSize = true;
            this.lblServiceTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblServiceTitle.Location = new System.Drawing.Point(20, 130);
            this.lblServiceTitle.Name = "lblServiceTitle";
            this.lblServiceTitle.Size = new System.Drawing.Size(195, 20);
            this.lblServiceTitle.TabIndex = 2;
            this.lblServiceTitle.Text = "1. Wiimote4Guns Service (REQUIRED):";
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblServiceStatus.Location = new System.Drawing.Point(330, 130);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(85, 20);
            this.lblServiceStatus.TabIndex = 3;
            this.lblServiceStatus.Text = "Checking...";
            // 
            // btnInstallService
            // 
            this.btnInstallService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnInstallService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInstallService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstallService.Location = new System.Drawing.Point(440, 125);
            this.btnInstallService.Name = "btnInstallService";
            this.btnInstallService.Size = new System.Drawing.Size(100, 30);
            this.btnInstallService.TabIndex = 4;
            this.btnInstallService.Text = "Install Service";
            this.btnInstallService.UseVisualStyleBackColor = false;
            this.btnInstallService.Click += new System.EventHandler(this.btnInstallService_Click);
            // 
            // btnUninstallService
            // 
            this.btnUninstallService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnUninstallService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUninstallService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstallService.Location = new System.Drawing.Point(550, 125);
            this.btnUninstallService.Name = "btnUninstallService";
            this.btnUninstallService.Size = new System.Drawing.Size(80, 30);
            this.btnUninstallService.TabIndex = 5;
            this.btnUninstallService.Text = "Uninstall";
            this.btnUninstallService.UseVisualStyleBackColor = false;
            this.btnUninstallService.Click += new System.EventHandler(this.btnUninstallService_Click);
            // 
            // 
            // lblVMultiTitle
            // 
            this.lblVMultiTitle.AutoSize = true;
            this.lblVMultiTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblVMultiTitle.Location = new System.Drawing.Point(20, 190);
            this.lblVMultiTitle.Name = "lblVMultiTitle";
            this.lblVMultiTitle.Size = new System.Drawing.Size(250, 20);
            this.lblVMultiTitle.TabIndex = 6;
            this.lblVMultiTitle.Text = "2. Legacy VMulti Driver (OPTIONAL):";
            // 
            // lblVMultiStatus
            // 
            this.lblVMultiStatus.AutoSize = true;
            this.lblVMultiStatus.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblVMultiStatus.Location = new System.Drawing.Point(330, 190);
            this.lblVMultiStatus.Name = "lblVMultiStatus";
            this.lblVMultiStatus.Size = new System.Drawing.Size(85, 20);
            this.lblVMultiStatus.TabIndex = 7;
            this.lblVMultiStatus.Text = "Checking...";
            // 
            // chkUnlockVMulti
            // 
            // [V57k] EN/FR: Gating checkbox in front of the install button - the legacy
            //     driver install stays locked until the user explicitly opts in.
            //     (FR: Case de déblocage devant le bouton d'installation - l'installation
            //     du pilote legacy reste verrouillée tant que l'utilisateur n'adhère pas
            //     explicitement.)
            this.chkUnlockVMulti.AutoSize = true;
            this.chkUnlockVMulti.ForeColor = System.Drawing.Color.LightGray;
            this.chkUnlockVMulti.Location = new System.Drawing.Point(20, 225);
            this.chkUnlockVMulti.Name = "chkUnlockVMulti";
            this.chkUnlockVMulti.Size = new System.Drawing.Size(410, 22);
            this.chkUnlockVMulti.TabIndex = 14;
            this.chkUnlockVMulti.Text = "I understand - unlock the VMulti install button";
            this.chkUnlockVMulti.UseVisualStyleBackColor = true;
            this.chkUnlockVMulti.CheckedChanged += new System.EventHandler(this.chkUnlockVMulti_CheckedChanged);
            // 
            // btnInstallVMulti
            // 
            this.btnInstallVMulti.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnInstallVMulti.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInstallVMulti.Enabled = false;
            this.btnInstallVMulti.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstallVMulti.Location = new System.Drawing.Point(440, 220);
            this.btnInstallVMulti.Name = "btnInstallVMulti";
            this.btnInstallVMulti.Size = new System.Drawing.Size(100, 30);
            this.btnInstallVMulti.TabIndex = 8;
            this.btnInstallVMulti.Text = "Install Driver";
            this.btnInstallVMulti.UseVisualStyleBackColor = false;
            this.btnInstallVMulti.Click += new System.EventHandler(this.btnInstallVMulti_Click);
            // 
            // btnUninstallVMulti
            // 
            this.btnUninstallVMulti.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnUninstallVMulti.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUninstallVMulti.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstallVMulti.Location = new System.Drawing.Point(550, 220);
            this.btnUninstallVMulti.Name = "btnUninstallVMulti";
            this.btnUninstallVMulti.Size = new System.Drawing.Size(80, 30);
            this.btnUninstallVMulti.TabIndex = 9;
            this.btnUninstallVMulti.Text = "Uninstall All";
            this.btnUninstallVMulti.UseVisualStyleBackColor = false;
            this.btnUninstallVMulti.Click += new System.EventHandler(this.btnUninstallVMulti_Click);
            // 
            // lblVMultiInfo
            // 
            // [V57k] EN/FR: First-run information about the new driver + the VMulti future.
            //     (FR: Information premier lancement sur le nouveau pilote + l'avenir de VMulti.)
            this.lblVMultiInfo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblVMultiInfo.ForeColor = System.Drawing.Color.LightGray;
            this.lblVMultiInfo.Location = new System.Drawing.Point(20, 255);
            this.lblVMultiInfo.Name = "lblVMultiInfo";
            this.lblVMultiInfo.Size = new System.Drawing.Size(610, 58);
            this.lblVMultiInfo.TabIndex = 15;
            this.lblVMultiInfo.Text = "NEW: 'RawInput (UMDF2)' works the same way and should NOT be blocked by Microsoft starting with Windows 11 26H02 (legacy certificate deprecation). Currently IN TESTING PHASE. VMulti will eventually be removed, or only available on Windows 10 and Windows 11 below 26H02.\r\nNOUVEAU : « RawInput (UMDF2) » fonctionne pareil et ne devrait PAS être bloqué par Microsoft à compter de Windows 11 26H02 (dépréciation des anciens certificats). Actuellement EN PHASE DE TEST. VMulti sera à terme supprimé, ou disponible uniquement pour Windows 10 et Windows 11 antérieurs à 26H02.";
            // 
            // 
            // btnContinue
            // 
            this.btnContinue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this.btnContinue.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnContinue.Enabled = false;
            this.btnContinue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnContinue.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnContinue.Location = new System.Drawing.Point(225, 325);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(200, 40);
            this.btnContinue.TabIndex = 10;
            this.btnContinue.Text = "Start Wiimote4Guns";
            this.btnContinue.UseVisualStyleBackColor = false;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // btnSkip
            // 
            this.btnSkip.BackColor = System.Drawing.Color.Gray;
            this.btnSkip.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSkip.Location = new System.Drawing.Point(225, 375);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(200, 30);
            this.btnSkip.TabIndex = 11;
            this.btnSkip.Text = "Skip Setup (Run Anyway)";
            this.btnSkip.UseVisualStyleBackColor = false;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            // 
            // chkDontShowAgain
            // 
            this.chkDontShowAgain.AutoSize = true;
            this.chkDontShowAgain.ForeColor = System.Drawing.Color.LightGray;
            this.chkDontShowAgain.Location = new System.Drawing.Point(215, 415);
            this.chkDontShowAgain.Name = "chkDontShowAgain";
            this.chkDontShowAgain.Size = new System.Drawing.Size(212, 19);
            this.chkDontShowAgain.TabIndex = 12;
            this.chkDontShowAgain.Text = "Don't show this wizard on startup";
            this.chkDontShowAgain.UseVisualStyleBackColor = true;
            this.chkDontShowAgain.CheckedChanged += new System.EventHandler(this.chkDontShowAgain_CheckedChanged);
            // 
            // btnReCheck
            // 
            this.btnReCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnReCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReCheck.Location = new System.Drawing.Point(520, 410);
            this.btnReCheck.Name = "btnReCheck";
            this.btnReCheck.Size = new System.Drawing.Size(100, 30);
            this.btnReCheck.TabIndex = 13;
            this.btnReCheck.Text = "🔄 Re-Check";
            this.btnReCheck.UseVisualStyleBackColor = false;
            this.btnReCheck.Click += new System.EventHandler(this.btnReCheck_Click);
            // 
            // SetupWizard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(650, 451);
            this.Controls.Add(this.btnReCheck);
            this.Controls.Add(this.chkDontShowAgain);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.lblVMultiInfo);
            this.Controls.Add(this.btnUninstallVMulti);
            this.Controls.Add(this.btnInstallVMulti);
            this.Controls.Add(this.chkUnlockVMulti);
            this.Controls.Add(this.lblVMultiStatus);
            this.Controls.Add(this.lblVMultiTitle);
            this.Controls.Add(this.btnUninstallService);
            this.Controls.Add(this.btnInstallService);
            this.Controls.Add(this.lblServiceStatus);
            this.Controls.Add(this.lblServiceTitle);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblTitle);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupWizard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wiimote4Guns Setup Wizard";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDescription;

        private System.Windows.Forms.Label lblServiceTitle;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Button btnInstallService;
        private System.Windows.Forms.Button btnUninstallService;
        private System.Windows.Forms.Label lblVMultiTitle;
        private System.Windows.Forms.Label lblVMultiStatus;
        private System.Windows.Forms.CheckBox chkUnlockVMulti;
        private System.Windows.Forms.Button btnInstallVMulti;
        private System.Windows.Forms.Button btnUninstallVMulti;
        private System.Windows.Forms.Label lblVMultiInfo;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.CheckBox chkDontShowAgain;
        private System.Windows.Forms.Button btnReCheck;
    }
}
