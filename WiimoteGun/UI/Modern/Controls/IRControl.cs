using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WiimoteLib;
// using WiimoteGun.Forms; // Resolved dynamically or via project structure

namespace WiimoteGun.Controls
{
    public partial class IRControl : UserControl
    {
        private System.Windows.Forms.Timer _irRefreshTimer;
        private int _selectedIRPlayer = 1;

        public IRControl()
        {
            InitializeComponent();
            BindEvents();
            
            // Default selection
            cbIRPlayerSelect.SelectedIndex = 0;

            // Timer
            _irRefreshTimer = new System.Windows.Forms.Timer();
            _irRefreshTimer.Interval = 33; // ~30 FPS
            _irRefreshTimer.Tick += IrRefreshTimer_Tick;
        }

        private void BindEvents()
        {
            // Player Selection
            cbIRPlayerSelect.SelectedIndexChanged += (s, e) =>
            {
                _selectedIRPlayer = cbIRPlayerSelect.SelectedIndex + 1;
                LoadCalibrationValues();
            };

            // Paint Canvas
            pbIRCanvas.Paint += PbIRCanvas_Paint;

            // Save Calibration
            btnSaveCalib.Click += (s, e) => SaveCalibration();

            // Open Gyro Viz
            btnOpenGyroViz.Click += (s, e) =>
            {
                try {
                    // Reflection or direct instantiation if namespace known
                    // For now, preserving original intent but safe
                    // GyroVisualizerForm is likely in Forms namespace
                    var formType = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                        .FirstOrDefault(t => t.Name == "GyroVisualizerForm");
                    
                    if (formType != null)
                    {
                        Form form = (Form)Activator.CreateInstance(formType);
                        form.Show(); // Show as independent window to avoid parent constraints
                    }
                    else 
                    {
                        MessageBox.Show("GyroVisualizerForm not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                } catch (Exception ex) {
                    MessageBox.Show("Error opening visualizer: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Open Fullscreen IR Viz
            btnOpenFullScreenIR.Click += (s, e) => OpenFullscreenIR();

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler BackRequested;

        public void LoadData()
        {
            LoadCalibrationValues();
            _irRefreshTimer.Start();
            
            // Auto-open Fullscreen IR Visualizer (EN/FR: Ouverture auto IR Visualizer plein écran)
            OpenFullscreenIR();
        }
        
        public void UnloadData()
        {
            _irRefreshTimer.Stop();
        }

        private void OpenFullscreenIR()
        {
            try 
            {
                // Check if already open
                var existingForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == "IRVisualizerForm");
                if (existingForm != null)
                {
                    existingForm.BringToFront();
                    existingForm.Activate();
                    return;
                }

                // Find type and instantiate
                var formType = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == "IRVisualizerForm");
                
                if (formType != null)
                {
                    Form form = (Form)Activator.CreateInstance(formType);
                    form.Show(); // Independent window
                }
                else 
                {
                     MessageBox.Show("IRVisualizerForm not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
             } 
             catch (Exception ex) 
             { 
                MessageBox.Show($"Error opening IR Visualizer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
             }
        }

        private void LoadCalibrationValues()
        {
            int offsetX = Options.Instance.GetDynamicPerspectiveOffsetX(_selectedIRPlayer);
            int offsetY = Options.Instance.GetDynamicPerspectiveOffsetY(_selectedIRPlayer);
            nudIROffsetX.Value = offsetX;
            nudIROffsetY.Value = offsetY;
        }

        private void SaveCalibration()
        {
            Options.Instance.SetDynamicPerspectiveOffsetX(_selectedIRPlayer, (int)nudIROffsetX.Value);
            Options.Instance.SetDynamicPerspectiveOffsetY(_selectedIRPlayer, (int)nudIROffsetY.Value);
            Options.Instance.Save();
            MessageBox.Show($"✓ P{_selectedIRPlayer} Offset Saved", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void IrRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible) return;
            
            pbIRCanvas.Invalidate();
            
            if (Program.WiiMoteManager != null)
            {
                var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedIRPlayer);
                if (controller != null && controller.Wiimote != null && controller.Wiimote.WiimoteState != null)
                {
                    var buttons = controller.Wiimote.WiimoteState.Buttons;
                    // Support HOME (BT) or MINUS (Mayflash)
                    if (buttons.Home || buttons.Minus)
                    {
                        bool offsetChanged = false;
                        if (buttons.Left) { nudIROffsetX.Value = Math.Max(nudIROffsetX.Minimum, nudIROffsetX.Value - 1); offsetChanged = true; }
                        else if (buttons.Right) { nudIROffsetX.Value = Math.Min(nudIROffsetX.Maximum, nudIROffsetX.Value + 1); offsetChanged = true; }
                        
                        if (buttons.Up) { nudIROffsetY.Value = Math.Max(nudIROffsetY.Minimum, nudIROffsetY.Value - 1); offsetChanged = true; }
                        else if (buttons.Down) { nudIROffsetY.Value = Math.Min(nudIROffsetY.Maximum, nudIROffsetY.Value + 1); offsetChanged = true; }
                        
                        if (offsetChanged)
                        {
                            Options.Instance.SetDynamicPerspectiveOffsetX(_selectedIRPlayer, (int)nudIROffsetX.Value);
                            Options.Instance.SetDynamicPerspectiveOffsetY(_selectedIRPlayer, (int)nudIROffsetY.Value);
                            // Auto-save on live adjust? Maybe better not to spam save, just updates memory
                        }
                    }
                }
            }
        }

        private void PbIRCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            int midX = pbIRCanvas.Width / 2;
            int midY = pbIRCanvas.Height / 2;
            
            g.DrawLine(Pens.DarkGray, midX, 0, midX, pbIRCanvas.Height);
            g.DrawLine(Pens.DarkGray, 0, midY, pbIRCanvas.Width, midY);
            
            if (Program.WiiMoteManager == null) return;
            
            var controller = Program.WiiMoteManager.Controllers.FirstOrDefault(c => c.PlayerIndex == _selectedIRPlayer);
            
            if (controller != null)
            {
                var irState = controller.Wiimote.WiimoteState.IRState;
                float scaleX = (float)pbIRCanvas.Width / 1024f;
                float scaleY = (float)pbIRCanvas.Height / 768f;
                
                for (int i = 0; i < 4; i++)
                {
                    if (irState[i].Found)
                    {
                        float x = irState[i].RawPosition.X * scaleX;
                        float y = irState[i].RawPosition.Y * scaleY;
                        x = pbIRCanvas.Width - x; // Mirror
                        
                        int size = irState[i].Size + 5;
                        g.FillEllipse(Brushes.White, x - size/2, y - size/2, size, size);
                        g.DrawString((i+1).ToString(), this.Font, Brushes.Yellow, x + 5, y + 5);
                    }
                }
            }
            else
            {
                string msg = $"Player {_selectedIRPlayer} not connected";
                SizeF size = g.MeasureString(msg, this.Font);
                g.DrawString(msg, this.Font, Brushes.Red, midX - size.Width/2, midY - size.Height/2);
            }
        }
    }
}
