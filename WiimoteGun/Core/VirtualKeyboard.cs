using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual on-screen keyboard for profile naming with QWERTY/AZERTY support
    /// (EN/FR: Clavier virtuel pour nommage de profils avec support QWERTY/AZERTY)
    /// </summary>
    public class VirtualKeyboard : Form
    {
        // Keyboard layout enum (EN/FR: Énumération layout clavier)
        public enum KeyboardLayout
        {
            QWERTY,
            AZERTY
        }

        // Events (EN/FR: Événements)
        public event EventHandler<string> TextEntered;
        public event EventHandler KeyboardClosed;

        // Constants (EN/FR: Constantes)
        private const int KEY_SIZE = 45;
        private const int KEY_SPACING = 6;
        private const int ZOOM_SCALE = 15; // Pixels to grow on hover
        private const float MAGNETIC_STRENGTH_KEY = 0.4f; // Stronger for small keys
        private const int MAGNETIC_RADIUS_KEY = 60;

        // Design colors (EN/FR: Couleurs du design)
        private static readonly Color ColorBackground = Color.FromArgb(30, 30, 30);
        private static readonly Color ColorKey = Color.FromArgb(50, 50, 50);
        private static readonly Color ColorKeyHover = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorKeyActive = Color.FromArgb(28, 151, 234);
        private static readonly Color ColorText = Color.White;

        // State (EN/FR: État)
        private KeyboardLayout _currentLayout = KeyboardLayout.QWERTY;
        private bool _shiftPressed = false;
        private bool _capsLock = false;
        private Button _hoveredButton = null;
        private TextBox _targetTextBox;

        // UI Components (EN/FR: Composants UI)
        private Panel _keyboardPanel;
        private Button _btnLayoutSwitch;
        private Button _btnShift;
        private Label _lblTitle;

        // Dragging state (EN/FR: État glisser-déposer)
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public VirtualKeyboard(TextBox targetTextBox)
        {
            _targetTextBox = targetTextBox;
            InitializeKeyboard();
        }

        // ... (InitializeKeyboard is already correct from previous step)

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

            // Magnetic pointer effect on keys (EN/FR: Effet pointer magnétique sur touches)
            Point cursorPos = _keyboardPanel.PointToClient(Cursor.Position);

            Button closestButton = null;
            float closestDistance = float.MaxValue;
            Point closestCenter = Point.Empty;

            foreach (Control ctrl in _keyboardPanel.Controls)
            {
                if (!(ctrl is Button btn) || !btn.Visible)
                    continue;

                // Get button center
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

                Point screenPos = _keyboardPanel.PointToScreen(new Point(attractedX, attractedY));

                if (Math.Abs(screenPos.X - Cursor.Position.X) > 1 ||
                    Math.Abs(screenPos.Y - Cursor.Position.Y) > 1)
                {
                    Cursor.Position = screenPos;
                }
            }
        }

        private void InitializeKeyboard()
        {
            // Form settings (EN/FR: Paramètres du formulaire)
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(700, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorBackground;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.98;

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

            // Title (EN/FR: Titre)
            _lblTitle = new Label
            {
                Text = "Virtual Keyboard",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = ColorText,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            this.Controls.Add(_lblTitle);

            // Enable dragging via Title and Form background
            // (EN/FR: Activer déplacement via Titre et fond Formulaire)
            this.MouseDown += StartDrag;
            _lblTitle.MouseDown += StartDrag;
            this.MouseUp += StopDrag;
            _lblTitle.MouseUp += StopDrag;

            // Close button (EN/FR: Bouton fermer)
            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(35, 35),
                Location = new Point(this.Width - 45, 10),
                BackColor = Color.Transparent,
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => CloseKeyboard();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Transparent;
            this.Controls.Add(btnClose);

            // Keyboard panel (EN/FR: Panel clavier)
            _keyboardPanel = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(660, 280),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_keyboardPanel);

            // Build keyboard (EN/FR: Construire clavier)
            BuildKeyboard();

            // Mouse move for magnetic effect AND dragging
            // (EN/FR: Déplacement souris pour effet magnétique ET glisser)
            this.MouseMove += VirtualKeyboard_MouseMove;
            _lblTitle.MouseMove += VirtualKeyboard_MouseMove; // Allow dragging via title
            _keyboardPanel.MouseMove += VirtualKeyboard_MouseMove;

            // ESC to close (EN/FR: ESC pour fermer)
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    CloseKeyboard();
                
                // Handle Backspace (EN/FR: Gérer Retour arrière)
                if (e.KeyCode == Keys.Back)
                {
                    Backspace();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                
                // Handle Enter (EN/FR: Gérer Entrée)
                if (e.KeyCode == Keys.Enter)
                {
                    CloseKeyboard();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };


            // Relay physical keyboard input (EN/FR: Relayer entrée clavier physique)
            this.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar))
                {
                    InsertText(e.KeyChar.ToString());
                    e.Handled = true;
                }
            };
        }

        private void BuildKeyboard()
        {
            _keyboardPanel.Controls.Clear();

            // QWERTY/AZERTY layouts (EN/FR: Layouts QWERTY/AZERTY)
            string[][] rows;
            if (_currentLayout == KeyboardLayout.QWERTY)
            {
                rows = new string[][]
                {
                    new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
                    new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]" },
                    new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", ";", "'" },
                    new[] { "z", "x", "c", "v", "b", "n", "m", ",", ".", "/" }
                };
            }
            else // AZERTY
            {
                rows = new string[][]
                {
                    new[] { "&", "é", "\"", "'", "(", "-", "è", "_", "ç", "à", ")", "=" },
                    new[] { "a", "z", "e", "r", "t", "y", "u", "i", "o", "p", "^", "$" },
                    new[] { "q", "s", "d", "f", "g", "h", "j", "k", "l", "m", "ù", "*" },
                    new[] { "w", "x", "c", "v", "b", "n", ",", ";", ":", "!" }
                };
            }

            int startX = 10;
            int yPos = 10;

            // Create rows (EN/FR: Créer rangées)
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                int xPos = startX + (rowIndex * 25); // Offset for keyboard shape
                foreach (string key in rows[rowIndex])
                {
                    Button btnKey = CreateKey(key, xPos, yPos);
                    _keyboardPanel.Controls.Add(btnKey);
                    xPos += KEY_SIZE + KEY_SPACING;
                }
                yPos += KEY_SIZE + KEY_SPACING;
            }

            // Special keys row (EN/FR: Rangée touches spéciales)
            yPos += 5;

            // Shift key (EN/FR: Touche Maj)
            _btnShift = new Button
            {
                Text = "⇧ Shift",
                Size = new Size(80, KEY_SIZE),
                Location = new Point(startX, yPos),
                BackColor = _shiftPressed ? ColorKeyActive : ColorKey,
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Tag = "SHIFT"
            };
            _btnShift.FlatAppearance.BorderSize = 0;
            _btnShift.Click += (s, e) =>
            {
                _shiftPressed = !_shiftPressed;
                _btnShift.BackColor = _shiftPressed ? ColorKeyActive : ColorKey;
            };
            _btnShift.MouseEnter += Key_MouseEnter;
            _btnShift.MouseLeave += Key_MouseLeave;
            _keyboardPanel.Controls.Add(_btnShift);

            // Space bar (EN/FR: Barre espace)
            Button btnSpace = new Button
            {
                Text = "Space",
                Size = new Size(250, KEY_SIZE),
                Location = new Point(startX + 90, yPos),
                BackColor = ColorKey,
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Tag = "SPACE"
            };
            btnSpace.FlatAppearance.BorderSize = 0;
            btnSpace.Click += (s, e) => InsertText(" ");
            btnSpace.MouseEnter += Key_MouseEnter;
            btnSpace.MouseLeave += Key_MouseLeave;
            _keyboardPanel.Controls.Add(btnSpace);

            // Backspace (EN/FR: Retour arrière)
            Button btnBackspace = new Button
            {
                Text = "⌫",
                Size = new Size(80, KEY_SIZE),
                Location = new Point(startX + 350, yPos),
                BackColor = Color.FromArgb(192, 0, 0),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F),
                Tag = "BACKSPACE"
            };
            btnBackspace.FlatAppearance.BorderSize = 0;
            btnBackspace.Click += (s, e) => Backspace();
            btnBackspace.MouseEnter += Key_MouseEnter;
            btnBackspace.MouseLeave += Key_MouseLeave;
            _keyboardPanel.Controls.Add(btnBackspace);

            // Enter/OK (EN/FR: Entrée/OK)
            Button btnEnter = new Button
            {
                Text = "✓ OK",
                Size = new Size(100, KEY_SIZE),
                Location = new Point(startX + 440, yPos),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Tag = "ENTER"
            };
            btnEnter.FlatAppearance.BorderSize = 0;
            btnEnter.Click += (s, e) => CloseKeyboard();
            btnEnter.MouseEnter += Key_MouseEnter;
            btnEnter.MouseLeave += Key_MouseLeave;
            _keyboardPanel.Controls.Add(btnEnter);

            // Layout switch button (EN/FR: Bouton changement layout)
            _btnLayoutSwitch = new Button
            {
                Text = _currentLayout == KeyboardLayout.QWERTY ? "AZERTY" : "QWERTY",
                Size = new Size(90, KEY_SIZE),
                Location = new Point(startX + 550, yPos),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Tag = "LAYOUT"
            };
            _btnLayoutSwitch.FlatAppearance.BorderSize = 0;
            _btnLayoutSwitch.Click += (s, e) =>
            {
                _currentLayout = _currentLayout == KeyboardLayout.QWERTY ? KeyboardLayout.AZERTY : KeyboardLayout.QWERTY;
                BuildKeyboard();
            };
            _btnLayoutSwitch.MouseEnter += Key_MouseEnter;
            _btnLayoutSwitch.MouseLeave += Key_MouseLeave;
            _keyboardPanel.Controls.Add(_btnLayoutSwitch);
        }

        private Button CreateKey(string character, int x, int y)
        {
            Button btn = new Button
            {
                Text = character,
                Size = new Size(KEY_SIZE, KEY_SIZE),
                Location = new Point(x, y),
                BackColor = ColorKey,
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Tag = character
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += Key_Click;
            btn.MouseEnter += Key_MouseEnter;
            btn.MouseLeave += Key_MouseLeave;
            return btn;
        }

        private void Key_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string character = btn.Tag.ToString();
            
            // Apply shift/caps (EN/FR: Appliquer maj/caps)
            if (char.IsLetter(character[0]))
            {
                if (_shiftPressed || _capsLock)
                {
                    character = character.ToUpper();
                    _shiftPressed = false; // One-time shift
                    if (_btnShift != null)
                        _btnShift.BackColor = ColorKey;
                }
            }

            InsertText(character);
        }

        private void InsertText(string text)
        {
            if (_targetTextBox != null)
            {
                // Auto-capitalize first letter (EN/FR: Première lettre auto-majuscule)
                if (string.IsNullOrEmpty(_targetTextBox.Text) && char.IsLetter(text[0]))
                {
                    text = text.ToUpper();
                }

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
            else if (tag == "LAYOUT")
            {
                btn.BackColor = Color.FromArgb(100, 100, 100);
            }
            else
            {
                btn.BackColor = ColorKey;
            }

            btn.Size = new Size(btn.Width - ZOOM_SCALE, btn.Height - ZOOM_SCALE);
            btn.Location = new Point(btn.Location.X + ZOOM_SCALE / 2, btn.Location.Y + ZOOM_SCALE / 2);

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
