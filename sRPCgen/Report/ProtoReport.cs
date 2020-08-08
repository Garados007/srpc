using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sRPCgen.Report
{
    class ProtoReport
    {
        public string File { get; set; }

        public List<string> Dependencies { get; } = new List<string>();

        public DateTime LastChange { get; set; }

        public static (ProtoReport, GeneratedReport) ReadReport(string file, string source)
        {
            _ = file ?? throw new ArgumentNullException(nameof(file));
            var info = new FileInfo(file);
            if (!info.Exists)
                return (null, null);
            var regex = new Regex(@"(?<file>.+): ((?<dep>[^\\$]+)[\\\s]*)+");
            using var reader = info.OpenText();
            var match = regex.Match(reader.ReadToEnd());
            if (!match.Success)
                return (null, null);
            var report = new ProtoReport
            {
                File = source,
                LastChange = info.LastWriteTimeUtc,
            };
            report.Dependencies.AddRange(match.Groups["dep"].Captures.Select(x => x.Value));
            var gen = new GeneratedReport
            {
                File = match.Groups["file"].Value,
                Source = source,
                LastBuild = new FileInfo(match.Groups["file"].Value).LastWriteTimeUtc,
                Srpc = false,
            };
            return (report, gen);
        }

        public void Save(Utf8JsonWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStartObject();
            writer.WriteString("file", File);
            writer.WriteString("last-change", LastChange);
            writer.WritePropertyName("deps");
            writer.WriteStartArray();
            foreach (var dep in Dependencies.Where(x => x != null))
                writer.WriteStringValue(dep);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public static ProtoReport Load(ref JsonElement json)
        {
            var report = new ProtoReport
            {
                File = json.GetProperty("file").GetString(),
                LastChange = json.GetProperty("last-change").GetDateTime(),
            };
            report.Dependencies.AddRange(
                json.GetProperty("deps").EnumerateArray().Select(x => x.GetString()));
            return report;
        }
    }
}
