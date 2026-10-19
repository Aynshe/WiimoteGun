using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V61-FIX-6] Manager for the AUTO off-screen reload FOREGROUND exclusions.
    ///     The auto reload never fires while one of the CHECKED executables is the
    ///     foreground process (built-in: emulationstation.exe, explorer.exe). The user
    ///     can add executables via the file browser, disable an entry without removing
    ///     it (checkbox) or remove it (X button). Every action saves Options
    ///     immediately — no Save button. Bilingual FR/EN (Windows culture).
    /// FR: [V61-FIX-6] Gestionnaire des exclusions du rechargement AUTO hors écran
    ///     (exécutables au PREMIER PLAN). La recharge auto ne se déclenche jamais quand
    ///     l'un des exécutables COCHÉS est au premier plan (intégrés :
    ///     emulationstation.exe, explorer.exe). L'utilisateur peut ajouter des
    ///     exécutables via l'explorateur, désactiver une entrée sans la supprimer
    ///     (case à cocher) ou la retirer (bouton X). Chaque action sauvegarde
    ///     immédiatement — pas de bouton Enregistrer. Bilingue FR/EN (culture Windows).
    /// </summary>
    internal class OffScreenExcludesForm : Form
    {
        private bool _isFrench;
        private Panel _listPanel;

        public OffScreenExcludesForm()
        {
            try
            {
                var ci = System.Globalization.CultureInfo.CurrentUICulture;
                _isFrench = ci != null && (ci.TwoLetterISOLanguageName == "fr" || ci.Name.StartsWith("fr", StringComparison.Ordinal));
            }
            catch { _isFrench = false; }

            // Form setup (EN/FR: Configuration du formulaire)
            BackColor = Color.FromArgb(30, 30, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 430);
            Text = "Wiimote4Guns — " + L("Auto Off-Screen Reload: excluded executables",
                                          "Rechargement AUTO hors écran : exécutables exclus");

            Label lblTitle = new Label
            {
                Text = L("Foreground executables excluded from the AUTO off-screen reload",
                         "Exécutables au premier plan exclus du rechargement AUTO hors écran"),
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(14, 12),
                Size = new Size(492, 22)
            };
            Controls.Add(lblTitle);

            Label lblHint = new Label
            {
                Text = L("The AUTO off-screen reload is suppressed while one of the CHECKED executables is in the FOREGROUND.\r\nBuilt-in: emulationstation.exe (RetroBat frontend), explorer.exe (Windows shell).",
                         "La recharge AUTO hors écran est supprimée quand l'un des exécutables COCHÉS est au PREMIER PLAN.\r\nIntégrés : emulationstation.exe (frontend RetroBat), explorer.exe (shell Windows)."),
                ForeColor = Color.FromArgb(160, 160, 170),
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(14, 38),
                Size = new Size(492, 40)
            };
            Controls.Add(lblHint);

            Button btnAdd = new Button
            {
                Text = L("Add executable...", "Ajouter un exécutable..."),
                BackColor = Color.FromArgb(0, 122, 204),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new Point(14, 84),
                Size = new Size(170, 28)
            };
            btnAdd.Click += (s, e) => AddExecutable();
            Controls.Add(btnAdd);

            _listPanel = new Panel
            {
                BackColor = Color.FromArgb(38, 38, 48),
                AutoScroll = true,
                Location = new Point(14, 120),
                Size = new Size(492, 264),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_listPanel);

            Button btnClose = new Button
            {
                Text = L("Close", "Fermer"),
                BackColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new Point(426, 392),
                Size = new Size(80, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            RebuildList();
            WiimoteGun.UI.UiScaler.ApplyForm(this); // [V57n] UI zoom (bounds, fonts, screen clamp)
        }

        private string L(string en, string fr) { return _isFrench ? fr : en; }

        /// <summary>
        /// EN: [V61-FIX-6] File browser → add one executable (duplicates rejected).
        /// FR: [V61-FIX-6] Explorateur → ajoute un exécutable (doublons refusés).
        /// </summary>
        private void AddExecutable()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = L("Select an executable to exclude", "Sélectionnez un exécutable à exclure"),
                Filter = "Executable (*.exe)|*.exe",
                CheckFileExists = true
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                string name = System.IO.Path.GetFileName(ofd.FileName);

                var list = Options.Instance.OffScreenAutoExcludes;
                if (list == null)
                {
                    list = new System.Collections.Generic.List<OffScreenExeExclude>();
                    Options.Instance.OffScreenAutoExcludes = list;
                }

                foreach (OffScreenExeExclude ex in list)
                {
                    if (ex != null && string.Equals(ex.ExeName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(this,
                            L("'" + name + "' is already in the list.", "'" + name + "' est déjà dans la liste."),
                            "Wiimote4Guns", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                list.Add(new OffScreenExeExclude(name));
                SaveAndRebuild("[V61-FIX-6] Auto-reload exclusion added: " + name);
            }
        }

        /// <summary>
        /// EN: [V61-FIX-6] AUTO-SAVE on every change - there is no Save button.
        /// FR: [V61-FIX-6] SAUVEGARDE AUTOMATIQUE à chaque changement - pas de bouton Enregistrer.
        /// </summary>
        private void SaveAndRebuild(string logMessage)
        {
            Options.Instance.Save();
            SimpleLogger.Instance.Info(logMessage);
            RebuildList();
        }

        /// <summary>
        /// EN: [V61-FIX-6] Rebuilds the list: one row per entry = checkbox (enable,
        ///     exe name as label) + X button (remove). Auto-save on every action.
        /// FR: [V61-FIX-6] Reconstruit la liste : une ligne par entrée = case à cocher
        ///     (activer, nom de l'exe comme libellé) + bouton X (retirer). Auto-save à
        ///     chaque action.
        /// </summary>
        private void RebuildList()
        {
            _listPanel.SuspendLayout();
            _listPanel.Controls.Clear();

            var list = Options.Instance.OffScreenAutoExcludes;
            int y = 8;
            if (list != null)
            {
                foreach (OffScreenExeExclude entry in list)
                {
                    OffScreenExeExclude captured = entry;
                    int rowY = y;

                    CheckBox chk = new CheckBox
                    {
                        Checked = captured.Enabled,
                        Text = captured.ExeName,
                        ForeColor = Color.White,
                        AutoSize = true,
                        Location = new Point(10, rowY)
                    };
                    chk.CheckedChanged += (s, e) =>
                    {
                        // EN/FR: checkbox = enable/disable WITHOUT removing the entry
                        captured.Enabled = chk.Checked;
                        Options.Instance.Save(); // auto-save, no Save button
                        SimpleLogger.Instance.Info("[V61-FIX-6] Auto-reload exclusion '" + captured.ExeName + "' " + (chk.Checked ? "enabled" : "disabled") + ".");
                    };
                    _listPanel.Controls.Add(chk);

                    Button btnRemove = new Button
                    {
                        Text = "X",
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(255, 120, 120),
                        BackColor = Color.FromArgb(45, 45, 55),
                        Size = new Size(26, 22),
                        Location = new Point(456, rowY),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };
                    ToolTip tipRemove = new ToolTip();
                    tipRemove.SetToolTip(btnRemove, L("Remove from the list", "Retirer de la liste"));
                    btnRemove.Click += (s, e) =>
                    {
                        Options.Instance.OffScreenAutoExcludes.Remove(captured);
                        SaveAndRebuild("[V61-FIX-6] Auto-reload exclusion removed: " + captured.ExeName);
                    };
                    _listPanel.Controls.Add(btnRemove);

                    y += 32;
                }
            }

            if (_listPanel.Controls.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = L("(empty - the AUTO off-screen reload works everywhere)",
                             "(vide - le rechargement AUTO hors écran fonctionne partout)"),
                    ForeColor = Color.FromArgb(120, 120, 130),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                    Location = new Point(10, 8),
                    AutoSize = true
                };
                _listPanel.Controls.Add(lblEmpty);
            }

            _listPanel.ResumeLayout();
        }
    }
}
