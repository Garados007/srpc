using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace sRPC.TCP
{
    /// <summary>
    /// The <see cref="ApiClient{T}"/> wrapper for a <see cref="TcpClient"/>. This
    /// manages the connection and automatic reconnection of it.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class TcpApiClient<T> : IDisposable, IApi<T>
        where T : class, IApiClientDefinition, new()
    {
        private ApiClient<T> client;
        private TcpClient tcpClient;

        /// <summary>
        /// The current API interface. Do not cache this object, because it
        /// can change after a reconnection
        /// </summary>
        public T Api => client?.Api;

        /// <summary>
        /// The current <see cref="IPEndPoint"/> of the Server.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for a <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="endPoint">the <see cref="IPEndPoint"/> of the server</param>
        /// <exception cref="ArgumentNullException" />
        public TcpApiClient(IPEndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            Connect();
        }

        private async void Connect(ApiClient<T> oldApi = null)
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(EndPoint.Address, EndPoint.Port);
            var stream = tcpClient.GetStream();
            client = new ApiClient<T>(stream, stream, oldApi);
            client.Disconnected += Client_Disconnected;
            client.Start();
        }

        private void Client_Disconnected(ApiBase api, IOException ex)
        {
            if (api != client)
                return;
            tcpClient.Dispose();
            Connect((ApiClient<T>)api);
        }

        /// <summary>
        /// Dispose the <see cref="ApiClient{T}"/> and <see cref="TcpClient"/>
        /// </summary>
        public void Dispose()
        {
            tcpClient?.Dispose();
            client?.Dispose();
        }
    }
}
