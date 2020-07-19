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
}
