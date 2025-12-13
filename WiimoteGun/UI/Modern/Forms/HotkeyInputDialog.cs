using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

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

        public Hotkey Hotkey => _hotkey;

        public HotkeyInputDialog(int playerIndex, Hotkey existingHotkey)
        {
            _playerIndex = playerIndex;
            _hotkey = existingHotkey ?? new Hotkey();
            
            if (existingHotkey != null)
            {
                _capturedKeys = new List<Keys>(existingHotkey.KeyCombination);
            }

            InitializeComponent();
            InitializeCustomSettings();
            LoadExistingData();
        }

        private void InitializeCustomSettings()
        {
            this.Text = _hotkey.TriggerButton == null ? "Add Hotkey" : "Edit Hotkey";
        }

        private void LoadExistingData()
        {
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
            if (_cmbTriggerButton.SelectedItem == null)
            {
                MessageBox.Show("Please select a trigger button.", "Validation Error",
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
            _hotkey.TriggerButton = _cmbTriggerButton.SelectedItem.ToString();
            _hotkey.PressType = _rbShort.Checked ? HotkeyPressType.Short : HotkeyPressType.Long;
            _hotkey.KeyCombination = new List<Keys>(_capturedKeys);
            _hotkey.Description = _txtDescription.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
