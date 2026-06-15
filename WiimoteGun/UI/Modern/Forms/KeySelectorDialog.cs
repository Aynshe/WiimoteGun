using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WiimoteGun.UI.Modern.Forms;

namespace WiimoteGun
{
    
    /// <summary>
    /// Modal dialog for selecting a keyboard key with scrollable list (EN/FR: Dialogue modal pour sélectionner une touche clavier avec liste scrollable)
    /// </summary>
    public partial class KeySelectorDialog : Form
    {
        public Keys SelectedKey { get; private set; }
        
        public KeySelectorDialog()
        {
            SelectedKey = Keys.None;
            InitializeComponent();
            
            // Set FlatAppearance border sizes (Designer doesn't support this)
            btnOK.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;
            
            PopulateKeys();
            
            txtSearch.Click += (s, e) => ShowVirtualKeyboard();
        }
        
        // Event handlers
        private void listBoxKeys_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxKeys.SelectedItem != null && listBoxKeys.SelectedItem is KeyDisplayItem)
            {
                KeyDisplayItem item = (KeyDisplayItem)listBoxKeys.SelectedItem;
                SelectedKey = item.Key;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private static bool IsDigitOrOemKey(Keys key)
        {
            // EN/FR: Détecter les touches de chiffres et OEM pour ajouter leur variante Shiftée
            return (key >= Keys.D0 && key <= Keys.D9) ||
                   key == Keys.Oemtilde ||
                   key == Keys.OemMinus ||
                   key == Keys.Oemplus ||
                   key == Keys.OemOpenBrackets ||
                   key == Keys.OemCloseBrackets ||
                   key == Keys.OemPipe ||
                   key == Keys.OemSemicolon ||
                   key == Keys.OemQuotes ||
                   key == Keys.Oemcomma ||
                   key == Keys.OemPeriod ||
                   key == Keys.OemQuestion ||
                   key == Keys.Oem102;
        }

        private void PopulateKeys()
        {
            // Get all Keys enum values wrapped with AZERTY display names (EN/FR: Obtenir toutes valeurs Keys avec noms AZERTY)
            var baseKeys = Enum.GetValues(typeof(Keys))
                .Cast<Keys>()
                .Where(k => k != Keys.None && k != Keys.Menu) // Exclude None and Menu
                .ToArray();

            var list = new System.Collections.Generic.List<KeyDisplayItem>();
            foreach (var k in baseKeys)
            {
                list.Add(new KeyDisplayItem(k));
                if (IsDigitOrOemKey(k))
                {
                    list.Add(new KeyDisplayItem(k | Keys.Shift));
                }
            }

            var keys = list.OrderBy(k => k.DisplayName).ToArray();
            
            listBoxKeys.Items.Clear();
            foreach (var key in keys)
            {
                listBoxKeys.Items.Add(key);
            }
        }
        
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // Filter keys based on search (check both enum name and display name)
            // (EN/FR: Filtrer touches selon recherche - vérifie nom enum et nom affiché)
            string searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                PopulateKeys();
                return;
            }
            
            var baseKeys = Enum.GetValues(typeof(Keys))
                .Cast<Keys>()
                .Where(k => k != Keys.None && k != Keys.Menu)
                .ToArray();

            var list = new System.Collections.Generic.List<KeyDisplayItem>();
            foreach (var k in baseKeys)
            {
                list.Add(new KeyDisplayItem(k));
                if (IsDigitOrOemKey(k))
                {
                    list.Add(new KeyDisplayItem(k | Keys.Shift));
                }
            }

            var filteredKeys = list
                .Where(k => k.DisplayName.ToLower().Contains(searchText) || k.Key.ToString().ToLower().Contains(searchText))
                .OrderBy(k => k.DisplayName)
                .ToArray();
            
            listBoxKeys.Items.Clear();
            foreach (var key in filteredKeys)
            {
                listBoxKeys.Items.Add(key);
            }
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (listBoxKeys.SelectedItem != null && listBoxKeys.SelectedItem is KeyDisplayItem)
            {
                KeyDisplayItem item = (KeyDisplayItem)listBoxKeys.SelectedItem;
                SelectedKey = item.Key;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }
        
        private void ShowVirtualKeyboard()
        {
             VirtualKeyboard keyboard = new VirtualKeyboard(txtSearch);
             keyboard.StartPosition = FormStartPosition.Manual;
             
             Point screenPos = txtSearch.PointToScreen(new Point(0, txtSearch.Height));
             keyboard.Location = new Point(
                screenPos.X + (txtSearch.Width - keyboard.Width) / 2,
                screenPos.Y + 5 
             );
             
            var screen = Screen.FromControl(this);
            if (keyboard.Bottom > screen.WorkingArea.Bottom)
            {
                 keyboard.Top = txtSearch.PointToScreen(Point.Empty).Y - keyboard.Height - 5;
            }

             keyboard.ShowDialog(this);
        }
    }
}
