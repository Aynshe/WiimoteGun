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
            this._lblModifier = new System.Windows.Forms.Label();
            this._cmbModifier = new System.Windows.Forms.ComboBox();
            this._lblTrigger = new System.Windows.Forms.Label();
            this._cmbTriggerButton = new System.Windows.Forms.ComboBox();
            this._grpShort = new System.Windows.Forms.GroupBox();
            this._txtShortKeys = new System.Windows.Forms.TextBox();
            this._btnCaptureShort = new System.Windows.Forms.Button();
            this._btnClearShort = new System.Windows.Forms.Button();
            this._grpLong = new System.Windows.Forms.GroupBox();
            this._txtLongKeys = new System.Windows.Forms.TextBox();
            this._btnCaptureLong = new System.Windows.Forms.Button();
            this._btnClearLong = new System.Windows.Forms.Button();
            this._lblDescription = new System.Windows.Forms.Label();
            this._txtDescription = new System.Windows.Forms.TextBox();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._cbSharedHotkey = new System.Windows.Forms.CheckBox();
            this._grpShort.SuspendLayout();
            this._grpLong.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblModifier
            // 
            this._lblModifier.AutoSize = true;
            this._lblModifier.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblModifier.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblModifier.Location = new System.Drawing.Point(20, 20);
            this._lblModifier.Name = "_lblModifier";
            this._lblModifier.Size = new System.Drawing.Size(111, 19);
            this._lblModifier.TabIndex = 8;
            this._lblModifier.Text = "Modifier Button:";
            // 
            // _cmbModifier
            // 
            this._cmbModifier.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._cmbModifier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbModifier.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._cmbModifier.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._cmbModifier.FormattingEnabled = true;
            this._cmbModifier.Location = new System.Drawing.Point(150, 17);
            this._cmbModifier.Name = "_cmbModifier";
            this._cmbModifier.Size = new System.Drawing.Size(250, 25);
            this._cmbModifier.TabIndex = 9;
            this._cmbModifier.SelectedIndexChanged += new System.EventHandler(this.CmbModifier_SelectedIndexChanged);
            // 
            // _lblTrigger
            // 
            this._lblTrigger.AutoSize = true;
            this._lblTrigger.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblTrigger.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblTrigger.Location = new System.Drawing.Point(20, 60);
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
            this._cmbTriggerButton.Location = new System.Drawing.Point(150, 57);
            this._cmbTriggerButton.Name = "_cmbTriggerButton";
            this._cmbTriggerButton.Size = new System.Drawing.Size(250, 25);
            this._cmbTriggerButton.TabIndex = 1;
            // 
            // _grpShort
            // 
            this._grpShort.Controls.Add(this._txtShortKeys);
            this._grpShort.Controls.Add(this._btnCaptureShort);
            this._grpShort.Controls.Add(this._btnClearShort);
            this._grpShort.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._grpShort.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this._grpShort.Location = new System.Drawing.Point(20, 100);
            this._grpShort.Name = "_grpShort";
            this._grpShort.Size = new System.Drawing.Size(380, 100);
            this._grpShort.TabIndex = 2;
            this._grpShort.TabStop = false;
            this._grpShort.Text = "Short Press Action (< 500ms)";
            // 
            // _txtShortKeys
            // 
            this._txtShortKeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._txtShortKeys.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtShortKeys.ForeColor = System.Drawing.Color.White;
            this._txtShortKeys.Location = new System.Drawing.Point(15, 25);
            this._txtShortKeys.Name = "_txtShortKeys";
            this._txtShortKeys.ReadOnly = true;
            this._txtShortKeys.Size = new System.Drawing.Size(350, 25);
            this._txtShortKeys.TabIndex = 0;
            // 
            // _btnCaptureShort
            // 
            this._btnCaptureShort.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this._btnCaptureShort.FlatAppearance.BorderSize = 0;
            this._btnCaptureShort.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCaptureShort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnCaptureShort.ForeColor = System.Drawing.Color.White;
            this._btnCaptureShort.Location = new System.Drawing.Point(15, 60);
            this._btnCaptureShort.Name = "_btnCaptureShort";
            this._btnCaptureShort.Size = new System.Drawing.Size(250, 30);
            this._btnCaptureShort.TabIndex = 1;
            this._btnCaptureShort.Text = "🎹 Capture Short Keys";
            this._btnCaptureShort.UseVisualStyleBackColor = false;
            this._btnCaptureShort.Click += new System.EventHandler(this.BtnCaptureShort_Click);
            // 
            // _btnClearShort
            // 
            this._btnClearShort.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnClearShort.FlatAppearance.BorderSize = 0;
            this._btnClearShort.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearShort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnClearShort.ForeColor = System.Drawing.Color.White;
            this._btnClearShort.Location = new System.Drawing.Point(275, 60);
            this._btnClearShort.Name = "_btnClearShort";
            this._btnClearShort.Size = new System.Drawing.Size(90, 30);
            this._btnClearShort.TabIndex = 2;
            this._btnClearShort.Text = "Clear";
            this._btnClearShort.UseVisualStyleBackColor = false;
            this._btnClearShort.Click += new System.EventHandler(this.BtnClearShort_Click);
            // 
            // _grpLong
            // 
            this._grpLong.Controls.Add(this._txtLongKeys);
            this._grpLong.Controls.Add(this._btnCaptureLong);
            this._grpLong.Controls.Add(this._btnClearLong);
            this._grpLong.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._grpLong.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this._grpLong.Location = new System.Drawing.Point(20, 210);
            this._grpLong.Name = "_grpLong";
            this._grpLong.Size = new System.Drawing.Size(380, 100);
            this._grpLong.TabIndex = 3;
            this._grpLong.TabStop = false;
            this._grpLong.Text = "Long Press Action (≥ 500ms)";
            // 
            // _txtLongKeys
            // 
            this._txtLongKeys.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._txtLongKeys.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtLongKeys.ForeColor = System.Drawing.Color.White;
            this._txtLongKeys.Location = new System.Drawing.Point(15, 25);
            this._txtLongKeys.Name = "_txtLongKeys";
            this._txtLongKeys.ReadOnly = true;
            this._txtLongKeys.Size = new System.Drawing.Size(350, 25);
            this._txtLongKeys.TabIndex = 0;
            // 
            // _btnCaptureLong
            // 
            this._btnCaptureLong.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this._btnCaptureLong.FlatAppearance.BorderSize = 0;
            this._btnCaptureLong.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCaptureLong.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnCaptureLong.ForeColor = System.Drawing.Color.White;
            this._btnCaptureLong.Location = new System.Drawing.Point(15, 60);
            this._btnCaptureLong.Name = "_btnCaptureLong";
            this._btnCaptureLong.Size = new System.Drawing.Size(250, 30);
            this._btnCaptureLong.TabIndex = 1;
            this._btnCaptureLong.Text = "🎹 Capture Long Keys";
            this._btnCaptureLong.UseVisualStyleBackColor = false;
            this._btnCaptureLong.Click += new System.EventHandler(this.BtnCaptureLong_Click);
            // 
            // _btnClearLong
            // 
            this._btnClearLong.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnClearLong.FlatAppearance.BorderSize = 0;
            this._btnClearLong.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearLong.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnClearLong.ForeColor = System.Drawing.Color.White;
            this._btnClearLong.Location = new System.Drawing.Point(275, 60);
            this._btnClearLong.Name = "_btnClearLong";
            this._btnClearLong.Size = new System.Drawing.Size(90, 30);
            this._btnClearLong.TabIndex = 2;
            this._btnClearLong.Text = "Clear";
            this._btnClearLong.UseVisualStyleBackColor = false;
            this._btnClearLong.Click += new System.EventHandler(this.BtnClearLong_Click);
            // 
            // _lblDescription
            // 
            this._lblDescription.AutoSize = true;
            this._lblDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._lblDescription.Location = new System.Drawing.Point(20, 325);
            this._lblDescription.Name = "_lblDescription";
            this._lblDescription.Size = new System.Drawing.Size(81, 19);
            this._lblDescription.TabIndex = 4;
            this._lblDescription.Text = "Description:";
            // 
            // _txtDescription
            // 
            this._txtDescription.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this._txtDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._txtDescription.Location = new System.Drawing.Point(150, 322);
            this._txtDescription.MaxLength = 50;
            this._txtDescription.Name = "_txtDescription";
            this._txtDescription.Size = new System.Drawing.Size(250, 25);
            this._txtDescription.TabIndex = 5;
            this._txtDescription.Click += new System.EventHandler(this._txtDescription_Click);
            // 
            // _btnOK
            // 
            this._btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this._btnOK.FlatAppearance.BorderSize = 0;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnOK.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnOK.ForeColor = System.Drawing.Color.White;
            this._btnOK.Location = new System.Drawing.Point(190, 370);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(100, 35);
            this._btnOK.TabIndex = 6;
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
            this._btnCancel.Location = new System.Drawing.Point(300, 370);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(100, 35);
            this._btnCancel.TabIndex = 7;
            this._btnCancel.Text = "✕ Cancel";
            this._btnCancel.UseVisualStyleBackColor = false;
            // 
            // _cbSharedHotkey
            // 
            this._cbSharedHotkey.AutoSize = true;
            this._cbSharedHotkey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._cbSharedHotkey.ForeColor = System.Drawing.Color.White;
            this._cbSharedHotkey.Location = new System.Drawing.Point(150, 350);
            this._cbSharedHotkey.Name = "_cbSharedHotkey";
            this._cbSharedHotkey.Size = new System.Drawing.Size(183, 19);
            this._cbSharedHotkey.TabIndex = 10;
            this._cbSharedHotkey.Text = "Share with all players (Shared)";
            this._cbSharedHotkey.UseVisualStyleBackColor = true;
            // 
            // HotkeyInputDialog
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(430, 430);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._cbSharedHotkey);
            this.Controls.Add(this._txtDescription);
            this.Controls.Add(this._lblDescription);
            this.Controls.Add(this._grpLong);
            this.Controls.Add(this._grpShort);
            this.Controls.Add(this._cmbTriggerButton);
            this.Controls.Add(this._lblTrigger);
            this.Controls.Add(this._cmbModifier);
            this.Controls.Add(this._lblModifier);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HotkeyInputDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hotkey";
            this._grpShort.ResumeLayout(false);
            this._grpShort.PerformLayout();
            this._grpLong.ResumeLayout(false);
            this._grpLong.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblModifier;
        private System.Windows.Forms.ComboBox _cmbModifier;
        private System.Windows.Forms.Label _lblTrigger;
        private System.Windows.Forms.ComboBox _cmbTriggerButton;
        private System.Windows.Forms.GroupBox _grpShort;
        private System.Windows.Forms.TextBox _txtShortKeys;
        private System.Windows.Forms.Button _btnCaptureShort;
        private System.Windows.Forms.Button _btnClearShort;
        private System.Windows.Forms.GroupBox _grpLong;
        private System.Windows.Forms.TextBox _txtLongKeys;
        private System.Windows.Forms.Button _btnCaptureLong;
        private System.Windows.Forms.Button _btnClearLong;
        private System.Windows.Forms.Label _lblDescription;
        private System.Windows.Forms.TextBox _txtDescription;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.CheckBox _cbSharedHotkey;
    }
}

