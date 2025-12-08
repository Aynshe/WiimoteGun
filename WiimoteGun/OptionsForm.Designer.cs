
namespace WiimoteGun
{
    partial class OptionsForm
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.cbStartWithWindows = new System.Windows.Forms.CheckBox();
            this.rbBoth = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn4PlayersInfo = new System.Windows.Forms.Button();
            this.chk4Players = new System.Windows.Forms.CheckBox();
            this.chkSharedKeyboard = new System.Windows.Forms.CheckBox();
            this.chkNotifications = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rbBlueTooth = new System.Windows.Forms.RadioButton();
            this.rbDolphinbar = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cboLEDLayout = new System.Windows.Forms.ComboBox();
            this.lblLEDLayout = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.btnInstallPlayer1 = new System.Windows.Forms.Button();
            this.btnUninstallPlayer1 = new System.Windows.Forms.Button();
            this.btnInstallPlayer2 = new System.Windows.Forms.Button();
            this.btnUninstallPlayer2 = new System.Windows.Forms.Button();
            this.btnInstallPlayer3 = new System.Windows.Forms.Button();
            this.btnUninstallPlayer3 = new System.Windows.Forms.Button();
            this.btnInstallPlayer4 = new System.Windows.Forms.Button();
            this.btnUninstallPlayer4= new System.Windows.Forms.Button();
            this.chkAutoLockVMulti = new System.Windows.Forms.CheckBox();
            this.chkPermissiveCalibration = new System.Windows.Forms.CheckBox();
            
            // Gesture Controls Init
            this.grpGestures = new System.Windows.Forms.GroupBox();
            this.chkOffScreenReload = new System.Windows.Forms.CheckBox();
            this.chkGrenadeGesture = new System.Windows.Forms.CheckBox();
            this.grpShakeReload = new System.Windows.Forms.GroupBox();
            this.chkEnableShake = new System.Windows.Forms.CheckBox();
            this.rbShakeWiimote = new System.Windows.Forms.RadioButton();
            this.rbShakeNunchuk = new System.Windows.Forms.RadioButton();
            this.lblShakeSensitivity = new System.Windows.Forms.Label();
            this.cboShakeSensitivity = new System.Windows.Forms.ComboBox();
            this.cboOffScreenMode = new System.Windows.Forms.ComboBox();
            this.rbGrenadeWiimote = new System.Windows.Forms.RadioButton();
            this.rbGrenadeNunchuk = new System.Windows.Forms.RadioButton();
            
            // Mouse Mode Init
            this.grpMouseMode = new System.Windows.Forms.GroupBox();
            this.rbMouseSendInput = new System.Windows.Forms.RadioButton();
            this.rbMouseRawInput = new System.Windows.Forms.RadioButton();
            this.lblMouseModeWarning = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.grpGestures.SuspendLayout();
            this.grpShakeReload.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(324, 910);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 26);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(243, 910);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 26);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Simulate mouse on monitor";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numericUpDown1.Location = new System.Drawing.Point(264, 28);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(105, 20);
            this.numericUpDown1.TabIndex = 3;
            this.numericUpDown1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // cbStartWithWindows
            // 
            this.cbStartWithWindows.AutoSize = true;
            this.cbStartWithWindows.Location = new System.Drawing.Point(13, 30);
            this.cbStartWithWindows.Name = "cbStartWithWindows";
            this.cbStartWithWindows.Size = new System.Drawing.Size(117, 17);
            this.cbStartWithWindows.TabIndex = 4;
            this.cbStartWithWindows.Text = "Start with Windows";
            this.cbStartWithWindows.UseVisualStyleBackColor = true;
            // 
            // rbBoth
            // 
            this.rbBoth.AutoSize = true;
            this.rbBoth.Checked = true;
            this.rbBoth.Location = new System.Drawing.Point(13, 29);
            this.rbBoth.Name = "rbBoth";
            this.rbBoth.Size = new System.Drawing.Size(145, 17);
            this.rbBoth.TabIndex = 5;
            this.rbBoth.TabStop = true;
            this.rbBoth.Text = "Dolphinbar and Bluetooth";
            this.rbBoth.UseVisualStyleBackColor = true;
            // 
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.chkSharedKeyboard);
            this.groupBox1.Controls.Add(this.btn4PlayersInfo);
            this.groupBox1.Controls.Add(this.chk4Players);
            this.groupBox1.Controls.Add(this.chkNotifications);
            this.groupBox1.Controls.Add(this.cbStartWithWindows);
            this.groupBox1.Controls.Add(this.chkNotifications);
            this.groupBox1.Controls.Add(this.cbStartWithWindows);
            this.groupBox1.Location = new System.Drawing.Point(12, 534);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(384, 130);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Misc options";
            // 
            // chkNotifications
            // 
            this.chkNotifications.AutoSize = true;
            this.chkNotifications.Location = new System.Drawing.Point(13, 53);
            this.chkNotifications.Name = "chkNotifications";
            this.chkNotifications.Size = new System.Drawing.Size(112, 17);
            this.chkNotifications.TabIndex = 5;
            this.chkNotifications.Text = "Show notifications";
            this.chkNotifications.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.rbBlueTooth);
            this.groupBox2.Controls.Add(this.rbDolphinbar);
            this.groupBox2.Controls.Add(this.rbBoth);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(384, 108);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Wiimote detection";
            // 
            // grpMouseMode
            // 
            this.grpMouseMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpMouseMode.Controls.Add(this.lblMouseModeWarning);
            this.grpMouseMode.Controls.Add(this.rbMouseRawInput);
            this.grpMouseMode.Controls.Add(this.rbMouseSendInput);
            this.grpMouseMode.Location = new System.Drawing.Point(12, 126);
            this.grpMouseMode.Name = "grpMouseMode";
            this.grpMouseMode.Size = new System.Drawing.Size(384, 95);
            this.grpMouseMode.TabIndex = 8;
            this.grpMouseMode.TabStop = false;
            this.grpMouseMode.Text = "Mouse Activation Mode";
            // 
            // rbMouseSendInput
            // 
            this.rbMouseSendInput.AutoSize = true;
            this.rbMouseSendInput.Location = new System.Drawing.Point(13, 25);
            this.rbMouseSendInput.Name = "rbMouseSendInput";
            this.rbMouseSendInput.Size = new System.Drawing.Size(190, 17);
            this.rbMouseSendInput.TabIndex = 0;
            this.rbMouseSendInput.Text = "SendInput (Single Player - Legacy)";
            this.rbMouseSendInput.UseVisualStyleBackColor = true;
            this.rbMouseSendInput.CheckedChanged += new System.EventHandler(this.rbMouseSendInput_CheckedChanged);
            // 
            // rbMouseRawInput
            // 
            this.rbMouseRawInput.AutoSize = true;
            this.rbMouseRawInput.Checked = true;
            this.rbMouseRawInput.Location = new System.Drawing.Point(13, 48);
            this.rbMouseRawInput.Name = "rbMouseRawInput";
            this.rbMouseRawInput.Size = new System.Drawing.Size(220, 17);
            this.rbMouseRawInput.TabIndex = 1;
            this.rbMouseRawInput.TabStop = true;
            this.rbMouseRawInput.Text = "RawInput/Interception (Multi-Player)";
            this.rbMouseRawInput.UseVisualStyleBackColor = true;
            // 
            // lblMouseModeWarning
            // 
            this.lblMouseModeWarning.AutoSize = true;
            this.lblMouseModeWarning.ForeColor = System.Drawing.Color.OrangeRed;
            this.lblMouseModeWarning.Location = new System.Drawing.Point(30, 70);
            this.lblMouseModeWarning.Name = "lblMouseModeWarning";
            this.lblMouseModeWarning.Size = new System.Drawing.Size(180, 13);
            this.lblMouseModeWarning.TabIndex = 2;
            this.lblMouseModeWarning.Text = "⚠ Only Player 1 active in this mode";
            this.lblMouseModeWarning.Visible = false;
            // 
            // rbBlueTooth
            // 
            this.rbBlueTooth.AutoSize = true;
            this.rbBlueTooth.Location = new System.Drawing.Point(13, 75);
            this.rbBlueTooth.Name = "rbBlueTooth";
            this.rbBlueTooth.Size = new System.Drawing.Size(70, 17);
            this.rbBlueTooth.TabIndex = 7;
            this.rbBlueTooth.Text = "Bluetooth";
            this.rbBlueTooth.UseVisualStyleBackColor = true;
            // 
            // rbDolphinbar
            // 
            this.rbDolphinbar.AutoSize = true;
            this.rbDolphinbar.Location = new System.Drawing.Point(13, 52);
            this.rbDolphinbar.Name = "rbDolphinbar";
            this.rbDolphinbar.Size = new System.Drawing.Size(76, 17);
            this.rbDolphinbar.TabIndex = 6;
            this.rbDolphinbar.Text = "Dolphinbar";
            this.rbDolphinbar.UseVisualStyleBackColor = true;
            // 
            // chk4Players
            // 
            this.chk4Players.AutoSize = true;
            this.chk4Players.Location = new System.Drawing.Point(13, 76);
            this.chk4Players.Name = "chk4Players";
            this.chk4Players.Size = new System.Drawing.Size(200, 17);
            this.chk4Players.TabIndex = 6;
            this.chk4Players.Text = "Enable 4 Players (Experimental)";
            this.chk4Players.UseVisualStyleBackColor = true;
            // 
            // btn4PlayersInfo
            // 
            this.btn4PlayersInfo.Location = new System.Drawing.Point(219, 72);
            this.btn4PlayersInfo.Name = "btn4PlayersInfo";
            this.btn4PlayersInfo.Size = new System.Drawing.Size(24, 24);
            this.btn4PlayersInfo.TabIndex = 7;
            this.btn4PlayersInfo.Text = "?";
            this.btn4PlayersInfo.UseVisualStyleBackColor = true;
            this.btn4PlayersInfo.Click += new System.EventHandler(this.btn4PlayersInfo_Click);
            // 
            // chkSharedKeyboard
            // 
            this.chkSharedKeyboard.AutoSize = true;
            this.chkSharedKeyboard.Location = new System.Drawing.Point(13, 102);
            this.chkSharedKeyboard.Name = "chkSharedKeyboard";
            this.chkSharedKeyboard.Size = new System.Drawing.Size(235, 17);
            this.chkSharedKeyboard.TabIndex = 8;
            this.chkSharedKeyboard.Text = "Use Shared Keyboard (Recommended)";
            this.chkSharedKeyboard.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.cboLEDLayout);
            this.groupBox3.Controls.Add(this.lblLEDLayout);
            this.groupBox3.Controls.Add(this.trackBar1);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.chkPermissiveCalibration);
            this.groupBox3.Controls.Add(this.numericUpDown1);
            this.groupBox3.Location = new System.Drawing.Point(12, 227);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(384, 160);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Mouse emulation ( Restart required )";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "IR Sensitivity";
            // 
            // trackBar1
            // 
            this.trackBar1.AutoSize = false;
            this.trackBar1.Location = new System.Drawing.Point(257, 59);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(119, 30);
            this.trackBar1.TabIndex = 5;
            // 
            // lblLEDLayout
            // 
            this.lblLEDLayout.AutoSize = true;
            this.lblLEDLayout.Location = new System.Drawing.Point(10, 97);
            this.lblLEDLayout.Name = "lblLEDLayout";
            this.lblLEDLayout.Size = new System.Drawing.Size(65, 13);
            this.lblLEDLayout.TabIndex = 6;
            this.lblLEDLayout.Text = "LED Layout";
            // 
            // cboLEDLayout
            // 
            this.cboLEDLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cboLEDLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLEDLayout.FormattingEnabled = true;
            this.cboLEDLayout.Items.AddRange(new object[] {
            "Wiimote Bar (Standard)",
            "Gun4IR Diamond",
            "2-Wiimote Bar (Top/Bottom)",
            "4 Corner LEDs"});
            this.cboLEDLayout.Location = new System.Drawing.Point(150, 94);
            this.cboLEDLayout.Name = "cboLEDLayout";
            this.cboLEDLayout.Size = new System.Drawing.Size(226, 21);
            this.cboLEDLayout.TabIndex = 7;
            // 
            // chkPermissiveCalibration
            // 
            this.chkPermissiveCalibration.AutoSize = true;
            this.chkPermissiveCalibration.Location = new System.Drawing.Point(150, 121);
            this.chkPermissiveCalibration.Name = "chkPermissiveCalibration";
            this.chkPermissiveCalibration.Size = new System.Drawing.Size(220, 17);
            this.chkPermissiveCalibration.TabIndex = 8;
            this.chkPermissiveCalibration.Text = "Permissive Calibration (Large Screens)";
            this.chkPermissiveCalibration.UseVisualStyleBackColor = true;
            // 
            // grpGestures
            // 
            this.grpGestures.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGestures.Controls.Add(this.cboOffScreenMode);
            this.grpGestures.Controls.Add(this.chkOffScreenReload);
            this.grpGestures.Controls.Add(this.rbGrenadeNunchuk);
            this.grpGestures.Controls.Add(this.rbGrenadeWiimote);
            this.grpGestures.Controls.Add(this.chkGrenadeGesture);
            this.grpGestures.Controls.Add(this.grpShakeReload);
            this.grpGestures.Location = new System.Drawing.Point(12, 393);
            this.grpGestures.Name = "grpGestures";
            this.grpGestures.Size = new System.Drawing.Size(384, 135);
            this.grpGestures.TabIndex = 9;
            this.grpGestures.TabStop = false;
            this.grpGestures.Text = "Gestures & Reload";
            // 
            // chkOffScreenReload
            // 
            this.chkOffScreenReload.AutoSize = true;
            this.chkOffScreenReload.Location = new System.Drawing.Point(13, 20);
            this.chkOffScreenReload.Name = "chkOffScreenReload";
            this.chkOffScreenReload.Size = new System.Drawing.Size(200, 17);
            this.chkOffScreenReload.TabIndex = 0;
            this.chkOffScreenReload.Text = "Off-screen Reload (Right Click)";
            this.chkOffScreenReload.UseVisualStyleBackColor = true;
            // 
            // cboOffScreenMode
            // 
            this.cboOffScreenMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOffScreenMode.FormattingEnabled = true;
            this.cboOffScreenMode.Items.AddRange(new object[] {
            "On Click",
            "Automatic"});
            this.cboOffScreenMode.Location = new System.Drawing.Point(220, 18);
            this.cboOffScreenMode.Name = "cboOffScreenMode";
            this.cboOffScreenMode.Size = new System.Drawing.Size(100, 21);
            this.cboOffScreenMode.TabIndex = 3;
            // 
            // chkGrenadeGesture
            // 
            this.chkGrenadeGesture.AutoSize = true;
            this.chkGrenadeGesture.Location = new System.Drawing.Point(13, 43);
            this.chkGrenadeGesture.Name = "chkGrenadeGesture";
            this.chkGrenadeGesture.Size = new System.Drawing.Size(70, 17);
            this.chkGrenadeGesture.TabIndex = 1;
            this.chkGrenadeGesture.Text = "Grenade:";
            this.chkGrenadeGesture.UseVisualStyleBackColor = true;
            // 
            // rbGrenadeWiimote
            // 
            this.rbGrenadeWiimote.AutoSize = true;
            this.rbGrenadeWiimote.Checked = true;
            this.rbGrenadeWiimote.Location = new System.Drawing.Point(90, 42);
            this.rbGrenadeWiimote.Name = "rbGrenadeWiimote";
            this.rbGrenadeWiimote.Size = new System.Drawing.Size(80, 17);
            this.rbGrenadeWiimote.TabIndex = 4;
            this.rbGrenadeWiimote.TabStop = true;
            this.rbGrenadeWiimote.Text = "Wiimote";
            this.rbGrenadeWiimote.UseVisualStyleBackColor = true;
            // 
            // rbGrenadeNunchuk
            // 
            this.rbGrenadeNunchuk.AutoSize = true;
            this.rbGrenadeNunchuk.Location = new System.Drawing.Point(180, 42);
            this.rbGrenadeNunchuk.Name = "rbGrenadeNunchuk";
            this.rbGrenadeNunchuk.Size = new System.Drawing.Size(80, 17);
            this.rbGrenadeNunchuk.TabIndex = 5;
            this.rbGrenadeNunchuk.Text = "Nunchuk";
            this.rbGrenadeNunchuk.UseVisualStyleBackColor = true;
            // 
            // grpShakeReload
            // 
            this.grpShakeReload.Controls.Add(this.chkEnableShake);
            this.grpShakeReload.Controls.Add(this.rbShakeWiimote);
            this.grpShakeReload.Controls.Add(this.rbShakeNunchuk);
            this.grpShakeReload.Controls.Add(this.lblShakeSensitivity);
            this.grpShakeReload.Controls.Add(this.cboShakeSensitivity);
            this.grpShakeReload.Location = new System.Drawing.Point(10, 66);
            this.grpShakeReload.Name = "grpShakeReload";
            this.grpShakeReload.Size = new System.Drawing.Size(365, 60);
            this.grpShakeReload.TabIndex = 2;
            this.grpShakeReload.TabStop = false;
            this.grpShakeReload.Text = "Shake Reload (Right Click)";
            // 
            // chkEnableShake
            // 
            this.chkEnableShake.AutoSize = true;
            this.chkEnableShake.Location = new System.Drawing.Point(6, 20);
            this.chkEnableShake.Name = "chkEnableShake";
            this.chkEnableShake.Size = new System.Drawing.Size(65, 17);
            this.chkEnableShake.TabIndex = 0;
            this.chkEnableShake.Text = "Enable";
            this.chkEnableShake.UseVisualStyleBackColor = true;
            // 
            // rbShakeWiimote
            // 
            this.rbShakeWiimote.AutoSize = true;
            this.rbShakeWiimote.Checked = true;
            this.rbShakeWiimote.Location = new System.Drawing.Point(70, 19);
            this.rbShakeWiimote.Name = "rbShakeWiimote";
            this.rbShakeWiimote.Size = new System.Drawing.Size(80, 17);
            this.rbShakeWiimote.TabIndex = 1;
            this.rbShakeWiimote.TabStop = true;
            this.rbShakeWiimote.Text = "Wiimote";
            this.rbShakeWiimote.UseVisualStyleBackColor = true;
            // 
            // rbShakeNunchuk
            // 
            this.rbShakeNunchuk.AutoSize = true;
            this.rbShakeNunchuk.Location = new System.Drawing.Point(150, 19);
            this.rbShakeNunchuk.Name = "rbShakeNunchuk";
            this.rbShakeNunchuk.Size = new System.Drawing.Size(80, 17);
            this.rbShakeNunchuk.TabIndex = 2;
            this.rbShakeNunchuk.Text = "Nunchuk";
            this.rbShakeNunchuk.UseVisualStyleBackColor = true;
            // 
            // lblShakeSensitivity
            // 
            this.lblShakeSensitivity.AutoSize = true;
            this.lblShakeSensitivity.Location = new System.Drawing.Point(230, 21);
            this.lblShakeSensitivity.Name = "lblShakeSensitivity";
            this.lblShakeSensitivity.Size = new System.Drawing.Size(57, 13);
            this.lblShakeSensitivity.TabIndex = 3;
            this.lblShakeSensitivity.Text = "Sensitivity:";
            // 
            // cboShakeSensitivity
            // 
            this.cboShakeSensitivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboShakeSensitivity.FormattingEnabled = true;
            this.cboShakeSensitivity.Items.AddRange(new object[] { "Low", "Medium", "High" });
            this.cboShakeSensitivity.Location = new System.Drawing.Point(290, 18);
            this.cboShakeSensitivity.Name = "cboShakeSensitivity";
            this.cboShakeSensitivity.Size = new System.Drawing.Size(65, 21);
            this.cboShakeSensitivity.TabIndex = 4;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkAutoLockVMulti);
            this.groupBox4.Controls.Add(this.btnInstallPlayer1);
            this.groupBox4.Controls.Add(this.btnUninstallPlayer1);
            this.groupBox4.Controls.Add(this.btnInstallPlayer2);
            this.groupBox4.Controls.Add(this.btnUninstallPlayer2);
            this.groupBox4.Controls.Add(this.btnInstallPlayer3);
            this.groupBox4.Controls.Add(this.btnUninstallPlayer3);
            this.groupBox4.Controls.Add(this.btnInstallPlayer4);
            this.groupBox4.Controls.Add(this.btnUninstallPlayer4);
            this.groupBox4.Controls.Add(this.btnInstall);
            this.groupBox4.Controls.Add(this.btnUninstall);
            this.groupBox4.Location = new System.Drawing.Point(12, 670);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(384, 210);
            this.groupBox4.TabIndex = 10;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Virtual HID Driver";
            // 
            // chkAutoLockVMulti
            // 
            this.chkAutoLockVMulti.AutoSize = true;
            this.chkAutoLockVMulti.Checked = true;
            this.chkAutoLockVMulti.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoLockVMulti.Location = new System.Drawing.Point(13, 180);
            this.chkAutoLockVMulti.Name = "chkAutoLockVMulti";
            this.chkAutoLockVMulti.Size = new System.Drawing.Size(270, 17);
            this.chkAutoLockVMulti.TabIndex = 10;
            this.chkAutoLockVMulti.Text = "Auto-Lock VMulti to Player 1 \u0026 2 \u0026 3 \u0026 4";
            this.chkAutoLockVMulti.UseVisualStyleBackColor = true;
            // 
            // btnInstall
            // 
            this.btnInstall.Location = new System.Drawing.Point(13, 20);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(180, 25);
            this.btnInstall.TabIndex = 0;
            this.btnInstall.Text = "📦 Install Interception Driver";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // btnUninstall
            // 
            this.btnUninstall.Location = new System.Drawing.Point(199, 20);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(170, 25);
            this.btnUninstall.TabIndex = 1;
            this.btnUninstall.Text = "❌ Uninstall Interception";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            // 
            // btnInstallPlayer1
            // 
            this.btnInstallPlayer1.Location = new System.Drawing.Point(13, 55);
            this.btnInstallPlayer1.Name = "btnInstallPlayer1";
            this.btnInstallPlayer1.Size = new System.Drawing.Size(120, 25);
            this.btnInstallPlayer1.TabIndex = 2;
            this.btnInstallPlayer1.Text = "➕ Add Player 1";
            this.btnInstallPlayer1.UseVisualStyleBackColor = true;
            this.btnInstallPlayer1.Click += new System.EventHandler(this.btnInstallPlayer1_Click);
            // 
            // btnUninstallPlayer1
            // 
            this.btnUninstallPlayer1.Location = new System.Drawing.Point(138, 55);
            this.btnUninstallPlayer1.Name = "btnUninstallPlayer1";
            this.btnUninstallPlayer1.Size = new System.Drawing.Size(55, 25);
            this.btnUninstallPlayer1.TabIndex = 3;
            this.btnUninstallPlayer1.Text = "❌";
            this.btnUninstallPlayer1.UseVisualStyleBackColor = true;
            this.btnUninstallPlayer1.Click += new System.EventHandler(this.btnUninstallPlayer1_Click);
            // 
            // btnInstallPlayer2
            // 
            this.btnInstallPlayer2.Location = new System.Drawing.Point(199, 55);
            this.btnInstallPlayer2.Name = "btnInstallPlayer2";
            this.btnInstallPlayer2.Size = new System.Drawing.Size(120, 25);
            this.btnInstallPlayer2.TabIndex = 4;
            this.btnInstallPlayer2.Text = "➕ Add Player 2";
            this.btnInstallPlayer2.UseVisualStyleBackColor = true;
            this.btnInstallPlayer2.Click += new System.EventHandler(this.btnInstallPlayer2_Click);
            // 
            // btnUninstallPlayer2
            // 
            this.btnUninstallPlayer2.Location = new System.Drawing.Point(324, 55);
            this.btnUninstallPlayer2.Name = "btnUninstallPlayer2";
            this.btnUninstallPlayer2.Size = new System.Drawing.Size(45, 25);
            this.btnUninstallPlayer2.TabIndex = 5;
            this.btnUninstallPlayer2.Text = "❌";
            this.btnUninstallPlayer2.UseVisualStyleBackColor = true;
            this.btnUninstallPlayer2.Click += new System.EventHandler(this.btnUninstallPlayer2_Click);
            // 
            // btnInstallPlayer3
            // 
            this.btnInstallPlayer3.Location = new System.Drawing.Point(13, 86);
            this.btnInstallPlayer3.Name = "btnInstallPlayer3";
            this.btnInstallPlayer3.Size = new System.Drawing.Size(120, 25);
            this.btnInstallPlayer3.TabIndex = 6;
            this.btnInstallPlayer3.Text = "➕ Add Player 3";
            this.btnInstallPlayer3.UseVisualStyleBackColor = true;
            this.btnInstallPlayer3.Click += new System.EventHandler(this.btnInstallPlayer3_Click);
            // 
            // btnUninstallPlayer3
            // 
            this.btnUninstallPlayer3.Location = new System.Drawing.Point(138, 86);
            this.btnUninstallPlayer3.Name = "btnUninstallPlayer3";
            this.btnUninstallPlayer3.Size = new System.Drawing.Size(55, 25);
            this.btnUninstallPlayer3.TabIndex = 7;
            this.btnUninstallPlayer3.Text = "❌";
            this.btnUninstallPlayer3.UseVisualStyleBackColor = true;
            this.btnUninstallPlayer3.Click += new System.EventHandler(this.btnUninstallPlayer3_Click);
            // 
            // btnInstallPlayer4
            // 
            this.btnInstallPlayer4.Location = new System.Drawing.Point(199, 86);
            this.btnInstallPlayer4.Name = "btnInstallPlayer4";
            this.btnInstallPlayer4.Size = new System.Drawing.Size(120, 25);
            this.btnInstallPlayer4.TabIndex = 8;
            this.btnInstallPlayer4.Text = "➕ Add Player 4";
            this.btnInstallPlayer4.UseVisualStyleBackColor = true;
            this.btnInstallPlayer4.Click += new System.EventHandler(this.btnInstallPlayer4_Click);
            // 
            // btnUninstallPlayer4
            // 
            this.btnUninstallPlayer4.Location = new System.Drawing.Point(324, 86);
            this.btnUninstallPlayer4.Name = "btnUninstallPlayer4";
            this.btnUninstallPlayer4.Size = new System.Drawing.Size(45, 25);
            this.btnUninstallPlayer4.TabIndex = 9;
            this.btnUninstallPlayer4.Text = "❌";
            this.btnUninstallPlayer4.UseVisualStyleBackColor = true;
            this.btnUninstallPlayer4.Click += new System.EventHandler(this.btnUninstallPlayer4_Click);
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(411, 960); // Increased height to fit buttons below driver group
            this.Controls.Add(this.grpGestures);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.grpGestures);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.grpMouseMode);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WiimoteGun Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.CheckBox cbStartWithWindows;
        private System.Windows.Forms.RadioButton rbBoth;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbBlueTooth;
        private System.Windows.Forms.RadioButton rbDolphinbar;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox chkNotifications;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.Button btnInstallPlayer1;
        private System.Windows.Forms.Button btnUninstallPlayer1;
        private System.Windows.Forms.Button btnInstallPlayer2;
        private System.Windows.Forms.Button btnUninstallPlayer2;
        private System.Windows.Forms.Button btnInstallPlayer3;
        private System.Windows.Forms.Button btnUninstallPlayer3;
        private System.Windows.Forms.Button btnInstallPlayer4;
        private System.Windows.Forms.Button btnUninstallPlayer4;
        private System.Windows.Forms.CheckBox chk4Players;
        private System.Windows.Forms.Button btn4PlayersInfo;
        private System.Windows.Forms.ComboBox cboLEDLayout;
        private System.Windows.Forms.Label lblLEDLayout;
        private System.Windows.Forms.CheckBox chkSharedKeyboard;
        
        // Gesture Controls
        private System.Windows.Forms.GroupBox grpGestures;
        private System.Windows.Forms.CheckBox chkOffScreenReload;
        private System.Windows.Forms.CheckBox chkGrenadeGesture;
        private System.Windows.Forms.GroupBox grpShakeReload;
        private System.Windows.Forms.CheckBox chkEnableShake;
        private System.Windows.Forms.RadioButton rbShakeNunchuk;
        private System.Windows.Forms.RadioButton rbShakeWiimote;
        private System.Windows.Forms.ComboBox cboShakeSensitivity;
        private System.Windows.Forms.ComboBox cboOffScreenMode;
        private System.Windows.Forms.Label lblShakeSensitivity;
        private System.Windows.Forms.RadioButton rbGrenadeWiimote;
        private System.Windows.Forms.RadioButton rbGrenadeNunchuk;
        private System.Windows.Forms.GroupBox grpMouseMode;
        private System.Windows.Forms.RadioButton rbMouseSendInput;
        private System.Windows.Forms.RadioButton rbMouseRawInput;
        private System.Windows.Forms.Label lblMouseModeWarning;
        private System.Windows.Forms.CheckBox chkAutoLockVMulti;
        private System.Windows.Forms.CheckBox chkPermissiveCalibration;
    }
}