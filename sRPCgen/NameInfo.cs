using System;
using System.Collections.Generic;
using System.Text;

namespace sRPCgen
{
    class NameInfo
    {
        public string Name { get; }

        public string ProtoBufName { get; }

        public string CSharpName { get; }

        public NameInfo(string name, string protoBufName, string csharpName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ProtoBufName = protoBufName ?? throw new ArgumentNullException(nameof(protoBufName));
            CSharpName = csharpName ?? throw new ArgumentNullException(nameof(csharpName));
        }
    }
}
