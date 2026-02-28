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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsControl));
            this.optEnableFPSMode = new System.Windows.Forms.CheckBox();
            this.lblHelpPersistentGamePads = new System.Windows.Forms.Label();
            this.panelOptionsSidebar = new System.Windows.Forms.Panel();
            this.btnTabGeneral = new System.Windows.Forms.Button();
            this.btnTabDetection = new System.Windows.Forms.Button();
            this.btnTabGestures = new System.Windows.Forms.Button();
            this.btnTabEmulators = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.tabsOptions = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.optUseHighPerfTimers = new System.Windows.Forms.CheckBox();
            this.optEnableHomographyCache = new System.Windows.Forms.CheckBox();
            this.optEnableDistanceCompensation = new System.Windows.Forms.CheckBox();
            this.optIRSmoothingStrength = new System.Windows.Forms.NumericUpDown();
            this.lblIRSmoothingStrength = new System.Windows.Forms.Label();
            this.optIRExtrapolationStrength = new System.Windows.Forms.NumericUpDown();
            this.lblIRExtrapolationStrength = new System.Windows.Forms.Label();
            this.optUseIRExtrapolation = new System.Windows.Forms.CheckBox();
            this.optEnableIRSmoothing = new System.Windows.Forms.CheckBox();
            this.optEnableVirtualPolling = new System.Windows.Forms.CheckBox();
            this.lblVirtualPollingRate = new System.Windows.Forms.Label();
            this.optVirtualPollingRate = new System.Windows.Forms.NumericUpDown();
            this.optLogLevel = new System.Windows.Forms.ComboBox();
            this.lblLogLevelModern = new System.Windows.Forms.Label();
            this.optAutoStart = new System.Windows.Forms.ComboBox();
            this.lblAutoStart = new System.Windows.Forms.Label();
            this.btnConfigureGamePad = new System.Windows.Forms.Button();
            this.optPersistentGamePads = new System.Windows.Forms.CheckBox();
            this.optEnableGamePadSwap = new System.Windows.Forms.CheckBox();
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
            this.lblGrenadeDevice = new System.Windows.Forms.Label();
            this.optGrenadeDevice = new System.Windows.Forms.ComboBox();
            this.lblShakeDevice = new System.Windows.Forms.Label();
            this.optShakeDevice = new System.Windows.Forms.ComboBox();
            this.lblGesturesDevSeparator = new System.Windows.Forms.Label();
            this.optShakeSensitivity = new System.Windows.Forms.ComboBox();
            this.lblShakeSensitivity = new System.Windows.Forms.Label();
            this.optEnableGrenadeGesture = new System.Windows.Forms.CheckBox();
            this.optEnableShakeReload = new System.Windows.Forms.CheckBox();
            this.optOffScreenReloadAuto = new System.Windows.Forms.CheckBox();
            this.optEnableOffScreenReload = new System.Windows.Forms.CheckBox();
            this.tabEmulators = new System.Windows.Forms.TabPage();
            this.lblHelpRestartCemu = new System.Windows.Forms.Label();
            this.lblHelpRestartDolphin = new System.Windows.Forms.Label();
            this.btnBrowseCemu = new System.Windows.Forms.Button();
            this.txtCemuPath = new System.Windows.Forms.TextBox();
            this.lblCemuPath = new System.Windows.Forms.Label();
            this.btnBrowseDolphin = new System.Windows.Forms.Button();
            this.txtDolphinPath = new System.Windows.Forms.TextBox();
            this.lblDolphinPath = new System.Windows.Forms.Label();
            this.btnBrowseDuckStation = new System.Windows.Forms.Button();
            this.txtDuckStationPath = new System.Windows.Forms.TextBox();
            this.lblDuckStationPath = new System.Windows.Forms.Label();
            this.btnBrowsePCSX2 = new System.Windows.Forms.Button();
            this.txtPCSX2Path = new System.Windows.Forms.TextBox();
            this.lblPCSX2Path = new System.Windows.Forms.Label();
            this.optStandaloneMode = new System.Windows.Forms.CheckBox();
            this.optRestartOnCemu = new System.Windows.Forms.CheckBox();
            this.optRestartOnDolphin = new System.Windows.Forms.CheckBox();
            this.toolTipRestart = new System.Windows.Forms.ToolTip(this.components);
            this.btnApply = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.panelOptionsSidebar.SuspendLayout();
            this.tabsOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSmoothingStrength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRExtrapolationStrength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optVirtualPollingRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).BeginInit();
            this.tabDetection.SuspendLayout();
            this.tabGestures.SuspendLayout();
            this.tabEmulators.SuspendLayout();
            this.SuspendLayout();
            // 
            // optEnableFPSMode
            // 
            this.optEnableFPSMode.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableFPSMode.ForeColor = System.Drawing.Color.Goldenrod;
            this.optEnableFPSMode.Location = new System.Drawing.Point(20, 323);
            this.optEnableFPSMode.Name = "optEnableFPSMode";
            this.optEnableFPSMode.Size = new System.Drawing.Size(154, 25);
            this.optEnableFPSMode.TabIndex = 1;
            this.optEnableFPSMode.Text = "FPS Mode  (Alpha DEV)";
            this.optEnableFPSMode.UseVisualStyleBackColor = true;
            // 
            // lblHelpPersistentGamePads
            // 
            this.lblHelpPersistentGamePads.AutoSize = true;
            this.lblHelpPersistentGamePads.Cursor = System.Windows.Forms.Cursors.Help;
            this.lblHelpPersistentGamePads.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblHelpPersistentGamePads.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblHelpPersistentGamePads.Location = new System.Drawing.Point(197, 389);
            this.lblHelpPersistentGamePads.Name = "lblHelpPersistentGamePads";
            this.lblHelpPersistentGamePads.Size = new System.Drawing.Size(12, 15);
            this.lblHelpPersistentGamePads.TabIndex = 17;
            this.lblHelpPersistentGamePads.Text = "?";
            this.toolTipRestart.SetToolTip(this.lblHelpPersistentGamePads, resources.GetString("lblHelpPersistentGamePads.ToolTip"));
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
            this.tabsOptions.Size = new System.Drawing.Size(400, 704);
            this.tabsOptions.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabsOptions.TabIndex = 1;
            // 
            // tabGeneral
            // 
            this.tabGeneral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabGeneral.Controls.Add(this.optEnableFPSMode);
            this.tabGeneral.Controls.Add(this.lblHelpPersistentGamePads);
            this.tabGeneral.Controls.Add(this.optUseHighPerfTimers);
            this.tabGeneral.Controls.Add(this.optEnableHomographyCache);
            this.tabGeneral.Controls.Add(this.optEnableDistanceCompensation);
            this.tabGeneral.Controls.Add(this.optIRSmoothingStrength);
            this.tabGeneral.Controls.Add(this.lblIRSmoothingStrength);
            this.tabGeneral.Controls.Add(this.optIRExtrapolationStrength);
            this.tabGeneral.Controls.Add(this.lblIRExtrapolationStrength);
            this.tabGeneral.Controls.Add(this.optUseIRExtrapolation);
            this.tabGeneral.Controls.Add(this.optEnableIRSmoothing);
            this.tabGeneral.Controls.Add(this.optEnableVirtualPolling);
            this.tabGeneral.Controls.Add(this.lblVirtualPollingRate);
            this.tabGeneral.Controls.Add(this.optVirtualPollingRate);
            this.tabGeneral.Controls.Add(this.optLogLevel);
            this.tabGeneral.Controls.Add(this.lblLogLevelModern);
            this.tabGeneral.Controls.Add(this.optAutoStart);
            this.tabGeneral.Controls.Add(this.lblAutoStart);
            this.tabGeneral.Controls.Add(this.btnConfigureGamePad);
            this.tabGeneral.Controls.Add(this.optPersistentGamePads);
            this.tabGeneral.Controls.Add(this.optEnableGamePadSwap);
            this.tabGeneral.Controls.Add(this.optShowNotifications);
            this.tabGeneral.Controls.Add(this.optIRSensitivity);
            this.tabGeneral.Controls.Add(this.lblIRSensitivity);
            this.tabGeneral.Controls.Add(this.optLEDLayout);
            this.tabGeneral.Controls.Add(this.lblLEDLayout);
            this.tabGeneral.Controls.Add(this.optMonitorId);
            this.tabGeneral.Controls.Add(this.lblMonitorId);
            this.tabGeneral.Controls.Add(this.optMouseMode);
            this.tabGeneral.Controls.Add(this.lblMouseMode);
            this.tabGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(392, 678);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            // 
            // optUseHighPerfTimers
            // 
            this.optUseHighPerfTimers.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optUseHighPerfTimers.ForeColor = System.Drawing.Color.White;
            this.optUseHighPerfTimers.Location = new System.Drawing.Point(20, 485);
            this.optUseHighPerfTimers.Name = "optUseHighPerfTimers";
            this.optUseHighPerfTimers.Size = new System.Drawing.Size(250, 25);
            this.optUseHighPerfTimers.TabIndex = 8;
            this.optUseHighPerfTimers.Text = "High Performance Timers";
            this.optUseHighPerfTimers.UseVisualStyleBackColor = true;
            // 
            // optEnableHomographyCache
            // 
            this.optEnableHomographyCache.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableHomographyCache.ForeColor = System.Drawing.Color.White;
            this.optEnableHomographyCache.Location = new System.Drawing.Point(20, 511);
            this.optEnableHomographyCache.Name = "optEnableHomographyCache";
            this.optEnableHomographyCache.Size = new System.Drawing.Size(250, 25);
            this.optEnableHomographyCache.TabIndex = 9;
            this.optEnableHomographyCache.Text = "Enable Homography Cache (Static Mode)";
            this.optEnableHomographyCache.UseVisualStyleBackColor = true;
            // 
            // optEnableDistanceCompensation
            // 
            this.optEnableDistanceCompensation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableDistanceCompensation.ForeColor = System.Drawing.Color.White;
            this.optEnableDistanceCompensation.Location = new System.Drawing.Point(20, 537);
            this.optEnableDistanceCompensation.Name = "optEnableDistanceCompensation";
            this.optEnableDistanceCompensation.Size = new System.Drawing.Size(320, 25);
            this.optEnableDistanceCompensation.TabIndex = 9;
            this.optEnableDistanceCompensation.Text = "Distance Compensation (Single Sensor Bar only)";
            this.optEnableDistanceCompensation.UseVisualStyleBackColor = true;
            // 
            // optIRSmoothingStrength
            // 
            this.optIRSmoothingStrength.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optIRSmoothingStrength.ForeColor = System.Drawing.Color.White;
            this.optIRSmoothingStrength.Location = new System.Drawing.Point(166, 457);
            this.optIRSmoothingStrength.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.optIRSmoothingStrength.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.optIRSmoothingStrength.Name = "optIRSmoothingStrength";
            this.optIRSmoothingStrength.Size = new System.Drawing.Size(70, 20);
            this.optIRSmoothingStrength.TabIndex = 7;
            this.optIRSmoothingStrength.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblIRSmoothingStrength
            // 
            this.lblIRSmoothingStrength.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblIRSmoothingStrength.ForeColor = System.Drawing.Color.White;
            this.lblIRSmoothingStrength.Location = new System.Drawing.Point(40, 452);
            this.lblIRSmoothingStrength.Name = "lblIRSmoothingStrength";
            this.lblIRSmoothingStrength.Size = new System.Drawing.Size(120, 25);
            this.lblIRSmoothingStrength.TabIndex = 0;
            this.lblIRSmoothingStrength.Text = "Strength (1-10):";
            this.lblIRSmoothingStrength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optIRExtrapolationStrength
            // 
            this.optIRExtrapolationStrength.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optIRExtrapolationStrength.DecimalPlaces = 1;
            this.optIRExtrapolationStrength.ForeColor = System.Drawing.Color.White;
            this.optIRExtrapolationStrength.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.optIRExtrapolationStrength.Location = new System.Drawing.Point(166, 594);
            this.optIRExtrapolationStrength.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.optIRExtrapolationStrength.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.optIRExtrapolationStrength.Name = "optIRExtrapolationStrength";
            this.optIRExtrapolationStrength.Size = new System.Drawing.Size(70, 20);
            this.optIRExtrapolationStrength.TabIndex = 11;
            this.optIRExtrapolationStrength.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // lblIRExtrapolationStrength
            // 
            this.lblIRExtrapolationStrength.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblIRExtrapolationStrength.ForeColor = System.Drawing.Color.White;
            this.lblIRExtrapolationStrength.Location = new System.Drawing.Point(40, 587);
            this.lblIRExtrapolationStrength.Name = "lblIRExtrapolationStrength";
            this.lblIRExtrapolationStrength.Size = new System.Drawing.Size(120, 25);
            this.lblIRExtrapolationStrength.TabIndex = 0;
            this.lblIRExtrapolationStrength.Text = "Strength (0.1-10.0):";
            this.lblIRExtrapolationStrength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optUseIRExtrapolation
            // 
            this.optUseIRExtrapolation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optUseIRExtrapolation.ForeColor = System.Drawing.Color.Goldenrod;
            this.optUseIRExtrapolation.Location = new System.Drawing.Point(20, 564);
            this.optUseIRExtrapolation.Name = "optUseIRExtrapolation";
            this.optUseIRExtrapolation.Size = new System.Drawing.Size(250, 25);
            this.optUseIRExtrapolation.TabIndex = 10;
            this.optUseIRExtrapolation.Text = "Experimental: IR Extrapolation";
            this.optUseIRExtrapolation.UseVisualStyleBackColor = true;
            // 
            // optEnableIRSmoothing
            // 
            this.optEnableIRSmoothing.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableIRSmoothing.ForeColor = System.Drawing.Color.White;
            this.optEnableIRSmoothing.Location = new System.Drawing.Point(20, 426);
            this.optEnableIRSmoothing.Name = "optEnableIRSmoothing";
            this.optEnableIRSmoothing.Size = new System.Drawing.Size(220, 25);
            this.optEnableIRSmoothing.TabIndex = 6;
            this.optEnableIRSmoothing.Text = "Enable IR Smoothing (EMA)";
            this.optEnableIRSmoothing.UseVisualStyleBackColor = true;
            // 
            // optEnableVirtualPolling
            // 
            this.optEnableVirtualPolling.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableVirtualPolling.ForeColor = System.Drawing.Color.Goldenrod;
            this.optEnableVirtualPolling.Location = new System.Drawing.Point(20, 620);
            this.optEnableVirtualPolling.Name = "optEnableVirtualPolling";
            this.optEnableVirtualPolling.Size = new System.Drawing.Size(250, 25);
            this.optEnableVirtualPolling.TabIndex = 12;
            this.optEnableVirtualPolling.Text = "Enable Virtual Polling (Upsampling)";
            this.optEnableVirtualPolling.UseVisualStyleBackColor = true;
            // 
            // lblVirtualPollingRate
            // 
            this.lblVirtualPollingRate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblVirtualPollingRate.ForeColor = System.Drawing.Color.White;
            this.lblVirtualPollingRate.Location = new System.Drawing.Point(40, 645);
            this.lblVirtualPollingRate.Name = "lblVirtualPollingRate";
            this.lblVirtualPollingRate.Size = new System.Drawing.Size(120, 25);
            this.lblVirtualPollingRate.TabIndex = 0;
            this.lblVirtualPollingRate.Text = "Rate (Hz):";
            this.lblVirtualPollingRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optVirtualPollingRate
            // 
            this.optVirtualPollingRate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optVirtualPollingRate.ForeColor = System.Drawing.Color.White;
            this.optVirtualPollingRate.Location = new System.Drawing.Point(166, 650);
            this.optVirtualPollingRate.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.optVirtualPollingRate.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.optVirtualPollingRate.Name = "optVirtualPollingRate";
            this.optVirtualPollingRate.Size = new System.Drawing.Size(70, 20);
            this.optVirtualPollingRate.TabIndex = 13;
            this.optVirtualPollingRate.Value = new decimal(new int[] {
            250,
            0,
            0,
            0});
            // 
            // optLogLevel
            // 
            this.optLogLevel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optLogLevel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optLogLevel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optLogLevel.ForeColor = System.Drawing.Color.White;
            this.optLogLevel.FormattingEnabled = true;
            this.optLogLevel.Items.AddRange(new object[] {
            "ALL",
            "TRACE",
            "DEBUG",
            "INFO",
            "WARNING",
            "ERROR",
            "FATAL",
            "NONE"});
            this.optLogLevel.Location = new System.Drawing.Point(140, 233);
            this.optLogLevel.Name = "optLogLevel";
            this.optLogLevel.Size = new System.Drawing.Size(200, 23);
            this.optLogLevel.TabIndex = 5;
            // 
            // lblLogLevelModern
            // 
            this.lblLogLevelModern.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblLogLevelModern.ForeColor = System.Drawing.Color.White;
            this.lblLogLevelModern.Location = new System.Drawing.Point(10, 233);
            this.lblLogLevelModern.Name = "lblLogLevelModern";
            this.lblLogLevelModern.Size = new System.Drawing.Size(120, 25);
            this.lblLogLevelModern.TabIndex = 0;
            this.lblLogLevelModern.Text = "Logging Level:";
            this.lblLogLevelModern.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optAutoStart
            // 
            this.optAutoStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optAutoStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optAutoStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optAutoStart.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optAutoStart.ForeColor = System.Drawing.Color.White;
            this.optAutoStart.FormattingEnabled = true;
            this.optAutoStart.Location = new System.Drawing.Point(140, 280);
            this.optAutoStart.Name = "optAutoStart";
            this.optAutoStart.Size = new System.Drawing.Size(200, 23);
            this.optAutoStart.TabIndex = 6;
            // 
            // lblAutoStart
            // 
            this.lblAutoStart.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblAutoStart.ForeColor = System.Drawing.Color.White;
            this.lblAutoStart.Location = new System.Drawing.Point(10, 280);
            this.lblAutoStart.Name = "lblAutoStart";
            this.lblAutoStart.Size = new System.Drawing.Size(120, 25);
            this.lblAutoStart.TabIndex = 0;
            this.lblAutoStart.Text = "Auto-Start:";
            this.lblAutoStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnConfigureGamePad
            // 
            this.btnConfigureGamePad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnConfigureGamePad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfigureGamePad.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnConfigureGamePad.ForeColor = System.Drawing.Color.White;
            this.btnConfigureGamePad.Location = new System.Drawing.Point(220, 353);
            this.btnConfigureGamePad.Name = "btnConfigureGamePad";
            this.btnConfigureGamePad.Size = new System.Drawing.Size(120, 25);
            this.btnConfigureGamePad.TabIndex = 3;
            this.btnConfigureGamePad.Text = "Configure...";
            this.btnConfigureGamePad.UseVisualStyleBackColor = false;
            this.btnConfigureGamePad.Click += new System.EventHandler(this.BtnConfigureGamePad_Click);
            // 
            // optPersistentGamePads
            // 
            this.optPersistentGamePads.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optPersistentGamePads.ForeColor = System.Drawing.Color.White;
            this.optPersistentGamePads.Location = new System.Drawing.Point(20, 384);
            this.optPersistentGamePads.Name = "optPersistentGamePads";
            this.optPersistentGamePads.Size = new System.Drawing.Size(220, 25);
            this.optPersistentGamePads.TabIndex = 4;
            this.optPersistentGamePads.Text = "Stabilize GamePad Indices";
            this.optPersistentGamePads.UseVisualStyleBackColor = true;
            // 
            // optEnableGamePadSwap
            // 
            this.optEnableGamePadSwap.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableGamePadSwap.ForeColor = System.Drawing.Color.White;
            this.optEnableGamePadSwap.Location = new System.Drawing.Point(20, 353);
            this.optEnableGamePadSwap.Name = "optEnableGamePadSwap";
            this.optEnableGamePadSwap.Size = new System.Drawing.Size(189, 25);
            this.optEnableGamePadSwap.TabIndex = 2;
            this.optEnableGamePadSwap.Text = "Enable GamePad Swap Mode";
            this.optEnableGamePadSwap.UseVisualStyleBackColor = true;
            // 
            // optShowNotifications
            // 
            this.optShowNotifications.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optShowNotifications.ForeColor = System.Drawing.Color.White;
            this.optShowNotifications.Location = new System.Drawing.Point(20, 202);
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
            this.optIRSensitivity.Location = new System.Drawing.Point(140, 148);
            this.optIRSensitivity.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.optIRSensitivity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.optIRSensitivity.Name = "optIRSensitivity";
            this.optIRSensitivity.Size = new System.Drawing.Size(100, 20);
            this.optIRSensitivity.TabIndex = 1;
            this.optIRSensitivity.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblIRSensitivity
            // 
            this.lblIRSensitivity.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblIRSensitivity.ForeColor = System.Drawing.Color.White;
            this.lblIRSensitivity.Location = new System.Drawing.Point(10, 148);
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
            "Four Corners"});
            this.optLEDLayout.Location = new System.Drawing.Point(140, 104);
            this.optLEDLayout.Name = "optLEDLayout";
            this.optLEDLayout.Size = new System.Drawing.Size(200, 23);
            this.optLEDLayout.TabIndex = 1;
            // 
            // lblLEDLayout
            // 
            this.lblLEDLayout.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblLEDLayout.ForeColor = System.Drawing.Color.White;
            this.lblLEDLayout.Location = new System.Drawing.Point(10, 104);
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
            this.optMonitorId.Location = new System.Drawing.Point(140, 63);
            this.optMonitorId.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.optMonitorId.Name = "optMonitorId";
            this.optMonitorId.Size = new System.Drawing.Size(100, 20);
            this.optMonitorId.TabIndex = 1;
            // 
            // lblMonitorId
            // 
            this.lblMonitorId.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblMonitorId.ForeColor = System.Drawing.Color.White;
            this.lblMonitorId.Location = new System.Drawing.Point(10, 63);
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
            this.optMouseMode.Location = new System.Drawing.Point(140, 22);
            this.optMouseMode.Name = "optMouseMode";
            this.optMouseMode.Size = new System.Drawing.Size(200, 23);
            this.optMouseMode.TabIndex = 1;
            // 
            // lblMouseMode
            // 
            this.lblMouseMode.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblMouseMode.ForeColor = System.Drawing.Color.White;
            this.lblMouseMode.Location = new System.Drawing.Point(10, 22);
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
            this.tabDetection.Location = new System.Drawing.Point(4, 22);
            this.tabDetection.Name = "tabDetection";
            this.tabDetection.Size = new System.Drawing.Size(392, 678);
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
            // 
            // tabGestures
            // 
            this.tabGestures.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabGestures.Controls.Add(this.lblGrenadeDevice);
            this.tabGestures.Controls.Add(this.optGrenadeDevice);
            this.tabGestures.Controls.Add(this.lblShakeDevice);
            this.tabGestures.Controls.Add(this.optShakeDevice);
            this.tabGestures.Controls.Add(this.lblGesturesDevSeparator);
            this.tabGestures.Controls.Add(this.optShakeSensitivity);
            this.tabGestures.Controls.Add(this.lblShakeSensitivity);
            this.tabGestures.Controls.Add(this.optEnableGrenadeGesture);
            this.tabGestures.Controls.Add(this.optEnableShakeReload);
            this.tabGestures.Controls.Add(this.optOffScreenReloadAuto);
            this.tabGestures.Controls.Add(this.optEnableOffScreenReload);
            this.tabGestures.Location = new System.Drawing.Point(4, 22);
            this.tabGestures.Name = "tabGestures";
            this.tabGestures.Size = new System.Drawing.Size(392, 678);
            this.tabGestures.TabIndex = 3;
            this.tabGestures.Text = "Gestures";
            // 
            // lblGrenadeDevice
            // 
            this.lblGrenadeDevice.ForeColor = System.Drawing.Color.White;
            this.lblGrenadeDevice.Location = new System.Drawing.Point(20, 275);
            this.lblGrenadeDevice.Name = "lblGrenadeDevice";
            this.lblGrenadeDevice.Size = new System.Drawing.Size(120, 25);
            this.lblGrenadeDevice.TabIndex = 5;
            this.lblGrenadeDevice.Text = "Grenade Device:";
            this.lblGrenadeDevice.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optGrenadeDevice
            // 
            this.optGrenadeDevice.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optGrenadeDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optGrenadeDevice.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optGrenadeDevice.ForeColor = System.Drawing.Color.White;
            this.optGrenadeDevice.FormattingEnabled = true;
            this.optGrenadeDevice.Items.AddRange(new object[] {
            "Wiimote",
            "Nunchuk"});
            this.optGrenadeDevice.Location = new System.Drawing.Point(140, 275);
            this.optGrenadeDevice.Name = "optGrenadeDevice";
            this.optGrenadeDevice.Size = new System.Drawing.Size(200, 21);
            this.optGrenadeDevice.TabIndex = 6;
            // 
            // lblShakeDevice
            // 
            this.lblShakeDevice.ForeColor = System.Drawing.Color.White;
            this.lblShakeDevice.Location = new System.Drawing.Point(20, 195);
            this.lblShakeDevice.Name = "lblShakeDevice";
            this.lblShakeDevice.Size = new System.Drawing.Size(120, 25);
            this.lblShakeDevice.TabIndex = 3;
            this.lblShakeDevice.Text = "Shake Device:";
            this.lblShakeDevice.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optShakeDevice
            // 
            this.optShakeDevice.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optShakeDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optShakeDevice.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optShakeDevice.ForeColor = System.Drawing.Color.White;
            this.optShakeDevice.FormattingEnabled = true;
            this.optShakeDevice.Items.AddRange(new object[] {
            "Wiimote",
            "Nunchuk"});
            this.optShakeDevice.Location = new System.Drawing.Point(140, 195);
            this.optShakeDevice.Name = "optShakeDevice";
            this.optShakeDevice.Size = new System.Drawing.Size(200, 21);
            this.optShakeDevice.TabIndex = 4;
            // 
            // lblGesturesDevSeparator
            // 
            this.lblGesturesDevSeparator.AutoSize = true;
            this.lblGesturesDevSeparator.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblGesturesDevSeparator.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblGesturesDevSeparator.Location = new System.Drawing.Point(137, 93);
            this.lblGesturesDevSeparator.Name = "lblGesturesDevSeparator";
            this.lblGesturesDevSeparator.Size = new System.Drawing.Size(137, 15);
            this.lblGesturesDevSeparator.TabIndex = 0;
            this.lblGesturesDevSeparator.Text = "—— (Experimental) ——";
            // 
            // optShakeSensitivity
            // 
            this.optShakeSensitivity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optShakeSensitivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.optShakeSensitivity.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.optShakeSensitivity.ForeColor = System.Drawing.Color.White;
            this.optShakeSensitivity.FormattingEnabled = true;
            this.optShakeSensitivity.Items.AddRange(new object[] {
            "Very Low",
            "Low",
            "Medium",
            "High"});
            this.optShakeSensitivity.Location = new System.Drawing.Point(140, 150);
            this.optShakeSensitivity.Name = "optShakeSensitivity";
            this.optShakeSensitivity.Size = new System.Drawing.Size(200, 21);
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
            this.optEnableGrenadeGesture.TabIndex = 5;
            this.optEnableGrenadeGesture.Text = "Grenade Gesture";
            this.optEnableGrenadeGesture.UseVisualStyleBackColor = true;
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
            // tabEmulators
            // 
            this.tabEmulators.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabEmulators.Controls.Add(this.lblHelpRestartCemu);
            this.tabEmulators.Controls.Add(this.lblHelpRestartDolphin);
            this.tabEmulators.Controls.Add(this.btnBrowseCemu);
            this.tabEmulators.Controls.Add(this.txtCemuPath);
            this.tabEmulators.Controls.Add(this.lblCemuPath);
            this.tabEmulators.Controls.Add(this.btnBrowseDolphin);
            this.tabEmulators.Controls.Add(this.txtDolphinPath);
            this.tabEmulators.Controls.Add(this.lblDolphinPath);
            this.tabEmulators.Controls.Add(this.btnBrowseDuckStation);
            this.tabEmulators.Controls.Add(this.txtDuckStationPath);
            this.tabEmulators.Controls.Add(this.lblDuckStationPath);
            this.tabEmulators.Controls.Add(this.btnBrowsePCSX2);
            this.tabEmulators.Controls.Add(this.txtPCSX2Path);
            this.tabEmulators.Controls.Add(this.lblPCSX2Path);
            this.tabEmulators.Controls.Add(this.optStandaloneMode);
            this.tabEmulators.Controls.Add(this.optRestartOnCemu);
            this.tabEmulators.Controls.Add(this.optRestartOnDolphin);
            this.tabEmulators.Location = new System.Drawing.Point(4, 22);
            this.tabEmulators.Name = "tabEmulators";
            this.tabEmulators.Size = new System.Drawing.Size(392, 678);
            this.tabEmulators.TabIndex = 4;
            this.tabEmulators.Text = "Emulators";
            // 
            // lblHelpRestartCemu
            // 
            this.lblHelpRestartCemu.AutoSize = true;
            this.lblHelpRestartCemu.Cursor = System.Windows.Forms.Cursors.Help;
            this.lblHelpRestartCemu.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblHelpRestartCemu.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblHelpRestartCemu.Location = new System.Drawing.Point(164, 64);
            this.lblHelpRestartCemu.Name = "lblHelpRestartCemu";
            this.lblHelpRestartCemu.Size = new System.Drawing.Size(12, 15);
            this.lblHelpRestartCemu.TabIndex = 16;
            this.lblHelpRestartCemu.Text = "?";
            this.toolTipRestart.SetToolTip(this.lblHelpRestartCemu, "Automatically restarts the Wiimote connection when Cemu is detected to ensure pro" +
        "per synchronization.\n(FR: Redémarre automatiquement la Wiimote pour assurer la s" +
        "ynchronisation avec Cemu.)");
            // 
            // lblHelpRestartDolphin
            // 
            this.lblHelpRestartDolphin.AutoSize = true;
            this.lblHelpRestartDolphin.Cursor = System.Windows.Forms.Cursors.Help;
            this.lblHelpRestartDolphin.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblHelpRestartDolphin.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblHelpRestartDolphin.Location = new System.Drawing.Point(164, 24);
            this.lblHelpRestartDolphin.Name = "lblHelpRestartDolphin";
            this.lblHelpRestartDolphin.Size = new System.Drawing.Size(12, 15);
            this.lblHelpRestartDolphin.TabIndex = 15;
            this.lblHelpRestartDolphin.Text = "?";
            this.toolTipRestart.SetToolTip(this.lblHelpRestartDolphin, "Automatically restarts the Wiimote connection when Dolphin is detected to ensure " +
        "proper synchronization.\n(FR: Redémarre automatiquement la Wiimote pour assurer l" +
        "a synchronisation avec Dolphin.)");
            // 
            // btnBrowseCemu
            // 
            this.btnBrowseCemu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnBrowseCemu.FlatAppearance.BorderSize = 0;
            this.btnBrowseCemu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseCemu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnBrowseCemu.ForeColor = System.Drawing.Color.White;
            this.btnBrowseCemu.Location = new System.Drawing.Point(290, 385);
            this.btnBrowseCemu.Name = "btnBrowseCemu";
            this.btnBrowseCemu.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseCemu.TabIndex = 14;
            this.btnBrowseCemu.Text = "Browse...";
            this.btnBrowseCemu.UseVisualStyleBackColor = false;
            // 
            // txtCemuPath
            // 
            this.txtCemuPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtCemuPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCemuPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtCemuPath.ForeColor = System.Drawing.Color.White;
            this.txtCemuPath.Location = new System.Drawing.Point(20, 385);
            this.txtCemuPath.Name = "txtCemuPath";
            this.txtCemuPath.Size = new System.Drawing.Size(260, 23);
            this.txtCemuPath.TabIndex = 13;
            // 
            // lblCemuPath
            // 
            this.lblCemuPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCemuPath.ForeColor = System.Drawing.Color.White;
            this.lblCemuPath.Location = new System.Drawing.Point(20, 360);
            this.lblCemuPath.Name = "lblCemuPath";
            this.lblCemuPath.Size = new System.Drawing.Size(350, 20);
            this.lblCemuPath.TabIndex = 12;
            this.lblCemuPath.Text = "Cemu Emulators Root (Manual):";
            // 
            // btnBrowseDolphin
            // 
            this.btnBrowseDolphin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnBrowseDolphin.FlatAppearance.BorderSize = 0;
            this.btnBrowseDolphin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseDolphin.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnBrowseDolphin.ForeColor = System.Drawing.Color.White;
            this.btnBrowseDolphin.Location = new System.Drawing.Point(290, 315);
            this.btnBrowseDolphin.Name = "btnBrowseDolphin";
            this.btnBrowseDolphin.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseDolphin.TabIndex = 11;
            this.btnBrowseDolphin.Text = "Browse...";
            this.btnBrowseDolphin.UseVisualStyleBackColor = false;
            // 
            // txtDolphinPath
            // 
            this.txtDolphinPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtDolphinPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDolphinPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtDolphinPath.ForeColor = System.Drawing.Color.White;
            this.txtDolphinPath.Location = new System.Drawing.Point(20, 315);
            this.txtDolphinPath.Name = "txtDolphinPath";
            this.txtDolphinPath.Size = new System.Drawing.Size(260, 23);
            this.txtDolphinPath.TabIndex = 10;
            // 
            // lblDolphinPath
            // 
            this.lblDolphinPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDolphinPath.ForeColor = System.Drawing.Color.White;
            this.lblDolphinPath.Location = new System.Drawing.Point(20, 290);
            this.lblDolphinPath.Name = "lblDolphinPath";
            this.lblDolphinPath.Size = new System.Drawing.Size(350, 20);
            this.lblDolphinPath.TabIndex = 9;
            this.lblDolphinPath.Text = "Dolphin Emulators Root (Manual):";
            // 
            // btnBrowseDuckStation
            // 
            this.btnBrowseDuckStation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnBrowseDuckStation.FlatAppearance.BorderSize = 0;
            this.btnBrowseDuckStation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseDuckStation.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnBrowseDuckStation.ForeColor = System.Drawing.Color.White;
            this.btnBrowseDuckStation.Location = new System.Drawing.Point(290, 245);
            this.btnBrowseDuckStation.Name = "btnBrowseDuckStation";
            this.btnBrowseDuckStation.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseDuckStation.TabIndex = 8;
            this.btnBrowseDuckStation.Text = "Browse...";
            this.btnBrowseDuckStation.UseVisualStyleBackColor = false;
            // 
            // txtDuckStationPath
            // 
            this.txtDuckStationPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtDuckStationPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDuckStationPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtDuckStationPath.ForeColor = System.Drawing.Color.White;
            this.txtDuckStationPath.Location = new System.Drawing.Point(20, 245);
            this.txtDuckStationPath.Name = "txtDuckStationPath";
            this.txtDuckStationPath.Size = new System.Drawing.Size(260, 23);
            this.txtDuckStationPath.TabIndex = 7;
            // 
            // lblDuckStationPath
            // 
            this.lblDuckStationPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDuckStationPath.ForeColor = System.Drawing.Color.White;
            this.lblDuckStationPath.Location = new System.Drawing.Point(20, 220);
            this.lblDuckStationPath.Name = "lblDuckStationPath";
            this.lblDuckStationPath.Size = new System.Drawing.Size(350, 20);
            this.lblDuckStationPath.TabIndex = 6;
            this.lblDuckStationPath.Text = "DuckStation Emulators Root (Manual):";
            // 
            // btnBrowsePCSX2
            // 
            this.btnBrowsePCSX2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnBrowsePCSX2.FlatAppearance.BorderSize = 0;
            this.btnBrowsePCSX2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowsePCSX2.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnBrowsePCSX2.ForeColor = System.Drawing.Color.White;
            this.btnBrowsePCSX2.Location = new System.Drawing.Point(290, 175);
            this.btnBrowsePCSX2.Name = "btnBrowsePCSX2";
            this.btnBrowsePCSX2.Size = new System.Drawing.Size(80, 23);
            this.btnBrowsePCSX2.TabIndex = 5;
            this.btnBrowsePCSX2.Text = "Browse...";
            this.btnBrowsePCSX2.UseVisualStyleBackColor = false;
            // 
            // txtPCSX2Path
            // 
            this.txtPCSX2Path.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtPCSX2Path.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPCSX2Path.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtPCSX2Path.ForeColor = System.Drawing.Color.White;
            this.txtPCSX2Path.Location = new System.Drawing.Point(20, 175);
            this.txtPCSX2Path.Name = "txtPCSX2Path";
            this.txtPCSX2Path.Size = new System.Drawing.Size(260, 23);
            this.txtPCSX2Path.TabIndex = 4;
            // 
            // lblPCSX2Path
            // 
            this.lblPCSX2Path.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPCSX2Path.ForeColor = System.Drawing.Color.White;
            this.lblPCSX2Path.Location = new System.Drawing.Point(20, 150);
            this.lblPCSX2Path.Name = "lblPCSX2Path";
            this.lblPCSX2Path.Size = new System.Drawing.Size(350, 20);
            this.lblPCSX2Path.TabIndex = 3;
            this.lblPCSX2Path.Text = "PCSX2 Emulators Root (Manual):";
            // 
            // optStandaloneMode
            // 
            this.optStandaloneMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.optStandaloneMode.ForeColor = System.Drawing.Color.Goldenrod;
            this.optStandaloneMode.Location = new System.Drawing.Point(20, 110);
            this.optStandaloneMode.Name = "optStandaloneMode";
            this.optStandaloneMode.Size = new System.Drawing.Size(300, 25);
            this.optStandaloneMode.TabIndex = 2;
            this.optStandaloneMode.Text = "Standalone / Portable Mode (No RetroBat)";
            this.optStandaloneMode.UseVisualStyleBackColor = true;
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
            // toolTipRestart
            // 
            this.toolTipRestart.AutoPopDelay = 10000;
            this.toolTipRestart.InitialDelay = 500;
            this.toolTipRestart.ReshowDelay = 100;
            this.toolTipRestart.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTipRestart.ToolTipTitle = "Reinitialization Help";
            // 
            // btnApply
            // 
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnApply.FlatAppearance.BorderSize = 0;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(155, 720);
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
            this.btnReset.Location = new System.Drawing.Point(345, 720);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 40);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "↺ Reset";
            this.btnReset.UseVisualStyleBackColor = false;
            // 
            // OptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
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
            ((System.ComponentModel.ISupportInitialize)(this.optIRSmoothingStrength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRExtrapolationStrength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optVirtualPollingRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).EndInit();
            this.tabDetection.ResumeLayout(false);
            this.tabGestures.ResumeLayout(false);
            this.tabGestures.PerformLayout();
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
        private System.Windows.Forms.Label lblMouseMode;
        private System.Windows.Forms.ComboBox optMouseMode;
        private System.Windows.Forms.Label lblMonitorId;
        private System.Windows.Forms.NumericUpDown optMonitorId;
        private System.Windows.Forms.Label lblLEDLayout;
        private System.Windows.Forms.ComboBox optLEDLayout;
        private System.Windows.Forms.Label lblIRSensitivity;
        private System.Windows.Forms.NumericUpDown optIRSensitivity;
        private System.Windows.Forms.CheckBox optShowNotifications;
        private System.Windows.Forms.CheckBox optEnableGamePadSwap;
        private System.Windows.Forms.CheckBox optPersistentGamePads;
        private System.Windows.Forms.CheckBox optDetectDolphin;
        private System.Windows.Forms.CheckBox optDetectBluetooth;
        private System.Windows.Forms.Label lblDetectionInfo;
        private System.Windows.Forms.CheckBox optEnableOffScreenReload;
        private System.Windows.Forms.CheckBox optOffScreenReloadAuto;
        private System.Windows.Forms.CheckBox optEnableShakeReload;

        private System.Windows.Forms.CheckBox optEnableGrenadeGesture;
        private System.Windows.Forms.Label lblGesturesDevSeparator;
        private System.Windows.Forms.Label lblShakeSensitivity;
        private System.Windows.Forms.ComboBox optShakeSensitivity;
        private System.Windows.Forms.Label lblShakeDevice;
        private System.Windows.Forms.ComboBox optShakeDevice;
        private System.Windows.Forms.Label lblGrenadeDevice;
        private System.Windows.Forms.ComboBox optGrenadeDevice;
        private System.Windows.Forms.Button btnConfigureGamePad;
        private System.Windows.Forms.CheckBox optRestartOnDolphin;
        private System.Windows.Forms.CheckBox optRestartOnCemu;
        public System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.ComboBox optLogLevel;
        private System.Windows.Forms.Label lblLogLevelModern;
        private System.Windows.Forms.ComboBox optAutoStart;
        private System.Windows.Forms.Label lblAutoStart;
        private System.Windows.Forms.CheckBox optEnableIRSmoothing;
        private System.Windows.Forms.Label lblIRSmoothingStrength;
        private System.Windows.Forms.NumericUpDown optIRSmoothingStrength;
        private System.Windows.Forms.CheckBox optUseHighPerfTimers;
        private System.Windows.Forms.CheckBox optEnableHomographyCache;
        private System.Windows.Forms.CheckBox optEnableDistanceCompensation;
        private System.Windows.Forms.CheckBox optUseIRExtrapolation;
        private System.Windows.Forms.Label lblIRExtrapolationStrength;
        private System.Windows.Forms.NumericUpDown optIRExtrapolationStrength;
        private System.Windows.Forms.CheckBox optEnableVirtualPolling;
        private System.Windows.Forms.Label lblVirtualPollingRate;
        private System.Windows.Forms.NumericUpDown optVirtualPollingRate;
        private System.Windows.Forms.CheckBox optStandaloneMode;
        private System.Windows.Forms.Label lblPCSX2Path;
        private System.Windows.Forms.TextBox txtPCSX2Path;
        private System.Windows.Forms.Button btnBrowsePCSX2;
        private System.Windows.Forms.Label lblDuckStationPath;
        private System.Windows.Forms.TextBox txtDuckStationPath;
        private System.Windows.Forms.Button btnBrowseDuckStation;
        private System.Windows.Forms.Label lblDolphinPath;
        private System.Windows.Forms.TextBox txtDolphinPath;
        private System.Windows.Forms.Button btnBrowseDolphin;
        private System.Windows.Forms.Label lblCemuPath;
        private System.Windows.Forms.TextBox txtCemuPath;
        private System.Windows.Forms.Button btnBrowseCemu;
        private System.Windows.Forms.Label lblHelpRestartDolphin;
        private System.Windows.Forms.Label lblHelpRestartCemu;
        private System.Windows.Forms.Label lblHelpPersistentGamePads;
        private System.Windows.Forms.CheckBox optEnableFPSMode;
        private System.Windows.Forms.ToolTip toolTipRestart;
    }
}
