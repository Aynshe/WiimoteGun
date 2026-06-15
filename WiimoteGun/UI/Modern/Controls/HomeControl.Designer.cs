namespace WiimoteGun.Controls
{
    partial class HomeControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblHomeTitle = new System.Windows.Forms.Label();
            this.lblHomeDescription = new System.Windows.Forms.Label();
            this.btnNavOptions = new System.Windows.Forms.Button();
            this.btnNavMapping = new System.Windows.Forms.Button();
            this.btnNavAssign = new System.Windows.Forms.Button();
            this.btnNavIRViz = new System.Windows.Forms.Button();
            this.btnOpenSetupWizard = new System.Windows.Forms.Button();
            this.lblVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblHomeTitle
            // 
            this.lblHomeTitle.AutoSize = true;
            this.lblHomeTitle.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblHomeTitle.ForeColor = System.Drawing.Color.White;
            this.lblHomeTitle.Location = new System.Drawing.Point(80, 50);
            this.lblHomeTitle.Name = "lblHomeTitle";
            this.lblHomeTitle.Size = new System.Drawing.Size(404, 45);
            this.lblHomeTitle.TabIndex = 0;
            this.lblHomeTitle.Text = "Wiimote4Guns - RetroBat";
            // 
            // lblHomeDescription
            // 
            this.lblHomeDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblHomeDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblHomeDescription.Location = new System.Drawing.Point(80, 110);
            this.lblHomeDescription.Name = "lblHomeDescription";
            this.lblHomeDescription.Size = new System.Drawing.Size(400, 50);
            this.lblHomeDescription.TabIndex = 1;
            this.lblHomeDescription.Text = "Lightgun gaming solution for RetroBat\r\nChoose a menu to configure your Wiimotes";
            this.lblHomeDescription.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btnNavOptions
            // 
            this.btnNavOptions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnNavOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavOptions.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnNavOptions.ForeColor = System.Drawing.Color.White;
            this.btnNavOptions.Location = new System.Drawing.Point(160, 200);
            this.btnNavOptions.Name = "btnNavOptions";
            this.btnNavOptions.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.btnNavOptions.Size = new System.Drawing.Size(240, 80);
            this.btnNavOptions.TabIndex = 2;
            this.btnNavOptions.Text = "⚙️ Options";
            this.btnNavOptions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavOptions.UseVisualStyleBackColor = false;
            this.btnNavOptions.Click += new System.EventHandler(this.BtnNavOptions_Click);
            this.btnNavOptions.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this.btnNavOptions.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // btnNavMapping
            // 
            this.btnNavMapping.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnNavMapping.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavMapping.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnNavMapping.ForeColor = System.Drawing.Color.White;
            this.btnNavMapping.Location = new System.Drawing.Point(160, 300);
            this.btnNavMapping.Name = "btnNavMapping";
            this.btnNavMapping.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.btnNavMapping.Size = new System.Drawing.Size(240, 80);
            this.btnNavMapping.TabIndex = 3;
            this.btnNavMapping.Text = "🎮 Button Mapping";
            this.btnNavMapping.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavMapping.UseVisualStyleBackColor = false;
            this.btnNavMapping.Click += new System.EventHandler(this.BtnNavMapping_Click);
            this.btnNavMapping.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this.btnNavMapping.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // btnNavAssign
            // 
            this.btnNavAssign.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnNavAssign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavAssign.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnNavAssign.ForeColor = System.Drawing.Color.White;
            this.btnNavAssign.Location = new System.Drawing.Point(160, 400);
            this.btnNavAssign.Name = "btnNavAssign";
            this.btnNavAssign.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.btnNavAssign.Size = new System.Drawing.Size(240, 80);
            this.btnNavAssign.TabIndex = 4;
            this.btnNavAssign.Text = "📡 Assign Wiimote";
            this.btnNavAssign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavAssign.UseVisualStyleBackColor = false;
            this.btnNavAssign.Click += new System.EventHandler(this.BtnNavAssign_Click);
            this.btnNavAssign.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this.btnNavAssign.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // btnNavIRViz
            // 
            this.btnNavIRViz.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnNavIRViz.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavIRViz.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnNavIRViz.ForeColor = System.Drawing.Color.White;
            this.btnNavIRViz.Location = new System.Drawing.Point(160, 500);
            this.btnNavIRViz.Name = "btnNavIRViz";
            this.btnNavIRViz.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.btnNavIRViz.Size = new System.Drawing.Size(240, 80);
            this.btnNavIRViz.TabIndex = 5;
            this.btnNavIRViz.Text = "📊 IR Visualizer";
            this.btnNavIRViz.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavIRViz.UseVisualStyleBackColor = false;
            this.btnNavIRViz.Click += new System.EventHandler(this.BtnNavIRViz_Click);
            this.btnNavIRViz.MouseEnter += new System.EventHandler(this.Btn_MouseEnter);
            this.btnNavIRViz.MouseLeave += new System.EventHandler(this.Btn_MouseLeave);
            // 
            // btnOpenSetupWizard
            // 
            this.btnOpenSetupWizard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnOpenSetupWizard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenSetupWizard.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnOpenSetupWizard.ForeColor = System.Drawing.Color.White;
            this.btnOpenSetupWizard.Location = new System.Drawing.Point(180, 620);
            this.btnOpenSetupWizard.Name = "btnOpenSetupWizard";
            this.btnOpenSetupWizard.Size = new System.Drawing.Size(200, 45);
            this.btnOpenSetupWizard.TabIndex = 6;
            this.btnOpenSetupWizard.Text = "🔧 Open Setup Wizard";
            this.btnOpenSetupWizard.UseVisualStyleBackColor = false;
            this.btnOpenSetupWizard.Click += new System.EventHandler(this.BtnOpenSetupWizard_Click);
            this.btnOpenSetupWizard.MouseEnter += new System.EventHandler(this.BtnSetup_MouseEnter);
            this.btnOpenSetupWizard.MouseLeave += new System.EventHandler(this.BtnSetup_MouseLeave);
            // 
            // lblVersion
            // 
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lblVersion.Location = new System.Drawing.Point(0, 680);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(560, 25);
            this.lblVersion.TabIndex = 7;
            this.lblVersion.Text = "v0.0.0";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // HomeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnNavIRViz);
            this.Controls.Add(this.btnNavAssign);
            this.Controls.Add(this.btnNavMapping);
            this.Controls.Add(this.btnNavOptions);
            this.Controls.Add(this.btnOpenSetupWizard);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblHomeDescription);
            this.Controls.Add(this.lblHomeTitle);
            this.Name = "HomeControl";
            this.Size = new System.Drawing.Size(560, 720);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblHomeTitle;
        private System.Windows.Forms.Label lblHomeDescription;
        private System.Windows.Forms.Button btnNavOptions;
        private System.Windows.Forms.Button btnNavMapping;
        private System.Windows.Forms.Button btnNavAssign;
        private System.Windows.Forms.Button btnNavIRViz;
        private System.Windows.Forms.Button btnOpenSetupWizard;
        private System.Windows.Forms.Label lblVersion;
    }
}
