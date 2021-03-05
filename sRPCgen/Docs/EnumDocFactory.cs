using System;
using System.Linq;
using System.Collections.Generic;
using Google.Protobuf.Reflection;

namespace sRPCgen.Docs
{
    public class EnumDocFactory : IDocFactory
    {
        public IDocFactory DocFactory { get; }

        public int Message { get; }

        private readonly int fieldId;

        public EnumDocFactory(FileDocFactory docFactory, int message)
        {
            DocFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
            Message = message;
            fieldId = 5;
        }

        public EnumDocFactory(IMessageDocFactory docFactory, int message)
        {
            DocFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
            Message = message;
            fieldId = 4;
        }

        public SourceCodeInfo.Types.Location GetDoc(IEnumerable<int> path)
        {
            if (path == null)
                return null;
            return DocFactory.GetDoc(
                path?.Prepend(Message).Prepend(fieldId).ToArray()
            );
        }

        public SourceCodeInfo.Types.Location GetEnumDoc()
        {
            return GetDoc(Enumerable.Empty<int>());
        }
    }
}