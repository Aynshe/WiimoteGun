using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WiimoteGun
{
    /// <summary>
    /// Represents a single hotkey with trigger button and keyboard output
    /// (EN/FR: Représente une hotkey avec bouton déclencheur et sortie clavier)
    /// </summary>
    [Serializable]
    public class Hotkey
    {
        /// <summary>
        /// Trigger button (e.g., "A", "B", "Up", "Down")
        /// (EN/FR: Bouton déclencheur)
        /// </summary>
        public string TriggerButton { get; set; }

        /// <summary>
        /// Press type: Short or Long (EN/FR: Type de pression)
        /// </summary>
        public HotkeyPressType PressType { get; set; }

        /// <summary>
        /// Keyboard keys to send - NOT serialized directly (EN/FR: Touches clavier - pas sérialisé directement)
        /// </summary>
        [XmlIgnore]
        public List<Keys> KeyCombination { get; set; }

        /// <summary>
        /// String representation for XML serialization (EN/FR: Représentation string pour sérialisation XML)
        /// </summary>
        public string KeyCombinationString
        {
            get { return string.Join(",", KeyCombination ?? new List<Keys>()); }
            set { KeyCombination = ParseKeyCombin(value); }
        }

        /// <summary>
        /// User description (optional) (EN/FR: Description utilisateur)
        /// </summary>
        public string Description { get; set; }

        public Hotkey()
        {
            KeyCombination = new List<Keys>();
        }

        public Hotkey(string triggerButton, HotkeyPressType pressType, List<Keys> keyCombination, string description = "")
        {
            TriggerButton = triggerButton;
            PressType = pressType;
            KeyCombination = keyCombination ?? new List<Keys>();
            Description = description;
        }

        /// <summary>
        /// Get friendly name for display (EN/FR: Nom convivial pour affichage)
        /// </summary>
        public string GetDisplayName()
        {
            string pressTypeStr = PressType == HotkeyPressType.Short ? "Short" : "Long";
            string keysStr = string.Join("+", KeyCombination);
            return $"Home + {TriggerButton} ({pressTypeStr}) → {keysStr}";
        }

        /// <summary>
        /// Convert Keys list to string for serialization (EN/FR: Convertir liste Keys en string)
        /// </summary>
        public string KeyCombinationToString()
        {
            return string.Join(",", KeyCombination);
        }

        /// <summary>
        /// Parse string to Keys list for deserialization (EN/FR: Parser string vers liste Keys)
        /// </summary>
        public static List<Keys> ParseKeyCombin(string keysString)
        {
            List<Keys> keys = new List<Keys>();
            if (string.IsNullOrEmpty(keysString))
                return keys;

            string[] parts = keysString.Split(',');
            foreach (string part in parts)
            {
                if (Enum.TryParse<Keys>(part.Trim(), out Keys key))
                {
                    keys.Add(key);
                }
            }
            return keys;
        }

        /// <summary>
        /// Create a deep copy of the hotkey (EN/FR: Créer une copie profonde)
        /// </summary>
        public Hotkey Clone()
        {
            return new Hotkey
            {
                TriggerButton = this.TriggerButton,
                PressType = this.PressType,
                KeyCombination = new List<Keys>(this.KeyCombination),
                Description = this.Description
            };
        }
    }

    /// <summary>
    /// Hotkey press type: Short or Long (EN/FR: Type de pression hotkey)
    /// </summary>
    public enum HotkeyPressType
    {
        /// <summary>
        /// Short press (&lt; 500ms) (EN/FR: Pression courte)
        /// </summary>
        Short,

        /// <summary>
        /// Long press (≥ 500ms) (EN/FR: Pression longue)
        /// </summary>
        Long
    }
}
