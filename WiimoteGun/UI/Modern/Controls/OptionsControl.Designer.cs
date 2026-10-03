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
            this.optIRSmoothingStrengthV2 = new System.Windows.Forms.NumericUpDown();
            this.lblIRSmoothingStrengthV2 = new System.Windows.Forms.Label();
            this.optIRExtrapolationStrengthV2 = new System.Windows.Forms.NumericUpDown();
            this.lblIRExtrapolationStrengthV2 = new System.Windows.Forms.Label();
            this.optVirtualPollingRateV2 = new System.Windows.Forms.NumericUpDown();
            this.lblVirtualPollingRateV2 = new System.Windows.Forms.Label();
            this.lblV2ModelNote = new System.Windows.Forms.Label();
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
            this.optAutoBtReset = new System.Windows.Forms.CheckBox();
            this.numBtResetDelay = new System.Windows.Forms.NumericUpDown();
            this.lblBtResetDelay = new System.Windows.Forms.Label();
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
            // [V55y] Reload rumble controls (Designer-instantiated for VS visual preview)
            // (EN/FR: Contrôles vibration rechargement - instanciés dans le Designer pour prévisuel VS)
            this.chkReloadRumble = new System.Windows.Forms.CheckBox();
            this.lblReloadRumbleIntensity = new System.Windows.Forms.Label();
            this.trkReloadRumbleIntensity = new System.Windows.Forms.TrackBar();
            this.lblReloadRumbleStyle = new System.Windows.Forms.Label();
            this.cboReloadRumbleStyle = new System.Windows.Forms.ComboBox();
            // [V55z] Custom style controls (Designer-instantiated for VS visual preview)
            // (EN/FR: Contrôles du style personnalisé - instanciés dans le Designer pour prévisuel VS)
            this.lblReloadRumbleTicks = new System.Windows.Forms.Label();
            this.trkReloadRumbleTicks = new System.Windows.Forms.TrackBar();
            this.lblReloadRumbleTickOn = new System.Windows.Forms.Label();
            this.nudReloadRumbleTickOnMs = new System.Windows.Forms.NumericUpDown();
            this.lblReloadRumbleTickOff = new System.Windows.Forms.Label();
            this.nudReloadRumbleTickOffMs = new System.Windows.Forms.NumericUpDown();
            this.tabEmulators = new System.Windows.Forms.TabPage();
            this.tabEsScripts = new System.Windows.Forms.TabPage();
            this.lblEsTitle = new System.Windows.Forms.Label();
            this.lblEsInfo = new System.Windows.Forms.Label();
            this.chkEsScripts = new System.Windows.Forms.CheckBox();
            this.lblEsScriptsStatus = new System.Windows.Forms.Label();
            this.chkEsTileHotkey = new System.Windows.Forms.CheckBox();
            this.lblEsTileDelay = new System.Windows.Forms.Label();
            this.numEsTileDelay = new System.Windows.Forms.NumericUpDown();
            this.lblEsTileInfo = new System.Windows.Forms.Label();
            this.optLockModeOnGameStart = new System.Windows.Forms.CheckBox();
            this.lblLockModeDesc = new System.Windows.Forms.Label();
            this.btnTabEsScripts = new System.Windows.Forms.Button();
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
            ((System.ComponentModel.ISupportInitialize)(this.optIRSmoothingStrengthV2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRExtrapolationStrengthV2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optVirtualPollingRateV2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).BeginInit();
            // [V55y] TrackBar BeginInit (Designer norm)
            ((System.ComponentModel.ISupportInitialize)(this.trkReloadRumbleIntensity)).BeginInit();
            // [V55z] Custom style controls BeginInit (Designer norm)
            ((System.ComponentModel.ISupportInitialize)(this.trkReloadRumbleTicks)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReloadRumbleTickOnMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReloadRumbleTickOffMs)).BeginInit();
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
            this.panelOptionsSidebar.Controls.Add(this.btnTabEsScripts);
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
            // btnTabEsScripts
            // 
            this.btnTabEsScripts.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnTabEsScripts.FlatAppearance.BorderSize = 0;
            this.btnTabEsScripts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabEsScripts.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTabEsScripts.ForeColor = System.Drawing.Color.White;
            this.btnTabEsScripts.Location = new System.Drawing.Point(2, 210);
            this.btnTabEsScripts.Name = "btnTabEsScripts";
            this.btnTabEsScripts.Size = new System.Drawing.Size(145, 45);
            this.btnTabEsScripts.TabIndex = 0;
            this.btnTabEsScripts.Text = "🕹️ ES Scripts";
            this.btnTabEsScripts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTabEsScripts.UseVisualStyleBackColor = false;
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
            this.tabsOptions.Controls.Add(this.tabEsScripts);
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
            this.tabGeneral.Controls.Add(this.lblV2ModelNote);
            this.tabGeneral.Controls.Add(this.lblIRSmoothingStrengthV2);
            this.tabGeneral.Controls.Add(this.optIRSmoothingStrengthV2);
            this.tabGeneral.Controls.Add(this.lblIRExtrapolationStrengthV2);
            this.tabGeneral.Controls.Add(this.optIRExtrapolationStrengthV2);
            this.tabGeneral.Controls.Add(this.lblVirtualPollingRateV2);
            this.tabGeneral.Controls.Add(this.optVirtualPollingRateV2);
            this.tabGeneral.Controls.Add(this.optLogLevel);
            this.tabGeneral.Controls.Add(this.lblLogLevelModern);
            this.tabGeneral.Controls.Add(this.optAutoStart);
            this.tabGeneral.Controls.Add(this.lblAutoStart);
            this.tabGeneral.Controls.Add(this.btnConfigureGamePad);
            this.tabGeneral.Controls.Add(this.optPersistentGamePads);
            this.tabGeneral.Controls.Add(this.optEnableGamePadSwap);
            this.tabGeneral.Controls.Add(this.optShowNotifications);
            this.tabGeneral.Controls.Add(this.lblBtResetDelay);
            this.tabGeneral.Controls.Add(this.numBtResetDelay);
            this.tabGeneral.Controls.Add(this.optAutoBtReset);
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
            0,
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
            0,
            0,
            0,
            0});
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
            0,
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
            // lblV2ModelNote
            // 
            this.lblV2ModelNote.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.lblV2ModelNote.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblV2ModelNote.Location = new System.Drawing.Point(240, 426);
            this.lblV2ModelNote.Name = "lblV2ModelNote";
            this.lblV2ModelNote.Size = new System.Drawing.Size(148, 28);
            this.lblV2ModelNote.TabIndex = 14;
            this.lblV2ModelNote.Text = "V2 = Wiimote Plus (RVL-CNT-01-TR)";
            // 
            // lblIRSmoothingStrengthV2
            // 
            this.lblIRSmoothingStrengthV2.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblIRSmoothingStrengthV2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblIRSmoothingStrengthV2.Location = new System.Drawing.Point(240, 452);
            this.lblIRSmoothingStrengthV2.Name = "lblIRSmoothingStrengthV2";
            this.lblIRSmoothingStrengthV2.Size = new System.Drawing.Size(26, 20);
            this.lblIRSmoothingStrengthV2.TabIndex = 15;
            this.lblIRSmoothingStrengthV2.Text = "V2:";
            this.lblIRSmoothingStrengthV2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optIRSmoothingStrengthV2
            // 
            this.optIRSmoothingStrengthV2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optIRSmoothingStrengthV2.ForeColor = System.Drawing.Color.White;
            this.optIRSmoothingStrengthV2.Location = new System.Drawing.Point(268, 457);
            this.optIRSmoothingStrengthV2.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.optIRSmoothingStrengthV2.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.optIRSmoothingStrengthV2.Name = "optIRSmoothingStrengthV2";
            this.optIRSmoothingStrengthV2.Size = new System.Drawing.Size(60, 20);
            this.optIRSmoothingStrengthV2.TabIndex = 16;
            this.optIRSmoothingStrengthV2.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // lblIRExtrapolationStrengthV2
            // 
            this.lblIRExtrapolationStrengthV2.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblIRExtrapolationStrengthV2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblIRExtrapolationStrengthV2.Location = new System.Drawing.Point(240, 589);
            this.lblIRExtrapolationStrengthV2.Name = "lblIRExtrapolationStrengthV2";
            this.lblIRExtrapolationStrengthV2.Size = new System.Drawing.Size(26, 20);
            this.lblIRExtrapolationStrengthV2.TabIndex = 17;
            this.lblIRExtrapolationStrengthV2.Text = "V2:";
            this.lblIRExtrapolationStrengthV2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optIRExtrapolationStrengthV2
            // 
            this.optIRExtrapolationStrengthV2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optIRExtrapolationStrengthV2.DecimalPlaces = 1;
            this.optIRExtrapolationStrengthV2.ForeColor = System.Drawing.Color.White;
            this.optIRExtrapolationStrengthV2.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.optIRExtrapolationStrengthV2.Location = new System.Drawing.Point(268, 594);
            this.optIRExtrapolationStrengthV2.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.optIRExtrapolationStrengthV2.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.optIRExtrapolationStrengthV2.Name = "optIRExtrapolationStrengthV2";
            this.optIRExtrapolationStrengthV2.Size = new System.Drawing.Size(60, 20);
            this.optIRExtrapolationStrengthV2.TabIndex = 18;
            this.optIRExtrapolationStrengthV2.Value = new decimal(new int[] {
            3,
            0,
            0,
            65536});
            // 
            // lblVirtualPollingRateV2
            // 
            this.lblVirtualPollingRateV2.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblVirtualPollingRateV2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblVirtualPollingRateV2.Location = new System.Drawing.Point(240, 645);
            this.lblVirtualPollingRateV2.Name = "lblVirtualPollingRateV2";
            this.lblVirtualPollingRateV2.Size = new System.Drawing.Size(26, 20);
            this.lblVirtualPollingRateV2.TabIndex = 19;
            this.lblVirtualPollingRateV2.Text = "V2:";
            this.lblVirtualPollingRateV2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optVirtualPollingRateV2
            // 
            this.optVirtualPollingRateV2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.optVirtualPollingRateV2.ForeColor = System.Drawing.Color.White;
            this.optVirtualPollingRateV2.Location = new System.Drawing.Point(268, 650);
            this.optVirtualPollingRateV2.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.optVirtualPollingRateV2.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.optVirtualPollingRateV2.Name = "optVirtualPollingRateV2";
            this.optVirtualPollingRateV2.Size = new System.Drawing.Size(60, 20);
            this.optVirtualPollingRateV2.TabIndex = 20;
            this.optVirtualPollingRateV2.Value = new decimal(new int[] {
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
            this.optShowNotifications.Size = new System.Drawing.Size(155, 25);
            this.optShowNotifications.TabIndex = 1;
            this.optShowNotifications.Text = "Show Notifications";
            this.optShowNotifications.UseVisualStyleBackColor = true;
            // 
            // optAutoBtReset
            // 
            this.optAutoBtReset.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optAutoBtReset.ForeColor = System.Drawing.Color.LightCoral;
            this.optAutoBtReset.Location = new System.Drawing.Point(180, 202);
            this.optAutoBtReset.Name = "optAutoBtReset";
            this.optAutoBtReset.Size = new System.Drawing.Size(120, 25);
            this.optAutoBtReset.TabIndex = 100;
            this.optAutoBtReset.Text = "BT Auto-Reset";
            this.optAutoBtReset.UseVisualStyleBackColor = true;
            // 
            // numBtResetDelay
            // 
            this.numBtResetDelay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.numBtResetDelay.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numBtResetDelay.ForeColor = System.Drawing.Color.White;
            this.numBtResetDelay.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numBtResetDelay.Location = new System.Drawing.Point(305, 203);
            this.numBtResetDelay.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numBtResetDelay.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numBtResetDelay.Name = "numBtResetDelay";
            this.numBtResetDelay.Size = new System.Drawing.Size(50, 24);
            this.numBtResetDelay.TabIndex = 101;
            this.numBtResetDelay.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // lblBtResetDelay
            // 
            this.lblBtResetDelay.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBtResetDelay.ForeColor = System.Drawing.Color.Silver;
            this.lblBtResetDelay.Location = new System.Drawing.Point(358, 205);
            this.lblBtResetDelay.Name = "lblBtResetDelay";
            this.lblBtResetDelay.Size = new System.Drawing.Size(25, 20);
            this.lblBtResetDelay.Text = "s";
            this.lblBtResetDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // [V55y] Reload rumble controls (EN/FR: Contrôles vibration rechargement)
            this.tabGestures.Controls.Add(this.chkReloadRumble);
            this.tabGestures.Controls.Add(this.lblReloadRumbleIntensity);
            this.tabGestures.Controls.Add(this.trkReloadRumbleIntensity);
            this.tabGestures.Controls.Add(this.lblReloadRumbleStyle);
            this.tabGestures.Controls.Add(this.cboReloadRumbleStyle);
            // [V55z] Custom style controls (EN/FR: Contrôles du style personnalisé)
            this.tabGestures.Controls.Add(this.lblReloadRumbleTicks);
            this.tabGestures.Controls.Add(this.trkReloadRumbleTicks);
            this.tabGestures.Controls.Add(this.lblReloadRumbleTickOn);
            this.tabGestures.Controls.Add(this.nudReloadRumbleTickOnMs);
            this.tabGestures.Controls.Add(this.lblReloadRumbleTickOff);
            this.tabGestures.Controls.Add(this.nudReloadRumbleTickOffMs);
            this.tabGestures.Location = new System.Drawing.Point(4, 22);
            this.tabGestures.Name = "tabGestures";
            this.tabGestures.Size = new System.Drawing.Size(392, 678);
            this.tabGestures.TabIndex = 3;
            this.tabGestures.Text = "Gestures";
            // 
            // lblGrenadeDevice
            // 
            this.lblGrenadeDevice.ForeColor = System.Drawing.Color.White;
            this.lblGrenadeDevice.Location = new System.Drawing.Point(20, 532);
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
            this.optGrenadeDevice.Location = new System.Drawing.Point(140, 532);
            this.optGrenadeDevice.Name = "optGrenadeDevice";
            this.optGrenadeDevice.Size = new System.Drawing.Size(200, 21);
            this.optGrenadeDevice.TabIndex = 6;
            // 
            // lblShakeDevice
            // 
            this.lblShakeDevice.ForeColor = System.Drawing.Color.White;
            this.lblShakeDevice.Location = new System.Drawing.Point(20, 452);
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
            this.optShakeDevice.Location = new System.Drawing.Point(140, 452);
            this.optShakeDevice.Name = "optShakeDevice";
            this.optShakeDevice.Size = new System.Drawing.Size(200, 21);
            this.optShakeDevice.TabIndex = 4;
            // 
            // lblGesturesDevSeparator
            // 
            this.lblGesturesDevSeparator.AutoSize = true;
            this.lblGesturesDevSeparator.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblGesturesDevSeparator.ForeColor = System.Drawing.Color.Goldenrod;
            this.lblGesturesDevSeparator.Location = new System.Drawing.Point(137, 355);
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
            this.optShakeSensitivity.Location = new System.Drawing.Point(140, 412);
            this.optShakeSensitivity.Name = "optShakeSensitivity";
            this.optShakeSensitivity.Size = new System.Drawing.Size(200, 21);
            this.optShakeSensitivity.TabIndex = 2;
            // 
            // lblShakeSensitivity
            // 
            this.lblShakeSensitivity.ForeColor = System.Drawing.Color.White;
            this.lblShakeSensitivity.Location = new System.Drawing.Point(20, 412);
            this.lblShakeSensitivity.Name = "lblShakeSensitivity";
            this.lblShakeSensitivity.Size = new System.Drawing.Size(120, 25);
            this.lblShakeSensitivity.TabIndex = 0;
            this.lblShakeSensitivity.Text = "Shake Sensitivity:";
            // 
            // optEnableGrenadeGesture
            // 
            this.optEnableGrenadeGesture.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optEnableGrenadeGesture.ForeColor = System.Drawing.Color.White;
            this.optEnableGrenadeGesture.Location = new System.Drawing.Point(20, 497);
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
            this.optEnableShakeReload.Location = new System.Drawing.Point(20, 372);
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
            // chkReloadRumble
            // 
            this.chkReloadRumble.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkReloadRumble.ForeColor = System.Drawing.Color.White;
            this.chkReloadRumble.Location = new System.Drawing.Point(20, 100);
            this.chkReloadRumble.Name = "chkReloadRumble";
            this.chkReloadRumble.Size = new System.Drawing.Size(200, 25);
            this.chkReloadRumble.TabIndex = 1;
            this.chkReloadRumble.Text = "Reload Rumble";
            this.chkReloadRumble.UseVisualStyleBackColor = true;
            // 
            // lblReloadRumbleIntensity
            // 
            this.lblReloadRumbleIntensity.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReloadRumbleIntensity.ForeColor = System.Drawing.Color.White;
            this.lblReloadRumbleIntensity.Location = new System.Drawing.Point(20, 130);
            this.lblReloadRumbleIntensity.Name = "lblReloadRumbleIntensity";
            this.lblReloadRumbleIntensity.Size = new System.Drawing.Size(110, 25);
            this.lblReloadRumbleIntensity.TabIndex = 2;
            this.lblReloadRumbleIntensity.Text = "Intensity:";
            this.lblReloadRumbleIntensity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // trkReloadRumbleIntensity
            // 
            this.trkReloadRumbleIntensity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.trkReloadRumbleIntensity.LargeChange = 10;
            this.trkReloadRumbleIntensity.Location = new System.Drawing.Point(140, 125);
            this.trkReloadRumbleIntensity.Maximum = 100;
            this.trkReloadRumbleIntensity.Minimum = 0;
            this.trkReloadRumbleIntensity.Name = "trkReloadRumbleIntensity";
            this.trkReloadRumbleIntensity.Size = new System.Drawing.Size(200, 45);
            this.trkReloadRumbleIntensity.SmallChange = 5;
            this.trkReloadRumbleIntensity.TabIndex = 2;
            this.trkReloadRumbleIntensity.TickFrequency = 10;
            // 
            // lblReloadRumbleStyle
            // 
            this.lblReloadRumbleStyle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReloadRumbleStyle.ForeColor = System.Drawing.Color.White;
            this.lblReloadRumbleStyle.Location = new System.Drawing.Point(20, 180);
            this.lblReloadRumbleStyle.Name = "lblReloadRumbleStyle";
            this.lblReloadRumbleStyle.Size = new System.Drawing.Size(110, 25);
            this.lblReloadRumbleStyle.TabIndex = 3;
            this.lblReloadRumbleStyle.Text = "Style:";
            this.lblReloadRumbleStyle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboReloadRumbleStyle
            // 
            this.cboReloadRumbleStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cboReloadRumbleStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReloadRumbleStyle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboReloadRumbleStyle.ForeColor = System.Drawing.Color.White;
            this.cboReloadRumbleStyle.FormattingEnabled = true;
            this.cboReloadRumbleStyle.Items.AddRange(new object[] {
            "Ratchet (mechanical)",
            "Short",
            "Long",
            "Custom"});
            this.cboReloadRumbleStyle.Location = new System.Drawing.Point(140, 180);
            this.cboReloadRumbleStyle.Name = "cboReloadRumbleStyle";
            this.cboReloadRumbleStyle.Size = new System.Drawing.Size(200, 21);
            this.cboReloadRumbleStyle.TabIndex = 4;
            // 
            // lblReloadRumbleTicks
            // 
            this.lblReloadRumbleTicks.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReloadRumbleTicks.ForeColor = System.Drawing.Color.White;
            this.lblReloadRumbleTicks.Location = new System.Drawing.Point(20, 215);
            this.lblReloadRumbleTicks.Name = "lblReloadRumbleTicks";
            this.lblReloadRumbleTicks.Size = new System.Drawing.Size(115, 25);
            this.lblReloadRumbleTicks.TabIndex = 5;
            this.lblReloadRumbleTicks.Text = "Ticks (Custom):";
            this.lblReloadRumbleTicks.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // trkReloadRumbleTicks
            // 
            this.trkReloadRumbleTicks.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.trkReloadRumbleTicks.LargeChange = 1;
            this.trkReloadRumbleTicks.Location = new System.Drawing.Point(140, 210);
            this.trkReloadRumbleTicks.Maximum = 10;
            this.trkReloadRumbleTicks.Minimum = 1;
            this.trkReloadRumbleTicks.Name = "trkReloadRumbleTicks";
            this.trkReloadRumbleTicks.Size = new System.Drawing.Size(200, 45);
            this.trkReloadRumbleTicks.SmallChange = 1;
            this.trkReloadRumbleTicks.TabIndex = 5;
            this.trkReloadRumbleTicks.TickFrequency = 1;
            this.trkReloadRumbleTicks.Value = 4;
            // 
            // lblReloadRumbleTickOn
            // 
            this.lblReloadRumbleTickOn.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReloadRumbleTickOn.ForeColor = System.Drawing.Color.White;
            this.lblReloadRumbleTickOn.Location = new System.Drawing.Point(20, 265);
            this.lblReloadRumbleTickOn.Name = "lblReloadRumbleTickOn";
            this.lblReloadRumbleTickOn.Size = new System.Drawing.Size(115, 25);
            this.lblReloadRumbleTickOn.TabIndex = 6;
            this.lblReloadRumbleTickOn.Text = "Tic ON (ms):";
            this.lblReloadRumbleTickOn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudReloadRumbleTickOnMs
            // 
            this.nudReloadRumbleTickOnMs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudReloadRumbleTickOnMs.ForeColor = System.Drawing.Color.White;
            this.nudReloadRumbleTickOnMs.Location = new System.Drawing.Point(140, 265);
            this.nudReloadRumbleTickOnMs.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudReloadRumbleTickOnMs.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudReloadRumbleTickOnMs.Name = "nudReloadRumbleTickOnMs";
            this.nudReloadRumbleTickOnMs.Size = new System.Drawing.Size(80, 23);
            this.nudReloadRumbleTickOnMs.TabIndex = 7;
            this.nudReloadRumbleTickOnMs.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // lblReloadRumbleTickOff
            // 
            this.lblReloadRumbleTickOff.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReloadRumbleTickOff.ForeColor = System.Drawing.Color.White;
            this.lblReloadRumbleTickOff.Location = new System.Drawing.Point(20, 305);
            this.lblReloadRumbleTickOff.Name = "lblReloadRumbleTickOff";
            this.lblReloadRumbleTickOff.Size = new System.Drawing.Size(115, 25);
            this.lblReloadRumbleTickOff.TabIndex = 8;
            this.lblReloadRumbleTickOff.Text = "Tic OFF (ms):";
            this.lblReloadRumbleTickOff.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudReloadRumbleTickOffMs
            // 
            this.nudReloadRumbleTickOffMs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudReloadRumbleTickOffMs.ForeColor = System.Drawing.Color.White;
            this.nudReloadRumbleTickOffMs.Location = new System.Drawing.Point(140, 305);
            this.nudReloadRumbleTickOffMs.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudReloadRumbleTickOffMs.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudReloadRumbleTickOffMs.Name = "nudReloadRumbleTickOffMs";
            this.nudReloadRumbleTickOffMs.Size = new System.Drawing.Size(80, 23);
            this.nudReloadRumbleTickOffMs.TabIndex = 9;
            this.nudReloadRumbleTickOffMs.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
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
            // tabEsScripts
            // 
            this.tabEsScripts.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.tabEsScripts.Controls.Add(this.lblEsTitle);
            this.tabEsScripts.Controls.Add(this.lblEsInfo);
            this.tabEsScripts.Controls.Add(this.chkEsScripts);
            this.tabEsScripts.Controls.Add(this.lblEsScriptsStatus);
            this.tabEsScripts.Controls.Add(this.chkEsTileHotkey);
            this.tabEsScripts.Controls.Add(this.lblEsTileDelay);
            this.tabEsScripts.Controls.Add(this.numEsTileDelay);
            this.tabEsScripts.Controls.Add(this.lblEsTileInfo);
            this.tabEsScripts.Controls.Add(this.optLockModeOnGameStart);
            this.tabEsScripts.Controls.Add(this.lblLockModeDesc);
            this.tabEsScripts.Location = new System.Drawing.Point(4, 22);
            this.tabEsScripts.Name = "tabEsScripts";
            this.tabEsScripts.Padding = new System.Windows.Forms.Padding(3);
            this.tabEsScripts.Size = new System.Drawing.Size(392, 678);
            this.tabEsScripts.TabIndex = 4;
            this.tabEsScripts.Text = "ES Scripts";
            // 
            // lblEsTitle
            // 
            this.lblEsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblEsTitle.ForeColor = System.Drawing.Color.White;
            this.lblEsTitle.Location = new System.Drawing.Point(20, 18);
            this.lblEsTitle.Name = "lblEsTitle";
            this.lblEsTitle.Size = new System.Drawing.Size(350, 25);
            this.lblEsTitle.TabIndex = 0;
            this.lblEsTitle.Text = "EmulationStation (RetroBat) Integration";
            // 
            // lblEsInfo
            // 
            this.lblEsInfo.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblEsInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblEsInfo.Location = new System.Drawing.Point(20, 45);
            this.lblEsInfo.Name = "lblEsInfo";
            this.lblEsInfo.Size = new System.Drawing.Size(350, 45);
            this.lblEsInfo.TabIndex = 1;
            this.lblEsInfo.Text = "Installs two scripts in RetroBat (game-start / system-selected) so WiimoteGun receives the launched game and the selected system, allowing per-game profile auto-load on shared emulators.";
            // 
            // chkEsScripts
            // 
            this.chkEsScripts.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkEsScripts.ForeColor = System.Drawing.Color.White;
            this.chkEsScripts.Location = new System.Drawing.Point(20, 100);
            this.chkEsScripts.Name = "chkEsScripts";
            this.chkEsScripts.Size = new System.Drawing.Size(350, 25);
            this.chkEsScripts.TabIndex = 2;
            this.chkEsScripts.Text = "Enable ES scripts (unchecking removes them)";
            this.chkEsScripts.UseVisualStyleBackColor = true;
            // 
            // lblEsScriptsStatus
            // 
            this.lblEsScriptsStatus.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblEsScriptsStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(200)))), ((int)(((byte)(120)))));
            this.lblEsScriptsStatus.Location = new System.Drawing.Point(35, 127);
            this.lblEsScriptsStatus.Name = "lblEsScriptsStatus";
            this.lblEsScriptsStatus.Size = new System.Drawing.Size(350, 35);
            this.lblEsScriptsStatus.TabIndex = 3;
            this.lblEsScriptsStatus.Text = "";
            // 
            // chkEsTileHotkey
            // 
            this.chkEsTileHotkey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkEsTileHotkey.ForeColor = System.Drawing.Color.White;
            this.chkEsTileHotkey.Location = new System.Drawing.Point(20, 170);
            this.chkEsTileHotkey.Name = "chkEsTileHotkey";
            this.chkEsTileHotkey.Size = new System.Drawing.Size(350, 25);
            this.chkEsTileHotkey.TabIndex = 4;
            this.chkEsTileHotkey.Text = "Long-press [+] (any Wiimote) opens the profiles tile modal";
            this.chkEsTileHotkey.UseVisualStyleBackColor = true;
            // 
            // lblEsTileDelay
            // 
            this.lblEsTileDelay.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblEsTileDelay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblEsTileDelay.Location = new System.Drawing.Point(35, 200);
            this.lblEsTileDelay.Name = "lblEsTileDelay";
            this.lblEsTileDelay.Size = new System.Drawing.Size(170, 20);
            this.lblEsTileDelay.TabIndex = 5;
            this.lblEsTileDelay.Text = "Long-press delay (seconds):";
            // 
            // numEsTileDelay
            // 
            this.numEsTileDelay.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numEsTileDelay.Location = new System.Drawing.Point(210, 197);
            this.numEsTileDelay.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numEsTileDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numEsTileDelay.Name = "numEsTileDelay";
            this.numEsTileDelay.Size = new System.Drawing.Size(60, 23);
            this.numEsTileDelay.TabIndex = 6;
            this.numEsTileDelay.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblEsTileInfo
            // 
            this.lblEsTileInfo.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblEsTileInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblEsTileInfo.Location = new System.Drawing.Point(20, 225);
            this.lblEsTileInfo.Name = "lblEsTileInfo";
            this.lblEsTileInfo.Size = new System.Drawing.Size(350, 45);
            this.lblEsTileInfo.TabIndex = 7;
            this.lblEsTileInfo.Text = "The delay avoids conflicts when [+] is also used as a hotkey/trigger button. The tile modal lists Mouse/GamePad profiles filtered by the current ES system and toggles XInput/DInput before launching a game.";
            // 
            // optLockModeOnGameStart
            // 
            this.optLockModeOnGameStart.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.optLockModeOnGameStart.ForeColor = System.Drawing.Color.White;
            this.optLockModeOnGameStart.Location = new System.Drawing.Point(20, 280);
            this.optLockModeOnGameStart.Name = "optLockModeOnGameStart";
            this.optLockModeOnGameStart.Size = new System.Drawing.Size(350, 25);
            this.optLockModeOnGameStart.TabIndex = 8;
            this.optLockModeOnGameStart.Text = "Lock active mode on game launch (game-start)";
            this.optLockModeOnGameStart.UseVisualStyleBackColor = true;
            // 
            // lblLockModeDesc
            // 
            this.lblLockModeDesc.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblLockModeDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblLockModeDesc.Location = new System.Drawing.Point(38, 308);
            this.lblLockModeDesc.Name = "lblLockModeDesc";
            this.lblLockModeDesc.Size = new System.Drawing.Size(340, 32);
            this.lblLockModeDesc.TabIndex = 9;
            this.lblLockModeDesc.Text = "Prevents accidental mode changes with the Home button during gameplay.";
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
            this.optStandaloneMode.Text = "Standalone / Gamepad multiplayer";
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
            ((System.ComponentModel.ISupportInitialize)(this.optIRSmoothingStrengthV2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRExtrapolationStrengthV2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optVirtualPollingRateV2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optIRSensitivity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optMonitorId)).EndInit();
            // [V55y] TrackBar EndInit (Designer norm)
            ((System.ComponentModel.ISupportInitialize)(this.trkReloadRumbleIntensity)).EndInit();
            // [V55z] Custom style controls EndInit (Designer norm)
            ((System.ComponentModel.ISupportInitialize)(this.trkReloadRumbleTicks)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReloadRumbleTickOnMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReloadRumbleTickOffMs)).EndInit();
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
        private System.Windows.Forms.TabPage tabEsScripts;
        private System.Windows.Forms.Label lblEsTitle;
        private System.Windows.Forms.Label lblEsInfo;
        private System.Windows.Forms.CheckBox chkEsScripts;
        private System.Windows.Forms.Label lblEsScriptsStatus;
        private System.Windows.Forms.CheckBox chkEsTileHotkey;
        private System.Windows.Forms.Label lblEsTileDelay;
        private System.Windows.Forms.NumericUpDown numEsTileDelay;
        private System.Windows.Forms.Label lblEsTileInfo;
        private System.Windows.Forms.CheckBox optLockModeOnGameStart;
        private System.Windows.Forms.Label lblLockModeDesc;
        private System.Windows.Forms.Button btnTabEsScripts;
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
        private System.Windows.Forms.CheckBox optAutoBtReset;
        private System.Windows.Forms.NumericUpDown numBtResetDelay;
        private System.Windows.Forms.Label lblBtResetDelay;
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
        // [V55y] Reload rumble controls (EN/FR: Contrôles vibration rechargement)
        private System.Windows.Forms.CheckBox chkReloadRumble;
        private System.Windows.Forms.Label lblReloadRumbleIntensity;
        private System.Windows.Forms.TrackBar trkReloadRumbleIntensity;
        private System.Windows.Forms.Label lblReloadRumbleStyle;
        private System.Windows.Forms.ComboBox cboReloadRumbleStyle;
        // [V55z] Custom style controls (EN/FR: Contrôles du style personnalisé)
        private System.Windows.Forms.Label lblReloadRumbleTicks;
        private System.Windows.Forms.TrackBar trkReloadRumbleTicks;
        private System.Windows.Forms.Label lblReloadRumbleTickOn;
        private System.Windows.Forms.NumericUpDown nudReloadRumbleTickOnMs;
        private System.Windows.Forms.Label lblReloadRumbleTickOff;
        private System.Windows.Forms.NumericUpDown nudReloadRumbleTickOffMs;
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
        private System.Windows.Forms.Label lblV2ModelNote;
        private System.Windows.Forms.Label lblIRSmoothingStrengthV2;
        private System.Windows.Forms.NumericUpDown optIRSmoothingStrengthV2;
        private System.Windows.Forms.Label lblIRExtrapolationStrengthV2;
        private System.Windows.Forms.NumericUpDown optIRExtrapolationStrengthV2;
        private System.Windows.Forms.Label lblVirtualPollingRateV2;
        private System.Windows.Forms.NumericUpDown optVirtualPollingRateV2;
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
