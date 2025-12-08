namespace WiimoteGun
{
    partial class ProfileOverlay
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.lblCurrentGame = new System.Windows.Forms.Label();
            this.btnSelectExe = new System.Windows.Forms.Button();
            this.lblLinkedExe = new System.Windows.Forms.Label();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.lblSubfolder = new System.Windows.Forms.Label();
            this.comboBoxSubfolders = new System.Windows.Forms.ComboBox();
            this.btnNewFolder = new System.Windows.Forms.Button();
            this.lblQuickMappings = new System.Windows.Forms.Label();
            this.btnAssignMode = new System.Windows.Forms.Button();
            this.btnHotkeys = new System.Windows.Forms.Button();
            this.lblAssignStatus = new System.Windows.Forms.Label();
            this.comboActionSelector = new System.Windows.Forms.ComboBox();
            this.btnConfirmAssign = new System.Windows.Forms.Button();
            this.btnCancelAssign = new System.Windows.Forms.Button();
            this.tabControlPlayers = new System.Windows.Forms.TabControl();
            this.tabP1 = new System.Windows.Forms.TabPage();
            this.panelP1Mappings = new System.Windows.Forms.Panel();
            this.tabP2 = new System.Windows.Forms.TabPage();
            this.panelP2Mappings = new System.Windows.Forms.Panel();
            this.tabP3 = new System.Windows.Forms.TabPage();
            this.panelP3Mappings = new System.Windows.Forms.Panel();
            this.tabP4 = new System.Windows.Forms.TabPage();
            this.panelP4Mappings = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.chkAutoLoad = new System.Windows.Forms.CheckBox();
            this.chkEnableGyro = new System.Windows.Forms.CheckBox();
            this.comboBoxProfiles = new System.Windows.Forms.ComboBox();
            this.lblLoadProfile = new System.Windows.Forms.Label();
            this.tabControlPlayers.SuspendLayout();
            this.tabP1.SuspendLayout();
            this.tabP2.SuspendLayout();
            this.tabP3.SuspendLayout();
            this.tabP4.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(210, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🎮 Remap Profile Manager";
            // 
            // lblCurrentGame
            // 
            this.lblCurrentGame.AutoSize = true;
            this.lblCurrentGame.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCurrentGame.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblCurrentGame.Location = new System.Drawing.Point(20, 55);
            this.lblCurrentGame.Name = "lblCurrentGame";
            this.lblCurrentGame.Size = new System.Drawing.Size(150, 15);
            this.lblCurrentGame.TabIndex = 1;
            this.lblCurrentGame.Text = "Current Game: Unknown";
            // 
            // lblLinkedExe
            // 
            this.lblLinkedExe.AutoSize = true;
            this.lblLinkedExe.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblLinkedExe.ForeColor = System.Drawing.Color.Gray;
            this.lblLinkedExe.Location = new System.Drawing.Point(20, 72);
            this.lblLinkedExe.Name = "lblLinkedExe";
            this.lblLinkedExe.Size = new System.Drawing.Size(0, 13);
            this.lblLinkedExe.TabIndex = 2;
            this.lblLinkedExe.Text = "";
            // 
            // btnSelectExe
            // 
            this.btnSelectExe.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.btnSelectExe.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(63, 63, 63);
            this.btnSelectExe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectExe.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnSelectExe.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.btnSelectExe.Location = new System.Drawing.Point(250, 52);
            this.btnSelectExe.Name = "btnSelectExe";
            this.btnSelectExe.Size = new System.Drawing.Size(30, 22);
            this.btnSelectExe.TabIndex = 15;
            this.btnSelectExe.Text = "...";
            this.btnSelectExe.UseVisualStyleBackColor = false;
            this.btnSelectExe.Click += new System.EventHandler(this.btnSelectExe_Click);
            // 
            // lblProfileName
            // 
            this.lblProfileName.AutoSize = true;
            this.lblProfileName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblProfileName.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblProfileName.Location = new System.Drawing.Point(20, 95);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Size = new System.Drawing.Size(96, 19);
            this.lblProfileName.TabIndex = 3;
            this.lblProfileName.Text = "Profile Name:";
            // 
            // txtProfileName
            // 
            this.txtProfileName.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.txtProfileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtProfileName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtProfileName.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.txtProfileName.Location = new System.Drawing.Point(140, 92);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(420, 25);
            this.txtProfileName.TabIndex = 3;
            // 
            // lblSubfolder
            // 
            this.lblSubfolder.AutoSize = true;
            this.lblSubfolder.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubfolder.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblSubfolder.Location = new System.Drawing.Point(20, 135);
            this.lblSubfolder.Name = "lblSubfolder";
            this.lblSubfolder.Size = new System.Drawing.Size(73, 19);
            this.lblSubfolder.TabIndex = 4;
            this.lblSubfolder.Text = "Subfolder:";
            // 
            // comboBoxSubfolders
            // 
            this.comboBoxSubfolders.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.comboBoxSubfolders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSubfolders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxSubfolders.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBoxSubfolders.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.comboBoxSubfolders.FormattingEnabled = true;
            this.comboBoxSubfolders.Location = new System.Drawing.Point(140, 132);
            this.comboBoxSubfolders.Name = "comboBoxSubfolders";
            this.comboBoxSubfolders.Size = new System.Drawing.Size(340, 25);
            this.comboBoxSubfolders.TabIndex = 5;
            this.comboBoxSubfolders.SelectedIndexChanged += new System.EventHandler(this.comboBoxSubfolders_SelectedIndexChanged);
            // 
            // btnNewFolder
            // 
            this.btnNewFolder.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.btnNewFolder.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(63, 63, 63);
            this.btnNewFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewFolder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnNewFolder.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.btnNewFolder.Location = new System.Drawing.Point(490, 132);
            this.btnNewFolder.Name = "btnNewFolder";
            this.btnNewFolder.Size = new System.Drawing.Size(70, 25);
            this.btnNewFolder.TabIndex = 6;
            this.btnNewFolder.Text = "New";
            this.btnNewFolder.UseVisualStyleBackColor = false;
            this.btnNewFolder.Click += new System.EventHandler(this.btnNewFolder_Click);
            // 
            // lblLoadProfile
            // 
            this.lblLoadProfile.AutoSize = true;
            this.lblLoadProfile.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblLoadProfile.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblLoadProfile.Location = new System.Drawing.Point(20, 175);
            this.lblLoadProfile.Name = "lblLoadProfile";
            this.lblLoadProfile.Size = new System.Drawing.Size(87, 19);
            this.lblLoadProfile.TabIndex = 7;
            this.lblLoadProfile.Text = "Load Profile:";
            // 
            // comboBoxProfiles
            // 
            this.comboBoxProfiles.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.comboBoxProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProfiles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxProfiles.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBoxProfiles.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.comboBoxProfiles.FormattingEnabled = true;
            this.comboBoxProfiles.Location = new System.Drawing.Point(140, 172);
            this.comboBoxProfiles.Name = "comboBoxProfiles";
            this.comboBoxProfiles.Size = new System.Drawing.Size(420, 25);
            this.comboBoxProfiles.TabIndex = 8;
            // 
            // lblQuickMappings
            // 
            this.lblQuickMappings.AutoSize = true;
            this.lblQuickMappings.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblQuickMappings.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.lblQuickMappings.Location = new System.Drawing.Point(20, 220);
            this.lblQuickMappings.Name = "lblQuickMappings";
            this.lblQuickMappings.Size = new System.Drawing.Size(150, 20);
            this.lblQuickMappings.TabIndex = 9;
            this.lblQuickMappings.Text = "Button Mappings:";
            // 
            // btnAssignMode
            // 
            this.btnAssignMode.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnAssignMode.FlatAppearance.BorderSize = 0;
            this.btnAssignMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAssignMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAssignMode.ForeColor = System.Drawing.Color.White;
            this.btnAssignMode.Location = new System.Drawing.Point(175, 215);
            this.btnAssignMode.Name = "btnAssignMode";
            this.btnAssignMode.Size = new System.Drawing.Size(160, 30);
            this.btnAssignMode.TabIndex = 10;
            this.btnAssignMode.Text = "✏️ Assign Button";
            this.btnAssignMode.UseVisualStyleBackColor = false;
            this.btnAssignMode.Click += new System.EventHandler(this.btnAssignMode_Click);
            // 
            // btnHotkeys
            // 
            this.btnHotkeys.BackColor = System.Drawing.Color.FromArgb(204, 122, 0);
            this.btnHotkeys.FlatAppearance.BorderSize = 0;
            this.btnHotkeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHotkeys.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnHotkeys.ForeColor = System.Drawing.Color.White;
            this.btnHotkeys.Location = new System.Drawing.Point(340, 215); // Adjacent to Assign Button
            this.btnHotkeys.Name = "btnHotkeys";
            this.btnHotkeys.Size = new System.Drawing.Size(140, 30);
            this.btnHotkeys.TabIndex = 16;
            this.btnHotkeys.Text = "⚡ Hotkeys";
            this.btnHotkeys.UseVisualStyleBackColor = false;
            this.btnHotkeys.Click += new System.EventHandler(this.btnHotkeys_Click);
            // 
            // lblAssignStatus - Popup overlay (EN/FR: Overlay popup)
            // 
            this.lblAssignStatus.BackColor = System.Drawing.Color.FromArgb(200, 30, 30, 30);
            this.lblAssignStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblAssignStatus.ForeColor = System.Drawing.Color.Orange;
            this.lblAssignStatus.Location = new System.Drawing.Point(150, 300);
            this.lblAssignStatus.Name = "lblAssignStatus";
            this.lblAssignStatus.Size = new System.Drawing.Size(300, 80);
            this.lblAssignStatus.TabIndex = 11;
            this.lblAssignStatus.Text = "";
            this.lblAssignStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblAssignStatus.Visible = false;
            // 
            // comboActionSelector
            // 
            this.comboActionSelector.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.comboActionSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboActionSelector.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboActionSelector.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.comboActionSelector.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.comboActionSelector.FormattingEnabled = true;
            this.comboActionSelector.Location = new System.Drawing.Point(100, 300);
            this.comboActionSelector.Name = "comboActionSelector";
            this.comboActionSelector.Size = new System.Drawing.Size(250, 25);
            this.comboActionSelector.TabIndex = 12;
            this.comboActionSelector.Visible = false;
            // 
            // btnConfirmAssign
            // 
            this.btnConfirmAssign.BackColor = System.Drawing.Color.FromArgb(0, 180, 0);
            this.btnConfirmAssign.FlatAppearance.BorderSize = 0;
            this.btnConfirmAssign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmAssign.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnConfirmAssign.ForeColor = System.Drawing.Color.White;
            this.btnConfirmAssign.Location = new System.Drawing.Point(360, 298);
            this.btnConfirmAssign.Name = "btnConfirmAssign";
            this.btnConfirmAssign.Size = new System.Drawing.Size(90, 28);
            this.btnConfirmAssign.TabIndex = 13;
            this.btnConfirmAssign.Text = "✓ Confirm";
            this.btnConfirmAssign.UseVisualStyleBackColor = false;
            this.btnConfirmAssign.Visible = false;
            this.btnConfirmAssign.Click += new System.EventHandler(this.btnConfirmAssign_Click);
            // 
            // btnCancelAssign
            // 
            this.btnCancelAssign.BackColor = System.Drawing.Color.FromArgb(180, 0, 0);
            this.btnCancelAssign.FlatAppearance.BorderSize = 0;
            this.btnCancelAssign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelAssign.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCancelAssign.ForeColor = System.Drawing.Color.White;
            this.btnCancelAssign.Location = new System.Drawing.Point(460, 298);
            this.btnCancelAssign.Name = "btnCancelAssign";
            this.btnCancelAssign.Size = new System.Drawing.Size(90, 28);
            this.btnCancelAssign.TabIndex = 14;
            this.btnCancelAssign.Text = "✗ Cancel";
            this.btnCancelAssign.UseVisualStyleBackColor = false;
            this.btnCancelAssign.Visible = false;
            this.btnCancelAssign.Click += new System.EventHandler(this.btnCancelAssign_Click);
            // 
            // tabControlPlayers
            // 
            this.tabControlPlayers.Controls.Add(this.tabP1);
            this.tabControlPlayers.Controls.Add(this.tabP2);
            this.tabControlPlayers.Controls.Add(this.tabP3);
            this.tabControlPlayers.Controls.Add(this.tabP4);
            this.tabControlPlayers.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.tabControlPlayers.Location = new System.Drawing.Point(24, 250);
            this.tabControlPlayers.Name = "tabControlPlayers";
            this.tabControlPlayers.SelectedIndex = 0;
            this.tabControlPlayers.Size = new System.Drawing.Size(535, 380);
            this.tabControlPlayers.TabIndex = 8;
            this.tabControlPlayers.SelectedIndexChanged += new System.EventHandler(this.tabControlPlayers_SelectedIndexChanged);
            // 
            // tabP1
            // 
            this.tabP1.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.tabP1.Controls.Add(this.panelP1Mappings);
            this.tabP1.Location = new System.Drawing.Point(4, 24);
            this.tabP1.Name = "tabP1";
            this.tabP1.Padding = new System.Windows.Forms.Padding(0);
            this.tabP1.Size = new System.Drawing.Size(527, 352);
            this.tabP1.TabIndex = 0;
            this.tabP1.Text = "Player 1";
            // 
            // panelP1Mappings
            // 
            this.panelP1Mappings.AutoScroll = true;
            this.panelP1Mappings.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.panelP1Mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelP1Mappings.Location = new System.Drawing.Point(0, 0);
            this.panelP1Mappings.Name = "panelP1Mappings";
            this.panelP1Mappings.Size = new System.Drawing.Size(527, 352);
            this.panelP1Mappings.TabIndex = 0;
            // 
            // tabP2
            // 
            this.tabP2.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.tabP2.Controls.Add(this.panelP2Mappings);
            this.tabP2.Location = new System.Drawing.Point(4, 24);
            this.tabP2.Name = "tabP2";
            this.tabP2.Padding = new System.Windows.Forms.Padding(0);
            this.tabP2.Size = new System.Drawing.Size(527, 352);
            this.tabP2.TabIndex = 1;
            this.tabP2.Text = "Player 2";
            // 
            // panelP2Mappings
            // 
            this.panelP2Mappings.AutoScroll = true;
            this.panelP2Mappings.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.panelP2Mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelP2Mappings.Location = new System.Drawing.Point(0, 0);
            this.panelP2Mappings.Name = "panelP2Mappings";
            this.panelP2Mappings.Size = new System.Drawing.Size(527, 352);
            this.panelP2Mappings.TabIndex = 0;
            // 
            // tabP3
            // 
            this.tabP3.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.tabP3.Controls.Add(this.panelP3Mappings);
            this.tabP3.Location = new System.Drawing.Point(4, 24);
            this.tabP3.Name = "tabP3";
            this.tabP3.Size = new System.Drawing.Size(527, 352);
            this.tabP3.TabIndex = 2;
            this.tabP3.Text = "Player 3";
            // 
            // panelP3Mappings
            // 
            this.panelP3Mappings.AutoScroll = true;
            this.panelP3Mappings.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.panelP3Mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelP3Mappings.Location = new System.Drawing.Point(0, 0);
            this.panelP3Mappings.Name = "panelP3Mappings";
            this.panelP3Mappings.Size = new System.Drawing.Size(527, 352);
            this.panelP3Mappings.TabIndex = 0;
            // 
            // tabP4
            // 
            this.tabP4.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.tabP4.Controls.Add(this.panelP4Mappings);
            this.tabP4.Location = new System.Drawing.Point(4, 24);
            this.tabP4.Name = "tabP4";
            this.tabP4.Size = new System.Drawing.Size(527, 352);
            this.tabP4.TabIndex = 3;
            this.tabP4.Text = "Player 4";
            // 
            // panelP4Mappings
            // 
            this.panelP4Mappings.AutoScroll = true;
            this.panelP4Mappings.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            this.panelP4Mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelP4Mappings.Location = new System.Drawing.Point(0, 0);
            this.panelP4Mappings.Name = "panelP4Mappings";
            this.panelP4Mappings.Size = new System.Drawing.Size(527, 352);
            this.panelP4Mappings.TabIndex = 0;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(100, 640);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(140, 40);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "💾 Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // chkAutoLoad
            // 
            this.chkAutoLoad.AutoSize = true;
            this.chkAutoLoad.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkAutoLoad.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.chkAutoLoad.Location = new System.Drawing.Point(185, 580);
            this.chkAutoLoad.Name = "chkAutoLoad";
            this.chkAutoLoad.Size = new System.Drawing.Size(200, 19);
            this.chkAutoLoad.TabIndex = 14;
            this.chkAutoLoad.Text = "⚙️ Auto-load for this executable";
            this.chkAutoLoad.UseVisualStyleBackColor = true;
            this.chkAutoLoad.CheckedChanged += new System.EventHandler(this.chkAutoLoad_CheckedChanged);
            // 
            // chkEnableGyro
            // 
            this.chkEnableGyro.AutoSize = true;
            this.chkEnableGyro.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkEnableGyro.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.chkEnableGyro.Location = new System.Drawing.Point(185, 605);
            this.chkEnableGyro.Name = "chkEnableGyro";
            this.chkEnableGyro.Size = new System.Drawing.Size(250, 19);
            this.chkEnableGyro.TabIndex = 15;
            this.chkEnableGyro.Text = "🎯 Enable Gyro Aiming (FPS Mode)";
            this.chkEnableGyro.UseVisualStyleBackColor = true;
            this.chkEnableGyro.CheckedChanged += new System.EventHandler(this.chkEnableGyro_CheckedChanged);
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnLoad.FlatAppearance.BorderSize = 0;
            this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoad.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Location = new System.Drawing.Point(360, 640);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(140, 40);
            this.btnLoad.TabIndex = 12;
            this.btnLoad.Text = "📂 Load";
            this.btnLoad.UseVisualStyleBackColor = false;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(420, 640);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(140, 40);
            this.btnClose.TabIndex = 13;
            this.btnClose.Text = "❌ Close";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ProfileOverlay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 840);
            this.Controls.Add(this.btnCancelAssign);
            this.Controls.Add(this.btnConfirmAssign);
            this.Controls.Add(this.comboActionSelector);
            this.Controls.Add(this.lblAssignStatus);
            this.Controls.Add(this.btnAssignMode);
            this.Controls.Add(this.btnHotkeys);
            this.Controls.Add(this.chkAutoLoad);
            this.Controls.Add(this.chkEnableGyro);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tabControlPlayers);
            this.Controls.Add(this.lblQuickMappings);
            this.Controls.Add(this.comboBoxProfiles);
            this.Controls.Add(this.lblLoadProfile);
            this.Controls.Add(this.btnNewFolder);
            this.Controls.Add(this.comboBoxSubfolders);
            this.Controls.Add(this.lblSubfolder);
            this.Controls.Add(this.txtProfileName);
            this.Controls.Add(this.lblProfileName);
            this.Controls.Add(this.lblLinkedExe);
            this.Controls.Add(this.btnSelectExe);
            this.Controls.Add(this.lblCurrentGame);
            this.Controls.Add(this.lblTitle);
            this.Name = "ProfileOverlay";
            this.Text = "ProfileOverlay";
            this.Load += new System.EventHandler(this.ProfileOverlay_Load);
            this.tabControlPlayers.ResumeLayout(false);
            this.tabP1.ResumeLayout(false);
            this.tabP2.ResumeLayout(false);
            this.tabP3.ResumeLayout(false);
            this.tabP4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblCurrentGame;
        private System.Windows.Forms.Button btnSelectExe;
        private System.Windows.Forms.Label lblLinkedExe;
        private System.Windows.Forms.Label lblProfileName;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Label lblSubfolder;
        private System.Windows.Forms.ComboBox comboBoxSubfolders;
        private System.Windows.Forms.Button btnNewFolder;
        private System.Windows.Forms.Label lblQuickMappings;
        private System.Windows.Forms.Button btnAssignMode;
        private System.Windows.Forms.Button btnHotkeys;
        private System.Windows.Forms.Label lblAssignStatus;
        private System.Windows.Forms.ComboBox comboActionSelector;
        private System.Windows.Forms.Button btnConfirmAssign;
        private System.Windows.Forms.Button btnCancelAssign;
        private System.Windows.Forms.TabControl tabControlPlayers;
        private System.Windows.Forms.TabPage tabP1;
        private System.Windows.Forms.Panel panelP1Mappings;
        private System.Windows.Forms.TabPage tabP2;
        private System.Windows.Forms.Panel panelP2Mappings;
        private System.Windows.Forms.TabPage tabP3;
        private System.Windows.Forms.Panel panelP3Mappings;
        private System.Windows.Forms.TabPage tabP4;
        private System.Windows.Forms.Panel panelP4Mappings;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.CheckBox chkAutoLoad;
        private System.Windows.Forms.CheckBox chkEnableGyro;
        private System.Windows.Forms.ComboBox comboBoxProfiles;
        private System.Windows.Forms.Label lblLoadProfile;
        
        // Navigation system (EN/FR: Système de navigation)
        private System.Windows.Forms.Panel panelHome;
        private System.Windows.Forms.Panel panelMapping;
        private System.Windows.Forms.Label lblHomeTitle;
        private System.Windows.Forms.Label lblHomeDescription;
        private System.Windows.Forms.Button btnNavOptions;
        private System.Windows.Forms.Button btnNavMapping;
        private System.Windows.Forms.Button btnNavAssign;
        private System.Windows.Forms.Button btnNavIRViz;
        private System.Windows.Forms.Label lblFooter;
        private System.Windows.Forms.Button btnBackToHome;
    }
}
