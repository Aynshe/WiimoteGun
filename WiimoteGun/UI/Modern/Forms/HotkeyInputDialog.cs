using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Dialog for adding/editing a single hotkey (Dual Action)
    /// (EN/FR: Dialogue pour ajouter/éditer une hotkey double action)
    /// </summary>
    public partial class HotkeyInputDialog : Form
    {
        private int _playerIndex;
        private Hotkey _hotkey;
        private List<Keys> _capturedShortKeys = new List<Keys>();
        private List<Keys> _capturedLongKeys = new List<Keys>();

        // All available modifier buttons (EN/FR: Tous les boutons modifier disponibles)
        private static readonly string[] AllModifierButtons = new[] { "A", "B", "One", "Two", "Plus", "Minus", "Home", "Up", "Down", "Left", "Right", "NunC", "NunZ", "NunUp", "NunDown", "NunLeft", "NunRight" };

        // Trigger buttons exclude Home — reserved as base modifier
        // (EN/FR: Les triggers excluent Home)
        private static readonly string[] AllTriggerButtons = new[] { "A", "B", "One", "Two", "Plus", "Minus", "Up", "Down", "Left", "Right", "NunC", "NunZ", "NunUp", "NunDown", "NunLeft", "NunRight" };

        public Hotkey Hotkey { get { return _hotkey; } }

        public HotkeyInputDialog(int playerIndex, Hotkey existingHotkey)
        {
            _playerIndex = playerIndex;
            _hotkey = existingHotkey ?? new Hotkey();
            
            if (existingHotkey != null)
            {
                if (existingHotkey.ShortPressKeys != null)
                    _capturedShortKeys = new List<Keys>(existingHotkey.ShortPressKeys);
                
                if (existingHotkey.LongPressKeys != null)
                    _capturedLongKeys = new List<Keys>(existingHotkey.LongPressKeys);
            }

            InitializeComponent();
            InitializeCustomControls();
            InitializeCustomSettings();
            LoadExistingData();
        }

        private void InitializeCustomControls()
        {
            // Note: Designer handles layout of Modifier, Trigger and Groups.
            // We only need to dynamically populate the Modifier and Trigger lists.
            
            // Populate modifier list
            _cmbModifier.Items.Clear();
            foreach (string btn in AllModifierButtons)
            {
                // DolphinBar: Home is a hardware button
                if (string.Equals(btn, "Home", StringComparison.OrdinalIgnoreCase) && Options.Instance.DetectDolphinbar)
                    continue;

                _cmbModifier.Items.Add(btn);
            }

            // Configure Shared checkbox visibility (P1 only)
            // (EN/FR: Configurer la visibilité de la case de partage (P1 uniquement))
            if (_cbSharedHotkey != null)
            {
                _cbSharedHotkey.Visible = (_playerIndex == 1);
                _cbSharedHotkey.Checked = _hotkey.IsShared;
            }
        }

        private void CmbModifier_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTriggerList();
        }

        private static readonly HashSet<string> DpadButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Up", "Down", "Left", "Right" };

        private void UpdateTriggerList()
        {
            string currentTrigger = _cmbTriggerButton.SelectedItem as string;
            string selectedModifier = _cmbModifier.SelectedItem as string;

            _cmbTriggerButton.Items.Clear();

            foreach (string btn in AllTriggerButtons)
            {
                // Exclude selected modifier
                if (string.Equals(btn, selectedModifier, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Exclude D-pad if Modifier is Home/Minus (Offset adjustment)
                if (DpadButtons.Contains(btn) &&
                    (string.Equals(selectedModifier, "Home", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(selectedModifier, "Minus", StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Home + Plus = Overlay command
                if (string.Equals(btn, "Plus", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(selectedModifier, "Home", StringComparison.OrdinalIgnoreCase))
                    continue;

                _cmbTriggerButton.Items.Add(btn);
            }

            if (currentTrigger != null && _cmbTriggerButton.Items.Contains(currentTrigger))
            {
                _cmbTriggerButton.SelectedItem = currentTrigger;
            }
        }

        private void InitializeCustomSettings()
        {
            this.Text = string.IsNullOrEmpty(_hotkey.TriggerButton) ? "Add Hotkey" : "Edit Hotkey";
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

            _txtDescription.Text = _hotkey.Description ?? "";
            UpdateShortKeysDisplay();
            UpdateLongKeysDisplay();

            if (_cbSharedHotkey != null)
            {
                _cbSharedHotkey.Checked = _hotkey.IsShared;
            }
        }

        #region Short Press Handlers
        private void BtnCaptureShort_Click(object sender, EventArgs e)
        {
            using (var captureDialog = new KeyCaptureDialog())
            {
                if (captureDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _capturedShortKeys = captureDialog.CapturedKeys;
                    UpdateShortKeysDisplay();
                }
            }
        }

        private void BtnClearShort_Click(object sender, EventArgs e)
        {
            _capturedShortKeys.Clear();
            UpdateShortKeysDisplay();
        }

        private void UpdateShortKeysDisplay()
        {
            if (_capturedShortKeys.Count == 0)
                _txtShortKeys.Text = "(None)";
            else
                _txtShortKeys.Text = string.Join(" + ", _capturedShortKeys);
        }
        #endregion

        #region Long Press Handlers
        private void BtnCaptureLong_Click(object sender, EventArgs e)
        {
            using (var captureDialog = new KeyCaptureDialog())
            {
                if (captureDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _capturedLongKeys = captureDialog.CapturedKeys;
                    UpdateLongKeysDisplay();
                }
            }
        }

        private void BtnClearLong_Click(object sender, EventArgs e)
        {
            _capturedLongKeys.Clear();
            UpdateLongKeysDisplay();
        }

        private void UpdateLongKeysDisplay()
        {
            if (_capturedLongKeys.Count == 0)
                _txtLongKeys.Text = "(None)";
            else
                _txtLongKeys.Text = string.Join(" + ", _capturedLongKeys);
        }
        #endregion

        private void ShowVirtualKeyboard(TextBox targetTextBox)
        {
            if (targetTextBox == null) return;

            VirtualKeyboard keyboard = new VirtualKeyboard(targetTextBox);
            keyboard.StartPosition = FormStartPosition.Manual;
            keyboard.Location = new Point(
                this.Location.X + (this.Width - keyboard.Width) / 2,
                this.Location.Y + (this.Height - keyboard.Height) / 2
            );
            keyboard.ShowDialog(this);
        }

        private void _txtDescription_Click(object sender, EventArgs e)
        {
            ShowVirtualKeyboard(_txtDescription);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate
            if (_cmbModifier.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a modifier button.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);               return;
            }

            if (_cmbTriggerButton.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a trigger button.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);               return;
            }

            if (_cmbModifier.SelectedItem.ToString() == _cmbTriggerButton.SelectedItem.ToString())
            {
                MessageBox.Show(this, "Modifier and Trigger cannot be the same button.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);               return;
            }

            // At least one action must be defined
            if (_capturedShortKeys.Count == 0 && _capturedLongKeys.Count == 0)
            {
                MessageBox.Show(this, "Please define at least one action (Short or Long press).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);               return;
            }

            // Create/update hotkey
            _hotkey.ModifierButton = _cmbModifier.SelectedItem.ToString();
            _hotkey.TriggerButton = _cmbTriggerButton.SelectedItem.ToString();
            
            // Assign keys (clone lists)
            _hotkey.ShortPressKeys = new List<Keys>(_capturedShortKeys);
            _hotkey.LongPressKeys = new List<Keys>(_capturedLongKeys);
            
            _hotkey.Description = _txtDescription.Text.Trim();
            _hotkey.IsShared = _cbSharedHotkey != null ? _cbSharedHotkey.Checked : false;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
