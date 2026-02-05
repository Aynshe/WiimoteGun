using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Dialog for editing player hotkeys
    /// (EN/FR: Dialogue pour éditer les hotkeys du joueur)
    /// </summary>
    /// <summary>
    /// Dialog for editing player hotkeys
    /// (EN/FR: Dialogue pour éditer les hotkeys du joueur)
    /// </summary>
    public partial class HotkeyEditorDialog : Form
    {
        private int _playerIndex;
        private HotkeyProfile _hotkeyProfile;
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private CheckBox _cbShared;

        public HotkeyEditorDialog(int playerIndex)
        {
            _playerIndex = playerIndex;
            // Get initial profile based on current options
            _hotkeyProfile = HotkeyManager.GetProfile(playerIndex);
            
            InitializeComponent();
            InitializeCustomControls();
            
            // Dynamic Title setting
            UpdateTitle();
            
            LoadHotkeys();
        }

        private void UpdateTitle()
        {
            if (Options.Instance.UseSharedHotkeys && _playerIndex != 1)
            {
                 this.Text = string.Format("Hotkeys - Player {0} (Using Shared P1)", _playerIndex);
                 _lblTitle.Text = string.Format("⚡ Hotkeys - Player {0} (Shared P1)", _playerIndex);
            }
            else
            {
                this.Text = string.Format("Hotkeys - Player {0}", _playerIndex);
                _lblTitle.Text = string.Format("⚡ Hotkeys Configuration - Player {0}", _playerIndex);
            }
        }

        private void InitializeCustomControls()
        {
            // Create "Use Shared Hotkeys" checkbox
            _cbShared = new CheckBox();
            _cbShared.AutoSize = true;
            _cbShared.Font = new System.Drawing.Font("Segoe UI", 10F);
            _cbShared.ForeColor = System.Drawing.Color.White;
            _cbShared.Location = new Point(20, this.Height - 70); // Bottom left (approx)
            _cbShared.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _cbShared.Text = _playerIndex == 1 ? "Apply to All Players (Shared)" : "Use Shared Configuration (From P1)";
            _cbShared.Checked = Options.Instance.UseSharedHotkeys;
            _cbShared.CheckedChanged += _cbShared_CheckedChanged;
            
            this.Controls.Add(_cbShared);
            // Ensure it's on top
            _cbShared.BringToFront();
        }

        private void _cbShared_CheckedChanged(object sender, EventArgs e)
        {
            // Calculate logic
            bool isShared = _cbShared.Checked;
            
            // Update global option
            if (Options.Instance.UseSharedHotkeys != isShared)
            {
                Options.Instance.UseSharedHotkeys = isShared;
                Options.Instance.Save();
                
                // Refresh profile reference
                // If Shared=True: GetProfile(P2) returns P1.
                // If Shared=False: GetProfile(P2) returns P2.
                _hotkeyProfile = HotkeyManager.GetProfile(_playerIndex);
                
                UpdateTitle();
                LoadHotkeys();
                
                // If we are P2 and switched to Shared -> We see P1 keys.
                // If we are P2 and switched to Individual -> We see P2 keys (Empty or Custom).
            }
        }

        private void LoadHotkeys()
        {
            _listViewHotkeys.Items.Clear();

            foreach (var hotkey in _hotkeyProfile.Hotkeys)
            {
                var item = new ListViewItem(new[]
                {
                    string.Format("{0} + {1}", hotkey.ModifierButton, hotkey.TriggerButton),
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
                    _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.PressType, selectedHotkey.ModifierButton);
                    
                    // Add the NEW/UPDATED hotkey
                    // (EN/FR: Ajouter la NOUVELLE hotkey)
                    _hotkeyProfile.AddOrUpdateHotkey(dialog.Hotkey);
                    
                    LoadHotkeys();
                }
            }
        }

        // Handler linked from Designer
        private void _btnEdit_Click(object sender, EventArgs e)
        {
            EditSelectedHotkey();
        }

        // Handler linked from Designer
        private void _listViewHotkeys_DoubleClick(object sender, EventArgs e)
        {
            EditSelectedHotkey();
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
                string.Format("Delete hotkey: {0}?", selectedHotkey.GetDisplayName()),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.PressType, selectedHotkey.ModifierButton);
                LoadHotkeys();
            }
        }

        /// <summary>
        /// Get the updated hotkey profile
        /// </summary>
        public HotkeyProfile HotkeyProfile { get { return _hotkeyProfile; } }
    }


}
