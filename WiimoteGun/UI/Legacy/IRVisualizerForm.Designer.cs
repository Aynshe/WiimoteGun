namespace WiimoteGun.UI.Legacy
{
    partial class IRVisualizerForm
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
            this.chkShowCalibration = new System.Windows.Forms.CheckBox();
            this.cmbPlayer = new System.Windows.Forms.ComboBox();
            this.btnOffsetLeft = new System.Windows.Forms.Button();
            this.btnOffsetRight = new System.Windows.Forms.Button();
            this.btnOffsetUp = new System.Windows.Forms.Button();
            this.btnOffsetDown = new System.Windows.Forms.Button();
            this.lblOffsetValue = new System.Windows.Forms.Label();
            this.btnSaveOffset = new System.Windows.Forms.Button();
            this.lblWiimoteInfo = new System.Windows.Forms.Label();
            this._updateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // chkShowCalibration
            // 
            this.chkShowCalibration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkShowCalibration.AutoSize = true;
            this.chkShowCalibration.BackColor = System.Drawing.Color.Transparent;
            this.chkShowCalibration.ForeColor = System.Drawing.Color.White;
            this.chkShowCalibration.Location = new System.Drawing.Point(10, 570);
            this.chkShowCalibration.Name = "chkShowCalibration";
            this.chkShowCalibration.Size = new System.Drawing.Size(105, 17);
            this.chkShowCalibration.TabIndex = 0;
            this.chkShowCalibration.Text = "Show Calibration";
            this.chkShowCalibration.UseVisualStyleBackColor = false;
            // 
            // cmbPlayer
            // 
            this.cmbPlayer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbPlayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlayer.FormattingEnabled = true;
            this.cmbPlayer.Items.AddRange(new object[] {
            "Player 1",
            "Player 2",
            "Player 3",
            "Player 4"});
            this.cmbPlayer.Location = new System.Drawing.Point(340, 530);
            this.cmbPlayer.Name = "cmbPlayer";
            this.cmbPlayer.Size = new System.Drawing.Size(100, 21);
            this.cmbPlayer.TabIndex = 1;
            this.cmbPlayer.SelectedIndexChanged += new System.EventHandler(this.CmbPlayer_SelectedIndexChanged);
            this.cmbPlayer.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CmbPlayer_KeyDown);
            // 
            // btnOffsetLeft
            // 
            this.btnOffsetLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOffsetLeft.Location = new System.Drawing.Point(180, 530);
            this.btnOffsetLeft.Name = "btnOffsetLeft";
            this.btnOffsetLeft.Size = new System.Drawing.Size(35, 30);
            this.btnOffsetLeft.TabIndex = 2;
            this.btnOffsetLeft.Text = "←";
            this.btnOffsetLeft.UseVisualStyleBackColor = true;
            this.btnOffsetLeft.Click += new System.EventHandler(this.BtnOffsetLeft_Click);
            // 
            // btnOffsetRight
            // 
            this.btnOffsetRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOffsetRight.Location = new System.Drawing.Point(220, 530);
            this.btnOffsetRight.Name = "btnOffsetRight";
            this.btnOffsetRight.Size = new System.Drawing.Size(35, 30);
            this.btnOffsetRight.TabIndex = 3;
            this.btnOffsetRight.Text = "→";
            this.btnOffsetRight.UseVisualStyleBackColor = true;
            this.btnOffsetRight.Click += new System.EventHandler(this.BtnOffsetRight_Click);
            // 
            // btnOffsetUp
            // 
            this.btnOffsetUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOffsetUp.Location = new System.Drawing.Point(260, 530);
            this.btnOffsetUp.Name = "btnOffsetUp";
            this.btnOffsetUp.Size = new System.Drawing.Size(35, 30);
            this.btnOffsetUp.TabIndex = 4;
            this.btnOffsetUp.Text = "▲";
            this.btnOffsetUp.UseVisualStyleBackColor = true;
            this.btnOffsetUp.Click += new System.EventHandler(this.BtnOffsetUp_Click);
            // 
            // btnOffsetDown
            // 
            this.btnOffsetDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOffsetDown.Location = new System.Drawing.Point(300, 530);
            this.btnOffsetDown.Name = "btnOffsetDown";
            this.btnOffsetDown.Size = new System.Drawing.Size(35, 30);
            this.btnOffsetDown.TabIndex = 5;
            this.btnOffsetDown.Text = "▼";
            this.btnOffsetDown.UseVisualStyleBackColor = true;
            this.btnOffsetDown.Click += new System.EventHandler(this.BtnOffsetDown_Click);
            // 
            // lblOffsetValue
            // 
            this.lblOffsetValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblOffsetValue.AutoSize = true;
            this.lblOffsetValue.BackColor = System.Drawing.Color.Transparent;
            this.lblOffsetValue.ForeColor = System.Drawing.Color.Orange;
            this.lblOffsetValue.Location = new System.Drawing.Point(450, 535);
            this.lblOffsetValue.Name = "lblOffsetValue";
            this.lblOffsetValue.Size = new System.Drawing.Size(54, 13);
            this.lblOffsetValue.TabIndex = 6;
            this.lblOffsetValue.Text = "X: 0, Y: 0";
            // 
            // btnSaveOffset
            // 
            this.btnSaveOffset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSaveOffset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSaveOffset.FlatAppearance.BorderSize = 0;
            this.btnSaveOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveOffset.ForeColor = System.Drawing.Color.White;
            this.btnSaveOffset.Location = new System.Drawing.Point(580, 530);
            this.btnSaveOffset.Name = "btnSaveOffset";
            this.btnSaveOffset.Size = new System.Drawing.Size(100, 30);
            this.btnSaveOffset.TabIndex = 7;
            this.btnSaveOffset.Text = "💾 Save Offset";
            this.btnSaveOffset.UseVisualStyleBackColor = false;
            this.btnSaveOffset.Click += new System.EventHandler(this.BtnSaveOffset_Click);
            // 
            // lblWiimoteInfo
            // 
            this.lblWiimoteInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblWiimoteInfo.AutoSize = true;
            this.lblWiimoteInfo.BackColor = System.Drawing.Color.Transparent;
            this.lblWiimoteInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWiimoteInfo.ForeColor = System.Drawing.Color.LightGray;
            this.lblWiimoteInfo.Location = new System.Drawing.Point(180, 565);
            this.lblWiimoteInfo.Name = "lblWiimoteInfo";
            this.lblWiimoteInfo.Size = new System.Drawing.Size(250, 13);
            this.lblWiimoteInfo.TabIndex = 8;
            this.lblWiimoteInfo.Text = "💡 Hold HOME (BT) or MINUS (Mayflash) + D-Pad to adjust offset";
            // 
            // _updateTimer
            // 
            this._updateTimer.Interval = 33;
            // 
            // IRVisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.lblWiimoteInfo);
            this.Controls.Add(this.btnSaveOffset);
            this.Controls.Add(this.lblOffsetValue);
            this.Controls.Add(this.btnOffsetDown);
            this.Controls.Add(this.btnOffsetUp);
            this.Controls.Add(this.btnOffsetRight);
            this.Controls.Add(this.btnOffsetLeft);
            this.Controls.Add(this.cmbPlayer);
            this.Controls.Add(this.chkShowCalibration);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Name = "IRVisualizerForm";
            this.Text = "Wiimote IR Visualizer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkShowCalibration;
        private System.Windows.Forms.ComboBox cmbPlayer;
        private System.Windows.Forms.Button btnOffsetLeft;
        private System.Windows.Forms.Button btnOffsetRight;
        private System.Windows.Forms.Button btnOffsetUp;
        private System.Windows.Forms.Button btnOffsetDown;
        private System.Windows.Forms.Label lblOffsetValue;
        private System.Windows.Forms.Button btnSaveOffset;
        private System.Windows.Forms.Label lblWiimoteInfo;
        private System.Windows.Forms.Timer _updateTimer;
    }
}
