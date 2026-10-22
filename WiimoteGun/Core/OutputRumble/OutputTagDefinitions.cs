using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WiimoteGun.Core.OutputRumble
{
    /// <summary>EN: One tag rule (pattern -> player + event). FR: Une regle (motif -> joueur + event).</summary>
    internal sealed class OutputTagRule
    {
        public string Pattern;
        public int Player;
        public OutputEvent Event;
    }

    /// <summary>
    /// EN: [V62-OutputRumble] Loads and resolves the emulator output TAG -> (player, event) mapping
    ///     from a dedicated INI file (output_tags.ini next to the app). Matching is case-insensitive
    ///     CONTAINS so it works with or without a MAME device prefix ("mainpcb:"). First match wins.
    ///     The file is reloaded automatically when changed on disk.
    /// FR: [V62-OutputRumble] Charge et resout le mapping TAG d'output -> (joueur, event) depuis un
    ///     fichier INI dedie (output_tags.ini). Match CONTAINS insensible a la casse (compatible avec
    ///     ou sans prefixe de device MAME). Premier match gagnant. Recharge auto si le fichier change.
    /// </summary>
    internal sealed class OutputTagDefinitions
    {
        private readonly List<OutputTagRule> _rules = new List<OutputTagRule>();
        private readonly string _path;
        private DateTime _lastWriteUtc = DateTime.MinValue;

        public OutputTagDefinitions(string path)
        {
            _path = path;
            Load();
        }

        public int RuleCount { get { return _rules.Count; } }

        /// <summary>EN: Reload the file if its mtime changed. FR: Recharge le fichier si son mtime a change.</summary>
        public void RefreshIfChanged()
        {
            try
            {
                DateTime w;
                try { w = File.GetLastWriteTimeUtc(_path); }
                catch { return; }
                if (w != _lastWriteUtc) Load();
            }
            catch { }
        }

        public void Load()
        {
            var loaded = new List<OutputTagRule>();
            try
            {
                if (File.Exists(_path))
                {
                    foreach (var raw in File.ReadAllLines(_path, Encoding.UTF8))
                    {
                        string line = (raw ?? "").Trim();
                        if (line.Length == 0) continue;
                        if (line[0] == ';' || line[0] == '#' || line[0] == '[') continue;

                        var parts = line.Split('|');
                        if (parts.Length < 3) continue;
                        string pat = parts[0].Trim();
                        if (pat.Length == 0) continue;

                        int player;
                        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out player)) continue;
                        if (player < 1 || player > 4) continue;

                        OutputEvent oe = ParseEvent(parts[2]);
                        if (oe == OutputEvent.None) continue;

                        loaded.Add(new OutputTagRule { Pattern = pat, Player = player, Event = oe });
                    }
                    try { _lastWriteUtc = File.GetLastWriteTimeUtc(_path); } catch { }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning("[V62-OutputRumble] Failed to read output tags file: " + ex.Message);
            }

            // EN/FR: merge — file rules FIRST (they win on first match), then the built-in
            // defaults as a fallback, so adding a custom rule in [Rules] never disables the
            // common raw names (additive + overridable).
            var merged = new List<OutputTagRule>(loaded);
            merged.AddRange(DefaultRules());
            _rules.Clear();
            _rules.AddRange(merged);
        }

        /// <summary>EN: First rule whose pattern is contained in the raw tag. FR: Premiere regle dont le motif est contenu dans le tag.</summary>
        public bool TryResolve(string rawTag, out OutputTagRule rule)
        {
            rule = null;
            if (string.IsNullOrEmpty(rawTag)) return false;
            for (int i = 0; i < _rules.Count; i++)
            {
                var r = _rules[i];
                if (rawTag.IndexOf(r.Pattern, StringComparison.OrdinalIgnoreCase) >= 0) { rule = r; return true; }
            }
            return false;
        }

        private static OutputEvent ParseEvent(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "fire": case "shot": case "recoil": case "trigger": return OutputEvent.Fire;
                case "ammo": case "ammunition": case "magazine": case "mag": case "bullets": return OutputEvent.Ammo;
                case "reload": case "reloading": return OutputEvent.Reload;
                case "damage": case "damaged": case "dmg": case "hit": return OutputEvent.Damage;
                default: return OutputEvent.None;
            }
        }

        /// <summary>EN: Built-in fallback rules if the file is missing/empty. FR: Regles de repli integrees si fichier absent/vide.</summary>
        private static List<OutputTagRule> DefaultRules()
        {
            var list = new List<OutputTagRule>();
            for (int p = 1; p <= 4; p++)
            {
                string P = p.ToString();
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Gun_Recoil", Player = p, Event = OutputEvent.Fire });
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Gun_Hit", Player = p, Event = OutputEvent.Damage });
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Damaged", Player = p, Event = OutputEvent.Damage });
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Gun_Ammo", Player = p, Event = OutputEvent.Ammo });
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Ammo", Player = p, Event = OutputEvent.Ammo });
                list.Add(new OutputTagRule { Pattern = "Player" + P + "_Reload", Player = p, Event = OutputEvent.Reload });
                list.Add(new OutputTagRule { Pattern = "TriggerPress_P" + P, Player = p, Event = OutputEvent.Fire });
                // Short MAME output-name variants (no underscore), so copy-pasting a raw name
                // still resolves even without a rule in output_tags.ini.
                list.Add(new OutputTagRule { Pattern = "P" + P + "_GunRecoil", Player = p, Event = OutputEvent.Fire });
                list.Add(new OutputTagRule { Pattern = "P" + P + "_Damaged", Player = p, Event = OutputEvent.Damage });
                list.Add(new OutputTagRule { Pattern = "P" + P + "_Gun_Hit", Player = p, Event = OutputEvent.Damage });
                list.Add(new OutputTagRule { Pattern = "P" + P + "_Ammo", Player = p, Event = OutputEvent.Ammo });
                list.Add(new OutputTagRule { Pattern = "P" + P + "_Reload", Player = p, Event = OutputEvent.Reload });
            }
            return list;
        }
    }
}
