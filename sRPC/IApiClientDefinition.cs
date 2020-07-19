using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace sRPC
{
    public interface IApiClientDefinition
    {
        event Func<NetworkRequest, Task<NetworkResponse>> PerformMessage;
    }
}
