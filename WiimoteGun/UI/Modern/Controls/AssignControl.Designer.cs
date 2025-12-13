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
            this.panelPlayer2 = new System.Windows.Forms.Panel();
            this.panelPlayer3 = new System.Windows.Forms.Panel();
            this.panelPlayer4 = new System.Windows.Forms.Panel();
            
            // Player 1 Controls
            this.lblPlayerName1 = new System.Windows.Forms.Label();
            this.lblStatus1 = new System.Windows.Forms.Label();
            this.lblMac1 = new System.Windows.Forms.Label();
            this.lblBattery1 = new System.Windows.Forms.Label();
            this.btnIdentify1 = new System.Windows.Forms.Button();
            this.btnDevices1 = new System.Windows.Forms.Button();
            this.lblRumble1 = new System.Windows.Forms.Label();
            this.chkRumble1 = new System.Windows.Forms.CheckBox();
            this.lblIntensity1 = new System.Windows.Forms.Label();
            this.lblIntensityVal1 = new System.Windows.Forms.Label();
            this.trkIntensity1 = new System.Windows.Forms.TrackBar();
            this.lblDuration1 = new System.Windows.Forms.Label();
            this.nudDuration1 = new System.Windows.Forms.NumericUpDown();
            this.lblMs1 = new System.Windows.Forms.Label();

            // Player 2 Controls
            this.lblPlayerName2 = new System.Windows.Forms.Label();
            this.lblStatus2 = new System.Windows.Forms.Label();
            this.lblMac2 = new System.Windows.Forms.Label();
            this.lblBattery2 = new System.Windows.Forms.Label();
            this.btnIdentify2 = new System.Windows.Forms.Button();
            this.btnDevices2 = new System.Windows.Forms.Button();
            this.lblRumble2 = new System.Windows.Forms.Label();
            this.chkRumble2 = new System.Windows.Forms.CheckBox();
            this.lblIntensity2 = new System.Windows.Forms.Label();
            this.lblIntensityVal2 = new System.Windows.Forms.Label();
            this.trkIntensity2 = new System.Windows.Forms.TrackBar();
            this.lblDuration2 = new System.Windows.Forms.Label();
            this.nudDuration2 = new System.Windows.Forms.NumericUpDown();
            this.lblMs2 = new System.Windows.Forms.Label();

            // Player 3 Controls
            this.lblPlayerName3 = new System.Windows.Forms.Label();
            this.lblStatus3 = new System.Windows.Forms.Label();
            this.lblMac3 = new System.Windows.Forms.Label();
            this.lblBattery3 = new System.Windows.Forms.Label();
            this.btnIdentify3 = new System.Windows.Forms.Button();
            this.btnDevices3 = new System.Windows.Forms.Button();
            this.lblRumble3 = new System.Windows.Forms.Label();
            this.chkRumble3 = new System.Windows.Forms.CheckBox();
            this.lblIntensity3 = new System.Windows.Forms.Label();
            this.lblIntensityVal3 = new System.Windows.Forms.Label();
            this.trkIntensity3 = new System.Windows.Forms.TrackBar();
            this.lblDuration3 = new System.Windows.Forms.Label();
            this.nudDuration3 = new System.Windows.Forms.NumericUpDown();
            this.lblMs3 = new System.Windows.Forms.Label();

            // Player 4 Controls
            this.lblPlayerName4 = new System.Windows.Forms.Label();
            this.lblStatus4 = new System.Windows.Forms.Label();
            this.lblMac4 = new System.Windows.Forms.Label();
            this.lblBattery4 = new System.Windows.Forms.Label();
            this.btnIdentify4 = new System.Windows.Forms.Button();
            this.btnDevices4 = new System.Windows.Forms.Button();
            this.lblRumble4 = new System.Windows.Forms.Label();
            this.chkRumble4 = new System.Windows.Forms.CheckBox();
            this.lblIntensity4 = new System.Windows.Forms.Label();
            this.lblIntensityVal4 = new System.Windows.Forms.Label();
            this.trkIntensity4 = new System.Windows.Forms.TrackBar();
            this.lblDuration4 = new System.Windows.Forms.Label();
            this.nudDuration4 = new System.Windows.Forms.NumericUpDown();
            this.lblMs4 = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration4)).BeginInit();
            this.panelAssignContent.SuspendLayout();
            this.panelPlayer1.SuspendLayout();
            this.panelPlayer2.SuspendLayout();
            this.panelPlayer3.SuspendLayout();
            this.panelPlayer4.SuspendLayout();
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

            // ============================================
            // PLAYER 1 SETUP
            // ============================================
            this.panelPlayer1.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.panelPlayer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer1.Location = new System.Drawing.Point(10, 5);
            this.panelPlayer1.Name = "panelPlayer1";
            this.panelPlayer1.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer1.TabIndex = 1;

            this.lblPlayerName1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName1.ForeColor = System.Drawing.Color.FromArgb(0, 180, 255);
            this.lblPlayerName1.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName1.Name = "lblPlayerName1";
            this.lblPlayerName1.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName1.Text = "Player 1";

            this.lblStatus1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus1.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus1.Location = new System.Drawing.Point(120, 15);
            this.lblStatus1.Name = "lblStatus1";
            this.lblStatus1.Size = new System.Drawing.Size(250, 25);
            this.lblStatus1.Text = "Waiting for connection...";

            this.lblMac1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMac1.ForeColor = System.Drawing.Color.Gray;
            this.lblMac1.Location = new System.Drawing.Point(120, 40);
            this.lblMac1.Name = "lblMac1";
            this.lblMac1.Size = new System.Drawing.Size(200, 20);
            this.lblMac1.Text = "MAC: --:--:--:--:--:--";

            this.lblBattery1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery1.ForeColor = System.Drawing.Color.White;
            this.lblBattery1.Location = new System.Drawing.Point(120, 60);
            this.lblBattery1.Name = "lblBattery1";
            this.lblBattery1.Size = new System.Drawing.Size(100, 20);
            this.lblBattery1.Text = "🔋 --%";
            this.lblBattery1.Tag = "Battery";

            this.btnIdentify1.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnIdentify1.FlatAppearance.BorderSize = 0;
            this.btnIdentify1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify1.ForeColor = System.Drawing.Color.White;
            this.btnIdentify1.Location = new System.Drawing.Point(400, 10);
            this.btnIdentify1.Name = "btnIdentify1";
            this.btnIdentify1.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify1.Text = "📳 Identify";

            this.btnDevices1.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnDevices1.FlatAppearance.BorderSize = 0;
            this.btnDevices1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices1.ForeColor = System.Drawing.Color.White;
            this.btnDevices1.Location = new System.Drawing.Point(400, 50);
            this.btnDevices1.Name = "btnDevices1";
            this.btnDevices1.Size = new System.Drawing.Size(90, 30);
            this.btnDevices1.Text = "⚙️ Devices";

            this.lblRumble1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble1.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblRumble1.Location = new System.Drawing.Point(10, 90);
            this.lblRumble1.Name = "lblRumble1";
            this.lblRumble1.Size = new System.Drawing.Size(120, 20);
            this.lblRumble1.Text = "Rumble Settings:";

            this.chkRumble1.ForeColor = System.Drawing.Color.White;
            this.chkRumble1.Location = new System.Drawing.Point(130, 90);
            this.chkRumble1.Name = "chkRumble1";
            this.chkRumble1.Size = new System.Drawing.Size(70, 20);
            this.chkRumble1.Text = "Enable";

            this.lblIntensity1.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblIntensity1.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity1.Name = "lblIntensity1";
            this.lblIntensity1.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity1.Text = "Intensity:";

            this.trkIntensity1.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity1.Maximum = 100;
            this.trkIntensity1.Name = "trkIntensity1";
            this.trkIntensity1.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity1.TickFrequency = 10;

            this.lblIntensityVal1.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal1.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal1.Name = "lblIntensityVal1";
            this.lblIntensityVal1.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal1.Text = "50%";

            this.lblDuration1.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblDuration1.Location = new System.Drawing.Point(310, 120);
            this.lblDuration1.Name = "lblDuration1";
            this.lblDuration1.Size = new System.Drawing.Size(60, 20);
            this.lblDuration1.Text = "Duration:";

            this.nudDuration1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.nudDuration1.ForeColor = System.Drawing.Color.White;
            this.nudDuration1.Location = new System.Drawing.Point(375, 117);
            this.nudDuration1.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudDuration1.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudDuration1.Name = "nudDuration1";
            this.nudDuration1.Size = new System.Drawing.Size(60, 23);
            this.nudDuration1.Value = new decimal(new int[] { 50, 0, 0, 0 });

            this.lblMs1.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblMs1.Location = new System.Drawing.Point(440, 120);
            this.lblMs1.Name = "lblMs1";
            this.lblMs1.Size = new System.Drawing.Size(25, 20);
            this.lblMs1.Text = "ms";

            this.panelPlayer1.Controls.Add(this.lblMs1);
            this.panelPlayer1.Controls.Add(this.nudDuration1);
            this.panelPlayer1.Controls.Add(this.lblDuration1);
            this.panelPlayer1.Controls.Add(this.trkIntensity1);
            this.panelPlayer1.Controls.Add(this.lblIntensityVal1);
            this.panelPlayer1.Controls.Add(this.lblIntensity1);
            this.panelPlayer1.Controls.Add(this.chkRumble1);
            this.panelPlayer1.Controls.Add(this.lblRumble1);
            this.panelPlayer1.Controls.Add(this.btnDevices1);
            this.panelPlayer1.Controls.Add(this.btnIdentify1);
            this.panelPlayer1.Controls.Add(this.lblBattery1);
            this.panelPlayer1.Controls.Add(this.lblMac1);
            this.panelPlayer1.Controls.Add(this.lblStatus1);
            this.panelPlayer1.Controls.Add(this.lblPlayerName1);

            // ============================================
            // PLAYER 2 SETUP
            // ============================================
            this.panelPlayer2.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.panelPlayer2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer2.Location = new System.Drawing.Point(10, 170);
            this.panelPlayer2.Name = "panelPlayer2";
            this.panelPlayer2.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer2.TabIndex = 2;

            this.lblPlayerName2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName2.ForeColor = System.Drawing.Color.FromArgb(0, 180, 255);
            this.lblPlayerName2.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName2.Name = "lblPlayerName2";
            this.lblPlayerName2.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName2.Text = "Player 2";

            this.lblStatus2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus2.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus2.Location = new System.Drawing.Point(120, 15);
            this.lblStatus2.Name = "lblStatus2";
            this.lblStatus2.Size = new System.Drawing.Size(250, 25);
            this.lblStatus2.Text = "Waiting for connection...";

            this.lblMac2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMac2.ForeColor = System.Drawing.Color.Gray;
            this.lblMac2.Location = new System.Drawing.Point(120, 40);
            this.lblMac2.Name = "lblMac2";
            this.lblMac2.Size = new System.Drawing.Size(200, 20);
            this.lblMac2.Text = "MAC: --:--:--:--:--:--";

            this.lblBattery2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery2.ForeColor = System.Drawing.Color.White;
            this.lblBattery2.Location = new System.Drawing.Point(120, 60);
            this.lblBattery2.Name = "lblBattery2";
            this.lblBattery2.Size = new System.Drawing.Size(100, 20);
            this.lblBattery2.Text = "🔋 --%";
            this.lblBattery2.Tag = "Battery";

            this.btnIdentify2.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnIdentify2.FlatAppearance.BorderSize = 0;
            this.btnIdentify2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify2.ForeColor = System.Drawing.Color.White;
            this.btnIdentify2.Location = new System.Drawing.Point(400, 10);
            this.btnIdentify2.Name = "btnIdentify2";
            this.btnIdentify2.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify2.Text = "📳 Identify";

            this.btnDevices2.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnDevices2.FlatAppearance.BorderSize = 0;
            this.btnDevices2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices2.ForeColor = System.Drawing.Color.White;
            this.btnDevices2.Location = new System.Drawing.Point(400, 50);
            this.btnDevices2.Name = "btnDevices2";
            this.btnDevices2.Size = new System.Drawing.Size(90, 30);
            this.btnDevices2.Text = "⚙️ Devices";

            this.lblRumble2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble2.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblRumble2.Location = new System.Drawing.Point(10, 90);
            this.lblRumble2.Name = "lblRumble2";
            this.lblRumble2.Size = new System.Drawing.Size(120, 20);
            this.lblRumble2.Text = "Rumble Settings:";

            this.chkRumble2.ForeColor = System.Drawing.Color.White;
            this.chkRumble2.Location = new System.Drawing.Point(130, 90);
            this.chkRumble2.Name = "chkRumble2";
            this.chkRumble2.Size = new System.Drawing.Size(70, 20);
            this.chkRumble2.Text = "Enable";

            this.lblIntensity2.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblIntensity2.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity2.Name = "lblIntensity2";
            this.lblIntensity2.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity2.Text = "Intensity:";

            this.trkIntensity2.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity2.Maximum = 100;
            this.trkIntensity2.Name = "trkIntensity2";
            this.trkIntensity2.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity2.TickFrequency = 10;

            this.lblIntensityVal2.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal2.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal2.Name = "lblIntensityVal2";
            this.lblIntensityVal2.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal2.Text = "50%";

            this.lblDuration2.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblDuration2.Location = new System.Drawing.Point(310, 120);
            this.lblDuration2.Name = "lblDuration2";
            this.lblDuration2.Size = new System.Drawing.Size(60, 20);
            this.lblDuration2.Text = "Duration:";

            this.nudDuration2.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.nudDuration2.ForeColor = System.Drawing.Color.White;
            this.nudDuration2.Location = new System.Drawing.Point(375, 117);
            this.nudDuration2.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudDuration2.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudDuration2.Name = "nudDuration2";
            this.nudDuration2.Size = new System.Drawing.Size(60, 23);
            this.nudDuration2.Value = new decimal(new int[] { 50, 0, 0, 0 });

            this.lblMs2.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblMs2.Location = new System.Drawing.Point(440, 120);
            this.lblMs2.Name = "lblMs2";
            this.lblMs2.Size = new System.Drawing.Size(25, 20);
            this.lblMs2.Text = "ms";

            this.panelPlayer2.Controls.Add(this.lblMs2);
            this.panelPlayer2.Controls.Add(this.nudDuration2);
            this.panelPlayer2.Controls.Add(this.lblDuration2);
            this.panelPlayer2.Controls.Add(this.trkIntensity2);
            this.panelPlayer2.Controls.Add(this.lblIntensityVal2);
            this.panelPlayer2.Controls.Add(this.lblIntensity2);
            this.panelPlayer2.Controls.Add(this.chkRumble2);
            this.panelPlayer2.Controls.Add(this.lblRumble2);
            this.panelPlayer2.Controls.Add(this.btnDevices2);
            this.panelPlayer2.Controls.Add(this.btnIdentify2);
            this.panelPlayer2.Controls.Add(this.lblBattery2);
            this.panelPlayer2.Controls.Add(this.lblMac2);
            this.panelPlayer2.Controls.Add(this.lblStatus2);
            this.panelPlayer2.Controls.Add(this.lblPlayerName2);

            // ============================================
            // PLAYER 3 SETUP
            // ============================================
            this.panelPlayer3.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.panelPlayer3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer3.Location = new System.Drawing.Point(10, 335);
            this.panelPlayer3.Name = "panelPlayer3";
            this.panelPlayer3.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer3.TabIndex = 3;

            this.lblPlayerName3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName3.ForeColor = System.Drawing.Color.FromArgb(0, 180, 255);
            this.lblPlayerName3.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName3.Name = "lblPlayerName3";
            this.lblPlayerName3.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName3.Text = "Player 3";

            this.lblStatus3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus3.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus3.Location = new System.Drawing.Point(120, 15);
            this.lblStatus3.Name = "lblStatus3";
            this.lblStatus3.Size = new System.Drawing.Size(250, 25);
            this.lblStatus3.Text = "Waiting for connection...";

            this.lblMac3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMac3.ForeColor = System.Drawing.Color.Gray;
            this.lblMac3.Location = new System.Drawing.Point(120, 40);
            this.lblMac3.Name = "lblMac3";
            this.lblMac3.Size = new System.Drawing.Size(200, 20);
            this.lblMac3.Text = "MAC: --:--:--:--:--:--";

            this.lblBattery3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery3.ForeColor = System.Drawing.Color.White;
            this.lblBattery3.Location = new System.Drawing.Point(120, 60);
            this.lblBattery3.Name = "lblBattery3";
            this.lblBattery3.Size = new System.Drawing.Size(100, 20);
            this.lblBattery3.Text = "🔋 --%";
            this.lblBattery3.Tag = "Battery";

            this.btnIdentify3.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnIdentify3.FlatAppearance.BorderSize = 0;
            this.btnIdentify3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify3.ForeColor = System.Drawing.Color.White;
            this.btnIdentify3.Location = new System.Drawing.Point(400, 10);
            this.btnIdentify3.Name = "btnIdentify3";
            this.btnIdentify3.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify3.Text = "📳 Identify";

            this.btnDevices3.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnDevices3.FlatAppearance.BorderSize = 0;
            this.btnDevices3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices3.ForeColor = System.Drawing.Color.White;
            this.btnDevices3.Location = new System.Drawing.Point(400, 50);
            this.btnDevices3.Name = "btnDevices3";
            this.btnDevices3.Size = new System.Drawing.Size(90, 30);
            this.btnDevices3.Text = "⚙️ Devices";

            this.lblRumble3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble3.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblRumble3.Location = new System.Drawing.Point(10, 90);
            this.lblRumble3.Name = "lblRumble3";
            this.lblRumble3.Size = new System.Drawing.Size(120, 20);
            this.lblRumble3.Text = "Rumble Settings:";

            this.chkRumble3.ForeColor = System.Drawing.Color.White;
            this.chkRumble3.Location = new System.Drawing.Point(130, 90);
            this.chkRumble3.Name = "chkRumble3";
            this.chkRumble3.Size = new System.Drawing.Size(70, 20);
            this.chkRumble3.Text = "Enable";

            this.lblIntensity3.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblIntensity3.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity3.Name = "lblIntensity3";
            this.lblIntensity3.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity3.Text = "Intensity:";

            this.trkIntensity3.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity3.Maximum = 100;
            this.trkIntensity3.Name = "trkIntensity3";
            this.trkIntensity3.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity3.TickFrequency = 10;

            this.lblIntensityVal3.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal3.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal3.Name = "lblIntensityVal3";
            this.lblIntensityVal3.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal3.Text = "50%";

            this.lblDuration3.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblDuration3.Location = new System.Drawing.Point(310, 120);
            this.lblDuration3.Name = "lblDuration3";
            this.lblDuration3.Size = new System.Drawing.Size(60, 20);
            this.lblDuration3.Text = "Duration:";

            this.nudDuration3.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.nudDuration3.ForeColor = System.Drawing.Color.White;
            this.nudDuration3.Location = new System.Drawing.Point(375, 117);
            this.nudDuration3.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudDuration3.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudDuration3.Name = "nudDuration3";
            this.nudDuration3.Size = new System.Drawing.Size(60, 23);
            this.nudDuration3.Value = new decimal(new int[] { 50, 0, 0, 0 });

            this.lblMs3.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblMs3.Location = new System.Drawing.Point(440, 120);
            this.lblMs3.Name = "lblMs3";
            this.lblMs3.Size = new System.Drawing.Size(25, 20);
            this.lblMs3.Text = "ms";

            this.panelPlayer3.Controls.Add(this.lblMs3);
            this.panelPlayer3.Controls.Add(this.nudDuration3);
            this.panelPlayer3.Controls.Add(this.lblDuration3);
            this.panelPlayer3.Controls.Add(this.trkIntensity3);
            this.panelPlayer3.Controls.Add(this.lblIntensityVal3);
            this.panelPlayer3.Controls.Add(this.lblIntensity3);
            this.panelPlayer3.Controls.Add(this.chkRumble3);
            this.panelPlayer3.Controls.Add(this.lblRumble3);
            this.panelPlayer3.Controls.Add(this.btnDevices3);
            this.panelPlayer3.Controls.Add(this.btnIdentify3);
            this.panelPlayer3.Controls.Add(this.lblBattery3);
            this.panelPlayer3.Controls.Add(this.lblMac3);
            this.panelPlayer3.Controls.Add(this.lblStatus3);
            this.panelPlayer3.Controls.Add(this.lblPlayerName3);

            // ============================================
            // PLAYER 4 SETUP
            // ============================================
            this.panelPlayer4.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.panelPlayer4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPlayer4.Location = new System.Drawing.Point(10, 500);
            this.panelPlayer4.Name = "panelPlayer4";
            this.panelPlayer4.Size = new System.Drawing.Size(500, 160);
            this.panelPlayer4.TabIndex = 4;

            this.lblPlayerName4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPlayerName4.ForeColor = System.Drawing.Color.FromArgb(0, 180, 255);
            this.lblPlayerName4.Location = new System.Drawing.Point(10, 10);
            this.lblPlayerName4.Name = "lblPlayerName4";
            this.lblPlayerName4.Size = new System.Drawing.Size(100, 30);
            this.lblPlayerName4.Text = "Player 4";

            this.lblStatus4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblStatus4.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus4.Location = new System.Drawing.Point(120, 15);
            this.lblStatus4.Name = "lblStatus4";
            this.lblStatus4.Size = new System.Drawing.Size(250, 25);
            this.lblStatus4.Text = "Waiting for connection...";

            this.lblMac4.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMac4.ForeColor = System.Drawing.Color.Gray;
            this.lblMac4.Location = new System.Drawing.Point(120, 40);
            this.lblMac4.Name = "lblMac4";
            this.lblMac4.Size = new System.Drawing.Size(200, 20);
            this.lblMac4.Text = "MAC: --:--:--:--:--:--";

            this.lblBattery4.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBattery4.ForeColor = System.Drawing.Color.White;
            this.lblBattery4.Location = new System.Drawing.Point(120, 60);
            this.lblBattery4.Name = "lblBattery4";
            this.lblBattery4.Size = new System.Drawing.Size(100, 20);
            this.lblBattery4.Text = "🔋 --%";
            this.lblBattery4.Tag = "Battery";

            this.btnIdentify4.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnIdentify4.FlatAppearance.BorderSize = 0;
            this.btnIdentify4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIdentify4.ForeColor = System.Drawing.Color.White;
            this.btnIdentify4.Location = new System.Drawing.Point(400, 10);
            this.btnIdentify4.Name = "btnIdentify4";
            this.btnIdentify4.Size = new System.Drawing.Size(90, 30);
            this.btnIdentify4.Text = "📳 Identify";

            this.btnDevices4.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnDevices4.FlatAppearance.BorderSize = 0;
            this.btnDevices4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDevices4.ForeColor = System.Drawing.Color.White;
            this.btnDevices4.Location = new System.Drawing.Point(400, 50);
            this.btnDevices4.Name = "btnDevices4";
            this.btnDevices4.Size = new System.Drawing.Size(90, 30);
            this.btnDevices4.Text = "⚙️ Devices";

            this.lblRumble4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRumble4.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblRumble4.Location = new System.Drawing.Point(10, 90);
            this.lblRumble4.Name = "lblRumble4";
            this.lblRumble4.Size = new System.Drawing.Size(120, 20);
            this.lblRumble4.Text = "Rumble Settings:";

            this.chkRumble4.ForeColor = System.Drawing.Color.White;
            this.chkRumble4.Location = new System.Drawing.Point(130, 90);
            this.chkRumble4.Name = "chkRumble4";
            this.chkRumble4.Size = new System.Drawing.Size(70, 20);
            this.chkRumble4.Text = "Enable";

            this.lblIntensity4.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblIntensity4.Location = new System.Drawing.Point(10, 120);
            this.lblIntensity4.Name = "lblIntensity4";
            this.lblIntensity4.Size = new System.Drawing.Size(60, 20);
            this.lblIntensity4.Text = "Intensity:";

            this.trkIntensity4.Location = new System.Drawing.Point(70, 115);
            this.trkIntensity4.Maximum = 100;
            this.trkIntensity4.Name = "trkIntensity4";
            this.trkIntensity4.Size = new System.Drawing.Size(180, 45);
            this.trkIntensity4.TickFrequency = 10;

            this.lblIntensityVal4.ForeColor = System.Drawing.Color.White;
            this.lblIntensityVal4.Location = new System.Drawing.Point(255, 120);
            this.lblIntensityVal4.Name = "lblIntensityVal4";
            this.lblIntensityVal4.Size = new System.Drawing.Size(50, 20);
            this.lblIntensityVal4.Text = "50%";

            this.lblDuration4.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblDuration4.Location = new System.Drawing.Point(310, 120);
            this.lblDuration4.Name = "lblDuration4";
            this.lblDuration4.Size = new System.Drawing.Size(60, 20);
            this.lblDuration4.Text = "Duration:";

            this.nudDuration4.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.nudDuration4.ForeColor = System.Drawing.Color.White;
            this.nudDuration4.Location = new System.Drawing.Point(375, 117);
            this.nudDuration4.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudDuration4.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudDuration4.Name = "nudDuration4";
            this.nudDuration4.Size = new System.Drawing.Size(60, 23);
            this.nudDuration4.Value = new decimal(new int[] { 50, 0, 0, 0 });

            this.lblMs4.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.lblMs4.Location = new System.Drawing.Point(440, 120);
            this.lblMs4.Name = "lblMs4";
            this.lblMs4.Size = new System.Drawing.Size(25, 20);
            this.lblMs4.Text = "ms";

            this.panelPlayer4.Controls.Add(this.lblMs4);
            this.panelPlayer4.Controls.Add(this.nudDuration4);
            this.panelPlayer4.Controls.Add(this.lblDuration4);
            this.panelPlayer4.Controls.Add(this.trkIntensity4);
            this.panelPlayer4.Controls.Add(this.lblIntensityVal4);
            this.panelPlayer4.Controls.Add(this.lblIntensity4);
            this.panelPlayer4.Controls.Add(this.chkRumble4);
            this.panelPlayer4.Controls.Add(this.lblRumble4);
            this.panelPlayer4.Controls.Add(this.btnDevices4);
            this.panelPlayer4.Controls.Add(this.btnIdentify4);
            this.panelPlayer4.Controls.Add(this.lblBattery4);
            this.panelPlayer4.Controls.Add(this.lblMac4);
            this.panelPlayer4.Controls.Add(this.lblStatus4);
            this.panelPlayer4.Controls.Add(this.lblPlayerName4);
            
            // 
            // btnBack
            // 
            this.btnBack = new System.Windows.Forms.Button();
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
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.panelAssignContent);
            this.Controls.Add(this.lblTitle);
            this.Name = "AssignControl";
            this.Size = new System.Drawing.Size(560, 782);
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkIntensity4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration4)).EndInit();
            this.panelAssignContent.ResumeLayout(false);
            this.panelPlayer1.ResumeLayout(false);
            this.panelPlayer2.ResumeLayout(false);
            this.panelPlayer3.ResumeLayout(false);
            this.panelPlayer4.ResumeLayout(false);
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
        private System.Windows.Forms.Label lblMac1;
        private System.Windows.Forms.Label lblBattery1;
        private System.Windows.Forms.Button btnIdentify1;
        private System.Windows.Forms.Button btnDevices1;
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
        private System.Windows.Forms.Label lblMac2;
        private System.Windows.Forms.Label lblBattery2;
        private System.Windows.Forms.Button btnIdentify2;
        private System.Windows.Forms.Button btnDevices2;
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
        private System.Windows.Forms.Label lblMac3;
        private System.Windows.Forms.Label lblBattery3;
        private System.Windows.Forms.Button btnIdentify3;
        private System.Windows.Forms.Button btnDevices3;
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
        private System.Windows.Forms.Label lblMac4;
        private System.Windows.Forms.Label lblBattery4;
        private System.Windows.Forms.Button btnIdentify4;
        private System.Windows.Forms.Button btnDevices4;
        private System.Windows.Forms.Label lblRumble4;
        private System.Windows.Forms.CheckBox chkRumble4;
        private System.Windows.Forms.Label lblIntensity4;
        private System.Windows.Forms.Label lblIntensityVal4;
        private System.Windows.Forms.TrackBar trkIntensity4;
        private System.Windows.Forms.Label lblDuration4;
        private System.Windows.Forms.NumericUpDown nudDuration4;
        private System.Windows.Forms.Label lblMs4;
        public System.Windows.Forms.Button btnBack;
    }
}
