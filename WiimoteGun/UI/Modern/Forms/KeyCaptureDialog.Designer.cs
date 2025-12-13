namespace WiimoteGun
{
    partial class KeyCaptureDialog
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
            this._lblInfo = new System.Windows.Forms.Label();
            this._txtDisplay = new System.Windows.Forms.TextBox();
            this._btnDone = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // 
            // _lblInfo
            // 
            this._lblInfo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblInfo.ForeColor = System.Drawing.Color.Gray;
            this._lblInfo.Location = new System.Drawing.Point(20, 20);
            this._lblInfo.Name = "_lblInfo";
            this._lblInfo.Size = new System.Drawing.Size(360, 40);
            this._lblInfo.TabIndex = 0;
            this._lblInfo.Text = "Press the keys for your hotkey combination\n(e.g., hold Ctrl+Alt then press F4)";

            // 
            // _txtDisplay
            // 
            this._txtDisplay.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this._txtDisplay.Location = new System.Drawing.Point(20, 70);
            this._txtDisplay.Name = "_txtDisplay";
            this._txtDisplay.ReadOnly = true;
            this._txtDisplay.Size = new System.Drawing.Size(360, 29);
            this._txtDisplay.TabIndex = 1;
            this._txtDisplay.Text = "(Waiting for keys...)";
            this._txtDisplay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // 
            // _btnDone
            // 
            this._btnDone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this._btnDone.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnDone.FlatAppearance.BorderSize = 0;
            this._btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDone.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnDone.ForeColor = System.Drawing.Color.White;
            this._btnDone.Location = new System.Drawing.Point(180, 120);
            this._btnDone.Name = "_btnDone";
            this._btnDone.Size = new System.Drawing.Size(90, 30);
            this._btnDone.TabIndex = 2;
            this._btnDone.Text = "✓ Done";
            this._btnDone.UseVisualStyleBackColor = false;

            // 
            // _btnCancel
            // 
            this._btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatAppearance.BorderSize = 0;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._btnCancel.ForeColor = System.Drawing.Color.White;
            this._btnCancel.Location = new System.Drawing.Point(280, 120);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(100, 30);
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "✕ Cancel";
            this._btnCancel.UseVisualStyleBackColor = false;

            // 
            // KeyCaptureDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.ClientSize = new System.Drawing.Size(400, 170);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnDone);
            this.Controls.Add(this._txtDisplay);
            this.Controls.Add(this._lblInfo);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "KeyCaptureDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Capture Keys";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblInfo;
        private System.Windows.Forms.TextBox _txtDisplay;
        private System.Windows.Forms.Button _btnDone;
        private System.Windows.Forms.Button _btnCancel;
    }
}
