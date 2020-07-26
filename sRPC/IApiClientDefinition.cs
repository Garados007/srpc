using System;
using System.Threading;
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

    /// <summary>
    /// The basic interface that every generated api client implements and
    /// <see cref="ApiClient{T}"/> uses. This is the Version 2 interface that
    /// extends some functionality.
    /// </summary>
    public interface IApiClientDefinition2 : IApiClientDefinition
    {
        /// <summary>
        /// Submit a message with a <see cref="CancellationToken"/>
        /// </summary>
        event Func<NetworkRequest, CancellationToken, Task<NetworkResponse>> PerformMessage2;
    }
}
