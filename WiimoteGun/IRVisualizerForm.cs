using System;
using System.Drawing;
using System.Windows.Forms;
using WiimoteLib;
using WiimoteLib.DataTypes;
using System.Collections.Generic;

namespace WiimoteGun
{
    public class IRVisualizerForm : Form
    {
        private Timer _updateTimer;
        private Dictionary<string, WiimoteState> _lastStates = new Dictionary<string, WiimoteState>();

        public IRVisualizerForm()
        {
            this.Text = "Wiimote IR Visualizer";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            // Position on correct screen (EN/FR: Positionner sur le bon écran)
            int screenIndex = Options.Instance.MonitorId;
            Screen screen = Screen.PrimaryScreen;
            if (screenIndex >= 0 && screenIndex < Screen.AllScreens.Length)
            {
                screen = Screen.AllScreens[screenIndex];
            }

            this.StartPosition = FormStartPosition.Manual;
            var bounds = screen.Bounds;
            this.Location = new Point(
                bounds.Left + (bounds.Width - this.Width) / 2,
                bounds.Top + (bounds.Height - this.Height) / 2
            );

            this.FormClosing += IRVisualizerForm_FormClosing;
            this.Paint += IRVisualizerForm_Paint;

            _updateTimer = new Timer();
            _updateTimer.Interval = 33; // ~30 FPS
            _updateTimer.Tick += _updateTimer_Tick;
            _updateTimer.Start();
        }

        private void IRVisualizerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _updateTimer.Stop();
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            // Poll current states
            var wiimotes = WiimoteManager.ConnectedWiimotes;
            foreach (var wm in wiimotes)
            {
                if (wm.WiimoteState != null)
                {
                    _lastStates[wm.DevicePath] = wm.WiimoteState;
                }
            }
            this.Invalidate();
        }

        private void IRVisualizerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            var wiimotes = WiimoteManager.ConnectedWiimotes;
            int count = wiimotes.Length;
            if (count == 0)
            {
                g.DrawString("No Wiimotes Connected", SystemFonts.DefaultFont, Brushes.White, 10, 10);
                return;
            }

            int widthPerRemote = this.ClientSize.Width / count;
            int height = this.ClientSize.Height;

            for (int i = 0; i < count; i++)
            {
                var wm = wiimotes[i];
                int offsetX = i * widthPerRemote;
                
                // Draw separator
                if (i > 0)
                    g.DrawLine(Pens.Gray, offsetX, 0, offsetX, height);

                g.DrawString($"Wiimote {i + 1}", SystemFonts.DefaultFont, Brushes.White, offsetX + 10, 10);

                if (_lastStates.ContainsKey(wm.DevicePath))
                {
                    var state = _lastStates[wm.DevicePath];
                    var ir = state.IRState;

                    g.DrawString($"Mode: {ir.Mode}, Sens: {ir.Sensitivity}", SystemFonts.DefaultFont, Brushes.White, offsetX + 10, 30);

                    // Draw IR dots
                    // IR coordinates are 0-1023 for X, 0-767 for Y
                    // Map to current view area
                    
                    for (int p = 0; p < 4; p++)
                    {
                        var point = ir[p];
                        if (point.Found)
                        {
                            float x = offsetX + (point.RawPosition.X / 1023f) * widthPerRemote;
                            float y = (point.RawPosition.Y / 767f) * height;
                            
                            // Invert X because Wiimote camera sees mirrored image? Usually yes.
                            // Let's draw raw first.
                            
                            int size = point.Size + 5;
                            g.FillEllipse(Brushes.Red, x - size/2, y - size/2, size, size);
                            g.DrawString($"P{p}", SystemFonts.DefaultFont, Brushes.Yellow, x, y);
                        }
                    }
                }
            }
        }
    }
}
