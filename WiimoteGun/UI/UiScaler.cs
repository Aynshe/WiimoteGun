using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI
{
    /// <summary>
    /// [V57n] EN: Global UI zoom engine. One percentage (Options.UiScalePercent, 80..150,
    ///     step 10) drives EVERY window and page of the application. The scale is applied
    ///     ONCE, at CREATION time - each Form calls ApplyForm(this) at the very end of its
    ///     constructor (after InitializeComponent AND any runtime control creation), so
    ///     open windows are never re-scaled live and can never end up in a corrupted layout
    ///     state. The pages (UserControls) hosted inside a form at that moment are covered
    ///     by the form-wide scale; pages created later call ApplyFonts themselves.
    ///     Mechanism, in order:
    ///       1. form.Scale(factor) scales every child's BOUNDS (native WinForms, recursive);
    ///       2. THEN fonts are scaled bottom-up by reference tracking: a control whose Font
    ///          object is the same reference as its parent's inherits the parent's scaled
    ///          font automatically (WinForms propagation), so only EXPLICIT fonts are scaled -
    ///          no child is ever scaled twice. Bounds first, fonts second: AutoSize labels
    ///          then grow to the new font at already-scaled positions;
    ///       3. the form is clamped into the target screen's working area (never off-screen).
    ///     FR: Moteur de zoom UI global. Un pourcentage (Options.UiScalePercent, 80..150,
    ///     pas de 10) pilote TOUTES les fenêtres et pages de l'application. Le scale est
    ///     appliqué UNE FOIS, à la CRÉATION - chaque Form appelle ApplyForm(this) tout en
    ///     fin de constructeur (après InitializeComponent ET la création runtime des
    ///     contrôles), donc aucune fenêtre ouverte n'est re-scalée en live et ne peut se
    ///     retrouver dans un état de mise en page corrompu. Les pages (UserControls)
    ///     hébergées dans le form à ce moment sont couvertes par le scale du form ; les
    ///     pages créées plus tard appellent ApplyFonts elles-mêmes. Mécanisme, dans
    ///     l'ordre :
    ///       1. form.Scale(facteur) scale les BORNES de chaque enfant (WinForms natif,
    ///          récursif) ;
    ///       2. PUIS les polices sont scalées avec suivi par référence : un contrôle dont
    ///          l'objet Font est la même référence que celle de son parent hérite
    ///          automatiquement la police scalée du parent (propagation WinForms), donc
    ///          seules les polices EXPLICITES sont scalées - aucun enfant n'est jamais
    ///          scalé deux fois. Bornes d'abord, polices ensuite : les labels AutoSize
    ///          grossissent alors selon la nouvelle police à des positions déjà scalées ;
    ///       3. le form est borné dans la zone de travail de l'écran cible (jamais hors
    ///          écran).
    /// </summary>
    public static class UiScaler
    {
        public const int MinPercent = 80;
        public const int MaxPercent = 150;
        public const int StepPercent = 10;

        /// <summary>EN: Current zoom factor (1.0 = 100%). FR: Facteur de zoom courant (1.0 = 100 %).</summary>
        public static float Factor
        {
            get
            {
                int p = Options.Instance.UiScalePercent;
                if (p < MinPercent) p = MinPercent;
                if (p > MaxPercent) p = MaxPercent;
                return p / 100f;
            }
        }

        /// <summary>EN: True when a zoom other than 100% is active. FR: Vrai si un zoom ≠ 100 % est actif.</summary>
        public static bool IsZoomed
        {
            get { return Math.Abs(Factor - 1f) > 0.001f; }
        }

        // EN: Idempotency registry: every Font object that has been scaled (or that was
        //     produced by a scale) is tracked here, so ApplyFonts can be RE-CALLED on a
        //     page after it re-populated its runtime rows (LoadData) without ever
        //     double-scaling a font that was already processed.
        //     FR: Registre d'idempotence : tout objet Font déjà scalé (ou produit par un
        //     scale) y est suivi, ce qui permet de RE-APPELER ApplyFonts sur une page
        //     après qu'elle a repeuplé ses lignes runtime (LoadData) sans jamais doubler
        //     une police déjà traitée.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Font, object> _scaledFonts =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Font, object>();

        private static bool IsScaled(Font f)
        {
            object dummy;
            return _scaledFonts.TryGetValue(f, out dummy);
        }

        private static void MarkScaled(Font original, Font scaled)
        {
            try
            {
                if (original != null) _scaledFonts.Add(original, null);
                if (scaled != null && !ReferenceEquals(scaled, original)) _scaledFonts.Add(scaled, null);
            }
            catch { }
        }

        private static Font ScaleFont(Font f, float factor)
        {
            if (f == null || IsScaled(f)) return f;
            try
            {
                Font scaled = new Font(f.FontFamily, f.SizeInPoints * factor, f.Style);
                MarkScaled(f, scaled);
                return scaled;
            }
            catch
            {
                return f;
            }
        }

        /// <summary>
        /// EN: Scale a hardcoded pixel metric by the current zoom factor (tile sizes,
        ///     drawing metrics, paddings...). FR: Multiplie une métrique codée en dur par le
        ///     facteur de zoom courant (tailles de tuiles, métriques de dessin, paddings...).
        /// </summary>
        public static int S(int pixels)
        {
            return (int)Math.Round(pixels * Factor);
        }

        /// <summary>
        /// EN: Apply the global zoom to a form AT CREATION. Call at the very end of the
        ///     constructor, after InitializeComponent and after any runtime-created child
        ///     (so the whole control tree is scaled). No-op at 100%.
        ///     FR: Applique le zoom global à un form À LA CRÉATION. Appeler tout en fin de
        ///     constructeur, après InitializeComponent et après tout enfant créé en runtime
        ///     (pour que l'arbre de contrôles complet soit scalé). No-op à 100 %.
        /// </summary>
        public static void ApplyForm(Form form)
        {
            if (form == null || form.IsDisposed) return;
            float f = Factor;
            if (Math.Abs(f - 1f) < 0.001f) return;

            try
            {
                Size before = form.Size;
                form.SuspendLayout();
                foreach (Control c in form.Controls)
                {
                    try { c.SuspendLayout(); }
                    catch { }
                }

                // 1. Bounds first (native recursive scale of the whole tree)
                //    (EN/FR: Les bornes d'abord - scale récursif natif de tout l'arbre)
                form.Scale(new SizeF(f, f));

                if (form.MinimumSize != Size.Empty)
                    form.MinimumSize = new Size((int)Math.Round(form.MinimumSize.Width * f), (int)Math.Round(form.MinimumSize.Height * f));
                if (form.MaximumSize != Size.Empty)
                    form.MaximumSize = new Size((int)Math.Round(form.MaximumSize.Width * f), (int)Math.Round(form.MaximumSize.Height * f));

                // 2. Fonts second - reference-tracked so inherited fonts follow the parent
                //    and explicit fonts are scaled exactly once.
                //    (EN/FR: Les polices ensuite - suivi par référence : les polices héritées
                //    suivent le parent, les polices explicites sont scalées exactement une fois.)
                ApplyFonts(form, f);

                // 3. Clamp into the target screen working area (never off-screen)
                //    (EN/FR: Bornage dans la zone de travail de l'écran cible - jamais hors écran)
                ClampToScreen(form);

                // [V57o3] EN/FR: Diagnostics - one DEBUG line per scaled window so a single
                //     user test pinpoints any divergent step (zoom, sizes, paths).
                SimpleLogger.Instance.Debug(string.Format(
                    "[V57n] ApplyForm({0}): zoom={1}% f={2} size {3}x{4} -> {5}x{6}",
                    form.Name, Options.Instance.UiScalePercent, f.ToString("0.00"),
                    before.Width, before.Height, form.Width, form.Height));
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning("[V57n] UiScaler.ApplyForm failed on " + form.Name + ": " + ex.Message);
            }
            finally
            {
                foreach (Control c in form.Controls)
                {
                    try { c.ResumeLayout(false); }
                    catch { }
                }
                form.ResumeLayout(false);
                form.PerformLayout();
            }
        }

        /// <summary>
        /// EN: Scale fonts of a control tree without touching bounds - for pages/user
        ///     controls created AFTER their host was scaled (they size themselves via
        ///     docking, only their inner fonts/layout need the zoom). No-op at 100%.
        ///     FR: Scale les polices d'un arbre de contrôles sans toucher aux bornes - pour
        ///     les pages/user controls créés APRÈS le scale de leur hôte (elles se
        ///     dimensionnent par docking, seules leurs polices/mise en page internes ont
        ///     besoin du zoom). No-op à 100 %.
        /// </summary>
        public static void ApplyFonts(Control root)
        {
            ApplyFonts(root, Factor);
        }

        private static void ApplyFonts(Control root, float f)
        {
            if (root == null || Math.Abs(f - 1f) < 0.001f) return;
            try
            {
                root.Font = ScaleFont(root.Font, f);
            }
            catch { }
            ScaleFontsWalk(root, f);
        }

        // EN: A child whose Font is the SAME reference as its parent's is inherited: the
        //     parent's scaled font propagates to it automatically, scaling it again would
        //     double it. Only explicit fonts are scaled here - and the idempotency
        //     registry skips anything already processed.
        //     FR: Un enfant dont la Font est la MÊME référence que celle de son parent est
        //     héritée : la police scalée du parent lui est propagée automatiquement, la
        //     scaler à nouveau la doublerait. Seules les polices explicites sont scalées
        //     ici - et le registre d'idempotence saute tout ce qui a déjà été traité.
        private static void ScaleFontsWalk(Control parent, float f)
        {
            foreach (Control c in parent.Controls)
            {
                try
                {
                    if (c.Font != null && !ReferenceEquals(c.Font, parent.Font) && !IsScaled(c.Font))
                        c.Font = ScaleFont(c.Font, f);
                }
                catch { }
                ScaleFontsWalk(c, f);
            }
        }

        /// <summary>
        /// EN: Scale the direct children of a runtime-populated container AFTER a
        ///     LoadData rebuild (the pages' rows are created with unscaled geometry).
        ///     Safe to call right after each rebuild because the rows are cleared and
        ///     recreated fresh - never call it twice on the same generation of rows.
        ///     No-op at 100%.
        ///     FR: Scale les enfants directs d'un conteneur repeuplé à l'exécution APRÈS un
        ///     rebuild de LoadData (les lignes des pages sont créées avec une géométrie non
        ///     scalée). Appelable sans danger juste après chaque rebuild car les lignes
        ///     sont vidées puis recréées fraîches - ne jamais l'appeler deux fois sur la
        ///     même génération de lignes. No-op à 100 %.
        /// </summary>
        public static void ScaleChildren(ContainerControl container)
        {
            ScaleChildren((Control)container);
        }

        public static void ScaleChildren(Panel container)
        {
            ScaleChildren((Control)container);
        }

        public static void ScaleChildren(Control container)
        {
            if (container == null || container.IsDisposed) return;
            float f = Factor;
            if (Math.Abs(f - 1f) < 0.001f) return;

            try
            {
                container.SuspendLayout();
                foreach (Control c in container.Controls)
                {
                    try { c.Scale(new SizeF(f, f)); }
                    catch { }
                }
            }
            finally
            {
                container.ResumeLayout(false);
                container.PerformLayout();
            }
        }

        // EN: Keep a window fully inside the target screen's working area: cap oversized
        //     windows and re-anchor off-screen positions.
        //     FR: Garde une fenêtre entièrement dans la zone de travail de l'écran cible :
        //     borne les fenêtres trop grandes et ré-ancre les positions hors écran.
        private static void ClampToScreen(Form form)
        {
            try
            {
                Screen screen = Screen.FromControl(form);
                Rectangle wa = screen.WorkingArea;

                if (form.Width > wa.Width || form.Height > wa.Height)
                    form.Size = new Size(Math.Min(form.Width, wa.Width), Math.Min(form.Height, wa.Height));

                int x = Math.Max(wa.Left, Math.Min(form.Left, wa.Right - form.Width));
                int y = Math.Max(wa.Top, Math.Min(form.Top, wa.Bottom - form.Height));
                form.Location = new Point(x, y);
            }
            catch { }
        }
    }
}
