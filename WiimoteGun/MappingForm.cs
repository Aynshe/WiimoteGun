using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WiimoteGun
{
    public partial class MappingForm : Form
    {
        private int _currentPlayer = 1;
        private bool _isInitializing = true;

        public MappingForm()
        {
            InitializeComponent();
            Font = SystemFonts.MessageBoxFont;

            PopulateComboBoxes();
            
            // Select Player 1 by default AFTER populating comboboxes (EN/FR: Sélectionner Joueur 1 par défaut APRÈS avoir rempli les comboboxes)
            playerComboBox.SelectedIndex = 0;
            
            LoadSettings();
            
            _isInitializing = false; // Allow saving now
        }

        private void PopulateComboBoxes()
        {
            var actions = new object[] { new ButtonAction() }
                .Concat(Enum.GetValues(typeof(SpecialAction)).Cast<SpecialAction>()
                    .Where(sa => sa != SpecialAction.None)
                    .Select(sa => new ButtonAction(sa)))
                .Concat(Enum.GetValues(typeof(Keys)).Cast<Keys>()
                    .Where(k => k != Keys.None && k != Keys.Menu)
                    .Select(k => new ButtonAction(k)))
                .ToArray();

            foreach (TabPage page in tabControl1.TabPages)
            {
                foreach (var comboBox in page.Controls.OfType<ComboBox>())
                {
                    comboBox.Items.AddRange(actions);
                }
            }
        }

        private void LoadSettings()
        {
            var mappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);

            comboBoxWiiA.SelectedItem = mappings.WiiA;
            comboBoxWiiB.SelectedItem = mappings.WiiB;
            comboBoxWiiUp.SelectedItem = mappings.WiiUp;
            comboBoxWiiDown.SelectedItem = mappings.WiiDown;
            comboBoxWiiLeft.SelectedItem = mappings.WiiLeft;
            comboBoxWiiRight.SelectedItem = mappings.WiiRight;
            comboBoxWiiOne.SelectedItem = mappings.WiiOne;
            comboBoxWiiTwo.SelectedItem = mappings.WiiTwo;
            comboBoxWiiPlus.SelectedItem = mappings.WiiPlus;
            comboBoxWiiMinus.SelectedItem = mappings.WiiMinus;
            comboBoxNunC.SelectedItem = mappings.NunC;
            comboBoxNunZ.SelectedItem = mappings.NunZ;
            comboBoxNunUp.SelectedItem = mappings.NunUp;
            comboBoxNunDown.SelectedItem = mappings.NunDown;
            comboBoxNunLeft.SelectedItem = mappings.NunLeft;
            comboBoxNunRight.SelectedItem = mappings.NunRight;
        }

        private void SaveCurrentPlayerSettings()
        {
            var mappings = Options.Instance.GetMappingsForPlayer(_currentPlayer);

            // Save button mappings for current player
            mappings.WiiA = (ButtonAction)comboBoxWiiA.SelectedItem;
            mappings.WiiB = (ButtonAction)comboBoxWiiB.SelectedItem;
            mappings.WiiUp = (ButtonAction)comboBoxWiiUp.SelectedItem;
            mappings.WiiDown = (ButtonAction)comboBoxWiiDown.SelectedItem;
            mappings.WiiLeft = (ButtonAction)comboBoxWiiLeft.SelectedItem;
            mappings.WiiRight = (ButtonAction)comboBoxWiiRight.SelectedItem;
            mappings.WiiOne = (ButtonAction)comboBoxWiiOne.SelectedItem;
            mappings.WiiTwo = (ButtonAction)comboBoxWiiTwo.SelectedItem;
            mappings.WiiPlus = (ButtonAction)comboBoxWiiPlus.SelectedItem;
            mappings.WiiMinus = (ButtonAction)comboBoxWiiMinus.SelectedItem;
            mappings.NunC = (ButtonAction)comboBoxNunC.SelectedItem;
            mappings.NunZ = (ButtonAction)comboBoxNunZ.SelectedItem;
            mappings.NunUp = (ButtonAction)comboBoxNunUp.SelectedItem;
            mappings.NunDown = (ButtonAction)comboBoxNunDown.SelectedItem;
            mappings.NunLeft = (ButtonAction)comboBoxNunLeft.SelectedItem;
            mappings.NunRight = (ButtonAction)comboBoxNunRight.SelectedItem;
        }

        private void playerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return; // Don't save during initialization
            
            // Save current player settings before switching
            SaveCurrentPlayerSettings();

            // Switch to new player
            _currentPlayer = playerComboBox.SelectedIndex + 1;

            // Load new player settings
            LoadSettings();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Save current player settings
            SaveCurrentPlayerSettings();

            // Save all to file
            Options.Instance.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            Options.Instance.ResetPlayerMappings(_currentPlayer);
            LoadSettings();
        }
    }
}
