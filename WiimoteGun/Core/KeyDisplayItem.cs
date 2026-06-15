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
        
        public static string GetAzertyKeyName(Keys key)
        {
            return ButtonAction.GetAzertyKeyName(key);
        }
    }
}
