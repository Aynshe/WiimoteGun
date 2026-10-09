using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace WiimoteGun
{
    /// <summary>
    /// [3.1.0.1] EN: EMBEDDED UPDATE SCRIPTS - the executable IS the source of truth.
    ///     Both update scripts (WiimoteUpdate.ps1 at the app root, UpdateService.ps1 in
    ///     the service folder) are EMBEDDED as resources in WiimoteGun.exe and written to
    ///     disk at every app start when the on-disk copy differs from the embedded one.
    ///     This permanently solves the "broken script survives updates" class of bugs
    ///     (the 3.1.0.0 transition shipped a buggy WiimoteUpdate.ps1 that skipped the
    ///     service subfolders, and pre-V57d UpdateService.ps1 copies stayed live forever):
    ///     update archives no longer need to carry the PS1s - the binary regenerates them
    ///     matching its own version, every time.
    ///     FR: SCRIPTS DE MISE À JOUR EMBARQUÉS - l'exécutable EST la source de vérité.
    ///     Les deux scripts de mise à jour (WiimoteUpdate.ps1 à la racine de l'app,
    ///     UpdateService.ps1 dans le dossier du service) sont EMBARQUÉS en ressources
    ///     dans WiimoteGun.exe et écrits sur disque à chaque démarrage de l'app quand la
    ///     copie disque diffère de la copie embarquée. Ceci règle définitivement la
    ///     classe de bugs « un script bogué survit aux mises à jour » (la transition
    ///     3.1.0.0 a livré un WiimoteUpdate.ps1 bogué qui sautait les sous-dossiers du
    ///     service, et des copies pré-V57d d'UpdateService.ps1 sont restées vivantes
    ///     pour toujours) : les archives de mise à jour n'ont plus besoin de transporter
    ///     les PS1 - le binaire les régénère conformes à sa propre version, à chaque fois.
    /// </summary>
    public static class ScriptRegenerator
    {
        /// <summary>
        /// EN: Write the embedded WiimoteUpdate.ps1 and UpdateService.ps1 to disk when
        ///     the on-disk copy differs from the embedded one. Called at every app start
        ///     (cheap: content comparison, no-op when identical). Never throws.
        ///     FR: Écrit les WiimoteUpdate.ps1 et UpdateService.ps1 embarqués sur disque
        ///     quand la copie disque diffère de l'embarquée. Appelé à chaque démarrage
        ///     de l'app (peu coûteux : comparaison de contenu, no-op à l'identique).
        ///     Ne lève jamais d'exception.
        /// </summary>
        public static void Regenerate()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                // EN/FR: [0] = WiimoteUpdate.ps1 -> app root; [1] = UpdateService.ps1 -> service folder
                WriteIfDifferent("WiimoteGun.WiimoteUpdate.ps1", Path.Combine(appDir, "WiimoteUpdate.ps1"));
                WriteIfDifferent("WiimoteGun.UpdateService.ps1", Path.Combine(appDir, "WiimoteGun.Service", "UpdateService.ps1"));
            }
            catch (Exception ex)
            {
                // EN/FR: Never break app startup over the script regeneration
                SimpleLogger.Instance.Warning("[3.1.0.1] ScriptRegenerator skipped: " + ex.Message);
            }
        }

        /// <summary>
        /// EN: Write an embedded text resource to a target path when the target differs
        ///     (missing or different content). BOM-safe: the embedded resource carries the
        ///     script bytes verbatim.
        ///     FR: Écrit une ressource texte embarquée vers un chemin cible quand la cible
        ///     diffère (absente ou contenu différent). Sûr pour le BOM : la ressource
        ///     embarquée porte les octets du script verbatim.
        /// </summary>
        private static void WriteIfDifferent(string resourceName, string targetPath)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        SimpleLogger.Instance.Warning("[3.1.0.1] Embedded script not found: " + resourceName);
                        return;
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        byte[] embedded = ms.ToArray();

                        // EN/FR: Compare with the on-disk copy (byte-exact) - no-op when identical
                        if (File.Exists(targetPath))
                        {
                            byte[] disk = File.ReadAllBytes(targetPath);
                            if (disk.Length == embedded.Length)
                            {
                                bool same = true;
                                for (int i = 0; i < disk.Length; i++)
                                {
                                    if (disk[i] != embedded[i]) { same = false; break; }
                                }
                                if (same) return; // EN/FR: identical - nothing to do
                            }
                        }

                        File.WriteAllBytes(targetPath, embedded);
                        SimpleLogger.Instance.Info("[3.1.0.1] Update script regenerated from the binary: " + Path.GetFileName(targetPath));
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning("[3.1.0.1] Could not regenerate " + Path.GetFileName(targetPath) + ": " + ex.Message);
            }
        }
    }
}
