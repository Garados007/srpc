using System;
using System.Threading.Tasks;

namespace sRPC
{
    public interface IApiClientDefinition
    {
        event Func<NetworkRequest, Task<NetworkResponse>> PerformMessage;
    }
}
