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
            LoadProfileUI(); // Initialize Profile tab (EN/FR: Initialiser onglet Profils)
            
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

        // ========== PROFILE MANAGEMENT (EN/FR: Gestion des profils) ==========

        private void LoadProfileUI()
        {
            // Load subfolders (EN/FR: Charger sous-dossiers)
            comboBoxSubfolders.Items.Clear();
            comboBoxSubfolders.Items.Add("[Root]");
            
            var subfolders = RemapProfileManager.GetSubfolders();
            foreach (var folder in subfolders)
            {
                comboBoxSubfolders.Items.Add(folder);
            }
            
            // Select [Root] by default
            comboBoxSubfolders.SelectedIndex = 0;
            
            RefreshProfileList();
        }

        private void RefreshProfileList()
        {
            comboBoxProfiles.Items.Clear();
            
            string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Root]")
                selectedFolder = null;
            
            var profiles = RemapProfileManager.GetProfilesInFolder(selectedFolder);
            foreach (var profile in profiles)
            {
                comboBoxProfiles.Items.Add(profile);
            }
            
            if (comboBoxProfiles.Items.Count > 0)
                comboBoxProfiles.SelectedIndex = 0;
        }

        private void comboBoxSubfolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshProfileList();
        }

        private void btnRefreshProfiles_Click(object sender, EventArgs e)
        {
            LoadProfileUI();
            MessageBox.Show("Profile list refreshed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnNewFolder_Click(object sender, EventArgs e)
        {
            using (var inputDialog = new Form())
            {
                inputDialog.Text = "New Folder";
                inputDialog.Width = 300;
                inputDialog.Height = 120;
                inputDialog.StartPosition = FormStartPosition.CenterParent;
                
                Label label = new Label() { Left = 10, Top = 15, Width = 280, Text = "Folder Name:" };
                TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 260 };
                Button okButton = new Button() { Text = "OK", Left = 110, Top = 60, DialogResult = DialogResult.OK };
                Button cancelButton = new Button() { Text = "Cancel", Left = 190, Top = 60, DialogResult = DialogResult.Cancel };
                
                inputDialog.Controls.Add(label);
                inputDialog.Controls.Add(textBox);
                inputDialog.Controls.Add(okButton);
                inputDialog.Controls.Add(cancelButton);
                inputDialog.AcceptButton = okButton;
                inputDialog.CancelButton = cancelButton;
                
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderName = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        try
                        {
                            string remapDir = RemapProfileManager.GetRemapDirectory();
                            string newFolderPath = System.IO.Path.Combine(remapDir, folderName);
                            System.IO.Directory.CreateDirectory(newFolderPath);
                            
                            LoadProfileUI();
                            comboBoxSubfolders.SelectedItem = folderName;
                            MessageBox.Show(string.Format("Folder '{0}' created!", folderName), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to create folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnSaveProfile_Click(object sender, EventArgs e)
        {
            string profileName = txtProfileName.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Ensure current player settings are saved (EN/FR: Assurer sauvegarde joueur actuel)
            SaveCurrentPlayerSettings();
            
            // Build profile from current Options (EN/FR: Construire profil depuis Options actuel)
            var profile = new RemapProfile
            {
                ProfileName = profileName,
                P1Mappings = new PlayerMappings(),
                P2Mappings = new PlayerMappings(),
                P3Mappings = new PlayerMappings(),
                P4Mappings = new PlayerMappings()
            };
            
            profile.P1Mappings.CopyFrom(Options.Instance.P1Mappings);
            profile.P2Mappings.CopyFrom(Options.Instance.P2Mappings);
            profile.P3Mappings.CopyFrom(Options.Instance.P3Mappings);
            profile.P4Mappings.CopyFrom(Options.Instance.P4Mappings);
            
            // Get subfolder (EN/FR: Obtenir sous-dossier)
            string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
            if (selectedFolder == "[Root]")
                selectedFolder = null;
            
            // Save profile (EN/FR: Sauvegarder profil)
            bool success = RemapProfileManager.SaveProfile(profileName, selectedFolder, profile);
            
            if (success)
            {
                RefreshProfileList();
                MessageBox.Show(string.Format("Profile '{0}' saved successfully!", profileName), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save profile. Check logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadProfile_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                MessageBox.Show("Please select a profile to load.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Build relative path (EN/FR: Construire chemin relatif)
            string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
            string relativePath;
            
            if (selectedFolder == "[Root]" || string.IsNullOrEmpty(selectedFolder))
                relativePath = selectedProfile;
            else
                relativePath = System.IO.Path.Combine(selectedFolder, selectedProfile);
            
            // Load profile (EN/FR: Charger profil)
            var profile = RemapProfileManager.LoadProfile(relativePath);
            
            if (profile != null)
            {
                // Apply to Options (EN/FR: Appliquer aux Options)
                if (profile.P1Mappings != null)
                    Options.Instance.P1Mappings.CopyFrom(profile.P1Mappings);
                if (profile.P2Mappings != null)
                    Options.Instance.P2Mappings.CopyFrom(profile.P2Mappings);
                if (profile.P3Mappings != null)
                    Options.Instance.P3Mappings.CopyFrom(profile.P3Mappings);
                if (profile.P4Mappings != null)
                    Options.Instance.P4Mappings.CopyFrom(profile.P4Mappings);
                
                // Reload UI for current player (EN/FR: Recharger UI pour joueur actuel)
                LoadSettings();
                
                // Set profile name in text field for easy updates (EN/FR: Définir nom profil pour mises à jour faciles)
                // Remove .remap extension if present (EN/FR: Retirer extension .remap si présente)
                string profileNameWithoutExt = selectedProfile.EndsWith(".remap", StringComparison.OrdinalIgnoreCase)
                    ? selectedProfile.Substring(0, selectedProfile.Length - 6)
                    : selectedProfile;
                txtProfileName.Text = profileNameWithoutExt;
                
                MessageBox.Show(string.Format("Profile '{0}' loaded successfully!", profile.ProfileName), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to load profile. Check logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedProfile))
            {
                MessageBox.Show("Please select a profile to delete.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(string.Format("Are you sure you want to delete '{0}'?", selectedProfile), 
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string selectedFolder = comboBoxSubfolders.SelectedItem?.ToString();
                    string relativePath;
                    
                    if (selectedFolder == "[Root]" || string.IsNullOrEmpty(selectedFolder))
                        relativePath = selectedProfile;
                    else
                        relativePath = System.IO.Path.Combine(selectedFolder, selectedProfile);
                    
                    string remapDir = RemapProfileManager.GetRemapDirectory();
                    string fullPath = System.IO.Path.Combine(remapDir, relativePath);
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        RefreshProfileList();
                        MessageBox.Show(string.Format("Profile '{0}' deleted successfully!", selectedProfile), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Profile file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Failed to delete profile: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
