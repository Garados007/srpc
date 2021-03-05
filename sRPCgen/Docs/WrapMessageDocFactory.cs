using System.Collections.Generic;
using System;
using System.Linq;
using Google.Protobuf.Reflection;

namespace sRPCgen.Docs
{
    public class WrapMessageDocFactory : IMessageDocFactory
    {
        public IMessageDocFactory BaseFactory { get; }

        public int Message { get; }

        public WrapMessageDocFactory(IMessageDocFactory baseFactory, int messageIndex)
        {
            BaseFactory = baseFactory ?? throw new ArgumentNullException(nameof(baseFactory));
            Message = messageIndex;
        }

        public SourceCodeInfo.Types.Location GetDoc(IEnumerable<int> path)
            => BaseFactory.GetDoc(path?.Prepend(Message).Prepend(3));

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