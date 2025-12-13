namespace WiimoteGun.UI.Calibrate
{
    partial class CalibrationModeSelectionForm
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
            this._lblTitle = new System.Windows.Forms.Label();
            this._lblSubtitle = new System.Windows.Forms.Label();
            this._btnDynamic = new System.Windows.Forms.Button();
            this._btnStandard = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblTitle
            // 
            this._lblTitle.AutoSize = false;
            this._lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTitle.Font = new System.Drawing.Font("Arial", 36F, System.Drawing.FontStyle.Bold);
            this._lblTitle.ForeColor = System.Drawing.Color.White;
            this._lblTitle.Height = 100;
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(800, 100);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "CHOOSE TRACKING MODE";
            this._lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblSubtitle
            // 
            this._lblSubtitle.AutoSize = false;
            this._lblSubtitle.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblSubtitle.Font = new System.Drawing.Font("Arial", 20F, System.Drawing.FontStyle.Regular);
            this._lblSubtitle.ForeColor = System.Drawing.Color.LightGray;
            this._lblSubtitle.Height = 60;
            this._lblSubtitle.Name = "_lblSubtitle";
            this._lblSubtitle.Size = new System.Drawing.Size(800, 60);
            this._lblSubtitle.TabIndex = 1;
            this._lblSubtitle.Text = "Select your preferred tracking method";
            this._lblSubtitle.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // _btnDynamic
            // 
            this._btnDynamic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this._btnDynamic.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnDynamic.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDynamic.Font = new System.Drawing.Font("Arial", 24F, System.Drawing.FontStyle.Bold);
            this._btnDynamic.ForeColor = System.Drawing.Color.White;
            this._btnDynamic.Location = new System.Drawing.Point(100, 250);
            this._btnDynamic.Name = "_btnDynamic";
            this._btnDynamic.Size = new System.Drawing.Size(600, 400);
            this._btnDynamic.TabIndex = 2;
            this._btnDynamic.Text = "DYNAMIC MODE" + System.Environment.NewLine + "(AUTO)" + System.Environment.NewLine + System.Environment.NewLine + "✓ No calibration needed" + System.Environment.NewLine + "✓ Fixes staircase effect" + System.Environment.NewLine + "✓ Absolute perspective";
            this._btnDynamic.UseVisualStyleBackColor = false;
            this._btnDynamic.Click += new System.EventHandler(this.BtnDynamic_Click);
            this._btnDynamic.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this._btnDynamic.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // _btnStandard
            // 
            this._btnStandard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnStandard.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnStandard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnStandard.Font = new System.Drawing.Font("Arial", 24F, System.Drawing.FontStyle.Bold);
            this._btnStandard.ForeColor = System.Drawing.Color.White;
            this._btnStandard.Location = new System.Drawing.Point(750, 250);
            this._btnStandard.Name = "_btnStandard";
            this._btnStandard.Size = new System.Drawing.Size(600, 400);
            this._btnStandard.TabIndex = 3;
            this._btnStandard.Text = "STANDARD CALIBRATION" + System.Environment.NewLine + "(MANUAL)" + System.Environment.NewLine + System.Environment.NewLine + "• Classic 5-point calibration" + System.Environment.NewLine + "• Manual precision" + System.Environment.NewLine + "• Use if Dynamic is inaccurate";
            this._btnStandard.UseVisualStyleBackColor = false;
            this._btnStandard.Click += new System.EventHandler(this.BtnStandard_Click);
            this._btnStandard.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this._btnStandard.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // CalibrationModeSelectionForm
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this._btnStandard);
            this.Controls.Add(this._btnDynamic);
            this.Controls.Add(this._lblSubtitle);
            this.Controls.Add(this._lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "CalibrationModeSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Calibration Mode Selection";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CalibrationModeSelectionForm_KeyDown);
            this.Load += new System.EventHandler(this.CalibrationModeSelectionForm_Load);
            this.Resize += new System.EventHandler(this.CalibrationModeSelectionForm_Resize);
            this.Shown += new System.EventHandler(this.CalibrationModeSelectionForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblSubtitle;
        private System.Windows.Forms.Button _btnDynamic;
        private System.Windows.Forms.Button _btnStandard;
    }
}
