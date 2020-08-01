using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace sRPCgen
{
    public static class Extensions
    {
        public static void WriteLines(this StreamWriter writer, params string[] lines)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            _ = lines ?? throw new ArgumentNullException(nameof(lines));
            foreach (var line in lines)
                if (line != null)
                    writer.WriteLine(line?.Replace("\t", "    ") ?? "");
        }
    }
}
