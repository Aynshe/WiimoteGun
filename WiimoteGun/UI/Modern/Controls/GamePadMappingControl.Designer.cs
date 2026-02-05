namespace WiimoteGun.Controls
{
    partial class GamePadMappingControl
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
            this.btnBack = new System.Windows.Forms.Button();
            this.tabControlPlayers = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabControlSettings = new System.Windows.Forms.TabControl();
            this.tabPageMappings = new System.Windows.Forms.TabPage();
            this.grpAxes = new System.Windows.Forms.GroupBox();
            this.cboNunchukAxis = new System.Windows.Forms.ComboBox();
            this.lblNunchukAxis = new System.Windows.Forms.Label();
            this.cboIRAxis = new System.Windows.Forms.ComboBox();
            this.lblIRAxis = new System.Windows.Forms.Label();
            this.grpButtons = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPageCalibration = new System.Windows.Forms.TabPage();
            this.grpCalibration = new System.Windows.Forms.GroupBox();
            this.lblLinearityTitle = new System.Windows.Forms.Label();
            this.numLinearity = new System.Windows.Forms.NumericUpDown();
            this.lblLinearityDesc = new System.Windows.Forms.Label();
            this.lblOverscanTitle = new System.Windows.Forms.Label();
            this.numOverscan = new System.Windows.Forms.NumericUpDown();
            this.lblOverscanDesc = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.tabControlPlayers.SuspendLayout();
            this.tabControlSettings.SuspendLayout();
            this.tabPageMappings.SuspendLayout();
            this.grpAxes.SuspendLayout();
            this.grpButtons.SuspendLayout();
            this.tabPageCalibration.SuspendLayout();
            this.grpCalibration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLinearity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOverscan)).BeginInit();
            this.SuspendLayout();
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(10, 730);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(80, 30);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "⬅ Back";
            this.btnBack.UseVisualStyleBackColor = false;
            // 
            // tabControlPlayers
            // 
            this.tabControlPlayers.Controls.Add(this.tabPage1);
            this.tabControlPlayers.Controls.Add(this.tabPage2);
            this.tabControlPlayers.Controls.Add(this.tabPage3);
            this.tabControlPlayers.Controls.Add(this.tabPage4);
            this.tabControlPlayers.Location = new System.Drawing.Point(10, 10);
            this.tabControlPlayers.Name = "tabControlPlayers";
            this.tabControlPlayers.SelectedIndex = 0;
            this.tabControlPlayers.Size = new System.Drawing.Size(540, 30);
            this.tabControlPlayers.TabIndex = 1;
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
            // tabControlSettings
            // 
            this.tabControlSettings.Controls.Add(this.tabPageMappings);
            this.tabControlSettings.Controls.Add(this.tabPageCalibration);
            this.tabControlSettings.Location = new System.Drawing.Point(10, 50);
            this.tabControlSettings.Name = "tabControlSettings";
            this.tabControlSettings.SelectedIndex = 0;
            this.tabControlSettings.Size = new System.Drawing.Size(540, 660);
            this.tabControlSettings.TabIndex = 2;
            // 
            // tabPageMappings
            // 
            this.tabPageMappings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.tabPageMappings.Controls.Add(this.grpAxes);
            this.tabPageMappings.Controls.Add(this.grpButtons);
            this.tabPageMappings.Location = new System.Drawing.Point(4, 24);
            this.tabPageMappings.Name = "tabPageMappings";
            this.tabPageMappings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageMappings.Size = new System.Drawing.Size(532, 632);
            this.tabPageMappings.TabIndex = 0;
            this.tabPageMappings.Text = "Mappings";
            // 
            // grpAxes
            // 
            this.grpAxes.Controls.Add(this.cboNunchukAxis);
            this.grpAxes.Controls.Add(this.lblNunchukAxis);
            this.grpAxes.Controls.Add(this.cboIRAxis);
            this.grpAxes.Controls.Add(this.lblIRAxis);
            this.grpAxes.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpAxes.ForeColor = System.Drawing.Color.White;
            this.grpAxes.Location = new System.Drawing.Point(6, 6);
            this.grpAxes.Name = "grpAxes";
            this.grpAxes.Size = new System.Drawing.Size(520, 100);
            this.grpAxes.TabIndex = 0;
            this.grpAxes.TabStop = false;
            this.grpAxes.Text = "Analog Sticks Mapping";
            // 
            // cboNunchukAxis
            // 
            this.cboNunchukAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cboNunchukAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboNunchukAxis.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboNunchukAxis.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboNunchukAxis.ForeColor = System.Drawing.Color.White;
            this.cboNunchukAxis.FormattingEnabled = true;
            this.cboNunchukAxis.Location = new System.Drawing.Point(200, 60);
            this.cboNunchukAxis.Name = "cboNunchukAxis";
            this.cboNunchukAxis.Size = new System.Drawing.Size(200, 23);
            this.cboNunchukAxis.TabIndex = 3;
            // 
            // lblNunchukAxis
            // 
            this.lblNunchukAxis.AutoSize = true;
            this.lblNunchukAxis.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblNunchukAxis.Location = new System.Drawing.Point(20, 63);
            this.lblNunchukAxis.Name = "lblNunchukAxis";
            this.lblNunchukAxis.Size = new System.Drawing.Size(130, 15);
            this.lblNunchukAxis.TabIndex = 2;
            this.lblNunchukAxis.Text = "Nunchuk Joystick Axis:";
            // 
            // cboIRAxis
            // 
            this.cboIRAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cboIRAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboIRAxis.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboIRAxis.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboIRAxis.ForeColor = System.Drawing.Color.White;
            this.cboIRAxis.FormattingEnabled = true;
            this.cboIRAxis.Location = new System.Drawing.Point(200, 25);
            this.cboIRAxis.Name = "cboIRAxis";
            this.cboIRAxis.Size = new System.Drawing.Size(200, 23);
            this.cboIRAxis.TabIndex = 1;
            // 
            // lblIRAxis
            // 
            this.lblIRAxis.AutoSize = true;
            this.lblIRAxis.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblIRAxis.Location = new System.Drawing.Point(20, 28);
            this.lblIRAxis.Name = "lblIRAxis";
            this.lblIRAxis.Size = new System.Drawing.Size(109, 15);
            this.lblIRAxis.TabIndex = 0;
            this.lblIRAxis.Text = "Wiimote IR Sensor:";
            // 
            // grpButtons
            // 
            this.grpButtons.Controls.Add(this.flowLayoutPanelButtons);
            this.grpButtons.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpButtons.ForeColor = System.Drawing.Color.White;
            this.grpButtons.Location = new System.Drawing.Point(6, 112);
            this.grpButtons.Name = "grpButtons";
            this.grpButtons.Size = new System.Drawing.Size(520, 514);
            this.grpButtons.TabIndex = 1;
            this.grpButtons.TabStop = false;
            this.grpButtons.Text = "Buttons Mapping";
            // 
            // flowLayoutPanelButtons
            // 
            this.flowLayoutPanelButtons.AutoScroll = true;
            this.flowLayoutPanelButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelButtons.Location = new System.Drawing.Point(3, 19);
            this.flowLayoutPanelButtons.Name = "flowLayoutPanelButtons";
            this.flowLayoutPanelButtons.Size = new System.Drawing.Size(514, 492);
            this.flowLayoutPanelButtons.TabIndex = 0;
            // 
            // tabPageCalibration
            // 
            this.tabPageCalibration.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.tabPageCalibration.Controls.Add(this.grpCalibration);
            this.tabPageCalibration.Location = new System.Drawing.Point(4, 24);
            this.tabPageCalibration.Name = "tabPageCalibration";
            this.tabPageCalibration.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageCalibration.Size = new System.Drawing.Size(532, 632);
            this.tabPageCalibration.TabIndex = 1;
            this.tabPageCalibration.Text = "IR Calibration";
            // 
            // grpCalibration
            // 
            this.grpCalibration.Controls.Add(this.lblLinearityTitle);
            this.grpCalibration.Controls.Add(this.numLinearity);
            this.grpCalibration.Controls.Add(this.lblLinearityDesc);
            this.grpCalibration.Controls.Add(this.lblOverscanTitle);
            this.grpCalibration.Controls.Add(this.numOverscan);
            this.grpCalibration.Controls.Add(this.lblOverscanDesc);
            this.grpCalibration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpCalibration.ForeColor = System.Drawing.Color.White;
            this.grpCalibration.Location = new System.Drawing.Point(10, 10);
            this.grpCalibration.Name = "grpCalibration";
            this.grpCalibration.Size = new System.Drawing.Size(512, 250);
            this.grpCalibration.TabIndex = 0;
            this.grpCalibration.TabStop = false;
            this.grpCalibration.Text = "IR Sensor Optimization";
            // 
            // lblLinearityTitle
            // 
            this.lblLinearityTitle.AutoSize = true;
            this.lblLinearityTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLinearityTitle.Location = new System.Drawing.Point(20, 40);
            this.lblLinearityTitle.Name = "lblLinearityTitle";
            this.lblLinearityTitle.Size = new System.Drawing.Size(121, 15);
            this.lblLinearityTitle.TabIndex = 0;
            this.lblLinearityTitle.Text = "IR Linearity (S-Curve):";
            // 
            // numLinearity
            // 
            this.numLinearity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.numLinearity.DecimalPlaces = 2;
            this.numLinearity.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numLinearity.ForeColor = System.Drawing.Color.White;
            this.numLinearity.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this.numLinearity.Location = new System.Drawing.Point(200, 38);
            this.numLinearity.Maximum = new decimal(new int[] { 40, 0, 0, 65536 });
            this.numLinearity.Minimum = new decimal(new int[] { 5, 0, 0, 65536 });
            this.numLinearity.Name = "numLinearity";
            this.numLinearity.Size = new System.Drawing.Size(80, 23);
            this.numLinearity.TabIndex = 1;
            this.numLinearity.Value = new decimal(new int[] { 13, 0, 0, 65536 });
            // 
            // lblLinearityDesc
            // 
            this.lblLinearityDesc.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblLinearityDesc.ForeColor = System.Drawing.Color.Silver;
            this.lblLinearityDesc.Location = new System.Drawing.Point(20, 70);
            this.lblLinearityDesc.Name = "lblLinearityDesc";
            this.lblLinearityDesc.Size = new System.Drawing.Size(470, 45);
            this.lblLinearityDesc.TabIndex = 2;
            this.lblLinearityDesc.Text = "Adjusts cursor acceleration towards edges. Higher values (1.3+) fix cursor advancing faster than aimed.\r\n(EN/FR: Ajuste l\'accélération vers les bords. > 1.3 corrige l\'avance du curseur.)";
            // 
            // lblOverscanTitle
            // 
            this.lblOverscanTitle.AutoSize = true;
            this.lblOverscanTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblOverscanTitle.Location = new System.Drawing.Point(20, 130);
            this.lblOverscanTitle.Name = "lblOverscanTitle";
            this.lblOverscanTitle.Size = new System.Drawing.Size(117, 15);
            this.lblOverscanTitle.TabIndex = 3;
            this.lblOverscanTitle.Text = "IR Overscan (Margin):";
            // 
            // numOverscan
            // 
            this.numOverscan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.numOverscan.DecimalPlaces = 2;
            this.numOverscan.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numOverscan.ForeColor = System.Drawing.Color.White;
            this.numOverscan.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numOverscan.Location = new System.Drawing.Point(200, 128);
            this.numOverscan.Maximum = new decimal(new int[] { 45, 0, 0, 131072 });
            this.numOverscan.Name = "numOverscan";
            this.numOverscan.Size = new System.Drawing.Size(80, 23);
            this.numOverscan.TabIndex = 4;
            this.numOverscan.Value = new decimal(new int[] { 10, 0, 0, 131072 });
            // 
            // lblOverscanDesc
            // 
            this.lblOverscanDesc.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblOverscanDesc.ForeColor = System.Drawing.Color.Silver;
            this.lblOverscanDesc.Location = new System.Drawing.Point(20, 160);
            this.lblOverscanDesc.Name = "lblOverscanDesc";
            this.lblOverscanDesc.Size = new System.Drawing.Size(470, 45);
            this.lblOverscanDesc.TabIndex = 5;
            this.lblOverscanDesc.Text = "Margin before reaching 100% axis value. 0.10 (10%) is standard. Max 0.45.\r\n(EN/FR: Marge avant d\'atteindre 100% de l\'axe. 0.10 conseillé. Max 0.45.)";
            // 
            // btnApply
            // 
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(400, 730);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(150, 30);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "💾 Save Changes";
            this.btnApply.UseVisualStyleBackColor = false;
            this.btnApply.Click += new System.EventHandler(this.BtnApply_Click);
            // 
            // GamePadMappingControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.tabControlSettings);
            this.Controls.Add(this.tabControlPlayers);
            this.Controls.Add(this.btnBack);
            this.Name = "GamePadMappingControl";
            this.Size = new System.Drawing.Size(560, 780);
            this.tabControlPlayers.ResumeLayout(false);
            this.tabControlSettings.ResumeLayout(false);
            this.tabPageMappings.ResumeLayout(false);
            this.grpAxes.ResumeLayout(false);
            this.grpAxes.PerformLayout();
            this.grpButtons.ResumeLayout(false);
            this.tabPageCalibration.ResumeLayout(false);
            this.grpCalibration.ResumeLayout(false);
            this.grpCalibration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLinearity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOverscan)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.TabControl tabControlPlayers;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabControl tabControlSettings;
        private System.Windows.Forms.TabPage tabPageMappings;
        private System.Windows.Forms.TabPage tabPageCalibration;
        private System.Windows.Forms.GroupBox grpCalibration;
        private System.Windows.Forms.Label lblLinearityTitle;
        private System.Windows.Forms.NumericUpDown numLinearity;
        private System.Windows.Forms.Label lblLinearityDesc;
        private System.Windows.Forms.Label lblOverscanTitle;
        private System.Windows.Forms.NumericUpDown numOverscan;
        private System.Windows.Forms.Label lblOverscanDesc;
        private System.Windows.Forms.GroupBox grpAxes;
        private System.Windows.Forms.Label lblIRAxis;
        private System.Windows.Forms.ComboBox cboIRAxis;
        private System.Windows.Forms.Label lblNunchukAxis;
        private System.Windows.Forms.ComboBox cboNunchukAxis;
        private System.Windows.Forms.GroupBox grpButtons;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelButtons;
        private System.Windows.Forms.Button btnApply;
    }
}
