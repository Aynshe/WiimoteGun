using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// EN: [V47+V50] Internal folder explorer - dark themed like Windows Explorer (dark),
    /// with an editable PATH BAR to paste a folder directly, lazy-loaded tree, OK/Cancel.
    /// FR: [V47+V50] Explorateur de dossiers interne - thème sombre calqué sur l'explorateur
    /// Windows (sombre), avec BARRE DE CHEMIN éditable pour coller un dossier directement,
    /// arbre à chargement paresseux, OK/Annuler.
    /// </summary>
    internal partial class FolderBrowserExForm : Form
    {
        private const string DummyNodeTag = "dummy"; // Lazy loading marker (EN/FR: Marqueur de chargement paresseux)

        private FolderBrowserExForm()
        {
            InitializeComponent();
            BuildLogic();
        }

        private void BuildLogic()
        {
            // Buttons (EN/FR: Boutons)
            btnOK.BackColor = Color.FromArgb(0, 122, 204);
            btnCancel.BackColor = Color.FromArgb(70, 70, 70);
            btnGo.BackColor = Color.FromArgb(70, 70, 70);
            btnOK.Enabled = false;

            btnOK.Click += (s, e) =>
            {
                string path = txtPath.Text.Trim();
                if (!string.IsNullOrEmpty(path) && (tvFolders.SelectedNode != null || Directory.Exists(path)))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // [V50] Path bar: paste/edit a full path and jump to it
            // (EN/FR: Barre de chemin : coller/éditer un chemin complet et y aller)
            btnGo.Click += (s, e) => NavigateToPath(txtPath.Text);
            txtPath.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    NavigateToPath(txtPath.Text);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            // Tree behavior (EN/FR: Comportement de l'arbre)
            tvFolders.BeforeExpand += TvFolders_BeforeExpand;
            tvFolders.AfterSelect += (s, e) =>
            {
                txtPath.Text = GetNodePath(e.Node);
                btnOK.Enabled = true;
            };
            tvFolders.NodeMouseDoubleClick += (s, e) =>
            {
                // Double-click: expand or confirm (EN/FR: Double-clic : déplier ou confirmer)
                if (!e.Node.IsExpanded && e.Node.Nodes.Count > 0) e.Node.Expand();
                else if (tvFolders.SelectedNode == e.Node) { DialogResult = DialogResult.OK; Close(); }
            };

            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };

            LoadDrives();
        }

        private void LoadDrives()
        {
            tvFolders.BeginUpdate();
            tvFolders.Nodes.Clear();
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    string label = string.IsNullOrEmpty(drive.VolumeLabel)
                        ? drive.Name
                        : $"{drive.VolumeLabel} ({drive.Name.Replace("\\", "")})";
                    var node = new TreeNode(label) { Tag = drive.Name, ForeColor = Color.FromArgb(240, 240, 240) };
                    AddDummyNode(node);
                    tvFolders.Nodes.Add(node);
                }
            }
            catch { }
            tvFolders.EndUpdate();
        }

        private void AddDummyNode(TreeNode node)
        {
            node.Nodes.Add(new TreeNode("...") { Tag = DummyNodeTag, ForeColor = Color.FromArgb(140, 140, 140) });
        }

        private void TvFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            // Remove the dummy and load the real children (EN/FR: Retirer le dummy et charger les vrais enfants)
            if (node.Nodes.Count == 1 && (node.Nodes[0].Tag as string) == DummyNodeTag)
                node.Nodes.Clear();

            string path = GetNodePath(node);
            try
            {
                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        var attr = File.GetAttributes(dir);
                        if ((attr & FileAttributes.Hidden) != 0 || (attr & FileAttributes.System) != 0) continue;

                        var child = new TreeNode(Path.GetFileName(dir)) { Tag = dir, ForeColor = Color.FromArgb(240, 240, 240) };
                        AddDummyNode(child); // May contain subfolders (EN/FR: Peut contenir des sous-dossiers)
                        node.Nodes.Add(child);
                    }
                    catch { /* Access denied (EN/FR: Accès refusé) */ }
                }
            }
            catch { }
        }

        /// <summary>EN: Full path of a node (Tag holds it). FR: Chemin complet d'un nœud (le Tag le contient).</summary>
        private string GetNodePath(TreeNode node)
        {
            return node?.Tag as string ?? "";
        }

        /// <summary>
        /// EN: [V50] Jump to a path typed/pasted in the path bar: expands the tree
        /// progressively and selects the target node.
        /// FR: [V50] Va vers un chemin saisi/collé dans la barre : déplie l'arbre
        /// progressivement et sélectionne le nœud cible.
        /// </summary>
        private void NavigateToPath(string rawPath)
        {
            try
            {
                string path = (rawPath ?? "").Trim().TrimEnd('\\', '/').TrimEnd(':', ' ');
                if (rawPath != null && rawPath.Trim().EndsWith(":"))
                    path = rawPath.Trim() + "\\"; // Bare drive like "S:" (EN/FR: Lecteur seul comme "S:")

                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    lblTitle.Text = "Path not found: " + (rawPath ?? "");
                    return;
                }

                path = Path.GetFullPath(path);
                lblTitle.Text = "Select a folder";

                // Build the ancestor chain: S:\ , S:\RetroBat , ... (EN/FR: Chaîne d'ancêtres)
                var chain = new System.Collections.Generic.List<string>();
                string cur = path.TrimEnd('\\');
                while (!string.IsNullOrEmpty(cur))
                {
                    chain.Add(cur);
                    string parent = Path.GetDirectoryName(cur);
                    if (string.IsNullOrEmpty(parent) || parent.Equals(cur, StringComparison.OrdinalIgnoreCase)) break;
                    cur = parent.TrimEnd('\\');
                    if (cur.Length == 2 && cur[1] == ':') { chain.Add(cur + "\\"); break; }
                }
                chain.Reverse();

                // Walk the tree, expanding each level (EN/FR: Parcourir l'arbre en dépliant chaque niveau)
                TreeNode node = null;
                tvFolders.BeginUpdate();
                foreach (string level in chain)
                {
                    TreeNode match = FindNodeByTag(node?.Nodes ?? tvFolders.Nodes, level);
                    if (match == null) break;
                    match.Expand(); // Triggers lazy load (EN/FR: Déclenche le chargement paresseux)
                    node = match;
                }
                tvFolders.EndUpdate();

                if (node != null)
                {
                    tvFolders.SelectedNode = node;
                    node.EnsureVisible();
                }
                txtPath.Text = path;
                btnOK.Enabled = true;
            }
            catch (Exception ex)
            {
                lblTitle.Text = "Navigation error: " + ex.Message;
            }
        }

        private static TreeNode FindNodeByTag(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode n in nodes)
            {
                string tag = n.Tag as string;
                if (tag != null && tag.TrimEnd('\\').Equals(path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    return n;
            }
            return null;
        }

        /// <summary>
        /// EN: Show the folder explorer. Returns the selected folder path, or null if cancelled.
        /// FR: Affiche l'explorateur de dossiers. Retourne le chemin sélectionné, ou null si annulé.
        /// </summary>
        public static string Show(IWin32Window owner, string title, string initialPath = null)
        {
            using (var form = new FolderBrowserExForm())
            {
                if (!string.IsNullOrEmpty(title)) form.lblTitle.Text = title;

                // Jump directly to the initial folder via the path bar (EN/FR: Aller directement au dossier initial)
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    try { form.NavigateToPath(initialPath); }
                    catch { form.txtPath.Text = initialPath; form.btnOK.Enabled = true; }
                }

                string typedPath = null;
                form.FormClosed += (s, e) => { typedPath = form.txtPath.Text.Trim(); };

                DialogResult dr = form.ShowDialog(owner);
                if (dr != DialogResult.OK) return null;

                // The path bar takes priority (paste/type) (EN/FR: La barre prime (coller/saisir))
                if (!string.IsNullOrEmpty(typedPath) && Directory.Exists(typedPath))
                {
                    string nodePath = form.GetNodePath(form.tvFolders.SelectedNode);
                    // If the node matches the typed path, both agree (EN/FR: Si le nœud correspond, les deux s'accordent)
                    if (string.IsNullOrEmpty(nodePath) ||
                        nodePath.TrimEnd('\\').Equals(typedPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        return typedPath;
                    return typedPath;
                }

                string nodeP = form.GetNodePath(form.tvFolders.SelectedNode);
                return !string.IsNullOrEmpty(nodeP) ? nodeP : typedPath;
            }
        }
    }
}
