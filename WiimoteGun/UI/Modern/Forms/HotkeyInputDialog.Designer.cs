namespace WiimoteGun
{
    partial class HotkeyInputDialog
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
            this._lblTrigger = new System.Windows.Forms.Label();
            this._cmbTriggerButton = new System.Windows.Forms.ComboBox();
            this.lblType = new System.Windows.Forms.Label();
            this._rbShort = new System.Windows.Forms.RadioButton();
            this._rbLong = new System.Windows.Forms.RadioButton();
            this._lblKeys = new System.Windows.Forms.Label();
            this._txtKeys = new System.Windows.Forms.TextBox();
            this._btnCaptureKeys = new System.Windows.Forms.Button();
            this._btnClearKeys = new System.Windows.Forms.Button();
            this._lblDescription = new System.Windows.Forms.Label();
            this._txtDescription = new System.Windows.Forms.TextBox();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblTrigger
            // 
            this._lblTrigger.AutoSize = true;
            this._lblTrigger.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblTrigger.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblTrigger.Location = new System.Drawing.Point(20, 20);
            this._lblTrigger.Name = "_lblTrigger";
            this._lblTrigger.Size = new System.Drawing.Size(99, 19);
            this._lblTrigger.TabIndex = 0;
            this._lblTrigger.Text = "Trigger Button:";
            // 
            // _cmbTriggerButton
            // 
            this._cmbTriggerButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._cmbTriggerButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbTriggerButton.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._cmbTriggerButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._cmbTriggerButton.FormattingEnabled = true;
            this._cmbTriggerButton.Items.AddRange(new object[] {
            "A",
            "B",
            "One",
            "Two",
            "Minus",
            "Up",
            "Down",
            "Left",
            "Right"});
            this._cmbTriggerButton.Location = new System.Drawing.Point(150, 17);
            this._cmbTriggerButton.Name = "_cmbTriggerButton";
            this._cmbTriggerButton.Size = new System.Drawing.Size(250, 25);
            this._cmbTriggerButton.TabIndex = 1;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblType.Location = new System.Drawing.Point(20, 60);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(76, 19);
            this.lblType.TabIndex = 2;
            this.lblType.Text = "Press Type:";
            // 
            // _rbShort
            // 
            this._rbShort.AutoSize = true;
            this._rbShort.Checked = true;
            this._rbShort.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._rbShort.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._rbShort.Location = new System.Drawing.Point(150, 60);
            this._rbShort.Name = "_rbShort";
            this._rbShort.Size = new System.Drawing.Size(117, 23);
            this._rbShort.TabIndex = 3;
            this._rbShort.TabStop = true;
            this._rbShort.Text = "Short (<500ms)";
            this._rbShort.UseVisualStyleBackColor = true;
            // 
            // _rbLong
            // 
            this._rbLong.AutoSize = true;
            this._rbLong.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._rbLong.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._rbLong.Location = new System.Drawing.Point(280, 60);
            this._rbLong.Name = "_rbLong";
            this._rbLong.Size = new System.Drawing.Size(120, 23);
            this._rbLong.TabIndex = 4;
            this._rbLong.Text = "Long (≥500ms)";
            this._rbLong.UseVisualStyleBackColor = true;
            // 
            // _lblKeys
            // 
            this._lblKeys.AutoSize = true;
            this._lblKeys.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblKeys.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblKeys.Location = new System.Drawing.Point(20, 100);
            this._lblKeys.Name = "_lblKeys";
            this._lblKeys.Size = new System.Drawing.Size(86, 19);
            this._lblKeys.TabIndex = 5;
            this._lblKeys.Text = "Output Keys:";
            // 
            // _txtKeys
            // 
            this._txtKeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._txtKeys.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtKeys.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._txtKeys.Location = new System.Drawing.Point(150, 97);
            this._txtKeys.Name = "_txtKeys";
            this._txtKeys.ReadOnly = true;
            this._txtKeys.Size = new System.Drawing.Size(250, 25);
            this._txtKeys.TabIndex = 6;
            // 
            // _btnCaptureKeys
            // 
            this._btnCaptureKeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this._btnCaptureKeys.FlatAppearance.BorderSize = 0;
            this._btnCaptureKeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCaptureKeys.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnCaptureKeys.ForeColor = System.Drawing.Color.White;
            this._btnCaptureKeys.Location = new System.Drawing.Point(150, 135);
            this._btnCaptureKeys.Name = "_btnCaptureKeys";
            this._btnCaptureKeys.Size = new System.Drawing.Size(150, 30);
            this._btnCaptureKeys.TabIndex = 7;
            this._btnCaptureKeys.Text = "🎹 Capture Keys";
            this._btnCaptureKeys.UseVisualStyleBackColor = false;
            this._btnCaptureKeys.Click += new System.EventHandler(this.BtnCaptureKeys_Click);
            // 
            // _btnClearKeys
            // 
            this._btnClearKeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this._btnClearKeys.FlatAppearance.BorderSize = 0;
            this._btnClearKeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearKeys.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnClearKeys.ForeColor = System.Drawing.Color.White;
            this._btnClearKeys.Location = new System.Drawing.Point(310, 135);
            this._btnClearKeys.Name = "_btnClearKeys";
            this._btnClearKeys.Size = new System.Drawing.Size(90, 30);
            this._btnClearKeys.TabIndex = 8;
            this._btnClearKeys.Text = "Clear";
            this._btnClearKeys.UseVisualStyleBackColor = false;
            this._btnClearKeys.Click += new System.EventHandler(this._btnClearKeys_Click);
            // 
            // _lblDescription
            // 
            this._lblDescription.AutoSize = true;
            this._lblDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblDescription.Location = new System.Drawing.Point(20, 185);
            this._lblDescription.Name = "_lblDescription";
            this._lblDescription.Size = new System.Drawing.Size(81, 19);
            this._lblDescription.TabIndex = 9;
            this._lblDescription.Text = "Description:";
            // 
            // _txtDescription
            // 
            this._txtDescription.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._txtDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._txtDescription.Location = new System.Drawing.Point(150, 182);
            this._txtDescription.MaxLength = 50;
            this._txtDescription.Name = "_txtDescription";
            this._txtDescription.Size = new System.Drawing.Size(250, 25);
            this._txtDescription.TabIndex = 10;
            this._txtDescription.Click += new System.EventHandler(this._txtDescription_Click);
            // 
            // _btnOK
            // 
            this._btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this._btnOK.FlatAppearance.BorderSize = 0;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnOK.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnOK.ForeColor = System.Drawing.Color.White;
            this._btnOK.Location = new System.Drawing.Point(190, 245);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(100, 35);
            this._btnOK.TabIndex = 11;
            this._btnOK.Text = "✓ OK";
            this._btnOK.UseVisualStyleBackColor = false;
            this._btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatAppearance.BorderSize = 0;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._btnCancel.ForeColor = System.Drawing.Color.White;
            this._btnCancel.Location = new System.Drawing.Point(300, 245);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(100, 35);
            this._btnCancel.TabIndex = 12;
            this._btnCancel.Text = "✕ Cancel";
            this._btnCancel.UseVisualStyleBackColor = false;
            // 
            // HotkeyInputDialog
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(450, 310);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._txtDescription);
            this.Controls.Add(this._lblDescription);
            this.Controls.Add(this._btnClearKeys);
            this.Controls.Add(this._btnCaptureKeys);
            this.Controls.Add(this._txtKeys);
            this.Controls.Add(this._lblKeys);
            this.Controls.Add(this._rbLong);
            this.Controls.Add(this._rbShort);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this._cmbTriggerButton);
            this.Controls.Add(this._lblTrigger);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HotkeyInputDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hotkey";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblTrigger;
        private System.Windows.Forms.ComboBox _cmbTriggerButton;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.RadioButton _rbShort;
        private System.Windows.Forms.RadioButton _rbLong;
        private System.Windows.Forms.Label _lblKeys;
        private System.Windows.Forms.TextBox _txtKeys;
        private System.Windows.Forms.Button _btnCaptureKeys;
        private System.Windows.Forms.Button _btnClearKeys;
        private System.Windows.Forms.Label _lblDescription;
        private System.Windows.Forms.TextBox _txtDescription;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
    }
}
