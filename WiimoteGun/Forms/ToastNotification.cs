using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Toast notification control for non-intrusive messages
    /// (EN/FR: Contrôle de notification toast pour messages non-intrusifs)
    /// </summary>
    public class ToastNotification : Panel
    {
        private Label _messageLabel;
        private Timer _fadeTimer;
        private Timer _dismissTimer;
        private float _opacity = 1.0f;

        public ToastNotification()
        {
            this.Size = new Size(300, 60);
            this.BackColor = Color.FromArgb(0, 122, 204); // Accent color
            this.Visible = false;
            
            // Message label (EN/FR: Étiquette de message)
            _messageLabel = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 0, 15, 0)
            };
            
            this.Controls.Add(_messageLabel);
            
            // Fade timer (EN/FR: Timer de fondu)
            _fadeTimer = new Timer { Interval = 50 };
            _fadeTimer.Tick += FadeTimer_Tick;
            
            // Dismiss timer (EN/FR: Timer de fermeture)
            _dismissTimer = new Timer { Interval = 2500 }; // 2.5 seconds
            _dismissTimer.Tick += DismissTimer_Tick;
        }

        public void Show(string message, int durationMs = 2500)
        {
            _messageLabel.Text = message;
            _opacity = 1.0f;
            this.Visible = true;
            this.BringToFront();
            
            // Center horizontally at bottom (EN/FR: Centrer horizontalement en bas)
            if (this.Parent != null)
            {
                this.Location = new Point(
                    (this.Parent.Width - this.Width) / 2,
                    this.Parent.Height - this.Height - 20
                );
            }
            
            _dismissTimer.Interval = durationMs;
            _dismissTimer.Start();
        }

        private void DismissTimer_Tick(object sender, EventArgs e)
        {
            _dismissTimer.Stop();
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            _opacity -= 0.1f;
            
            if (_opacity <= 0)
            {
                _fadeTimer.Stop();
                this.Visible = false;
            }
            else
            {
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw with opacity (EN/FR: Dessiner avec opacité)
            using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), this.BackColor)))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
            
            // Draw rounded corners (EN/FR: Dessiner coins arrondis)
            using (Pen borderPen = new Pen(Color.FromArgb((int)(255 * _opacity), Color.White), 1))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                GraphicsPath path = new GraphicsPath();
                int radius = 8;
                Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
                path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
                path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                e.Graphics.DrawPath(borderPen, path);
            }
            
            // Update label opacity (EN/FR: Mettre à jour opacité du label)
            _messageLabel.ForeColor = Color.FromArgb((int)(255 * _opacity), Color.White);
            
            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Dispose();
                _dismissTimer?.Dispose();
                _messageLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
