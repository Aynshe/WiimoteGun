namespace W4G_Setup
{
    partial class SetupForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.txtInstallPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblCheckTitle = new System.Windows.Forms.Label();
            this.chkArchive = new System.Windows.Forms.Label();
            this.chkRetroBat = new System.Windows.Forms.Label();
            this.chkRuntime = new System.Windows.Forms.Label();
            this.chkInstalled = new System.Windows.Forms.Label();
            this.chkLayoutWarning = new System.Windows.Forms.Label();
            this.lblLayout = new System.Windows.Forms.Label();
            this.cmbLayout = new System.Windows.Forms.ComboBox();
            this.chkHybrid = new System.Windows.Forms.CheckBox();
            this.lblHybridInfo = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnFinish = new System.Windows.Forms.Button();
            this.lnkGitHub = new System.Windows.Forms.LinkLabel();
            this.lnkRetroBat = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.lblTitle.Location = new System.Drawing.Point(20, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(520, 38);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Wiimote4Guns";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
            this.lblSubtitle.ForeColor = System.Drawing.Color.LightGray;
            this.lblSubtitle.Location = new System.Drawing.Point(20, 50);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(520, 20);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Setup Wizard";
            this.lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.lblPath.ForeColor = System.Drawing.Color.White;
            this.lblPath.Location = new System.Drawing.Point(20, 88);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(112, 17);
            this.lblPath.TabIndex = 2;
            this.lblPath.Text = "Installation folder:";
            // 
            // txtInstallPath
            // 
            this.txtInstallPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(55)))));
            this.txtInstallPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtInstallPath.ForeColor = System.Drawing.Color.White;
            this.txtInstallPath.Location = new System.Drawing.Point(20, 108);
            this.txtInstallPath.Name = "txtInstallPath";
            this.txtInstallPath.Size = new System.Drawing.Size(400, 24);
            this.txtInstallPath.TabIndex = 3;
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(70)))));
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.ForeColor = System.Drawing.Color.White;
            this.btnBrowse.Location = new System.Drawing.Point(430, 106);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(100, 27);
            this.btnBrowse.TabIndex = 4;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = false;
            // 
            // lblCheckTitle
            // 
            this.lblCheckTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCheckTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblCheckTitle.Location = new System.Drawing.Point(20, 142);
            this.lblCheckTitle.Name = "lblCheckTitle";
            this.lblCheckTitle.Size = new System.Drawing.Size(510, 20);
            this.lblCheckTitle.TabIndex = 5;
            this.lblCheckTitle.Text = "System Check";
            // 
            // chkArchive
            // 
            this.chkArchive.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkArchive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.chkArchive.Location = new System.Drawing.Point(30, 165);
            this.chkArchive.Name = "chkArchive";
            this.chkArchive.Size = new System.Drawing.Size(500, 18);
            this.chkArchive.TabIndex = 6;
            this.chkArchive.Text = "...";
            // 
            // chkRetroBat
            // 
            this.chkRetroBat.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkRetroBat.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.chkRetroBat.Location = new System.Drawing.Point(30, 183);
            this.chkRetroBat.Name = "chkRetroBat";
            this.chkRetroBat.Size = new System.Drawing.Size(500, 18);
            this.chkRetroBat.TabIndex = 7;
            this.chkRetroBat.Text = "...";
            // 
            // chkRuntime
            // 
            this.chkRuntime.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkRuntime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.chkRuntime.Location = new System.Drawing.Point(30, 201);
            this.chkRuntime.Name = "chkRuntime";
            this.chkRuntime.Size = new System.Drawing.Size(500, 18);
            this.chkRuntime.TabIndex = 8;
            this.chkRuntime.Text = "...";
            // 
            // chkInstalled
            // 
            this.chkInstalled.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.chkInstalled.Location = new System.Drawing.Point(30, 219);
            this.chkInstalled.Name = "chkInstalled";
            this.chkInstalled.Size = new System.Drawing.Size(500, 18);
            this.chkInstalled.TabIndex = 9;
            this.chkInstalled.Text = "...";
            // 
            // chkLayoutWarning
            // 
            this.chkLayoutWarning.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.chkLayoutWarning.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(140)))), ((int)(((byte)(0)))));
            this.chkLayoutWarning.Location = new System.Drawing.Point(30, 240);
            this.chkLayoutWarning.Name = "chkLayoutWarning";
            this.chkLayoutWarning.Size = new System.Drawing.Size(500, 28);
            this.chkLayoutWarning.TabIndex = 10;
            this.chkLayoutWarning.Visible = false;
            // 
            // lblLayout
            // 
            this.lblLayout.AutoSize = true;
            this.lblLayout.ForeColor = System.Drawing.Color.White;
            this.lblLayout.Location = new System.Drawing.Point(20, 278);
            this.lblLayout.Name = "lblLayout";
            this.lblLayout.Size = new System.Drawing.Size(75, 17);
            this.lblLayout.TabIndex = 11;
            this.lblLayout.Text = "LED Layout:";
            // 
            // cmbLayout
            // 
            this.cmbLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(55)))));
            this.cmbLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLayout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbLayout.ForeColor = System.Drawing.Color.White;
            this.cmbLayout.FormattingEnabled = true;
            this.cmbLayout.Items.AddRange(new object[] {
            "Wiimote Bar",
            "Gun4IR Diamond",
            "Two Wiimote Bars (Or Blacom)",
            "Four Corners"});
            this.cmbLayout.Location = new System.Drawing.Point(101, 275);
            this.cmbLayout.Name = "cmbLayout";
            this.cmbLayout.Size = new System.Drawing.Size(220, 25);
            this.cmbLayout.TabIndex = 12;
            // 
            // chkHybrid
            // 
            this.chkHybrid.AutoSize = true;
            this.chkHybrid.ForeColor = System.Drawing.Color.White;
            this.chkHybrid.Location = new System.Drawing.Point(20, 306);
            this.chkHybrid.Name = "chkHybrid";
            this.chkHybrid.Size = new System.Drawing.Size(203, 21);
            this.chkHybrid.TabIndex = 13;
            this.chkHybrid.Text = "Enable Hybrid PnP (alpha test)";
            // 
            // lblHybridInfo
            // 
            this.lblHybridInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHybridInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lblHybridInfo.Location = new System.Drawing.Point(24, 330);
            this.lblHybridInfo.Name = "lblHybridInfo";
            this.lblHybridInfo.Size = new System.Drawing.Size(500, 95);
            this.lblHybridInfo.TabIndex = 14;
            this.lblHybridInfo.Click += new System.EventHandler(this.lblHybridInfo_Click);
            // 
            // progress
            // 
            this.progress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.progress.Location = new System.Drawing.Point(20, 428);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(510, 16);
            this.progress.TabIndex = 15;
            this.progress.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(190)))));
            this.lblStatus.Location = new System.Drawing.Point(20, 450);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(510, 28);
            this.lblStatus.TabIndex = 16;
            // 
            // btnInstall
            // 
            this.btnInstall.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnInstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInstall.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnInstall.ForeColor = System.Drawing.Color.White;
            this.btnInstall.Location = new System.Drawing.Point(210, 490);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(140, 40);
            this.btnInstall.TabIndex = 17;
            this.btnInstall.Text = "Install";
            this.btnInstall.UseVisualStyleBackColor = false;
            // 
            // btnFinish
            // 
            this.btnFinish.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(80)))));
            this.btnFinish.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFinish.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnFinish.ForeColor = System.Drawing.Color.White;
            this.btnFinish.Location = new System.Drawing.Point(210, 490);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(140, 40);
            this.btnFinish.TabIndex = 18;
            this.btnFinish.Text = "Finish";
            this.btnFinish.UseVisualStyleBackColor = false;
            this.btnFinish.Visible = false;
            // 
            // lnkGitHub
            // 
            this.lnkGitHub.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lnkGitHub.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lnkGitHub.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            this.lnkGitHub.Location = new System.Drawing.Point(35, 533);
            this.lnkGitHub.Name = "lnkGitHub";
            this.lnkGitHub.Size = new System.Drawing.Size(130, 18);
            this.lnkGitHub.TabIndex = 19;
            this.lnkGitHub.TabStop = true;
            this.lnkGitHub.Text = "GitHub: Wiimote4Gun";
            this.lnkGitHub.VisitedLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            // 
            // lnkRetroBat
            // 
            this.lnkRetroBat.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lnkRetroBat.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lnkRetroBat.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            this.lnkRetroBat.Location = new System.Drawing.Point(396, 533);
            this.lnkRetroBat.Name = "lnkRetroBat";
            this.lnkRetroBat.Size = new System.Drawing.Size(105, 18);
            this.lnkRetroBat.TabIndex = 20;
            this.lnkRetroBat.TabStop = true;
            this.lnkRetroBat.Text = "www.retrobat.org";
            this.lnkRetroBat.VisitedLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            // 
            // SetupForm
            // 
            this.AcceptButton = this.btnInstall;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(551, 560);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(this.txtInstallPath);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lblCheckTitle);
            this.Controls.Add(this.chkArchive);
            this.Controls.Add(this.chkRetroBat);
            this.Controls.Add(this.chkRuntime);
            this.Controls.Add(this.chkInstalled);
            this.Controls.Add(this.chkLayoutWarning);
            this.Controls.Add(this.lblLayout);
            this.Controls.Add(this.cmbLayout);
            this.Controls.Add(this.chkHybrid);
            this.Controls.Add(this.lblHybridInfo);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnInstall);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.lnkGitHub);
            this.Controls.Add(this.lnkRetroBat);
            this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "SetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wiimote4Guns Setup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtInstallPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblCheckTitle;
        private System.Windows.Forms.Label chkArchive;
        private System.Windows.Forms.Label chkRetroBat;
        private System.Windows.Forms.Label chkRuntime;
        private System.Windows.Forms.Label chkInstalled;
        private System.Windows.Forms.Label chkLayoutWarning;
        private System.Windows.Forms.Label lblLayout;
        private System.Windows.Forms.ComboBox cmbLayout;
        private System.Windows.Forms.CheckBox chkHybrid;
        private System.Windows.Forms.Label lblHybridInfo;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnFinish;
        private System.Windows.Forms.LinkLabel lnkGitHub;
        private System.Windows.Forms.LinkLabel lnkRetroBat;
    }
}
