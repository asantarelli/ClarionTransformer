using System;
using System.IO;
using System.Text;

namespace ClarionTransformer.Services
{
    public static class TransformerProfileService
    {
        public static readonly string SettingsPath        = ResolveDataFile("clarion-transformer.json");
        public static readonly string DefaultProtocolPath = ResolveDataFile("Protocolo_ClarionTransformer.md");

        // Archivos en %APPDATA%\ClarionTransformer\. Hasta 1.0.0 vivian en %APPDATA%\ClarionAssistant\
        // (carpeta de otro addin): se mueven una sola vez a la carpeta propia; si no se puede mover, se copia.
        private static string ResolveDataFile(string fileName)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
            {
                string path = Path.Combine(appData, "ClarionTransformer", fileName);
                MigrateLegacyFile(Path.Combine(appData, "ClarionAssistant", fileName), path);
                return path;
            }

            string asmDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(asmDir ?? ".", fileName);
        }

        private static void MigrateLegacyFile(string legacyPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath) || !File.Exists(legacyPath)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                try { File.Move(legacyPath, newPath); }
                catch { File.Copy(legacyPath, newPath); }
            }
            catch { }
        }

        private static TransformerSettings _cached;

        public static TransformerSettings Load()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                    var parsed = SimpleJsonParser.ParseSettings(json);
                    if (parsed != null) { _cached = parsed; return _cached; }
                }
            }
            catch { }
            _cached = new TransformerSettings();
            Save(_cached);
            return _cached;
        }

        public static void Save(TransformerSettings settings)
        {
            try
            {
                _cached = settings;
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsPath, SimpleJsonParser.SerializeSettings(settings), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "ClarionTransformer: no se pudo guardar la configuracion.\n" + ex.Message,
                    "ClarionTransformer", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        public static TransformerProfile GetActiveProfile()
        {
            var s = Load();
            foreach (var p in s.Profiles)
                if (p.ProfileName == s.ActiveProfile) return p;
            return s.Profiles.Count > 0 ? s.Profiles[0] : new TransformerProfile();
        }

        /// <summary>Busca un perfil por nombre exacto (usado por los comandos de menu fijos). Null si no existe.</summary>
        public static TransformerProfile GetProfileByName(string name)
        {
            var s = Load();
            foreach (var p in s.Profiles)
                if (p.ProfileName == name) return p;
            return null;
        }

        public static void Invalidate() => _cached = null;
    }

    internal static class SimpleJsonParser
    {
        public static string SerializeSettings(TransformerSettings s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"ActiveProfile\": " + Q(s.ActiveProfile) + ",");
            sb.AppendLine("  \"AnthropicApiKey\": " + Q(s.AnthropicApiKey) + ",");
            sb.AppendLine("  \"AiModel\": " + Q(s.AiModel) + ",");
            sb.AppendLine("  \"Profiles\": [");
            for (int i = 0; i < s.Profiles.Count; i++)
            {
                sb.Append(SerializeProfile(s.Profiles[i], "    "));
                if (i < s.Profiles.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeProfile(TransformerProfile p, string indent)
        {
            var sb = new StringBuilder();
            string i2 = indent + "  ";
            sb.AppendLine(indent + "{");
            sb.AppendLine(i2 + Q("ProfileName") + ": " + Q(p.ProfileName) + ",");
            sb.AppendLine(i2 + Q("AiProtocolFile") + ": " + Q(p.AiProtocolFile) + ",");
            sb.AppendLine(i2 + Q("AiExtraInstructions") + ": " + Q(p.AiExtraInstructions) + ",");
            sb.AppendLine(i2 + Q("CreateBackup") + ": " + (p.CreateBackup ? "true" : "false") + ",");
            sb.AppendLine(i2 + Q("AddComments") + ": " + (p.AddComments ? "true" : "false") + ",");
            sb.AppendLine(i2 + Q("Reindent") + ": " + (p.Reindent ? "true" : "false") + ",");
            sb.AppendLine(i2 + Q("IndentSpaces") + ": " + p.IndentSpaces);
            sb.Append(indent + "}");
            return sb.ToString();
        }

        public static TransformerSettings ParseSettings(string json)
        {
            try
            {
                var s = new TransformerSettings();
                s.ActiveProfile   = ReadString(json, "ActiveProfile")   ?? s.ActiveProfile;
                s.AnthropicApiKey = ReadString(json, "AnthropicApiKey") ?? s.AnthropicApiKey;
                s.AiModel         = ReadString(json, "AiModel")         ?? s.AiModel;
                s.Profiles.Clear();

                int profilesStart = json.IndexOf("\"Profiles\"", StringComparison.Ordinal);
                if (profilesStart < 0) return s;
                int arrayStart = json.IndexOf('[', profilesStart);
                if (arrayStart < 0) return s;

                int pos = arrayStart + 1;
                while (pos < json.Length)
                {
                    int objStart = json.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = FindMatchingBrace(json, objStart);
                    if (objEnd < 0) break;
                    string block = json.Substring(objStart, objEnd - objStart + 1);
                    var p = ParseProfile(block);
                    if (p != null) s.Profiles.Add(p);
                    pos = objEnd + 1;
                }

                if (s.Profiles.Count == 0) s.Profiles.Add(new TransformerProfile());
                return s;
            }
            catch { return null; }
        }

        private static TransformerProfile ParseProfile(string block)
        {
            var p = new TransformerProfile();
            p.ProfileName         = ReadString(block, "ProfileName")         ?? p.ProfileName;
            p.AiProtocolFile      = ReadString(block, "AiProtocolFile")      ?? p.AiProtocolFile;
            p.AiExtraInstructions = ReadString(block, "AiExtraInstructions") ?? p.AiExtraInstructions;
            bool? backup = ReadBool(block, "CreateBackup");
            if (backup.HasValue) p.CreateBackup = backup.Value;
            bool? comments = ReadBool(block, "AddComments");
            if (comments.HasValue) p.AddComments = comments.Value;
            bool? reindent = ReadBool(block, "Reindent");
            if (reindent.HasValue) p.Reindent = reindent.Value;
            int? indentSpaces = ReadInt(block, "IndentSpaces");
            if (indentSpaces.HasValue && indentSpaces.Value > 0) p.IndentSpaces = indentSpaces.Value;
            return p;
        }

        private static string ReadString(string json, string key)
        {
            string pat = "\"" + key + "\"";
            int k = json.IndexOf(pat, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k + pat.Length);
            if (colon < 0) return null;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            int q2 = q1 + 1;
            while (q2 < json.Length)
            {
                if (json[q2] == '\\') { q2 += 2; continue; }
                if (json[q2] == '"') break;
                q2++;
            }
            if (q2 >= json.Length) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1).Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r");
        }

        private static bool? ReadBool(string json, string key)
        {
            string pat = "\"" + key + "\"";
            int k = json.IndexOf(pat, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k + pat.Length);
            if (colon < 0) return null;
            int p = colon + 1;
            while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
            if (json.Length - p >= 4 && json.Substring(p, 4) == "true") return true;
            if (json.Length - p >= 5 && json.Substring(p, 5) == "false") return false;
            return null;
        }

        private static int? ReadInt(string json, string key)
        {
            string pat = "\"" + key + "\"";
            int k = json.IndexOf(pat, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k + pat.Length);
            if (colon < 0) return null;
            int p = colon + 1;
            while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
            int start = p;
            while (p < json.Length && (char.IsDigit(json[p]) || json[p] == '-')) p++;
            if (p == start) return null;
            return int.TryParse(json.Substring(start, p - start), out int result) ? result : (int?)null;
        }

        private static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

        private static int FindMatchingBrace(string s, int open)
        {
            int depth = 0;
            bool inStr = false;
            for (int i = open; i < s.Length; i++)
            {
                if (inStr) { if (s[i] == '\\') i++; else if (s[i] == '"') inStr = false; continue; }
                if (s[i] == '"') { inStr = true; continue; }
                if (s[i] == '{') depth++;
                else if (s[i] == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }
    }
}
