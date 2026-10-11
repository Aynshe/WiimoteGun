using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using WiimoteGun.Common.Win32;

namespace WiimoteGun.UI
{
    class NotifyForm : Form
    {
        // [V56e] Active notifications: tiles STACK downward when several show at the
        // same moment (the update tile goes lower if another one shows simultaneously).
        // (EN/FR: Notifications actives : les tuiles S'EMPILENT vers le bas quand
        // plusieurs s'affichent au même moment - la tuile mise à jour descend plus bas
        // si une autre s'affiche simultanément.)
        private static readonly System.Collections.Generic.List<NotifyForm> _activeForms =
            new System.Collections.Generic.List<NotifyForm>();

        private const int StackGap = 8;

        public NotifyForm() : this(5000)
        {
        }

        public NotifyForm(int durationMs)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            var bounds = Screen.PrimaryScreen.Bounds;

            Opacity = 0;
            BackColor = System.Drawing.Color.FromArgb(16, 16, 48);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            AutoScaleMode = AutoScaleMode.Dpi;
            ShowInTaskbar = false;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = null;

            Width = 350; // Increased from 250 to avoid text truncation (EN/FR: Augmenté pour éviter troncature)
            Height = 80; // Increased from 60 for multi-line support (EN/FR: Augmenté pour multi-lignes)

            // [V56e] Stack below the notifications currently on screen (tile effect)
            // (EN/FR: S'empiler sous les notifications actuellement à l'écran - effet tuile)
            int activeCount;
            lock (_activeForms) { activeCount = _activeForms.Count; }
            int stackOffset = activeCount * (Height + StackGap);

            Location = new System.Drawing.Point(bounds.Right - Width - 16, bounds.Top + 16 + stackOffset);

            StartPosition = FormStartPosition.Manual;

            _timer = new Timer();
            _timer.Interval = durationMs > 0 ? durationMs : 5000; // [V56e] Configurable display time (EN/FR: Durée d'affichage paramétrable)
            _timer.Tick += (a, b) =>
                {
                    Close();
                    Dispose();
                };

            _timer.Start();
        }

        Timer _timer;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // [V58-fix] EN: WS_EX_NOACTIVATE - the notification NEVER steals focus
                //     (the foreground app keeps receiving input). WS_EX_TOOLWINDOW keeps
                //     it out of Alt-Tab. Both together let the tile show OVER fullscreen
                //     apps (MAME etc.) without causing a minimize or desktop flash.
                //     FR: WS_EX_NOACTIVATE - la notification ne vole JAMAIS le focus
                //     (l'app au premier plan continue de recevoir les entrées).
                //     WS_EX_TOOLWINDOW l'exclut d'Alt-Tab. Ensemble, la tuile s'affiche
                //     AU-DESSUS des apps plein écran (MAME etc.) sans les minimiser ni
                //     provoquer de retour bureau.
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // [V56e] Register in the active tile list (stacking reference)
            // (EN/FR: S'enregistrer dans la liste des tuiles actives - référence d'empilement)
            lock (_activeForms)
            {
                if (!_activeForms.Contains(this)) _activeForms.Add(this);
            }

            // [V58-fix] EN: Topmost WITHOUT activation - SWP.NOACTIVATE prevents any
            //     focus change. NO SetForegroundWindow / SetActiveWindow: those were
            //     the root cause of the input blocking and the fullscreen disturbance.
            //     FR: Premier plan SANS activation - SWP.NOACTIVATE empêche tout
            //     changement de focus. PAS de SetForegroundWindow / SetActiveWindow :
            //     c'étaient les causes racines du blocage d'entrées et du dérangement
            //     du plein écran.
            User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP.NOACTIVATE | SWP.NOMOVE | SWP.NOSIZE);

            Opacity = 0.9;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // [V56e] Unregister from the active tile list
            // (EN/FR: Se désenregistrer de la liste des tuiles actives)
            lock (_activeForms)
            {
                _activeForms.Remove(this);
            }
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014) // WM_ERASEBKGND
            {
                using (var g = Graphics.FromHdc(m.WParam))
                    g.Clear(Color.Black);

                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Use larger font for better readability (EN/FR: Police plus grande pour meilleure lisibilité)
            Font largerFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 11, FontStyle.Bold);
            
            // Allow word wrap for long text (EN/FR: Permettre retour ligne pour texte long)
            TextRenderer.DrawText(e.Graphics, _text, 
                largerFont, 
                this.ClientRectangle,
                Color.White, 
                Color.Transparent, 
                TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                
            largerFont.Dispose();
            
            // Draw app name in top right corner (EN/FR: Dessiner nom app coin haut droit)
            using (Font smallFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 8, FontStyle.Regular))
            {
                string appName = "(Wiimote4Guns)";
                Size size = TextRenderer.MeasureText(appName, smallFont);
                Point location = new Point(this.ClientRectangle.Right - size.Width - 5, 2);
                
                TextRenderer.DrawText(e.Graphics, appName, smallFont, location, Color.Gray);
            }
        }

        private string _text;

        public void UpdateState(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateState), new object[] { text });
                return;
            }

            _text = text;
            Invalidate();
        }
    }

}
