using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace WiimoteGun
{
    /// <summary>
    /// Manages remap profile loading/saving from RetroBat directory
    /// (EN/FR: Gère le chargement/sauvegarde des profils remap depuis le dossier RetroBat)
    /// </summary>
    public class RemapProfileManager
    {
        private const string RETROBAT_REGISTRY_KEY = @"Software\RetroBat";
        private const string RETROBAT_PATH_VALUE = "LatestKnownInstallPath";
        private const string REMAP_SUBFOLDER = @"user\WiimoteGunRemap";
        private const string DEFAULT_PROFILE_NAME = "default.remap";

        private static string _cachedRemapDirectory = null;

        /// <summary>
        /// Get RetroBat installation path from registry
        /// (EN/FR: Obtenir le chemin d'installation RetroBat depuis le registre)
        /// </summary>
        public static string GetRetroBatPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RETROBAT_REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(RETROBAT_PATH_VALUE);
                        if (value != null)
                        {
                            string path = value.ToString();
                            SimpleLogger.Instance.Info($"RetroBat path found in registry: {path}");
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning($"Failed to read RetroBat registry: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get the remap directory path (creates if doesn't exist)
        /// (EN/FR: Obtenir le chemin du dossier remap (crée si inexistant))
        /// </summary>
        public static string GetRemapDirectory()
        {
            if (_cachedRemapDirectory != null && Directory.Exists(_cachedRemapDirectory))
                return _cachedRemapDirectory;

            string retroBatPath = GetRetroBatPath();
            string remapDir;

            if (!string.IsNullOrEmpty(retroBatPath) && Directory.Exists(retroBatPath))
            {
                remapDir = Path.Combine(retroBatPath, REMAP_SUBFOLDER);
                SimpleLogger.Instance.Info($"Using RetroBat remap directory: {remapDir}");
            }
            else
            {
                // Fallback: use local directory if RetroBat not found
                // (EN/FR: Repli : utiliser dossier local si RetroBat introuvable)
                string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                remapDir = Path.Combine(exeDir, "RemapProfiles");
                SimpleLogger.Instance.Warning($"RetroBat not found, using fallback directory: {remapDir}");
            }

            // Create directory if it doesn't exist (EN/FR: Créer le dossier s'il n'existe pas)
            try
            {
                if (!Directory.Exists(remapDir))
                {
                    Directory.CreateDirectory(remapDir);
                    SimpleLogger.Instance.Info($"Created remap directory: {remapDir}");
                }

                _cachedRemapDirectory = remapDir;
                return remapDir;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to create remap directory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a remap profile from relative path (e.g., "myfolder/game.remap")
        /// (EN/FR: Charger un profil remap depuis un chemin relatif)
        /// </summary>
        public static RemapProfile LoadProfile(string relativePath)
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir))
            {
                SimpleLogger.Instance.Error("Cannot load profile: remap directory unavailable");
                return null;
            }

            string fullPath = Path.Combine(remapDir, relativePath);
            // Normalize separators to fix mixed slash issues (EN/FR: Normaliser séparateurs pour corriger problèmes slashs mixtes)
            fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (!File.Exists(fullPath))
            {
                SimpleLogger.Instance.Error($"Remap profile not found: {fullPath}");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RemapProfile));
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    RemapProfile profile = serializer.Deserialize(stream) as RemapProfile;
                    SimpleLogger.Instance.Info($"Loaded remap profile: {fullPath}");
                    return profile;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load remap profile {fullPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if default.remap exists at root
        /// (EN/FR: Vérifier si default.remap existe à la racine)
        /// </summary>
        public static RemapProfile LoadDefaultProfile()
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir))
                return null;

            string defaultPath = Path.Combine(remapDir, DEFAULT_PROFILE_NAME);
            if (!File.Exists(defaultPath))
            {
                SimpleLogger.Instance.Info("No default.remap found");
                return null;
            }

            SimpleLogger.Instance.Info($"Loading default.remap from: {defaultPath}");
            return LoadProfile(DEFAULT_PROFILE_NAME);
        }
        
        /// <summary>
        /// Save a remap profile
        /// (EN/FR: Sauvegarder un profil remap)
        /// </summary>
        public static bool SaveProfile(string profileName, string subfolder, RemapProfile profile)
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir))
            {
                SimpleLogger.Instance.Error("Cannot save profile: remap directory unavailable");
                return false;
            }
            
            // AUTO-CREATE default.remap on first profile save (EN/FR: Auto-créer default.remap à la première sauvegarde)
            // This preserves the initial settings.cfg mapping before it gets changed
            // (EN/FR: Cela préserve le mapping initial de settings.cfg avant qu'il ne soit modifié)
            string defaultPath = Path.Combine(remapDir, DEFAULT_PROFILE_NAME);
            if (!File.Exists(defaultPath))
            {
                try
                {
                    // Create default.remap from current Options settings (EN/FR: Créer default.remap depuis les options actuelles)
                    RemapProfile defaultProfile = new RemapProfile
                    {
                        ProfileName = "Default",
                        P1Mappings = Options.Instance.P1Mappings?.Clone() ?? new PlayerMappings(),
                        P2Mappings = Options.Instance.P2Mappings?.Clone() ?? new PlayerMappings(),
                        P3Mappings = Options.Instance.P3Mappings?.Clone() ?? new PlayerMappings(),
                        P4Mappings = Options.Instance.P4Mappings?.Clone() ?? new PlayerMappings()
                    };
                    
                    XmlSerializer serializer = new XmlSerializer(typeof(RemapProfile));
                    using (FileStream stream = File.Create(defaultPath))
                    {
                        serializer.Serialize(stream, defaultProfile);
                    }
                    
                    SimpleLogger.Instance.Info($"Auto-created default.remap from current settings.cfg mapping: {defaultPath}");
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning($"Failed to auto-create default.remap: {ex.Message}");
                    // Don't return false - this is not critical, continue with profile save
                }
            }
            
            // Protection: Only default.remap allowed in Root folder (EN/FR: Protection : seul default.remap autorisé dans dossier Root)
            if (string.IsNullOrEmpty(subfolder) || subfolder == "[Root]")
            {
                string normalizedName = profileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase) 
                    ? profileName 
                    : profileName + ".remap";
                    
                if (!normalizedName.Equals("default.remap", StringComparison.OrdinalIgnoreCase))
                {
                    SimpleLogger.Instance.Error($"Cannot save '{profileName}' in Root folder. Only 'default.remap' is allowed in Root. Please select a subfolder.");
                    System.Windows.Forms.MessageBox.Show(
                        "Cannot save profile in Root folder.\n\n" +
                        "Only 'default.remap' is allowed in the Root directory.\n" +
                        "Please select or create a subfolder for your custom profiles.",
                        "Root Folder Protected",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    return false;
                }
            }

            // Build target directory (EN/FR: Construire le dossier cible)
            string targetDir = string.IsNullOrEmpty(subfolder)
                ? remapDir
                : Path.Combine(remapDir, subfolder);

            // Create subfolder if needed (EN/FR: Créer le sous-dossier si nécessaire)
            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    SimpleLogger.Instance.Info($"Created subfolder: {targetDir}");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to create subfolder: {ex.Message}");
                return false;
            }

            // Ensure .remap extension (EN/FR: Assurer l'extension .remap)
            if (!profileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
               profileName += ".remap";

            string fullPath = Path.Combine(targetDir, profileName);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RemapProfile));
                using (FileStream stream = File.Create(fullPath))
                {
                    serializer.Serialize(stream, profile);
                }

                SimpleLogger.Instance.Info($"Saved remap profile: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save remap profile {fullPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of subfolders in remap directory
        /// (EN/FR: Obtenir la liste des sous-dossiers dans le dossier remap)
        /// </summary>
        public static List<string> GetSubfolders()
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir) || !Directory.Exists(remapDir))
                return new List<string>();

            try
            {
                var directories = Directory.GetDirectories(remapDir)
                    .Select(d => Path.GetFileName(d))
                    .ToList();

                return directories;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to get subfolders: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get list of .remap files in a specific folder
        /// (EN/FR: Obtenir la liste des fichiers .remap dans un dossier spécifique)
        /// </summary>
        public static List<string> GetProfilesInFolder(string subfolder)
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir))
                return new List<string>();

            string targetDir = string.IsNullOrEmpty(subfolder)
                ? remapDir
                : Path.Combine(remapDir, subfolder);

            if (!Directory.Exists(targetDir))
                return new List<string>();

            try
            {
                var profiles = Directory.GetFiles(targetDir, "*.remap")
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                return profiles;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to get profiles in folder {subfolder}: {ex.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Get the full path to a profile file (EN/FR: Obtenir le chemin complet vers un fichier de profil)
        /// </summary>
        public static string GetProfilePath(string profileName, string subfolder = "")
        {
            string remapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(remapDir))
                return null;
            
            string targetDir = string.IsNullOrEmpty(subfolder) ? remapDir : Path.Combine(remapDir, subfolder);
            return Path.Combine(targetDir, profileName);
        }
    }

    /// <summary>
    /// Remap profile data structure
    /// (EN/FR: Structure de données pour profil remap)
    /// </summary>
    [Serializable]
    public class RemapProfile
    {
        public string ProfileName { get; set; }
        public PlayerMappings P1Mappings { get; set; }
        public PlayerMappings P2Mappings { get; set; }
        public PlayerMappings P3Mappings { get; set; }
        public PlayerMappings P4Mappings { get; set; }

        public RemapProfile()
        {
            ProfileName = "Unnamed";
            P1Mappings = new PlayerMappings();
            P2Mappings = new PlayerMappings();
            P3Mappings = new PlayerMappings();
            P4Mappings = new PlayerMappings();
        }
    }
}
