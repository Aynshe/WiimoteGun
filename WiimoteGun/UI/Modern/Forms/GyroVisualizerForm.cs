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

        // Smoothing queues
        // 1. Wiimote Accelerometer (Tilt)
        private System.Collections.Generic.Queue<float> _accPitchHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _accRollHistory = new System.Collections.Generic.Queue<float>();

        // 2. Wiimote MotionPlus (Gyro)
        private System.Collections.Generic.Queue<float> _mpPitchHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _mpRollHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _mpYawHistory = new System.Collections.Generic.Queue<float>();

        // 3. Nunchuk Accelerometer (Tilt)
        private System.Collections.Generic.Queue<float> _nunPitchHistory = new System.Collections.Generic.Queue<float>();
        private System.Collections.Generic.Queue<float> _nunRollHistory = new System.Collections.Generic.Queue<float>();

        // Calibration offsets
        // MP Gyro Offsets
        private float _mpCalibrationPitch = 0f;
        private float _mpCalibrationRoll = 0f;
        private float _mpCalibrationYaw = 0f;

        // Wiimote Accel Offsets
        private float _accXOffset = 0f;
        private float _accYOffset = 0f;
        private float _accZOffset = 0f;

        // Nunchuk Accel Offsets
        private float _nunAccXOffset = 0f;
        private float _nunAccYOffset = 0f;
        private float _nunAccZOffset = 0f;
        private float _nunStickXOffset = 0f;
        private float _nunStickYOffset = 0f;

        private const int SMOOTHING_FRAMES = 5;

        public GyroVisualizerForm()
        {
            InitializeComponent();
            
            this.TopMost = true; // EN/FR: Toujours au premier plan (Always on top)
            
            // Attach Timer Event manually (not in designer for this)
            refreshTimer.Tick += RefreshTimer_Tick;
            
            // Initial Selection
            cbPlayerSelect.SelectedIndex = 0;

            // EN/FR: Mise à jour des textes d'aide et boutons pour la calibration unifiée
            btnCalibrateGyro.Text = "🔧 Calibrate All Sensors\n(Zero Position)";
            lblHelp.Text = "• Left: Wiimote Accelerometer (Tilt)\n" +
                          "• Center: MotionPlus Gyroscope (Precise)\n" +
                          "• Right: Nunchuk Accelerometer (Tilt)\n" +
                          "Place Wiimote FLAT and facing the screen, then click Calibrate to zero all sensors.";
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            pbGyroCanvas.Invalidate();
            UpdateSensorData();
        }

        private void CbPlayerSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedPlayer = cbPlayerSelect.SelectedIndex + 1;
            LoadCalibrationForSelectedPlayer();
        }

        public void SelectPlayer(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4) return;
            if (cbPlayerSelect.Items.Count >= playerIndex)
            {
                cbPlayerSelect.SelectedIndex = playerIndex - 1;
            }
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

        private void UpdateSensorData()
        {
            if (Program.WiiMoteManager == null) return;

            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            if (controller == null || controller.Wiimote == null || controller.Wiimote.WiimoteState == null)
            {
                return;
            }

            var state = controller.Wiimote.WiimoteState;

            // 1. Process Wiimote Accelerometer (Always Available)
            // ---------------------------------------------------
            var accel = state.Accel.Values;
            float calX = accel.X - _accXOffset;
            float calY = accel.Y - _accYOffset;
            float calZ = accel.Z - _accZOffset;
            // Assuming Z is perpendicular to the plate - adding 1.0 back to Z if we calibrated it to zero
            float zRef = calZ + (Math.Abs(_accZOffset) > 0.5f ? _accZOffset : 0); 

            float accPitch = (float)(Math.Atan2(calY, zRef) * 180.0 / Math.PI);
            float accRoll = -(float)(Math.Atan2(calX, zRef) * 180.0 / Math.PI);
            
            SmoothValue(_accPitchHistory, accPitch);
            SmoothValue(_accRollHistory, accRoll);

            // 2. Process MotionPlus (If Available)
            // ------------------------------------
            bool hasMotionPlus = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlus ||
                                  state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);
            
            if (hasMotionPlus) // [FIX V23] Trust ExtensionType, IsDetected is not reliably set
            {
                float mpYaw = state.MotionPlus.Values.Yaw;
                float mpPitch = state.MotionPlus.Values.Pitch;
                float mpRoll = state.MotionPlus.Values.Roll;

                SmoothValue(_mpYawHistory, mpYaw);
                SmoothValue(_mpPitchHistory, mpPitch);
                SmoothValue(_mpRollHistory, mpRoll);
            }

            // 3. Process Nunchuk (If Available)
            // ---------------------------------
            bool hasNunchuk = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.Nunchuk ||
                               state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);

            if (hasNunchuk)
            {
                var nAccel = state.Nunchuk.Accel.Values;
                float nCalX = nAccel.X - _nunAccXOffset;
                float nCalY = nAccel.Y - _nunAccYOffset;
                float nCalZ = nAccel.Z - _nunAccZOffset;
                float nZRef = nCalZ + (Math.Abs(_nunAccZOffset) > 0.5f ? _nunAccZOffset : 0); 

                float nPitch = (float)(Math.Atan2(nCalY, nZRef) * 180.0 / Math.PI);
                float nRoll = -(float)(Math.Atan2(nCalX, nZRef) * 180.0 / Math.PI);
                
                SmoothValue(_nunPitchHistory, nPitch);
                SmoothValue(_nunRollHistory, nRoll);
            }

            // Update Labels (Showing MP preferentially, or Accel)
            // --------------------------------------------------
            // [V23] Labels removed, values are now drawn on canvas
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
            
            // Draw Viewport dividers
            using (Pen axisPen = new Pen(Color.FromArgb(40, 40, 40), 1))
            {
                g.DrawLine(axisPen, w/3, 0, w/3, h);
                g.DrawLine(axisPen, (w/3)*2, 0, (w/3)*2, h);
            }

            if (Program.WiiMoteManager == null) return;
            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            
            if (controller == null || controller.Wiimote == null) {
                 g.DrawString("Disconnected", new Font("Segoe UI", 12), Brushes.Gray, 10, 10);
                 return;
            }

            var state = controller.Wiimote.WiimoteState;
            bool hasMotionPlus = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlus ||
                                  state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);
            bool hasNunchuk = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.Nunchuk ||
                               state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);

            int sectionW = w / 3;
            int centerY = h / 2;

            // 1. Wiimote Accelerometer (Left)
            // -------------------------------
            float accPitch = _accPitchHistory.Count > 0 ? _accPitchHistory.Average() : 0;
            float accRoll = _accRollHistory.Count > 0 ? _accRollHistory.Average() : 0;
            DrawWireframeCube(g, sectionW / 2, centerY, 100, accPitch, 0, accRoll, "Wiimote Accel", Color.Orange);
            DrawValues(g, sectionW / 2, centerY + 80, 0, accPitch, accRoll, false);

            // 2. Wiimote MotionPlus (Center)
            // ------------------------------
            if (hasMotionPlus) // [FIX V23] Trust ExtensionType
            {
                float mpYaw = (_mpYawHistory.Count > 0 ? _mpYawHistory.Average() : 0) - _mpCalibrationYaw;
                float mpPitch = (_mpPitchHistory.Count > 0 ? _mpPitchHistory.Average() : 0) - _mpCalibrationPitch;
                float mpRoll = (_mpRollHistory.Count > 0 ? _mpRollHistory.Average() : 0) - _mpCalibrationRoll;
                DrawWireframeCube(g, sectionW + (sectionW / 2), centerY, 100, mpPitch, mpYaw, mpRoll, "MotionPlus Gyro", Color.Cyan);
                DrawValues(g, sectionW + (sectionW / 2), centerY + 80, mpYaw, mpPitch, mpRoll, true);
            }
            else
            {
                DrawPlaceholder(g, sectionW + (sectionW / 2), centerY, "MotionPlus\nNot Detected");
            }

            // 3. Nunchuk Accelerometer (Right)
            // --------------------------------
            if (hasNunchuk)
            {
                float nPitch = _nunPitchHistory.Count > 0 ? _nunPitchHistory.Average() : 0;
                float nRoll = _nunRollHistory.Count > 0 ? _nunRollHistory.Average() : 0;
                DrawWireframeCube(g, (sectionW * 2) + (sectionW / 2), centerY, 80, nPitch, 0, nRoll, "Nunchuk Accel", Color.Yellow);
                DrawValues(g, (sectionW * 2) + (sectionW / 2), centerY + 70, 0, nPitch, nRoll, false);
            }
            else
            {
                DrawPlaceholder(g, (sectionW * 2) + (sectionW / 2), centerY, "Nunchuk\nNot Connected");
            }
        }

        private void DrawValues(Graphics g, int cx, int cy, float yaw, float pitch, float roll, bool showYaw)
        {
            using (Font f = new Font("Consolas", 9F))
            using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center })
            {
                string text = "";
                if (showYaw) text += string.Format("Yaw: {0,6:F2}\n", yaw);
                text += string.Format("Pitch: {0,6:F2}\nRoll: {1,6:F2}", pitch, roll);
                
                g.DrawString(text, f, Brushes.White, cx, cy, sf);
            }
        }

        private void DrawPlaceholder(Graphics g, int cx, int cy, string text)
        {
            using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Font f = new Font("Segoe UI", 9F, FontStyle.Italic))
            {
                g.DrawString(text, f, Brushes.DimGray, cx, cy, sf);
                g.DrawRectangle(Pens.DimGray, cx - 40, cy - 40, 80, 80);
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
            _mpCalibrationPitch = 0f;
            _mpCalibrationRoll = 0f;
            _mpCalibrationYaw = 0f;

            if (Program.WiiMoteManager == null) return;
            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            
            if (controller != null && controller.Wiimote != null)
            {
                var uniqueId = controller.Wiimote.UniqueId;
                var calib = Options.Instance.GetCalibration(uniqueId);
                if (calib != null)
                {
                    _mpCalibrationPitch = calib.PitchOffset;
                    _mpCalibrationRoll = calib.RollOffset;
                    _mpCalibrationYaw = calib.YawOffset;
                    
                    _accXOffset = calib.AccXOffset;
                    _accYOffset = calib.AccYOffset;
                    _accZOffset = calib.AccZOffset;
                    
                    _nunAccXOffset = calib.NunAccXOffset;
                    _nunAccYOffset = calib.NunAccYOffset;
                    _nunAccZOffset = calib.NunAccZOffset;
                    _nunStickXOffset = calib.NunStickXOffset;
                    _nunStickYOffset = calib.NunStickYOffset;

                    SimpleLogger.Instance.Info(string.Format("Loaded full sensor calibration for P{0} ({1})", _selectedPlayer, uniqueId));
                }
            }
        }

        private void BtnCalibrateGyro_Click(object sender, EventArgs e)
        {
            if (Program.WiiMoteManager == null) return;

            var controller = Program.WiiMoteManager.GetControllers().FirstOrDefault(c => c.PlayerIndex == _selectedPlayer);
            if (controller == null || controller.Wiimote == null)
            {
                MessageBox.Show(string.Format("Player {0} is not connected.", _selectedPlayer), "Calibration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var state = controller.Wiimote.WiimoteState;

            // 1. Capture Wiimote Accelerometer (Zero-G / Flat)
            // ------------------------------------------------
            // We assume Wiimote is flat on table: X=0, Y=0, Z=1 (approx)
            // We capture current values as offsets to make them (0,0,1) effectively.
            _accXOffset = state.Accel.Values.X;
            _accYOffset = state.Accel.Values.Y;
            _accZOffset = state.Accel.Values.Z;

            // 2. Capture Nunchuk (If connected)
            // ---------------------------------
            bool hasNunchuk = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.Nunchuk || 
                               state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);
            
            if (hasNunchuk)
            {
                _nunAccXOffset = state.Nunchuk.Accel.Values.X;
                _nunAccYOffset = state.Nunchuk.Accel.Values.Y;
                _nunAccZOffset = state.Nunchuk.Accel.Values.Z;
                
                _nunStickXOffset = state.Nunchuk.Joystick.X;
                _nunStickYOffset = state.Nunchuk.Joystick.Y;
            }
            else
            {
                // Do not reset if not connected? Better to keep previous or reset?
                // Let's reset to avoid using bad values if plugged later.
                _nunAccXOffset = 0;
                _nunAccYOffset = 0;
                _nunAccZOffset = 0; // standard is usually around 0,0,1G
                _nunStickXOffset = 0;
                _nunStickYOffset = 0;
            }
            
            // 3. Capture MotionPlus Gyro (If connected)
            // -----------------------------------------
            bool hasMotionPlus = (state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlus ||
                                  state.ExtensionType == WiimoteLib.DataTypes.ExtensionType.MotionPlusNunchuk);

            if (hasMotionPlus) // [FIX V23] Trust ExtensionType
            {
                // MotionPlus "Zero" is just current orientation for Yaw/Pitch/Roll 
                // (Since we accumulate, we just offset the current accumulation to 0)
                if (_mpPitchHistory.Count > 0) _mpCalibrationPitch = _mpPitchHistory.Average();
                if (_mpRollHistory.Count > 0) _mpCalibrationRoll = _mpRollHistory.Average();
                if (_mpYawHistory.Count > 0) _mpCalibrationYaw = _mpYawHistory.Average();
            }
            else
            {
                _mpCalibrationPitch = 0f;
                _mpCalibrationRoll = 0f;
                _mpCalibrationYaw = 0f;
            }

            // 4. Save to persistent options
            // -----------------------------
            string uniqueId = controller.Wiimote.UniqueId;
            Options.Instance.SetCalibration(uniqueId, _mpCalibrationPitch, _mpCalibrationRoll, _mpCalibrationYaw, 
                _accXOffset, _accYOffset, _accZOffset,
                _nunAccXOffset, _nunAccYOffset, _nunAccZOffset,
                _nunStickXOffset, _nunStickYOffset);

            SimpleLogger.Instance.Info(string.Format("Calibrated P{0}: MP={1}, Acc={2:F2}/{3:F2}, Nun={4:F2}/{5:F2}", 
                _selectedPlayer, hasMotionPlus, _accXOffset, _accYOffset, _nunAccXOffset, _nunAccYOffset));

            MessageBox.Show(this, "Calibration Successful!\n\n" +
                            "Offsets updated in Options.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
