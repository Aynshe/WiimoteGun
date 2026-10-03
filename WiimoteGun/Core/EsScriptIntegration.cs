using System;
using System.IO;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// EN: EmulationStation (RetroBat) scripts integration.
    /// Receives game-start and system-selected values from ES scripts and exposes the
    /// current game/system to the profile auto-load and the tile modal.
    /// The scripts are installed in <RetroBat>\emulationstation\.emulationstation\scripts\
    /// (game-start / system-selected) and call this application with -esgamestart %* /
    /// -essystem %* so the values are forwarded to the running instance (see MessageWindow).
    /// FR: Intégration des scripts EmulationStation (RetroBat).
    /// Reçoit les valeurs game-start et system-selected des scripts ES et expose le
    /// jeu/système courant à l'auto-load des profils et à la modale en tuiles.
    /// Les scripts sont installés dans <RetroBat>\emulationstation\.emulationstation\scripts\
    /// (game-start / system-selected) et appellent cette application avec -esgamestart %* /
    /// -essystem %* afin que les valeurs soient transférées à l'instance en cours (voir MessageWindow).
    /// </summary>
    public static class EsScriptIntegration
    {
        // Command line arguments (EN/FR: Arguments de ligne de commande)
        public const string ArgGameStart = "-esgamestart";
        public const string ArgSystemSelected = "-essystem";
        public const string ArgGameEnd = "-esgameend"; // [V47] End of game: clears the game state

        // Temp files used to transfer values between instances (EN/FR: Fichiers temporaires de transfert entre instances)
        public static readonly string GameStartTempFile = Path.Combine(Path.GetTempPath(), "WiimoteGun_EsGameStart.tmp");
        public static readonly string SystemTempFile = Path.Combine(Path.GetTempPath(), "WiimoteGun_EsSystem.tmp");
        public static readonly string GameEndTempFile = Path.Combine(Path.GetTempPath(), "WiimoteGun_EsGameEnd.tmp"); // [V47]

        private static readonly object _lock = new object();

        // ═════════════════════════════════════════════════════════════════
        // ES STATE (EN/FR: ÉTAT ES)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>EN: Last system selected in ES (e.g. "mastersystem"). FR: Dernier système sélectionné dans ES.</summary>
        public static string LastSystem { get; private set; }

        /// <summary>EN: Last game ROM full path from ES game-start. FR: Chemin complet de la dernière rom (game-start ES).</summary>
        public static string LastGameRomPath { get; private set; }

        /// <summary>EN: Last game name (ROM file name without extension). FR: Nom du dernier jeu (nom de fichier rom sans extension).</summary>
        public static string LastGameName { get; private set; }

        /// <summary>
        /// EN: [V43] Last game file OR FOLDER name WITH extension (e.g. "Daytona USA.pc").
        /// Folder-based systems (TeknoParrot & co) launch games through folders with
        /// extensions (.pc/.game/.win): the folder name is the game identity.
        /// FR: [V43] Nom du dernier fichier OU dossier AVEC extension (ex: "Daytona USA.pc").
        /// Les systèmes à dossiers (TeknoParrot & co) lancent les jeux via des dossiers avec
        /// extensions (.pc/.game/.win) : le nom du dossier est l'identité du jeu.
        /// </summary>
        public static string LastGameNameRaw { get; private set; }

        /// <summary>EN: [V43] True when the ES game value points to a FOLDER (not a file). FR: [V43] True si la valeur jeu ES pointe sur un DOSSIER (pas un fichier).</summary>
        public static bool LastGameIsFolder { get; private set; }

        /// <summary>EN: UTC timestamp of the last game-start. FR: Horodatage UTC du dernier game-start.</summary>
        public static DateTime? LastGameStartUtc { get; private set; }

        /// <summary>
        /// EN: Raised when the user long-presses PLUS on any connected Wiimote
        /// (requests the profile tile modal). FR: Déclenché lors d'un appui long sur PLUS
        /// sur n'importe quelle wiimote connectée (demande d'ouverture de la modale profils).
        /// </summary>
        public static event Action TileModalRequested;

        /// <summary>EN: True when an ES game-start has been received (and not cleared). FR: True si un game-start ES a été reçu (non effacé).</summary>
        public static bool HasCurrentGame { get { lock (_lock) { return LastGameName != null; } } }

        // ═════════════════════════════════════════════════════════════════
        // VALUE PROCESSING (EN/FR: TRAITEMENT DES VALEURS)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// EN: Handle a game-start value: "<rom path>" "<game name>" "<...>".
        /// The first quoted token is the ROM path (any file extension, could be a .exe).
        /// FR: Traite une valeur game-start : "<chemin rom>" "<nom du jeu>" "<...>".
        /// Le premier token quoté est le chemin de la rom (extension quelconque, peut être un .exe).
        /// </summary>
        public static void HandleGameStart(string rawArgs)
        {
            string romPath = ExtractFirstToken(rawArgs);
            if (string.IsNullOrEmpty(romPath))
            {
                SimpleLogger.Instance.Warning($"[ES] game-start received but no ROM path could be extracted: {rawArgs}");
                return;
            }

            lock (_lock)
            {
                LastGameRomPath = romPath;
                LastGameName = Path.GetFileNameWithoutExtension(romPath);
                LastGameNameRaw = Path.GetFileName(romPath.TrimEnd('\\', '/')); // [V43] Folder/file name with extension
                try { LastGameIsFolder = Directory.Exists(romPath); } catch { LastGameIsFolder = false; } // [V43]
                LastGameStartUtc = DateTime.UtcNow;
            }

            EsLog($"game-start: system='{LastSystem}', game='{LastGameName}'" + (LastGameIsFolder ? $" (folder: '{LastGameNameRaw}')" : "") + $", rom='{romPath}', raw='{rawArgs}'");
            SimpleLogger.Instance.Info($"[ES] game-start: game='{LastGameName}' (see WiimoteGun_ES.log)");
        }

        /// <summary>
        /// EN: Handle a system-selected value (e.g. "mastersystem").
        /// FR: Traite une valeur system-selected (ex: "mastersystem").
        /// </summary>
        public static void HandleSystemSelected(string rawArgs)
        {
            string system = ExtractFirstToken(rawArgs);
            if (string.IsNullOrEmpty(system))
            {
                SimpleLogger.Instance.Warning($"[ES] system-selected received but no system name could be extracted: {rawArgs}");
                return;
            }

            lock (_lock)
            {
                LastSystem = system;
            }

            EsLog($"system-selected: '{system}', raw='{rawArgs}'");
            SimpleLogger.Instance.Info($"[ES] system-selected: '{system}'");
        }

        /// <summary>
        /// EN: Handle a game-end value: the game is over, clear the game state and wait
        /// for the next game-start. The ES scripts own the game state lifecycle.
        /// FR: Traite une valeur game-end : le jeu est terminé, efface l'état du jeu et
        /// attend le prochain game-start. Les scripts ES pilotent le cycle de vie.
        /// </summary>
        public static void HandleGameEnd(string rawArgs)
        {
            EsLog($"game-end received: raw='{rawArgs}'");
            ClearGameStart();
            SimpleLogger.Instance.Info("[ES] game-end: game state cleared, waiting for next game-start");
        }

        /// <summary>
        /// EN: Clear the current game (also called on game-end).
        /// FR: Efface le jeu courant (aussi appelé au game-end).
        /// </summary>
        public static void ClearGameStart()
        {
            lock (_lock)
            {
                LastGameRomPath = null;
                LastGameName = null;
                LastGameNameRaw = null; // [V43]
                LastGameIsFolder = false; // [V43]
                LastGameStartUtc = null;
            }
        }

        /// <summary>
        /// EN: Extract the first token of a raw %* value, respecting double quotes
        /// (special characters are never blocking). FR: Extrait le premier token d'une
        /// valeur %* brute, en respectant les guillemets (les caractères spéciaux ne bloquent jamais).
        /// </summary>
        private static string ExtractFirstToken(string rawArgs)
        {
            if (string.IsNullOrEmpty(rawArgs)) return null;
            rawArgs = rawArgs.Trim();
            if (rawArgs.Length == 0) return null;

            if (rawArgs[0] == '"')
            {
                int end = rawArgs.IndexOf('"', 1);
                if (end > 1) return rawArgs.Substring(1, end - 1);
                if (end == -1) return rawArgs.Substring(1); // Unterminated quote (EN/FR: Guillemet non fermé)
                return null;
            }

            // Unquoted: take up to the first space (EN/FR: Non quoté : jusqu'au premier espace)
            int sp = rawArgs.IndexOf(' ');
            return sp == -1 ? rawArgs : rawArgs.Substring(0, sp);
        }

        // ═════════════════════════════════════════════════════════════════
        // SCRIPTS MANAGEMENT (EN/FR: GESTION DES SCRIPTS)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// EN: ES scripts root: <RetroBat>\emulationstation\.emulationstation\scripts.
        /// Path is resolved dynamically from the registry (survives RetroBat moves).
        /// FR: Racine des scripts ES : <RetroBat>\emulationstation\.emulationstation\scripts.
        /// Chemin résolu dynamiquement depuis le registre (survit aux déplacements RetroBat).
        /// </summary>
        public static string GetEsScriptsRoot()
        {
            string retroBat = RemapProfileManager.TryGetRetroBatRegistryPath();
            if (string.IsNullOrEmpty(retroBat)) return null;
            return Path.Combine(retroBat, "emulationstation", ".emulationstation", "scripts");
        }

        public static string GameStartScriptPath
        {
            get { string root = GetEsScriptsRoot(); return root == null ? null : Path.Combine(root, "game-start", "wiimotegun_es.bat"); }
        }

        public static string SystemSelectedScriptPath
        {
            get { string root = GetEsScriptsRoot(); return root == null ? null : Path.Combine(root, "system-selected", "wiimotegun_es.bat"); }
        }

        public static string GameEndScriptPath
        {
            get { string root = GetEsScriptsRoot(); return root == null ? null : Path.Combine(root, "game-end", "wiimotegun_es.bat"); } // [V47]
        }

        /// <summary>
        /// EN: Install or refresh the two ES scripts (called at startup and when the option
        /// is enabled in Options). Scripts are rewritten whenever missing or when their
        /// content no longer points at the current application path (dynamic paths).
        /// FR: Installe ou rafraîchit les deux scripts ES (appelé au démarrage et à
        /// l'activation de l'option). Les scripts sont réécrits s'ils manquent ou si leur
        /// contenu ne pointe plus vers le chemin actuel de l'application (chemins dynamiques).
        /// </summary>
        public static bool EnsureScriptsInstalled()
        {
            string gameStart = GameStartScriptPath;
            string systemSelected = SystemSelectedScriptPath;
            string gameEnd = GameEndScriptPath; // [V47]
            if (gameStart == null || systemSelected == null || gameEnd == null)
            {
                SimpleLogger.Instance.Info("[ES] RetroBat not found in registry, scripts not installed");
                return false;
            }

            string appPath = System.Windows.Forms.Application.ExecutablePath;
            bool ok = true;
            ok &= WriteScriptIfNeeded(gameStart, BuildScriptContent(appPath, ArgGameStart, "game-start"));
            ok &= WriteScriptIfNeeded(systemSelected, BuildScriptContent(appPath, ArgSystemSelected, "system-selected"));
            ok &= WriteScriptIfNeeded(gameEnd, BuildScriptContent(appPath, ArgGameEnd, "game-end")); // [V47]
            return ok;
        }

        /// <summary>EN: Remove the ES scripts (option unchecked). FR: Supprime les scripts ES (option décochée).</summary>
        public static void RemoveScripts()
        {
            TryDelete(GameStartScriptPath);
            TryDelete(SystemSelectedScriptPath);
            TryDelete(GameEndScriptPath); // [V47]
            SimpleLogger.Instance.Info("[ES] Scripts removed (option disabled)");
        }

        /// <summary>EN: True when the scripts exist and point at the current application. FR: True si les scripts existent et pointent vers l'application actuelle.</summary>
        public static bool AreScriptsValid()
        {
            string appPath = System.Windows.Forms.Application.ExecutablePath;
            string gs = GameStartScriptPath;
            string ss = SystemSelectedScriptPath;
            string ge = GameEndScriptPath; // [V47]
            if (gs == null || ss == null || ge == null) return false;
            return File.Exists(gs) && File.ReadAllText(gs).Contains(appPath)
                && File.Exists(ss) && File.ReadAllText(ss).Contains(appPath)
                && File.Exists(ge) && File.ReadAllText(ge).Contains(appPath); // [V47]
        }

        /// <summary>
        /// EN: Startup entry point: install/verify or remove scripts according to the option,
        /// then consume any pending ES values that arrived while no instance was running.
        /// FR: Point d'entrée démarrage : installe/vérifie ou supprime les scripts selon
        /// l'option, puis consomme les valeurs ES en attente reçues sans instance en cours.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (Options.Instance.EsScriptsEnabled)
                    EnsureScriptsInstalled();
                else if (File.Exists(GameStartScriptPath) || File.Exists(SystemSelectedScriptPath))
                    RemoveScripts();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ES] Failed to manage scripts: {ex.Message}");
            }

            ProcessPendingTempFiles();
        }

        /// <summary>
        /// EN: Consume pending temp files (values sent by scripts while no instance was
        /// running). FR: Consomme les fichiers temporaires en attente (valeurs envoyées
        /// par les scripts alors qu'aucune instance ne tournait).
        /// </summary>
        public static void ProcessPendingTempFiles()
        {
            try
            {
                if (File.Exists(GameStartTempFile))
                {
                    HandleGameStart(File.ReadAllText(GameStartTempFile));
                    File.Delete(GameStartTempFile);
                }
                if (File.Exists(SystemTempFile))
                {
                    HandleSystemSelected(File.ReadAllText(SystemTempFile));
                    File.Delete(SystemTempFile);
                }
                if (File.Exists(GameEndTempFile)) // [V47]
                {
                    HandleGameEnd(File.ReadAllText(GameEndTempFile));
                    File.Delete(GameEndTempFile);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning($"[ES] Failed to process pending values: {ex.Message}");
            }
        }

        private static bool WriteScriptIfNeeded(string scriptPath, string content)
        {
            try
            {
                if (File.Exists(scriptPath))
                {
                    string existing = File.ReadAllText(scriptPath);
                    if (existing == content)
                    {
                        SimpleLogger.Instance.Info($"[ES] Script OK: {scriptPath}");
                        return true;
                    }
                }

                string dir = Path.GetDirectoryName(scriptPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(scriptPath, content);
                SimpleLogger.Instance.Info($"[ES] Script installed: {scriptPath}");
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ES] Failed to write script {scriptPath}: {ex.Message}");
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try { if (path != null && File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { SimpleLogger.Instance.Warning($"[ES] Failed to delete {path}: {ex.Message}"); }
        }

        private static string BuildScriptContent(string appPath, string argument, string eventName)
        {
            // NOTE: %* passes all ES arguments verbatim, including quotes and special characters.
            // (EN/FR: %* transmet tous les arguments ES tels quels, guillemets et caractères spéciaux inclus.)
            return "@echo off\r\n" +
                   "rem Wiimote4Guns EmulationStation integration - " + eventName + "\r\n" +
                   "rem Auto-generated by WiimoteGun, do not edit (path is refreshed automatically)\r\n" +
                   "start \"\" /B \"" + appPath + "\" " + argument + " %*\r\n" +
                   "exit\r\n";
        }

        /// <summary>
        /// EN: [V50] ES system names = the folders of <RetroBat>\roms\ (teknoparrot, switch, ps2...).
        /// FR: [V50] Noms des systèmes ES = les dossiers de <RetroBat>\roms\ (teknoparrot, switch, ps2...).
        /// </summary>
        public static System.Collections.Generic.List<string> GetEsSystemNames()
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                string retroBat = RemapProfileManager.TryGetRetroBatRegistryPath();
                if (string.IsNullOrEmpty(retroBat)) return result;
                string romsDir = Path.Combine(retroBat, "roms");
                if (!Directory.Exists(romsDir)) return result;

                foreach (string dir in Directory.GetDirectories(romsDir))
                {
                    try
                    {
                        var attr = File.GetAttributes(dir);
                        if ((attr & FileAttributes.Hidden) != 0) continue;
                        result.Add(Path.GetFileName(dir));
                    }
                    catch { }
                }
                result.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return result;
        }

        /// <summary>
        /// EN: [V50] Resolve a game name for an ADVANCED association (outside a game session):
        /// reuses the remembered selection when present (no double prompt), otherwise asks.
        /// FR: [V50] Résout un nom de jeu pour une association AVANCÉE (hors session de jeu) :
        /// réutilise la sélection mémorisée si présente (pas de double demande), sinon demande.
        /// </summary>
        public static bool TryResolveGameNameAdvanced(bool folderExpected, IWin32Window owner,
            string rememberedGameName, bool rememberedIsFolder,
            out string gameName, out bool isFolder)
        {
            // Remembered selection: reuse without prompting
            // (EN/FR: Sélection mémorisée : réutiliser sans redemander)
            if (!string.IsNullOrEmpty(rememberedGameName))
            {
                gameName = rememberedGameName;
                isFolder = rememberedIsFolder;
                return true;
            }
            return TryResolveGameName(folderExpected, owner, out gameName, out isFolder);
        }

        /// <summary>
        /// EN: [V44] Resolve the game name for a link. When a game is in progress (received
        /// via ES game-start), returns it automatically. When NO game is running, lets the
        /// user pick the game FILE or FOLDER in advance (file without extension / folder
        /// name with extension), so an emulator can be associated before launching.
        /// FR: [V44] Résout le nom de jeu pour un lien. Si un jeu est en cours (reçu via
        /// game-start ES), le retourne automatiquement. Sans jeu en cours, permet à
        /// l'utilisateur de choisir le FICHIER ou le DOSSIER du jeu à l'avance (fichier
        /// sans extension / nom de dossier avec extension), pour associer un émulateur
        /// avant lancement.
        /// </summary>
        public static bool TryResolveGameName(bool folderExpected, IWin32Window owner, out string gameName, out bool isFolder)
        {
            gameName = null;
            isFolder = false;

            lock (_lock)
            {
                if (LastGameName != null)
                {
                    // Game in progress: use the ES value (EN/FR: Jeu en cours : valeur ES)
                    if (folderExpected)
                    {
                        if (!LastGameIsFolder)
                        {
                            gameName = null;
                            return false; // Caller warns: ES value is a file (EN/FR: L'appelant avertit : la valeur ES est un fichier)
                        }
                        gameName = LastGameNameRaw;
                        isFolder = true;
                    }
                    else
                    {
                        gameName = LastGameName;
                        isFolder = false;
                    }
                    return true;
                }
            }

            // No game running: manual selection in advance
            // (EN/FR: Pas de jeu en cours : sélection manuelle à l'avance)
            try
            {
                if (folderExpected)
                {
                    // [V47] Internal dark-themed folder explorer instead of the legacy dialog
                    // (EN/FR: Explorateur de dossiers interne thème sombre au lieu du dialogue historique)
                    string initial = LastGameRomPath != null && Directory.Exists(Path.GetDirectoryName(LastGameRomPath))
                        ? Path.GetDirectoryName(LastGameRomPath)
                        : null;
                    // [V55v] Trace before opening the dialog: if the app freezes inside the
                    // shell exploration, this is the last line of the log (diagnostic).
                    // (EN/FR: Trace avant l'ouverture du dialogue : si l'app gèle dans
                    // l'exploration shell, c'est la dernière ligne du log (diagnostic).)
                    SimpleLogger.Instance.Info("[ES] Opening game FOLDER selection dialog (FolderBrowserEx)...");
                    string selected = UI.Modern.Forms.FolderBrowserExForm.Show(owner,
                        "Select the game FOLDER (its name with extension will identify the game)", initial);
                    if (string.IsNullOrEmpty(selected)) return false;
                    gameName = Path.GetFileName(selected.TrimEnd('\\', '/'));
                    isFolder = true;
                    SimpleLogger.Instance.Info($"[ES] Manual game folder selected: '{gameName}'");
                    return !string.IsNullOrEmpty(gameName);
                }
                else
                {
                    // [V55v] Trace before opening the dialog: a shell namespace hang
                    // (disconnected network drive, shell extension) freezes the whole
                    // app inside ShowDialog — this line identifies it in the log.
                    // (EN/FR: Trace avant l'ouverture du dialogue : un gel du namespace
                    // shell (lecteur réseau déconnecté, extension shell) fige toute
                    // l'app dans ShowDialog — cette ligne l'identifie dans le log.)
                    SimpleLogger.Instance.Info("[ES] Opening game FILE selection dialog (OpenFileDialog)...");
                    using (var ofd = new System.Windows.Forms.OpenFileDialog())
                    {
                        ofd.Title = "Select the game FILE (its name without extension will identify the game)";
                        ofd.Filter = "Game file (*.*)|*.*";
                        if (ofd.ShowDialog(owner) != DialogResult.OK) return false;
                        gameName = Path.GetFileNameWithoutExtension(ofd.FileName);
                        isFolder = false;
                        SimpleLogger.Instance.Info($"[ES] Manual game file selected: '{gameName}'");
                        return !string.IsNullOrEmpty(gameName);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ES] Manual game selection failed: {ex.Message}");
                return false;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // DEDICATED ES LOG (EN/FR: LOG ES DÉDIÉ)
        // Max 2 MB then rolled to *.log.old, same mechanism as WiimoteGun.log.
        // (EN/FR: Max 2 Mo puis basculé en *.log.old, même mécanisme que WiimoteGun.log.)
        // ═════════════════════════════════════════════════════════════════

        private static readonly object _esLogLock = new object();
        private static readonly string _esLogFilePath =
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "WiimoteGun_ES.log");

        /// <summary>
        /// EN: Write a line to the dedicated ES events log (WiimoteGun_ES.log).
        /// Rotates to .old above 2 MB.
        /// FR: Écrit une ligne dans le log dédié des événements ES (WiimoteGun_ES.log).
        /// Bascule en .old au-delà de 2 Mo.
        /// </summary>
        private static void EsLog(string message)
        {
            lock (_esLogLock)
            {
                try
                {
                    if (File.Exists(_esLogFilePath))
                    {
                        // Rotate above 2 MB (EN/FR: Rotation au-dessus de 2 Mo)
                        if (new FileInfo(_esLogFilePath).Length > 2.0 * 1024 * 1024)
                        {
                            string prevLog = _esLogFilePath + ".old";
                            try
                            {
                                if (File.Exists(prevLog)) File.Delete(prevLog);
                                File.Move(_esLogFilePath, prevLog);
                            }
                            catch { }
                        }
                    }
                    File.AppendAllText(_esLogFilePath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
                }
                catch { }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // TILE MODAL HOTKEY (EN/FR: HOTKEY MODALE EN TUILES)
        // ═════════════════════════════════════════════════════════════════

        internal static void RaiseTileModalRequested()
        {
            TileModalRequested?.Invoke();
        }
    }
}
