namespace WiimoteGun
{
    partial class ModalInputDialog
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
            if (disposing)
            {

                _promptLabel?.Dispose();
                _inputTextBox?.Dispose();
                _okButton?.Dispose();
                _cancelButton?.Dispose();
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
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._inputTextBox = new System.Windows.Forms.TextBox();
            this._promptLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _cancelButton
            // 
            this._cancelButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._cancelButton.FlatAppearance.BorderSize = 0;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._cancelButton.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._cancelButton.ForeColor = System.Drawing.Color.White;
            this._cancelButton.Location = new System.Drawing.Point(255, 100);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 35);
            this._cancelButton.TabIndex = 3;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = false;
            this._cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // _okButton
            // 
            this._okButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this._okButton.FlatAppearance.BorderSize = 0;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._okButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._okButton.ForeColor = System.Drawing.Color.White;
            this._okButton.Location = new System.Drawing.Point(170, 100);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 35);
            this._okButton.TabIndex = 2;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = false;
            this._okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // _inputTextBox
            // 
            this._inputTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this._inputTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._inputTextBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._inputTextBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._inputTextBox.Location = new System.Drawing.Point(20, 55);
            this._inputTextBox.Name = "_inputTextBox";
            this._inputTextBox.Size = new System.Drawing.Size(310, 25);
            this._inputTextBox.TabIndex = 1;
            this._inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputTextBox_KeyDown);
            // 
            // _promptLabel
            // 
            this._promptLabel.Font = new System.Drawing.Font("Segoe UI", 11F);
            this._promptLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this._promptLabel.Location = new System.Drawing.Point(20, 20);
            this._promptLabel.Name = "_promptLabel";
            this._promptLabel.Size = new System.Drawing.Size(310, 25);
            this._promptLabel.TabIndex = 0;
            this._promptLabel.Text = "Enter folder name:";
            // 
            // ModalInputDialog
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.ClientSize = new System.Drawing.Size(350, 160);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._inputTextBox);
            this.Controls.Add(this._promptLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModalInputDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _promptLabel;
        private System.Windows.Forms.TextBox _inputTextBox;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
    }
}
