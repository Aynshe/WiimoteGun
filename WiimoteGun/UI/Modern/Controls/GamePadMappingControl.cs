using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun.Controls
{
    public partial class GamePadMappingControl : UserControl
    {
        private int _currentPlayer = 1;
        
        // Dictionary to map ComboBox index/item to GamePadButton
        private List<GamePadButtonItem> _gamePadButtons;
        private List<GamePadAxisItem> _gamePadAxes;

        public event EventHandler BackRequested;

        public GamePadMappingControl()
        {
            InitializeComponent();
            InitializeDataSources();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeDataSources(); // Ensure data is loaded when control is shown
        }

        private void InitializeDataSources()
        {
            // Initialize GamePad Buttons list for ComboBoxes
            _gamePadButtons = new List<GamePadButtonItem>
            {
                new GamePadButtonItem("None", GamePadButton.None),
                new GamePadButtonItem("Button A (1)", GamePadButton.Button1),
                new GamePadButtonItem("Button B (2)", GamePadButton.Button2),
                new GamePadButtonItem("Button X (3)", GamePadButton.Button3),
                new GamePadButtonItem("Button Y (4)", GamePadButton.Button4),
                new GamePadButtonItem("Left Bumper (LB)", GamePadButton.Button5),
                new GamePadButtonItem("Right Bumper (RB)", GamePadButton.Button6),
                new GamePadButtonItem("Left Trigger (Button)", GamePadButton.Button7),
                new GamePadButtonItem("Right Trigger (Button)", GamePadButton.Button8),
                new GamePadButtonItem("Back / Select", GamePadButton.Button9),
                new GamePadButtonItem("Start", GamePadButton.Button10),
                new GamePadButtonItem("Left Stick Click", GamePadButton.Button11),
                new GamePadButtonItem("Right Stick Click", GamePadButton.Button12),
                new GamePadButtonItem("D-Pad Up", GamePadButton.DPadUp),
                new GamePadButtonItem("D-Pad Down", GamePadButton.DPadDown),
                new GamePadButtonItem("D-Pad Left", GamePadButton.DPadLeft),
                new GamePadButtonItem("D-Pad Right", GamePadButton.DPadRight)
            };

            // Initialize Axes list
            _gamePadAxes = new List<GamePadAxisItem>
            {
                new GamePadAxisItem("None", GamePadAxis.None),
                new GamePadAxisItem("Left Stick (X/Y)", GamePadAxis.LeftStick),
                new GamePadAxisItem("Right Stick (Rx/Ry)", GamePadAxis.RightStick),
                new GamePadAxisItem("Digital D-Pad (Up/Down/Left/Right)", GamePadAxis.Dpad)
            };

            // Populate Axe ComboBoxes
            cboIRAxis.DisplayMember = "Name";
            cboIRAxis.ValueMember = "Value";
            cboIRAxis.DataSource = new List<GamePadAxisItem>(_gamePadAxes);

            cboNunchukAxis.DisplayMember = "Name";
            cboNunchukAxis.ValueMember = "Value";
            cboNunchukAxis.DataSource = new List<GamePadAxisItem>(_gamePadAxes);

            // Back button handler
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            // Initial Load
            LoadCurrentMappings();
        }

        public void LoadData()
        {
            LoadCurrentMappings();
        }

        private void TabControlPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlPlayers == null) return;
            if (_gamePadButtons == null) return;
            _currentPlayer = tabControlPlayers.SelectedIndex + 1;
            LoadCurrentMappings();
        }

        private void LoadCurrentMappings()
        {
            GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
            if (mappings == null) return;

            // Load Axes
            SetAxisSelection(cboIRAxis, mappings.IRSensorAxis);
            SetAxisSelection(cboNunchukAxis, mappings.NunchukJoystickAxis);

            // Load IR Calibration (EN/FR: Charger calibrage IR)
            numLinearity.Value = (decimal)Math.Max(0.5, Math.Min(4.0, mappings.IRLinearity));
            numOverscan.Value = (decimal)Math.Max(0.0, Math.Min(0.45, mappings.IROverscan));

            // Load Buttons (Re-create controls to ensure fresh state)
            flowLayoutPanelButtons.Controls.Clear();
            flowLayoutPanelButtons.SuspendLayout();

            AddSectionHeader("Wiimote Buttons");
            AddMappingRow("A Button", mappings.WiiA, (val) => mappings.WiiA = val);
            AddMappingRow("B Button", mappings.WiiB, (val) => mappings.WiiB = val);
            AddMappingRow("1 Button", mappings.Wii1, (val) => mappings.Wii1 = val);
            AddMappingRow("2 Button", mappings.Wii2, (val) => mappings.Wii2 = val);
            AddMappingRow("Plus (+)", mappings.WiiPlus, (val) => mappings.WiiPlus = val);
            AddMappingRow("Minus (-)", mappings.WiiMinus, (val) => mappings.WiiMinus = val);
            AddMappingRow("D-Pad Up", mappings.WiiUp, (val) => mappings.WiiUp = val);
            AddMappingRow("D-Pad Down", mappings.WiiDown, (val) => mappings.WiiDown = val);
            AddMappingRow("D-Pad Left", mappings.WiiLeft, (val) => mappings.WiiLeft = val);
            AddMappingRow("D-Pad Right", mappings.WiiRight, (val) => mappings.WiiRight = val);
            AddMappingRow("Home Button", mappings.WiiHome, (val) => mappings.WiiHome = val);

            AddSectionHeader("Nunchuk Buttons");
            AddMappingRow("C Button", mappings.NunchukC, (val) => mappings.NunchukC = val);
            AddMappingRow("Z Button", mappings.NunchukZ, (val) => mappings.NunchukZ = val);

            flowLayoutPanelButtons.ResumeLayout();
        }

        private void SetAxisSelection(ComboBox cbo, GamePadAxis axis)
        {
            foreach (GamePadAxisItem item in cbo.Items)
            {
                if (item.Value == axis)
                {
                    cbo.SelectedItem = item;
                    return;
                }
            }
            if (cbo.Items.Count > 0)
                cbo.SelectedIndex = 0;
        }

        private void AddSectionHeader(string title)
        {
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Underline);
            lbl.ForeColor = Color.FromArgb(0, 122, 204); // Accent color
            lbl.AutoSize = true;
            lbl.Margin = new Padding(10, 20, 10, 5);
            flowLayoutPanelButtons.Controls.Add(lbl);
            flowLayoutPanelButtons.SetFlowBreak(lbl, true); // Force new line after
        }

        private void AddMappingRow(string labelText, GamePadButton currentValue, Action<GamePadButton> setter)
        {
            Panel row = new Panel();
            row.Size = new Size(500, 35);
            row.Margin = new Padding(5);
            
            Label lbl = new Label();
            lbl.Text = labelText + ":";
            lbl.Font = new Font("Segoe UI", 9.5f);
            lbl.ForeColor = Color.White;
            lbl.AutoSize = false;
            lbl.Size = new Size(150, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(10, 5);
            
            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Color.FromArgb(50, 50, 50);
            cbo.ForeColor = Color.White;
            cbo.Size = new Size(200, 25);
            cbo.Location = new Point(170, 5);
            cbo.DisplayMember = "Name";
            cbo.ValueMember = "Value";
            
            // Populate
            cbo.DataSource = new List<GamePadButtonItem>(_gamePadButtons);
            
            // Set selection is done after adding to parent to ensure BindingContext
            // SetButtonSelection(cbo, currentValue);

            // Event handler to update mapping immediately (in memory object)
            // But we only save on "Save Changes"
            cbo.SelectedIndexChanged += (s, e) => 
            {
                if (cbo.SelectedItem is GamePadButtonItem item)
                {
                    setter(item.Value);
                }
            };

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
            
            flowLayoutPanelButtons.Controls.Add(row);

            // Set selection now that control is in the tree (has BindingContext)
            SetButtonSelection(cbo, currentValue);
        }

        private void SetButtonSelection(ComboBox cbo, GamePadButton button)
        {
            foreach (GamePadButtonItem item in cbo.Items)
            {
                if (item.Value == button)
                {
                    cbo.SelectedItem = item;
                    return;
                }
            }
            if (cbo.Items.Count > 0)
                cbo.SelectedIndex = 0;
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Axe settings update
            GamePadMappings mappings = Options.Instance.GetGamePadMappingsForPlayer(_currentPlayer);
            if (mappings != null)
            {
                if (cboIRAxis.SelectedItem is GamePadAxisItem irItem)
                    mappings.IRSensorAxis = irItem.Value;
                
                if (cboNunchukAxis.SelectedItem is GamePadAxisItem nunItem)
                    mappings.NunchukJoystickAxis = nunItem.Value;

                // IR Calibration update (EN/FR: Mise à jour calibrage IR)
                mappings.IRLinearity = (float)numLinearity.Value;
                mappings.IROverscan = (float)numOverscan.Value;
            }

            // Buttons are updated in real-time via setter delegates in AddMappingRow
            // So we just need to save options
            Options.Instance.Save();
            
            MessageBox.Show($"GamePad Mappings for Player {_currentPlayer} saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // If we are currently running, maybe we need to reload mappings in the controller?
            // The controller reads Options.Instance directly usually, or we might need to trigger something.
            // For now assume direct read.
        }

        // Helper classes for ComboBox items
        private class GamePadButtonItem
        {
            public string Name { get; set; }
            public GamePadButton Value { get; set; }
            public GamePadButtonItem(string name, GamePadButton value) { Name = name; Value = value; }
        }

        private class GamePadAxisItem
        {
            public string Name { get; set; }
            public GamePadAxis Value { get; set; }
            public GamePadAxisItem(string name, GamePadAxis value) { Name = name; Value = value; }
        }
    }
}
