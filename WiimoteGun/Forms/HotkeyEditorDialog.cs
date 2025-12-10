using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Dialog for editing player hotkeys
    /// (EN/FR: Dialogue pour éditer les hotkeys du joueur)
    /// </summary>
    public class HotkeyEditorDialog : Form
    {
        private int _playerIndex;
        private HotkeyProfile _hotkeyProfile;
        
        // UI Components
        private ListView _listViewHotkeys;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;
        private Button _btnOK;
        private Button _btnCancel;
        private Label _lblTitle;

        // Design colors (match ProfileOverlay)
        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorPanel = Color.FromArgb(37, 37, 37);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);

        public HotkeyEditorDialog(int playerIndex)
        {
            _playerIndex = playerIndex;
            _hotkeyProfile = HotkeyManager.GetProfile(playerIndex);
            
            InitializeUI();
            LoadHotkeys();
        }

        private void InitializeUI()
        {
            // Form settings
            this.Text = $"Hotkeys - Player {_playerIndex}";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;

            // Title label
            _lblTitle = new Label
            {
                Text = $"⚡ Hotkeys Configuration - Player {_playerIndex}",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = ColorAccent,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            this.Controls.Add(_lblTitle);

            // Info label
            Label lblInfo = new Label
            {
                Text = "Configure hotkeys with Home button + another button\n" +
                       "Short press (<500ms) and Long press (≥500ms) can trigger different actions",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(560, 40),
                Location = new Point(20, 55)
            };
            this.Controls.Add(lblInfo);

            // ListView for hotkeys
            _listViewHotkeys = new ListView
            {
                Location = new Point(20, 105),
                Size = new Size(560, 280),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F)
            };
            
            _listViewHotkeys.Columns.Add("Trigger", 150);
            _listViewHotkeys.Columns.Add("Type", 80);
            _listViewHotkeys.Columns.Add("Output Keys", 200);
            _listViewHotkeys.Columns.Add("Description", 130);
            
            _listViewHotkeys.DoubleClick += (s, e) => EditSelectedHotkey();
            
            this.Controls.Add(_listViewHotkeys);

            // Buttons
            int buttonY = 400;
            
            _btnAdd = new Button
            {
                Text = "➕ Add Hotkey",
                Location = new Point(20, buttonY),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(_btnAdd);

            _btnEdit = new Button
            {
                Text = "✏ Edit",
                Location = new Point(150, buttonY),
                Size = new Size(100, 35),
                BackColor = ColorAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            _btnEdit.FlatAppearance.BorderSize = 0;
            _btnEdit.Click += (s, e) => EditSelectedHotkey();
            this.Controls.Add(_btnEdit);

            _btnDelete = new Button
            {
                Text = "🗑 Delete",
                Location = new Point(260, buttonY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(_btnDelete);

            // OK/Cancel buttons
            _btnOK = new Button
            {
                Text = "✓ Save",
                Location = new Point(380, buttonY),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            _btnOK.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_btnOK);

            _btnCancel = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(480, buttonY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }

        private void LoadHotkeys()
        {
            _listViewHotkeys.Items.Clear();

            foreach (var hotkey in _hotkeyProfile.Hotkeys)
            {
                var item = new ListViewItem(new[]
                {
                    $"Home + {hotkey.TriggerButton}",
                    hotkey.PressType == HotkeyPressType.Short ? "Short" : "Long",
                    string.Join(" + ", hotkey.KeyCombination),
                    hotkey.Description
                });
                item.Tag = hotkey;
                _listViewHotkeys.Items.Add(item);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new HotkeyInputDialog(_playerIndex, null))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _hotkeyProfile.AddOrUpdateHotkey(dialog.Hotkey);
                    LoadHotkeys();
                }
            }
        }

        private void EditSelectedHotkey()
        {
            if (_listViewHotkeys.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a hotkey to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedHotkey = _listViewHotkeys.SelectedItems[0].Tag as Hotkey;
            
            // CRITICAL: Clone the hotkey before editing to avoid modifying the live object directly
            // (EN/FR: Cloner la hotkey avant édition pour éviter modification directe)
            var hotkeyToEdit = selectedHotkey.Clone();
            
            using (var dialog = new HotkeyInputDialog(_playerIndex, hotkeyToEdit))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Remove the OLD hotkey (using original values from selectedHotkey)
                    // (EN/FR: Supprimer l'ANCIENNE hotkey)
                    _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.PressType);
                    
                    // Add the NEW/UPDATED hotkey
                    // (EN/FR: Ajouter la NOUVELLE hotkey)
                    _hotkeyProfile.AddOrUpdateHotkey(dialog.Hotkey);
                    
                    LoadHotkeys();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_listViewHotkeys.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a hotkey to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedHotkey = _listViewHotkeys.SelectedItems[0].Tag as Hotkey;
            var result = MessageBox.Show(
                $"Delete hotkey: {selectedHotkey.GetDisplayName()}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.PressType);
                LoadHotkeys();
            }
        }

        /// <summary>
        /// Get the updated hotkey profile
        /// </summary>
        public HotkeyProfile HotkeyProfile => _hotkeyProfile;
    }

    /// <summary>
    /// Dialog for adding/editing a single hotkey
    /// (EN/FR: Dialogue pour ajouter/éditer une hotkey)
    /// </summary>
    public class HotkeyInputDialog : Form
    {
        private int _playerIndex;
        private Hotkey _hotkey;
        private List<Keys> _capturedKeys = new List<Keys>();

        // UI Components
        private Label _lblTrigger;
        private ComboBox _cmbTriggerButton;
        private RadioButton _rbShort;
        private RadioButton _rbLong;
        private Label _lblKeys;
        private TextBox _txtKeys;
        private Button _btnCaptureKeys;
        private Button _btnClearKeys;
        private Label _lblDescription;
        private TextBox _txtDescription;
        private Button _btnOK;
        private Button _btnCancel;

        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorPanel = Color.FromArgb(37, 37, 37);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);
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

            InitializeUI();
            LoadExistingData();
        }

        private void InitializeUI()
        {
            this.Text = _hotkey.TriggerButton == null ? "Add Hotkey" : "Edit Hotkey";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;

            int y = 20;

            // Trigger Button
            _lblTrigger = new Label
            {
                Text = "Trigger Button:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(_lblTrigger);

            _cmbTriggerButton = new ComboBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F)
            };
            _cmbTriggerButton.Items.AddRange(new object[] 
            { 
                "A", "B", "One", "Two", "Minus", 
                "Up", "Down", "Left", "Right" 
            });
            this.Controls.Add(_cmbTriggerButton);

            y += 40;

            // Press Type
            Label lblType = new Label
            {
                Text = "Press Type:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(lblType);

            _rbShort = new RadioButton
            {
                Text = "Short (<500ms)",
                Location = new Point(150, y),
                AutoSize = true,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F),
                Checked = true
            };
            this.Controls.Add(_rbShort);

            _rbLong = new RadioButton
            {
                Text = "Long (≥500ms)",
                Location = new Point(280, y),
                AutoSize = true,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(_rbLong);

            y += 40;

            // Keyboard Output
            _lblKeys = new Label
            {
                Text = "Output Keys:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(_lblKeys);

            _txtKeys = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(250, 25),
                ReadOnly = true,
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(_txtKeys);

            y += 35;

            _btnCaptureKeys = new Button
            {
                Text = "🎹 Capture Keys",
                Location = new Point(150, y),
                Size = new Size(150, 30),
                BackColor = ColorAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            _btnCaptureKeys.FlatAppearance.BorderSize = 0;
            _btnCaptureKeys.Click += BtnCaptureKeys_Click;
            this.Controls.Add(_btnCaptureKeys);

            _btnClearKeys = new Button
            {
                Text = "Clear",
                Location = new Point(310, y),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            _btnClearKeys.FlatAppearance.BorderSize = 0;
            _btnClearKeys.Click += (s, e) =>
            {
                _capturedKeys.Clear();
                UpdateKeysDisplay();
            };
            this.Controls.Add(_btnClearKeys);

            y += 50;

            // Description
            _lblDescription = new Label
            {
                Text = "Description:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(_lblDescription);

            _txtDescription = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(250, 25),
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 10F),
                MaxLength = 50
            };
            _txtDescription.Click += (s, e) => ShowVirtualKeyboard(_txtDescription);
            this.Controls.Add(_txtDescription);

            y += 60;

            // OK/Cancel
            _btnOK = new Button
            {
                Text = "✓ OK",
                Location = new Point(190, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnOK.FlatAppearance.BorderSize = 0;
            _btnOK.Click += BtnOK_Click;
            this.Controls.Add(_btnOK);

            _btnCancel = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(300, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
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

    /// <summary>
    /// Dialog for capturing keyboard keys
    /// (EN/FR: Dialogue pour capturer les touches clavier)
    /// </summary>
    public class KeyCaptureDialog : Form
    {
        private List<Keys> _capturedKeys = new List<Keys>();
        private Label _lblInfo;
        private TextBox _txtDisplay;
        private Button _btnDone;
        private Button _btnCancel;

        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);

        public List<Keys> CapturedKeys => _capturedKeys;

        public KeyCaptureDialog()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Capture Keys";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;
            this.KeyPreview = true;

            _lblInfo = new Label
            {
                Text = "Press the keys for your hotkey combination\n(e.g., hold Ctrl+Alt then press F4)",
                Location = new Point(20, 20),
                AutoSize = false,
                Size = new Size(360, 40),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray
            };
            this.Controls.Add(_lblInfo);

            _txtDisplay = new TextBox
            {
                Location = new Point(20, 70),
                Size = new Size(360, 30),
                ReadOnly = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                Text = "(Waiting for keys...)"
            };
            this.Controls.Add(_txtDisplay);

            _btnDone = new Button
            {
                Text = "✓ Done",
                Location = new Point(180, 120),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(_btnDone);

            _btnCancel = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(280, 120),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_btnCancel);

            this.KeyDown += KeyCaptureDialog_KeyDown;
            this.KeyUp += KeyCaptureDialog_KeyUp;
        }

        private void KeyCaptureDialog_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            // Only block Enter - used to confirm dialog (EN/FR: Ne bloquer que Enter - utilisé pour confirmer dialogue)
            // ESC can now be captured as a hotkey since we have a Cancel button
            if (e.KeyCode == Keys.Enter)
                return;

            // CRITICAL FIX: Use KeyData to get the actual key pressed (EN/FR: Utiliser KeyData pour vraie touche)
            // KeyData includes modifiers, KeyCode only gives base key
            // This properly handles AZERTY, QWERTZ, and other layouts
            Keys actualKey = e.KeyCode;
            
            // Don't add modifier keys by themselves, they'll be in other keys
            // (EN/FR: Ne pas ajouter modificateurs seuls, ils seront dans autres touches)
            bool isModifierOnly = (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Control || 
                                   e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Shift ||
                                   e.KeyCode == Keys.Menu || e.KeyCode == Keys.Alt ||
                                   e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin);
            
            if (isModifierOnly)
            {
                // Just show it in display but don't add yet
                // (EN/FR: Juste afficher mais ne pas ajouter)
                UpdateDisplay();
                return;
            }
            
            // Add modifiers first if pressed (EN/FR: Ajouter modificateurs d'abord si pressés)
            if (e.Control && !_capturedKeys.Contains(Keys.Control))
            {
                _capturedKeys.Add(Keys.Control);
            }
            if (e.Alt && !_capturedKeys.Contains(Keys.Alt))
            {
                _capturedKeys.Add(Keys.Alt);
            }
            if (e.Shift && !_capturedKeys.Contains(Keys.Shift))
            {
                _capturedKeys.Add(Keys.Shift);
            }
            
            // Add the main key (EN/FR: Ajouter touche principale)
            if (!_capturedKeys.Contains(actualKey))
            {
                _capturedKeys.Add(actualKey);
                UpdateDisplay();
            }
        }

        private void KeyCaptureDialog_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void UpdateDisplay()
        {
            if (_capturedKeys.Count == 0)
            {
                _txtDisplay.Text = "(Waiting for keys...)";
            }
            else
            {
                _txtDisplay.Text = string.Join(" + ", _capturedKeys);
            }
        }
    }
}
