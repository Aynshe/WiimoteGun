using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiimoteGun.UI.Modern.Forms
{
    /// <summary>
    /// Simple floating panel overlay for in-game offset adjustment
    /// (EN/FR: Panneau flottant simple pour ajustement offset en jeu)
    /// Includes cursor dot and info panel with smooth fade effect
    /// </summary>
    public class OffsetAdjustmentOverlay : Form
    {
        private static OffsetAdjustmentOverlay _instance;
        private static readonly object _lock = new object();
        
        // Cursor dot form (EN/FR: Formulaire point curseur)
        private CursorDotForm _cursorDot;
        
        // State (EN/FR: État)
        private int _playerIndex = 1;
        private int _offsetX = 0;
        private int _offsetY = 0;
        private volatile bool _isShowing = false;
        private volatile bool _isFadingOut = false;
        
        // Fade animation (EN/FR: Animation de fondu)
        // Very slow fade: 0.007 opacity decrease per 30ms tick = ~4.0 seconds total fade
        private const double FADE_STEP = 0.007;
        private System.Windows.Forms.Timer _fadeTimer;
        
        // Colors per player (EN/FR: Couleurs par joueur)
        private static readonly Color[] PlayerColors = new Color[]
        {
            Color.FromArgb(100, 180, 255),  // P1 Blue
            Color.FromArgb(255, 100, 100),  // P2 Red
            Color.FromArgb(100, 255, 130),  // P3 Green
            Color.FromArgb(255, 220, 100)   // P4 Yellow
        };
        
        // Panel size (EN/FR: Taille panneau)
        private const int PANEL_WIDTH = 220;
        private const int PANEL_HEIGHT = 120;
        private const int OFFSET_FROM_CURSOR = 90;
        
        // Fonts (EN/FR: Polices)
        private Font _titleFont;
        private Font _valueFont;
        
        // Timer for position updates (EN/FR: Timer pour mise à jour position)
        private System.Windows.Forms.Timer _updateTimer;
        
        private OffsetAdjustmentOverlay()
        {
            // Double buffering (EN/FR: Double buffering)
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.UpdateStyles();

            InitializeComponent();
        }
        
        public static OffsetAdjustmentOverlay Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null || _instance.IsDisposed)
                    {
                        _instance = new OffsetAdjustmentOverlay();
                    }
                    return _instance;
                }
            }
        }
        
        /// <summary>
        /// Initialize the overlay and subscribe to events
        /// (EN/FR: Initialiser l'overlay et s'abonner aux événements)
        /// Must be called from UI thread after construction
        /// </summary>
        public void Initialize()
        {
            // Force handle creation (EN/FR: Forcer création du handle)
            var dummy = this.Handle;
            
            // Create cursor dot (EN/FR: Créer point curseur)
            _cursorDot = new CursorDotForm();
            var dotDummy = _cursorDot.Handle;
            
            // Subscribe to events (EN/FR: S'abonner aux événements)
            WiiMoteController.OffsetAdjustmentChanged += OnOffsetChanged;
            
            // Start timer (EN/FR: Démarrer timer)
            _updateTimer.Start();
            
            SimpleLogger.Instance.Info("[OffsetOverlay] Initialized on UI thread");
        }
        
        private void InitializeComponent()
        {
            // Form settings (EN/FR: Paramètres du formulaire)
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(220, 120); // PANEL_WIDTH=220, PANEL_HEIGHT=120
            this.BackColor = Color.FromArgb(30, 30, 35);
            this.Name = "OffsetAdjustmentOverlay";
            this.Opacity = 0.95;
            
            // Create fonts (EN/FR: Créer polices)
            _titleFont = new Font("Segoe UI", 16, FontStyle.Bold); // x2 approx (9->18 is too big for UI sometimes, 16 is good)
            _valueFont = new Font("Consolas", 20, FontStyle.Bold); // x2 approx (11->22, 20 is good)
            
            // Timer for position updates (EN/FR: Timer pour mise à jour position)
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 30; // ~33fps
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // Fade timer (EN/FR: Timer de fondu)
            _fadeTimer = new System.Windows.Forms.Timer();
            _fadeTimer.Interval = 30; // ~33fps fade animation
            _fadeTimer.Tick += FadeTimer_Tick;
            
            // Paint event (EN/FR: Événement paint)
            this.Paint += OffsetOverlay_Paint;
            
            // Initial position off-screen (EN/FR: Position initiale hors écran)
            this.Location = new Point(-500, -500);
        }
        
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
        
        protected override CreateParams CreateParams
        {
            get
            {
                // Make tool window and no activate (EN/FR: Fenêtre outil sans activation)
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
        
        private void OnOffsetChanged(int playerIndex, int offsetX, int offsetY, bool isActive)
        {
            // Store values thread-safely (EN/FR: Stocker valeurs thread-safe)
            _playerIndex = playerIndex;
            _offsetX = offsetX;
            _offsetY = offsetY;
            
            if (isActive)
            {
                // Always reset opacity and show when active (EN/FR: Toujours reset opacité et afficher si actif)
                // This handles both initial show and direction change during fade
                SafeInvoke(() => ShowOverlay());
            }
            else if (_isShowing && !_isFadingOut)
            {
                // Start smooth fade out (EN/FR: Démarrer fondu smooth)
                _isFadingOut = true;
                SafeInvoke(() => StartFadeOut());
            }
        }
        
        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed) return;
                
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Info($"[OffsetOverlay] SafeInvoke error: {ex.Message}");
            }
        }
        
        private void ShowOverlay()
        {
            try
            {
                // Stop fade timer and reset state (EN/FR: Arrêter timer de fondu et reset état)
                _fadeTimer.Stop();
                _isFadingOut = false;
                _isShowing = true;
                
                // Update color on cursor dot (EN/FR: Mettre à jour couleur sur point curseur)
                int idx = Math.Min(Math.Max(_playerIndex - 1, 0), PlayerColors.Length - 1);
                _cursorDot.SetColor(PlayerColors[idx]);
                
                // Reset opacity to full (EN/FR: Réinitialiser opacité à 100%)
                this.Opacity = 0.95;
                _cursorDot.SetOpacity(1.0);
                
                UpdatePosition();
                this.Visible = true;
                this.BringToFront();
                
                // Show cursor dot (EN/FR: Afficher point curseur)
                _cursorDot.ShowDot();
                
                this.Invalidate();
                SimpleLogger.Instance.Info($"[OffsetOverlay] Shown for P{_playerIndex}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Info($"[OffsetOverlay] Show error: {ex.Message}");
            }
        }
        
        private void StartFadeOut()
        {
            try
            {
                // Start fade animation (EN/FR: Démarrer animation de fondu)
                _fadeTimer.Start();
            }
            catch { }
        }
        
        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Check if fade was cancelled (EN/FR: Vérifier si fondu annulé)
                if (!_isFadingOut)
                {
                    _fadeTimer.Stop();
                    return;
                }
                
                // Decrease opacity (EN/FR: Diminuer opacité)
                double newOpacity = this.Opacity - FADE_STEP;
                
                if (newOpacity <= 0)
                {
                    // Fade complete, hide (EN/FR: Fondu terminé, cacher)
                    _fadeTimer.Stop();
                    _isFadingOut = false;
                    _isShowing = false;
                    this.Visible = false;
                    _cursorDot.HideDot();
                    this.Opacity = 0.95; // Reset for next show (EN/FR: Réinitialiser pour prochain affichage)
                    _cursorDot.SetOpacity(1.0);
                    SimpleLogger.Instance.Info($"[OffsetOverlay] Hidden. Final: X={_offsetX}, Y={_offsetY}");
                }
                else
                {
                    // Continue fading (EN/FR: Continuer le fondu)
                    this.Opacity = newOpacity;
                    _cursorDot.SetOpacity(newOpacity / 0.95);
                }
            }
            catch { }
        }
        
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isShowing && this.Visible)
            {
                UpdatePosition();
                _cursorDot.UpdatePosition();
                this.Invalidate();
            }
        }
        
        private void UpdatePosition()
        {
            try
            {
                Point cursor = Cursor.Position;
                Screen screen = Screen.FromPoint(cursor);
                Rectangle bounds = screen.Bounds;
                
                int x = cursor.X + OFFSET_FROM_CURSOR;
                int y = cursor.Y - PANEL_HEIGHT / 2;
                
                // Keep on screen (EN/FR: Garder à l'écran)
                if (x + PANEL_WIDTH > bounds.Right - 10)
                    x = cursor.X - PANEL_WIDTH - OFFSET_FROM_CURSOR;
                if (y < bounds.Top + 10)
                    y = bounds.Top + 10;
                if (y + PANEL_HEIGHT > bounds.Bottom - 10)
                    y = bounds.Bottom - PANEL_HEIGHT - 10;
                
                this.Location = new Point(x, y);
            }
            catch { }
        }
        
        private void OffsetOverlay_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Get player color (EN/FR: Obtenir couleur joueur)
            int idx = Math.Min(Math.Max(_playerIndex - 1, 0), PlayerColors.Length - 1);
            Color pColor = PlayerColors[idx];
            
            // Draw border (EN/FR: Dessiner bordure)
            using (Pen borderPen = new Pen(pColor, 3))
            {
                g.DrawRectangle(borderPen, 1, 1, this.Width - 3, this.Height - 3);
            }
            
            // Draw title (EN/FR: Dessiner titre)
            string title = string.Format("P{0} OFFSET", _playerIndex);
            using (SolidBrush titleBrush = new SolidBrush(pColor))
            {
                g.DrawString(title, _titleFont, titleBrush, 16, 12);
            }
            
            // Draw X value (EN/FR: Dessiner valeur X)
            string xText = string.Format("X: {0}", _offsetX >= 0 ? "+" + _offsetX : _offsetX.ToString());
            using (SolidBrush valueBrush = new SolidBrush(Color.White))
            {
                g.DrawString(xText, _valueFont, valueBrush, 20, 48);
            }
            
            // Draw Y value (EN/FR: Dessiner valeur Y)
            string yText = string.Format("Y: {0}", _offsetY >= 0 ? "+" + _offsetY : _offsetY.ToString());
            using (SolidBrush valueBrush = new SolidBrush(Color.White))
            {
                g.DrawString(yText, _valueFont, valueBrush, 20, 80);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    WiiMoteController.OffsetAdjustmentChanged -= OnOffsetChanged;
                    _updateTimer?.Stop();
                    _updateTimer?.Dispose();
                    _fadeTimer?.Stop();
                    _fadeTimer?.Dispose();
                    _titleFont?.Dispose();
                    _valueFont?.Dispose();
                    _cursorDot?.Dispose();
                }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
    
    /// <summary>
    /// Small circular dot that follows cursor position
    /// (EN/FR: Petit point circulaire qui suit la position du curseur)
    /// </summary>
    internal class CursorDotForm : Form
    {
        private const int DOT_SIZE = 20;
        private Color _dotColor = Color.FromArgb(100, 180, 255);
        
        public CursorDotForm()
        {
            // Double buffering (EN/FR: Double buffering)
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.UpdateStyles();

            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(20, 20); // DOT_SIZE = 20
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            
            this.Paint += CursorDot_Paint;
            
            // Initial position off-screen (EN/FR: Position initiale hors écran)
            this.Location = new Point(-500, -500);
        }
        
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
        
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT - click through
                return cp;
            }
        }
        
        public void SetColor(Color color)
        {
            _dotColor = color;
            this.Invalidate();
        }
        
        public void SetOpacity(double opacity)
        {
            this.Opacity = Math.Max(0, Math.Min(1, opacity));
        }
        
        public void ShowDot()
        {
            UpdatePosition();
            this.Visible = true;
            this.BringToFront();
        }
        
        public void HideDot()
        {
            this.Visible = false;
        }
        
        public void UpdatePosition()
        {
            try
            {
                Point cursor = Cursor.Position;
                // Center dot on cursor (EN/FR: Centrer le point sur le curseur)
                this.Location = new Point(cursor.X - DOT_SIZE / 2, cursor.Y - DOT_SIZE / 2);
            }
            catch { }
        }
        
        private void CursorDot_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw filled circle (EN/FR: Dessiner cercle rempli)
            using (SolidBrush brush = new SolidBrush(_dotColor))
            {
                g.FillEllipse(brush, 2, 2, DOT_SIZE - 4, DOT_SIZE - 4);
            }
            
            // Draw border (EN/FR: Dessiner bordure)
            using (Pen pen = new Pen(Color.White, 2))
            {
                g.DrawEllipse(pen, 2, 2, DOT_SIZE - 4, DOT_SIZE - 4);
            }
        }
    }
}
