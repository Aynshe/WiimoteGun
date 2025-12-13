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
    /// <summary>
    /// Dialog for editing player hotkeys
    /// (EN/FR: Dialogue pour éditer les hotkeys du joueur)
    /// </summary>
    public partial class HotkeyEditorDialog : Form
    {
        private int _playerIndex;
        private HotkeyProfile _hotkeyProfile;
        
        // Colors are now handled in Designer, or can be kept here if dynamic
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);

        public HotkeyEditorDialog(int playerIndex)
        {
            _playerIndex = playerIndex;
            _hotkeyProfile = HotkeyManager.GetProfile(playerIndex);
            
            InitializeComponent();
            
            // Dynamic Title setting
            this.Text = $"Hotkeys - Player {_playerIndex}";
            _lblTitle.Text = $"⚡ Hotkeys Configuration - Player {_playerIndex}";
            
            LoadHotkeys();
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


}
