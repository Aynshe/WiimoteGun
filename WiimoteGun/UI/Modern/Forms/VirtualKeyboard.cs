using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// Virtual on-screen keyboard for profile naming with QWERTY/AZERTY support
    /// (EN/FR: Clavier virtuel pour nommage de profils avec support QWERTY/AZERTY)
    /// </summary>
    public partial class VirtualKeyboard : Form
    {
        // Events (EN/FR: Événements)
        public event EventHandler<string> TextEntered;
        public event EventHandler KeyboardClosed;

        // Constants (EN/FR: Constantes)
        private const int ZOOM_SCALE = 15; // Pixels to grow on hover
        private const float MAGNETIC_STRENGTH_KEY = 0.4f; // Stronger for small keys
        private const int MAGNETIC_RADIUS_KEY = 60;
        
        // Colors
        private static readonly Color ColorKey = Color.FromArgb(50, 50, 50);
        private static readonly Color ColorKeyHover = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorKeyActive = Color.FromArgb(28, 151, 234);
        internal static readonly Color ColorText = Color.White;

        // State (EN/FR: État)
        private bool _shiftPressed = false;
        private bool _capsLock = false;
        private Button _hoveredButton = null;
        private TextBox _targetTextBox;


        // Dragging state (EN/FR: État glisser-déposer)
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public VirtualKeyboard(TextBox targetTextBox)
        {
            _targetTextBox = targetTextBox;
            InitializeComponent();
            InitializeRuntimeUI();
            ApplyRegion();
        }

        private void InitializeRuntimeUI()
        {
            // Hide Tab Headers at runtime (User wants a Switch button)
            tabControlLayouts.Appearance = TabAppearance.Buttons;
            tabControlLayouts.ItemSize = new Size(0, 1);
            tabControlLayouts.SizeMode = TabSizeMode.Fixed;
            
            // Special Keys are now in Designer
            // AddSpecialKeys(tabPageQwerty);
            // AddSpecialKeys(tabPageAzerty);
            
            // Attach Events to ALL buttons
            foreach (TabPage page in tabControlLayouts.TabPages)
            {
                foreach (Control ctrl in page.Controls)
                {
                    if (ctrl is Button btn) AttachButtonEvents(btn);
                }
            }
        }
        
        // Special Keys Loop removed


        private void AttachButtonEvents(Button btn)
        {
            btn.Click -= Key_Click;
            btn.Click += Key_Click;
            btn.MouseEnter -= Key_MouseEnter;
            btn.MouseEnter += Key_MouseEnter;
            btn.MouseLeave -= Key_MouseLeave;
            btn.MouseLeave += Key_MouseLeave;
        }

        private void ApplyRegion()
        {
            // Rounded corners (EN/FR: Coins arrondis)
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 12;
            Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            this.Region = new Region(path);
        }

        private void StartDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        private void StopDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
            }
        }

        private void VirtualKeyboard_MouseMove(object sender, MouseEventArgs e)
        {
            // Handle Dragging (EN/FR: Gérer glisser)
            if (_isDragging)
            {
                Point currentScreenPos = this.PointToScreen(e.Location);
                this.Location = new Point(
                    currentScreenPos.X - _dragStartPoint.X,
                    currentScreenPos.Y - _dragStartPoint.Y
                );
                return; // Skip magnetic effect while dragging
            }
            
            // Magnetic effect logic - Find closest button in Current Tab
            TabPage currentPage = tabControlLayouts.SelectedTab;
            if (currentPage == null) return;

            Point cursorPos = currentPage.PointToClient(Cursor.Position);

            Button closestButton = null;
            float closestDistance = float.MaxValue;
            Point closestCenter = Point.Empty;

            foreach (Control ctrl in currentPage.Controls)
            {
                if (!(ctrl is Button btn) || !btn.Visible)
                    continue;

                // Get button center relative to TabPage
                Point btnCenter = new Point(
                    btn.Location.X + btn.Width / 2,
                    btn.Location.Y + btn.Height / 2
                );

                // Calculate distance
                float distance = (float)Math.Sqrt(
                    Math.Pow(cursorPos.X - btnCenter.X, 2) +
                    Math.Pow(cursorPos.Y - btnCenter.Y, 2)
                );

                if (distance < MAGNETIC_RADIUS_KEY && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestButton = btn;
                    closestCenter = btnCenter;
                }
            }

            // Apply magnetic attraction
            if (closestButton != null)
            {
                float deltaX = closestCenter.X - cursorPos.X;
                float deltaY = closestCenter.Y - cursorPos.Y;

                int attractedX = cursorPos.X + (int)(deltaX * MAGNETIC_STRENGTH_KEY);
                int attractedY = cursorPos.Y + (int)(deltaY * MAGNETIC_STRENGTH_KEY);

                Point screenPos = currentPage.PointToScreen(new Point(attractedX, attractedY));

                if (Math.Abs(screenPos.X - Cursor.Position.X) > 1 ||
                    Math.Abs(screenPos.Y - Cursor.Position.Y) > 1)
                {
                    Cursor.Position = screenPos;
                }
            }
        }

        private void BtnSwitchLayout_Click(object sender, EventArgs e)
        {
            if (tabControlLayouts.SelectedTab == tabPageQwerty)
            {
                tabControlLayouts.SelectedTab = tabPageAzerty;
                btnSwitchLayout.Text = "QWERTY";
            }
            else
            {
                tabControlLayouts.SelectedTab = tabPageQwerty;
                btnSwitchLayout.Text = "AZERTY";
            }
        }

        // Designer Events
        private void BtnClose_Click(object sender, EventArgs e) => CloseKeyboard();
        
        // ... (Other standard Key events)

        private void Key_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string tag = btn.Tag?.ToString();
            
            if (tag == "SHIFT")
            {
                _shiftPressed = !_shiftPressed;
                // Update BOTH shift buttons visuals if present
                UpdateShiftVisuals();
                return;
            }
            else if (tag == "SPACE")
            {
                InsertText(" ");
                return;
            }
            else if (tag == "BACKSPACE")
            {
                Backspace();
                return;
            }
            else if (tag == "ENTER")
            {
                CloseKeyboard();
                return;
            }

            // Normal key
            string character = btn.Text;
            
            // Fix double ampersand due to WinForms escaping
            if (character == "&&") character = "&";
            
            // Apply shift/caps (EN/FR: Appliquer maj/caps)
            if (!string.IsNullOrEmpty(character) && char.IsLetter(character[0]))
            {
                if (_shiftPressed || _capsLock)
                {
                    character = character.ToUpper();
                    _shiftPressed = false; // One-time shift
                    UpdateShiftVisuals();
                }
            }

            InsertText(character);
        }
        
        private void UpdateShiftVisuals()
        {
            foreach (TabPage page in tabControlLayouts.TabPages)
            {
                foreach (Control ctrl in page.Controls)
                {
                    if (ctrl is Button btn)
                    {
                        // Update Shift Key Color
                        if (btn.Tag?.ToString() == "SHIFT")
                        {
                            btn.BackColor = _shiftPressed ? ColorKeyActive : ColorKey;
                        }
                        
                        // Update Letter Keys Case
                        // Only single letters, avoid Special Keys
                        if (btn.Tag == null && btn.Text.Length == 1 && char.IsLetter(btn.Text[0]))
                        {
                            btn.Text = _shiftPressed ? btn.Text.ToUpper() : btn.Text.ToLower();
                        }
                    }
                }
            }
        }

        private void InsertText(string text)
        {
            if (_targetTextBox != null)
            {


                int selStart = _targetTextBox.SelectionStart;
                _targetTextBox.Text = _targetTextBox.Text.Insert(selStart, text);
                _targetTextBox.SelectionStart = selStart + text.Length;
            }

            TextEntered?.Invoke(this, text);
        }

        private void Backspace()
        {
            if (_targetTextBox != null && _targetTextBox.Text.Length > 0)
            {
                int selStart = _targetTextBox.SelectionStart;
                if (selStart > 0)
                {
                    _targetTextBox.Text = _targetTextBox.Text.Remove(selStart - 1, 1);
                    _targetTextBox.SelectionStart = selStart - 1;
                }
            }
        }

        private void Key_MouseEnter(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // Zoom effect (EN/FR: Effet zoom)
            btn.Size = new Size(btn.Width + ZOOM_SCALE, btn.Height + ZOOM_SCALE);
            btn.Location = new Point(btn.Location.X - ZOOM_SCALE / 2, btn.Location.Y - ZOOM_SCALE / 2);
            // Text Zoom
            btn.Font = new Font(btn.Font.FontFamily, btn.Font.Size + 4, btn.Font.Style);
            
            btn.BackColor = ColorKeyHover;
            btn.BringToFront();

            _hoveredButton = btn;
        }

        private void Key_MouseLeave(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // Reset size (EN/FR: Réinitialiser taille)
            string tag = btn.Tag?.ToString();
            
            if (tag == "SHIFT" && _shiftPressed)
            {
                btn.BackColor = ColorKeyActive;
            }
            else if (tag == "BACKSPACE")
            {
                btn.BackColor = Color.FromArgb(192, 0, 0);
            }
            else if (tag == "ENTER")
            {
                btn.BackColor = Color.FromArgb(0, 150, 0);
            }
            else
            {
                btn.BackColor = ColorKey;
            }

            btn.Size = new Size(btn.Width - ZOOM_SCALE, btn.Height - ZOOM_SCALE);
            btn.Location = new Point(btn.Location.X + ZOOM_SCALE / 2, btn.Location.Y + ZOOM_SCALE / 2);
            // Text Reset
            btn.Font = new Font(btn.Font.FontFamily, btn.Font.Size - 4, btn.Font.Style);

            if (_hoveredButton == btn)
                _hoveredButton = null;
        }

        private void CloseKeyboard()
        {
            KeyboardClosed?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}
