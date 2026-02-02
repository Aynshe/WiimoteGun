namespace WiimoteGun.Controls
{
    partial class OptionsControl
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.panelOptionsSidebar = new System.Windows.Forms.Panel();
            this.btnTabGeneral = new System.Windows.Forms.Button();

            this.btnTabDetection = new System.Windows.Forms.Button();
            this.btnTabGestures = new System.Windows.Forms.Button();
            this.btnTabEmulators = new System.Windows.Forms.Button();

            this.btnBack = new System.Windows.Forms.Button();
            this.tabsOptions = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.optShowNotifications = new System.Windows.Forms.CheckBox();
            this.optIRSensitivity = new System.Windows.Forms.NumericUpDown();
            this.lblIRSensitivity = new System.Windows.Forms.Label();
            this.optLEDLayout = new System.Windows.Forms.ComboBox();
            this.lblLEDLayout = new System.Windows.Forms.Label();
            this.optMonitorId = new System.Windows.Forms.NumericUpDown();
            this.lblMonitorId = new System.Windows.Forms.Label();
            this.optMouseMode = new System.Windows.Forms.ComboBox();
            this.lblMouseMode = new System.Windows.Forms.Label();

            this.tabDetection = new System.Windows.Forms.TabPage();
            this.lblDetectionInfo = new System.Windows.Forms.Label();
            this.optDetectBluetooth = new System.Windows.Forms.CheckBox();
            this.optDetectDolphin = new System.Windows.Forms.CheckBox();
            this.tabGestures = new System.Windows.Forms.TabPage();
            this.optShakeSensitivity = new System.Windows.Forms.TrackBar();
            this.lblShakeSensitivity = new System.Windows.Forms.Label();
            this.optEnableGrenadeGesture = new System.Windows.Forms.CheckBox();
            this.optShakeFromNunchuk = new System.Windows.Forms.CheckBox();
            this.optEnableShakeReload = new System.Windows.Forms.CheckBox();
            this.optOffScreenReloadAuto = new System.Windows.Forms.CheckBox();
            this.optEnableOffScreenReload = new System.Windows.Forms.CheckBox();
            this.tabEmulators = new System.Windows.Forms.TabPage();
            this.optRestartOnCemu = new System.Windows.Forms.CheckBox();
            this.optRestartOnDolphin = new System.Windows.Forms.CheckBox();

            this.btnApply = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.panelOptionsSidebar.SuspendLayout();
            this.tabsOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).BeginInit();

            this.tabDetection.SuspendLayout();
            this.tabGestures.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optShakeSensitivity)).BeginInit();
            this.tabEmulators.SuspendLayout();

            this.SuspendLayout();
            // 
            // panelOptionsSidebar
            // 
            this.panelOptionsSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panelOptionsSidebar.Controls.Add(this.btnTabGeneral);

            this.panelOptionsSidebar.Controls.Add(this.btnTabDetection);
            this.panelOptionsSidebar.Controls.Add(this.btnTabGestures);
            this.panelOptionsSidebar.Controls.Add(this.btnTabEmulators);

            this.panelOptionsSidebar.Controls.Add(this.btnBack);
            this.panelOptionsSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelOptionsSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelOptionsSidebar.Name = "panelOptionsSidebar";
            this.panelOptionsSidebar.Size = new System.Drawing.Size(150, 770);
            this.panelOptionsSidebar.TabIndex = 0;
            // 
            // btnTabGeneral
            // 
            this.btnTabGeneral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnTabGeneral.FlatAppearance.BorderSize = 0;
            this.btnTabGeneral.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabGeneral.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTabGeneral.ForeColor = System.Drawing.Color.White;
            this.btnTabGeneral.Location = new System.Drawing.Point(2, 10);
            this.btnTabGeneral.Name = "btnTabGeneral";
            this.btnTabGeneral.Size = new System.Drawing.Size(145, 45);
            this.btnTabGeneral.TabIndex = 0;
            this.btnTabGeneral.Text = "⚙️ General";
            this.btnTabGeneral.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTabGeneral.UseVisualStyleBackColor = false;

            // 
            // btnTabDetection
            // 
            this.btnTabDetection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnTabDetection.FlatAppearance.BorderSize = 0;
            this.btnTabDetection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabDetection.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTabDetection.ForeColor = System.Drawing.Color.White;
            this.btnTabDetection.Location = new System.Drawing.Point(2, 60);
            this.btnTabDetection.Name = "btnTabDetection";
            this.btnTabDetection.Size = new System.Drawing.Size(145, 45);
            this.btnTabDetection.TabIndex = 0;
            this.btnTabDetection.Text = "📡 Detection";
            this.btnTabDetection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTabDetection.UseVisualStyleBackColor = false;
            // 
            // btnTabGestures
            // 
            this.btnTabGestures.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnTabGestures.FlatAppearance.BorderSize = 0;
            this.btnTabGestures.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabGestures.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTabGestures.ForeColor = System.Drawing.Color.White;
            this.lblGesturesDevSeparator = new System.Windows.Forms.Label();
            this.btnTabGestures.Location = new System.Drawing.Point(2, 110);
            this.btnTabGestures.Name = "btnTabGestures";
            this.btnTabGestures.Size = new System.Drawing.Size(145, 45);
            this.btnTabGestures.TabIndex = 0;
            this.btnTabGestures.Text = "🤌 Gestures";
            this.btnTabGestures.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTabGestures.UseVisualStyleBackColor = false;
            // 
            // btnTabEmulators
            // 
            this.btnTabEmulators.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnTabEmulators.FlatAppearance.BorderSize = 0;
            this.btnTabEmulators.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabEmulators.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTabEmulators.ForeColor = System.Drawing.Color.White;
            this.btnTabEmulators.Location = new System.Drawing.Point(2, 160);
            this.btnTabEmulators.Name = "btnTabEmulators";
            this.btnTabEmulators.Size = new System.Drawing.Size(145, 45);
            this.btnTabEmulators.TabIndex = 0;
            this.btnTabEmulators.Text = "🎮 Emulators";
            this.btnTabEmulators.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTabEmulators.UseVisualStyleBackColor = false;

            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(35, 730);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(80, 30);
            this.btnBack.TabIndex = 10;
            this.btnBack.Text = "⬅ Back";
            this.btnBack.UseVisualStyleBackColor = false;
            // 
            // tabsOptions
            // 
            this.tabsOptions.Controls.Add(this.tabGeneral);

            this.tabsOptions.Controls.Add(this.tabDetection);
            this.tabsOptions.Controls.Add(this.tabGestures);
            this.tabsOptions.Controls.Add(this.tabEmulators);

            this.tabsOptions.Location = new System.Drawing.Point(155, 10);
            this.tabsOptions.Name = "tabsOptions";
            this.tabsOptions.SelectedIndex = 0;
            this.tabsOptions.Size = new System.Drawing.Size(400, 600);
            this.tabsOptions.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabsOptions.TabIndex = 1;
            // 
            // tabGeneral
            // 
            this.tabGeneral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabGeneral.Controls.Add(this.optShowNotifications);
            this.tabGeneral.Controls.Add(this.optIRSensitivity);
            this.tabGeneral.Controls.Add(this.lblIRSensitivity);
            this.tabGeneral.Controls.Add(this.optLEDLayout);
            this.tabGeneral.Controls.Add(this.lblLEDLayout);
            this.tabGeneral.Controls.Add(this.optMonitorId);
            this.tabGeneral.Controls.Add(this.lblMonitorId);
            this.tabGeneral.Controls.Add(this.optMouseMode);
            this.tabGeneral.Controls.Add(this.lblMouseMode);
            this.tabGeneral.Location = new System.Drawing.Point(4, 5);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(392, 591);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            // 
            // optShowNotifications
            // 
            this.optShowNotifications.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optShowNotifications.ForeColor = System.Drawing.Color.White;
            this.optShowNotifications.Location = new System.Drawing.Point(20, 220);
            this.optShowNotifications.Name = "optShowNotifications";
            this.optShowNotifications.Size = new System.Drawing.Size(200, 25);
            this.optShowNotifications.TabIndex = 1;
            this.optShowNotifications.Text = "Show Notifications";
            this.optShowNotifications.UseVisualStyleBackColor = true;
            // 
            // optIRSensitivity
            // 
            this.optIRSensitivity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optIRSensitivity.ForeColor = System.Drawing.Color.White;
            this.optIRSensitivity.Location = new System.Drawing.Point(140, 170);
            this.optIRSensitivity.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.optIRSensitivity.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.optIRSensitivity.Name = "optIRSensitivity";
            this.optIRSensitivity.Size = new System.Drawing.Size(100, 25);
            this.optIRSensitivity.TabIndex = 1;
            this.optIRSensitivity.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblIRSensitivity
            // 
            this.lblIRSensitivity.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblIRSensitivity.ForeColor = System.Drawing.Color.White;
            this.lblIRSensitivity.Location = new System.Drawing.Point(10, 170);
            this.lblIRSensitivity.Name = "lblIRSensitivity";
            this.lblIRSensitivity.Size = new System.Drawing.Size(120, 25);
            this.lblIRSensitivity.TabIndex = 0;
            this.lblIRSensitivity.Text = "IR Sensitivity:";
            this.lblIRSensitivity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optLEDLayout
            // 
            this.optLEDLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optLEDLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optLEDLayout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optLEDLayout.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optLEDLayout.ForeColor = System.Drawing.Color.White;
            this.optLEDLayout.FormattingEnabled = true;
            this.optLEDLayout.Items.AddRange(new object[] {
            "Wiimote Bar",
            "Gun4IR Diamond",
            "Two Wiimote Bars",
            "Four Corners"});
            this.optLEDLayout.Location = new System.Drawing.Point(140, 120);
            this.optLEDLayout.Name = "optLEDLayout";
            this.optLEDLayout.Size = new System.Drawing.Size(200, 25);
            this.optLEDLayout.TabIndex = 1;
            // 
            // lblLEDLayout
            // 
            this.lblLEDLayout.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblLEDLayout.ForeColor = System.Drawing.Color.White;
            this.lblLEDLayout.Location = new System.Drawing.Point(10, 120);
            this.lblLEDLayout.Name = "lblLEDLayout";
            this.lblLEDLayout.Size = new System.Drawing.Size(120, 25);
            this.lblLEDLayout.TabIndex = 0;
            this.lblLEDLayout.Text = "LED Layout:";
            this.lblLEDLayout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optMonitorId
            // 
            this.optMonitorId.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optMonitorId.ForeColor = System.Drawing.Color.White;
            this.optMonitorId.Location = new System.Drawing.Point(140, 70);
            this.optMonitorId.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            this.optMonitorId.Name = "optMonitorId";
            this.optMonitorId.Size = new System.Drawing.Size(100, 25);
            this.optMonitorId.TabIndex = 1;
            // 
            // lblMonitorId
            // 
            this.lblMonitorId.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblMonitorId.ForeColor = System.Drawing.Color.White;
            this.lblMonitorId.Location = new System.Drawing.Point(10, 70);
            this.lblMonitorId.Name = "lblMonitorId";
            this.lblMonitorId.Size = new System.Drawing.Size(120, 25);
            this.lblMonitorId.TabIndex = 0;
            this.lblMonitorId.Text = "Monitor ID:";
            this.lblMonitorId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optMouseMode
            // 
            this.optMouseMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optMouseMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optMouseMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optMouseMode.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optMouseMode.ForeColor = System.Drawing.Color.White;
            this.optMouseMode.FormattingEnabled = true;
            this.optMouseMode.Items.AddRange(new object[] {
            "SendInput",
            "RawInput"});
            this.optMouseMode.Location = new System.Drawing.Point(140, 20);
            this.optMouseMode.Name = "optMouseMode";
            this.optMouseMode.Size = new System.Drawing.Size(200, 25);
            this.optMouseMode.TabIndex = 1;
            // 
            // lblMouseMode
            // 
            this.lblMouseMode.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblMouseMode.ForeColor = System.Drawing.Color.White;
            this.lblMouseMode.Location = new System.Drawing.Point(10, 20);
            this.lblMouseMode.Name = "lblMouseMode";
            this.lblMouseMode.Size = new System.Drawing.Size(120, 25);
            this.lblMouseMode.TabIndex = 0;
            this.lblMouseMode.Text = "Mouse Mode:";
            this.lblMouseMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // 
            // tabDetection
            // 
            this.tabDetection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabDetection.Controls.Add(this.lblDetectionInfo);
            this.tabDetection.Controls.Add(this.optDetectBluetooth);
            this.tabDetection.Controls.Add(this.optDetectDolphin);
            this.tabDetection.Location = new System.Drawing.Point(4, 5);
            this.tabDetection.Name = "tabDetection";
            this.tabDetection.Size = new System.Drawing.Size(392, 591);
            this.tabDetection.TabIndex = 2;
            this.tabDetection.Text = "Detection";
            // 
            // lblDetectionInfo
            // 
            this.lblDetectionInfo.ForeColor = System.Drawing.Color.Gray;
            this.lblDetectionInfo.Location = new System.Drawing.Point(20, 140);
            this.lblDetectionInfo.Name = "lblDetectionInfo";
            this.lblDetectionInfo.Size = new System.Drawing.Size(350, 40);
            this.lblDetectionInfo.TabIndex = 0;
            this.lblDetectionInfo.Text = "Restart required after changing connection settings.";
            // 
            // optDetectBluetooth
            // 
            this.optDetectBluetooth.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optDetectBluetooth.ForeColor = System.Drawing.Color.White;
            this.optDetectBluetooth.Location = new System.Drawing.Point(20, 60);
            this.optDetectBluetooth.Name = "optDetectBluetooth";
            this.optDetectBluetooth.Size = new System.Drawing.Size(200, 25);
            this.optDetectBluetooth.TabIndex = 1;
            this.optDetectBluetooth.Text = "Detect Bluetooth";
            this.optDetectBluetooth.UseVisualStyleBackColor = true;
            // 
            // optDetectDolphin
            // 
            this.optDetectDolphin.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optDetectDolphin.ForeColor = System.Drawing.Color.White;
            this.optDetectDolphin.Location = new System.Drawing.Point(20, 20);
            this.optDetectDolphin.Name = "optDetectDolphin";
            this.optDetectDolphin.Size = new System.Drawing.Size(200, 25);
            this.optDetectDolphin.TabIndex = 1;
            this.optDetectDolphin.Text = "Detect DolphinBar";
            this.optDetectDolphin.UseVisualStyleBackColor = true;

            this.optDetectDolphin.UseVisualStyleBackColor = true;
            // 
            // tabGestures
            // 
            this.tabGestures.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabGestures.Controls.Add(this.lblGesturesDevSeparator);
            this.tabGestures.Controls.Add(this.optShakeSensitivity);
            this.tabGestures.Controls.Add(this.lblShakeSensitivity);
            this.tabGestures.Controls.Add(this.optEnableGrenadeGesture);
            this.tabGestures.Controls.Add(this.optShakeFromNunchuk);
            this.tabGestures.Controls.Add(this.optEnableShakeReload);
            this.tabGestures.Controls.Add(this.optOffScreenReloadAuto);
            this.tabGestures.Controls.Add(this.optEnableOffScreenReload);
            this.tabGestures.Location = new System.Drawing.Point(4, 5);
            this.tabGestures.Name = "tabGestures";
            this.tabGestures.Size = new System.Drawing.Size(392, 591);
            this.tabGestures.TabIndex = 3;
            this.tabGestures.Text = "Gestures";
            // 
            // optShakeSensitivity
            // 
            this.optShakeSensitivity.Location = new System.Drawing.Point(140, 150);
            this.optShakeSensitivity.Maximum = 2;
            this.optShakeSensitivity.Name = "optShakeSensitivity";
            this.optShakeSensitivity.Size = new System.Drawing.Size(200, 45);
            this.optShakeSensitivity.TabIndex = 2;
            // 
            // lblShakeSensitivity
            // 
            this.lblShakeSensitivity.ForeColor = System.Drawing.Color.White;
            this.lblShakeSensitivity.Location = new System.Drawing.Point(20, 150);
            this.lblShakeSensitivity.Name = "lblShakeSensitivity";
            this.lblShakeSensitivity.Size = new System.Drawing.Size(120, 25);
            this.lblShakeSensitivity.TabIndex = 0;
            this.lblShakeSensitivity.Text = "Shake Sensitivity:";
            // 
            // optEnableGrenadeGesture
            // 
            this.optEnableGrenadeGesture.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableGrenadeGesture.ForeColor = System.Drawing.Color.White;
            this.optEnableGrenadeGesture.Location = new System.Drawing.Point(20, 240);
            this.optEnableGrenadeGesture.Name = "optEnableGrenadeGesture";
            this.optEnableGrenadeGesture.Size = new System.Drawing.Size(200, 25);
            this.optEnableGrenadeGesture.TabIndex = 1;
            this.optEnableGrenadeGesture.Text = "Grenade Gesture";
            this.optEnableGrenadeGesture.UseVisualStyleBackColor = true;
            // 
            // optShakeFromNunchuk
            // 
            this.optShakeFromNunchuk.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optShakeFromNunchuk.ForeColor = System.Drawing.Color.White;
            this.optShakeFromNunchuk.Location = new System.Drawing.Point(20, 200);
            this.optShakeFromNunchuk.Name = "optShakeFromNunchuk";
            this.optShakeFromNunchuk.Size = new System.Drawing.Size(200, 25);
            this.optShakeFromNunchuk.TabIndex = 1;
            this.optShakeFromNunchuk.Text = "Shake from Nunchuk";
            this.optShakeFromNunchuk.UseVisualStyleBackColor = true;
            // 
            // optEnableShakeReload
            // 
            this.optEnableShakeReload.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableShakeReload.ForeColor = System.Drawing.Color.White;
            this.optEnableShakeReload.Location = new System.Drawing.Point(20, 110);
            this.optEnableShakeReload.Name = "optEnableShakeReload";
            this.optEnableShakeReload.Size = new System.Drawing.Size(200, 25);
            this.optEnableShakeReload.TabIndex = 1;
            this.optEnableShakeReload.Text = "Shake Reload";
            this.optEnableShakeReload.UseVisualStyleBackColor = true;
            // 
            // optOffScreenReloadAuto
            // 
            this.optOffScreenReloadAuto.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optOffScreenReloadAuto.ForeColor = System.Drawing.Color.White;
            this.optOffScreenReloadAuto.Location = new System.Drawing.Point(20, 60);
            this.optOffScreenReloadAuto.Name = "optOffScreenReloadAuto";
            this.optOffScreenReloadAuto.Size = new System.Drawing.Size(200, 25);
            this.optOffScreenReloadAuto.TabIndex = 1;
            this.optOffScreenReloadAuto.Text = "Auto Off-Screen";
            this.optOffScreenReloadAuto.UseVisualStyleBackColor = true;
            // 
            // optEnableOffScreenReload
            // 
            this.optEnableOffScreenReload.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableOffScreenReload.ForeColor = System.Drawing.Color.White;
            this.optEnableOffScreenReload.Location = new System.Drawing.Point(20, 20);
            this.optEnableOffScreenReload.Name = "optEnableOffScreenReload";
            this.optEnableOffScreenReload.Size = new System.Drawing.Size(200, 25);
            this.optEnableOffScreenReload.TabIndex = 1;
            this.optEnableOffScreenReload.Text = "Off-Screen Reload";
            this.optEnableOffScreenReload.UseVisualStyleBackColor = true;
            // 
            // lblGesturesDevSeparator
            // 
            this.lblGesturesDevSeparator.AutoSize = true;
            this.lblGesturesDevSeparator.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblGesturesDevSeparator.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblGesturesDevSeparator.Location = new System.Drawing.Point(20, 95);
            this.lblGesturesDevSeparator.Name = "lblGesturesDevSeparator";
            this.lblGesturesDevSeparator.Size = new System.Drawing.Size(200, 15);
            this.lblGesturesDevSeparator.TabIndex = 0;
            this.lblGesturesDevSeparator.Text = "—— In Development (For Test) ——";
            // 
            // tabEmulators
            // 
            this.tabEmulators.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabEmulators.Controls.Add(this.optRestartOnCemu);
            this.tabEmulators.Controls.Add(this.optRestartOnDolphin);
            this.tabEmulators.Location = new System.Drawing.Point(4, 5);
            this.tabEmulators.Name = "tabEmulators";
            this.tabEmulators.Size = new System.Drawing.Size(392, 591);
            this.tabEmulators.TabIndex = 4;
            this.tabEmulators.Text = "Emulators";
            // 
            // optRestartOnCemu
            // 
            this.optRestartOnCemu.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optRestartOnCemu.ForeColor = System.Drawing.Color.White;
            this.optRestartOnCemu.Location = new System.Drawing.Point(20, 60);
            this.optRestartOnCemu.Name = "optRestartOnCemu";
            this.optRestartOnCemu.Size = new System.Drawing.Size(200, 25);
            this.optRestartOnCemu.TabIndex = 1;
            this.optRestartOnCemu.Text = "Restart on Cemu";
            this.optRestartOnCemu.UseVisualStyleBackColor = true;
            // 
            // optRestartOnDolphin
            // 
            this.optRestartOnDolphin.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optRestartOnDolphin.ForeColor = System.Drawing.Color.White;
            this.optRestartOnDolphin.Location = new System.Drawing.Point(20, 20);
            this.optRestartOnDolphin.Name = "optRestartOnDolphin";
            this.optRestartOnDolphin.Size = new System.Drawing.Size(200, 25);
            this.optRestartOnDolphin.TabIndex = 1;
            this.optRestartOnDolphin.Text = "Restart on Dolphin";
            this.optRestartOnDolphin.UseVisualStyleBackColor = true;

            // 
            // btnApply
            // 
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnApply.FlatAppearance.BorderSize = 0;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(155, 620);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(180, 40);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "💾 Apply & Restart";
            this.btnApply.UseVisualStyleBackColor = false;
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(345, 620);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 40);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "↺ Reset";
            this.btnReset.UseVisualStyleBackColor = false;
            // 
            // OptionsControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.tabsOptions);
            this.Controls.Add(this.panelOptionsSidebar);
            this.Name = "OptionsControl";
            this.Size = new System.Drawing.Size(560, 770);
            this.panelOptionsSidebar.ResumeLayout(false);
            this.tabsOptions.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).EndInit();

            this.tabDetection.ResumeLayout(false);
            this.tabDetection.PerformLayout();
            this.tabGestures.ResumeLayout(false);
            this.tabGestures.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optShakeSensitivity)).EndInit();
            this.tabEmulators.ResumeLayout(false);
            this.tabEmulators.PerformLayout();

            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelOptionsSidebar;
        private System.Windows.Forms.Button btnTabGeneral;

        private System.Windows.Forms.Button btnTabDetection;
        private System.Windows.Forms.Button btnTabGestures;
        private System.Windows.Forms.Button btnTabEmulators;


        private System.Windows.Forms.TabControl tabsOptions;
        private System.Windows.Forms.TabPage tabGeneral;

        private System.Windows.Forms.TabPage tabDetection;
        private System.Windows.Forms.TabPage tabGestures;
        private System.Windows.Forms.TabPage tabEmulators;

        
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnReset;
        
        // General
        private System.Windows.Forms.Label lblMouseMode;
        private System.Windows.Forms.ComboBox optMouseMode;
        private System.Windows.Forms.Label lblMonitorId;
        private System.Windows.Forms.NumericUpDown optMonitorId;
        private System.Windows.Forms.Label lblLEDLayout;
        private System.Windows.Forms.ComboBox optLEDLayout;
        private System.Windows.Forms.Label lblIRSensitivity;
        private System.Windows.Forms.NumericUpDown optIRSensitivity;
        private System.Windows.Forms.CheckBox optShowNotifications;



        // Detection
        private System.Windows.Forms.CheckBox optDetectDolphin;
        private System.Windows.Forms.CheckBox optDetectBluetooth;
        private System.Windows.Forms.Label lblDetectionInfo;

        // Gestures
        private System.Windows.Forms.CheckBox optEnableOffScreenReload;
        private System.Windows.Forms.CheckBox optOffScreenReloadAuto;
        private System.Windows.Forms.CheckBox optEnableShakeReload;
        private System.Windows.Forms.CheckBox optShakeFromNunchuk;
        private System.Windows.Forms.CheckBox optEnableGrenadeGesture;
        private System.Windows.Forms.Label lblGesturesDevSeparator;
        private System.Windows.Forms.Label lblShakeSensitivity;
        private System.Windows.Forms.TrackBar optShakeSensitivity;

        // Emulators
        private System.Windows.Forms.CheckBox optRestartOnDolphin;
        private System.Windows.Forms.CheckBox optRestartOnCemu;


        
        public System.Windows.Forms.Button btnBack;
    }
}
