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
        public bool IsEmulator { get; set; } // [V42] Exe is an emulator: profile is linked per GAME (EN/FR: L'exe est un émulateur : profil lié par JEU)
        public string GameName { get; set; } // [V42] ES game name (rom file without extension) when IsEmulator
        public bool GameIsFolder { get; set; } // [V43] Game identity is the FOLDER name with extension (TeknoParrot .pc/.game/.win...)
        public string SystemName { get; set; } // [V46] ES system selected when the link was created (tile modal system filtering)
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

        public static void AddMapping(string exeName, string profilePath, string exePath = null, string gamePadProfilePath = null, bool isEmulator = false, string gameName = null, bool gameIsFolder = false, string systemName = null)
        {
            lock (_lock)
            {
                // [V52b] Sync with the disk BEFORE mutating: an external (editor) edit made
                // since the last app write must never be clobbered by a stale in-memory
                // copy when the app saves another change.
                // (EN/FR: Synchro disque AVANT mutation : une modification externe (éditeur)
                // depuis la dernière écriture de l'app ne doit jamais être écrasée par une
                // copie mémoire périmée quand l'app sauve un autre changement.)
                ReloadIfChangedExternallyCore();

                // [V52b] The ROOT default.remap (mouse OR GamePad side) can be EDITED but
                // NEVER associated to an executable in the JSON: it is the base fallback
                // mapping. Reject the link instead of polluting the JSON.
                // (EN/FR: Le default.remap RACINE (côté souris OU GamePad) peut être MODIFIÉ
                // mais JAMAIS associé à un exécutable dans le JSON : c'est le mapping de
                // base de repli. Rejeter le lien au lieu de polluer le JSON.)
                bool profileIsRootDefault = IsRootDefaultProfilePath(profilePath);
                bool gamePadIsRootDefault = IsRootDefaultProfilePath(gamePadProfilePath);
                if (profileIsRootDefault || gamePadIsRootDefault)
                {
                    SimpleLogger.Instance.Warning("[V52b] Association to the root default.remap is not allowed (base mapping only): request ignored for '" + (exeName ?? "?") + "'.");
                }
                string normalizedProfilePath = profileIsRootDefault ? null : profilePath?.Replace('\\', '/');
                string normalizedGamePadProfilePath = gamePadIsRootDefault ? null : gamePadProfilePath?.Replace('\\', '/');
                if (string.IsNullOrEmpty(normalizedProfilePath) && string.IsNullOrEmpty(normalizedGamePadProfilePath))
                    return; // Nothing left to link (EN/FR: Plus rien à lier)

                string normalizedGameName = string.IsNullOrEmpty(gameName) ? null : gameName.Trim();
                string normalizedSystemName = string.IsNullOrEmpty(systemName) ? null : systemName.Trim(); // [V50]
                
                // CRITICAL FIX: Allow Many-to-One (Many Games -> One Profile). 
                // Do NOT remove existing mappings for this profile.
                // (EN/FR: Permettre Plusieurs-à-Un. NE PAS supprimer les mappings existants pour ce profil.)
                // _mappings.RemoveAll(m => m.ProfilePath.Equals(normalizedProfilePath, StringComparison.OrdinalIgnoreCase));
                
                // string normalizedGamePadProfilePath = null;
                
                // Try to find existing mapping for this executable
                // [V42] Keyed by (exe, GameName): several games can share one emulator
                // (EN/FR: Chercher mapping existant, clé (exe, GameName) : plusieurs
                // jeux peuvent partager un émulateur)
                GameProfileMapping existing = null;
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    existing = _mappings.FirstOrDefault(m => 
                        !string.IsNullOrEmpty(m.ExecutablePath) && 
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normalizedGameName ?? "", StringComparison.OrdinalIgnoreCase));
                }
                
                if (existing == null)
                {
                    existing = _mappings.FirstOrDefault(m => 
                        !string.IsNullOrEmpty(m.ExecutableName) &&
                        m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normalizedGameName ?? "", StringComparison.OrdinalIgnoreCase));
                }

                if (existing != null)
                {
                    SimpleLogger.Instance.Info($"Updating existing mapping for {exeName}" + (normalizedGameName != null ? $" [game: {normalizedGameName}]" : "") + $" -> {profilePath}");
                    if (normalizedProfilePath != null) existing.ProfilePath = normalizedProfilePath;
                    if (normalizedGamePadProfilePath != null) existing.GamePadProfilePath = normalizedGamePadProfilePath;
                    existing.AutoLoad = true;
                    if (!string.IsNullOrEmpty(exePath)) existing.ExecutablePath = exePath;
                    // [V42] Emulator / per-game link
                    existing.IsEmulator = isEmulator;
                    existing.GameName = normalizedGameName;
                    existing.GameIsFolder = gameIsFolder; // [V43]
                    existing.SystemName = normalizedSystemName ?? CaptureSystemName(); // [V46+V50]
                }
                else
                {
                    SimpleLogger.Instance.Info($"Adding new mapping used for {exeName}" + (normalizedGameName != null ? $" [game: {normalizedGameName}]" : "") + $" -> {profilePath}");
                    existing = new GameProfileMapping
                    {
                        ExecutableName = exeName,
                        ExecutablePath = exePath,
                        ProfilePath = normalizedProfilePath,
                        GamePadProfilePath = normalizedGamePadProfilePath,
                        IsEmulator = isEmulator,
                        GameName = normalizedGameName,
                        GameIsFolder = gameIsFolder, // [V43]
                        SystemName = normalizedSystemName ?? CaptureSystemName(), // [V46+V50]
                        AutoLoad = true
                    };
                    _mappings.Add(existing);
                }

                // [V32] Constraint: one executable = ONE association PER GAME.
                // [V42] The key is (exe, GameName) so several games can share one emulator.
                // Purge any other duplicate entries for the same exe AND same game.
                // (EN/FR: Contrainte : un exécutable = UNE association PAR JEU.
                // La clé est (exe, GameName) pour que plusieurs jeux partagent un émulateur.)
                var duplicates = _mappings.Where(m => m != existing &&
                    ((!string.IsNullOrEmpty(m.ExecutableName) && m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(m.ExecutablePath) && m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                    && string.Equals(m.GameName ?? "", normalizedGameName ?? "", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (duplicates.Any())
                {
                    foreach (var dup in duplicates) _mappings.Remove(dup);
                    SimpleLogger.Instance.Info($"Removed {duplicates.Count} duplicate mapping(s) for executable '{exeName}'.");
                }

                // [V36] Constraint: one GamePad profile = ONE executable.
                // Setting a GamePad profile link REPLACES the link of any other
                // executable previously associated with the same profile (no second
                // link can accumulate). Empty entries are removed entirely.
                // (EN/FR: Contrainte : un profil GamePad = UN seul exécutable.
                // Définir un lien profil GamePad REMPLACE le lien de tout autre
                // exécutable précédemment associé au même profil (aucun deuxième lien
                // ne peut s'accumuler). Les entrées vides sont supprimées entièrement.)
                if (!string.IsNullOrEmpty(normalizedGamePadProfilePath))
                {
                    string newLink = normalizedGamePadProfilePath;
                    var previousLinks = _mappings.Where(m => m != existing &&
                        !string.IsNullOrEmpty(m.GamePadProfilePath) &&
                        m.GamePadProfilePath.Equals(newLink, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var prev in previousLinks)
                    {
                        prev.GamePadProfilePath = null;
                        string prevExe = !string.IsNullOrEmpty(prev.ExecutablePath) ? Path.GetFileName(prev.ExecutablePath) : prev.ExecutableName;
                        bool removed = false;
                        if (string.IsNullOrEmpty(prev.ProfilePath))
                        {
                            _mappings.Remove(prev);
                            removed = true;
                        }
                        SimpleLogger.Instance.Info($"Replaced GamePad profile link '{newLink}': previous executable '{prevExe}' " + (removed ? "removed from mappings JSON." : "lost its GamePad link (mouse link kept)."));
                    }
                }

                // [V37] Same constraint for MOUSE profiles: one profile = ONE executable.
                // Setting a mouse profile link REPLACES the link of any other executable
                // previously associated with the same profile. Empty entries are removed.
                // (EN/FR: Même contrainte pour les profils SOURIS : un profil = UN seul
                // exécutable. Définir un lien profil souris REMPLACE le lien de tout autre
                // exécutable précédemment associé au même profil. Entrées vides supprimées.)
                if (!string.IsNullOrEmpty(normalizedProfilePath))
                {
                    var previousLinks = _mappings.Where(m => m != existing &&
                        !string.IsNullOrEmpty(m.ProfilePath) &&
                        m.ProfilePath.Equals(normalizedProfilePath, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var prev in previousLinks)
                    {
                        prev.ProfilePath = null;
                        string prevExe = !string.IsNullOrEmpty(prev.ExecutablePath) ? Path.GetFileName(prev.ExecutablePath) : prev.ExecutableName;
                        bool removed = false;
                        if (string.IsNullOrEmpty(prev.GamePadProfilePath))
                        {
                            _mappings.Remove(prev);
                            removed = true;
                        }
                        SimpleLogger.Instance.Info($"Replaced mouse profile link '{normalizedProfilePath}': previous executable '{prevExe}' " + (removed ? "removed from mappings JSON." : "lost its mouse link (GamePad link kept)."));
                    }
                }

                SaveMappings();
            }
        }

        public static void RemoveMapping(string exeName)
        {
            lock (_lock)
            {
                ReloadIfChangedExternallyCore(); // [V52b] Never mutate a stale copy
                var existing = _mappings.FirstOrDefault(m => m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _mappings.Remove(existing);
                    SaveMappings();
                }
            }
        }

        /// <summary>
        /// EN: [V52b] True when the path is the ROOT default profile ("default.remap"
        /// without any folder prefix, mouse or GamePad side). The root default is the
        /// editable BASE mapping and must NEVER be associated in the JSON.
        /// FR: [V52b] True si le chemin est le profil default RACINE ("default.remap"
        /// sans préfixe de dossier, côté souris ou GamePad). Le default racine est le
        /// mapping de BASE modifiable et ne doit JAMAIS être associé dans le JSON.
        /// </summary>
        public static bool IsRootDefaultProfilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Replace('\\', '/').Trim().Equals("default.remap", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// EN: [V37] Mouse-side equivalent of RemoveGamePadProfileLinkForExecutable.
        /// Removes the mouse profile link of an executable; the entry is deleted from
        /// the JSON only when it has no GamePad link left (unchecked Auto-load = no
        /// association). Does NOT destroy an existing GamePad link.
        /// [V42] Keyed by (exe, GameName): unchecking removes the link of the current
        /// game when the exe is an emulator link.
        /// FR: [V37] Équivalent souris de RemoveGamePadProfileLinkForExecutable.
        /// Retire le lien profil souris d'un exécutable ; l'entrée n'est supprimée du
        /// JSON que s'il ne reste aucun lien GamePad (Auto-load décoché = aucune
        /// association). Ne détruit PAS un lien GamePad existant.
        /// [V42] Clé (exe, GameName) : le décochage retire le lien du jeu courant quand
        /// l'exe est un lien émulateur.
        /// </summary>
        public static void RemoveProfileLinkForExecutable(string exeName, string exePath = null, string gameName = null)
        {
            lock (_lock)
            {
                ReloadIfChangedExternallyCore(); // [V52b] Never mutate a stale copy
                string normGame = string.IsNullOrEmpty(gameName) ? null : gameName.Trim();

                // Match by full path first, then by name (EN/FR: Correspondance par chemin complet puis par nom)
                GameProfileMapping mapping = null;
                if (!string.IsNullOrEmpty(exePath))
                {
                    mapping = _mappings.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.ExecutablePath) &&
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normGame ?? "", StringComparison.OrdinalIgnoreCase));
                }
                if (mapping == null)
                {
                    mapping = _mappings.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.ExecutableName) &&
                        m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normGame ?? "", StringComparison.OrdinalIgnoreCase));
                }

                if (mapping != null)
                {
                    mapping.ProfilePath = null;

                    if (string.IsNullOrEmpty(mapping.GamePadProfilePath))
                    {
                        _mappings.Remove(mapping);
                        SimpleLogger.Instance.Info($"Removed executable '{exeName}'" + (normGame != null ? $" [game: {normGame}]" : "") + " from mappings JSON (mouse Auto-load unchecked, no remaining association).");
                    }
                    else
                    {
                        SimpleLogger.Instance.Info($"Removed mouse profile link for executable '{exeName}' (GamePad profile link kept).");
                    }
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
                ReloadIfChangedExternallyCore(); // [V52b] Never mutate a stale copy

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
                ReloadIfChangedExternallyCore(); // [V52b] Never mutate a stale copy
                string normalizedPath = gamePadProfilePath?.Replace('\\', '/');
                var toUpdate = _mappings.Where(m => 
                    !string.IsNullOrEmpty(m.GamePadProfilePath) && 
                    m.GamePadProfilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();

                if (toUpdate.Any())
                {
                    foreach (var mapping in toUpdate)
                    {
                        mapping.GamePadProfilePath = null;
                    }

                    // [V32] Remove entries that have no link left at all (no mouse profile,
                    // no GamePad profile) so the JSON stays clean.
                    // (EN/FR: Retirer les entrées sans aucun lien restant (ni profil souris,
                    // ni profil GamePad) pour garder le JSON propre.)
                    var empty = toUpdate.Where(m => string.IsNullOrEmpty(m.ProfilePath)).ToList();
                    foreach (var mapping in empty) _mappings.Remove(mapping);

                    SaveMappings();
                    SimpleLogger.Instance.Info($"Removed GamePad profile link '{gamePadProfilePath}' from {toUpdate.Count} mapping(s)" + (empty.Any() ? $" ({empty.Count} empty mapping(s) deleted)." : "."));
                }
            }
        }

        /// <summary>
        /// Remove GamePad profile link for a specific executable (EN/FR: Supprimer lien profil GamePad pour un exécutable précis)
        /// EN: [V32] The executable is REMOVED from the JSON when the unchecked Auto-load
        /// link was its only association (no mouse ProfilePath left).
        /// FR: [V32] L'exécutable est RETIRÉ du JSON quand le lien Auto-load décoché était
        /// sa seule association (plus de ProfilePath souris).
        /// </summary>
        public static void RemoveGamePadProfileLinkForExecutable(string exeName, string exePath = null, string gameName = null)
        {
            lock (_lock)
            {
                ReloadIfChangedExternallyCore(); // [V52b] Never mutate a stale copy
                string normGame = string.IsNullOrEmpty(gameName) ? null : gameName.Trim();

                // Match by full path first, then by name (EN/FR: Correspondance par chemin complet puis par nom)
                GameProfileMapping mapping = null;
                if (!string.IsNullOrEmpty(exePath))
                {
                    mapping = _mappings.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.ExecutablePath) &&
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normGame ?? "", StringComparison.OrdinalIgnoreCase));
                }
                if (mapping == null)
                {
                    mapping = _mappings.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.ExecutableName) &&
                        m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.GameName ?? "", normGame ?? "", StringComparison.OrdinalIgnoreCase));
                }

                if (mapping != null)
                {
                    mapping.GamePadProfilePath = null;

                    // [V32] If the entry has no mouse profile link either, remove the
                    // executable from the JSON entirely (unchecked Auto-load = no association).
                    // (EN/FR: Si l'entrée n'a plus de lien profil souris non plus, retirer
                    // l'exécutable du JSON (Auto-load décoché = aucune association).)
                    if (string.IsNullOrEmpty(mapping.ProfilePath))
                    {
                        _mappings.Remove(mapping);
                        SimpleLogger.Instance.Info($"Removed executable '{exeName}'" + (normGame != null ? $" [game: {normGame}]" : "") + " from mappings JSON (Auto-load unchecked, no remaining association).");
                    }
                    else
                    {
                        SimpleLogger.Instance.Info($"Removed GamePad profile link for executable '{exeName}' (mouse profile link kept).");
                    }
                    SaveMappings();
                }
            }
        }

        /// <summary>
        /// EN: [V46] Capture the ES system selected when the link is created.
        /// FR: [V46] Capture le système ES sélectionné au moment de la création du lien.
        /// </summary>
        private static string CaptureSystemName()
        {
            try
            {
                string sys = EsScriptIntegration.LastSystem;
                return string.IsNullOrEmpty(sys) ? null : sys.Trim();
            }
            catch { return null; }
        }

        /// <summary>
        /// EN: [V46] Best mapping for an executable. Game-name scoring accepts BOTH variants:
        /// folder links store the name WITH extension (HOTD4SP.teknoparrot), file links the
        /// name WITHOUT extension (HOTD4SP). Passing both ES values makes folder links
        /// actually resolve (bug fix: profile never loaded for folder-based games).
        /// FR: [V46] Meilleur mapping pour un exécutable. Le score du nom de jeu accepte les
        /// DEUX variants : les liens dossier stockent le nom AVEC extension
        /// (HOTD4SP.teknoparrot), les liens fichier le nom SANS extension (HOTD4SP).
        /// Passer les deux valeurs ES fait réellement résoudre les liens dossier (fix :
        /// profil jamais chargé pour les jeux en dossiers).
        /// </summary>
        public static GameProfileMapping GetBestMapping(string exeName, string exePath = null, string esGameName = null, string esGameNameRaw = null)
        {
            lock (_lock)
            {
                ReloadIfChangedExternally(); // [V49] Keep in-memory state coherent with the disk file

                GameProfileMapping best = null;
                int bestScore = 0;

                foreach (var m in _mappings)
                {
                    if (!m.AutoLoad) continue;

                    bool exeMatch = false;
                    if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(m.ExecutablePath) &&
                        m.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        exeMatch = true;
                    }
                    else if (!string.IsNullOrEmpty(m.ExecutableName) &&
                             m.ExecutableName.Equals(exeName, StringComparison.OrdinalIgnoreCase))
                    {
                        // If this mapping has a path but we didn't match it above (and exePath was provided),
                        // it's a different game with the same exe name -> Don't load!
                        // (EN/FR: Si ce mapping a un chemin mais pas de match (et exePath fourni),
                        // c'est un autre jeu avec le même nom d'exe -> Ne pas charger!)
                        if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(m.ExecutablePath))
                            continue;
                        exeMatch = true;
                    }

                    if (!exeMatch) continue;

                    int score;
                    if (string.IsNullOrEmpty(m.GameName))
                    {
                        score = 1; // Generic exe link (EN/FR: Lien exe générique)
                    }
                    else if (!string.IsNullOrEmpty(esGameName) &&
                             m.GameName.Equals(esGameName, StringComparison.OrdinalIgnoreCase))
                    {
                        score = 2; // File-style match (name without extension) (EN/FR: Match style fichier)
                    }
                    else if (!string.IsNullOrEmpty(esGameNameRaw) &&
                             m.GameName.Equals(esGameNameRaw, StringComparison.OrdinalIgnoreCase))
                    {
                        score = 2; // Folder-style match (name with extension) (EN/FR: Match style dossier)
                    }
                    else
                    {
                        score = 0; // Game-scoped link for ANOTHER game (EN/FR: Lien jeu pour un AUTRE jeu)
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = m;
                        if (score == 2) break; // Exact game match (EN/FR: Match jeu exact)
                    }
                }

                return best;
            }
        }

        /// <summary>
        /// EN: Get the mouse profile for a game. [V46] esGameNameRaw resolves folder links.
        /// FR: Obtient le profil souris d'un jeu. [V46] esGameNameRaw résout les liens dossier.
        /// </summary>
        public static string GetProfileForGame(string exeName, string exePath = null, string esGameName = null, string esGameNameRaw = null)
        {
            lock (_lock)
            {
                return GetBestMapping(exeName, exePath, esGameName, esGameNameRaw)?.ProfilePath;
            }
        }

        public static string GetGamePadProfileForGame(string exeName, string exePath = null, string esGameName = null, string esGameNameRaw = null)
        {
            lock (_lock)
            {
                return GetBestMapping(exeName, exePath, esGameName, esGameNameRaw)?.GamePadProfilePath;
            }
        }

        /// <summary>
        /// EN: [V46] Find the mapping that links a profile (mouse or GamePad side).
        /// FR: [V46] Trouve le mapping qui lie un profil (côté souris ou GamePad).
        /// </summary>
        public static GameProfileMapping GetMappingByProfilePath(string profilePath, bool gamePadSide)
        {
            if (string.IsNullOrEmpty(profilePath)) return null;
            string norm = profilePath.Replace('\\', '/');

            lock (_lock)
            {
                return _mappings.FirstOrDefault(m =>
                {
                    string p = gamePadSide ? m.GamePadProfilePath : m.ProfilePath;
                    return !string.IsNullOrEmpty(p) && p.Replace('\\', '/').Equals(norm, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        /// <summary>
        /// EN: [V46] Distinct profile paths whose mapping was created for the given ES system.
        /// Used by the tile modal to STRICTLY filter profiles per system.
        /// FR: [V46] Chemins de profils distincts dont le mapping a été créé pour le système
        /// ES donné. Utilisé par la modale en tuiles pour filtrer STRICTEMENT par système.
        /// </summary>
        public static List<string> GetProfilePathsForSystem(string system, bool gamePadSide)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(system)) return result;

            lock (_lock)
            {
                foreach (var m in _mappings)
                {
                    if (m.SystemName == null || !m.SystemName.Equals(system, StringComparison.OrdinalIgnoreCase)) continue;
                    string p = (gamePadSide ? m.GamePadProfilePath : m.ProfilePath)?.Replace('\\', '/');
                    if (string.IsNullOrEmpty(p)) continue;
                    if (!result.Any(x => x.Equals(p, StringComparison.OrdinalIgnoreCase)))
                        result.Add(p);
                }
            }
            return result;
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
        /// EN: [V35] Get ALL executables linked to a GamePad profile (not only the first).
        /// Used by the UI so the info text never hides a second (e.g. legacy) link.
        /// FR: [V35] Obtenir TOUS les exécutables liés à un profil GamePad (pas seulement
        /// le premier). Utilisé par l'UI pour que le texte d'info ne cache jamais un
        /// deuxième lien (ex: hérité).
        /// </summary>
        public static List<string> GetExecutablesForGamePadProfile(string profilePath)
        {
            lock (_lock)
            {
                var result = new List<string>();
                string normalizedPath = profilePath?.Replace('\\', '/');
                if (string.IsNullOrEmpty(normalizedPath)) return result;

                foreach (var m in _mappings)
                {
                    if (m.AutoLoad &&
                        m.GamePadProfilePath?.Replace('\\', '/').Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.Add(!string.IsNullOrEmpty(m.ExecutablePath)
                            ? Path.GetFileName(m.ExecutablePath)
                            : m.ExecutableName);
                    }
                }
                return result;
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

                    // [V52b] Auto-heal legacy JSON: the ROOT default.remap is never
                    // associable — strip such links and drop entries left empty.
                    // (EN/FR: Auto-réparation du JSON hérité : le default.remap RACINE
                    // n'est jamais associable — retirer ces liens et supprimer les
                    // entrées devenues vides.)
                    if (_mappings != null)
                    {
                        int healed = 0;
                        foreach (var m in _mappings)
                        {
                            if (IsRootDefaultProfilePath(m.ProfilePath)) { m.ProfilePath = null; healed++; }
                            if (IsRootDefaultProfilePath(m.GamePadProfilePath)) { m.GamePadProfilePath = null; healed++; }
                        }
                        _mappings.RemoveAll(m => string.IsNullOrEmpty(m.ProfilePath) && string.IsNullOrEmpty(m.GamePadProfilePath));
                        if (healed > 0)
                        {
                            SimpleLogger.Instance.Warning($"[V52b] Removed {healed} root default.remap link(s) from the JSON (the base mapping is never associable).");
                            SaveMappings();
                        }
                    }

                    // [V52b] Disk marker at LOAD time: an external (editor) edit made AFTER
                    // startup must be detectable even before the app ever writes — otherwise
                    // the first mutator would clobber it with a stale in-memory copy.
                    // (EN/FR: Marqueur disque au CHARGEMENT : une modification externe
                    // (éditeur) faite APRÈS le démarrage doit être détectable même avant
                    // toute écriture de l'app — sinon le premier mutateur l'écraserait
                    // avec une copie mémoire périmée.)
                    try { _lastOwnWriteUtc = File.GetLastWriteTimeUtc(jsonPath); } catch { }
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
                _lastOwnWriteUtc = File.GetLastWriteTimeUtc(jsonPath); // [V49] Own write marker
                SimpleLogger.Instance.Info($"[GameProfileMapping] Mappings saved to JSON: {jsonPath}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to save game mappings: {ex.Message}");
            }
        }

        // [V49] External change detection: an editor (VS Code / Notepad) holding a stale
        // copy can overwrite the JSON and resurrect removed associations. Reload from
        // disk when the file changed outside of this application so the UI and the file
        // always stay coherent.
        // (EN/FR: Détection de modification externe : un éditeur (VS Code / Notepad)
        // détenant une copie périmée peut écraser le JSON et ressusciter des associations
        // supprimées. Recharger depuis le disque quand le fichier a changé hors de cette
        // application, pour que l'UI et le fichier restent toujours cohérents.)
        private static DateTime _lastOwnWriteUtc = DateTime.MinValue;
        private static DateTime _lastExternalCheckUtc = DateTime.MinValue;

        public static void ReloadIfChangedExternally()
        {
            try
            {
                // Throttle: check at most every 2 seconds (EN/FR: Bride : max toutes les 2s)
                if ((DateTime.UtcNow - _lastExternalCheckUtc).TotalSeconds < 2.0) return;
                _lastExternalCheckUtc = DateTime.UtcNow;
                ReloadIfChangedExternallyCore();
            }
            catch { }
        }

        /// <summary>
        /// EN: [V52b] UNTHROTTLED disk sync used by every MUTATOR before it writes: a
        /// stale in-memory copy must never clobber an external (editor) edit or any
        /// newer disk state when the app saves another change.
        /// FR: [V52b] Synchro disque NON bridlée utilisée par chaque MUTATEUR avant
        /// d'écrire : une copie mémoire périmée ne doit jamais écraser une modification
        /// externe (éditeur) ou un état disque plus récent quand l'app sauve un changement.
        /// </summary>
        private static void ReloadIfChangedExternallyCore()
        {
            try
            {
                string remapDir = RemapProfileManager.GetRemapDirectory();
                string jsonPath = Path.Combine(remapDir, JsonMappingFileName);
                if (!File.Exists(jsonPath)) return;

                DateTime diskUtc = File.GetLastWriteTimeUtc(jsonPath);
                if (_lastOwnWriteUtc != DateTime.MinValue && diskUtc > _lastOwnWriteUtc.AddSeconds(1))
                {
                    SimpleLogger.Instance.Warning("[GameProfileMapping] JSON changed EXTERNALLY (editor?): reloading from disk");
                    LoadMappings();
                    _lastOwnWriteUtc = diskUtc;
                }
            }
            catch { }
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
                    sb.AppendLine($"    \"IsEmulator\": {(m.IsEmulator ? "true" : "false")},");
                    sb.AppendLine($"    \"GameName\": \"{Escape(m.GameName)}\",");
                    sb.AppendLine($"    \"GameIsFolder\": {(m.GameIsFolder ? "true" : "false")},");
                    sb.AppendLine($"    \"SystemName\": \"{Escape(m.SystemName)}\",");
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
                    m.GameName = ExtractValue(objStr, "GameName"); // [V42]
                    string isEmuStr = ExtractValue(objStr, "IsEmulator"); // [V42]
                    m.IsEmulator = isEmuStr != null && isEmuStr.ToLower() == "true";
                    string isFolderStr = ExtractValue(objStr, "GameIsFolder"); // [V43]
                    m.GameIsFolder = isFolderStr != null && isFolderStr.ToLower() == "true";
                    m.SystemName = ExtractValue(objStr, "SystemName"); // [V46]
                     
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
