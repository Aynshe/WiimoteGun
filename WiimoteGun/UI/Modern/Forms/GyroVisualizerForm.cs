using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WiimoteLib;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// Popup window for 3D Gyroscope visualization and calibration
    /// (EN/FR: Fenêtre popup pour visualisation 3D gyroscope et calibration)
    /// </summary>
    /// <summary>
    /// Popup window for 3D Gyroscope visualization and calibration
    /// (EN/FR: Fenêtre popup pour visualisation 3D gyroscope et calibration)
    /// </summary>
    public partial class GyroVisualizerForm : Form
    {
        private int _selectedPlayer = 1;

        // Calibration offsets (EN/FR: Offsets de calibration)
        private float _pitchOffset = 0f;
        private float _rollOffset = 0f;
        private float _yawOffset = 0f;

        // Smoothing queues (EN/FR: Files d'attente pour lissage)
        private System.Collections.Generic.Queue<float> _pitchHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _rollHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _yawHistory = new System.Collections.Generic.Queue<float>();
        private const int SMOOTHING_FRAMES = 5;

        // Nunchuk Smoothing queues (EN/FR: Files d'attente pour lissage Nunchuk)
        private System.Collections.Generic.Queue<float> _nunPitchHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _nunRollHistory = new System.Collections.Generic.Queue<float>();

        public GyroVisualizerForm()
        {
            InitializeComponent();
            
            // Attach Timer Event manually (not in designer for this)
            refreshTimer.Tick += RefreshTimer_Tick;
            
            // Initial Selection
            cbPlayerSelect.SelectedIndex = 0;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            pbGyroCanvas.Invalidate();
            UpdateGyroLabels();
        }

        private void CbPlayerSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedPlayer = cbPlayerSelect.SelectedIndex + 1;
            LoadCalibrationForSelectedPlayer();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadCalibrationForSelectedPlayer();
            refreshTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            refreshTimer.Stop();
        }

        private void UpdateGyroLabels()
        {
            if (Program.WiiMoteManager == null) return;

            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            if (controller == null || controller.Wiimote == null || controller.Wiimote.WiimoteState == null)
            {
                lblGyroYaw.Text = "Yaw: --";
                lblGyroPitch.Text = "Pitch: --";
                lblGyroRoll.Text = "Roll: --";
                lblGyroMode.Text = "Mode: Disconnected";
                return;
            }

            var state = controller.Wiimote.WiimoteState;

            // Check if MotionPlus is available (EN/FR: Vérifier si MotionPlus disponible)
            bool hasMotionPlus = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlus ||
                                  state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);
            bool hasNunchuk = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.Nunchuk ||
                               state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);

            float rawPitch = 0, rawYaw = 0, rawRoll = 0;

            if (hasMotionPlus && state.MotionPlus.IsDetected)
            {
                // Use MotionPlus gyroscope data (EN/FR: Utiliser données gyroscope MotionPlus)
                rawYaw = state.MotionPlus.Values.Yaw;
                rawPitch = state.MotionPlus.Values.Pitch;
                rawRoll = state.MotionPlus.Values.Roll;
                
                lblGyroMode.Text = "Mode: MotionPlus\n(Precise Gyro)";
                lblGyroMode.ForeColor = Color.LightGreen;
            }
            else
            {
                // Use Accelerometer fallback (EN/FR: Utiliser fallback accéléromètre)
                var accel = state.Accel.Values;

                // Calculate tilt angles from gravity vector (EN/FR: Calculer angles inclinaison depuis vecteur gravité)
                rawPitch = (float)(Math.Atan2(accel.Y, accel.Z) * 180.0 / Math.PI);
                rawRoll = (float)(Math.Atan2(accel.X, accel.Z) * 180.0 / Math.PI);
                rawYaw = 0; // Accelerometer cannot detect yaw

                lblGyroMode.Text = "Mode: Accelerometer\n(Fallback Tilt)";
                lblGyroMode.ForeColor = Color.Orange;
            }

            // Nunchuk Data
            if (hasNunchuk)
            {
                var nAccel = state.Nunchuk.Accel.Values;
                float nPitch = (float)(Math.Atan2(nAccel.Y, nAccel.Z) * 180.0 / Math.PI);
                float nRoll = (float)(Math.Atan2(nAccel.X, nAccel.Z) * 180.0 / Math.PI);
                
                SmoothValue(_nunPitchHistory, nPitch);
                SmoothValue(_nunRollHistory, nRoll);
            }

            // Apply smoothing (EN/FR: Appliquer lissage)
            float smoothPitch = SmoothValue(_pitchHistory, rawPitch);
            float smoothRoll = SmoothValue(_rollHistory, rawRoll);
            float smoothYaw = SmoothValue(_yawHistory, rawYaw);

            // Apply calibration offsets (EN/FR: Appliquer offsets calibration)
            float displayPitch = smoothPitch - _pitchOffset;
            float displayRoll = smoothRoll - _rollOffset;
            float displayYaw = smoothYaw - _yawOffset;

            lblGyroYaw.Text = string.Format("Yaw:   {0,7:F2}", displayYaw);
            lblGyroPitch.Text = string.Format("Pitch: {0,7:F2}°", displayPitch);
            lblGyroRoll.Text = string.Format("Roll:  {0,7:F2}°", displayRoll);
        }

        private float SmoothValue(System.Collections.Generic.Queue<float> history, float newValue)
        {
            history.Enqueue(newValue);
            while (history.Count > SMOOTHING_FRAMES) history.Dequeue();
            return history.Average();
        }

        private void PbGyroCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(20, 20, 20));

            int w = pbGyroCanvas.Width;
            int h = pbGyroCanvas.Height;
            
            // Draw axes (EN/FR: Dessiner axes)
            using (Pen axisPen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                g.DrawLine(axisPen, w/2, 0, w/2, h);
                g.DrawLine(axisPen, 0, h/2, w, h/2);
            }

            if (Program.WiiMoteManager == null) return;

            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            if (controller == null || controller.Wiimote == null || controller.Wiimote.WiimoteState == null)
            {
                string msg = "Wiimote not connected";
                g.DrawString(msg, new Font("Segoe UI", 10F), Brushes.Gray, 10, 10);
                return;
            }

            // Use smoothed and calibrated values from history (EN/FR: Utiliser valeurs lissées et calibrées)
            float pitch = (_pitchHistory.Count > 0 ? _pitchHistory.Average() : 0) - _pitchOffset;
            float roll = (_rollHistory.Count > 0 ? _rollHistory.Average() : 0) - _rollOffset;
            float yaw = (_yawHistory.Count > 0 ? _yawHistory.Average() : 0) - _yawOffset;

            // Draw Wiimote (Left side if Nunchuk present, else Center)
            // (EN/FR: Dessiner Wiimote (Gauche si Nunchuk présent, sinon Centre))
            bool hasNunchuk = (controller.Wiimote.WiimoteState.ExtensionType == WiimoteLib.DataTypes.ExtensionType.Nunchuk ||
                               controller.Wiimote.WiimoteState.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);

            int wmX = hasNunchuk ? w / 4 : w / 2;
            int wmY = h / 2;
            
            DrawWireframeCube(g, wmX, wmY, 100, pitch, yaw, roll, "Wiimote", Color.Cyan);

            if (hasNunchuk)
            {
                float nPitch = _nunPitchHistory.Count > 0 ? _nunPitchHistory.Average() : 0;
                float nRoll = _nunRollHistory.Count > 0 ? _nunRollHistory.Average() : 0;
                // Nunchuk has no yaw, assume 0
                
                int nunX = (w / 4) * 3;
                int nunY = h / 2;
                
                DrawWireframeCube(g, nunX, nunY, 80, nPitch, 0, nRoll, "Nunchuk", Color.Orange);
            }
        }

        private void DrawWireframeCube(Graphics g, int cx, int cy, int size, float pitch, float yaw, float roll, string label, Color color)
        {
            // Define cube vertices
            float s = size / 2f;
            float[,] vertices = {
                {-s, -s, -s}, {s, -s, -s}, {s, s, -s}, {-s, s, -s},
                {-s, -s,  s}, {s, -s,  s}, {s, s,  s}, {-s, s,  s}
            };

            // Apply rotations
            float pitchRad = pitch * (float)Math.PI / 180f;
            float yawRad = yaw * (float)Math.PI / 180f;
            float rollRad = roll * (float)Math.PI / 180f;

            float[,] rotated = new float[8, 3];
            for (int i = 0; i < 8; i++)
            {
                float x = vertices[i, 0];
                float y = vertices[i, 1];
                float z = vertices[i, 2];

                // Pitch rotation (X-axis)
                float y1 = y * (float)Math.Cos(pitchRad) - z * (float)Math.Sin(pitchRad);
                float z1 = y * (float)Math.Sin(pitchRad) + z * (float)Math.Cos(pitchRad);

                // Yaw rotation (Y-axis)
                float x2 = x * (float)Math.Cos(yawRad) + z1 * (float)Math.Sin(yawRad);
                float z2 = -x * (float)Math.Sin(yawRad) + z1 * (float)Math.Cos(yawRad);

                // Roll rotation (Z-axis)
                float x3 = x2 * (float)Math.Cos(rollRad) - y1 * (float)Math.Sin(rollRad);
                float y3 = x2 * (float)Math.Sin(rollRad) + y1 * (float)Math.Cos(rollRad);

                rotated[i, 0] = x3;
                rotated[i, 1] = y3;
                rotated[i, 2] = z2;
            }

            // Draw edges
            int[,] edges = {
                {0,1}, {1,2}, {2,3}, {3,0},
                {4,5}, {5,6}, {6,7}, {7,4},
                {0,4}, {1,5}, {2,6}, {3,7}
            };

            using (Pen edgePen = new Pen(color, 2))
            {
                for (int i = 0; i < edges.GetLength(0); i++)
                {
                    int v1 = edges[i, 0];
                    int v2 = edges[i, 1];

                    int x1 = cx + (int)rotated[v1, 0];
                    int y1 = cy - (int)rotated[v1, 1];
                    int x2 = cx + (int)rotated[v2, 0];
                    int y2 = cy - (int)rotated[v2, 1];

                    g.DrawLine(edgePen, x1, y1, x2, y2);
                }
            }

            // Draw front face vertices
            using (Brush dotBrush = new SolidBrush(Color.Yellow))
            {
                for (int i = 4; i < 8; i++)
                {
                    int x = cx + (int)rotated[i, 0];
                    int y = cy - (int)rotated[i, 1];
                    g.FillEllipse(dotBrush, x - 4, y - 4, 8, 8);
                }
            }
            
            // Draw label
            using (Font labelFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                SizeF sizeStr = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, Brushes.White, cx - sizeStr.Width/2, cy + size/2 + 10);
            }
        }

        private void LoadCalibrationForSelectedPlayer()
        {
            _pitchOffset = 0f;
            _rollOffset = 0f;
            _yawOffset = 0f;

            if (Program.WiiMoteManager == null) return;
            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            
            if (controller != null && controller.Wiimote != null)
            {
                var uniqueId = controller.Wiimote.UniqueId;
                var calib = Options.Instance.GetCalibration(uniqueId);
                if (calib != null)
                {
                    _pitchOffset = calib.PitchOffset;
                    _rollOffset = calib.RollOffset;
                    _yawOffset = calib.YawOffset;
                    SimpleLogger.Instance.Info(string.Format("Loaded gyro calibration for P{0} ({1}): P={2:F1}, R={3:F1}", _selectedPlayer, uniqueId, _pitchOffset, _rollOffset));
                }
            }
        }

        private void BtnCalibrateGyro_Click(object sender, EventArgs e)
        {
            if (Program.WiiMoteManager == null) return;

            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            if (controller == null)
            {
                MessageBox.Show(string.Format("Player {0} is not connected.", _selectedPlayer), "Calibration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Capture current smoothed values as offsets (EN/FR: Capturer valeurs lissées comme offsets)
            if (_pitchHistory.Count > 0) _pitchOffset = _pitchHistory.Average();
            if (_rollHistory.Count > 0) _rollOffset = _rollHistory.Average();
            if (_yawHistory.Count > 0) _yawOffset = _yawHistory.Average();

            // Save to options (EN/FR: Sauvegarder dans options)
            if (controller.Wiimote != null)
            {
                string uniqueId = controller.Wiimote.UniqueId;
                Options.Instance.SetCalibration(uniqueId, _pitchOffset, _rollOffset, _yawOffset);
                SimpleLogger.Instance.Info(string.Format("Gyroscope calibration saved for P{0} ({1}): P={2:F1}, R={3:F1}", _selectedPlayer, uniqueId, _pitchOffset, _rollOffset));
            }

            MessageBox.Show(string.Format("Gyroscope calibrated and saved!\n" +
                          "Offsets set to: Pitch {0:F1}°, Roll {1:F1}°\n" +
                          "Saved for device ID: {2}", _pitchOffset, _rollOffset, controller.Wiimote != null ? controller.Wiimote.UniqueId : "unknown"), "Calibration Saved",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
