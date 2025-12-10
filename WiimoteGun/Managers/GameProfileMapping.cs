using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace WiimoteGun
{
    public class GameProfileMapping
    {
        public string ExecutableName { get; set; }
        public string ExecutablePath { get; set; } // Full path for strict matching (EN/FR: Chemin complet pour correspondance stricte)
        public string ProfilePath { get; set; }
        public bool AutoLoad { get; set; }
    }

    public static class GameProfileMappingManager
    {
        private static readonly string MappingFileName = "game_profile_mappings.xml";
        private static List<GameProfileMapping> _mappings = new List<GameProfileMapping>();
        private static object _lock = new object();

        static GameProfileMappingManager()
        {
            LoadMappings();
        }

        public static void AddMapping(string exeName, string profilePath, string exePath = null)
        {
            lock (_lock)
            {
                // Normalize profile path to forward slashes for consistency (EN/FR: Normaliser chemin profil avec slashs avant pour cohérence)
                string normalizedProfilePath = profilePath?.Replace('\\', '/');
                
                // Remove any existing mappings for this profile to enforce 1:1 relationship
                // (EN/FR: Supprimer tout mapping existant pour ce profil pour forcer relation 1:1)
                _mappings.RemoveAll(m => m.ProfilePath.Replace('\\', '/').Equals(normalizedProfilePath, StringComparison.OrdinalIgnoreCase));
                
                // Try to find existing mapping for this executable
                // (EN/FR: Chercher mapping existant pour cet exécutable)
                GameProfileMapping existing = null;
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    existing = _mappings.FirstOrDefault(m => 
                        !string.IsNullOrEmpty(m.ExecutablePath) && 
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase));
                }
                
                if (existing == null)
                {
                    existing = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                }

                if (existing != null)
                {
                    existing.ProfilePath = normalizedProfilePath;
                    existing.AutoLoad = true;
                    if (!string.IsNullOrEmpty(exePath)) existing.ExecutablePath = exePath;
                }
                else
                {
                    _mappings.Add(new GameProfileMapping
                    {
                        ExecutableName = exeName,
                        ExecutablePath = exePath,
                        ProfilePath = normalizedProfilePath,
                        AutoLoad = true
                    });
                }
                SaveMappings();
            }
        }

        public static void RemoveMapping(string exeName)
        {
            lock (_lock)
            {
                var existing = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _mappings.Remove(existing);
                    SaveMappings();
                }
            }
        }
        
        /// <summary>
        /// Remove mapping by profile path (EN/FR: Supprimer mapping par chemin de profil)
        /// </summary>
        public static void RemoveMappingByProfile(string profilePath)
        {
            lock (_lock)
            {
                var toRemove = _mappings.Where(m => m.ProfilePath.Equals(profilePath, StringComparison.OrdinalIgnoreCase)).ToList();
                if (toRemove.Any())
                {
                    foreach (var mapping in toRemove)
                    {
                        _mappings.Remove(mapping);
                    }
                    SaveMappings();
                    SimpleLogger.Instance.Info($"Removed {toRemove.Count} mapping(s) for profile: {profilePath}");
                }
            }
        }

        public static string GetProfileForGame(string exeName, string exePath = null)
        {
            lock (_lock)
            {
                // Priority to path matching (EN/FR: Priorité à la correspondance par chemin)
                if (!string.IsNullOrEmpty(exePath))
                {
                    var pathMapping = _mappings.FirstOrDefault(m => 
                        !string.IsNullOrEmpty(m.ExecutablePath) && 
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase));
                        
                    if (pathMapping != null && pathMapping.AutoLoad)
                        return pathMapping.ProfilePath;
                }
                
                // Fallback to name matching (EN/FR: Repli sur correspondance par nom)
                var nameMapping = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                if (nameMapping != null && nameMapping.AutoLoad)
                {
                    // If this mapping has a path but we didn't match it above (and exePath was provided), 
                    // it means it's a different game with same exe name -> Don't load!
                    // (EN/FR: Si ce mapping a un chemin mais pas de match (et exePath fourni), c'est un autre jeu -> Ne pas charger!)
                    if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(nameMapping.ExecutablePath))
                        return null;
                        
                    return nameMapping.ProfilePath;
                }
                return null;
            }
        }
        
        public static string GetExecutableForProfile(string profilePath)
        {
            lock (_lock)
            {
                // Normalize path separators for comparison (EN/FR: Normaliser séparateurs chemin pour comparaison)
                string normalizedPath = profilePath?.Replace('\\', '/');
                
                // Find first mapping that points to this profile
                // (EN/FR: Trouver premier mapping pointant vers ce profil)
                var mapping = _mappings.FirstOrDefault(m => 
                    m.ProfilePath.Replace('\\', '/').Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));
                    
                if (mapping != null && mapping.AutoLoad)
                {
                    return !string.IsNullOrEmpty(mapping.ExecutablePath) 
                        ? Path.GetFileName(mapping.ExecutablePath) // Return exe name (maybe full path is too long for UI)
                        : mapping.ExecutableName;
                }
                return null;
            }
        }
        
        /// <summary>
        /// Get mapping for a specific executable name (reverse lookup)
        /// (EN/FR: Obtenir mapping pour un nom d'exécutable spécifique (recherche inverse))
        /// </summary>
        public static GameProfileMapping GetMappingForExecutable(string executableName)
        {
            lock (_lock)
            {
                // Find first mapping for this executable name (EN/FR: Trouver premier mapping pour ce nom d'exécutable)
                return _mappings.FirstOrDefault(m => 
                    m.ExecutableName.Equals(executableName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static void LoadMappings()
        {
            try
            {
                string remapDir = RemapProfileManager.GetRemapDirectory();
                string filePath = Path.Combine(remapDir, MappingFileName);

                if (File.Exists(filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<GameProfileMapping>));
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        _mappings = (List<GameProfileMapping>)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load game mappings: {ex.Message}");
                _mappings = new List<GameProfileMapping>();
            }
        }

        private static void SaveMappings()
        {
            try
            {
                string remapDir = RemapProfileManager.GetRemapDirectory();
                string filePath = Path.Combine(remapDir, MappingFileName);

                XmlSerializer serializer = new XmlSerializer(typeof(List<GameProfileMapping>));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, _mappings);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save game mappings: {ex.Message}");
            }
        }
    }
}
