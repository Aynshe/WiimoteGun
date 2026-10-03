using System;
using System.Collections.Generic;
using System.Text;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: Minimal generic JSON model used by the app's persistent memory files.
    /// Preserves unknown keys and key order on round-trip (parse -> save), so
    /// future services can own their own section of a shared JSON file without
    /// this parser knowing about them.
    /// FR: Modèle JSON générique minimal utilisé par les fichiers de mémoire
    /// persistante de l'app. Préserve les clés inconnues et l'ordre des clés au
    /// round-trip (lecture -> sauvegarde), pour qu'un futur service puisse
    /// posséder sa propre section d'un fichier JSON partagé sans que ce parseur
    /// en ait connaissance.
    /// </summary>
    public sealed class JsonValue
    {
        public enum JsonType { Null, Object, Array, String, Boolean, Number }

        public JsonType Type = JsonType.Null;
        public string Str;                        // String value (FR: Valeur chaîne)
        public bool Bool;                         // Boolean value (FR: Valeur booléenne)
        public string RawNumber;                  // Number raw text, kept for exact round-trip (FR: Texte brut du nombre, conservé pour un round-trip exact)
        public List<KeyValuePair<string, JsonValue>> Pairs; // Object members, order preserved (FR: Membres d'objet, ordre préservé)
        public List<JsonValue> Items;            // Array elements (FR: Éléments de tableau)

        public static JsonValue NullValue() { return new JsonValue { Type = JsonType.Null }; }
        public static JsonValue NewString(string s) { return new JsonValue { Type = JsonType.String, Str = s ?? "" }; }
        public static JsonValue NewBool(bool b) { return new JsonValue { Type = JsonType.Boolean, Bool = b }; }
        public static JsonValue NewNumber(string raw) { return new JsonValue { Type = JsonType.Number, RawNumber = raw }; }
        public static JsonValue NewObject() { return new JsonValue { Type = JsonType.Object, Pairs = new List<KeyValuePair<string, JsonValue>>() }; }
        public static JsonValue NewArray() { return new JsonValue { Type = JsonType.Array, Items = new List<JsonValue>() }; }

        /// <summary>EN: Get a member of an object (null when missing). FR: Obtient un membre d'objet (null si absent).</summary>
        public JsonValue Get(string key)
        {
            if (Type != JsonType.Object || Pairs == null) return null;
            for (int i = 0; i < Pairs.Count; i++)
            {
                if (string.Equals(Pairs[i].Key, key, StringComparison.OrdinalIgnoreCase)) return Pairs[i].Value;
            }
            return null;
        }

        /// <summary>EN: Set (replace or append) a member on an object. FR: Définit (remplace ou ajoute) un membre d'objet.</summary>
        public void Set(string key, JsonValue value)
        {
            if (Type != JsonType.Object) throw new InvalidOperationException("JsonValue is not an object");
            if (Pairs == null) Pairs = new List<KeyValuePair<string, JsonValue>>();
            for (int i = 0; i < Pairs.Count; i++)
            {
                if (string.Equals(Pairs[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    Pairs[i] = new KeyValuePair<string, JsonValue>(key, value);
                    return;
                }
            }
            Pairs.Add(new KeyValuePair<string, JsonValue>(key, value));
        }

        /// <summary>EN: Get an object member, creating it when missing. FR: Obtient un membre objet, créé si absent.</summary>
        public JsonValue GetOrAddObject(string key)
        {
            JsonValue v = Get(key);
            if (v != null && v.Type == JsonType.Object) return v;
            JsonValue created = NewObject();
            Set(key, created);
            return created;
        }

        /// <summary>EN: Get an array member, creating it when missing. FR: Obtient un membre tableau, créé si absent.</summary>
        public JsonValue GetOrAddArray(string key)
        {
            JsonValue v = Get(key);
            if (v != null && v.Type == JsonType.Array) return v;
            JsonValue created = NewArray();
            Set(key, created);
            return created;
        }

        /// <summary>EN: Read a string member with fallback. FR: Lit un membre chaîne avec valeur de repli.</summary>
        public string GetString(string key, string fallback = null)
        {
            JsonValue v = Get(key);
            if (v == null || v.Type != JsonType.String) return fallback;
            return v.Str;
        }
    }

    /// <summary>
    /// EN: Tiny recursive-descent JSON parser + pretty serializer (no external
    /// dependencies). Understands the full JSON grammar: objects, arrays,
    /// escaped strings (\uXXXX included), booleans, null and numbers.
    /// FR: Mini parseur JSON à descente récursive + sérialiseur lisible (sans
    /// dépendance externe). Comprend la grammaire JSON complète : objets,
    /// tableaux, chaînes échappées (\uXXXX inclus), booléens, null et nombres.
    /// </summary>
    public static class JsonLite
    {
        public static JsonValue Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            int pos = 0;
            JsonValue value = ParseValue(json, ref pos);
            SkipWs(json, ref pos);
            if (pos != json.Length) throw new FormatException("Trailing characters after JSON value at position " + pos);
            return value;
        }

        public static string Serialize(JsonValue value)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        // ============ Parser (EN/FR: Analyseur) ============

        private static void SkipWs(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') pos++;
                else break;
            }
        }

        private static JsonValue ParseValue(string s, ref int pos)
        {
            SkipWs(s, ref pos);
            if (pos >= s.Length) throw new FormatException("Unexpected end of JSON input");
            char c = s[pos];
            switch (c)
            {
                case '{': return ParseObject(s, ref pos);
                case '[': return ParseArray(s, ref pos);
                case '"': return JsonValue.NewString(ParseString(s, ref pos));
                case 't': ExpectLiteral(s, ref pos, "true"); return JsonValue.NewBool(true);
                case 'f': ExpectLiteral(s, ref pos, "false"); return JsonValue.NewBool(false);
                case 'n': ExpectLiteral(s, ref pos, "null"); return JsonValue.NullValue();
                default: return ParseNumber(s, ref pos);
            }
        }

        private static JsonValue ParseObject(string s, ref int pos)
        {
            pos++; // '{'
            JsonValue obj = JsonValue.NewObject();
            SkipWs(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; return obj; }
            while (true)
            {
                SkipWs(s, ref pos);
                if (pos >= s.Length || s[pos] != '"') throw new FormatException("Expected object key string at position " + pos);
                string key = ParseString(s, ref pos);
                SkipWs(s, ref pos);
                if (pos >= s.Length || s[pos] != ':') throw new FormatException("Expected ':' at position " + pos);
                pos++;
                JsonValue value = ParseValue(s, ref pos);
                obj.Set(key, value);
                SkipWs(s, ref pos);
                if (pos >= s.Length) throw new FormatException("Unterminated object");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == '}') { pos++; return obj; }
                throw new FormatException("Expected ',' or '}' at position " + pos);
            }
        }

        private static JsonValue ParseArray(string s, ref int pos)
        {
            pos++; // '['
            JsonValue arr = JsonValue.NewArray();
            SkipWs(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; return arr; }
            while (true)
            {
                JsonValue value = ParseValue(s, ref pos);
                arr.Items.Add(value);
                SkipWs(s, ref pos);
                if (pos >= s.Length) throw new FormatException("Unterminated array");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == ']') { pos++; return arr; }
                throw new FormatException("Expected ',' or ']' at position " + pos);
            }
        }

        private static string ParseString(string s, ref int pos)
        {
            pos++; // opening '"'
            StringBuilder sb = new StringBuilder();
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == '"') { pos++; return sb.ToString(); }
                if (c == '\\')
                {
                    pos++;
                    if (pos >= s.Length) throw new FormatException("Unterminated escape in string");
                    char e = s[pos];
                    switch (e)
                    {
                        case '"': sb.Append('"'); pos++; break;
                        case '\\': sb.Append('\\'); pos++; break;
                        case '/': sb.Append('/'); pos++; break;
                        case 'b': sb.Append('\b'); pos++; break;
                        case 'f': sb.Append('\f'); pos++; break;
                        case 'n': sb.Append('\n'); pos++; break;
                        case 'r': sb.Append('\r'); pos++; break;
                        case 't': sb.Append('\t'); pos++; break;
                        case 'u':
                            if (pos + 4 >= s.Length) throw new FormatException("Invalid \\u escape in string");
                            string hex = s.Substring(pos + 1, 4);
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            pos += 5;
                            break;
                        default:
                            throw new FormatException("Invalid escape '\\" + e + "' in string");
                    }
                }
                else
                {
                    sb.Append(c);
                    pos++;
                }
            }
            throw new FormatException("Unterminated string");
        }

        private static void ExpectLiteral(string s, ref int pos, string literal)
        {
            if (pos + literal.Length > s.Length || s.Substring(pos, literal.Length) != literal)
                throw new FormatException("Invalid literal at position " + pos);
            pos += literal.Length;
        }

        private static JsonValue ParseNumber(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length)
            {
                char c = s[pos];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') pos++;
                else break;
            }
            if (pos == start) throw new FormatException("Invalid number at position " + pos);
            return JsonValue.NewNumber(s.Substring(start, pos - start));
        }

        // ============ Serializer (EN/FR: Sérialiseur) ============

        private static void WriteValue(StringBuilder sb, JsonValue v, int indent)
        {
            if (v == null) { sb.Append("null"); return; }
            switch (v.Type)
            {
                case JsonValue.JsonType.Null: sb.Append("null"); break;
                case JsonValue.JsonType.Boolean: sb.Append(v.Bool ? "true" : "false"); break;
                case JsonValue.JsonType.Number: sb.Append(string.IsNullOrEmpty(v.RawNumber) ? "0" : v.RawNumber); break;
                case JsonValue.JsonType.String: WriteString(sb, v.Str); break;
                case JsonValue.JsonType.Object: WriteObject(sb, v, indent); break;
                case JsonValue.JsonType.Array: WriteArray(sb, v, indent); break;
            }
        }

        private static void WriteObject(StringBuilder sb, JsonValue v, int indent)
        {
            if (v.Pairs == null || v.Pairs.Count == 0) { sb.Append("{}"); return; }
            sb.Append("{");
            string innerIndent = Environment.NewLine + Repeat("  ", indent + 1);
            for (int i = 0; i < v.Pairs.Count; i++)
            {
                sb.Append(innerIndent);
                WriteString(sb, v.Pairs[i].Key);
                sb.Append(": ");
                WriteValue(sb, v.Pairs[i].Value, indent + 1);
                if (i < v.Pairs.Count - 1) sb.Append(",");
            }
            sb.Append(Environment.NewLine + Repeat("  ", indent) + "}");
        }

        private static void WriteArray(StringBuilder sb, JsonValue v, int indent)
        {
            if (v.Items == null || v.Items.Count == 0) { sb.Append("[]"); return; }
            sb.Append("[");
            string innerIndent = Environment.NewLine + Repeat("  ", indent + 1);
            for (int i = 0; i < v.Items.Count; i++)
            {
                sb.Append(innerIndent);
                WriteValue(sb, v.Items[i], indent + 1);
                if (i < v.Items.Count - 1) sb.Append(",");
            }
            sb.Append(Environment.NewLine + Repeat("  ", indent) + "]");
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append(string.Format("\\u{0:x4}", (int)c));
                            else sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        private static string Repeat(string s, int count)
        {
            if (count <= 0) return "";
            StringBuilder sb = new StringBuilder(s.Length * count);
            for (int i = 0; i < count; i++) sb.Append(s);
            return sb.ToString();
        }
    }
}
