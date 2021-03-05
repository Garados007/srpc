using Google.Protobuf.Reflection;
using System.Collections.Generic;

namespace sRPCgen.Docs
{
    public interface IMessageDocFactory : IDocFactory
    {
        SourceCodeInfo.Types.Location GetElementDoc();

        SourceCodeInfo.Types.Location GetFieldDoc(int fieldIndex);
    }
}