using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WiimoteGun
{
    /// <summary>
    /// Represents a single hotkey with trigger button and dual keyboard output (Short/Long)
    /// (EN/FR: Représente une hotkey avec bouton déclencheur et double sortie clavier)
    /// </summary>
    [Serializable]
    public class Hotkey : ICloneable
    {
        /// <summary>
        /// Trigger button (e.g., "A", "B", "Up", "Down")
        /// (EN/FR: Bouton déclencheur)
        /// </summary>
        public string TriggerButton { get; set; }

        /// <summary>
        /// Modifier button (e.g., "Home")
        /// (EN/FR: Bouton modificateur)
        /// </summary>
        public string ModifierButton { get; set; }

        /// <summary>
        /// Keys to simulate on Short Press (< 500ms)
        /// (EN/FR: Touches à simuler sur Pression Courte)
        /// </summary>
        [XmlIgnore]
        public List<Keys> ShortPressKeys { get; set; }

        /// <summary>
        /// Keys to simulate on Long Press (>= 500ms)
        /// (EN/FR: Touches à simuler sur Pression Longue)
        /// </summary>
        [XmlIgnore]
        public List<Keys> LongPressKeys { get; set; }

        /// <summary>
        /// String representation for XML serialization of ShortPressKeys
        /// </summary>
        public string ShortPressKeysString
        {
            get { return string.Join(",", ShortPressKeys ?? new List<Keys>()); }
            set { ShortPressKeys = ParseKeyCombin(value); }
        }

        /// <summary>
        /// String representation for XML serialization of LongPressKeys
        /// </summary>
        public string LongPressKeysString
        {
            get { return string.Join(",", LongPressKeys ?? new List<Keys>()); }
            set { LongPressKeys = ParseKeyCombin(value); }
        }

        /// <summary>
        /// Description of the hotkey action
        /// (EN/FR: Description de l'action)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this hotkey is shared with all other players (P1 only)
        /// (EN/FR: Si cette hotkey est partagée avec les autres joueurs)
        /// </summary>
        public bool IsShared { get; set; }

        public Hotkey()
        {
            ShortPressKeys = new List<Keys>();
            LongPressKeys = new List<Keys>();
            ModifierButton = "Home"; // Default
            IsShared = false;
        }

        public Hotkey Clone()
        {
            var clone = new Hotkey
            {
                TriggerButton = this.TriggerButton,
                ModifierButton = this.ModifierButton,
                Description = this.Description,
                IsShared = this.IsShared
            };

            if (this.ShortPressKeys != null)
                clone.ShortPressKeys = new List<Keys>(this.ShortPressKeys);
            
            if (this.LongPressKeys != null)
                clone.LongPressKeys = new List<Keys>(this.LongPressKeys);

            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public string GetDisplayName()
        {
            return string.Format("{0} + {1}", ModifierButton, TriggerButton);
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
                Keys key;
                if (Enum.TryParse<Keys>(part.Trim(), out key))
                {
                    keys.Add(key);
                }
            }
            return keys;
        }
    }
}
