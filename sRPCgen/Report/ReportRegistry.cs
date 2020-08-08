using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace sRPCgen.Report
{
    class ReportRegistry
    {
        public List<ProtoReport> Protos { get; } = new List<ProtoReport>();

        public List<GeneratedReport> Generateds { get; } = new List<GeneratedReport>();

        public void Save(string path)
        {
            using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            stream.SetLength(0);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            });
            writer.WriteStartObject();
            writer.WritePropertyName("proto");
            writer.WriteStartArray();
            foreach (var proto in Protos.Where(x => x != null))
                proto.Save(writer);
            writer.WriteEndArray();
            writer.WritePropertyName("generated");
            writer.WriteStartArray();
            foreach (var gen in Generateds.Where(x => x != null))
                gen.Save(writer);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            stream.Flush();
        }

        public static ReportRegistry Load(string file)
        {
            if (!File.Exists(file))
                return null;
            var content = File.ReadAllBytes(file);
            var reader = new Utf8JsonReader(content,
                new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            if (!JsonDocument.TryParseValue(ref reader, out JsonDocument document))
                return null;
            try
            {
                var registry = new ReportRegistry();
                registry.Protos.AddRange(document.RootElement
                    .GetProperty("proto").EnumerateArray()
                    .Select(x => ProtoReport.Load(ref x)));
                registry.Generateds.AddRange(document.RootElement
                    .GetProperty("generated").EnumerateArray()
                    .Select(x => GeneratedReport.Load(ref x)));
                return registry;
            }
            catch { return null; }
        }
    }
}
