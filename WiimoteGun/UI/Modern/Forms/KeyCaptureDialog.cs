using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Dialog for capturing keyboard keys
    /// (EN/FR: Dialogue pour capturer les touches clavier)
    /// </summary>
    /// <summary>
    /// Dialog for capturing keyboard keys
    /// (EN/FR: Dialogue pour capturer les touches clavier)
    /// </summary>
    public partial class KeyCaptureDialog : Form
    {
        private List<Keys> _capturedKeys = new List<Keys>();
        // Controls managed by Designer now
        
        // Colors are now handled in Designer, or can be kept here if dynamic
        private static readonly Color ColorBackground = Color.FromArgb(26, 26, 26);
        private static readonly Color ColorText = Color.FromArgb(224, 224, 224);

        public List<Keys> CapturedKeys { get { return _capturedKeys; } }

        public KeyCaptureDialog()
        {
            InitializeComponent();
            InitializeCustomEvents();
        }

        private void InitializeCustomEvents()
        {
            this.KeyDown += KeyCaptureDialog_KeyDown;
            this.KeyUp += KeyCaptureDialog_KeyUp;
        }

        /// <summary>
        /// Intercept all keys including F1, ESC, etc. before they are processed by the form
        /// (EN/FR: Intercepter toutes les touches y compris F1, ESC, etc. avant traitement par le formulaire)
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Extract the key code (without modifiers)
            Keys keyCode = keyData & Keys.KeyCode;
            
            // Allow Enter to close the dialog (OK)
            if (keyCode == Keys.Enter)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            // Capture the key
            HandleKeyInput(keyData);
            
            // Return true to indicate we processed the key (suppress default behavior like closing on ESC)
            return true;
        }

        // Refactored from KeyDown event to shared method
        private void HandleKeyInput(Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            
            // Don't add modifier keys by themselves
            bool isModifierOnly = (keyCode == Keys.ControlKey || keyCode == Keys.Control || 
                                   keyCode == Keys.ShiftKey || keyCode == Keys.Shift ||
                                   keyCode == Keys.Menu || keyCode == Keys.Alt ||
                                   keyCode == Keys.LWin || keyCode == Keys.RWin);
            
            if (isModifierOnly)
            {
                UpdateDisplay();
                return;
            }
            
            // Add modifiers first if pressed
            if ((keyData & Keys.Control) == Keys.Control && !_capturedKeys.Contains(Keys.Control))
            {
                _capturedKeys.Add(Keys.Control);
            }
            if ((keyData & Keys.Alt) == Keys.Alt && !_capturedKeys.Contains(Keys.Alt))
            {
                _capturedKeys.Add(Keys.Alt);
            }
            if ((keyData & Keys.Shift) == Keys.Shift && !_capturedKeys.Contains(Keys.Shift))
            {
                _capturedKeys.Add(Keys.Shift);
            }
            
            // Add the main key
            if (!_capturedKeys.Contains(keyCode))
            {
                _capturedKeys.Add(keyCode);
                UpdateDisplay();
            }
        }

        private void KeyCaptureDialog_KeyDown(object sender, KeyEventArgs e)
        {
            // Handled in ProcessCmdKey primarily, but keep this to suppress default handling just in case
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void KeyCaptureDialog_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void UpdateDisplay()
        {
            if (_capturedKeys.Count == 0)
            {
                _txtDisplay.Text = "(Waiting for keys...)";
            }
            else
            {
                _txtDisplay.Text = string.Join(" + ", _capturedKeys);
            }
        }
    }
}
