using System.Threading.Tasks;

namespace sRPC
{
    public interface IApiServerDefinition
    {
        Task<NetworkResponse> HandleMessage(NetworkRequest request);
    }
}
