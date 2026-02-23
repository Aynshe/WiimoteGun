namespace WiimoteGun.UI.Modern.Forms
{
    partial class GyroVisualizerForm
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
            this.components = new System.ComponentModel.Container();
            this.lblPlayer = new System.Windows.Forms.Label();
            this.cbPlayerSelect = new System.Windows.Forms.ComboBox();
            this.pbGyroCanvas = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnCalibrateGyro = new System.Windows.Forms.Button();
            this.lblHelp = new System.Windows.Forms.Label();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pbGyroCanvas)).BeginInit();
            this.SuspendLayout();
            // 
            // lblPlayer
            // 
            this.lblPlayer.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPlayer.ForeColor = System.Drawing.Color.White;
            this.lblPlayer.Location = new System.Drawing.Point(20, 20);
            this.lblPlayer.Name = "lblPlayer";
            this.lblPlayer.Size = new System.Drawing.Size(100, 25);
            this.lblPlayer.TabIndex = 0;
            this.lblPlayer.Text = "Select Player:";
            // 
            // cbPlayerSelect
            // 
            this.cbPlayerSelect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cbPlayerSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPlayerSelect.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbPlayerSelect.ForeColor = System.Drawing.Color.White;
            this.cbPlayerSelect.FormattingEnabled = true;
            this.cbPlayerSelect.Items.AddRange(new object[] {
            "Player 1",
            "Player 2",
            "Player 3",
            "Player 4"});
            this.cbPlayerSelect.Location = new System.Drawing.Point(130, 20);
            this.cbPlayerSelect.Name = "cbPlayerSelect";
            this.cbPlayerSelect.Size = new System.Drawing.Size(150, 23);
            this.cbPlayerSelect.TabIndex = 1;
            this.cbPlayerSelect.SelectedIndexChanged += new System.EventHandler(this.CbPlayerSelect_SelectedIndexChanged);
            // 
            // pbGyroCanvas
            // 
            this.pbGyroCanvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.pbGyroCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbGyroCanvas.Location = new System.Drawing.Point(20, 60);
            this.pbGyroCanvas.Name = "pbGyroCanvas";
            this.pbGyroCanvas.Size = new System.Drawing.Size(750, 350);
            this.pbGyroCanvas.TabIndex = 2;
            this.pbGyroCanvas.TabStop = false;
            this.pbGyroCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.PbGyroCanvas_Paint);
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.LightGray;
            this.lblTitle.Location = new System.Drawing.Point(790, 60);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(170, 25);
            this.lblTitle.TabIndex = 3;
            this.lblTitle.Text = "📐 Orientation:";
            // 
 
            // btnCalibrateGyro
            // 
            this.btnCalibrateGyro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(102)))), ((int)(((byte)(0)))));
            this.btnCalibrateGyro.FlatAppearance.BorderSize = 0;
            this.btnCalibrateGyro.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalibrateGyro.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalibrateGyro.ForeColor = System.Drawing.Color.White;
            this.btnCalibrateGyro.Location = new System.Drawing.Point(790, 340);
            this.btnCalibrateGyro.Name = "btnCalibrateGyro";
            this.btnCalibrateGyro.Size = new System.Drawing.Size(180, 55);
            this.btnCalibrateGyro.TabIndex = 8;
            this.btnCalibrateGyro.Text = "🔧 Calibrate Gyro\n(Set Zero Position)";
            this.btnCalibrateGyro.UseVisualStyleBackColor = false;
            this.btnCalibrateGyro.Click += new System.EventHandler(this.BtnCalibrateGyro_Click);
            // 
            // lblHelp
            // 
            this.lblHelp.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblHelp.ForeColor = System.Drawing.Color.Gray;
            this.lblHelp.Location = new System.Drawing.Point(20, 420);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(760, 60);
            this.lblHelp.TabIndex = 9;
            this.lblHelp.Text = "The 3D cube shows Wiimote orientation. Nunchuk shown on right if connected.\r\n• Mo" +
    "tionPlus: Precise gyroscope data\r\n• Fallback: Accelerometer tilt estimate\r\n\r\nPl" +
    "ace Wiimote flat and click Calibrate to zero.";
            // 
            // refreshTimer
            // 
            this.refreshTimer.Interval = 33;
            // 
            // GyroVisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(1000, 500);
            this.Controls.Add(this.lblHelp);
            this.Controls.Add(this.btnCalibrateGyro);

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.pbGyroCanvas);
            this.Controls.Add(this.cbPlayerSelect);
            this.Controls.Add(this.lblPlayer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GyroVisualizerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "🎯 Gyroscope Visualizer - 3D Orientation";
            ((System.ComponentModel.ISupportInitialize)(this.pbGyroCanvas)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblPlayer;
        private System.Windows.Forms.ComboBox cbPlayerSelect;
        private System.Windows.Forms.PictureBox pbGyroCanvas;
            private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnCalibrateGyro;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.Timer refreshTimer;
    }
}
