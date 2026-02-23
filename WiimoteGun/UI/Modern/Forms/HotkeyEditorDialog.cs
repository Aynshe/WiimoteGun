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
    public partial class HotkeyEditorDialog : Form
    {
        private int _playerIndex;
        private HotkeyProfile _hotkeyProfile;
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);

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
            this.Text = string.Format("Hotkeys - Player {0}", _playerIndex);
            _lblTitle.Text = string.Format("⚡ Hotkeys Configuration - Player {0}", _playerIndex);
        }

        private void InitializeCustomControls()
        {
        }


        private void LoadHotkeys()
        {
            _listViewHotkeys.Items.Clear();

            // 1. Get player's specific hotkeys
            var hotkeysToShow = new List<Hotkey>(_hotkeyProfile.Hotkeys);
            
            // 2. If not Player 1, also include shared hotkeys from Player 1
            // (EN/FR: Si pas Joueur 1, inclure aussi les hotkeys partagées de P1)
            if (_playerIndex != 1)
            {
                var p1Profile = HotkeyManager.GetProfile(1);
                foreach (var p1Hotkey in p1Profile.Hotkeys)
                {
                    if (p1Hotkey.IsShared)
                    {
                        // Check for override (same trigger/modifier)
                        bool overridden = _hotkeyProfile.HasHotkey(p1Hotkey.TriggerButton, p1Hotkey.ModifierButton);
                        if (!overridden)
                        {
                            hotkeysToShow.Add(p1Hotkey);
                        }
                    }
                }
            }

            foreach (var hotkey in hotkeysToShow)
            {
                string desc = hotkey.Description ?? "";
                bool isFallback = _playerIndex != 1 && !_hotkeyProfile.HasHotkey(hotkey.TriggerButton, hotkey.ModifierButton);
                
                if (hotkey.IsShared && _playerIndex == 1)
                    desc = "[Shared] " + desc;
                else if (isFallback)
                    desc = "[From P1] " + desc;

                var item = new ListViewItem(new[]
                {
                    string.Format("{0} + {1}", hotkey.ModifierButton, hotkey.TriggerButton),
                    hotkey.ShortPressKeys != null && hotkey.ShortPressKeys.Count > 0 ? string.Join("+", hotkey.ShortPressKeys) : "(None)",
                    hotkey.LongPressKeys != null && hotkey.LongPressKeys.Count > 0 ? string.Join("+", hotkey.LongPressKeys) : "(None)",
                    desc
                });
                
                if (isFallback)
                {
                     item.ForeColor = Color.FromArgb(170, 170, 170); // Dim shared ones (Griser les partagées)
                }
                
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
                    // Force save to Options and update Manager (EN/FR: Forcer sauvegarde et MAJ Manager)
                    HotkeyManager.SetProfile(_playerIndex, _hotkeyProfile);
                    LoadHotkeys();
                }
            }
        }

        private void EditSelectedHotkey()
        {
            if (_listViewHotkeys.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select a hotkey to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedHotkey = _listViewHotkeys.SelectedItems[0].Tag as Hotkey;
            
            // CRITICAL: Clone the hotkey before editing to avoid modifying the live object directly
            // (EN/FR: Cloner la hotkey avant édition pour éviter modification directe)
            bool isFallback = _playerIndex != 1 && !_hotkeyProfile.HasHotkey(selectedHotkey.TriggerButton, selectedHotkey.ModifierButton);
            var hotkeyToEdit = selectedHotkey.Clone();
            
            // If it's a fallback from P1, editing it effectively creates an override for the current player
            if (isFallback)
            {
                 hotkeyToEdit.IsShared = false; // Overrides are local by default
            }
            
            using (var dialog = new HotkeyInputDialog(_playerIndex, hotkeyToEdit))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Remove the OLD hotkey (using original values from selectedHotkey)
                    // (EN/FR: Supprimer l'ANCIENNE hotkey)
                    // We extract Trigger and Modifier explicitly in case they were changed in the dialog, 
                    // but here we must remove the OLD one first.
                    _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.ModifierButton);
                    
                    // Add the NEW/UPDATED hotkey
                    // (EN/FR: Ajouter la NOUVELLE hotkey)
                    _hotkeyProfile.AddOrUpdateHotkey(dialog.Hotkey);
                    
                    // Force save to Options and update Manager
                    HotkeyManager.SetProfile(_playerIndex, _hotkeyProfile);
                    
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
                MessageBox.Show(this, "Please select a hotkey to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedHotkey = _listViewHotkeys.SelectedItems[0].Tag as Hotkey;
            
            // Cannot delete shared P1 hotkeys from P2 editor
            bool isFallback = _playerIndex != 1 && !_hotkeyProfile.HasHotkey(selectedHotkey.TriggerButton, selectedHotkey.ModifierButton);
            if (isFallback)
            {
                MessageBox.Show(this, "Shared hotkeys from Player 1 can only be deleted from the Player 1 configuration.", "Cannot Delete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(this,
                string.Format("Delete hotkey: {0}?", selectedHotkey.GetDisplayName()),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _hotkeyProfile.RemoveHotkey(selectedHotkey.TriggerButton, selectedHotkey.ModifierButton);
                // Force save to Options and update Manager
                HotkeyManager.SetProfile(_playerIndex, _hotkeyProfile);
                LoadHotkeys();
            }
        }

        /// <summary>
        /// Get the updated hotkey profile
        /// </summary>
        public HotkeyProfile HotkeyProfile { get { return _hotkeyProfile; } }
    }
}
