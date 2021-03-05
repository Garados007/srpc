using System;
using System.Linq;
using System.Collections.Generic;
using Google.Protobuf.Reflection;

namespace sRPCgen.Docs
{
    public class BaseMessageDocFactory : IMessageDocFactory
    {
        public FileDocFactory FileDocFactory { get; }

        public int Message { get; }

        public BaseMessageDocFactory(FileDocFactory docFactory, int message)
        {
            FileDocFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
            Message = message;
        }

        public SourceCodeInfo.Types.Location GetDoc(IEnumerable<int> path)
        {
            if (path == null)
                return null;
            return FileDocFactory.GetDoc(
                path?.Prepend(Message).Prepend(4).ToArray()
            );
        }

        public SourceCodeInfo.Types.Location GetElementDoc()
        {
            return GetDoc(Enumerable.Empty<int>());
        }

        public SourceCodeInfo.Types.Location GetFieldDoc(int fieldIndex)
        {
            return GetDoc(new [] { 2, fieldIndex });
        }
    }
}