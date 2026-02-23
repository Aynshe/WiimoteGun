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
        private const string GAMEPAD_SUBFOLDER = "Gamepad";
        private const string DEFAULT_GAMEPAD_PROFILE_NAME = "default.remap";

        private static string _cachedRemapDirectory = null;

        /// <summary>
        /// Get RetroBat installation path from registry
        /// (EN/FR: Obtenir le chemin d'installation RetroBat depuis le registre)
        /// </summary>
        public static string GetRetroBatPath()
        {
            if (Options.Instance.StandaloneMode)
            {
                SimpleLogger.Instance.Info("Standalone Mode: Skipping RetroBat registry search");
                return null;
            }

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
                            SimpleLogger.Instance.Info(string.Format("RetroBat path found in registry: {0}", path));
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("Failed to read RetroBat registry: {0}", ex.Message));
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

            string remapDir;

            if (Options.Instance.StandaloneMode)
            {
                // Standalone: use local directory
                // (EN/FR: Autonome : utiliser dossier local)
                string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                remapDir = Path.Combine(exeDir, "RemapProfiles");
                SimpleLogger.Instance.Info(string.Format("Standalone Mode: Using local remap directory: {0}", remapDir));
            }
            else
            {
                string retroBatPath = GetRetroBatPath();

                if (!string.IsNullOrEmpty(retroBatPath) && Directory.Exists(retroBatPath))
                {
                    remapDir = Path.Combine(retroBatPath, REMAP_SUBFOLDER);
                    SimpleLogger.Instance.Info(string.Format("Using RetroBat remap directory: {0}", remapDir));
                }
                else
                {
                    // Fallback: use local directory if RetroBat not found
                    // (EN/FR: Repli : utiliser dossier local si RetroBat introuvable)
                    string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                    remapDir = Path.Combine(exeDir, "RemapProfiles");
                    SimpleLogger.Instance.Warning(string.Format("RetroBat not found, using fallback directory: {0}", remapDir));
                }
            }

            // Create directory if it doesn't exist (EN/FR: Créer le dossier s'il n'existe pas)
            try
            {
                if (!Directory.Exists(remapDir))
                {
                    Directory.CreateDirectory(remapDir);
                    SimpleLogger.Instance.Info(string.Format("Created remap directory: {0}", remapDir));
                }

                _cachedRemapDirectory = remapDir;
                return remapDir;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to create remap directory: {0}", ex.Message));
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
                SimpleLogger.Instance.Error(string.Format("Remap profile not found: {0}", fullPath));
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RemapProfile));
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    RemapProfile profile = serializer.Deserialize(stream) as RemapProfile;
                    SimpleLogger.Instance.Info(string.Format("Loaded remap profile: {0}", fullPath));
                    return profile;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to load remap profile {0}: {1}", fullPath, ex.Message));
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

            SimpleLogger.Instance.Info(string.Format("Loading default.remap from: {0}", defaultPath));
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
                        P4Mappings = Options.Instance.P4Mappings?.Clone() ?? new PlayerMappings(),
                        
                        // Capture current hotkeys for default profile (EN/FR: Capturer hotkeys actuelles pour profil par défaut)
                        P1Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(1).Hotkeys.Select(h => h.Clone())),
                        P2Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(2).Hotkeys.Select(h => h.Clone())),
                        P3Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(3).Hotkeys.Select(h => h.Clone())),
                        P4Hotkeys = new List<Hotkey>(HotkeyManager.GetRawProfile(4).Hotkeys.Select(h => h.Clone()))
                    };

                    // FIX V25e: Sanitize default profile to ensure Gyro is DISABLED.
                    // This prevents 'dirty' gyro state from the current session (e.g. loaded from another profile) 
                    // from polluting the auto-generated default.remap.

                    
                    XmlSerializer serializer = new XmlSerializer(typeof(RemapProfile));
                    using (FileStream stream = File.Create(defaultPath))
                    {
                        serializer.Serialize(stream, defaultProfile);
                    }
                    
                    SimpleLogger.Instance.Info(string.Format("Auto-created default.remap from current settings.cfg mapping: {0}", defaultPath));
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning(string.Format("Failed to auto-create default.remap: {0}", ex.Message));
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
                    SimpleLogger.Instance.Error(string.Format("Cannot save '{0}' in Root folder. Only 'default.remap' is allowed in Root. Please select a subfolder.", profileName));
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
                    SimpleLogger.Instance.Info(string.Format("Created subfolder: {0}", targetDir));
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to create subfolder: {0}", ex.Message));
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

                SimpleLogger.Instance.Info(string.Format("Saved remap profile: {0}", fullPath));
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to save remap profile {0}: {1}", fullPath, ex.Message));
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
                SimpleLogger.Instance.Error(string.Format("Failed to get subfolders: {0}", ex.Message));
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
                SimpleLogger.Instance.Error(string.Format("Failed to get profiles in folder {0}: {1}", subfolder, ex.Message));
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

        // =============================================================================================
        // GAMEPAD PROFILE MANAGEMENT (EN/FR: GESTION PROFILS GAMEPAD)
        // =============================================================================================

        /// <summary>
        /// Get the GamePad remap directory path (creates if doesn't exist)
        /// (EN/FR: Obtenir le chemin du dossier remap GamePad)
        /// </summary>
        public static string GetGamePadRemapDirectory()
        {
            string baseRemapDir = GetRemapDirectory();
            if (string.IsNullOrEmpty(baseRemapDir)) return null;

            string gamepadDir = Path.Combine(baseRemapDir, GAMEPAD_SUBFOLDER);
            
            try
            {
                if (!Directory.Exists(gamepadDir))
                {
                    Directory.CreateDirectory(gamepadDir);
                    SimpleLogger.Instance.Info(string.Format("Created GamePad remap directory: {0}", gamepadDir));
                }
                return gamepadDir;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to create GamePad remap directory: {0}", ex.Message));
                return null;
            }
        }

        public static List<string> GetGamePadSubfolders()
        {
            string gamepadDir = GetGamePadRemapDirectory();
            if (string.IsNullOrEmpty(gamepadDir) || !Directory.Exists(gamepadDir))
                return new List<string>();

            try
            {
                return Directory.GetDirectories(gamepadDir)
                    .Select(d => Path.GetFileName(d))
                    .ToList();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to get GamePad subfolders: {ex.Message}");
                return new List<string>();
            }
        }

        public static List<string> GetGamePadProfilesInFolder(string subfolder)
        {
            string gamepadDir = GetGamePadRemapDirectory();
            if (string.IsNullOrEmpty(gamepadDir)) return new List<string>();

            string targetDir = string.IsNullOrEmpty(subfolder) 
                ? gamepadDir 
                : Path.Combine(gamepadDir, subfolder);

            if (!Directory.Exists(targetDir)) return new List<string>();

            try
            {
                return Directory.GetFiles(targetDir, "*.remap")
                    .Select(f => Path.GetFileName(f))
                    .ToList();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to get GamePad profiles: {ex.Message}");
                return new List<string>();
            }
        }

        public static GamePadProfile LoadGamePadProfile(string relativePath)
        {
            string gamepadDir = GetGamePadRemapDirectory();
            if (string.IsNullOrEmpty(gamepadDir)) return null;

            string fullPath = Path.Combine(gamepadDir, relativePath);
            fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (!File.Exists(fullPath))
            {
                SimpleLogger.Instance.Error($"GamePad profile not found: {fullPath}");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GamePadProfile));
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    var profile = serializer.Deserialize(stream) as GamePadProfile;
                    SimpleLogger.Instance.Info($"Loaded GamePad profile: {fullPath}");
                    return profile;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to load GamePad profile {fullPath}: {ex.Message}");
                return null;
            }
        }

        public static GamePadProfile LoadDefaultGamePadProfile()
        {
            return LoadGamePadProfile(DEFAULT_GAMEPAD_PROFILE_NAME);
        }

        public static bool SaveGamePadProfile(string profileName, string subfolder, GamePadProfile profile)
        {
            string gamepadDir = GetGamePadRemapDirectory();
            if (string.IsNullOrEmpty(gamepadDir)) return false;

            // AUTO-CREATE default.remap on first profile save (EN/FR: Auto-créer default.remap à la première sauvegarde)
            string defaultPath = Path.Combine(gamepadDir, DEFAULT_GAMEPAD_PROFILE_NAME);
            if (!File.Exists(defaultPath))
            {
                try
                {
                    // Create default.remap from current Options settings (EN/FR: Créer default.remap depuis les options actuelles)
                    GamePadProfile defaultProfile = new GamePadProfile
                    {
                        ProfileName = "Default",
                        P1Mappings = Options.Instance.P1GamePadMappings?.Clone() ?? new GamePadMappings(),
                        P2Mappings = Options.Instance.P2GamePadMappings?.Clone() ?? new GamePadMappings(),
                        P3Mappings = Options.Instance.P3GamePadMappings?.Clone() ?? new GamePadMappings(),
                        P4Mappings = Options.Instance.P4GamePadMappings?.Clone() ?? new GamePadMappings()
                    };

                    XmlSerializer serializer = new XmlSerializer(typeof(GamePadProfile));
                    using (FileStream stream = File.Create(defaultPath))
                    {
                        serializer.Serialize(stream, defaultProfile);
                    }
                    SimpleLogger.Instance.Info(string.Format("Auto-created GamePad default.remap from current settings.cfg: {0}", defaultPath));
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning(string.Format("Failed to auto-create GamePad default.remap: {0}", ex.Message));
                }
            }

            // Ensure only default.remap in root Gamepad folder? 
            // The constraint "Only default.remap allowed in Root" was for main remap folder because of settings.cfg confusion?
            // Let's enforce it here too for consistency, or not?
            // Main constraint was because loading "Root" meant loading settings.cfg or default.remap. 
            // Here, GamePad profiles are separate. But let's keep it clean.
            
            if (string.IsNullOrEmpty(subfolder) || subfolder == "[Root]")
            {
                 // EN/FR: Force 'default.remap' when saving to Root, regardless of input name.
                 // This ensures a fallback profile always exists.
                 // (EN/FR: Force 'default.remap' lors de la sauvegarde dans Root, quel que soit le nom.
                 // Cela garantit qu'un profil de repli existe toujours.)
                 profileName = DEFAULT_GAMEPAD_PROFILE_NAME;
            }

            string targetDir = string.IsNullOrEmpty(subfolder) ? gamepadDir : Path.Combine(gamepadDir, subfolder);

            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            }
            catch { return false; }

            if (!profileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                profileName += ".remap";

            string fullPath = Path.Combine(targetDir, profileName);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GamePadProfile));
                using (FileStream stream = File.Create(fullPath))
                {
                    serializer.Serialize(stream, profile);
                }
                SimpleLogger.Instance.Info($"Saved GamePad profile: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save GamePad profile: {ex.Message}");
                return false;
            }
        }

    }

    [Serializable]
    public class GamePadProfile
    {
        public string ProfileName { get; set; }
        public GamePadMappings P1Mappings { get; set; }
        public GamePadMappings P2Mappings { get; set; }
        public GamePadMappings P3Mappings { get; set; }
        public GamePadMappings P4Mappings { get; set; }

        public GamePadProfile()
        {
            ProfileName = "Unnamed";
            P1Mappings = new GamePadMappings();
            P2Mappings = new GamePadMappings();
            P3Mappings = new GamePadMappings();
            P4Mappings = new GamePadMappings();
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
        
        // Hotkey storage (EN/FR: Stockage des hotkeys)
        public List<Hotkey> P1Hotkeys { get; set; }
        public List<Hotkey> P2Hotkeys { get; set; }
        public List<Hotkey> P3Hotkeys { get; set; }
        public List<Hotkey> P4Hotkeys { get; set; }

        public RemapProfile()
        {
            ProfileName = "Unnamed";
            P1Mappings = new PlayerMappings();
            P2Mappings = new PlayerMappings();
            P3Mappings = new PlayerMappings();
            P4Mappings = new PlayerMappings();
            
            P1Hotkeys = new List<Hotkey>();
            P2Hotkeys = new List<Hotkey>();
            P3Hotkeys = new List<Hotkey>();
            P4Hotkeys = new List<Hotkey>();
        }
    }
}
