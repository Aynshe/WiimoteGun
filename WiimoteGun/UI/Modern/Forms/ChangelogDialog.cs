using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// [V60-FIX-5c] EN: Shows the changelog after an app update. Same template as
    ///     WelcomeDialog. Reads from the local 'changelog.md' — the [EN] and [FR]
    ///     sections are separated; a FR/EN toggle button switches between them.
    ///     Defaults to FR when Windows is French, EN otherwise.
    ///     FR: Affiche le changelog après une mise à jour. Même template que
    ///     WelcomeDialog. Lit le « changelog.md » local — les sections [EN] et [FR]
    ///     sont séparées ; un bouton FR/EN bascule entre elles. Défaut FR si
    ///     Windows est français, EN sinon.
    /// </summary>
    public partial class ChangelogDialog : Form
    {
        private string _version;
        private bool _showFrench;
        private string[] _mdLines;
        private Button _btnLang;
        private LinkLabel _lnkReleases;

        public ChangelogDialog(string version)
        {
            _version = version;
            InitializeComponent();

            // [V61-FIX-7] EN: DetectUrls MUST stay FALSE (explicit). The RichEdit
            //     auto-URL-detection machinery is the trigger of the SILENT NATIVE
            //     crash that kills the WHOLE APP a few seconds after the dialog opens
            //     (no .NET exception, no log, no crash dump — reproduced on the first
            //     real update 3.1.6.0 -> 3.1.6.1). The WelcomeDialog (same template,
            //     validated) has NO URL machinery at all. A native crash cannot be
            //     caught by the try-catch in Program.cs — prevention only.
            //     FR: DetectUrls doit rester FALSE (explicite). Le mécanisme
            //     d'auto-détection d'URLs du RichEdit est le déclencheur du crash
            //     NATIF SILENCIEUX qui tue TOUTE L'APP quelques secondes après
            //     l'ouverture du dialogue (aucune exception .NET, aucun log, aucun
            //     crash dump — reproduit à la première vraie mise à jour
            //     3.1.6.0 -> 3.1.6.1). Le WelcomeDialog (même template, validé)
            //     n'a AUCUNE machinerie d'URL. Un crash natif n'est pas attrapable
            //     par le try-catch de Program.cs — prévention uniquement.
            richTextBox.DetectUrls = false;

            // [V60-FIX-5c] EN: Default language from the Windows culture (French
            //     -> FR, everything else -> EN).
            //     FR: Langue par défaut selon la culture Windows (français -> FR,
            //     tout le reste -> EN).
            try
            {
                var ci = CultureInfo.CurrentUICulture;
                if (ci != null && (ci.TwoLetterISOLanguageName == "fr" || ci.Name.StartsWith("fr")))
                    _showFrench = true;
            }
            catch { }

            // Load changelog.md
            string changelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "changelog.md");
            try
            {
                if (File.Exists(changelogPath))
                    _mdLines = File.ReadAllLines(changelogPath);
            }
            catch { }

            // FR/EN toggle button
            _btnLang = new Button
            {
                Text = _showFrench ? "EN" : "FR",
                Size = new Size(50, 28),
                Location = new Point(12, 598),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TabIndex = 2,
                Cursor = Cursors.Hand
            };
            _btnLang.Click += (s, e) =>
            {
                _showFrench = !_showFrench;
                _btnLang.Text = _showFrench ? "EN" : "FR";
                PopulateContent();
            };
            this.Controls.Add(_btnLang);

            // [V61-FIX-7] EN/FR: the clickable releases link is a standard WinForms
            //     LinkLabel (bottom bar, next to OK) — NO RichTextBox auto-detected
            //     links (native crash trigger — see DetectUrls above).
            _lnkReleases = new LinkLabel
            {
                Text = "🔗 GitHub Releases",
                LinkColor = Color.White,
                ActiveLinkColor = Color.FromArgb(0, 180, 255),
                VisitedLinkColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(470, 610),
                Size = new Size(170, 22),
                Cursor = Cursors.Hand,
                TabIndex = 3
            };
            _lnkReleases.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/Aynshe/WiimoteGun/releases",
                        UseShellExecute = true
                    });
                }
                catch (Exception linkEx)
                {
                    SimpleLogger.Instance.Warning("[V60] Failed to open changelog link: " + linkEx.Message);
                }
            };
            this.Controls.Add(_lnkReleases);

            WiimoteGun.UI.UiScaler.ApplyForm(this); // UI zoom
            PopulateContent();

            btnOK.FlatAppearance.BorderSize = 0;
            richTextBox.SelectionLength = 0;
            btnOK.Focus();

            this.Paint += (s, e) =>
            {
                using (var font = new Font("Segoe UI", 18F, FontStyle.Bold))
                {
                    var titleSize = e.Graphics.MeasureString("Wiimote4Guns", font);
                    e.Graphics.DrawString("Wiimote4Guns", font, Brushes.White,
                        (this.ClientSize.Width - titleSize.Width) / 2, 15);
                }
                using (var subFont = new Font("Segoe UI", 10F, FontStyle.Bold))
                {
                    string sub = "What's New in v" + _version;
                    var subSize = e.Graphics.MeasureString(sub, subFont);
                    e.Graphics.DrawString(sub, subFont, Brushes.LightGray,
                        (this.ClientSize.Width - subSize.Width) / 2, 45);
                }
            };
        }

        private void PopulateContent()
        {
            richTextBox.Clear();

            // [V60-FIX-5e] EN: Same visual template as the WelcomeDialog —
            //     colored section titles (11pt bold, alternating theme colors),
            //     bold subsection headers (9.5pt), bullet points with •,
            //     separator lines between sections.
            //     FR: Même template visuel que le WelcomeDialog —
            //     titres de section colorés (11pt gras, couleurs thème alternées),
            //     sous-titres en gras (9.5pt), puces avec •,
            //     lignes séparatrices entre les sections.

            // GitHub releases link ALWAYS at the top
            // [V61-FIX-7] EN/FR: plain styled TEXT (blue + underlined) — NOT a native
            //     RichEdit auto-detected link (crash trigger). The clickable link is
            //     the LinkLabel in the bottom bar.
            AppendColoredTitle("🔗 RELEASES / RELEASES\n", Color.FromArgb(0, 150, 255));
            AppendLinkText("https://github.com/Aynshe/WiimoteGun/releases\n\n");
            AppendText("─────────────────────────────────────────────────────────────────────\n\n");

            // Parse the markdown for this version, filtering by [EN] or [FR]
            if (_mdLines != null && _mdLines.Length > 0)
            {
                bool inVersionSection = false;
                bool foundContent = false;
                string currentLang = null;
                int sectionIndex = 0;
                // [V60-FIX-5e] EN: Alternating section colors (same palette as WelcomeDialog)
                //     FR: Couleurs de section alternées (même palette que WelcomeDialog)
                Color[] sectionColors = new Color[]
                {
                    Color.FromArgb(0, 120, 215),   // Blue (app theme)
                    Color.FromArgb(0, 150, 100),   // Green
                    Color.FromArgb(255, 140, 0),    // Orange
                    Color.FromArgb(220, 50, 50),    // Red
                    Color.FromArgb(130, 80, 200),   // Purple
                };

                foreach (string rawLine in _mdLines)
                {
                    string line = rawLine.TrimEnd('\r');

                    // Version section header
                    if (line.StartsWith("## ") && !line.StartsWith("### "))
                    {
                        if (inVersionSection)
                            break; // Next version section
                        if (line.TrimStart('#', ' ').StartsWith("v" + _version, StringComparison.OrdinalIgnoreCase))
                        {
                            inVersionSection = true;
                            continue;
                        }
                    }

                    if (!inVersionSection) continue;

                    // Language section: ### [EN] or ### [FR] — display as a section
                    // title (WITHOUT the language tag) only when it matches the
                    // active language; otherwise skip the whole section's content.
                    //     FR: Section de langue : ### [EN] ou ### [FR] — affichée comme
                    //     titre de section (SANS le tag langue) uniquement si elle
                    //     correspond à la langue active ; sinon tout le contenu de la
                    //     section est ignoré.
                    if (line.StartsWith("### ["))
                    {
                        if (line.Contains("[EN]")) currentLang = "EN";
                        else if (line.Contains("[FR]")) currentLang = "FR";
                        else currentLang = null;

                        if (currentLang != null && currentLang != (_showFrench ? "FR" : "EN"))
                            continue;

                        // Show the title without the [EN]/[FR] tag
                        string langTitle = line.Substring(4).Trim();
                        langTitle = langTitle.Replace("[EN]", "").Replace("[FR]", "").Trim();
                        if (!string.IsNullOrEmpty(langTitle))
                        {
                            Color color = sectionColors[sectionIndex % sectionColors.Length];
                            sectionIndex++;
                            if (sectionIndex > 1)
                                AppendText("\n");
                            AppendColoredTitle(langTitle + "\n", color);
                            foundContent = true;
                        }
                        continue;
                    }

                    // Skip lines that belong to the other language
                    if (currentLang != null && currentLang != (_showFrench ? "FR" : "EN"))
                        continue;

                    // Parse markdown-like syntax with WelcomeDialog formatting
                    if (line.StartsWith("### "))
                    {
                        // Section title — colored, 11pt bold (like WelcomeDialog)
                        string title = line.Substring(4).Trim();
                        Color color = sectionColors[sectionIndex % sectionColors.Length];
                        sectionIndex++;
                        if (sectionIndex > 1)
                            AppendText("\n"); // spacing between sections
                        AppendColoredTitle(title + "\n", color);
                        foundContent = true;
                    }
                    else if (line.StartsWith("- "))
                    {
                        // Bullet — 9.5pt with • prefix (like WelcomeDialog)
                        AppendText("• " + line.Substring(2) + "\n");
                        foundContent = true;
                    }
                    else if (line.Trim() == "---")
                    {
                        // Separator (like WelcomeDialog)
                        AppendText("\n─────────────────────────────────────────────────────────────────────\n\n");
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Regular text — 9.5pt (like WelcomeDialog)
                        AppendText(line + "\n");
                        foundContent = true;
                    }
                }

                if (!foundContent)
                {
                    AppendColoredTitle("⚠️ No changelog found for v" + _version + "\n\n", Color.FromArgb(255, 140, 0));
                    AppendText("See the full changelog on GitHub (link above).\n");
                }
            }
            else
            {
                AppendColoredTitle("⚠️ changelog.md not found / changelog.md introuvable\n\n", Color.FromArgb(255, 140, 0));
                AppendText("See the full changelog on GitHub (link above).\n");
            }
        }

        private void AppendColoredTitle(string text, Color color)
        {
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.SelectionColor = color;
            richTextBox.SelectionFont = new Font("Segoe UI", 11F, FontStyle.Bold);
            richTextBox.AppendText(text);
            richTextBox.SelectionColor = richTextBox.ForeColor;
            richTextBox.SelectionFont = richTextBox.Font;
        }

        /// <summary>
        /// EN: [V61-FIX-7] Appends a URL as PLAIN styled text (blue + underlined) —
        ///     the RichTextBox URL auto-detection stays OFF (native crash trigger).
        ///     The clickable link lives in the bottom-bar LinkLabel.
        /// FR: [V61-FIX-7] Ajoute une URL en TEXTE stylisé simple (bleu + souligné) —
        ///     l'auto-détection d'URLs du RichTextBox reste DÉSACTIVÉE (déclencheur du
        ///     crash natif). Le lien cliquable est le LinkLabel de la barre du bas.
        /// </summary>
        private void AppendLinkText(string text)
        {
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.SelectionColor = Color.FromArgb(0, 120, 255);
            richTextBox.SelectionFont = new Font("Segoe UI", 9.5F, FontStyle.Underline);
            richTextBox.AppendText(text);
            richTextBox.SelectionColor = richTextBox.ForeColor;
            richTextBox.SelectionFont = richTextBox.Font;
        }

        private void AppendBoldText(string text)
        {
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.SelectionFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            richTextBox.AppendText(text);
            richTextBox.SelectionFont = richTextBox.Font;
        }

        private void AppendText(string text)
        {
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.AppendText(text);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
