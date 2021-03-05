using Google.Protobuf.Reflection;
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

        public DescriptorProto Descriptor { get; }

        public Docs.IDocFactory Doc { get; }

        public NameInfo(DescriptorProto descriptor, Docs.IDocFactory doc, string name, string protoBufName, string csharpName)
        {
            Descriptor = descriptor;
            Doc = doc ?? throw new ArgumentNullException(nameof(doc));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ProtoBufName = protoBufName ?? throw new ArgumentNullException(nameof(protoBufName));
            CSharpName = csharpName ?? throw new ArgumentNullException(nameof(csharpName));
        }
    }
}
