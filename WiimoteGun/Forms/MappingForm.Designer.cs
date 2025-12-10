namespace WiimoteGun
{
    partial class MappingForm
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.playerLabel = new System.Windows.Forms.Label();
            this.playerComboBox = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.comboBoxWiiMinus = new System.Windows.Forms.ComboBox();
            this.labelWiiMinus = new System.Windows.Forms.Label();
            this.comboBoxWiiPlus = new System.Windows.Forms.ComboBox();
            this.labelWiiPlus = new System.Windows.Forms.Label();
            this.comboBoxWiiTwo = new System.Windows.Forms.ComboBox();
            this.labelWiiTwo = new System.Windows.Forms.Label();
            this.comboBoxWiiOne = new System.Windows.Forms.ComboBox();
            this.labelWiiOne = new System.Windows.Forms.Label();
            this.comboBoxWiiRight = new System.Windows.Forms.ComboBox();
            this.labelWiiRight = new System.Windows.Forms.Label();
            this.comboBoxWiiLeft = new System.Windows.Forms.ComboBox();
            this.labelWiiLeft = new System.Windows.Forms.Label();
            this.comboBoxWiiDown = new System.Windows.Forms.ComboBox();
            this.labelWiiDown = new System.Windows.Forms.Label();
            this.comboBoxWiiUp = new System.Windows.Forms.ComboBox();
            this.labelWiiUp = new System.Windows.Forms.Label();
            this.comboBoxWiiB = new System.Windows.Forms.ComboBox();
            this.labelWiiB = new System.Windows.Forms.Label();
            this.comboBoxWiiA = new System.Windows.Forms.ComboBox();
            this.labelWiiA = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.comboBoxNunRight = new System.Windows.Forms.ComboBox();
            this.labelNunRight = new System.Windows.Forms.Label();
            this.comboBoxNunLeft = new System.Windows.Forms.ComboBox();
            this.labelNunLeft = new System.Windows.Forms.Label();
            this.comboBoxNunDown = new System.Windows.Forms.ComboBox();
            this.labelNunDown = new System.Windows.Forms.Label();
            this.comboBoxNunUp = new System.Windows.Forms.ComboBox();
            this.labelNunUp = new System.Windows.Forms.Label();
            this.comboBoxNunZ = new System.Windows.Forms.ComboBox();
            this.labelNunZ = new System.Windows.Forms.Label();
            this.comboBoxNunC = new System.Windows.Forms.ComboBox();
            this.labelNunC = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnSaveProfile = new System.Windows.Forms.Button();
            this.btnLoadProfile = new System.Windows.Forms.Button();
            this.comboBoxProfiles = new System.Windows.Forms.ComboBox();
            this.labelProfiles = new System.Windows.Forms.Label();
            this.comboBoxSubfolders = new System.Windows.Forms.ComboBox();
            this.labelSubfolder = new System.Windows.Forms.Label();
            this.btnNewFolder = new System.Windows.Forms.Button();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.labelProfileName = new System.Windows.Forms.Label();
            this.btnRefreshProfiles = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(324, 358);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 26);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // btnOk
            //
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(243, 358);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 26);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnReset
            //
            this.btnReset = new System.Windows.Forms.Button();
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReset.Location = new System.Drawing.Point(162, 358);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 26);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            //
            // playerLabel
            //
            this.playerLabel.AutoSize = true;
            this.playerLabel.Location = new System.Drawing.Point(12, 18);
            this.playerLabel.Name = "playerLabel";
            this.playerLabel.Size = new System.Drawing.Size(42, 13);
            this.playerLabel.TabIndex = 10;
            this.playerLabel.Text = "Player:";
            //
            // playerComboBox
            //
            this.playerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.playerComboBox.FormattingEnabled = true;
            this.playerComboBox.Items.AddRange(new object[] { "Player 1", "Player 2", "Player 3", "Player 4" });
            this.playerComboBox.Location = new System.Drawing.Point(60, 15);
            this.playerComboBox.Name = "playerComboBox";
            this.playerComboBox.Size = new System.Drawing.Size(121, 21);
            this.playerComboBox.TabIndex = 11;
            this.playerComboBox.SelectedIndexChanged += new System.EventHandler(this.playerComboBox_SelectedIndexChanged);
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 45);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(387, 307);
            this.tabControl1.TabIndex = 9;
            //
            // tabPage1
            //
            this.tabPage1.Controls.Add(this.comboBoxWiiMinus);
            this.tabPage1.Controls.Add(this.labelWiiMinus);
            this.tabPage1.Controls.Add(this.comboBoxWiiPlus);
            this.tabPage1.Controls.Add(this.labelWiiPlus);
            this.tabPage1.Controls.Add(this.comboBoxWiiTwo);
            this.tabPage1.Controls.Add(this.labelWiiTwo);
            this.tabPage1.Controls.Add(this.comboBoxWiiOne);
            this.tabPage1.Controls.Add(this.labelWiiOne);
            this.tabPage1.Controls.Add(this.comboBoxWiiRight);
            this.tabPage1.Controls.Add(this.labelWiiRight);
            this.tabPage1.Controls.Add(this.comboBoxWiiLeft);
            this.tabPage1.Controls.Add(this.labelWiiLeft);
            this.tabPage1.Controls.Add(this.comboBoxWiiDown);
            this.tabPage1.Controls.Add(this.labelWiiDown);
            this.tabPage1.Controls.Add(this.comboBoxWiiUp);
            this.tabPage1.Controls.Add(this.labelWiiUp);
            this.tabPage1.Controls.Add(this.comboBoxWiiB);
            this.tabPage1.Controls.Add(this.labelWiiB);
            this.tabPage1.Controls.Add(this.comboBoxWiiA);
            this.tabPage1.Controls.Add(this.labelWiiA);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(379, 281);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Wiimote";
            this.tabPage1.UseVisualStyleBackColor = true;
            //
            // comboBoxWiiMinus
            //
            this.comboBoxWiiMinus.FormattingEnabled = true;
            this.comboBoxWiiMinus.Location = new System.Drawing.Point(150, 250);
            this.comboBoxWiiMinus.Name = "comboBoxWiiMinus";
            this.comboBoxWiiMinus.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiMinus.TabIndex = 19;
            //
            // labelWiiMinus
            //
            this.labelWiiMinus.AutoSize = true;
            this.labelWiiMinus.Location = new System.Drawing.Point(10, 253);
            this.labelWiiMinus.Name = "labelWiiMinus";
            this.labelWiiMinus.Size = new System.Drawing.Size(71, 13);
            this.labelWiiMinus.TabIndex = 18;
            this.labelWiiMinus.Text = "Minus Button";
            //
            // comboBoxWiiPlus
            //
            this.comboBoxWiiPlus.FormattingEnabled = true;
            this.comboBoxWiiPlus.Location = new System.Drawing.Point(150, 223);
            this.comboBoxWiiPlus.Name = "comboBoxWiiPlus";
            this.comboBoxWiiPlus.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiPlus.TabIndex = 17;
            //
            // labelWiiPlus
            //
            this.labelWiiPlus.AutoSize = true;
            this.labelWiiPlus.Location = new System.Drawing.Point(10, 226);
            this.labelWiiPlus.Name = "labelWiiPlus";
            this.labelWiiPlus.Size = new System.Drawing.Size(64, 13);
            this.labelWiiPlus.TabIndex = 16;
            this.labelWiiPlus.Text = "Plus Button";
            //
            // comboBoxWiiTwo
            //
            this.comboBoxWiiTwo.FormattingEnabled = true;
            this.comboBoxWiiTwo.Location = new System.Drawing.Point(150, 196);
            this.comboBoxWiiTwo.Name = "comboBoxWiiTwo";
            this.comboBoxWiiTwo.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiTwo.TabIndex = 15;
            //
            // labelWiiTwo
            //
            this.labelWiiTwo.AutoSize = true;
            this.labelWiiTwo.Location = new System.Drawing.Point(10, 199);
            this.labelWiiTwo.Name = "labelWiiTwo";
            this.labelWiiTwo.Size = new System.Drawing.Size(66, 13);
            this.labelWiiTwo.TabIndex = 14;
            this.labelWiiTwo.Text = "Two Button";
            //
            // comboBoxWiiOne
            //
            this.comboBoxWiiOne.FormattingEnabled = true;
            this.comboBoxWiiOne.Location = new System.Drawing.Point(150, 169);
            this.comboBoxWiiOne.Name = "comboBoxWiiOne";
            this.comboBoxWiiOne.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiOne.TabIndex = 13;
            //
            // labelWiiOne
            //
            this.labelWiiOne.AutoSize = true;
            this.labelWiiOne.Location = new System.Drawing.Point(10, 172);
            this.labelWiiOne.Name = "labelWiiOne";
            this.labelWiiOne.Size = new System.Drawing.Size(63, 13);
            this.labelWiiOne.TabIndex = 12;
            this.labelWiiOne.Text = "One Button";
            //
            // comboBoxWiiRight
            //
            this.comboBoxWiiRight.FormattingEnabled = true;
            this.comboBoxWiiRight.Location = new System.Drawing.Point(150, 142);
            this.comboBoxWiiRight.Name = "comboBoxWiiRight";
            this.comboBoxWiiRight.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiRight.TabIndex = 11;
            //
            // labelWiiRight
            //
            this.labelWiiRight.AutoSize = true;
            this.labelWiiRight.Location = new System.Drawing.Point(10, 145);
            this.labelWiiRight.Name = "labelWiiRight";
            this.labelWiiRight.Size = new System.Drawing.Size(68, 13);
            this.labelWiiRight.TabIndex = 10;
            this.labelWiiRight.Text = "DPad Right";
            //
            // comboBoxWiiLeft
            //
            this.comboBoxWiiLeft.FormattingEnabled = true;
            this.comboBoxWiiLeft.Location = new System.Drawing.Point(150, 115);
            this.comboBoxWiiLeft.Name = "comboBoxWiiLeft";
            this.comboBoxWiiLeft.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiLeft.TabIndex = 9;
            //
            // labelWiiLeft
            //
            this.labelWiiLeft.AutoSize = true;
            this.labelWiiLeft.Location = new System.Drawing.Point(10, 118);
            this.labelWiiLeft.Name = "labelWiiLeft";
            this.labelWiiLeft.Size = new System.Drawing.Size(61, 13);
            this.labelWiiLeft.TabIndex = 8;
            this.labelWiiLeft.Text = "DPad Left";
            //
            // comboBoxWiiDown
            //
            this.comboBoxWiiDown.FormattingEnabled = true;
            this.comboBoxWiiDown.Location = new System.Drawing.Point(150, 88);
            this.comboBoxWiiDown.Name = "comboBoxWiiDown";
            this.comboBoxWiiDown.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiDown.TabIndex = 7;
            //
            // labelWiiDown
            //
            this.labelWiiDown.AutoSize = true;
            this.labelWiiDown.Location = new System.Drawing.Point(10, 91);
            this.labelWiiDown.Name = "labelWiiDown";
            this.labelWiiDown.Size = new System.Drawing.Size(72, 13);
            this.labelWiiDown.TabIndex = 6;
            this.labelWiiDown.Text = "DPad Down";
            //
            // comboBoxWiiUp
            //
            this.comboBoxWiiUp.FormattingEnabled = true;
            this.comboBoxWiiUp.Location = new System.Drawing.Point(150, 61);
            this.comboBoxWiiUp.Name = "comboBoxWiiUp";
            this.comboBoxWiiUp.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiUp.TabIndex = 5;
            //
            // labelWiiUp
            //
            this.labelWiiUp.AutoSize = true;
            this.labelWiiUp.Location = new System.Drawing.Point(10, 64);
            this.labelWiiUp.Name = "labelWiiUp";
            this.labelWiiUp.Size = new System.Drawing.Size(58, 13);
            this.labelWiiUp.TabIndex = 4;
            this.labelWiiUp.Text = "DPad Up";
            //
            // comboBoxWiiB
            //
            this.comboBoxWiiB.FormattingEnabled = true;
            this.comboBoxWiiB.Location = new System.Drawing.Point(150, 34);
            this.comboBoxWiiB.Name = "comboBoxWiiB";
            this.comboBoxWiiB.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiB.TabIndex = 3;
            //
            // labelWiiB
            //
            this.labelWiiB.AutoSize = true;
            this.labelWiiB.Location = new System.Drawing.Point(10, 37);
            this.labelWiiB.Name = "labelWiiB";
            this.labelWiiB.Size = new System.Drawing.Size(50, 13);
            this.labelWiiB.TabIndex = 2;
            this.labelWiiB.Text = "B Button";
            //
            // comboBoxWiiA
            //
            this.comboBoxWiiA.FormattingEnabled = true;
            this.comboBoxWiiA.Location = new System.Drawing.Point(150, 7);
            this.comboBoxWiiA.Name = "comboBoxWiiA";
            this.comboBoxWiiA.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWiiA.TabIndex = 1;
            //
            // labelWiiA
            //
            this.labelWiiA.AutoSize = true;
            this.labelWiiA.Location = new System.Drawing.Point(10, 10);
            this.labelWiiA.Name = "labelWiiA";
            this.labelWiiA.Size = new System.Drawing.Size(50, 13);
            this.labelWiiA.TabIndex = 0;
            this.labelWiiA.Text = "A Button";
            //
            // tabPage2
            //
            this.tabPage2.Controls.Add(this.comboBoxNunRight);
            this.tabPage2.Controls.Add(this.labelNunRight);
            this.tabPage2.Controls.Add(this.comboBoxNunLeft);
            this.tabPage2.Controls.Add(this.labelNunLeft);
            this.tabPage2.Controls.Add(this.comboBoxNunDown);
            this.tabPage2.Controls.Add(this.labelNunDown);
            this.tabPage2.Controls.Add(this.comboBoxNunUp);
            this.tabPage2.Controls.Add(this.labelNunUp);
            this.tabPage2.Controls.Add(this.comboBoxNunZ);
            this.tabPage2.Controls.Add(this.labelNunZ);
            this.tabPage2.Controls.Add(this.comboBoxNunC);
            this.tabPage2.Controls.Add(this.labelNunC);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(379, 281);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Nunchuk";
            this.tabPage2.UseVisualStyleBackColor = true;
            //
            // comboBoxNunRight
            //
            this.comboBoxNunRight.FormattingEnabled = true;
            this.comboBoxNunRight.Location = new System.Drawing.Point(150, 142);
            this.comboBoxNunRight.Name = "comboBoxNunRight";
            this.comboBoxNunRight.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunRight.TabIndex = 11;
            //
            // labelNunRight
            //
            this.labelNunRight.AutoSize = true;
            this.labelNunRight.Location = new System.Drawing.Point(10, 145);
            this.labelNunRight.Name = "labelNunRight";
            this.labelNunRight.Size = new System.Drawing.Size(83, 13);
            this.labelNunRight.TabIndex = 10;
            this.labelNunRight.Text = "Joystick Right";
            //
            // comboBoxNunLeft
            //
            this.comboBoxNunLeft.FormattingEnabled = true;
            this.comboBoxNunLeft.Location = new System.Drawing.Point(150, 115);
            this.comboBoxNunLeft.Name = "comboBoxNunLeft";
            this.comboBoxNunLeft.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunLeft.TabIndex = 9;
            //
            // labelNunLeft
            //
            this.labelNunLeft.AutoSize = true;
            this.labelNunLeft.Location = new System.Drawing.Point(10, 118);
            this.labelNunLeft.Name = "labelNunLeft";
            this.labelNunLeft.Size = new System.Drawing.Size(76, 13);
            this.labelNunLeft.TabIndex = 8;
            this.labelNunLeft.Text = "Joystick Left";
            //
            // comboBoxNunDown
            //
            this.comboBoxNunDown.FormattingEnabled = true;
            this.comboBoxNunDown.Location = new System.Drawing.Point(150, 88);
            this.comboBoxNunDown.Name = "comboBoxNunDown";
            this.comboBoxNunDown.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunDown.TabIndex = 7;
            //
            // labelNunDown
            //
            this.labelNunDown.AutoSize = true;
            this.labelNunDown.Location = new System.Drawing.Point(10, 91);
            this.labelNunDown.Name = "labelNunDown";
            this.labelNunDown.Size = new System.Drawing.Size(87, 13);
            this.labelNunDown.TabIndex = 6;
            this.labelNunDown.Text = "Joystick Down";
            //
            // comboBoxNunUp
            //
            this.comboBoxNunUp.FormattingEnabled = true;
            this.comboBoxNunUp.Location = new System.Drawing.Point(150, 61);
            this.comboBoxNunUp.Name = "comboBoxNunUp";
            this.comboBoxNunUp.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunUp.TabIndex = 5;
            //
            // labelNunUp
            //
            this.labelNunUp.AutoSize = true;
            this.labelNunUp.Location = new System.Drawing.Point(10, 64);
            this.labelNunUp.Name = "labelNunUp";
            this.labelNunUp.Size = new System.Drawing.Size(73, 13);
            this.labelNunUp.TabIndex = 4;
            this.labelNunUp.Text = "Joystick Up";
            //
            // comboBoxNunZ
            //
            this.comboBoxNunZ.FormattingEnabled = true;
            this.comboBoxNunZ.Location = new System.Drawing.Point(150, 34);
            this.comboBoxNunZ.Name = "comboBoxNunZ";
            this.comboBoxNunZ.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunZ.TabIndex = 3;
            //
            // labelNunZ
            //
            this.labelNunZ.AutoSize = true;
            this.labelNunZ.Location = new System.Drawing.Point(10, 37);
            this.labelNunZ.Name = "labelNunZ";
            this.labelNunZ.Size = new System.Drawing.Size(52, 13);
            this.labelNunZ.TabIndex = 2;
            this.labelNunZ.Text = "Z Button";
            //
            // comboBoxNunC
            //
            this.comboBoxNunC.FormattingEnabled = true;
            this.comboBoxNunC.Location = new System.Drawing.Point(150, 7);
            this.comboBoxNunC.Name = "comboBoxNunC";
            this.comboBoxNunC.Size = new System.Drawing.Size(121, 21);
            this.comboBoxNunC.TabIndex = 1;
            //
            // labelNunC
            //
            this.labelNunC.AutoSize = true;
            this.labelNunC.Location = new System.Drawing.Point(10, 10);
            this.labelNunC.Name = "labelNunC";
            this.labelNunC.Size = new System.Drawing.Size(52, 13);
            this.labelNunC.TabIndex = 0;
            this.labelNunC.Text = "C Button";
            //
            // tabPage3
            //
            this.tabPage3.Controls.Add(this.btnRefreshProfiles);
            this.tabPage3.Controls.Add(this.labelProfileName);
            this.tabPage3.Controls.Add(this.txtProfileName);
            this.tabPage3.Controls.Add(this.btnNewFolder);
            this.tabPage3.Controls.Add(this.labelSubfolder);
            this.tabPage3.Controls.Add(this.comboBoxSubfolders);
            this.tabPage3.Controls.Add(this.labelProfiles);
            this.tabPage3.Controls.Add(this.comboBoxProfiles);
            this.tabPage3.Controls.Add(this.btnLoadProfile);
            this.tabPage3.Controls.Add(this.btnSaveProfile);
            this.tabPage3.Controls.Add(this.btnDeleteProfile);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(379, 281);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Profiles";
            this.tabPage3.UseVisualStyleBackColor = true;
            //
            // labelProfileName
            //
            this.labelProfileName.AutoSize = true;
            this.labelProfileName.Location = new System.Drawing.Point(10, 15);
            this.labelProfileName.Name = "labelProfileName";
            this.labelProfileName.Size = new System.Drawing.Size(77, 13);
            this.labelProfileName.TabIndex = 0;
            this.labelProfileName.Text = "Profile Name:";
            //
            // txtProfileName
            //
            this.txtProfileName.Location = new System.Drawing.Point(95, 12);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(200, 20);
            this.txtProfileName.TabIndex = 1;
            //
            // labelSubfolder
            //
            this.labelSubfolder.AutoSize = true;
            this.labelSubfolder.Location = new System.Drawing.Point(10, 45);
            this.labelSubfolder.Name = "labelSubfolder";
            this.labelSubfolder.Size = new System.Drawing.Size(62, 13);
            this.labelSubfolder.TabIndex = 2;
            this.labelSubfolder.Text = "Subfolder:";
            //
            // comboBoxSubfolders
            //
            this.comboBoxSubfolders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSubfolders.FormattingEnabled = true;
            this.comboBoxSubfolders.Items.AddRange(new object[] { "[Root]" });
            this.comboBoxSubfolders.Location = new System.Drawing.Point(95, 42);
            this.comboBoxSubfolders.Name = "comboBoxSubfolders";
            this.comboBoxSubfolders.Size = new System.Drawing.Size(150, 21);
            this.comboBoxSubfolders.TabIndex = 3;
            this.comboBoxSubfolders.SelectedIndexChanged += new System.EventHandler(this.comboBoxSubfolders_SelectedIndexChanged);
            //
            // btnNewFolder
            //
            this.btnNewFolder.Location = new System.Drawing.Point(251, 40);
            this.btnNewFolder.Name = "btnNewFolder";
            this.btnNewFolder.Size = new System.Drawing.Size(75, 23);
            this.btnNewFolder.TabIndex = 4;
            this.btnNewFolder.Text = "New Folder";
            this.btnNewFolder.UseVisualStyleBackColor = true;
            this.btnNewFolder.Click += new System.EventHandler(this.btnNewFolder_Click);
            //
            // labelProfiles
            //
            this.labelProfiles.AutoSize = true;
            this.labelProfiles.Location = new System.Drawing.Point(10, 75);
            this.labelProfiles.Name = "labelProfiles";
            this.labelProfiles.Size = new System.Drawing.Size(73, 13);
            this.labelProfiles.TabIndex = 5;
            this.labelProfiles.Text = "Load Profile:";
            //
            // comboBoxProfiles
            //
            this.comboBoxProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProfiles.FormattingEnabled = true;
            this.comboBoxProfiles.Location = new System.Drawing.Point(95, 72);
            this.comboBoxProfiles.Name = "comboBoxProfiles";
            this.comboBoxProfiles.Size = new System.Drawing.Size(200, 21);
            this.comboBoxProfiles.TabIndex = 6;
            //
            // btnRefreshProfiles
            //
            this.btnRefreshProfiles.Location = new System.Drawing.Point(301, 70);
            this.btnRefreshProfiles.Name = "btnRefreshProfiles";
            this.btnRefreshProfiles.Size = new System.Drawing.Size(25, 23);
            this.btnRefreshProfiles.TabIndex = 7;
            this.btnRefreshProfiles.Text = "↻";
            this.btnRefreshProfiles.UseVisualStyleBackColor = true;
            this.btnRefreshProfiles.Click += new System.EventHandler(this.btnRefreshProfiles_Click);
            //
            // btnLoadProfile
            //
            this.btnLoadProfile.Location = new System.Drawing.Point(13, 110);
            this.btnLoadProfile.Name = "btnLoadProfile";
            this.btnLoadProfile.Size = new System.Drawing.Size(100, 30);
            this.btnLoadProfile.TabIndex = 8;
            this.btnLoadProfile.Text = "Load Profile";
            this.btnLoadProfile.UseVisualStyleBackColor = true;
            this.btnLoadProfile.Click += new System.EventHandler(this.btnLoadProfile_Click);
            //
            // btnSaveProfile
            //
            this.btnSaveProfile.Location = new System.Drawing.Point(130, 110);
            this.btnSaveProfile.Name = "btnSaveProfile";
            this.btnSaveProfile.Size = new System.Drawing.Size(100, 30);
            this.btnSaveProfile.TabIndex = 9;
            this.btnSaveProfile.Text = "Save Profile";
            this.btnSaveProfile.UseVisualStyleBackColor = true;
            this.btnSaveProfile.Click += new System.EventHandler(this.btnSaveProfile_Click);
            //
            // btnDeleteProfile
            //
            this.btnDeleteProfile.Location = new System.Drawing.Point(247, 110);
            this.btnDeleteProfile.Name = "btnDeleteProfile";
            this.btnDeleteProfile.Size = new System.Drawing.Size(100, 30);
            this.btnDeleteProfile.TabIndex = 10;
            this.btnDeleteProfile.Text = "Delete Profile";
            this.btnDeleteProfile.UseVisualStyleBackColor = true;
            this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            //
            // MappingForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(411, 387);
            this.Controls.Add(this.playerLabel);
            this.Controls.Add(this.playerComboBox);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MappingForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Button Mapping";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Label playerLabel;
        private System.Windows.Forms.ComboBox playerComboBox;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ComboBox comboBoxWiiA;
        private System.Windows.Forms.Label labelWiiA;
        private System.Windows.Forms.ComboBox comboBoxWiiB;
        private System.Windows.Forms.Label labelWiiB;
        private System.Windows.Forms.ComboBox comboBoxWiiDown;
        private System.Windows.Forms.Label labelWiiDown;
        private System.Windows.Forms.ComboBox comboBoxWiiUp;
        private System.Windows.Forms.Label labelWiiUp;
        private System.Windows.Forms.ComboBox comboBoxWiiRight;
        private System.Windows.Forms.Label labelWiiRight;
        private System.Windows.Forms.ComboBox comboBoxWiiLeft;
        private System.Windows.Forms.Label labelWiiLeft;
        private System.Windows.Forms.ComboBox comboBoxWiiMinus;
        private System.Windows.Forms.Label labelWiiMinus;
        private System.Windows.Forms.ComboBox comboBoxWiiPlus;
        private System.Windows.Forms.Label labelWiiPlus;
        private System.Windows.Forms.ComboBox comboBoxWiiTwo;
        private System.Windows.Forms.Label labelWiiTwo;
        private System.Windows.Forms.ComboBox comboBoxWiiOne;
        private System.Windows.Forms.Label labelWiiOne;
        private System.Windows.Forms.ComboBox comboBoxNunZ;
        private System.Windows.Forms.Label labelNunZ;
        private System.Windows.Forms.ComboBox comboBoxNunC;
        private System.Windows.Forms.Label labelNunC;
        private System.Windows.Forms.ComboBox comboBoxNunDown;
        private System.Windows.Forms.Label labelNunDown;
        private System.Windows.Forms.ComboBox comboBoxNunUp;
        private System.Windows.Forms.Label labelNunUp;
        private System.Windows.Forms.ComboBox comboBoxNunRight;
        private System.Windows.Forms.Label labelNunRight;
        private System.Windows.Forms.ComboBox comboBoxNunLeft;
        private System.Windows.Forms.Label labelNunLeft;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label labelProfileName;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Label labelSubfolder;
        private System.Windows.Forms.ComboBox comboBoxSubfolders;
        private System.Windows.Forms.Button btnNewFolder;
        private System.Windows.Forms.Label labelProfiles;
        private System.Windows.Forms.ComboBox comboBoxProfiles;
        private System.Windows.Forms.Button btnRefreshProfiles;
        private System.Windows.Forms.Button btnLoadProfile;
        private System.Windows.Forms.Button btnSaveProfile;
        private System.Windows.Forms.Button btnDeleteProfile;
    }
}
