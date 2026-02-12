using System;
using System.Collections.Generic;
using System.Linq;

namespace WiimoteGun
{
    /// <summary>
    /// Hotkey profile for a single player
    /// (EN/FR: Profil de hotkeys pour un joueur)
    /// </summary>
    [Serializable]
    public class HotkeyProfile
    {
        /// <summary>
        /// Player index (1-4) (EN/FR: Index du joueur)
        /// </summary>
        public int PlayerIndex { get; set; }

        /// <summary>
        /// List of hotkeys for this player (EN/FR: Liste des hotkeys)
        /// </summary>
        public List<Hotkey> Hotkeys { get; set; }

        public HotkeyProfile()
        {
            Hotkeys = new List<Hotkey>();
        }

        public HotkeyProfile(int playerIndex)
        {
            PlayerIndex = playerIndex;
            Hotkeys = new List<Hotkey>();
        }

        /// <summary>
        /// Add or update a hotkey (EN/FR: Ajouter ou mettre à jour hotkey)
        /// </summary>
        public void AddOrUpdateHotkey(Hotkey hotkey)
        {
            // Remove existing hotkey with same button and modifier if exists
            // (EN/FR: Supprimer hotkey existante avec même bouton et modificateur)
            var existing = Hotkeys.FirstOrDefault(h => 
                string.Equals(h.TriggerButton, hotkey.TriggerButton, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(h.ModifierButton, hotkey.ModifierButton, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Hotkeys.Remove(existing);
            }

            Hotkeys.Add(hotkey);
            SimpleLogger.Instance.Info(string.Format("[Hotkey] Added/Updated: {0} for Player {1}", hotkey.GetDisplayName(), PlayerIndex));
        }

        /// <summary>
        /// Remove a hotkey (EN/FR: Supprimer une hotkey)
        /// </summary>
        public void RemoveHotkey(string triggerButton, string modifier = "Home")
        {
            var hotkey = Hotkeys.FirstOrDefault(h => 
                string.Equals(h.TriggerButton, triggerButton, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(h.ModifierButton, modifier, StringComparison.OrdinalIgnoreCase));

            if (hotkey != null)
            {
                Hotkeys.Remove(hotkey);
                SimpleLogger.Instance.Info(string.Format("[Hotkey] Removed: {0} + {1} for Player {2}", modifier, triggerButton, PlayerIndex));
            }
        }

        /// <summary>
        /// Get hotkey by trigger button (EN/FR: Obtenir hotkey)
        /// </summary>
        public Hotkey GetHotkey(string triggerButton, string modifier = "Home")
        {
            return Hotkeys.FirstOrDefault(h => 
                string.Equals(h.TriggerButton, triggerButton, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(h.ModifierButton, modifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if a hotkey exists (EN/FR: Vérifier si hotkey existe)
        /// </summary>
        public bool HasHotkey(string triggerButton, string modifier = "Home")
        {
            return GetHotkey(triggerButton, modifier) != null;
        }

        /// <summary>
        /// Clear all hotkeys (EN/FR: Effacer toutes les hotkeys)
        /// </summary>
        public void ClearAll()
        {
            Hotkeys.Clear();
            SimpleLogger.Instance.Info(string.Format("[Hotkey] Cleared all hotkeys for Player {0}", PlayerIndex));
        }

        /// <summary>
        /// Get count of hotkeys (EN/FR: Nombre de hotkeys)
        /// </summary>
        public int Count { get { return Hotkeys.Count; } }
    }
}
