using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Dialog for adding/editing a single hotkey
    /// (EN/FR: Dialogue pour ajouter/éditer une hotkey)
    /// </summary>
    /// <summary>
    /// Dialog for adding/editing a single hotkey
    /// (EN/FR: Dialogue pour ajouter/éditer une hotkey)
    /// </summary>
    public partial class HotkeyInputDialog : Form
    {
        private int _playerIndex;
        private Hotkey _hotkey;
        private List<Keys> _capturedKeys = new List<Keys>();

        // Colors are now handled in Designer, or can be kept here if dynamic
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);

        public Hotkey Hotkey { get { return _hotkey; } }

        private System.Windows.Forms.Label _lblModifier;
        private System.Windows.Forms.ComboBox _cmbModifier;

        public HotkeyInputDialog(int playerIndex, Hotkey existingHotkey)
        {
            _playerIndex = playerIndex;
            _hotkey = existingHotkey ?? new Hotkey();
            
            if (existingHotkey != null)
            {
                _capturedKeys = new List<Keys>(existingHotkey.KeyCombination);
            }

            InitializeComponent();
            InitializeCustomControls();
            InitializeCustomSettings();
            LoadExistingData();
        }

        private void InitializeCustomControls()
        {
            // Increase form height to accommodate new field
            this.Height = 360;

            // Shift existing controls down by 40px
            _lblTrigger.Top += 40;
            _cmbTriggerButton.Top += 40;
            lblType.Top += 40;
            _rbShort.Top += 40;
            _rbLong.Top += 40;
            _lblKeys.Top += 40;
            _txtKeys.Top += 40;
            _btnCaptureKeys.Top += 40;
            _btnClearKeys.Top += 40;
            _lblDescription.Top += 40;
            _txtDescription.Top += 40;
            _btnOK.Top += 40;
            _btnCancel.Top += 40;

            // Create Modifier Label
            _lblModifier = new Label();
            _lblModifier.AutoSize = true;
            _lblModifier.Font = new System.Drawing.Font("Segoe UI", 10F);
            _lblModifier.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            _lblModifier.Location = new Point(20, 20);
            _lblModifier.Text = "Modifier Button:";
            this.Controls.Add(_lblModifier);

            // Create Modifier ComboBox
            _cmbModifier = new ComboBox();
            _cmbModifier.BackColor = System.Drawing.Color.FromArgb(37, 37, 37);
            _cmbModifier.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            _cmbModifier.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbModifier.Font = new System.Drawing.Font("Segoe UI", 10F);
            _cmbModifier.Location = new Point(150, 17);
            _cmbModifier.Size = new Size(250, 25);
            // Populate from HotkeyManager.AllowedModifiers or manual list
            // (EN/FR: Remplir depuis HotkeyManager ou liste manuelle)
            string[] modifiers = new[] { "Home", "Minus", "Plus", "One", "Two", "A", "B", "Up", "Down", "Left", "Right" };
            _cmbModifier.Items.AddRange(modifiers);
            this.Controls.Add(_cmbModifier);
        }

        private void InitializeCustomSettings()
        {
            this.Text = _hotkey.TriggerButton == null ? "Add Hotkey" : "Edit Hotkey";
        }

        private void LoadExistingData()
        {
            // Set modifier
            if (!string.IsNullOrEmpty(_hotkey.ModifierButton))
            {
                if (_cmbModifier.Items.Contains(_hotkey.ModifierButton))
                    _cmbModifier.SelectedItem = _hotkey.ModifierButton;
                else
                    _cmbModifier.SelectedItem = "Home"; // Default
            }
            else
            {
                _cmbModifier.SelectedItem = "Home";
            }

            if (_hotkey.TriggerButton != null)
            {
                _cmbTriggerButton.SelectedItem = _hotkey.TriggerButton;
            }

            if (_hotkey.PressType == HotkeyPressType.Long)
            {
                _rbLong.Checked = true;
            }

            _txtDescription.Text = _hotkey.Description ?? "";
            UpdateKeysDisplay();
        }

        private void BtnCaptureKeys_Click(object sender, EventArgs e)
        {
            using (var captureDialog = new KeyCaptureDialog())
            {
                if (captureDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _capturedKeys = captureDialog.CapturedKeys;
                    UpdateKeysDisplay();
                }
            }
        }

        private void UpdateKeysDisplay()
        {
            if (_capturedKeys.Count == 0)
            {
                _txtKeys.Text = "(No keys captured)";
            }
            else
            {
                _txtKeys.Text = string.Join(" + ", _capturedKeys);
            }
        }

        private void ShowVirtualKeyboard(TextBox targetTextBox)
        {
            if (targetTextBox == null) return;

            // Create and show virtual keyboard (EN/FR: Créer et afficher clavier virtuel)
            VirtualKeyboard keyboard = new VirtualKeyboard(targetTextBox);
            
            // Center on parent form (EN/FR: Centrer sur formulaire parent)
            keyboard.StartPosition = FormStartPosition.Manual;
            keyboard.Location = new Point(
                this.Location.X + (this.Width - keyboard.Width) / 2,
                this.Location.Y + (this.Height - keyboard.Height) / 2
            );
            
            keyboard.ShowDialog(this);
        }

        // Handler linked from Designer
        private void _txtDescription_Click(object sender, EventArgs e)
        {
            ShowVirtualKeyboard(_txtDescription);
        }

        // Handler linked from Designer
        private void _btnClearKeys_Click(object sender, EventArgs e)
        {
            _capturedKeys.Clear();
            UpdateKeysDisplay();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate
            if (_cmbModifier.SelectedItem == null)
            {
                MessageBox.Show("Please select a modifier button.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbTriggerButton.SelectedItem == null)
            {
                MessageBox.Show("Please select a trigger button.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prevent Modifier == Trigger
            if (_cmbModifier.SelectedItem.ToString() == _cmbTriggerButton.SelectedItem.ToString())
            {
                MessageBox.Show("Modifier and Trigger cannot be the same button.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_capturedKeys.Count == 0)
            {
                MessageBox.Show("Please capture at least one key.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create/update hotkey
            _hotkey.ModifierButton = _cmbModifier.SelectedItem.ToString();
            _hotkey.TriggerButton = _cmbTriggerButton.SelectedItem.ToString();
            _hotkey.PressType = _rbShort.Checked ? HotkeyPressType.Short : HotkeyPressType.Long;
            _hotkey.KeyCombination = new List<Keys>(_capturedKeys);
            _hotkey.Description = _txtDescription.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
