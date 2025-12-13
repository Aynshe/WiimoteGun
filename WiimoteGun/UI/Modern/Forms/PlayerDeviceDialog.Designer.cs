namespace WiimoteGun
{
    partial class PlayerDeviceDialog
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblMouseTitle = new System.Windows.Forms.Label();
            this.cmbMouse = new System.Windows.Forms.ComboBox();
            this.chkLockMouse = new System.Windows.Forms.CheckBox();
            this.lblMouseInfo = new System.Windows.Forms.Label();
            this.lblKeyboardTitle = new System.Windows.Forms.Label();
            this.cmbKeyboard = new System.Windows.Forms.ComboBox();
            this.chkLockKeyboard = new System.Windows.Forms.CheckBox();
            this.lblKeyboardInfo = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(410, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Player Device Configuration";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblMouseTitle
            // 
            this.lblMouseTitle.Text = "🖱️ Mouse Device:";
            this.lblMouseTitle.Location = new System.Drawing.Point(20, 70);
            this.lblMouseTitle.Size = new System.Drawing.Size(150, 25);
            this.lblMouseTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblMouseTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblMouseTitle.Name = "lblMouseTitle";
            this.lblMouseTitle.TabIndex = 1;
            // 
            // cmbMouse
            // 
            this.cmbMouse.Location = new System.Drawing.Point(20, 100);
            this.cmbMouse.Size = new System.Drawing.Size(340, 25);
            this.cmbMouse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMouse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbMouse.ForeColor = System.Drawing.Color.White;
            this.cmbMouse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbMouse.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbMouse.Name = "cmbMouse";
            this.cmbMouse.TabIndex = 2;
            this.cmbMouse.SelectedIndexChanged += new System.EventHandler(this.CmbMouse_SelectedIndexChanged);
            // 
            // chkLockMouse
            // 
            this.chkLockMouse.Text = "🔒";
            this.chkLockMouse.Location = new System.Drawing.Point(370, 100);
            this.chkLockMouse.Size = new System.Drawing.Size(50, 25);
            this.chkLockMouse.ForeColor = System.Drawing.Color.White;
            this.chkLockMouse.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.chkLockMouse.Name = "chkLockMouse";
            this.chkLockMouse.TabIndex = 3;
            this.chkLockMouse.CheckedChanged += new System.EventHandler(this.ChkLockMouse_CheckedChanged);
            // 
            // lblMouseInfo
            // 
            this.lblMouseInfo.Text = "";
            this.lblMouseInfo.Location = new System.Drawing.Point(20, 130);
            this.lblMouseInfo.Size = new System.Drawing.Size(400, 20);
            this.lblMouseInfo.ForeColor = System.Drawing.Color.Gray;
            this.lblMouseInfo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            this.lblMouseInfo.Name = "lblMouseInfo";
            this.lblMouseInfo.TabIndex = 4;
            // 
            // lblKeyboardTitle
            // 
            this.lblKeyboardTitle.Text = "⌨️ Keyboard Device:";
            this.lblKeyboardTitle.Location = new System.Drawing.Point(20, 170);
            this.lblKeyboardTitle.Size = new System.Drawing.Size(180, 25);
            this.lblKeyboardTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblKeyboardTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblKeyboardTitle.Name = "lblKeyboardTitle";
            this.lblKeyboardTitle.TabIndex = 5;
            // 
            // cmbKeyboard
            // 
            this.cmbKeyboard.Location = new System.Drawing.Point(20, 200);
            this.cmbKeyboard.Size = new System.Drawing.Size(340, 25);
            this.cmbKeyboard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKeyboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbKeyboard.ForeColor = System.Drawing.Color.White;
            this.cmbKeyboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbKeyboard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbKeyboard.Name = "cmbKeyboard";
            this.cmbKeyboard.TabIndex = 6;
            this.cmbKeyboard.SelectedIndexChanged += new System.EventHandler(this.CmbKeyboard_SelectedIndexChanged);
            // 
            // chkLockKeyboard
            // 
            this.chkLockKeyboard.Text = "🔒";
            this.chkLockKeyboard.Location = new System.Drawing.Point(370, 200);
            this.chkLockKeyboard.Size = new System.Drawing.Size(50, 25);
            this.chkLockKeyboard.ForeColor = System.Drawing.Color.White;
            this.chkLockKeyboard.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.chkLockKeyboard.Name = "chkLockKeyboard";
            this.chkLockKeyboard.TabIndex = 7;
            this.chkLockKeyboard.CheckedChanged += new System.EventHandler(this.ChkLockKeyboard_CheckedChanged);
            // 
            // lblKeyboardInfo
            // 
            this.lblKeyboardInfo.Text = "";
            this.lblKeyboardInfo.Location = new System.Drawing.Point(20, 230);
            this.lblKeyboardInfo.Size = new System.Drawing.Size(400, 20);
            this.lblKeyboardInfo.ForeColor = System.Drawing.Color.Gray;
            this.lblKeyboardInfo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            this.lblKeyboardInfo.Name = "lblKeyboardInfo";
            this.lblKeyboardInfo.TabIndex = 8;
            // 
            // btnApply
            // 
            this.btnApply.Text = "✓ Apply";
            this.btnApply.Size = new System.Drawing.Size(120, 35);
            this.btnApply.Location = new System.Drawing.Point(130, 270);
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnApply.Name = "btnApply";
            this.btnApply.TabIndex = 9;
            this.btnApply.UseVisualStyleBackColor = false;
            this.btnApply.Click += new System.EventHandler(this.BtnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Text = "✗ Cancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 35);
            this.btnCancel.Location = new System.Drawing.Point(260, 270);
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 10;
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // PlayerDeviceDialog
            // 
            this.ClientSize = new System.Drawing.Size(450, 380);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.lblKeyboardInfo);
            this.Controls.Add(this.chkLockKeyboard);
            this.Controls.Add(this.cmbKeyboard);
            this.Controls.Add(this.lblKeyboardTitle);
            this.Controls.Add(this.lblMouseInfo);
            this.Controls.Add(this.chkLockMouse);
            this.Controls.Add(this.cmbMouse);
            this.Controls.Add(this.lblMouseTitle);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlayerDeviceDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Device Selection";
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblMouseTitle;
        private System.Windows.Forms.ComboBox cmbMouse;
        private System.Windows.Forms.CheckBox chkLockMouse;
        private System.Windows.Forms.Label lblMouseInfo;
        private System.Windows.Forms.Label lblKeyboardTitle;
        private System.Windows.Forms.ComboBox cmbKeyboard;
        private System.Windows.Forms.CheckBox chkLockKeyboard;
        private System.Windows.Forms.Label lblKeyboardInfo;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
    }
}
