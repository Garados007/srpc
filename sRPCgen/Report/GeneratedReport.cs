﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace sRPCgen.Report
{
    class GeneratedReport
    {
        public string File { get; set; }

        public string Source { get; set; }

        public DateTime LastBuild { get; set; }

        public bool Srpc { get; set; }

        public void Save(Utf8JsonWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStartObject();
            writer.WriteString("file", File);
            writer.WriteString("source", Source);
            writer.WriteString("last-build", LastBuild);
            writer.WriteBoolean("srpc", Srpc);
            writer.WriteEndObject();
        }
    }
}
