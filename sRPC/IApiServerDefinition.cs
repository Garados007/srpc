using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    /// <summary>
    /// The basic interface that every generated api server implements and
    /// <see cref="ApiServer{T}"/> uses.
    /// </summary>
    public interface IApiServerDefinition
    {
        /// <summary>
        /// handles the request and create the response
        /// </summary>
        /// <param name="request">the request</param>
        /// <returns>the response to it</returns>
        Task<NetworkResponse> HandleMessage(NetworkRequest request);
    }

    /// <summary>
    /// The basic interface that every generated api server implements and
    /// <see cref="ApiServer{T}"/> uses. This is the Version 2 interface that
    /// extends some functionality.
    /// </summary>
    public interface IApiServerDefinition2 : IApiServerDefinition
    {
        /// <summary>
        /// handles the request and create the response
        /// </summary>
        /// <param name="request">the request</param>
        /// <returns>the response to it</returns>
        Task<NetworkResponse> HandleMessage2(NetworkRequest request, CancellationToken cancellationToken);
    }
}
