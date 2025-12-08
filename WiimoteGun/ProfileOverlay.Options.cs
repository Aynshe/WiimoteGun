using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace WiimoteGun
{
    public partial class ProfileOverlay
    {
        // Options page controls (EN/FR: Contrôles page Options)
        private Panel panelOptions;
        private Panel panelOptionsSidebar;
        private Panel panelOptionsContent;
        private string _currentOptionsCategory = "General";
        
        /// <summary>
        /// Initialize Options panel with 2-column layout (EN/FR: Initialiser panel Options avec layout 2 colonnes)
        /// </summary>
        private void InitializeOptionsPanel()
        {
            // Main options panel
            panelOptions = new Panel
            {
                Name = "panelOptions",
                Size = new Size(560, 660),
                Location = new Point(20, 30),
                BackColor = Color.Transparent,
                Visible = false
            };
            
            // Sidebar (left column) for categories (EN/FR: Barre latérale (colonne gauche) pour catégories)
            panelOptionsSidebar = new Panel
            {
                Size = new Size(150, 660),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None
            };
            
            // Create category buttons (EN/FR: Créer boutons catégories)
            string[] categories = { "General", "Calibration", "Keyboard", "Detection", "Gestures", "Emulators", "Players" };
            string[] icons = { "⚙️", "🎯", "⌨️", "📡", "🤌", "🎮", "👥" };
            
            int yPos = 10;
            for (int i = 0; i < categories.Length; i++)
            {
                Button btnCategory = CreateCategoryButton(categories[i], icons[i], yPos);
                btnCategory.Click += (s, e) => ShowOptionsCategory(((Button)s).Tag.ToString());
                panelOptionsSidebar.Controls.Add(btnCategory);
                yPos += 50;
            }
            
            // Content panel (right column - scrollable) (EN/FR: Panel contenu (colonne droite - scrollable))
            panelOptionsContent = new Panel
            {
                Size = new Size(400, 600),
                Location = new Point(155, 5),
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            
            // Apply and Reset buttons at bottom (EN/FR: Boutons Apply et Reset en bas)
            Button btnApply = new Button
            {
                Text = "💾 Apply & Restart",
                Size = new Size(180, 40),
                Location = new Point(155, 615),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApplyOptions_Click;
            
            Button btnReset = new Button
            {
                Text = "↺ Reset",
                Size = new Size(100, 40),
                Location = new Point(345, 615),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) => LoadOptionsFromInstance();
            
            // Add all to main panel
            panelOptions.Controls.Add(panelOptionsSidebar);
            panelOptions.Controls.Add(panelOptionsContent);
            panelOptions.Controls.Add(btnApply);
            panelOptions.Controls.Add(btnReset);
            
            this.Controls.Add(panelOptions);
            panelOptions.BringToFront();
            
            // Load default category (EN/FR: Charger catégorie par défaut)
            ShowOptionsCategory("General");
        }
        
        private Button CreateCategoryButton(string name, string icon, int y)
        {
            var btn = new Button
            {
                Text = $"{icon} {name}",
                Tag = name,
                Size = new Size(145, 45),
                Location = new Point(2, y),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            
            // Hover effect
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(60, 60, 60);
            btn.MouseLeave += (s, e) => btn.BackColor = (btn.Tag.ToString() == _currentOptionsCategory) 
                ? Color.FromArgb(0, 122, 204) 
                : Color.FromArgb(40, 40, 40);
            
            return btn;
        }
        
        private void ShowOptionsCategory(string category)
        {
            _currentOptionsCategory = category;
            
            // Update sidebar button colors (EN/FR: Mettre à jour couleurs boutons sidebar)
            foreach (Control ctrl in panelOptionsSidebar.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = (btn.Tag.ToString() == category) 
                        ? Color.FromArgb(0, 122, 204) 
                        : Color.FromArgb(40, 40, 40);
                }
            }
            
            // Clear content panel (EN/FR: Vider panel contenu)
            panelOptionsContent.Controls.Clear();
            
            // Load category content (EN/FR: Charger contenu catégorie)
            switch (category)
            {
                case "General": LoadGeneralOptions(); break;
                case "Calibration": LoadCalibrationOptions(); break;
                case "Keyboard": LoadKeyboardOptions(); break;
                case "Detection": LoadDetectionOptions(); break;
                case "Gestures": LoadGesturesOptions(); break;
                case "Emulators": LoadEmulatorsOptions(); break;
                case "Players": LoadPlayersOptions(); break;
            }
        }
        
        private int _optionsYOffset;
        
        private void AddOptionLabel(string text)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(10, _optionsYOffset),
                Size = new Size(180, 25),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelOptionsContent.Controls.Add(lbl);
        }
        
        private ComboBox AddOptionComboBox(string[] items, string selectedValue = null)
        {
            ComboBox combo = new ComboBox
            {
                Location = new Point(200, _optionsYOffset),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            combo.Items.AddRange(items);
            if (selectedValue != null && combo.Items.Contains(selectedValue))
                combo.SelectedItem = selectedValue;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
            
            panelOptionsContent.Controls.Add(combo);
            _optionsYOffset += 35;
            return combo;
        }
        
        private CheckBox AddOptionCheckBox(string text, bool isChecked)
        {
            CheckBox chk = new CheckBox
            {
                Text = text,
                Location = new Point(200, _optionsYOffset),
                Size = new Size(180, 25),
                Checked = isChecked,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9F)
            };
            panelOptionsContent.Controls.Add(chk);
            _optionsYOffset += 35;
            return chk;
        }
        
        private NumericUpDown AddOptionNumeric(decimal value, decimal min, decimal max)
        {
            NumericUpDown numeric = new NumericUpDown
            {
                Location = new Point(200, _optionsYOffset),
                Size = new Size(100, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9F)
            };
            panelOptionsContent.Controls.Add(numeric);
            _optionsYOffset += 35;
            return numeric;
        }
        
        private TrackBar AddOptionTrackBar(int value, int min, int max)
        {
            TrackBar track = new TrackBar
            {
                Location = new Point(200, _optionsYOffset - 5),
                Size = new Size(180, 45),
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.BottomRight
            };
            panelOptionsContent.Controls.Add(track);
            
            Label lbl Value = new Label
            {
                Text = value.ToString(),
                Location = new Point(250, _optionsYOffset + 25),
                Size = new Size(30, 20),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.TopCenter
            };
            panelOptionsContent.Controls.Add(lblValue);
            track.ValueChanged += (s, e) => lblValue.Text = track.Value.ToString();
            
            _optionsYOffset += 50;
            return track;
        }
        
        private void AddSectionTitle(string title)
        {
            if (_optionsYOffset > 10) _optionsYOffset += 10; // Spacing
            
            Label lbl = new Label
            {
                Text = title,
                Location = new Point(5, _optionsYOffset),
                Size = new Size(380, 25),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelOptionsContent.Controls.Add(lbl);
            _optionsYOffset += 35;
        }
        
        // To be continued in next message due to length...
    }
}
