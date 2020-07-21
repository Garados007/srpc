using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace sRPC.TCP
{
    /// <summary>
    /// The <see cref="ApiServer{T}"/> wrapper for a <see cref="TcpListener"/>.
    /// This manages multiple connections with clients.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class TcpApiServer<T> : IDisposable
        where T : IApiServerDefinition, new()
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<ApiServer<T>, TcpClient> apiServers;

        /// <summary>
        /// The local <see cref="IPEndPoint"/>
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// Create a <see cref="ApiServer{T}"/> wrapper for a <see cref="TcpListener"/>.
        /// </summary>
        /// <param name="endPoint">the local <see cref="IPEndPoint"/></param>
        public TcpApiServer(IPEndPoint endPoint)
            : this(endPoint, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiServer{T}"/> wrapper for a <see cref="TcpListener"/>.
        /// </summary>
        /// <param name="endPoint">the local <see cref="IPEndPoint"/></param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public TcpApiServer(IPEndPoint endPoint, Action<T> setupApi)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            apiServers = new ConcurrentDictionary<ApiServer<T>, TcpClient>();
            tcpListener = new TcpListener(endPoint);
            SetupApi = setupApi;
            Listen();
        }

        private async void Listen()
        {
            tcpListener.Start();
            while (true)
            {
                TcpClient client;
                try { client = await tcpListener.AcceptTcpClientAsync(); }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                        return;
                    throw;
                }
                var stream = client.GetStream();
                var server = new ApiServer<T>(stream);
                apiServers.TryAdd(server, client);
                server.Disconnected += (api, _) =>
                {
                    if (apiServers.TryRemove((ApiServer<T>)api, out TcpClient client))
                        client.Dispose();
                    api.Dispose();
                };
                SetupApi?.Invoke(server.Api);
                server.Start();
            }
        }

        /// <summary>
        /// Dispose all connections and stop the server
        /// </summary>
        public void Dispose()
        {
            tcpListener.Stop();
            foreach (var (server, client) in apiServers.ToArray())
            {
                server.Dispose();
                client.Dispose();
            }
            apiServers.Clear();
        }
    }
}
