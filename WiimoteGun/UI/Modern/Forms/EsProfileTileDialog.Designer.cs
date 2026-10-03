namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V44] Designer part of the profile tile modal (editable in Visual Studio).
    /// FR: [V44] Partie Designer de la modale en tuiles des profils (éditable dans Visual Studio).
    /// </summary>
    partial class EsProfileTileDialog
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.lblDialogTitle = new System.Windows.Forms.Label();
            this.btnDialogClose = new System.Windows.Forms.Button();
            this.pnlTabs = new System.Windows.Forms.Panel();
            this.btnTabMouse = new System.Windows.Forms.Button();
            this.btnTabGamePad = new System.Windows.Forms.Button();
            this.pnlNav = new System.Windows.Forms.Panel();
            this.btnNavHome = new System.Windows.Forms.Button();
            this.btnNavBack = new System.Windows.Forms.Button();
            this.btnShowAll = new System.Windows.Forms.Button();
            this.lblCurrentPath = new System.Windows.Forms.Label();
            this.flowTiles = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.btnApiSwap = new System.Windows.Forms.Button();
            this.lblApiSwapInfo = new System.Windows.Forms.Label();
            this.pnlTitle.SuspendLayout();
            this.pnlTabs.SuspendLayout();
            this.pnlNav.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTitle
            // 
            this.pnlTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.pnlTitle.Controls.Add(this.btnDialogClose);
            this.pnlTitle.Controls.Add(this.lblDialogTitle);
            this.pnlTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Location = new System.Drawing.Point(0, 0);
            this.pnlTitle.Name = "pnlTitle";
            this.pnlTitle.Size = new System.Drawing.Size(780, 46);
            this.pnlTitle.TabIndex = 4;
            // 
            // lblDialogTitle
            // 
            this.lblDialogTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDialogTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblDialogTitle.ForeColor = System.Drawing.Color.White;
            this.lblDialogTitle.Name = "lblDialogTitle";
            this.lblDialogTitle.Padding = new System.Windows.Forms.Padding(14, 0, 60, 0);
            this.lblDialogTitle.Size = new System.Drawing.Size(780, 46);
            this.lblDialogTitle.TabIndex = 0;
            this.lblDialogTitle.Text = "PROFILES";
            this.lblDialogTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnDialogClose
            // 
            this.btnDialogClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDialogClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.btnDialogClose.FlatAppearance.BorderSize = 0;
            this.btnDialogClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDialogClose.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDialogClose.ForeColor = System.Drawing.Color.White;
            this.btnDialogClose.Location = new System.Drawing.Point(734, 7);
            this.btnDialogClose.Name = "btnDialogClose";
            this.btnDialogClose.Size = new System.Drawing.Size(40, 32);
            this.btnDialogClose.TabIndex = 1;
            this.btnDialogClose.Text = "X";
            this.btnDialogClose.UseVisualStyleBackColor = false;
            // 
            // pnlTabs
            // 
            this.pnlTabs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pnlTabs.Controls.Add(this.btnTabMouse);
            this.pnlTabs.Controls.Add(this.btnTabGamePad);
            this.pnlTabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTabs.Location = new System.Drawing.Point(0, 46);
            this.pnlTabs.Name = "pnlTabs";
            this.pnlTabs.Size = new System.Drawing.Size(780, 44);
            this.pnlTabs.TabIndex = 3;
            // 
            // btnTabMouse
            // 
            this.btnTabMouse.FlatAppearance.BorderSize = 0;
            this.btnTabMouse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabMouse.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTabMouse.ForeColor = System.Drawing.Color.White;
            this.btnTabMouse.Location = new System.Drawing.Point(20, 5);
            this.btnTabMouse.Name = "btnTabMouse";
            this.btnTabMouse.Size = new System.Drawing.Size(175, 34);
            this.btnTabMouse.TabIndex = 0;
            this.btnTabMouse.Text = "Mouse Profiles";
            this.btnTabMouse.UseVisualStyleBackColor = true;
            // 
            // btnTabGamePad
            // 
            this.btnTabGamePad.FlatAppearance.BorderSize = 0;
            this.btnTabGamePad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabGamePad.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTabGamePad.ForeColor = System.Drawing.Color.White;
            this.btnTabGamePad.Location = new System.Drawing.Point(205, 5);
            this.btnTabGamePad.Name = "btnTabGamePad";
            this.btnTabGamePad.Size = new System.Drawing.Size(175, 34);
            this.btnTabGamePad.TabIndex = 1;
            this.btnTabGamePad.Text = "GamePad Profiles";
            this.btnTabGamePad.UseVisualStyleBackColor = true;
            // 
            // pnlNav
            // 
            this.pnlNav.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.pnlNav.Controls.Add(this.btnNavHome);
            this.pnlNav.Controls.Add(this.btnNavBack);
            this.pnlNav.Controls.Add(this.btnShowAll);
            this.pnlNav.Controls.Add(this.lblCurrentPath);
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlNav.Location = new System.Drawing.Point(0, 90);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Size = new System.Drawing.Size(780, 40);
            this.pnlNav.TabIndex = 2;
            // 
            // btnNavHome
            // 
            this.btnNavHome.FlatAppearance.BorderSize = 0;
            this.btnNavHome.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavHome.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNavHome.ForeColor = System.Drawing.Color.White;
            this.btnNavHome.Location = new System.Drawing.Point(20, 5);
            this.btnNavHome.Name = "btnNavHome";
            this.btnNavHome.Size = new System.Drawing.Size(115, 30);
            this.btnNavHome.TabIndex = 0;
            this.btnNavHome.Text = "🏠 Home";
            this.btnNavHome.UseVisualStyleBackColor = true;
            // 
            // btnNavBack
            // 
            this.btnNavBack.FlatAppearance.BorderSize = 0;
            this.btnNavBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavBack.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNavBack.ForeColor = System.Drawing.Color.White;
            this.btnNavBack.Location = new System.Drawing.Point(145, 5);
            this.btnNavBack.Name = "btnNavBack";
            this.btnNavBack.Size = new System.Drawing.Size(115, 30);
            this.btnNavBack.TabIndex = 1;
            this.btnNavBack.Text = "⬅ Back";
            this.btnNavBack.UseVisualStyleBackColor = true;
            // 
            // btnShowAll
            // 
            this.btnShowAll.FlatAppearance.BorderSize = 0;
            this.btnShowAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnShowAll.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnShowAll.ForeColor = System.Drawing.Color.White;
            this.btnShowAll.Location = new System.Drawing.Point(270, 5);
            this.btnShowAll.Name = "btnShowAll";
            this.btnShowAll.Size = new System.Drawing.Size(150, 30);
            this.btnShowAll.TabIndex = 3;
            this.btnShowAll.Text = "🌐 All profiles";
            this.btnShowAll.UseVisualStyleBackColor = true;
            // 
            // lblCurrentPath
            // 
            this.lblCurrentPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurrentPath.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCurrentPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblCurrentPath.Location = new System.Drawing.Point(430, 5);
            this.lblCurrentPath.Name = "lblCurrentPath";
            this.lblCurrentPath.Size = new System.Drawing.Size(330, 30);
            this.lblCurrentPath.TabIndex = 2;
            this.lblCurrentPath.Text = "(root)";
            this.lblCurrentPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowTiles
            // 
            this.flowTiles.AutoScroll = true;
            this.flowTiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.flowTiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowTiles.Location = new System.Drawing.Point(0, 130);
            this.flowTiles.Name = "flowTiles";
            this.flowTiles.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            this.flowTiles.Size = new System.Drawing.Size(780, 314);
            this.flowTiles.TabIndex = 0;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(24)))), ((int)(((byte)(24)))));
            this.pnlFooter.Controls.Add(this.btnApiSwap);
            this.pnlFooter.Controls.Add(this.lblApiSwapInfo);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 444);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(780, 96);
            this.pnlFooter.TabIndex = 1;
            // 
            // btnApiSwap
            // 
            this.btnApiSwap.FlatAppearance.BorderSize = 0;
            this.btnApiSwap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApiSwap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnApiSwap.ForeColor = System.Drawing.Color.White;
            this.btnApiSwap.Location = new System.Drawing.Point(14, 24);
            this.btnApiSwap.Name = "btnApiSwap";
            this.btnApiSwap.Size = new System.Drawing.Size(340, 48);
            this.btnApiSwap.TabIndex = 0;
            this.btnApiSwap.Text = "GamePad API: XInput";
            this.btnApiSwap.UseVisualStyleBackColor = true;
            // 
            // lblApiSwapInfo
            // 
            this.lblApiSwapInfo.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblApiSwapInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.lblApiSwapInfo.Location = new System.Drawing.Point(370, 16);
            this.lblApiSwapInfo.Name = "lblApiSwapInfo";
            this.lblApiSwapInfo.Size = new System.Drawing.Size(396, 64);
            this.lblApiSwapInfo.TabIndex = 1;
            this.lblApiSwapInfo.Text = "Toggles the GamePad API (XInput / DInput-VMulti) for ALL players. Applied at the next GamePad activation - use it BEFORE launching a game. Profile auto-load never changes the API.";
            // 
            // EsProfileTileDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(780, 540);
            this.Controls.Add(this.flowTiles);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlNav);
            this.Controls.Add(this.pnlTabs);
            this.Controls.Add(this.pnlTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "EsProfileTileDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WiimoteGun Profiles";
            this.TopMost = true;
            this.pnlTitle.ResumeLayout(false);
            this.pnlTabs.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlTitle;
        private System.Windows.Forms.Label lblDialogTitle;
        private System.Windows.Forms.Button btnDialogClose;
        private System.Windows.Forms.Panel pnlTabs;
        private System.Windows.Forms.Button btnTabMouse;
        private System.Windows.Forms.Button btnTabGamePad;
        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.Button btnNavHome;
        private System.Windows.Forms.Button btnNavBack;
        private System.Windows.Forms.Button btnShowAll;
        private System.Windows.Forms.Label lblCurrentPath;
        private System.Windows.Forms.FlowLayoutPanel flowTiles;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Button btnApiSwap;
        private System.Windows.Forms.Label lblApiSwapInfo;
    }
}
