using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace sRPC.Tools
{
    public class ProtoPlatform : Task
    {
        [Output]
        public string Os { get; set; }

        [Output]
        public string Cpu { get; set; }

        public override bool Execute()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Os = "windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Os = "maxosx";
            else Os = "";

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86: Cpu = "x86"; break;
                case Architecture.X64: Cpu = "x64"; break;
                default: Cpu = ""; break;
            }

            return true;
        }
    }
}
