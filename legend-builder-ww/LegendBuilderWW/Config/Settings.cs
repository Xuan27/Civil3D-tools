using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace LegendBuilderWW.Config
{
    /// <summary>
    /// User-configurable settings for LegendBuilderWW. Persisted to a JSON file under %APPDATA%.
    /// </summary>
    public class Settings
    {
        public string SourceDwgPath { get; set; }
        public string SourceBlockName { get; set; }
        public string OutputBlockNamePrefix { get; set; }
        public double RowGroupingTolerance { get; set; }

        /// <summary>
        /// When true the legend is emitted as a single column; otherwise two columns (column-major:
        /// the left column fills top-to-bottom, then the right). Remembers the last choice made at
        /// the insertion prompt.
        /// </summary>
        public bool SingleColumn { get; set; }

        /// <summary>
        /// Remembered description edits, keyed by "RowType|Key" (e.g. "Block|V-UTIL-STRM-CULV").
        /// Applied when rows are built so a once-edited label (e.g. "STORM CULVERT") sticks across runs.
        /// Initialized non-null so a settings.json written before this field existed still loads.
        /// </summary>
        public Dictionary<string, string> DescriptionOverrides { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Optional Y threshold. Entities above this Y in the template block are treated as the legend title
        /// (LEGEND text + green bar) and skipped during row parsing. Null = auto-detect.
        /// </summary>
        public double? TitleEntityYThreshold { get; set; }

        public static string SettingsFilePath
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "WPS", "LegendBuilderWW", "settings.json");
            }
        }

        /// <summary>
        /// Loads settings from %APPDATA%. If the file does not exist, it is created from the embedded default seed.
        /// </summary>
        public static Settings Load()
        {
            string path = SettingsFilePath;
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            if (!File.Exists(path))
            {
                string defaultJson = ReadEmbeddedDefault();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, defaultJson, Encoding.UTF8);
                return serializer.Deserialize<Settings>(defaultJson);
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            Settings loaded = serializer.Deserialize<Settings>(json);
            return loaded ?? serializer.Deserialize<Settings>(ReadEmbeddedDefault());
        }

        public void Save()
        {
            string path = SettingsFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(this);
            File.WriteAllText(path, PrettyPrint(json), Encoding.UTF8);
        }

        private static string ReadEmbeddedDefault()
        {
            Assembly asm = typeof(Settings).Assembly;
            string resourceName = "LegendBuilderWW.Config.settings.default.json";
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Embedded default settings resource '{0}' not found.", resourceName));
                }
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static string PrettyPrint(string json)
        {
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(c);
                        sb.Append(' ');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
