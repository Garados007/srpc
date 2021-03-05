using Google.Protobuf.Reflection;
using System;

namespace sRPCgen.Docs
{
    public class ServiceDocFactory
    {
        public FileDocFactory FileDocFactory { get; }

        public int Service { get; }

        public ServiceDocFactory(FileDocFactory docFactory, int serviceIndex)
        {
            FileDocFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
            Service = serviceIndex;
        }

        public SourceCodeInfo.Types.Location GetDoc(params int[] path)
        {
            if (path == null)
                return null;
            var newPath = new int[path.Length + 2];
            newPath[0] = 6;
            newPath[1] = Service;
            Array.Copy(path, 0, newPath, 2, path.Length);
            return FileDocFactory.GetDoc(newPath);
        }
        
        public SourceCodeInfo.Types.Location GetServiceDoc()
            => FileDocFactory.GetServiceDoc(Service);
        
        public SourceCodeInfo.Types.Location GetServiceRpcDoc(int rpcIndex)
            => FileDocFactory.GetServiceRpcDoc(Service, rpcIndex);
    }
}