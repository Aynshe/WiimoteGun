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
        public Keys SelectedKey { get; private set; } = Keys.None;
        
        public KeySelectorDialog()
        {
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
            if (listBoxKeys.SelectedItem != null)
            {
                SelectedKey = (Keys)listBoxKeys.SelectedItem;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void PopulateKeys()
        {
            // Get all Keys enum values (EN/FR: Obtenir toutes valeurs enum Keys)
            var keys = Enum.GetValues(typeof(Keys))
                .Cast<Keys>()
                .Where(k => k != Keys.None && k != Keys.Menu) // Exclude None and Menu
                .OrderBy(k => k.ToString())
                .ToArray();
            
            listBoxKeys.Items.Clear();
            foreach (var key in keys)
            {
                listBoxKeys.Items.Add(key);
            }
        }
        
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // Filter keys based on search (EN/FR: Filtrer touches selon recherche)
            string searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                PopulateKeys();
                return;
            }
            
            var filteredKeys = Enum.GetValues(typeof(Keys))
                .Cast<Keys>()
                .Where(k => k != Keys.None && k != Keys.Menu)
                .Where(k => k.ToString().ToLower().Contains(searchText))
                .OrderBy(k => k.ToString())
                .ToArray();
            
            listBoxKeys.Items.Clear();
            foreach (var key in filteredKeys)
            {
                listBoxKeys.Items.Add(key);
            }
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (listBoxKeys.SelectedItem != null)
            {
                SelectedKey = (Keys)listBoxKeys.SelectedItem;
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

