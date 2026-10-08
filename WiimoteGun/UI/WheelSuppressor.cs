using System;
using System.Windows.Forms;

namespace WiimoteGun.UI
{
    /// <summary>
    /// [V57p] EN: Disables the mouse WHEEL on ComboBox and NumericUpDown controls so
    ///     scrolling a page can never silently change a selection or a value: when the
    ///     wheel rolls over one of these controls, the message is marked Handled and never
    ///     reaches the native window - the selection/value stays untouched. The page
    ///     itself still scrolls with the wheel everywhere else (the AutoScroll panels).
    ///     One exception: while a ComboBox drop-down list is OPEN, the wheel keeps
    ///     working (scrolling the open list).
    ///     FR: Désactive la MOLETTE sur les contrôles ComboBox et NumericUpDown pour que
    ///     le défilement d'une page ne change PLUS JAMAIS une sélection ou une valeur en
    ///     silence : quand la molette passe au-dessus d'un de ces contrôles, le message
    ///     est marqué Handled et n'atteint jamais la fenêtre native - la sélection/la
    ///     valeur reste intacte. La page continue de défiler à la molette partout ailleurs
    ///     (les panneaux AutoScroll). Une exception : pendant qu'une liste déroulante
    ///     ComboBox est OUVERTE, la molette continue de fonctionner (défilement de la
    ///     liste ouverte).
    /// </summary>
    public static class WheelSuppressor
    {
        // EN: Idempotency registry: controls that already have the suppression handler -
        //     runtime rows are cleared and rebuilt at every population, this must never
        //     attach twice to a live control.
        //     FR: Registre d'idempotence : contrôles ayant déjà le handler de suppression -
        //     les lignes runtime sont vidées et reconstruites à chaque population, il ne
        //     faut jamais attacher deux fois à un contrôle vivant.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, object> _suppressed =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Control, object>();

        /// <summary>
        /// EN: Recursively suppress the mouse wheel on every ComboBox and NumericUpDown
        ///     under the given root (Designer content AND runtime rows). Safe to call
        ///     repeatedly - already-suppressed controls are skipped.
        ///     FR: Supprime récursivement la molette sur chaque ComboBox et NumericUpDown
        ///     sous la racine donnée (contenu Designer ET lignes runtime). Appelable sans
        ///     danger de façon répétée - les contrôles déjà traités sont sautés.
        /// </summary>
        public static void ApplyTo(Control root)
        {
            if (root == null || root.IsDisposed) return;
            try
            {
                Walk(root);
            }
            catch
            {
                // EN/FR: A disposed child during enumeration is not an error
            }
        }

        private static void Walk(Control c)
        {
            if (c == null || c.IsDisposed) return;

            if (c is ComboBox || c is NumericUpDown)
            {
                object dummy;
                if (!_suppressed.TryGetValue(c, out dummy))
                {
                    _suppressed.Add(c, null);
                    c.MouseWheel += OnSuppressedWheel;
                }
            }

            foreach (Control child in c.Controls)
            {
                Walk(child);
            }
        }

        private static void OnSuppressedWheel(object sender, MouseEventArgs e)
        {
            try
            {
                // EN: Open drop-down list: keep the wheel working (scrolling the list).
                //     FR: Liste déroulante ouverte : la molette continue de fonctionner
                //     (défilement de la liste).
                ComboBox combo = sender as ComboBox;
                if (combo != null && combo.DroppedDown)
                    return;

                // EN: Mark the message Handled: the native window never receives
                //     WM_MOUSEWHEEL, so the selection/value cannot change silently.
                //     FR: Marque le message Handled : la fenêtre native ne reçoit jamais
                //     WM_MOUSEWHEEL, la sélection/la valeur ne peut plus changer en silence.
                System.Windows.Forms.HandledMouseEventArgs handled = e as System.Windows.Forms.HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }
            catch
            {
                // EN/FR: Never break the UI over a suppression edge case
            }
        }
    }
}
