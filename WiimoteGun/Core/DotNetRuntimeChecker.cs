using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WiimoteGun.Core
{
    /// <summary>
    /// [V57i] EN: Detects whether the machine has the .NET 10 runtime (Microsoft.NETCore.App
    ///     10.x, x64) installed - the runtime HmHost (the UMDF2 virtual input host, a
    ///     plain .NET 10 x64 console app) needs to start. Per user decision the
    ///     deployment is FRAMEWORK-DEPENDENT: the runtime is a machine prerequisite and
    ///     WiimoteGun proposes its download at the RawInput (UMDF2) mode activation.
    ///     The probe mirrors the apphost's own resolution order (no process spawn):
    ///       1. DOTNET_ROOT_X64 / DOTNET_ROOT environment locations
    ///       2. The x64 install location registered in HKLM\SOFTWARE\dotnet\Setup\InstalledVersions
    ///       3. The well-known default %ProgramFiles%\dotnet
    ///     and looks for shared\Microsoft.NETCore.App\v10.* directories (preview/RC
    ///     builds are excluded - they cannot serve a release-targeted app).
    ///     FR: Détecte si la machine possède le runtime .NET 10 (Microsoft.NETCore.App
    ///     10.x, x64) - le runtime dont HmHost (l'hôte d'entrée virtuelle UMDF2, simple
    ///     application console .NET 10 x64) a besoin pour démarrer. Par décision
    ///     utilisateur le déploiement est FRAMEWORK-DEPENDENT : le runtime est un
    ///     prérequis machine et WiimoteGun propose son téléchargement à l'activation du
    ///     mode RawInput (UMDF2). La sonde reflète l'ordre de résolution propre de
    ///     l'apphost (aucun spawn de processus) :
    ///       1. Emplacements DOTNET_ROOT_X64 / DOTNET_ROOT (variables d'environnement)
    ///       2. L'emplacement d'installation x64 enregistré dans
    ///          HKLM\SOFTWARE\dotnet\Setup\InstalledVersions
    ///       3. L'emplacement par défaut bien connu %ProgramFiles%\dotnet
    ///     et cherche les dossiers shared\Microsoft.NETCore.App\v10.* (les builds
    ///     preview/RC sont exclus - elles ne peuvent pas servir une app ciblée en release).
    /// </summary>
    public static class DotNetRuntimeChecker
    {
        /// <summary>
        /// EN: True when a usable .NET 10 runtime (Microsoft.NETCore.App 10.x release)
        ///     is present on this machine (x64 resolution, same order as the apphost).
        ///     Last resort: asks `dotnet --list-runtimes` when every filesystem candidate
        ///     came back empty (exotic layouts), so a proper install is never missed.
        ///     FR: Vrai quand un runtime .NET 10 utilisable (Microsoft.NETCore.App 10.x
        ///     release) est présent sur cette machine (résolution x64, même ordre que
        ///     l'apphost). En dernier recours : interroge `dotnet --list-runtimes` quand
        ///     tous les candidats filesystem sont vides (layouts exotiques), pour ne
        ///     jamais rater une installation correcte.
        /// </summary>
        public static bool IsNet10RuntimeInstalled()
        {
            foreach (string root in CandidateRoots())
            {
                try
                {
                    if (HasUsableNetCoreApp10(root))
                        return true;
                }
                catch
                {
                    // EN/FR: Unreadable candidate -> try the next one
                }
            }

            // EN: Last resort - the authoritative list from the dotnet host itself.
            //     FR: Dernier recours - la liste faisant foi, donnée par l'hôte dotnet.
            return ProbeDotnetListRuntimes();
        }

        // EN: Runs `dotnet --list-runtimes` and looks for "Microsoft.NETCore.App 10.".
        //     Covers exotic install layouts the filesystem probe may have missed. A
        //     missing/broken dotnet host returns false (the filesystem probe already
        //     covered the standard layouts).
        //     FR: Lance `dotnet --list-runtimes` et cherche « Microsoft.NETCore.App 10. ».
        //     Couvre les layouts d'installation exotiques que la sonde filesystem peut
        //     avoir ratés. Un hôte dotnet absent/cassé renvoie faux (la sonde filesystem
        //     couvre déjà les layouts standards).
        private static bool ProbeDotnetListRuntimes()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    if (proc == null)
                        return false;
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(5000);
                    foreach (string line in output.Split('\n'))
                    {
                        // EN: "Microsoft.NETCore.App 10.0.12 [C:\...\shared\Microsoft.NETCore.App]"
                        //     FR: "Microsoft.NETCore.App 10.0.12 [C:\...\shared\Microsoft.NETCore.App]"
                        if (line.TrimStart().StartsWith("Microsoft.NETCore.App 10.", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch
            {
                // EN/FR: dotnet not on PATH or spawn failed -> not detected this way
            }
            return false;
        }

        private static IEnumerable<string> CandidateRoots()
        {
            // 1. Environment overrides (custom install locations)
            //    (EN/FR: Redéfinitions d'environnement (emplacements d'installation personnalisés))
            foreach (string envVar in new[] { "DOTNET_ROOT_X64", "DOTNET_ROOT" })
            {
                string p = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                    yield return p;
            }

            // 2. The x64 install location registered by the .NET installer
            //    (EN/FR: L'emplacement d'installation x64 enregistré par l'installeur .NET)
            string registryLocation = null;
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64"))
                {
                    if (key != null)
                        registryLocation = key.GetValue("InstallLocation") as string;
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(registryLocation) && Directory.Exists(registryLocation))
                yield return registryLocation;

            // 3. The well-known default location (machine-wide x64 installs)
            //    (EN/FR: L'emplacement par défaut bien connu (installations x64 machine))
            yield return Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\dotnet");
        }

        private static bool HasUsableNetCoreApp10(string dotnetRoot)
        {
            string fxDir = Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App");
            if (!Directory.Exists(fxDir))
                return false;

            // [V57i-FIX] EN: The shared runtime folders are named WITHOUT the 'v' prefix
            //     (dotnet --list-runtimes shows "Microsoft.NETCore.App 10.0.12
            //     [C:\...\shared\Microsoft.NETCore.App]" -> the folder on disk is
            //     "10.0.12", NOT "v10.0.12"). The original "v10.*" glob found NOTHING and
            //     made the check fail on machines that DO have .NET 10 (the user's bug
            //     report). Both forms are checked; the version parse below is the real gate.
            //     FR: Les dossiers des runtimes partagés sont nommés SANS le préfixe « v »
            //     (dotnet --list-runtimes affiche « Microsoft.NETCore.App 10.0.12
            //     [C:\...\shared\Microsoft.NETCore.App] » -> le dossier disque est
            //     « 10.0.12 », PAS « v10.0.12 »). Le glob « v10.* » initial ne trouvait
            //     RIEN et faisait échouer la vérification sur des machines qui ONT .NET 10
            //     (bug rapporté par l'utilisateur). Les deux formes sont testées ; le
            //     parsage de version ci-dessous reste le vrai garde-fou.
            foreach (string dir in Directory.GetDirectories(fxDir, "10.*")
                .Concat(Directory.GetDirectories(fxDir, "v10.*")))
            {
                string verText = Path.GetFileName(dir).TrimStart('v');

                // EN: Preview / RC builds ("10.0.0-rc.2...", "10.0.0-preview...") cannot
                //     serve a release-targeted app - skip anything with a prerelease dash.
                //     FR: Les builds preview / RC (« 10.0.0-rc.2... », « 10.0.0-preview... »)
                //     ne peuvent pas servir une app ciblée en release - sauter tout ce qui
                //     porte un tiret de préversion.
                if (verText.IndexOf('-') >= 0)
                    continue;

                Version v;
                // EN: Major must be EXACTLY 10: the apphost's rollForward policy serves a
                //     net10.0 app from any 10.x release runtime, never from another major.
                //     FR: Le major doit être EXACTEMENT 10 : la politique rollForward de
                //     l'apphost sert une app net10.0 depuis n'importe quel runtime 10.x
                //     release, jamais depuis un autre major.
                if (Version.TryParse(verText, out v) && v.Major == 10)
                    return true;
            }
            return false;
        }
    }
}
