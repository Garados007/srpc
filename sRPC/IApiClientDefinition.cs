using System;
using System.Threading.Tasks;

namespace sRPC
{
    /// <summary>
    /// The basic interface that every generated api client implements and
    /// <see cref="ApiClient{T}"/> uses.
    /// </summary>
    public interface IApiClientDefinition
    {
        /// <summary>
        /// Submit a message
        /// </summary>
        event Func<NetworkRequest, Task<NetworkResponse>> PerformMessage;
    }
}
