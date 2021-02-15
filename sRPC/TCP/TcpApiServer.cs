using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace sRPC.TCP
{
    /// <summary>
    /// The <see cref="ApiServer{T}"/> wrapper for a <see cref="TcpListener"/>.
    /// This manages multiple connections with clients.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class TcpApiServer<T> : IDisposable, IAsyncDisposable
        where T : IApiServerDefinition, new()
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<ApiServer<T>, TcpClient> apiServers;

        /// <summary>
        /// The local <see cref="IPEndPoint"/>
        /// </summary>
        public IPEndPoint EndPoint => (IPEndPoint)tcpListener.LocalEndpoint;

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// The collection of all connected apis.
        /// </summary>
        public IEnumerable<T> Apis
            => apiServers.Keys.Select(x => x.Api);

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
            _ = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            apiServers = new ConcurrentDictionary<ApiServer<T>, TcpClient>();
            tcpListener = new TcpListener(endPoint);
            SetupApi = setupApi;
            _ = Listen();
        }

        private async Task Listen()
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

        /// <summary>
        /// Dispose all connections and stop the server
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            tcpListener.Stop();
            foreach (var (server, client) in apiServers.ToArray())
            {
                await server.DisposeAsync();
                client.Dispose();
            }
            apiServers.Clear();
        }
    }

    /// <summary>
    /// The <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a <see cref="TcpListener"/>.
    /// This manages multiple connections with clients.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API inteface for making requests</typeparam>
    /// <typeparam name="TResponse">the type of the API interface for responding</typeparam>
    public class TcpApiServer<TRequest, TResponse> : IDisposable, IAsyncDisposable
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<ApiServer<TRequest, TResponse>, TcpClient> apiServers;

        /// <summary>
        /// The local <see cref="IPEndPoint"/>
        /// </summary>
        public IPEndPoint EndPoint => (IPEndPoint)tcpListener.LocalEndpoint;

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="TRequest"/> Api.
        /// </summary>
        public Action<TRequest> SetupRequestApi { get; }

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="TResponse"/> Api.
        /// </summary>
        public Action<TResponse> SetupResponseApi { get; }

        /// <summary>
        /// The collection of all connected request apis.
        /// </summary>
        public IEnumerable<TRequest> RequestApis
            => apiServers.Keys.Select(x => x.RequestApi);

        /// <summary>
        /// The collection of all connected response apis.
        /// </summary>
        public IEnumerable<TResponse> ResponseApis
            => apiServers.Keys.Select(x => x.ResponseApi);

        /// <summary>
        /// Create a <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a <see cref="TcpListener"/>.
        /// </summary>
        /// <param name="endPoint">the local <see cref="IPEndPoint"/></param>
        public TcpApiServer(IPEndPoint endPoint)
            : this(endPoint, null, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a <see cref="TcpListener"/>.
        /// </summary>
        /// <param name="endPoint">the local <see cref="IPEndPoint"/></param>
        /// <param name="setupRequestApi">the initializer for the api interface before the client is started</param>
        /// <param name="setupResponseApi">the initializer for the api interface before the client is started</param>
        public TcpApiServer(IPEndPoint endPoint,
            Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            _ = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            apiServers = new ConcurrentDictionary<ApiServer<TRequest, TResponse>, TcpClient>();
            tcpListener = new TcpListener(endPoint);
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            _ = Listen();
        }

        private async Task Listen()
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
                var server = new ApiServer<TRequest, TResponse>(stream);
                apiServers.TryAdd(server, client);
                server.Disconnected += (api, _) =>
                {
                    if (apiServers.TryRemove((ApiServer<TRequest, TResponse>)api, out TcpClient client))
                        client.Dispose();
                    api.Dispose();
                };
                SetupRequestApi?.Invoke(server.RequestApi);
                SetupResponseApi?.Invoke(server.ResponseApi);
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

        /// <summary>
        /// Dispose all connections and stop the server
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            tcpListener.Stop();
            foreach (var (server, client) in apiServers.ToArray())
            {
                await server.DisposeAsync();
                client.Dispose();
            }
            apiServers.Clear();
        }
    }
}
