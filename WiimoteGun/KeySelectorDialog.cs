using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace WiimoteGun
{
    /// <summary>
    /// Modal dialog for selecting a keyboard key with scrollable list (EN/FR: Dialogue modal pour sélectionner une touche clavier avec liste scrollable)
    /// </summary>
    public class KeySelectorDialog : Form
    {
        private ListBox listBoxKeys;
        private Button btnOK;
        private Button btnCancel;
        private TextBox txtSearch;
        
        public Keys SelectedKey { get; private set; } = Keys.None;
        
        public KeySelectorDialog()
        {
            InitializeComponents();
            PopulateKeys();
        }
        
        private void InitializeComponents()
        {
            // Form properties (EN/FR: Propriétés formulaire)
            this.Text = "Select Keyboard Key";
            this.Size = new Size(350, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(26, 26, 26);
            
            // Search box (EN/FR: Boîte recherche)
            txtSearch = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(310, 25),
                BackColor = Color.FromArgb(37, 37, 37),
                ForeColor = Color.FromArgb(224, 224, 224),
                Font = new Font("Segoe UI", 10F)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            this.Controls.Add(txtSearch);
            
            // ListBox with scroll (EN/FR: ListBox avec scroll)
            listBoxKeys = new ListBox
            {
                Location = new Point(10, 45),
                Size = new Size(310, 360),
                BackColor = Color.FromArgb(37, 37, 37),
                ForeColor = Color.FromArgb(224, 224, 224),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            listBoxKeys.DoubleClick += (s, e) => { if (listBoxKeys.SelectedItem != null) this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.Add(listBoxKeys);
            
            // OK Button (EN/FR: Bouton OK)
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(130, 420),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);
            
            // Cancel Button (EN/FR: Bouton Annuler)
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(230, 420),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);
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
    }
}
