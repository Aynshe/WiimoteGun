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
            // Remove existing hotkey with same button and press type if exists
            // (EN/FR: Supprimer hotkey existante avec même bouton et type)
            var existing = Hotkeys.FirstOrDefault(h => 
                h.TriggerButton == hotkey.TriggerButton && 
                h.PressType == hotkey.PressType);

            if (existing != null)
            {
                Hotkeys.Remove(existing);
            }

            Hotkeys.Add(hotkey);
            SimpleLogger.Instance.Info($"[Hotkey] Added/Updated: {hotkey.GetDisplayName()} for Player {PlayerIndex}");
        }

        /// <summary>
        /// Remove a hotkey (EN/FR: Supprimer une hotkey)
        /// </summary>
        public void RemoveHotkey(string triggerButton, HotkeyPressType pressType)
        {
            var hotkey = Hotkeys.FirstOrDefault(h => 
                h.TriggerButton == triggerButton && 
                h.PressType == pressType);

            if (hotkey != null)
            {
                Hotkeys.Remove(hotkey);
                SimpleLogger.Instance.Info($"[Hotkey] Removed: Home + {triggerButton} ({pressType}) for Player {PlayerIndex}");
            }
        }

        /// <summary>
        /// Get hotkey by trigger button and press type (EN/FR: Obtenir hotkey)
        /// </summary>
        public Hotkey GetHotkey(string triggerButton, HotkeyPressType pressType)
        {
            return Hotkeys.FirstOrDefault(h => 
                h.TriggerButton == triggerButton && 
                h.PressType == pressType);
        }

        /// <summary>
        /// Check if a hotkey exists (EN/FR: Vérifier si hotkey existe)
        /// </summary>
        public bool HasHotkey(string triggerButton, HotkeyPressType pressType)
        {
            return GetHotkey(triggerButton, pressType) != null;
        }

        /// <summary>
        /// Clear all hotkeys (EN/FR: Effacer toutes les hotkeys)
        /// </summary>
        public void ClearAll()
        {
            Hotkeys.Clear();
            SimpleLogger.Instance.Info($"[Hotkey] Cleared all hotkeys for Player {PlayerIndex}");
        }

        /// <summary>
        /// Get count of hotkeys (EN/FR: Nombre de hotkeys)
        /// </summary>
        public int Count => Hotkeys.Count;
    }
}
