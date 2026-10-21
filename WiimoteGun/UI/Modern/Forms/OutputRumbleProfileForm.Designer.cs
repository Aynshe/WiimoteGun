namespace WiimoteGun.UI.Modern.Forms
{
    partial class OutputRumbleProfileForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.lblFire = new System.Windows.Forms.Label();
            this.trkFire = new System.Windows.Forms.TrackBar();
            this.lblFireValue = new System.Windows.Forms.Label();
            this.lblDamage = new System.Windows.Forms.Label();
            this.trkDamage = new System.Windows.Forms.TrackBar();
            this.lblDamageValue = new System.Windows.Forms.Label();
            this.lblFireDur = new System.Windows.Forms.Label();
            this.numFireDur = new System.Windows.Forms.NumericUpDown();
            this.lblDamageDur = new System.Windows.Forms.Label();
            this.numDamageDur = new System.Windows.Forms.NumericUpDown();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trkFire)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFireDur)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDamageDur)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkDamage)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblTitle.Location = new System.Drawing.Point(16, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(220, 19);
            this.lblTitle.Text = "Output rumble - this player";
            //
            // chkEnabled
            //
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkEnabled.ForeColor = System.Drawing.Color.White;
            this.chkEnabled.Location = new System.Drawing.Point(16, 48);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(230, 19);
            this.chkEnabled.Text = "Enable output-driven rumble";
            this.chkEnabled.UseVisualStyleBackColor = true;
            //
            // lblFire
            //
            this.lblFire.AutoSize = true;
            this.lblFire.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFire.ForeColor = System.Drawing.Color.White;
            this.lblFire.Location = new System.Drawing.Point(16, 84);
            this.lblFire.Name = "lblFire";
            this.lblFire.Size = new System.Drawing.Size(60, 15);
            this.lblFire.Text = "Fire force:";
            //
            // trkFire
            //
            this.trkFire.Location = new System.Drawing.Point(96, 80);
            this.trkFire.Maximum = 100;
            this.trkFire.Minimum = 10;
            this.trkFire.Name = "trkFire";
            this.trkFire.Size = new System.Drawing.Size(220, 45);
            this.trkFire.TabIndex = 2;
            this.trkFire.TickFrequency = 10;
            //
            // lblFireValue
            //
            this.lblFireValue.AutoSize = true;
            this.lblFireValue.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFireValue.ForeColor = System.Drawing.Color.White;
            this.lblFireValue.Location = new System.Drawing.Point(322, 84);
            this.lblFireValue.Name = "lblFireValue";
            this.lblFireValue.Size = new System.Drawing.Size(45, 15);
            this.lblFireValue.Text = "100%";
            //
            // lblDamage
            //
            this.lblDamage.AutoSize = true;
            this.lblDamage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDamage.ForeColor = System.Drawing.Color.White;
            this.lblDamage.Location = new System.Drawing.Point(16, 136);
            this.lblDamage.Name = "lblDamage";
            this.lblDamage.Size = new System.Drawing.Size(80, 15);
            this.lblDamage.Text = "Damage force:";
            //
            // trkDamage
            //
            this.trkDamage.Location = new System.Drawing.Point(96, 132);
            this.trkDamage.Maximum = 100;
            this.trkDamage.Minimum = 10;
            this.trkDamage.Name = "trkDamage";
            this.trkDamage.Size = new System.Drawing.Size(220, 45);
            this.trkDamage.TabIndex = 4;
            this.trkDamage.TickFrequency = 10;
            //
            // lblDamageValue
            //
            this.lblDamageValue.AutoSize = true;
            this.lblDamageValue.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDamageValue.ForeColor = System.Drawing.Color.White;
            this.lblDamageValue.Location = new System.Drawing.Point(322, 136);
            this.lblDamageValue.Name = "lblDamageValue";
            this.lblDamageValue.Size = new System.Drawing.Size(45, 15);
            //
            // lblFireDur [V62-OUTPUT-DUR]
            //
            this.lblFireDur.AutoSize = true;
            this.lblFireDur.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFireDur.ForeColor = System.Drawing.Color.White;
            this.lblFireDur.Location = new System.Drawing.Point(16, 190);
            this.lblFireDur.Name = "lblFireDur";
            this.lblFireDur.Size = new System.Drawing.Size(95, 15);
            this.lblFireDur.Text = "Fire length (ms):";
            //
            // numFireDur [V62-OUTPUT-DUR]
            //
            this.numFireDur.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.numFireDur.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numFireDur.ForeColor = System.Drawing.Color.White;
            this.numFireDur.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numFireDur.Location = new System.Drawing.Point(162, 188);
            this.numFireDur.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.numFireDur.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numFireDur.Name = "numFireDur";
            this.numFireDur.Size = new System.Drawing.Size(70, 23);
            this.numFireDur.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblDamageDur [V62-OUTPUT-DUR]
            //
            this.lblDamageDur.AutoSize = true;
            this.lblDamageDur.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDamageDur.ForeColor = System.Drawing.Color.White;
            this.lblDamageDur.Location = new System.Drawing.Point(16, 224);
            this.lblDamageDur.Name = "lblDamageDur";
            this.lblDamageDur.Size = new System.Drawing.Size(108, 15);
            this.lblDamageDur.Text = "Damage length (ms):";
            //
            // numDamageDur [V62-OUTPUT-DUR]
            //
            this.numDamageDur.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.numDamageDur.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numDamageDur.ForeColor = System.Drawing.Color.White;
            this.numDamageDur.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numDamageDur.Location = new System.Drawing.Point(162, 222);
            this.numDamageDur.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.numDamageDur.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numDamageDur.Name = "numDamageDur";
            this.numDamageDur.Size = new System.Drawing.Size(70, 23);
            this.numDamageDur.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // btnOk
            //
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(120, 260);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(90, 28);
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = false;
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(72)))));
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(216, 260);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 28);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            //
            // OutputRumbleProfileForm
            //
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(27)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(384, 314);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.lblFire);
            this.Controls.Add(this.trkFire);
            this.Controls.Add(this.lblFireValue);
            this.Controls.Add(this.lblDamage);
            this.Controls.Add(this.trkDamage);
            this.Controls.Add(this.lblDamageValue);
            this.Controls.Add(this.lblFireDur);
            this.Controls.Add(this.numFireDur);
            this.Controls.Add(this.lblDamageDur);
            this.Controls.Add(this.numDamageDur);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OutputRumbleProfileForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Output rumble";
            ((System.ComponentModel.ISupportInitialize)(this.trkFire)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkDamage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFireDur)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDamageDur)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Label lblFire;
        private System.Windows.Forms.TrackBar trkFire;
        private System.Windows.Forms.Label lblFireValue;
        private System.Windows.Forms.Label lblDamage;
        private System.Windows.Forms.TrackBar trkDamage;
        private System.Windows.Forms.Label lblDamageValue;
        // [V62-OUTPUT-DUR] EN/FR: rumble length per event (ms)
        private System.Windows.Forms.Label lblFireDur;
        private System.Windows.Forms.NumericUpDown numFireDur;
        private System.Windows.Forms.Label lblDamageDur;
        private System.Windows.Forms.NumericUpDown numDamageDur;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
