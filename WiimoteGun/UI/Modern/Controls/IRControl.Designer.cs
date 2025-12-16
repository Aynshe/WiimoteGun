namespace WiimoteGun.Controls
{
    partial class IRControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && _irRefreshTimer != null)
            {
                _irRefreshTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblPlayerSelect = new System.Windows.Forms.Label();
            this.cbIRPlayerSelect = new System.Windows.Forms.ComboBox();
            this.pbIRCanvas = new System.Windows.Forms.PictureBox();
            this.gbCalib = new System.Windows.Forms.GroupBox();
            this.lblX = new System.Windows.Forms.Label();
            this.nudIROffsetX = new System.Windows.Forms.NumericUpDown();
            this.lblY = new System.Windows.Forms.Label();
            this.nudIROffsetY = new System.Windows.Forms.NumericUpDown();
            this.btnSaveCalib = new System.Windows.Forms.Button();
            this.lblHelp = new System.Windows.Forms.Label();
            this.lblWiimoteHelp = new System.Windows.Forms.Label();
            this.btnOpenGyroViz = new System.Windows.Forms.Button();
            
            ((System.ComponentModel.ISupportInitialize)(this.pbIRCanvas)).BeginInit();
            this.gbCalib.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIROffsetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIROffsetY)).BeginInit();
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
            this.lblTitle.Text = "🎯 IR Visualizer & Calibration";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // 
            // lblPlayerSelect
            // 
            this.lblPlayerSelect.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPlayerSelect.ForeColor = System.Drawing.Color.White;
            this.lblPlayerSelect.Location = new System.Drawing.Point(20, 60);
            this.lblPlayerSelect.Name = "lblPlayerSelect";
            this.lblPlayerSelect.Size = new System.Drawing.Size(100, 25);
            this.lblPlayerSelect.TabIndex = 1;
            this.lblPlayerSelect.Text = "Select Player:";
            
            // 
            // cbIRPlayerSelect
            // 
            this.cbIRPlayerSelect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cbIRPlayerSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbIRPlayerSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbIRPlayerSelect.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbIRPlayerSelect.ForeColor = System.Drawing.Color.White;
            this.cbIRPlayerSelect.FormattingEnabled = true;
            this.cbIRPlayerSelect.Items.AddRange(new object[] {
            "Player 1",
            "Player 2",
            "Player 3",
            "Player 4"});
            this.cbIRPlayerSelect.Location = new System.Drawing.Point(130, 60);
            this.cbIRPlayerSelect.Name = "cbIRPlayerSelect";
            this.cbIRPlayerSelect.Size = new System.Drawing.Size(150, 25);
            this.cbIRPlayerSelect.TabIndex = 2;
            
            // 
            // pbIRCanvas
            // 
            this.pbIRCanvas.BackColor = System.Drawing.Color.Black;
            this.pbIRCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbIRCanvas.Location = new System.Drawing.Point(20, 100);
            this.pbIRCanvas.Name = "pbIRCanvas";
            this.pbIRCanvas.Size = new System.Drawing.Size(520, 390);
            this.pbIRCanvas.TabIndex = 3;
            this.pbIRCanvas.TabStop = false;
            
            // 
            // gbCalib
            // 
            this.gbCalib.Controls.Add(this.lblHelp);
            this.gbCalib.Controls.Add(this.btnSaveCalib);
            this.gbCalib.Controls.Add(this.nudIROffsetY);
            this.gbCalib.Controls.Add(this.lblY);
            this.gbCalib.Controls.Add(this.nudIROffsetX);
            this.gbCalib.Controls.Add(this.lblX);
            this.gbCalib.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.gbCalib.ForeColor = System.Drawing.Color.White;
            this.gbCalib.Location = new System.Drawing.Point(20, 500);
            this.gbCalib.Name = "gbCalib";
            this.gbCalib.Size = new System.Drawing.Size(520, 100);
            this.gbCalib.TabIndex = 4;
            this.gbCalib.TabStop = false;
            this.gbCalib.Text = "Manual Calibration (Offset)";
            
            // 
            // lblX
            // 
            this.lblX.Location = new System.Drawing.Point(20, 30);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(60, 20);
            this.lblX.TabIndex = 0;
            this.lblX.Text = "Offset X:";
            
            // 
            // nudIROffsetX
            // 
            this.nudIROffsetX.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudIROffsetX.ForeColor = System.Drawing.Color.White;
            this.nudIROffsetX.Location = new System.Drawing.Point(90, 30);
            this.nudIROffsetX.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.nudIROffsetX.Minimum = new decimal(new int[] { 500, 0, 0, -2147483648 });
            this.nudIROffsetX.Name = "nudIROffsetX";
            this.nudIROffsetX.Size = new System.Drawing.Size(80, 25);
            this.nudIROffsetX.TabIndex = 1;
            
            // 
            // lblY
            // 
            this.lblY.Location = new System.Drawing.Point(200, 30);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(60, 20);
            this.lblY.TabIndex = 2;
            this.lblY.Text = "Offset Y:";
            
            // 
            // nudIROffsetY
            // 
            this.nudIROffsetY.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudIROffsetY.ForeColor = System.Drawing.Color.White;
            this.nudIROffsetY.Location = new System.Drawing.Point(270, 30);
            this.nudIROffsetY.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.nudIROffsetY.Minimum = new decimal(new int[] { 500, 0, 0, -2147483648 });
            this.nudIROffsetY.Name = "nudIROffsetY";
            this.nudIROffsetY.Size = new System.Drawing.Size(80, 25);
            this.nudIROffsetY.TabIndex = 3;
            
            // 
            // btnSaveCalib
            // 
            this.btnSaveCalib.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSaveCalib.FlatAppearance.BorderSize = 0;
            this.btnSaveCalib.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveCalib.Location = new System.Drawing.Point(380, 25);
            this.btnSaveCalib.Name = "btnSaveCalib";
            this.btnSaveCalib.Size = new System.Drawing.Size(120, 35);
            this.btnSaveCalib.TabIndex = 4;
            this.btnSaveCalib.Text = "💾 Save Offset";
            this.btnSaveCalib.UseVisualStyleBackColor = false;
            
            // 
            // lblHelp
            // 
            this.lblHelp.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblHelp.ForeColor = System.Drawing.Color.Gray;
            this.lblHelp.Location = new System.Drawing.Point(20, 70);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(480, 20);
            this.lblHelp.TabIndex = 5;
            this.lblHelp.Text = "Adjust offsets if the crosshair is not aligned with your aim.";
            
            // 
            // lblWiimoteHelp
            // 
            this.lblWiimoteHelp.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblWiimoteHelp.ForeColor = System.Drawing.Color.LightBlue;
            this.lblWiimoteHelp.Location = new System.Drawing.Point(20, 610);
            this.lblWiimoteHelp.Name = "lblWiimoteHelp";
            this.lblWiimoteHelp.Size = new System.Drawing.Size(520, 20);
            this.lblWiimoteHelp.TabIndex = 5;
            this.lblWiimoteHelp.Text = "💡 Hold HOME (BT) or MINUS (Mayflash) + D-Pad to adjust offset";
            
            // 
            // btnOpenGyroViz
            // 
            this.btnOpenGyroViz.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnOpenGyroViz.FlatAppearance.BorderSize = 0;
            this.btnOpenGyroViz.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenGyroViz.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnOpenGyroViz.ForeColor = System.Drawing.Color.White;
            this.btnOpenGyroViz.Location = new System.Drawing.Point(285, 640);
            this.btnOpenGyroViz.Name = "btnOpenGyroViz";
            this.btnOpenGyroViz.Size = new System.Drawing.Size(255, 45);
            this.btnOpenGyroViz.TabIndex = 6;
            this.btnOpenGyroViz.Text = "🎯 Open 3D Gyro Visualizer";
            this.btnOpenGyroViz.UseVisualStyleBackColor = false;

            // 
            // btnOpenFullScreenIR
            // 
            this.btnOpenFullScreenIR = new System.Windows.Forms.Button();
            this.btnOpenFullScreenIR.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0))))); // Greenish
            this.btnOpenFullScreenIR.FlatAppearance.BorderSize = 0;
            this.btnOpenFullScreenIR.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFullScreenIR.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnOpenFullScreenIR.ForeColor = System.Drawing.Color.White;
            this.btnOpenFullScreenIR.Location = new System.Drawing.Point(20, 640);
            this.btnOpenFullScreenIR.Name = "btnOpenFullScreenIR";
            this.btnOpenFullScreenIR.Size = new System.Drawing.Size(255, 45);
            this.btnOpenFullScreenIR.TabIndex = 8;
            this.btnOpenFullScreenIR.Text = "📺 Fullscreen IR Visualizer";
            this.btnOpenFullScreenIR.UseVisualStyleBackColor = false;

            // 
            // btnBack
            // 
            this.btnBack = new System.Windows.Forms.Button();
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(20, 730);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(80, 30);
            this.btnBack.TabIndex = 7;
            this.btnBack.Text = "⬅ Back";
            this.btnBack.UseVisualStyleBackColor = false;
            
            // 
            // IRControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnOpenFullScreenIR);
            this.Controls.Add(this.btnOpenGyroViz);
            this.Controls.Add(this.lblWiimoteHelp);
            this.Controls.Add(this.gbCalib);
            this.Controls.Add(this.pbIRCanvas);
            this.Controls.Add(this.cbIRPlayerSelect);
            this.Controls.Add(this.lblPlayerSelect);
            this.Controls.Add(this.lblTitle);
            this.Name = "IRControl";
            this.Size = new System.Drawing.Size(560, 780);
            ((System.ComponentModel.ISupportInitialize)(this.pbIRCanvas)).EndInit();
            this.gbCalib.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudIROffsetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIROffsetY)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPlayerSelect;
        private System.Windows.Forms.ComboBox cbIRPlayerSelect;
        private System.Windows.Forms.PictureBox pbIRCanvas;
        private System.Windows.Forms.GroupBox gbCalib;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.NumericUpDown nudIROffsetX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.NumericUpDown nudIROffsetY;
        private System.Windows.Forms.Button btnSaveCalib;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.Label lblWiimoteHelp;
        private System.Windows.Forms.Button btnOpenGyroViz;
        private System.Windows.Forms.Button btnOpenFullScreenIR;
        public System.Windows.Forms.Button btnBack;
    }
}
