using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sRPCgen.Docs
{
    public class FileDocFactory : IDocFactory
    {
        private readonly DocFactory docFactory;
        private readonly int file;

        public FileDocFactory(DocFactory docFactory, int fileIndex)
        {
            this.docFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
            file = fileIndex;
        }

        public SourceCodeInfo.Types.Location GetDoc(params int[] path)
            => docFactory.GetDoc(file, path);
        
        public SourceCodeInfo.Types.Location GetServiceDoc(int serviceIndex)
            => docFactory.GetServiceDoc(file, serviceIndex);
        
        public SourceCodeInfo.Types.Location GetServiceRpcDoc(int serviceIndex, int rpcIndex)
            => docFactory.GetServiceRpcDoc(file, serviceIndex, rpcIndex);

        public SourceCodeInfo.Types.Location GetMessageDoc(int messageIndex)
            => docFactory.GetMessageDoc(file, messageIndex);

        public SourceCodeInfo.Types.Location GetDoc(IEnumerable<int> path)
        {
            return GetDoc(path.ToArray());
        }
    }
}