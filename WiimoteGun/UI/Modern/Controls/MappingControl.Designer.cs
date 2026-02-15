namespace WiimoteGun.Controls
{
    partial class MappingControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblCurrentGame = new System.Windows.Forms.Label();
            this.lblLinkedExe = new System.Windows.Forms.Label();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.lblSubfolder = new System.Windows.Forms.Label();
            this.lblLoadProfile = new System.Windows.Forms.Label();
            this.lblQuickMappings = new System.Windows.Forms.Label();
            this.btnSelectExe = new System.Windows.Forms.Button();
            this.btnNewFolder = new System.Windows.Forms.Button();
            this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnAssignMode = new System.Windows.Forms.Button();
            this.btnHotkeys = new System.Windows.Forms.Button();
            this.btnGamePadMapping = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.comboBoxSubfolders = new System.Windows.Forms.ComboBox();
            this.comboBoxProfiles = new System.Windows.Forms.ComboBox();
            this.chkAutoLoad = new System.Windows.Forms.CheckBox();
            this.chkEnableGyro = new System.Windows.Forms.CheckBox();
            this.tabControlPlayers = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.panelMappingDisplay = new System.Windows.Forms.Panel();
            this.lblAssignStatus = new System.Windows.Forms.Label();
            this.tabControlPlayers.SuspendLayout();
            this.comboActionSelector = new System.Windows.Forms.ComboBox();
            this.btnConfirmAssign = new System.Windows.Forms.Button();
            this.btnCancelAssign = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblCurrentGame
            // 
            this.lblCurrentGame.AutoSize = true;
            this.lblCurrentGame.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCurrentGame.ForeColor = System.Drawing.Color.White;
            this.lblCurrentGame.Location = new System.Drawing.Point(10, 10);
            this.lblCurrentGame.Name = "lblCurrentGame";
            this.lblCurrentGame.Size = new System.Drawing.Size(120, 15);
            this.lblCurrentGame.TabIndex = 0;
            this.lblCurrentGame.Text = "Current Game: None";
            // 
            // lblLinkedExe
            // 
            this.lblLinkedExe.AutoSize = true;
            this.lblLinkedExe.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLinkedExe.ForeColor = System.Drawing.Color.Gray;
            this.lblLinkedExe.Location = new System.Drawing.Point(10, 35);
            this.lblLinkedExe.Name = "lblLinkedExe";
            this.lblLinkedExe.Size = new System.Drawing.Size(97, 15);
            this.lblLinkedExe.TabIndex = 1;
            this.lblLinkedExe.Text = "Linked EXE: None";
            // 
            // lblProfileName
            // 
            this.lblProfileName.AutoSize = true;
            this.lblProfileName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblProfileName.ForeColor = System.Drawing.Color.White;
            this.lblProfileName.Location = new System.Drawing.Point(10, 70);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Size = new System.Drawing.Size(80, 15);
            this.lblProfileName.TabIndex = 2;
            this.lblProfileName.Text = "Profile Name:";
            // 
            // lblSubfolder
            // 
            this.lblSubfolder.AutoSize = true;
            this.lblSubfolder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubfolder.ForeColor = System.Drawing.Color.White;
            this.lblSubfolder.Location = new System.Drawing.Point(10, 100);
            this.lblSubfolder.Name = "lblSubfolder";
            this.lblSubfolder.Size = new System.Drawing.Size(63, 15);
            this.lblSubfolder.TabIndex = 3;
            this.lblSubfolder.Text = "Subfolder:";
            // 
            // lblLoadProfile
            // 
            this.lblLoadProfile.AutoSize = true;
            this.lblLoadProfile.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLoadProfile.ForeColor = System.Drawing.Color.White;
            this.lblLoadProfile.Location = new System.Drawing.Point(10, 130);
            this.lblLoadProfile.Name = "lblLoadProfile";
            this.lblLoadProfile.Size = new System.Drawing.Size(75, 15);
            this.lblLoadProfile.TabIndex = 4;
            this.lblLoadProfile.Text = "Load Profile:";
            // 
            // lblQuickMappings
            // 
            this.lblQuickMappings.AutoSize = true;
            this.lblQuickMappings.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblQuickMappings.ForeColor = System.Drawing.Color.White;
            this.lblQuickMappings.Location = new System.Drawing.Point(10, 170);
            this.lblQuickMappings.Name = "lblQuickMappings";
            this.lblQuickMappings.Size = new System.Drawing.Size(103, 15);
            this.lblQuickMappings.TabIndex = 5;
            this.lblQuickMappings.Text = "Quick Mappings:";
            // 
            // btnSelectExe
            // 
            this.btnSelectExe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnSelectExe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectExe.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnSelectExe.ForeColor = System.Drawing.Color.White;
            this.btnSelectExe.Location = new System.Drawing.Point(450, 8);
            this.btnSelectExe.Name = "btnSelectExe";
            this.btnSelectExe.Size = new System.Drawing.Size(100, 25);
            this.btnSelectExe.TabIndex = 6;
            this.btnSelectExe.Text = "Select EXE";
            this.btnSelectExe.UseVisualStyleBackColor = false;
            this.btnSelectExe.Click += new System.EventHandler(this.BtnSelectExe_Click);
            // 
            // btnNewFolder
            // 
            this.btnNewFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnNewFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewFolder.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnNewFolder.ForeColor = System.Drawing.Color.White;
            this.btnNewFolder.Location = new System.Drawing.Point(320, 97);
            this.btnNewFolder.Name = "btnNewFolder";
            this.btnNewFolder.Size = new System.Drawing.Size(80, 25);
            this.btnNewFolder.TabIndex = 7;
            this.btnNewFolder.Text = "New Folder";
            this.btnNewFolder.UseVisualStyleBackColor = false;
            this.btnNewFolder.Click += new System.EventHandler(this.BtnNewFolder_Click);
            // 
            // btnDeleteProfile
            // 
            this.btnDeleteProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnDeleteProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteProfile.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnDeleteProfile.ForeColor = System.Drawing.Color.White;
            this.btnDeleteProfile.Location = new System.Drawing.Point(320, 127);
            this.btnDeleteProfile.Name = "btnDeleteProfile";
            this.btnDeleteProfile.Size = new System.Drawing.Size(40, 25);
            this.btnDeleteProfile.TabIndex = 8;
            this.btnDeleteProfile.Text = "🗑";
            this.btnDeleteProfile.UseVisualStyleBackColor = false;
            this.btnDeleteProfile.Click += new System.EventHandler(this.BtnDeleteProfile_Click);
            // 
            // btnAssignMode
            // 
            this.btnAssignMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnAssignMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAssignMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAssignMode.ForeColor = System.Drawing.Color.White;
            this.btnAssignMode.Location = new System.Drawing.Point(190, 165);
            this.btnAssignMode.Name = "btnAssignMode";
            this.btnAssignMode.Size = new System.Drawing.Size(160, 30);
            this.btnAssignMode.TabIndex = 9;
            this.btnAssignMode.Text = "✏️ Assign Button";
            this.btnAssignMode.UseVisualStyleBackColor = false;
            this.btnAssignMode.Click += new System.EventHandler(this.BtnAssignMode_Click);
            // 
            // btnHotkeys
            // 
            this.btnHotkeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnHotkeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHotkeys.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnHotkeys.ForeColor = System.Drawing.Color.White;
            this.btnHotkeys.Location = new System.Drawing.Point(360, 165);
            this.btnHotkeys.Name = "btnHotkeys";
            this.btnHotkeys.Size = new System.Drawing.Size(100, 30);
            this.btnHotkeys.TabIndex = 10;
            this.btnHotkeys.Text = "Hotkeys";
            this.btnHotkeys.UseVisualStyleBackColor = false;
            this.btnHotkeys.Click += new System.EventHandler(this.BtnHotkeys_Click);
            // 
            // btnGamePadMapping
            // 
            this.btnGamePadMapping.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnGamePadMapping.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGamePadMapping.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnGamePadMapping.ForeColor = System.Drawing.Color.White;
            this.btnGamePadMapping.Location = new System.Drawing.Point(470, 165);
            this.btnGamePadMapping.Name = "btnGamePadMapping";
            this.btnGamePadMapping.Size = new System.Drawing.Size(80, 30);
            this.btnGamePadMapping.TabIndex = 10;
            this.btnGamePadMapping.Text = "🎮 GamePad";
            this.btnGamePadMapping.UseVisualStyleBackColor = false;
            this.btnGamePadMapping.Click += new System.EventHandler(this.BtnGamePadMapping_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(10, 685);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 25);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "💾 Save Profile";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoad.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Location = new System.Drawing.Point(170, 685);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(150, 25);
            this.btnLoad.TabIndex = 12;
            this.btnLoad.Text = "📂 Load Profile";
            this.btnLoad.UseVisualStyleBackColor = false;
            this.btnLoad.Click += new System.EventHandler(this.BtnLoad_Click);
            // 
            // txtProfileName
            // 
            this.txtProfileName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtProfileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtProfileName.ForeColor = System.Drawing.Color.White;
            this.txtProfileName.Location = new System.Drawing.Point(110, 67);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(200, 23);
            this.txtProfileName.TabIndex = 13;
            // 
            // comboBoxSubfolders
            // 
            this.comboBoxSubfolders.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.comboBoxSubfolders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSubfolders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxSubfolders.ForeColor = System.Drawing.Color.White;
            this.comboBoxSubfolders.FormattingEnabled = true;
            this.comboBoxSubfolders.Location = new System.Drawing.Point(110, 97);
            this.comboBoxSubfolders.Name = "comboBoxSubfolders";
            this.comboBoxSubfolders.Size = new System.Drawing.Size(200, 23);
            this.comboBoxSubfolders.TabIndex = 14;
            this.comboBoxSubfolders.SelectedIndexChanged += new System.EventHandler(this.ComboBoxSubfolders_SelectedIndexChanged);
            // 
            // comboBoxProfiles
            // 
            this.comboBoxProfiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.comboBoxProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProfiles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxProfiles.ForeColor = System.Drawing.Color.White;
            this.comboBoxProfiles.FormattingEnabled = true;
            this.comboBoxProfiles.Location = new System.Drawing.Point(110, 127);
            this.comboBoxProfiles.Name = "comboBoxProfiles";
            this.comboBoxProfiles.Size = new System.Drawing.Size(200, 23);
            this.comboBoxProfiles.TabIndex = 15;
            this.comboBoxProfiles.SelectedIndexChanged += new System.EventHandler(this.ComboBoxProfiles_SelectedIndexChanged);
            // 
            // chkAutoLoad
            // 
            this.chkAutoLoad.AutoSize = true;
            this.chkAutoLoad.ForeColor = System.Drawing.Color.White;
            this.chkAutoLoad.Location = new System.Drawing.Point(10, 655);
            this.chkAutoLoad.Name = "chkAutoLoad";
            this.chkAutoLoad.Size = new System.Drawing.Size(151, 19);
            this.chkAutoLoad.TabIndex = 16;
            this.chkAutoLoad.Text = "Auto-load for this game";
            this.chkAutoLoad.UseVisualStyleBackColor = true;
            this.chkAutoLoad.CheckedChanged += new System.EventHandler(this.ChkAutoLoad_CheckedChanged);
            // 
            // chkEnableGyro
            // 
            this.chkEnableGyro.AutoSize = true;
            this.chkEnableGyro.ForeColor = System.Drawing.Color.White;
            this.chkEnableGyro.Location = new System.Drawing.Point(200, 655);
            this.chkEnableGyro.Name = "chkEnableGyro";
            this.chkEnableGyro.Size = new System.Drawing.Size(136, 19);
            this.chkEnableGyro.TabIndex = 17;
            this.chkEnableGyro.Text = "Enable Gyro Aiming (In Development)";
            this.chkEnableGyro.UseVisualStyleBackColor = true;
            this.chkEnableGyro.CheckedChanged += new System.EventHandler(this.ChkEnableGyro_CheckedChanged);
            // 
            // tabControlPlayers
            // 
            this.tabControlPlayers.Controls.Add(this.tabPage1);
            this.tabControlPlayers.Controls.Add(this.tabPage2);
            this.tabControlPlayers.Controls.Add(this.tabPage3);
            this.tabControlPlayers.Controls.Add(this.tabPage4);
            this.tabControlPlayers.Location = new System.Drawing.Point(10, 200);
            this.tabControlPlayers.Name = "tabControlPlayers";
            this.tabControlPlayers.SelectedIndex = 0;
            this.tabControlPlayers.Size = new System.Drawing.Size(540, 30);
            this.tabControlPlayers.TabIndex = 18;
            this.tabControlPlayers.SelectedIndexChanged += new System.EventHandler(this.TabControlPlayers_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(532, 2);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Player 1";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(532, 2);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Player 2";
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.tabPage3.Location = new System.Drawing.Point(4, 24);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(532, 2);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Player 3";
            // 
            // tabPage4
            // 
            this.tabPage4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.tabPage4.Location = new System.Drawing.Point(4, 24);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(532, 2);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Player 4";
            // 
            // panelMappingDisplay
            // 
            this.panelMappingDisplay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panelMappingDisplay.Location = new System.Drawing.Point(10, 235);
            this.panelMappingDisplay.Name = "panelMappingDisplay";
            this.panelMappingDisplay.Size = new System.Drawing.Size(540, 410);
            this.panelMappingDisplay.TabIndex = 19;
            // 
            // lblAssignStatus
            // 
            this.lblAssignStatus.AutoSize = false;
            this.lblAssignStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.lblAssignStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAssignStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblAssignStatus.ForeColor = System.Drawing.Color.Orange;
            this.lblAssignStatus.Location = new System.Drawing.Point(100, 350);
            this.lblAssignStatus.Name = "lblAssignStatus";
            this.lblAssignStatus.Size = new System.Drawing.Size(360, 100);
            this.lblAssignStatus.TabIndex = 19;
            this.lblAssignStatus.Text = "⏱ Press any button";
            this.lblAssignStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblAssignStatus.Visible = false;
            // 
            // comboActionSelector
            // 
            this.comboActionSelector.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.comboActionSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboActionSelector.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboActionSelector.ForeColor = System.Drawing.Color.White;
            this.comboActionSelector.FormattingEnabled = true;
            this.comboActionSelector.Location = new System.Drawing.Point(150, 460);
            this.comboActionSelector.Name = "comboActionSelector";
            this.comboActionSelector.Size = new System.Drawing.Size(260, 23);
            this.comboActionSelector.TabIndex = 20;
            this.comboActionSelector.Visible = false;
            // 
            // btnConfirmAssign
            // 
            this.btnConfirmAssign.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this.btnConfirmAssign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmAssign.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnConfirmAssign.ForeColor = System.Drawing.Color.White;
            this.btnConfirmAssign.Location = new System.Drawing.Point(150, 495);
            this.btnConfirmAssign.Name = "btnConfirmAssign";
            this.btnConfirmAssign.Size = new System.Drawing.Size(120, 30);
            this.btnConfirmAssign.TabIndex = 21;
            this.btnConfirmAssign.Text = "✓ Confirm";
            this.btnConfirmAssign.UseVisualStyleBackColor = false;
            this.btnConfirmAssign.Visible = false;
            this.btnConfirmAssign.Click += new System.EventHandler(this.BtnConfirmAssign_Click);
            // 
            // btnCancelAssign
            // 
            this.btnCancelAssign.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnCancelAssign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelAssign.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnCancelAssign.ForeColor = System.Drawing.Color.White;
            this.btnCancelAssign.Location = new System.Drawing.Point(290, 495);
            this.btnCancelAssign.Name = "btnCancelAssign";
            this.btnCancelAssign.Size = new System.Drawing.Size(120, 30);
            this.btnCancelAssign.TabIndex = 22;
            this.btnCancelAssign.Text = "✖ Cancel";
            this.btnCancelAssign.UseVisualStyleBackColor = false;
            this.btnCancelAssign.Visible = false;
            this.btnCancelAssign.Click += new System.EventHandler(this.BtnCancelAssign_Click);
            // 
            // btnBack
            // 
            this.btnBack = new System.Windows.Forms.Button();
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(10, 730);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(80, 30);
            this.btnBack.TabIndex = 23;
            this.btnBack.Text = "⬅ Back";
            this.btnBack.UseVisualStyleBackColor = false;
            // 
            // MappingControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnCancelAssign);
            this.Controls.Add(this.btnConfirmAssign);
            this.Controls.Add(this.comboActionSelector);
            this.Controls.Add(this.lblAssignStatus);
            this.Controls.Add(this.panelMappingDisplay);
            this.Controls.Add(this.btnGamePadMapping);
            this.Controls.Add(this.tabControlPlayers);
            this.Controls.Add(this.chkEnableGyro);
            this.Controls.Add(this.chkAutoLoad);
            this.Controls.Add(this.comboBoxProfiles);
            this.Controls.Add(this.comboBoxSubfolders);
            this.Controls.Add(this.txtProfileName);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnHotkeys);
            this.Controls.Add(this.btnAssignMode);
            this.Controls.Add(this.btnDeleteProfile);
            this.Controls.Add(this.btnNewFolder);
            this.Controls.Add(this.btnSelectExe);
            this.Controls.Add(this.lblQuickMappings);
            this.Controls.Add(this.lblLoadProfile);
            this.Controls.Add(this.lblSubfolder);
            this.Controls.Add(this.lblProfileName);
            this.Controls.Add(this.lblLinkedExe);
            this.Controls.Add(this.lblCurrentGame);
            this.Name = "MappingControl";
            this.Size = new System.Drawing.Size(560, 780);
            this.tabControlPlayers.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCurrentGame;
        private System.Windows.Forms.Label lblLinkedExe;
        private System.Windows.Forms.Label lblProfileName;
        private System.Windows.Forms.Label lblSubfolder;
        private System.Windows.Forms.Label lblLoadProfile;
        private System.Windows.Forms.Label lblQuickMappings;
        private System.Windows.Forms.Button btnSelectExe;
        private System.Windows.Forms.Button btnNewFolder;
        private System.Windows.Forms.Button btnDeleteProfile;
        private System.Windows.Forms.Button btnAssignMode;
        private System.Windows.Forms.Button btnHotkeys;
        private System.Windows.Forms.Button btnGamePadMapping;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.ComboBox comboBoxSubfolders;
        private System.Windows.Forms.ComboBox comboBoxProfiles;
        private System.Windows.Forms.CheckBox chkAutoLoad;
        private System.Windows.Forms.CheckBox chkEnableGyro;
        private System.Windows.Forms.Panel panelMappingDisplay;
        private System.Windows.Forms.Label lblAssignStatus;
        private System.Windows.Forms.ComboBox comboActionSelector;
        private System.Windows.Forms.Button btnConfirmAssign;
        private System.Windows.Forms.Button btnCancelAssign;
        private System.Windows.Forms.TabControl tabControlPlayers;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;

        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        
        public System.Windows.Forms.Button btnBack;
    }
}
