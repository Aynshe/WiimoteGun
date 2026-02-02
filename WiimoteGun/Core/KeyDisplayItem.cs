using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Wrapper to display AZERTY-friendly key names in listbox (EN/FR: Wrapper pour afficher noms AZERTY dans listbox)
    /// </summary>
    public class KeyDisplayItem
    {
        public Keys Key { get; set; }
        public string DisplayName { get; set; }
        
        public KeyDisplayItem(Keys key)
        {
            Key = key;
            DisplayName = GetAzertyKeyName(key);
        }
        
        public override string ToString() => DisplayName;
        
        /// <summary>
        /// Get AZERTY-friendly display name for a key (EN/FR: Obtenir nom AZERTY pour une touche)
        /// </summary>
        public static string GetAzertyKeyName(Keys key)
        {
            switch (key)
            {
                // Number row with AZERTY characters (EN/FR: Rangée chiffres)
                case Keys.D1: return "1 (&)";
                case Keys.D2: return "2 (é)";
                case Keys.D3: return "3 (\")";
                case Keys.D4: return "4 (')";
                case Keys.D5: return "5 (()";
                case Keys.D6: return "6 (-)";
                case Keys.D7: return "7 (è)";
                case Keys.D8: return "8 (_)";
                case Keys.D9: return "9 (ç)";
                case Keys.D0: return "0 (à)";
                
                // OEM keys with AZERTY mapping (EN/FR: Touches OEM AZERTY)
                case Keys.Oemtilde: return "² (Tilde)";
                case Keys.OemMinus: return ") (°)";
                case Keys.Oemplus: return "= (+)";
                case Keys.OemOpenBrackets: return "^ (¨)";
                case Keys.OemCloseBrackets: return "$ (£)";
                case Keys.OemPipe: return "* (µ)";
                case Keys.OemSemicolon: return "ù (%)";
                case Keys.OemQuotes: return "' (²)";
                case Keys.Oemcomma: return ", (?)";
                case Keys.OemPeriod: return "; (.)";
                case Keys.OemQuestion: return ": (/)";
                case Keys.Oem102: return "< (>)";
                
                // Common keys (EN/FR: Touches courantes)
                case Keys.Return: return "Enter ↵";
                case Keys.Space: return "Space ⎵";
                case Keys.Back: return "Backspace ⌫";
                case Keys.Tab: return "Tab ⇥";
                case Keys.Escape: return "Escape";
                case Keys.Delete: return "Delete";
                case Keys.Insert: return "Insert";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "Page Up";
                case Keys.PageDown: return "Page Down";
                case Keys.Up: return "↑ Up";
                case Keys.Down: return "↓ Down";
                case Keys.Left: return "← Left";
                case Keys.Right: return "→ Right";
                case Keys.CapsLock: return "Caps Lock";
                case Keys.NumLock: return "Num Lock";
                case Keys.Scroll: return "Scroll Lock";
                case Keys.PrintScreen: return "Print Screen";
                case Keys.Pause: return "Pause";
                
                // Modifiers (EN/FR: Modificateurs)
                case Keys.LShiftKey:
                case Keys.ShiftKey: return "Shift";
                case Keys.RShiftKey: return "Right Shift";
                case Keys.LControlKey:
                case Keys.ControlKey: return "Ctrl";
                case Keys.RControlKey: return "Right Ctrl";
                case Keys.LMenu: return "Alt";
                case Keys.RMenu: return "Alt Gr";
                case Keys.LWin: return "Win";
                case Keys.RWin: return "Right Win";
                
                // Numpad (EN/FR: Pavé numérique)
                case Keys.NumPad0: return "Num 0";
                case Keys.NumPad1: return "Num 1";
                case Keys.NumPad2: return "Num 2";
                case Keys.NumPad3: return "Num 3";
                case Keys.NumPad4: return "Num 4";
                case Keys.NumPad5: return "Num 5";
                case Keys.NumPad6: return "Num 6";
                case Keys.NumPad7: return "Num 7";
                case Keys.NumPad8: return "Num 8";
                case Keys.NumPad9: return "Num 9";
                case Keys.Multiply: return "Num *";
                case Keys.Add: return "Num +";
                case Keys.Subtract: return "Num -";
                case Keys.Divide: return "Num /";
                case Keys.Decimal: return "Num .";
                
                default: return key.ToString();
            }
        }
    }
}
