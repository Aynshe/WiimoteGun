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

        public List<Keys> CapturedKeys => _capturedKeys;

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

        private void KeyCaptureDialog_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            // Only block Enter - used to confirm dialog (EN/FR: Ne bloquer que Enter - utilisé pour confirmer dialogue)
            // ESC can now be captured as a hotkey since we have a Cancel button
            if (e.KeyCode == Keys.Enter)
                return;

            // CRITICAL FIX: Use KeyData to get the actual key pressed (EN/FR: Utiliser KeyData pour vraie touche)
            // KeyData includes modifiers, KeyCode only gives base key
            // This properly handles AZERTY, QWERTZ, and other layouts
            Keys actualKey = e.KeyCode;
            
            // Don't add modifier keys by themselves, they'll be in other keys
            // (EN/FR: Ne pas ajouter modificateurs seuls, ils seront dans autres touches)
            bool isModifierOnly = (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Control || 
                                   e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Shift ||
                                   e.KeyCode == Keys.Menu || e.KeyCode == Keys.Alt ||
                                   e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin);
            
            if (isModifierOnly)
            {
                // Just show it in display but don't add yet
                // (EN/FR: Juste afficher mais ne pas ajouter)
                UpdateDisplay();
                return;
            }
            
            // Add modifiers first if pressed (EN/FR: Ajouter modificateurs d'abord si pressés)
            if (e.Control && !_capturedKeys.Contains(Keys.Control))
            {
                _capturedKeys.Add(Keys.Control);
            }
            if (e.Alt && !_capturedKeys.Contains(Keys.Alt))
            {
                _capturedKeys.Add(Keys.Alt);
            }
            if (e.Shift && !_capturedKeys.Contains(Keys.Shift))
            {
                _capturedKeys.Add(Keys.Shift);
            }
            
            // Add the main key (EN/FR: Ajouter touche principale)
            if (!_capturedKeys.Contains(actualKey))
            {
                _capturedKeys.Add(actualKey);
                UpdateDisplay();
            }
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
