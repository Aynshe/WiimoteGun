using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.Controls
{
    /// <summary>
    /// EN: Non-blocking transient toast banner attached to a host control.
    /// Shows briefly at the bottom-center (2.5s), never steals focus, click to dismiss.
    /// Replaces blocking success MessageBoxes in mapping pages.
    /// FR: Bandeau transitoire non bloquant attaché à un contrôle hôte.
    /// S'affiche brièvement en bas au centre (2,5s), ne vole jamais le focus, clic pour fermer.
    /// Remplace les MessageBox de succès bloquantes dans les pages de mapping.
    /// </summary>
    internal class TransientToast : IDisposable
    {
        private readonly Control _host;
        private readonly Label _label;
        private readonly Timer _timer;

        public TransientToast(Control host)
        {
            if (host == null) throw new ArgumentNullException("host");
            _host = host;

            _label = new Label
            {
                Size = new Size(420, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Cursor = Cursors.Hand
            };
            _label.Click += (s, e) => Hide();
            _host.Controls.Add(_label);
            _label.BringToFront();

            _host.Resize += (s, e) => Position();

            _timer = new Timer { Interval = 2500 };
            _timer.Tick += (s, e) => Hide();

            _host.Disposed += (s, e) => Dispose();
        }

        /// <summary>
        /// EN: Show the toast for 2.5s. Does not take focus, the UI stays usable.
        /// FR: Affiche le bandeau pendant 2,5s. Ne prend pas le focus, l'UI reste utilisable.
        /// </summary>
        public void Show(string message, bool isError = false)
        {
            _label.Text = message;
            _label.BackColor = isError ? Color.FromArgb(180, 60, 60) : Color.FromArgb(0, 122, 204);

            // Auto-width to fit the message (EN/FR: Largeur auto pour contenir le message)
            int textWidth = TextRenderer.MeasureText(message, _label.Font).Width;
            _label.Width = Math.Min(Math.Max(260, textWidth + 40), Math.Max(280, _host.ClientSize.Width - 20));

            _label.Visible = true;
            _label.BringToFront();
            Position();

            _timer.Stop();
            _timer.Start();
        }

        public void Hide()
        {
            _timer.Stop();
            _label.Visible = false;
        }

        private void Position()
        {
            if (!_label.Visible) return;
            _label.Location = new Point(
                Math.Max(0, (_host.ClientSize.Width - _label.Width) / 2),
                Math.Max(0, _host.ClientSize.Height - _label.Height - 8));
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            if (!_label.IsDisposed) _label.Dispose();
        }
    }
}
