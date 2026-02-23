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
        public string GamePadProfilePath { get; set; } // V27: Link to GamePad profile (EN/FR: Lien vers profil GamePad)
        public bool AutoLoad { get; set; }
    }

    public static class GameProfileMappingManager
    {
        private static readonly string JsonMappingFileName = "game_profile_mappings.json";
        private static readonly string XmlMappingFileName = "game_profile_mappings.xml"; // Legacy support
        private static List<GameProfileMapping> _mappings = new List<GameProfileMapping>();
        private static object _lock = new object();

        static GameProfileMappingManager()
        {
            LoadMappings();
        }

        public static void AddMapping(string exeName, string profilePath, string exePath = null, string gamePadProfilePath = null)
        {
            lock (_lock)
            {
                // Normalize profile path to forward slashes for consistency (EN/FR: Normaliser chemin profil avec slashs avant pour cohérence)
                string normalizedProfilePath = profilePath?.Replace('\\', '/');
                
                // CRITICAL FIX: Allow Many-to-One (Many Games -> One Profile). 
                // Do NOT remove existing mappings for this profile.
                // (EN/FR: Permettre Plusieurs-à-Un. NE PAS supprimer les mappings existants pour ce profil.)
                // _mappings.RemoveAll(m => m.ProfilePath.Equals(normalizedProfilePath, StringComparison.OrdinalIgnoreCase));
                
                // string normalizedGamePadProfilePath = null;
                
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
                    SimpleLogger.Instance.Info($"Updating existing mapping for {exeName} -> {profilePath}");
                    if (normalizedProfilePath != null) existing.ProfilePath = normalizedProfilePath;
                    if (gamePadProfilePath != null) existing.GamePadProfilePath = gamePadProfilePath.Replace('\\', '/');
                    existing.AutoLoad = true;
                    if (!string.IsNullOrEmpty(exePath)) existing.ExecutablePath = exePath;
                }
                else
                {
                    SimpleLogger.Instance.Info($"Adding new mapping used for {exeName} -> {profilePath}");
                    _mappings.Add(new GameProfileMapping
                    {
                        ExecutableName = exeName,
                        ExecutablePath = exePath,
                        ProfilePath = normalizedProfilePath,
                        GamePadProfilePath = gamePadProfilePath?.Replace('\\', '/'),
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
                // Normalize input path to forward slashes for consistent comparison
                // (EN/FR: Normaliser le chemin d'entrée pour une comparaison cohérente)
                string normalizedPath = profilePath?.Replace('\\', '/');
                var toRemove = _mappings.Where(m => m.ProfilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();
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

        /// <summary>
        /// Remove GamePad profile link from mappings (EN/FR: Supprimer lien profil GamePad des mappings)
        /// </summary>
        public static void RemoveGamePadProfileLink(string gamePadProfilePath)
        {
            lock (_lock)
            {
                string normalizedPath = gamePadProfilePath?.Replace('\\', '/');
                var toUpdate = _mappings.Where(m => 
                    !string.IsNullOrEmpty(m.GamePadProfilePath) && 
                    m.GamePadProfilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();

                if (toUpdate.Any())
                {
                    foreach (var mapping in toUpdate)
                    {
                        mapping.GamePadProfilePath = null;
                        // If mapping becomes empty (no profile, no gamepad profile), maybe remove it?
                        // For now, keep it unless it has absolutely no data.
                        if (string.IsNullOrEmpty(mapping.ProfilePath) && string.IsNullOrEmpty(mapping.ExecutablePath))
                        {
                            // checking if strictly empty might be too aggressive, let's just clear the link.
                        }
                    }
                    SaveMappings();
                    SimpleLogger.Instance.Info($"Removed GamePad profile link '{gamePadProfilePath}' from {toUpdate.Count} mapping(s).");
                }
            }
        }

        /// <summary>
        /// Remove GamePad profile link for a specific executable (EN/FR: Supprimer lien profil GamePad pour un exécutable précis)
        /// </summary>
        public static void RemoveGamePadProfileLinkForExecutable(string exeName)
        {
            lock (_lock)
            {
                var mapping = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                if (mapping != null)
                {
                    mapping.GamePadProfilePath = null;
                    SaveMappings();
                    SimpleLogger.Instance.Info($"Removed GamePad profile link for executable '{exeName}'.");
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

        public static string GetGamePadProfileForGame(string exeName, string exePath = null)
        {
            lock (_lock)
            {
                // Priority to path matching
                if (!string.IsNullOrEmpty(exePath))
                {
                    var pathMapping = _mappings.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.ExecutablePath) &&
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase));

                    if (pathMapping != null && pathMapping.AutoLoad)
                        return pathMapping.GamePadProfilePath;
                }

                // Fallback to name matching
                var nameMapping = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                if (nameMapping != null && nameMapping.AutoLoad)
                {
                    if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(nameMapping.ExecutablePath))
                        return null;

                    return nameMapping.GamePadProfilePath;
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
                    m.ProfilePath?.Replace('\\', '/').Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) == true);
                    
                if (mapping != null && mapping.AutoLoad)
                {
                    return !string.IsNullOrEmpty(mapping.ExecutablePath) 
                        ? Path.GetFileName(mapping.ExecutablePath) // Return exe name (maybe full path is too long for UI)
                        : mapping.ExecutableName;
                }
                return null;
            }
        }

        public static string GetExecutableForGamePadProfile(string profilePath)
        {
            lock (_lock)
            {
                string normalizedPath = profilePath?.Replace('\\', '/');
                var mapping = _mappings.FirstOrDefault(m => 
                    m.GamePadProfilePath?.Replace('\\', '/').Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) == true);
                    
                if (mapping != null && mapping.AutoLoad)
                {
                    return !string.IsNullOrEmpty(mapping.ExecutablePath) 
                        ? Path.GetFileName(mapping.ExecutablePath)
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
                string jsonPath = Path.Combine(remapDir, JsonMappingFileName);
                string xmlPath = Path.Combine(remapDir, XmlMappingFileName);

                if (File.Exists(jsonPath))
                {
                    // Load JSON (EN/FR: Charger JSON)
                    string jsonContent = File.ReadAllText(jsonPath);
                    _mappings = SimpleJsonHelper.DeserializeMappings(jsonContent);
                }
                else
                {
                    // Migration Strategy (EN/FR: Stratégie de migration)
                    // 1. Check XML in Remap Directory
                    // 2. Check XML in Local Application Directory (Fallback for upgrade)
                    
                    string localXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XmlMappingFileName);
                    string sourceXmlPath = null;

                    if (File.Exists(xmlPath)) sourceXmlPath = xmlPath;
                    else if (File.Exists(localXmlPath)) sourceXmlPath = localXmlPath;

                    if (sourceXmlPath != null)
                    {
                        SimpleLogger.Instance.Info($"Migrating game mappings from XML ({sourceXmlPath}) to JSON...");
                        XmlSerializer serializer = new XmlSerializer(typeof(List<GameProfileMapping>));
                        using (StreamReader reader = new StreamReader(sourceXmlPath))
                        {
                            _mappings = (List<GameProfileMapping>)serializer.Deserialize(reader);
                        }
                        // Save immediately to create JSON (EN/FR: Sauvegarder immédiatement pour créer JSON)
                        SaveMappings();
                    }
                    else
                    {
                        // Create empty JSON if neither exists (EN/FR: Créer JSON vide si aucun n'existe)
                        _mappings = new List<GameProfileMapping>();
                        SaveMappings();
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
                string jsonPath = Path.Combine(remapDir, JsonMappingFileName);

                string jsonContent = SimpleJsonHelper.SerializeMappings(_mappings);
                File.WriteAllText(jsonPath, jsonContent);
                SimpleLogger.Instance.Info($"[GameProfileMapping] Mappings saved to JSON: {jsonPath}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save game mappings: {ex.Message}");
            }
        }

        // ============================================
        // Simple JSON Helper (No external dependencies)
        // (EN/FR: Aide JSON simple (Sans dépendances externes))
        // ============================================
        private static class SimpleJsonHelper
        {
            public static string SerializeMappings(List<GameProfileMapping> mappings)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < mappings.Count; i++)
                {
                    var m = mappings[i];
                    sb.AppendLine("  {");
                    sb.AppendLine($"    \"ExecutableName\": \"{Escape(m.ExecutableName)}\",");
                    sb.AppendLine($"    \"ExecutablePath\": \"{Escape(m.ExecutablePath)}\",");
                    sb.AppendLine($"    \"ProfilePath\": \"{Escape(m.ProfilePath)}\",");
                    sb.AppendLine($"    \"GamePadProfilePath\": \"{Escape(m.GamePadProfilePath)}\",");
                    sb.AppendLine($"    \"AutoLoad\": {(m.AutoLoad ? "true" : "false")}"); // Last item, no comma
                    sb.Append("  }");
                    if (i < mappings.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                return sb.ToString();
            }

            public static List<GameProfileMapping> DeserializeMappings(string json)
            {
                var list = new List<GameProfileMapping>();
                // Very basic parsing: split by objects -> "{" (EN/FR: Parsing très basique)
                // This assumes the format produced by SerializeMappings or similar simple structure
                
                string[] objects = json.Split(new[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var objStr in objects)
                {
                    if (!objStr.Contains("{")) continue;
                    
                    var m = new GameProfileMapping();
                    m.ExecutableName = ExtractValue(objStr, "ExecutableName");
                    m.ExecutablePath = ExtractValue(objStr, "ExecutablePath");
                    m.ProfilePath = ExtractValue(objStr, "ProfilePath");
                    m.GamePadProfilePath = ExtractValue(objStr, "GamePadProfilePath");
                    
                    string autoLoadStr = ExtractValue(objStr, "AutoLoad");
                    m.AutoLoad = autoLoadStr != null && autoLoadStr.ToLower() == "true";
                    
                    if (!string.IsNullOrEmpty(m.ExecutableName))
                        list.Add(m);
                }
                return list;
            }

            private static string Escape(string s)
            {
                if (s == null) return "";
                return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }

            private static string ExtractValue(string source, string key)
            {
                string searchKey = $"\"{key}\":";
                int keyIdx = source.IndexOf(searchKey);
                if (keyIdx == -1) return null;
                
                int startValue = keyIdx + searchKey.Length;
                
                // Identify if string or bool (EN/FR: Identifier si string ou bool)
                int quoteStart = source.IndexOf("\"", startValue);
                int boolStart = -1;
                
                int nextSearch = startValue;
                while (boolStart == -1 && nextSearch < source.Length && nextSearch < startValue + 20)
                {
                    if (char.IsLetter(source[nextSearch])) boolStart = nextSearch;
                    nextSearch++;
                }

                if (quoteStart != -1 && (boolStart == -1 || quoteStart < boolStart))
                {
                    // It's a string
                    int quoteEnd = source.IndexOf("\"", quoteStart + 1);
                    if (quoteEnd == -1) return null;
                    return source.Substring(quoteStart + 1, quoteEnd - quoteStart - 1).Replace("\\\\", "\\").Replace("\\\"", "\"");
                }
                else if (boolStart != -1)
                {
                    // It's a boolean/number
                    int valEnd = source.IndexOfAny(new[] { ',', '}', '\r', '\n' }, boolStart);
                    if (valEnd == -1) valEnd = source.Length;
                    return source.Substring(boolStart, valEnd - boolStart).Trim();
                }
                
                return null;
            }
        }
    }
}
