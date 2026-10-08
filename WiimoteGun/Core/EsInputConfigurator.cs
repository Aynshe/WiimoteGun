using System;
using System.IO;
using System.Text;

namespace WiimoteGun
{
    /// <summary>
    /// [V57q] EN: Registers the Wiimote4Guns UMDF2 DInput virtual gamepads
    ///     ("GamePad Wiimote4Guns P1..P4") in EmulationStation's es_input.cfg so ES
    ///     detects and binds them automatically. Based on the user's proven vmultia
    ///     entry, with: the CURRENT device identity (PID 0xBA1D, product-string name)
    ///     and the UP/DOWN STICK AXES INVERTED (user request - as tested on his setup).
    ///     SAFETY FIRST: this is a TEXT-LEVEL patch - the file is NEVER re-serialized
    ///     (no XML writer that would reformat it), existing content is never modified,
    ///     a one-time backup is taken before the first patch, the original BOM is
    ///     preserved, and any anomaly (missing </inputList>, locked file) aborts
    ///     without writing anything.
    ///     FR: Enregistre les gamepads virtuels DInput UMDF2 de Wiimote4Guns
    ///     (« GamePad Wiimote4Guns P1..P4 ») dans l'es_input.cfg d'EmulationStation
    ///     pour qu'ES les détecte et les binde automatiquement. Basé sur l'entrée
    ///     vmultia validée par l'utilisateur, avec : l'identité COURANTE des devices
    ///     (PID 0xBA1D, nom = chaîne produit) et les AXES HAUT/BAS DES STICKS INVERSÉS
    ///     (demande utilisateur - conforme à son test sur sa machine). SÉCURITÉ
    ///     D'ABORD : patch au niveau TEXTE - le fichier n'est JAMAIS re-sérialisé
    ///     (aucun writer XML qui le reformaterait), le contenu existant n'est jamais
    ///     modifié, une sauvegarde unique est prise avant le premier patch, le BOM
    ///     d'origine est préservé, et toute anomalie (</inputList> manquant, fichier
    ///     locké) interrompt sans rien écrire.
    /// </summary>
    public static class EsInputConfigurator
    {
        private static readonly string[] PlayerVidHexLe = { "1f", "2f", "3f", "4f" }; // EN/FR: VID 0x001F..0x004F as uint32 LE
        // [V57q-FIX] EN: The user-verified ES device GUID is 32 hex chars:
        //     "03000000" + VID-uint32LE (8) + PID-uint32LE (8) + "00000000" (8).
        //     ES detects "030000001f0000001dba000000000000" (P1) - the original code
        //     emitted 34 chars (unpadded PID + 14 zeros = 2 extra "00", never matched).
        //     FR: Le device GUID vérifié par l'utilisateur fait 32 caractères hexa :
        //     « 03000000 » + VID-uint32LE (8) + PID-uint32LE (8) + « 00000000 » (8).
        //     ES détecte « 030000001f0000001dba000000000000 » (P1) - le code initial
        //     produisait 34 caractères (PID non paddé + 14 zéros = 2 « 00 » en trop,
        //     jamais reconnu).
        private static string BuildGuid(int player)
        {
            return "03000000" + PlayerVidHexLe[player - 1] + "000000" + "1dba0000" + "00000000";
        }

        /// <summary>
        /// EN: Ensure the 4 gamepad entries exist in RetroBat's es_input.cfg. Idempotent:
        ///     players whose entry is already present (matched by deviceName) are skipped,
        ///     so user-customized entries are never touched. Safe: never writes unless the
        ///     file parses as an inputList (contains a closing tag).
        ///     FR: Garantit que les 4 entrées gamepad existent dans l'es_input.cfg de
        ///     RetroBat. Idempotent : les joueurs dont l'entrée est déjà présente (match
        ///     par deviceName) sont sautés, donc les entrées personnalisées par
        ///     l'utilisateur ne sont jamais touchées. Sûr : n'écrit jamais si le fichier
        ///     ne se présente pas comme un inputList (balise fermante présente).
        /// </summary>
        public static void EnsureGamepadEntries()
        {
            try
            {
                string retrobat = RemapProfileManager.TryGetRetroBatRegistryPath();
                if (string.IsNullOrEmpty(retrobat))
                    return;

                string esInputPath = Path.Combine(retrobat, "emulationstation", ".emulationstation", "es_input.cfg");
                if (!File.Exists(esInputPath))
                {
                    SimpleLogger.Instance.Info("[V57q] es_input.cfg not found (ES not installed or not run yet) - skipping the gamepad registration.");
                    return;
                }

                // EN: Read raw with BOM detection so the write-back is byte-identical
                //     outside our insertion.
                //     FR: Lecture brute avec détection du BOM pour que la réécriture soit
                //     identique à l'octet près hors notre insertion.
                byte[] raw = File.ReadAllBytes(esInputPath);
                bool hasBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
                string text = Encoding.UTF8.GetString(hasBom && raw.Length > 3 ? Sub(raw, 3, raw.Length - 3) : raw);
                if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1); // EN/FR: strip the decoded BOM char if any

                int closeIdx = text.LastIndexOf("</inputList>", StringComparison.Ordinal);
                if (closeIdx < 0)
                {
                    SimpleLogger.Instance.Warning("[V57q] es_input.cfg has no </inputList> - unexpected format, NOTHING written (no corruption risk).");
                    return;
                }

                StringBuilder toInsert = null;
                System.Collections.Generic.List<string> fixedPlayers = null;
                bool textModified = false;

                for (int player = 1; player <= 4; player++)
                {
                    string name = "GamePad Wiimote4Guns P" + player;
                    string nameAttr = "deviceName=\"" + name + "\"";
                    string guid = BuildGuid(player);

                    int nameIdx = text.IndexOf(nameAttr, StringComparison.Ordinal);
                    if (nameIdx < 0)
                    {
                        // EN/FR: Entry missing - insert it.
                        if (toInsert == null) toInsert = new StringBuilder();
                        toInsert.Append(BuildEntry(player, name, guid));
                        continue;
                    }

                    // [V57q-FIX] EN: Entry EXISTS (possibly the one written with the wrong
                    //     32-char GUID format): if its deviceGUID does not match the
                    //     correct 30-char one, REPLACE the whole inputConfig block -
                    //     otherwise (user-customized or already correct) never touch it.
                    //     FR: L'entrée EXISTE (peut-être celle écrite avec le mauvais
                    //     format de GUID 32 caractères) : si son deviceGUID ne correspond
                    //     pas au bon format de 30, REMPLACER tout le bloc inputConfig -
                    //     sinon (personnalisée par l'utilisateur ou déjà correcte) ne
                    //     jamais y toucher.
                    int blockStart = text.LastIndexOf("<inputConfig", nameIdx, StringComparison.Ordinal);
                    int blockClose = text.IndexOf("</inputConfig>", nameIdx, StringComparison.Ordinal);
                    if (blockStart < 0 || blockClose < 0)
                    {
                        SimpleLogger.Instance.Warning("[V57q] es_input.cfg: malformed existing entry for " + name + " - left untouched (no corruption risk).");
                        continue;
                    }

                    int blockEnd = blockClose + "</inputConfig>".Length;
                    string existingBlock = text.Substring(blockStart, blockEnd - blockStart);
                    if (existingBlock.IndexOf("deviceGUID=\"" + guid + "\"", StringComparison.Ordinal) >= 0)
                        continue; // EN/FR: correct GUID - leave it (idempotent)

                    text = text.Remove(blockStart, blockEnd - blockStart)
                               .Insert(blockStart, BuildEntry(player, name, guid));
                    textModified = true;
                    if (fixedPlayers == null) fixedPlayers = new System.Collections.Generic.List<string>();
                    fixedPlayers.Add(name);
                }

                if (fixedPlayers != null)
                {
                    File.WriteAllText(esInputPath, text, new UTF8Encoding(hasBom));
                    SimpleLogger.Instance.Info("[V57q] es_input.cfg: GUID format fixed (30-char ES format) for: " + string.Join(", ", fixedPlayers));
                }

                if (toInsert == null && !textModified)
                {
                    SimpleLogger.Instance.Debug("[V57q] es_input.cfg already has the 4 GamePad Wiimote4Guns entries - nothing to do.");
                    return;
                }

                if (toInsert != null)
                {
                    int closeIdx2 = text.LastIndexOf("</inputList>", StringComparison.Ordinal);
                    if (closeIdx2 < 0)
                    {
                        SimpleLogger.Instance.Warning("[V57q] es_input.cfg lost its </inputList> during the fix - insertion skipped (no corruption risk).");
                        return;
                    }

                    // EN: One-time backup before the FIRST patch of this file.
                    //     FR: Sauvegarde unique avant le PREMIER patch de ce fichier.
                    string backupPath = esInputPath + ".wiimote4guns.bak";
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(esInputPath, backupPath, overwrite: false);
                        SimpleLogger.Instance.Info("[V57q] es_input.cfg backup created: " + Path.GetFileName(backupPath));
                    }

                    text = text.Insert(closeIdx2, toInsert.ToString());
                    File.WriteAllText(esInputPath, text, new UTF8Encoding(hasBom));
                }

                SimpleLogger.Instance.Info("[V57q] es_input.cfg: GamePad Wiimote4Guns gamepad entries registered (30-char ES GUID format).");
            }
            catch (Exception ex)
            {
                // EN: Never break the caller - a locked file (ES running) just retries on
                //     the next automation pass.
                //     FR: Ne jamais casser l'appelant - un fichier locké (ES en cours)
                //     réessaiera à la prochaine passe d'automatisation.
                SimpleLogger.Instance.Warning("[V57q] es_input.cfg registration skipped: " + ex.Message);
            }
        }

        private static byte[] Sub(byte[] src, int offset, int count)
        {
            byte[] dst = new byte[count];
            Buffer.BlockCopy(src, offset, dst, 0, count);
            return dst;
        }

        // EN: One <inputConfig> block, modeled on the user's vmultia entry with the
        //     UP/DOWN axes inverted (joystick1up: -1 -> 1, joystick2up: 1 -> -1).
        //     FR: Un bloc <inputConfig>, calqué sur l'entrée vmultia de l'utilisateur
        //     avec les axes HAUT/BAS inversés (joystick1up : -1 -> 1, joystick2up : 1 -> -1).
        private static string BuildEntry(int player, string name, string guid)
        {
            string nl = "\r\n";
            StringBuilder sb = new StringBuilder();
            sb.Append("\t<inputConfig type=\"joystick\" deviceName=\"").Append(name).Append("\" deviceGUID=\"").Append(guid).Append("\">").Append(nl);
            sb.Append("\t\t<input name=\"a\" type=\"button\" id=\"0\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"b\" type=\"button\" id=\"1\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"down\" type=\"button\" id=\"13\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"hotkey\" type=\"button\" id=\"8\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"joystick1left\" type=\"axis\" id=\"0\" value=\"-1\" />").Append(nl);
            sb.Append("\t\t<input name=\"joystick1up\" type=\"axis\" id=\"1\" value=\"1\" />").Append(nl);   // [V57q] EN/FR: inverted (was -1 in the vmultia entry)
            sb.Append("\t\t<input name=\"joystick2left\" type=\"axis\" id=\"2\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"joystick2up\" type=\"axis\" id=\"3\" value=\"-1\" />").Append(nl);  // [V57q] EN/FR: inverted (was 1 in the vmultia entry)
            sb.Append("\t\t<input name=\"l2\" type=\"button\" id=\"4\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"left\" type=\"button\" id=\"14\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"r2\" type=\"button\" id=\"6\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"right\" type=\"button\" id=\"15\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"select\" type=\"button\" id=\"8\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"start\" type=\"button\" id=\"9\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"up\" type=\"button\" id=\"12\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"x\" type=\"button\" id=\"3\" value=\"1\" />").Append(nl);
            sb.Append("\t\t<input name=\"y\" type=\"button\" id=\"2\" value=\"1\" />").Append(nl);
            sb.Append("\t</inputConfig>").Append(nl);
            return sb.ToString();
        }
    }
}
