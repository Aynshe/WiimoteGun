namespace WiimoteGun.Controls
{
    partial class AssignControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && _assignRefreshTimer != null)
            {
                _assignRefreshTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelAssignContent = new System.Windows.Forms.Panel();
            this.panelPlayer1 = new System.Windows.Forms.Panel();
            this.btnSwapDown1 = new System.Windows.Forms.Button();
            this.lblMs1 = new System.Windows.Forms.Label();
            this.nudDuration1 = new System.Windows.Forms.NumericUpDown();
            this.lblDuration1 = new System.Windows.Forms.Label();
            this.trkIntensity1 = new System.Windows.Forms.TrackBar();
            this.lblIntensityVal1 = new System.Windows.Forms.Label();
            this.lblIntensity1 = new System.Windows.Forms.Label();
            this.chkRumble1 = new System.Windows.Forms.CheckBox();
            this.lblRumble1 = new System.Windows.Forms.Label();
            this.btnLock1 = new System.Windows.Forms.Button();
            this.btnDevices1 = new System.Windows.Forms.Button();
            this.btnIdentify1 = new System.Windows.Forms.Button();
            this.lblBattery1 = new System.Windows.Forms.Label();
            this.lblDeviceInfo1 = new System.Windows.Forms.Label();
            this.lblStatus1 = new System.Windows.Forms.Label();
            this.lblPlayerName1 = new System.Windows.Forms.Label();
            this.panelPlayer2 = new System.Windows.Forms.Panel();
            this.btnSwapDown2 = new System.Windows.Forms.Button();
            this.btnSwapUp2 = new System.Windows.Forms.Button();
            this.lblMs2 = new System.Windows.Forms.Label();
            this.nudDuration2 = new System.Windows.Forms.NumericUpDown();
            this.lblDuration2 = new System.Windows.Forms.Label();
            this.trkIntensity2 = new System.Windows.Forms.TrackBar();
            this.lblIntensityVal2 = new System.Windows.Forms.Label();
            this.lblIntensity2 = new System.Windows.Forms.Label();
            this.chkRumble2 = new System.Windows.Forms.CheckBox();
            this.lblRumble2 = new System.Windows.Forms.Label();
            this.btnLock2 = new System.Windows.Forms.Button();
            this.btnDevices2 = new System.Windows.Forms.Button();
            this.btnIdentify2 = new System.Windows.Forms.Button();
            this.lblBattery2 = new System.Windows.Forms.Label();
            this.lblDeviceInfo2 = new System.Windows.Forms.Label();
            this.lblStatus2 = new System.Windows.Forms.Label();
            this.lblPlayerName2 = new System.Windows.Forms.Label();
            this.panelPlayer3 = new System.Windows.Forms.Panel();
            this.btnSwapDown3 = new System.Windows.Forms.Button();
            this.btnSwapUp3 = new System.Windows.Forms.Button();
            this.lblMs3 = new System.Windows.Forms.Label();
            this.nudDuration3 = new System.Windows.Forms.NumericUpDown();
            this.lblDuration3 = new System.Windows.Forms.Label();
            this.trkIntensity3 = new System.Windows.Forms.TrackBar();
            this.lblIntensityVal3 = new System.Windows.Forms.Label();
            this.lblIntensity3 = new System.Windows.Forms.Label();
            this.chkRumble3 = new System.Windows.Forms.CheckBox();
            this.lblRumble3 = new System.Windows.Forms.Label();
            this.btnLock3 = new System.Windows.Forms.Button();
            this.btnDevices3 = new System.Windows.Forms.Button();
            this.btnIdentify3 = new System.Windows.Forms.Button();
            this.lblBattery3 = new System.Windows.Forms.Label();
            this.lblDeviceInfo3 = new System.Windows.Forms.Label();
            this.lblStatus3 = new System.Windows.Forms.Label();
            this.lblPlayerName3 = new System.Windows.Forms.Label();
            this.panelPlayer4 = new System.Windows.Forms.Panel();
            this.btnSwapUp4 = new System.Windows.Forms.Button();
            this.lblMs4 = new System.Windows.Forms.Label();
            this.nudDuration4 = new System.Windows.Forms.NumericUpDown();
            this.lblDuration4 = new System.Windows.Forms.Label();
            this.trkIntensity4 = new System.Windows.Forms.TrackBar();
            this.lblIntensityVal4 = new System.Windows.Forms.Label();
            this.lblIntensity4 = new System.Windows.Forms.Label();
            this.chkRumble4 = new System.Windows.Forms.CheckBox();
            this.lblRumble4 = new System.Windows.Forms.Label();
            this.btnLock4 = new System.Windows.Forms.Button();
            this.btnDevices4 = new System.Windows.Forms.Button();
            this.btnIdentify4 = new System.Windows.Forms.Button();
            this.lblBattery4 = new System.Windows.Forms.Label();
            this.lblDeviceInfo4 = new System.Windows.Forms.Label();
            this.lblStatus4 = new System.Windows.Forms.Label();
            this.lblPlayerName4 = new System.Windows.Forms.Label();
            this.btnBack = new System.Windows.Forms.Button();
            this.panelAssignContent.SuspendLayout();
            this.panelPlayer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity1)).BeginInit();
            this.panelPlayer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity2)).BeginInit();
            this.panelPlayer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity3)).BeginInit();
            this.panelPlayer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity4)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(10, 10);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(540, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "📡 Assign Wiimotes";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelAssignContent
            // 
            this.panelAssignContent.AutoScroll = true;
            this.panelAssignContent.BackColor = System.Drawing.Color.Transparent;
            this.panelAssignContent.Controls.Add(this.panelPlayer1);
            this.panelAssignContent.Controls.Add(this.panelPlayer2);
            this.panelAssignContent.Controls.Add(this.panelPlayer3);
            this.panelAssignContent.Controls.Add(this.panelPlayer4);
            this.panelAssignContent.Location = new System.Drawing.Point(10, 70);
            this.panelAssignContent.Name = "panelAssignContent";
            this.panelAssignContent.Size = new System.Drawing.Size(540, 682);
            this.panelAssignContent.TabIndex = 1;
            // 
            // panelPlayer1
            // 
            this.panelPlayer1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panelPlayer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer1.Controls.Add(this.btnSwapDown1);
            this.panelPlayer1.Controls.Add(this.lblMs1);
            this.panelPlayer1.Controls.Add(this.nudDuration1);
            this.panelPlayer1.Controls.Add(this.lblDuration1);
            this.panelPlayer1.Controls.Add(this.trkIntensity1);
            this.panelPlayer1.Controls.Add(this.lblIntensityVal1);
            this.panelPlayer1.Controls.Add(this.lblIntensity1);
            this.panelPlayer1.Controls.Add(this.chkRumble1);
            this.panelPlayer1.Controls.Add(this.lblRumble1);
            this.panelPlayer1.Controls.Add(this.btnLock1);
            this.panelPlayer1.Controls.Add(this.btnDevices1);
            this.panelPlayer1.Controls.Add(this.btnIdentify1);
            this.panelPlayer1.Controls.Add(this.lblBattery1);
            this.panelPlayer1.Controls.Add(this.lblDeviceInfo1);
            this.panelPlayer1.Controls.Add(this.lblStatus1);
            this.panelPlayer1.Controls.Add(this.lblPlayerName1);
            this.panelPlayer1.Location = new System.Drawing.Point(10, 5);
            this.panelPlayer1.Name = "panelPlayer1";
            this.panelPlayer1.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer1.TabIndex = 1;
            // 
            // btnSwapDown1
            // 
            this.btnSwapDown1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapDown1.Enabled = false;
            this.btnSwapDown1.FlatAppearance.BorderSize = 0;
            this.btnSwapDown1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapDown1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapDown1.ForeColor = System.Drawing.Color.White;
            this.btnSwapDown1.Location = new System.Drawing.Point(14, 61);
            this.btnSwapDown1.Name = "btnSwapDown1";
            this.btnSwapDown1.Size = new System.Drawing.Size(85, 23);
            this.btnSwapDown1.TabIndex = 0;
            this.btnSwapDown1.Text = "▼ P2";
            this.btnSwapDown1.UseVisualStyleBackColor = false;
            // 
            // lblMs1
            // 
            this.lblMs1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblMs1.Location = new System.Drawing.Point(440, 120);
            this.lblMs1.Name = "lblMs1";
            this.lblMs1.Size = new System.Drawing.Size(25, 20);
            this.lblMs1.TabIndex = 1;
            this.lblMs1.Text = "ms";
            // 
            // nudDuration1
            // 
            this.nudDuration1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudDuration1.ForeColor = System.Drawing.Color.White;
            this.nudDuration1.Location = new System.Drawing.Point(375, 117);
            this.nudDuration1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudDuration1.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDuration1.Name = "nudDuration1";
            this.nudDuration1.Size = new System.Drawing.Size(60, 20);
            this.nudDuration1.TabIndex = 2;
            this.nudDuration1.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblDuration1
            // 
            this.lblDuration1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblDuration1.Location = new System.Drawing.Point(310, 120);
            this.lblDuration1.Name = "lblDuration1";
            this.lblDuration1.Size = new System.Drawing.Size(60, 20);
            this.lblDuration1.TabIndex = 3;
            this.lblDuration1.Text = "Duration:";
            // 
            // trkIntensity1
            // 
            this.trkIntensity1.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity1.Maximum = 100;
            this.trkIntensity1.Name = "trkIntensity1";
            this.trkIntensity1.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity1.TabIndex = 4;
            this.trkIntensity1.TickFrequency = 10;
            // 
            // lblIntensityVal1
            // 
            this.lblIntensityVal1.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal1.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal1.Name = "lblIntensityVal1";
            this.lblIntensityVal1.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal1.TabIndex = 5;
            this.lblIntensityVal1.Text = "50%";
            // 
            // lblIntensity1
            // 
            this.lblIntensity1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblIntensity1.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity1.Name = "lblIntensity1";
            this.lblIntensity1.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity1.TabIndex = 6;
            this.lblIntensity1.Text = "Intensity:";
            // 
            // chkRumble1
            // 
            this.chkRumble1.ForeColor = System.Drawing.Color.White;
            this.chkRumble1.Location = new System.Drawing.Point(130, 90);
            this.chkRumble1.Name = "chkRumble1";
            this.chkRumble1.Size = new System.Drawing.Size(70, 20);
            this.chkRumble1.TabIndex = 7;
            this.chkRumble1.Text = "Enable";
            // 
            // lblRumble1
            // 
            this.lblRumble1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblRumble1.Location = new System.Drawing.Point(10, 90);
            this.lblRumble1.Name = "lblRumble1";
            this.lblRumble1.Size = new System.Drawing.Size(120, 20);
            this.lblRumble1.TabIndex = 8;
            this.lblRumble1.Text = "Rumble Settings:";
            // 
            // btnLock1
            // 
            this.btnLock1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnLock1.FlatAppearance.BorderSize = 0;
            this.btnLock1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLock1.ForeColor = System.Drawing.Color.White;
            this.btnLock1.Location = new System.Drawing.Point(400, 81);
            this.btnLock1.Name = "btnLock1";
            this.btnLock1.Size = new System.Drawing.Size(90, 30);
            this.btnLock1.TabIndex = 9;
            this.btnLock1.Text = "🔓 Unlock";
            this.btnLock1.UseVisualStyleBackColor = false;
            // 
            // btnDevices1
            // 
            this.btnDevices1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnDevices1.FlatAppearance.BorderSize = 0;
            this.btnDevices1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices1.ForeColor = System.Drawing.Color.White;
            this.btnDevices1.Location = new System.Drawing.Point(400, 44);
            this.btnDevices1.Name = "btnDevices1";
            this.btnDevices1.Size = new System.Drawing.Size(90, 30);
            this.btnDevices1.TabIndex = 10;
            this.btnDevices1.Text = "⚙️ Devices";
            this.btnDevices1.UseVisualStyleBackColor = false;
            // 
            // btnIdentify1
            // 
            this.btnIdentify1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnIdentify1.FlatAppearance.BorderSize = 0;
            this.btnIdentify1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify1.ForeColor = System.Drawing.Color.White;
            this.btnIdentify1.Location = new System.Drawing.Point(400, 6);
            this.btnIdentify1.Name = "btnIdentify1";
            this.btnIdentify1.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify1.TabIndex = 11;
            this.btnIdentify1.Text = "📳 Identify";
            this.btnIdentify1.UseVisualStyleBackColor = false;
            // 
            // lblBattery1
            // 
            this.lblBattery1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery1.ForeColor = System.Drawing.Color.White;
            this.lblBattery1.Location = new System.Drawing.Point(120, 60);
            this.lblBattery1.Name = "lblBattery1";
            this.lblBattery1.Size = new System.Drawing.Size(100, 20);
            this.lblBattery1.TabIndex = 12;
            this.lblBattery1.Tag = "Battery";
            this.lblBattery1.Text = "🔋 --%";
            // 
            // lblMac1
            // 
            this.lblDeviceInfo1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDeviceInfo1.ForeColor = System.Drawing.Color.Gray;
            this.lblDeviceInfo1.Location = new System.Drawing.Point(120, 40);
            this.lblDeviceInfo1.Name = "lblDeviceInfo1";
            this.lblDeviceInfo1.Size = new System.Drawing.Size(200, 20);
            this.lblDeviceInfo1.TabIndex = 13;
            this.lblDeviceInfo1.Text = "ID: --:--:--:--:--:--";
            // 
            // lblStatus1
            // 
            this.lblStatus1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus1.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus1.Location = new System.Drawing.Point(120, 15);
            this.lblStatus1.Name = "lblStatus1";
            this.lblStatus1.Size = new System.Drawing.Size(250, 25);
            this.lblStatus1.TabIndex = 14;
            this.lblStatus1.Text = "Waiting for connection...";
            // 
            // lblPlayerName1
            // 
            this.lblPlayerName1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblPlayerName1.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName1.Name = "lblPlayerName1";
            this.lblPlayerName1.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName1.TabIndex = 15;
            this.lblPlayerName1.Text = "Player 1";
            // 
            // panelPlayer2
            // 
            this.panelPlayer2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panelPlayer2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer2.Controls.Add(this.btnSwapDown2);
            this.panelPlayer2.Controls.Add(this.btnSwapUp2);
            this.panelPlayer2.Controls.Add(this.lblMs2);
            this.panelPlayer2.Controls.Add(this.nudDuration2);
            this.panelPlayer2.Controls.Add(this.lblDuration2);
            this.panelPlayer2.Controls.Add(this.trkIntensity2);
            this.panelPlayer2.Controls.Add(this.lblIntensityVal2);
            this.panelPlayer2.Controls.Add(this.lblIntensity2);
            this.panelPlayer2.Controls.Add(this.chkRumble2);
            this.panelPlayer2.Controls.Add(this.lblRumble2);
            this.panelPlayer2.Controls.Add(this.btnLock2);
            this.panelPlayer2.Controls.Add(this.btnDevices2);
            this.panelPlayer2.Controls.Add(this.btnIdentify2);
            this.panelPlayer2.Controls.Add(this.lblBattery2);
            this.panelPlayer2.Controls.Add(this.lblDeviceInfo2);
            this.panelPlayer2.Controls.Add(this.lblStatus2);
            this.panelPlayer2.Controls.Add(this.lblPlayerName2);
            this.panelPlayer2.Location = new System.Drawing.Point(10, 170);
            this.panelPlayer2.Name = "panelPlayer2";
            this.panelPlayer2.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer2.TabIndex = 2;
            // 
            // btnSwapDown2
            // 
            this.btnSwapDown2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapDown2.Enabled = false;
            this.btnSwapDown2.FlatAppearance.BorderSize = 0;
            this.btnSwapDown2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapDown2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapDown2.ForeColor = System.Drawing.Color.White;
            this.btnSwapDown2.Location = new System.Drawing.Point(14, 61);
            this.btnSwapDown2.Name = "btnSwapDown2";
            this.btnSwapDown2.Size = new System.Drawing.Size(85, 23);
            this.btnSwapDown2.TabIndex = 0;
            this.btnSwapDown2.Text = "▼ P3";
            this.btnSwapDown2.UseVisualStyleBackColor = false;
            // 
            // btnSwapUp2
            // 
            this.btnSwapUp2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapUp2.Enabled = false;
            this.btnSwapUp2.FlatAppearance.BorderSize = 0;
            this.btnSwapUp2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapUp2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapUp2.ForeColor = System.Drawing.Color.White;
            this.btnSwapUp2.Location = new System.Drawing.Point(14, 37);
            this.btnSwapUp2.Name = "btnSwapUp2";
            this.btnSwapUp2.Size = new System.Drawing.Size(85, 23);
            this.btnSwapUp2.TabIndex = 1;
            this.btnSwapUp2.Text = "▲ P1";
            this.btnSwapUp2.UseVisualStyleBackColor = false;
            // 
            // lblMs2
            // 
            this.lblMs2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblMs2.Location = new System.Drawing.Point(440, 120);
            this.lblMs2.Name = "lblMs2";
            this.lblMs2.Size = new System.Drawing.Size(25, 20);
            this.lblMs2.TabIndex = 2;
            this.lblMs2.Text = "ms";
            // 
            // nudDuration2
            // 
            this.nudDuration2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudDuration2.ForeColor = System.Drawing.Color.White;
            this.nudDuration2.Location = new System.Drawing.Point(375, 117);
            this.nudDuration2.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudDuration2.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDuration2.Name = "nudDuration2";
            this.nudDuration2.Size = new System.Drawing.Size(60, 20);
            this.nudDuration2.TabIndex = 3;
            this.nudDuration2.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblDuration2
            // 
            this.lblDuration2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblDuration2.Location = new System.Drawing.Point(310, 120);
            this.lblDuration2.Name = "lblDuration2";
            this.lblDuration2.Size = new System.Drawing.Size(60, 20);
            this.lblDuration2.TabIndex = 4;
            this.lblDuration2.Text = "Duration:";
            // 
            // trkIntensity2
            // 
            this.trkIntensity2.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity2.Maximum = 100;
            this.trkIntensity2.Name = "trkIntensity2";
            this.trkIntensity2.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity2.TabIndex = 5;
            this.trkIntensity2.TickFrequency = 10;
            // 
            // lblIntensityVal2
            // 
            this.lblIntensityVal2.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal2.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal2.Name = "lblIntensityVal2";
            this.lblIntensityVal2.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal2.TabIndex = 6;
            this.lblIntensityVal2.Text = "50%";
            // 
            // lblIntensity2
            // 
            this.lblIntensity2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblIntensity2.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity2.Name = "lblIntensity2";
            this.lblIntensity2.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity2.TabIndex = 7;
            this.lblIntensity2.Text = "Intensity:";
            // 
            // chkRumble2
            // 
            this.chkRumble2.ForeColor = System.Drawing.Color.White;
            this.chkRumble2.Location = new System.Drawing.Point(130, 90);
            this.chkRumble2.Name = "chkRumble2";
            this.chkRumble2.Size = new System.Drawing.Size(70, 20);
            this.chkRumble2.TabIndex = 8;
            this.chkRumble2.Text = "Enable";
            // 
            // lblRumble2
            // 
            this.lblRumble2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblRumble2.Location = new System.Drawing.Point(10, 90);
            this.lblRumble2.Name = "lblRumble2";
            this.lblRumble2.Size = new System.Drawing.Size(120, 20);
            this.lblRumble2.TabIndex = 9;
            this.lblRumble2.Text = "Rumble Settings:";
            // 
            // btnLock2
            // 
            this.btnLock2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnLock2.FlatAppearance.BorderSize = 0;
            this.btnLock2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLock2.ForeColor = System.Drawing.Color.White;
            this.btnLock2.Location = new System.Drawing.Point(400, 81);
            this.btnLock2.Name = "btnLock2";
            this.btnLock2.Size = new System.Drawing.Size(90, 30);
            this.btnLock2.TabIndex = 10;
            this.btnLock2.Text = "🔓 Unlock";
            this.btnLock2.UseVisualStyleBackColor = false;
            // 
            // btnDevices2
            // 
            this.btnDevices2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnDevices2.FlatAppearance.BorderSize = 0;
            this.btnDevices2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices2.ForeColor = System.Drawing.Color.White;
            this.btnDevices2.Location = new System.Drawing.Point(400, 44);
            this.btnDevices2.Name = "btnDevices2";
            this.btnDevices2.Size = new System.Drawing.Size(90, 30);
            this.btnDevices2.TabIndex = 11;
            this.btnDevices2.Text = "⚙️ Devices";
            this.btnDevices2.UseVisualStyleBackColor = false;
            // 
            // btnIdentify2
            // 
            this.btnIdentify2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnIdentify2.FlatAppearance.BorderSize = 0;
            this.btnIdentify2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify2.ForeColor = System.Drawing.Color.White;
            this.btnIdentify2.Location = new System.Drawing.Point(400, 6);
            this.btnIdentify2.Name = "btnIdentify2";
            this.btnIdentify2.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify2.TabIndex = 12;
            this.btnIdentify2.Text = "📳 Identify";
            this.btnIdentify2.UseVisualStyleBackColor = false;
            // 
            // lblBattery2
            // 
            this.lblBattery2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery2.ForeColor = System.Drawing.Color.White;
            this.lblBattery2.Location = new System.Drawing.Point(120, 60);
            this.lblBattery2.Name = "lblBattery2";
            this.lblBattery2.Size = new System.Drawing.Size(100, 20);
            this.lblBattery2.TabIndex = 13;
            this.lblBattery2.Tag = "Battery";
            this.lblBattery2.Text = "🔋 --%";
            // 
            // lblMac2
            // 
            this.lblDeviceInfo2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDeviceInfo2.ForeColor = System.Drawing.Color.Gray;
            this.lblDeviceInfo2.Location = new System.Drawing.Point(120, 40);
            this.lblDeviceInfo2.Name = "lblDeviceInfo2";
            this.lblDeviceInfo2.Size = new System.Drawing.Size(200, 20);
            this.lblDeviceInfo2.TabIndex = 14;
            this.lblDeviceInfo2.Text = "ID: --:--:--:--:--:--";
            // 
            // lblStatus2
            // 
            this.lblStatus2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus2.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus2.Location = new System.Drawing.Point(120, 15);
            this.lblStatus2.Name = "lblStatus2";
            this.lblStatus2.Size = new System.Drawing.Size(250, 25);
            this.lblStatus2.TabIndex = 15;
            this.lblStatus2.Text = "Waiting for connection...";
            // 
            // lblPlayerName2
            // 
            this.lblPlayerName2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblPlayerName2.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName2.Name = "lblPlayerName2";
            this.lblPlayerName2.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName2.TabIndex = 16;
            this.lblPlayerName2.Text = "Player 2";
            // 
            // panelPlayer3
            // 
            this.panelPlayer3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panelPlayer3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer3.Controls.Add(this.btnSwapDown3);
            this.panelPlayer3.Controls.Add(this.btnSwapUp3);
            this.panelPlayer3.Controls.Add(this.lblMs3);
            this.panelPlayer3.Controls.Add(this.nudDuration3);
            this.panelPlayer3.Controls.Add(this.lblDuration3);
            this.panelPlayer3.Controls.Add(this.trkIntensity3);
            this.panelPlayer3.Controls.Add(this.lblIntensityVal3);
            this.panelPlayer3.Controls.Add(this.lblIntensity3);
            this.panelPlayer3.Controls.Add(this.chkRumble3);
            this.panelPlayer3.Controls.Add(this.lblRumble3);
            this.panelPlayer3.Controls.Add(this.btnLock3);
            this.panelPlayer3.Controls.Add(this.btnDevices3);
            this.panelPlayer3.Controls.Add(this.btnIdentify3);
            this.panelPlayer3.Controls.Add(this.lblBattery3);
            this.panelPlayer3.Controls.Add(this.lblDeviceInfo3);
            this.panelPlayer3.Controls.Add(this.lblStatus3);
            this.panelPlayer3.Controls.Add(this.lblPlayerName3);
            this.panelPlayer3.Location = new System.Drawing.Point(10, 335);
            this.panelPlayer3.Name = "panelPlayer3";
            this.panelPlayer3.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer3.TabIndex = 3;
            // 
            // btnSwapDown3
            // 
            this.btnSwapDown3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapDown3.Enabled = false;
            this.btnSwapDown3.FlatAppearance.BorderSize = 0;
            this.btnSwapDown3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapDown3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapDown3.ForeColor = System.Drawing.Color.White;
            this.btnSwapDown3.Location = new System.Drawing.Point(14, 61);
            this.btnSwapDown3.Name = "btnSwapDown3";
            this.btnSwapDown3.Size = new System.Drawing.Size(85, 23);
            this.btnSwapDown3.TabIndex = 0;
            this.btnSwapDown3.Text = "▼ P4";
            this.btnSwapDown3.UseVisualStyleBackColor = false;
            // 
            // btnSwapUp3
            // 
            this.btnSwapUp3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapUp3.Enabled = false;
            this.btnSwapUp3.FlatAppearance.BorderSize = 0;
            this.btnSwapUp3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapUp3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapUp3.ForeColor = System.Drawing.Color.White;
            this.btnSwapUp3.Location = new System.Drawing.Point(14, 37);
            this.btnSwapUp3.Name = "btnSwapUp3";
            this.btnSwapUp3.Size = new System.Drawing.Size(85, 23);
            this.btnSwapUp3.TabIndex = 1;
            this.btnSwapUp3.Text = "▲ P2";
            this.btnSwapUp3.UseVisualStyleBackColor = false;
            // 
            // lblMs3
            // 
            this.lblMs3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblMs3.Location = new System.Drawing.Point(440, 120);
            this.lblMs3.Name = "lblMs3";
            this.lblMs3.Size = new System.Drawing.Size(25, 20);
            this.lblMs3.TabIndex = 2;
            this.lblMs3.Text = "ms";
            // 
            // nudDuration3
            // 
            this.nudDuration3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudDuration3.ForeColor = System.Drawing.Color.White;
            this.nudDuration3.Location = new System.Drawing.Point(375, 117);
            this.nudDuration3.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudDuration3.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDuration3.Name = "nudDuration3";
            this.nudDuration3.Size = new System.Drawing.Size(60, 20);
            this.nudDuration3.TabIndex = 3;
            this.nudDuration3.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblDuration3
            // 
            this.lblDuration3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblDuration3.Location = new System.Drawing.Point(310, 120);
            this.lblDuration3.Name = "lblDuration3";
            this.lblDuration3.Size = new System.Drawing.Size(60, 20);
            this.lblDuration3.TabIndex = 4;
            this.lblDuration3.Text = "Duration:";
            // 
            // trkIntensity3
            // 
            this.trkIntensity3.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity3.Maximum = 100;
            this.trkIntensity3.Name = "trkIntensity3";
            this.trkIntensity3.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity3.TabIndex = 5;
            this.trkIntensity3.TickFrequency = 10;
            // 
            // lblIntensityVal3
            // 
            this.lblIntensityVal3.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal3.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal3.Name = "lblIntensityVal3";
            this.lblIntensityVal3.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal3.TabIndex = 6;
            this.lblIntensityVal3.Text = "50%";
            // 
            // lblIntensity3
            // 
            this.lblIntensity3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblIntensity3.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity3.Name = "lblIntensity3";
            this.lblIntensity3.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity3.TabIndex = 7;
            this.lblIntensity3.Text = "Intensity:";
            // 
            // chkRumble3
            // 
            this.chkRumble3.ForeColor = System.Drawing.Color.White;
            this.chkRumble3.Location = new System.Drawing.Point(130, 90);
            this.chkRumble3.Name = "chkRumble3";
            this.chkRumble3.Size = new System.Drawing.Size(70, 20);
            this.chkRumble3.TabIndex = 8;
            this.chkRumble3.Text = "Enable";
            // 
            // lblRumble3
            // 
            this.lblRumble3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblRumble3.Location = new System.Drawing.Point(10, 90);
            this.lblRumble3.Name = "lblRumble3";
            this.lblRumble3.Size = new System.Drawing.Size(120, 20);
            this.lblRumble3.TabIndex = 9;
            this.lblRumble3.Text = "Rumble Settings:";
            // 
            // btnLock3
            // 
            this.btnLock3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnLock3.FlatAppearance.BorderSize = 0;
            this.btnLock3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLock3.ForeColor = System.Drawing.Color.White;
            this.btnLock3.Location = new System.Drawing.Point(400, 81);
            this.btnLock3.Name = "btnLock3";
            this.btnLock3.Size = new System.Drawing.Size(90, 30);
            this.btnLock3.TabIndex = 10;
            this.btnLock3.Text = "🔓 Unlock";
            this.btnLock3.UseVisualStyleBackColor = false;
            // 
            // btnDevices3
            // 
            this.btnDevices3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnDevices3.FlatAppearance.BorderSize = 0;
            this.btnDevices3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices3.ForeColor = System.Drawing.Color.White;
            this.btnDevices3.Location = new System.Drawing.Point(400, 44);
            this.btnDevices3.Name = "btnDevices3";
            this.btnDevices3.Size = new System.Drawing.Size(90, 30);
            this.btnDevices3.TabIndex = 11;
            this.btnDevices3.Text = "⚙️ Devices";
            this.btnDevices3.UseVisualStyleBackColor = false;
            // 
            // btnIdentify3
            // 
            this.btnIdentify3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnIdentify3.FlatAppearance.BorderSize = 0;
            this.btnIdentify3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify3.ForeColor = System.Drawing.Color.White;
            this.btnIdentify3.Location = new System.Drawing.Point(400, 6);
            this.btnIdentify3.Name = "btnIdentify3";
            this.btnIdentify3.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify3.TabIndex = 12;
            this.btnIdentify3.Text = "📳 Identify";
            this.btnIdentify3.UseVisualStyleBackColor = false;
            // 
            // lblBattery3
            // 
            this.lblBattery3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery3.ForeColor = System.Drawing.Color.White;
            this.lblBattery3.Location = new System.Drawing.Point(120, 60);
            this.lblBattery3.Name = "lblBattery3";
            this.lblBattery3.Size = new System.Drawing.Size(100, 20);
            this.lblBattery3.TabIndex = 13;
            this.lblBattery3.Tag = "Battery";
            this.lblBattery3.Text = "🔋 --%";
            // 
            // lblMac3
            // 
            this.lblDeviceInfo3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDeviceInfo3.ForeColor = System.Drawing.Color.Gray;
            this.lblDeviceInfo3.Location = new System.Drawing.Point(120, 40);
            this.lblDeviceInfo3.Name = "lblDeviceInfo3";
            this.lblDeviceInfo3.Size = new System.Drawing.Size(200, 20);
            this.lblDeviceInfo3.TabIndex = 14;
            this.lblDeviceInfo3.Text = "ID: --:--:--:--:--:--";
            // 
            // lblStatus3
            // 
            this.lblStatus3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus3.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus3.Location = new System.Drawing.Point(120, 15);
            this.lblStatus3.Name = "lblStatus3";
            this.lblStatus3.Size = new System.Drawing.Size(250, 25);
            this.lblStatus3.TabIndex = 15;
            this.lblStatus3.Text = "Waiting for connection...";
            // 
            // lblPlayerName3
            // 
            this.lblPlayerName3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblPlayerName3.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName3.Name = "lblPlayerName3";
            this.lblPlayerName3.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName3.TabIndex = 16;
            this.lblPlayerName3.Text = "Player 3";
            // 
            // panelPlayer4
            // 
            this.panelPlayer4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panelPlayer4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer4.Controls.Add(this.btnSwapUp4);
            this.panelPlayer4.Controls.Add(this.lblMs4);
            this.panelPlayer4.Controls.Add(this.nudDuration4);
            this.panelPlayer4.Controls.Add(this.lblDuration4);
            this.panelPlayer4.Controls.Add(this.trkIntensity4);
            this.panelPlayer4.Controls.Add(this.lblIntensityVal4);
            this.panelPlayer4.Controls.Add(this.lblIntensity4);
            this.panelPlayer4.Controls.Add(this.chkRumble4);
            this.panelPlayer4.Controls.Add(this.lblRumble4);
            this.panelPlayer4.Controls.Add(this.btnLock4);
            this.panelPlayer4.Controls.Add(this.btnDevices4);
            this.panelPlayer4.Controls.Add(this.btnIdentify4);
            this.panelPlayer4.Controls.Add(this.lblBattery4);
            this.panelPlayer4.Controls.Add(this.lblDeviceInfo4);
            this.panelPlayer4.Controls.Add(this.lblStatus4);
            this.panelPlayer4.Controls.Add(this.lblPlayerName4);
            this.panelPlayer4.Location = new System.Drawing.Point(10, 500);
            this.panelPlayer4.Name = "panelPlayer4";
            this.panelPlayer4.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer4.TabIndex = 4;
            // 
            // btnSwapUp4
            // 
            this.btnSwapUp4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSwapUp4.Enabled = false;
            this.btnSwapUp4.FlatAppearance.BorderSize = 0;
            this.btnSwapUp4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSwapUp4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSwapUp4.ForeColor = System.Drawing.Color.White;
            this.btnSwapUp4.Location = new System.Drawing.Point(14, 37);
            this.btnSwapUp4.Name = "btnSwapUp4";
            this.btnSwapUp4.Size = new System.Drawing.Size(85, 23);
            this.btnSwapUp4.TabIndex = 0;
            this.btnSwapUp4.Text = "▲ P3";
            this.btnSwapUp4.UseVisualStyleBackColor = false;
            // 
            // lblMs4
            // 
            this.lblMs4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblMs4.Location = new System.Drawing.Point(440, 120);
            this.lblMs4.Name = "lblMs4";
            this.lblMs4.Size = new System.Drawing.Size(25, 20);
            this.lblMs4.TabIndex = 1;
            this.lblMs4.Text = "ms";
            // 
            // nudDuration4
            // 
            this.nudDuration4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudDuration4.ForeColor = System.Drawing.Color.White;
            this.nudDuration4.Location = new System.Drawing.Point(375, 117);
            this.nudDuration4.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudDuration4.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDuration4.Name = "nudDuration4";
            this.nudDuration4.Size = new System.Drawing.Size(60, 20);
            this.nudDuration4.TabIndex = 2;
            this.nudDuration4.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblDuration4
            // 
            this.lblDuration4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblDuration4.Location = new System.Drawing.Point(310, 120);
            this.lblDuration4.Name = "lblDuration4";
            this.lblDuration4.Size = new System.Drawing.Size(60, 20);
            this.lblDuration4.TabIndex = 3;
            this.lblDuration4.Text = "Duration:";
            // 
            // trkIntensity4
            // 
            this.trkIntensity4.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity4.Maximum = 100;
            this.trkIntensity4.Name = "trkIntensity4";
            this.trkIntensity4.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity4.TabIndex = 4;
            this.trkIntensity4.TickFrequency = 10;
            // 
            // lblIntensityVal4
            // 
            this.lblIntensityVal4.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal4.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal4.Name = "lblIntensityVal4";
            this.lblIntensityVal4.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal4.TabIndex = 5;
            this.lblIntensityVal4.Text = "50%";
            // 
            // lblIntensity4
            // 
            this.lblIntensity4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblIntensity4.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity4.Name = "lblIntensity4";
            this.lblIntensity4.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity4.TabIndex = 6;
            this.lblIntensity4.Text = "Intensity:";
            // 
            // chkRumble4
            // 
            this.chkRumble4.ForeColor = System.Drawing.Color.White;
            this.chkRumble4.Location = new System.Drawing.Point(130, 90);
            this.chkRumble4.Name = "chkRumble4";
            this.chkRumble4.Size = new System.Drawing.Size(70, 20);
            this.chkRumble4.TabIndex = 7;
            this.chkRumble4.Text = "Enable";
            // 
            // lblRumble4
            // 
            this.lblRumble4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblRumble4.Location = new System.Drawing.Point(10, 90);
            this.lblRumble4.Name = "lblRumble4";
            this.lblRumble4.Size = new System.Drawing.Size(120, 20);
            this.lblRumble4.TabIndex = 8;
            this.lblRumble4.Text = "Rumble Settings:";
            // 
            // btnLock4
            // 
            this.btnLock4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnLock4.FlatAppearance.BorderSize = 0;
            this.btnLock4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLock4.ForeColor = System.Drawing.Color.White;
            this.btnLock4.Location = new System.Drawing.Point(400, 81);
            this.btnLock4.Name = "btnLock4";
            this.btnLock4.Size = new System.Drawing.Size(90, 30);
            this.btnLock4.TabIndex = 9;
            this.btnLock4.Text = "🔓 Unlock";
            this.btnLock4.UseVisualStyleBackColor = false;
            // 
            // btnDevices4
            // 
            this.btnDevices4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnDevices4.FlatAppearance.BorderSize = 0;
            this.btnDevices4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices4.ForeColor = System.Drawing.Color.White;
            this.btnDevices4.Location = new System.Drawing.Point(400, 43);
            this.btnDevices4.Name = "btnDevices4";
            this.btnDevices4.Size = new System.Drawing.Size(90, 30);
            this.btnDevices4.TabIndex = 10;
            this.btnDevices4.Text = "⚙️ Devices";
            this.btnDevices4.UseVisualStyleBackColor = false;
            // 
            // btnIdentify4
            // 
            this.btnIdentify4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnIdentify4.FlatAppearance.BorderSize = 0;
            this.btnIdentify4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify4.ForeColor = System.Drawing.Color.White;
            this.btnIdentify4.Location = new System.Drawing.Point(400, 5);
            this.btnIdentify4.Name = "btnIdentify4";
            this.btnIdentify4.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify4.TabIndex = 11;
            this.btnIdentify4.Text = "📳 Identify";
            this.btnIdentify4.UseVisualStyleBackColor = false;
            // 
            // lblBattery4
            // 
            this.lblBattery4.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery4.ForeColor = System.Drawing.Color.White;
            this.lblBattery4.Location = new System.Drawing.Point(120, 60);
            this.lblBattery4.Name = "lblBattery4";
            this.lblBattery4.Size = new System.Drawing.Size(100, 20);
            this.lblBattery4.TabIndex = 12;
            this.lblBattery4.Tag = "Battery";
            this.lblBattery4.Text = "🔋 --%";
            // 
            // lblMac4
            // 
            this.lblDeviceInfo4.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDeviceInfo4.ForeColor = System.Drawing.Color.Gray;
            this.lblDeviceInfo4.Location = new System.Drawing.Point(120, 40);
            this.lblDeviceInfo4.Name = "lblDeviceInfo4";
            this.lblDeviceInfo4.Size = new System.Drawing.Size(200, 20);
            this.lblDeviceInfo4.TabIndex = 13;
            this.lblDeviceInfo4.Text = "ID: --:--:--:--:--:--";
            // 
            // lblStatus4
            // 
            this.lblStatus4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus4.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus4.Location = new System.Drawing.Point(120, 15);
            this.lblStatus4.Name = "lblStatus4";
            this.lblStatus4.Size = new System.Drawing.Size(250, 25);
            this.lblStatus4.TabIndex = 14;
            this.lblStatus4.Text = "Waiting for connection...";
            // 
            // lblPlayerName4
            // 
            this.lblPlayerName4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblPlayerName4.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName4.Name = "lblPlayerName4";
            this.lblPlayerName4.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName4.TabIndex = 15;
            this.lblPlayerName4.Text = "Player 4";
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(10, 740);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(80, 30);
            this.btnBack.TabIndex = 2;
            this.btnBack.Text = "⬅ Back";
            this.btnBack.UseVisualStyleBackColor = false;
            // 
            // AssignControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.panelAssignContent);
            this.Controls.Add(this.lblTitle);
            this.Name = "AssignControl";
            this.Size = new System.Drawing.Size(560, 782);
            this.panelAssignContent.ResumeLayout(false);
            this.panelPlayer1.ResumeLayout(false);
            this.panelPlayer1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity1)).EndInit();
            this.panelPlayer2.ResumeLayout(false);
            this.panelPlayer2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity2)).EndInit();
            this.panelPlayer3.ResumeLayout(false);
            this.panelPlayer3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity3)).EndInit();
            this.panelPlayer4.ResumeLayout(false);
            this.panelPlayer4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelAssignContent;
        // Static panels for 4 players
        public System.Windows.Forms.Panel panelPlayer1;
        public System.Windows.Forms.Panel panelPlayer2;
        public System.Windows.Forms.Panel panelPlayer3;
        public System.Windows.Forms.Panel panelPlayer4;

        // Player 1 Controls
        private System.Windows.Forms.Label lblPlayerName1;
        private System.Windows.Forms.Label lblStatus1;
        private System.Windows.Forms.Label lblDeviceInfo1;
        private System.Windows.Forms.Label lblBattery1;
        private System.Windows.Forms.Button btnIdentify1;
        private System.Windows.Forms.Button btnDevices1;
        private System.Windows.Forms.Button btnLock1;
        private System.Windows.Forms.Label lblRumble1;
        private System.Windows.Forms.CheckBox chkRumble1;
        private System.Windows.Forms.Label lblIntensity1;
        private System.Windows.Forms.Label lblIntensityVal1;
        private System.Windows.Forms.TrackBar trkIntensity1;
        private System.Windows.Forms.Label lblDuration1;
        private System.Windows.Forms.NumericUpDown nudDuration1;
        private System.Windows.Forms.Label lblMs1;

        // Player 2 Controls
        private System.Windows.Forms.Label lblPlayerName2;
        private System.Windows.Forms.Label lblStatus2;
        private System.Windows.Forms.Label lblDeviceInfo2;
        private System.Windows.Forms.Label lblBattery2;
        private System.Windows.Forms.Button btnIdentify2;
        private System.Windows.Forms.Button btnDevices2;
        private System.Windows.Forms.Button btnLock2;
        private System.Windows.Forms.Label lblRumble2;
        private System.Windows.Forms.CheckBox chkRumble2;
        private System.Windows.Forms.Label lblIntensity2;
        private System.Windows.Forms.Label lblIntensityVal2;
        private System.Windows.Forms.TrackBar trkIntensity2;
        private System.Windows.Forms.Label lblDuration2;
        private System.Windows.Forms.NumericUpDown nudDuration2;
        private System.Windows.Forms.Label lblMs2;

        // Player 3 Controls
        private System.Windows.Forms.Label lblPlayerName3;
        private System.Windows.Forms.Label lblStatus3;
        private System.Windows.Forms.Label lblDeviceInfo3;
        private System.Windows.Forms.Label lblBattery3;
        private System.Windows.Forms.Button btnIdentify3;
        private System.Windows.Forms.Button btnDevices3;
        private System.Windows.Forms.Button btnLock3;
        private System.Windows.Forms.Label lblRumble3;
        private System.Windows.Forms.CheckBox chkRumble3;
        private System.Windows.Forms.Label lblIntensity3;
        private System.Windows.Forms.Label lblIntensityVal3;
        private System.Windows.Forms.TrackBar trkIntensity3;
        private System.Windows.Forms.Label lblDuration3;
        private System.Windows.Forms.NumericUpDown nudDuration3;
        private System.Windows.Forms.Label lblMs3;

        // Player 4 Controls
        private System.Windows.Forms.Label lblPlayerName4;
        private System.Windows.Forms.Label lblStatus4;
        private System.Windows.Forms.Label lblDeviceInfo4;
        private System.Windows.Forms.Label lblBattery4;
        private System.Windows.Forms.Button btnIdentify4;
        private System.Windows.Forms.Button btnDevices4;
        private System.Windows.Forms.Button btnLock4;
        private System.Windows.Forms.Label lblRumble4;
        private System.Windows.Forms.CheckBox chkRumble4;
        private System.Windows.Forms.Label lblIntensity4;
        private System.Windows.Forms.Label lblIntensityVal4;
        private System.Windows.Forms.TrackBar trkIntensity4;
        private System.Windows.Forms.Label lblDuration4;
        private System.Windows.Forms.NumericUpDown nudDuration4;
        private System.Windows.Forms.Label lblMs4;

        // Swap Up buttons for P2, P3, P4 (EN/FR: Boutons swap vers le haut pour P2, P3, P4)
        private System.Windows.Forms.Button btnSwapUp2;
        private System.Windows.Forms.Button btnSwapUp3;
        private System.Windows.Forms.Button btnSwapUp4;

        // Swap Down buttons for P1, P2, P3 (EN/FR: Boutons swap vers le bas pour P1, P2, P3)
        private System.Windows.Forms.Button btnSwapDown1;
        private System.Windows.Forms.Button btnSwapDown2;
        private System.Windows.Forms.Button btnSwapDown3;

        public System.Windows.Forms.Button btnBack;
    }
}
