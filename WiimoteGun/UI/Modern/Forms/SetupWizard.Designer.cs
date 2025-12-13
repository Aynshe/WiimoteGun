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
            this.lblInterceptionTitle = new System.Windows.Forms.Label();
            this.lblInterceptionStatus = new System.Windows.Forms.Label();
            this.btnInstallInterception = new System.Windows.Forms.Button();
            this.btnUninstallInterception = new System.Windows.Forms.Button();
            this.lblServiceTitle = new System.Windows.Forms.Label();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.btnInstallService = new System.Windows.Forms.Button();
            this.btnUninstallService = new System.Windows.Forms.Button();
            this.lblVMultiTitle = new System.Windows.Forms.Label();
            this.lblVMultiStatus = new System.Windows.Forms.Label();
            this.btnInstallVMulti = new System.Windows.Forms.Button();
            this.btnUninstallVMulti = new System.Windows.Forms.Button();
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
            this.lblDescription.Size = new System.Drawing.Size(610, 30);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "The following components are required for WiimoteGun to function correctly.";
            // 
            // lblInterceptionTitle
            // 
            this.lblInterceptionTitle.AutoSize = true;
            this.lblInterceptionTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblInterceptionTitle.Location = new System.Drawing.Point(20, 110);
            this.lblInterceptionTitle.Name = "lblInterceptionTitle";
            this.lblInterceptionTitle.Size = new System.Drawing.Size(180, 20);
            this.lblInterceptionTitle.TabIndex = 2;
            this.lblInterceptionTitle.Text = "1. Interception Driver:";
            // 
            // lblInterceptionStatus
            // 
            this.lblInterceptionStatus.AutoSize = true;
            this.lblInterceptionStatus.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblInterceptionStatus.Location = new System.Drawing.Point(280, 110);
            this.lblInterceptionStatus.Name = "lblInterceptionStatus";
            this.lblInterceptionStatus.Size = new System.Drawing.Size(85, 20);
            this.lblInterceptionStatus.TabIndex = 3;
            this.lblInterceptionStatus.Text = "Checking...";
            // 
            // btnInstallInterception
            // 
            this.btnInstallInterception.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnInstallInterception.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInstallInterception.Enabled = false;
            this.btnInstallInterception.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstallInterception.Location = new System.Drawing.Point(430, 105);
            this.btnInstallInterception.Name = "btnInstallInterception";
            this.btnInstallInterception.Size = new System.Drawing.Size(100, 30);
            this.btnInstallInterception.TabIndex = 4;
            this.btnInstallInterception.Text = "Install Driver";
            this.btnInstallInterception.UseVisualStyleBackColor = false;
            this.btnInstallInterception.Click += new System.EventHandler(this.btnInstallInterception_Click);
            // 
            // btnUninstallInterception
            // 
            this.btnUninstallInterception.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnUninstallInterception.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUninstallInterception.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstallInterception.Location = new System.Drawing.Point(540, 105);
            this.btnUninstallInterception.Name = "btnUninstallInterception";
            this.btnUninstallInterception.Size = new System.Drawing.Size(80, 30);
            this.btnUninstallInterception.TabIndex = 5;
            this.btnUninstallInterception.Text = "Uninstall";
            this.btnUninstallInterception.UseVisualStyleBackColor = false;
            this.btnUninstallInterception.Click += new System.EventHandler(this.btnUninstallInterception_Click);
            // 
            // lblServiceTitle
            // 
            this.lblServiceTitle.AutoSize = true;
            this.lblServiceTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblServiceTitle.Location = new System.Drawing.Point(20, 170);
            this.lblServiceTitle.Name = "lblServiceTitle";
            this.lblServiceTitle.Size = new System.Drawing.Size(195, 20);
            this.lblServiceTitle.TabIndex = 6;
            this.lblServiceTitle.Text = "2. WiimoteGun Service:";
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblServiceStatus.Location = new System.Drawing.Point(280, 170);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(85, 20);
            this.lblServiceStatus.TabIndex = 7;
            this.lblServiceStatus.Text = "Checking...";
            // 
            // btnInstallService
            // 
            this.btnInstallService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnInstallService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInstallService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstallService.Location = new System.Drawing.Point(430, 165);
            this.btnInstallService.Name = "btnInstallService";
            this.btnInstallService.Size = new System.Drawing.Size(100, 30);
            this.btnInstallService.TabIndex = 8;
            this.btnInstallService.Text = "Install Service";
            this.btnInstallService.UseVisualStyleBackColor = false;
            this.btnInstallService.Click += new System.EventHandler(this.btnInstallService_Click);
            // 
            // btnUninstallService
            // 
            this.btnUninstallService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnUninstallService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUninstallService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstallService.Location = new System.Drawing.Point(540, 165);
            this.btnUninstallService.Name = "btnUninstallService";
            this.btnUninstallService.Size = new System.Drawing.Size(80, 30);
            this.btnUninstallService.TabIndex = 9;
            this.btnUninstallService.Text = "Uninstall";
            this.btnUninstallService.UseVisualStyleBackColor = false;
            this.btnUninstallService.Click += new System.EventHandler(this.btnUninstallService_Click);
            // 
            // lblVMultiTitle
            // 
            this.lblVMultiTitle.AutoSize = true;
            this.lblVMultiTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblVMultiTitle.Location = new System.Drawing.Point(20, 230);
            this.lblVMultiTitle.Name = "lblVMultiTitle";
            this.lblVMultiTitle.Size = new System.Drawing.Size(250, 20);
            this.lblVMultiTitle.TabIndex = 10;
            this.lblVMultiTitle.Text = "3. WiimoteGun Driver:";
            // 
            // lblVMultiStatus
            // 
            this.lblVMultiStatus.AutoSize = true;
            this.lblVMultiStatus.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblVMultiStatus.Location = new System.Drawing.Point(280, 230);
            this.lblVMultiStatus.Name = "lblVMultiStatus";
            this.lblVMultiStatus.Size = new System.Drawing.Size(85, 20);
            this.lblVMultiStatus.TabIndex = 11;
            this.lblVMultiStatus.Text = "Checking...";
            // 
            // btnInstallVMulti
            // 
            this.btnInstallVMulti.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnInstallVMulti.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInstallVMulti.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstallVMulti.Location = new System.Drawing.Point(430, 225);
            this.btnInstallVMulti.Name = "btnInstallVMulti";
            this.btnInstallVMulti.Size = new System.Drawing.Size(100, 30);
            this.btnInstallVMulti.TabIndex = 12;
            this.btnInstallVMulti.Text = "Install Driver";
            this.btnInstallVMulti.UseVisualStyleBackColor = false;
            this.btnInstallVMulti.Click += new System.EventHandler(this.btnInstallVMulti_Click);
            // 
            // btnUninstallVMulti
            // 
            this.btnUninstallVMulti.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnUninstallVMulti.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUninstallVMulti.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstallVMulti.Location = new System.Drawing.Point(540, 225);
            this.btnUninstallVMulti.Name = "btnUninstallVMulti";
            this.btnUninstallVMulti.Size = new System.Drawing.Size(80, 30);
            this.btnUninstallVMulti.TabIndex = 13;
            this.btnUninstallVMulti.Text = "Uninstall All";
            this.btnUninstallVMulti.UseVisualStyleBackColor = false;
            this.btnUninstallVMulti.Click += new System.EventHandler(this.btnUninstallVMulti_Click);
            // 
            // btnContinue
            // 
            this.btnContinue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this.btnContinue.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnContinue.Enabled = false;
            this.btnContinue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnContinue.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnContinue.Location = new System.Drawing.Point(225, 310);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(200, 40);
            this.btnContinue.TabIndex = 14;
            this.btnContinue.Text = "Start WiimoteGun";
            this.btnContinue.UseVisualStyleBackColor = false;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // btnSkip
            // 
            this.btnSkip.BackColor = System.Drawing.Color.Gray;
            this.btnSkip.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSkip.Location = new System.Drawing.Point(225, 360);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(200, 30);
            this.btnSkip.TabIndex = 15;
            this.btnSkip.Text = "Skip Setup (Run Anyway)";
            this.btnSkip.UseVisualStyleBackColor = false;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            // 
            // chkDontShowAgain
            // 
            this.chkDontShowAgain.AutoSize = true;
            this.chkDontShowAgain.ForeColor = System.Drawing.Color.LightGray;
            this.chkDontShowAgain.Location = new System.Drawing.Point(215, 400);
            this.chkDontShowAgain.Name = "chkDontShowAgain";
            this.chkDontShowAgain.Size = new System.Drawing.Size(212, 19);
            this.chkDontShowAgain.TabIndex = 16;
            this.chkDontShowAgain.Text = "Don't show this wizard on startup";
            this.chkDontShowAgain.UseVisualStyleBackColor = true;
            this.chkDontShowAgain.CheckedChanged += new System.EventHandler(this.chkDontShowAgain_CheckedChanged);
            // 
            // btnReCheck
            // 
            this.btnReCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnReCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReCheck.Location = new System.Drawing.Point(520, 395);
            this.btnReCheck.Name = "btnReCheck";
            this.btnReCheck.Size = new System.Drawing.Size(100, 30);
            this.btnReCheck.TabIndex = 17;
            this.btnReCheck.Text = "🔄 Re-Check";
            this.btnReCheck.UseVisualStyleBackColor = false;
            this.btnReCheck.Click += new System.EventHandler(this.btnReCheck_Click);
            // 
            // SetupWizard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(650, 461);
            this.Controls.Add(this.btnReCheck);
            this.Controls.Add(this.chkDontShowAgain);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.btnUninstallVMulti);
            this.Controls.Add(this.btnInstallVMulti);
            this.Controls.Add(this.lblVMultiStatus);
            this.Controls.Add(this.lblVMultiTitle);
            this.Controls.Add(this.btnUninstallService);
            this.Controls.Add(this.btnInstallService);
            this.Controls.Add(this.lblServiceStatus);
            this.Controls.Add(this.lblServiceTitle);
            this.Controls.Add(this.btnUninstallInterception);
            this.Controls.Add(this.btnInstallInterception);
            this.Controls.Add(this.lblInterceptionStatus);
            this.Controls.Add(this.lblInterceptionTitle);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblTitle);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupWizard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WiimoteGun Setup Wizard";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblInterceptionTitle;
        private System.Windows.Forms.Label lblInterceptionStatus;
        private System.Windows.Forms.Button btnInstallInterception;
        private System.Windows.Forms.Button btnUninstallInterception;
        private System.Windows.Forms.Label lblServiceTitle;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Button btnInstallService;
        private System.Windows.Forms.Button btnUninstallService;
        private System.Windows.Forms.Label lblVMultiTitle;
        private System.Windows.Forms.Label lblVMultiStatus;
        private System.Windows.Forms.Button btnInstallVMulti;
        private System.Windows.Forms.Button btnUninstallVMulti;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.CheckBox chkDontShowAgain;
        private System.Windows.Forms.Button btnReCheck;
    }
}
