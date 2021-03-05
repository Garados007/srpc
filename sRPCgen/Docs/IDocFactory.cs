using Google.Protobuf.Reflection;
using System.Collections.Generic;

namespace sRPCgen.Docs
{
    public interface IDocFactory
    {
        SourceCodeInfo.Types.Location GetDoc(IEnumerable<int> path);
    }
}