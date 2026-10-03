using System;
using System.Collections.Generic;
using System.IO;
using WiimoteGun;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: Persistent memory of the emulator game-settings profiles managed by the
    /// "-wiimotegun" tag (mask = rename to ".ini-wiimotegun" in Wiimote/Mouse mode,
    /// unmask = rename back to ".ini" in GamePad mode so the emulator auto-loads
    /// the profile of the launched game).
    /// Rules ([V55q]):
    /// - A profile enters the memory ONLY when it is DETECTED TAGGED (it received
    ///   the tag at least once: from WiimoteGun's own mask cycle of a previous
    ///   build, or manually by the user), after a content safety check. An
    ///   untagged file NEVER enters the memory, whatever its content.
    /// - Once registered, the profile is managed FOR LIFE: the memory is the
    ///   source of truth and wins over any content check (an edited/rewritten
    ///   file keeps being masked/unmasked correctly). This is also the startup
    ///   crash-safety: a managed file left untagged by a crash while in GamePad
    ///   mode is re-masked in Wiimote/Mouse mode.
    /// - A profile never registered is IGNORED FOREVER: no tag is ever added to
    ///   it and no removal is ever attempted, whatever its content.
    /// The JSON file is SERVICE-keyed (extensible for any future service of the
    /// app) and separates each emulator (PCSX2 / Dolphin / DuckStation / future)
    /// in its own section. Unknown services/keys are preserved on save.
    /// FR: Mémoire persistante des profils de paramètres de jeu des émulateurs
    /// gérés par le tag "-wiimotegun" (masquage = renommage en ".ini-wiimotegun"
    /// en mode Wiimote/Souris, démasquage = renommage en ".ini" en mode GamePad
    /// pour que l'émulateur charge automatiquement le profil du jeu lancé).
    /// Règles ([V55q]) :
    /// - Un profil entre en mémoire UNIQUEMENT s'il est DÉTECTÉ TAGGÉ (il a
    ///   reçu le tag au moins une fois : d'un cycle de masquage WiimoteGun d'une
    ///   version précédente, ou manuellement par l'utilisateur), après une
    ///   vérification de sécurité du contenu. Un fichier non taggé n'entre
    ///   JAMAIS en mémoire, quel que soit son contenu.
    /// - Une fois enregistré, le profil est géré À VIE : la mémoire est la source
    ///   de vérité et prime sur toute vérification de contenu (un fichier
    ///   édité/réécrit continue d'être masqué/démasqué correctement). C'est
    ///   aussi la sécurité anti-crash au démarrage : un fichier géré laissé
    ///   sans tag par un crash en mode GamePad est re-masqué en mode
    ///   Wiimote/Souris.
    /// - Un profil jamais enregistré est IGNORÉ POUR TOUJOURS : aucun tag ne lui
    ///   est jamais ajouté et aucun retrait n'est jamais tenté, quel que soit
    ///   son contenu.
    /// Le fichier JSON est clé par SERVICE (évolutif pour tout futur service de
    /// l'app) et sépare chaque émulateur (PCSX2 / Dolphin / DuckStation / futur)
    /// dans sa propre section. Les services/clés inconnus sont préservés à la
    /// sauvegarde.
    /// </summary>
    public static class TagMemoryStore
    {
        // EN/FR: File stored next to game_profile_mappings.json in the remap directory
        private const string MemoryFileName = "wiimotegun_memory.json";
        private const string ServiceKey = "EmulatorTagManager";
        private const string EmulatorsKey = "Emulators";
        private const string ManagedFilesKey = "ManagedFiles";
        private const string SchemaVersion = "1";
        private const string TagSuffix = "-wiimotegun";

        public const string StateActive = "Active";
        public const string StateMasked = "Masked";

        private static readonly object _lock = new object();
        private static JsonValue _root;
        private static bool _loaded;
        private static bool _lastLoadFailed;
        private static DateTime _lastOwnWriteUtc = DateTime.MinValue;

        private static string MemoryPath()
        {
            return Path.Combine(RemapProfileManager.GetRemapDirectory(), MemoryFileName);
        }

        private static string Timestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            Load();
        }

        private static void Load()
        {
            _lastLoadFailed = false;
            try
            {
                string path = MemoryPath();
                if (File.Exists(path))
                {
                    _root = JsonLite.Parse(File.ReadAllText(path));
                    try { _lastOwnWriteUtc = File.GetLastWriteTimeUtc(path); } catch { }
                }
                else
                {
                    _root = null;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[TagMemory] Failed to read memory file: " + ex.Message);
                _root = null;
                _lastLoadFailed = true;
            }

            // EN: Missing or unreadable file -> rebuild a clean structure.
            // A corrupt file is backed up to ".bak" by Save() before healing.
            // FR: Fichier absent ou illisible -> reconstruire une structure propre.
            // Un fichier corrompu est sauvegardé en ".bak" par Save() avant réparation.
            if (_root == null || _root.Type != JsonValue.JsonType.Object)
            {
                _root = BuildEmptyRoot();
                Save();
            }
        }

        private static JsonValue BuildEmptyRoot()
        {
            JsonValue root = JsonValue.NewObject();
            root.Set("Version", JsonValue.NewString(SchemaVersion));
            JsonValue services = JsonValue.NewObject();
            services.Set(ServiceKey, BuildEmptyService());
            root.Set("Services", services);
            return root;
        }

        private static JsonValue BuildEmptyService()
        {
            JsonValue service = JsonValue.NewObject();
            service.Set(EmulatorsKey, JsonValue.NewObject());
            return service;
        }

        private static JsonValue GetEmulatorsNode()
        {
            JsonValue services = _root.GetOrAddObject("Services");
            JsonValue service = services.GetOrAddObject(ServiceKey);
            return service.GetOrAddObject(EmulatorsKey);
        }

        private static JsonValue GetManagedFilesArray(string emulator)
        {
            JsonValue emulators = GetEmulatorsNode();
            JsonValue emuNode = emulators.GetOrAddObject(emulator);
            return emuNode.GetOrAddArray(ManagedFilesKey);
        }

        private static JsonValue FindEntry(string emulator, string dir, string activeFileName)
        {
            JsonValue arr = GetManagedFilesArray(emulator);
            if (arr.Items == null) return null;
            for (int i = 0; i < arr.Items.Count; i++)
            {
                JsonValue entry = arr.Items[i];
                string eDir = entry.GetString("Dir", "");
                string eFile = entry.GetString("File", "");
                if (string.Equals(eDir, dir, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(eFile, activeFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// EN: True when the profile is registered in the memory (managed).
        /// FR: True si le profil est enregistré dans la mémoire (géré).
        /// </summary>
        public static bool IsRegistered(string emulator, string dir, string activeFileName)
        {
            lock (_lock)
            {
                EnsureLoaded();
                ReloadIfChangedExternallyCore();
                return FindEntry(emulator, dir, activeFileName) != null;
            }
        }

        /// <summary>
        /// EN: Register a profile as MANAGED. Returns true when it is newly
        /// registered (false when it was already in the memory).
        /// FR: Enregistre un profil comme GÉRÉ. Retourne true s'il vient d'être
        /// enregistré (false s'il était déjà en mémoire).
        /// </summary>
        public static bool Register(string emulator, string dir, string activeFileName, string initialState)
        {
            lock (_lock)
            {
                EnsureLoaded();
                ReloadIfChangedExternallyCore();
                if (FindEntry(emulator, dir, activeFileName) != null) return false;

                JsonValue entry = JsonValue.NewObject();
                entry.Set("Dir", JsonValue.NewString(dir));
                entry.Set("File", JsonValue.NewString(activeFileName));
                entry.Set("RegisteredUtc", JsonValue.NewString(Timestamp()));
                entry.Set("LastState", JsonValue.NewString(initialState));
                entry.Set("LastChangeUtc", JsonValue.NewString(Timestamp()));
                GetManagedFilesArray(emulator).Items.Add(entry);
                Save();

                SimpleLogger.Instance.Info(string.Format("[TagMemory] Registered managed {0} profile: {1} (state: {2})",
                    emulator, Path.Combine(dir, activeFileName), initialState));
                return true;
            }
        }

        /// <summary>
        /// EN: Update the recorded state (Active/Masked) of a registered profile.
        /// No-op when unchanged or not registered (defensive).
        /// FR: Met à jour l'état enregistré (Active/Masked) d'un profil enregistré.
        /// Sans effet si inchangé ou non enregistré (défensif).
        /// </summary>
        public static void SetState(string emulator, string dir, string activeFileName, string state)
        {
            lock (_lock)
            {
                EnsureLoaded();
                ReloadIfChangedExternallyCore();
                JsonValue entry = FindEntry(emulator, dir, activeFileName);
                if (entry == null) return;
                string current = entry.GetString("LastState", "");
                if (string.Equals(current, state, StringComparison.OrdinalIgnoreCase)) return;
                entry.Set("LastState", JsonValue.NewString(state));
                entry.Set("LastChangeUtc", JsonValue.NewString(Timestamp()));
                Save();
            }
        }

        /// <summary>
        /// EN: Remove the memory entries of a scanned directory whose file no
        /// longer exists on disk (neither active nor masked form). Entries of
        /// other directories are never touched.
        /// FR: Retire de la mémoire les entrées d'un dossier scanné dont le
        /// fichier n'existe plus sur le disque (ni à l'état actif ni masqué).
        /// Les entrées d'autres dossiers ne sont jamais touchées.
        /// </summary>
        public static int PruneDirectory(string emulator, string dir)
        {
            lock (_lock)
            {
                EnsureLoaded();
                ReloadIfChangedExternallyCore();
                JsonValue arr = GetManagedFilesArray(emulator);
                if (arr.Items == null || arr.Items.Count == 0) return 0;

                int removed = arr.Items.RemoveAll(e =>
                {
                    string eDir = e.GetString("Dir", "");
                    if (!string.Equals(eDir, dir, StringComparison.OrdinalIgnoreCase)) return false;
                    string eFile = e.GetString("File", "");
                    string activePath = Path.Combine(dir, eFile);
                    return !File.Exists(activePath) && !File.Exists(activePath + TagSuffix);
                });

                if (removed > 0)
                {
                    Save();
                    SimpleLogger.Instance.Info(string.Format("[TagMemory] Pruned {0} deleted profile(s) from {1} memory ({2})", removed, emulator, dir));
                }
                return removed;
            }
        }

        private static void Save()
        {
            try
            {
                string path = MemoryPath();
                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

                // EN: A file that failed to parse is backed up before healing it.
                // FR: Un fichier dont l'analyse a échoué est sauvegardé avant réparation.
                if (_lastLoadFailed && File.Exists(path))
                {
                    try { File.Copy(path, path + ".bak", true); } catch { }
                }
                _lastLoadFailed = false;

                File.WriteAllText(path, JsonLite.Serialize(_root));
                try { _lastOwnWriteUtc = File.GetLastWriteTimeUtc(path); } catch { }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[TagMemory] Failed to save memory file: " + ex.Message);
            }
        }

        // EN/FR: [V52b pattern] Reload from disk when the file was edited
        // externally (editor), so a hand-edited memory is never clobbered by a
        // stale in-memory copy when the app saves another change.
        private static void ReloadIfChangedExternallyCore()
        {
            try
            {
                string path = MemoryPath();
                if (!File.Exists(path)) return;
                DateTime diskUtc = File.GetLastWriteTimeUtc(path);
                if (_lastOwnWriteUtc != DateTime.MinValue && diskUtc > _lastOwnWriteUtc.AddSeconds(1))
                {
                    SimpleLogger.Instance.Warning("[TagMemory] Memory file changed EXTERNALLY (editor?): reloading from disk");
                    Load();
                }
            }
            catch { }
        }
    }
}
