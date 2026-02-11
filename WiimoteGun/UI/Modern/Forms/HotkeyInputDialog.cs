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

        // All available modifier buttons (EN/FR: Tous les boutons modifier disponibles)
        private static readonly string[] AllModifierButtons = new[] { "A", "B", "One", "Two", "Plus", "Minus", "Home", "Up", "Down", "Left", "Right" };

        // Trigger buttons exclude Home — reserved as base modifier, hardware function on Bluetooth
        // (EN/FR: Les triggers excluent Home — réservé comme modifier de base, fonction matérielle en Bluetooth)
        private static readonly string[] AllTriggerButtons = new[] { "A", "B", "One", "Two", "Plus", "Minus", "Up", "Down", "Left", "Right" };

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
            // Populate modifier list — filter Home if DolphinBar mode
            // (EN/FR: Remplir la liste modifier — masquer Home si mode DolphinBar)
            foreach (string btn in AllModifierButtons)
            {
                // DolphinBar: Home is a hardware button (changes connection mode with D-pad)
                // (EN/FR: DolphinBar : Home est un bouton matériel, change le mode de connexion avec D-pad)
                if (string.Equals(btn, "Home", StringComparison.OrdinalIgnoreCase) && Options.Instance.DetectDolphinbar)
                    continue;

                _cmbModifier.Items.Add(btn);
            }
            _cmbModifier.SelectedIndexChanged += CmbModifier_SelectedIndexChanged;
            this.Controls.Add(_cmbModifier);
        }

        /// <summary>
        /// Update trigger list when modifier changes, excluding the selected modifier
        /// (EN/FR: Mettre à jour la liste trigger quand le modifier change, en excluant le modifier sélectionné)
        /// </summary>
        private void CmbModifier_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTriggerList();
        }

        // D-pad buttons reserved for offset adjustment with Home (BT) or Minus (DolphinBar)
        // (EN/FR: Boutons D-pad réservés pour l'ajustement d'offset avec Home (BT) ou Minus (DolphinBar))
        private static readonly HashSet<string> DpadButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Up", "Down", "Left", "Right" };

        /// <summary>
        /// Populate trigger ComboBox with contextual filtering based on modifier and connection type
        /// (EN/FR: Remplir le ComboBox trigger avec filtrage contextuel basé sur le modifier et le type de connexion)
        /// </summary>
        private void UpdateTriggerList()
        {
            string currentTrigger = _cmbTriggerButton.SelectedItem as string;
            string selectedModifier = _cmbModifier.SelectedItem as string;

            _cmbTriggerButton.Items.Clear();

            foreach (string btn in AllTriggerButtons)
            {
                // Exclude the selected modifier from trigger list
                // (EN/FR: Exclure le modifier sélectionné de la liste trigger)
                if (string.Equals(btn, selectedModifier, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Home + D-pad = offset adjustment on Bluetooth → hide D-pad triggers
                // Minus + D-pad = offset adjustment on DolphinBar → hide D-pad triggers
                // (EN/FR: Home/Minus + D-pad = ajustement offset → masquer D-pad des triggers)
                if (DpadButtons.Contains(btn) &&
                    (string.Equals(selectedModifier, "Home", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(selectedModifier, "Minus", StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Home + Plus = Overlay command → hide Plus when Home is modifier
                // (EN/FR: Home + Plus = commande Overlay → masquer Plus quand Home est modifier)
                if (string.Equals(btn, "Plus", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(selectedModifier, "Home", StringComparison.OrdinalIgnoreCase))
                    continue;

                _cmbTriggerButton.Items.Add(btn);
            }

            // Restore previous selection if still valid
            // (EN/FR: Restaurer la sélection précédente si encore valide)
            if (currentTrigger != null && _cmbTriggerButton.Items.Contains(currentTrigger))
            {
                _cmbTriggerButton.SelectedItem = currentTrigger;
            }
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
