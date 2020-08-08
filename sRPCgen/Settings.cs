using System;
using System.Collections.Generic;
using System.Text;

namespace sRPCgen
{
    class Settings
    {
        public string File { get; set; }
        public string OutputDir { get; set; }
        public string NamespaceBase { get; set; }
        public string FileExtension { get; set; }

        public bool Verbose { get; set; }
        public bool BuildProtoc { get; set; }

        public string SearchDir { get; set; }
        public List<string> ProtoImports { get; } = new List<string>();
        public string ProtoExtension { get; set; }
        public string ErrorFormat { get; set; }
        public bool EmptySupport { get; set; }
        public List<string> IgnoreUnwrap { get; } = new List<string>();

        public void SetDefaults()
        {
            OutputDir ??= Environment.CurrentDirectory;
            NamespaceBase ??= "";
            FileExtension ??= ".g.cs";
            ProtoExtension ??= ".cs";
            ErrorFormat ??= "default";
        }
    }
}
