using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiServer{T}"/> wrapper for a <see cref="NamedPipeServerStream"/>.
    /// This manages the connection and automatic reconnection to a single client.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class NamedApiServer<T> : IDisposable, IAsyncDisposable, IApi<T>
        where T : class, IApiServerDefinition, new()
    {
        private ApiServer<T> server;
        private NamedPipeServerStream pipe;
        private readonly CancellationTokenSource cancellationToken;

        /// <summary>
        /// The current API interface. Do not cache this object, because it can
        /// change after a reconnection
        /// </summary>
        public T Api => server?.Api;

        /// <summary>
        /// The name of the pipe for this server
        /// </summary>
        public string PipeName { get; }

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// Create a <see cref="ApiServer{T}"/> wrapper for a 
        /// <see cref="NamedPipeServerStream"/>
        /// </summary>
        /// <param name="pipeName">the name of the pipe for this server</param>
        public NamedApiServer(string pipeName)
            : this(pipeName, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiServer{T}"/> wrapper for a 
        /// <see cref="NamedPipeServerStream"/>
        /// </summary>
        /// <param name="pipeName">the name of the pipe for this server</param>
        /// <param name="setupApi"></param>
        public NamedApiServer(string pipeName, Action<T> setupApi)
        {
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            SetupApi = setupApi;
            cancellationToken = new CancellationTokenSource();
            _ = Connect();
        }

        private async Task Connect()
        {
            pipe = new NamedPipeServerStream(
                PipeName, 
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);
            await pipe.WaitForConnectionAsync(cancellationToken.Token);
            if (cancellationToken.IsCancellationRequested)
                return;
            server = new ApiServer<T>(pipe, pipe);
            server.Disconnected += Server_Disconnected;
            SetupApi?.Invoke(server.Api);
            server.Start();
        }

        private void Server_Disconnected(ApiBase api, IOException ex)
        {
            if (api != server)
                return;
            server.Dispose(); // the messages will be discarded, the client has to send the requests again
            pipe.Dispose();
            _ = Connect();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public void Dispose()
        {
            cancellationToken.Dispose();
            pipe?.Dispose();
            server?.Dispose();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Dispose();
            if (pipe != null)
                await pipe.DisposeAsync();
            if (server != null)
                await server.DisposeAsync();
        }
    }

    /// <summary>
    /// The <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a <see cref="NamedPipeServerStream"/>.
    /// This manages the connection and automatic reconnection to a single client.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API inteface for making requests</typeparam>
    /// <typeparam name="TResponse">the type of the API interface for responding</typeparam>
    public class NamedApiServer<TRequest, TResponse> : IDisposable, IAsyncDisposable, IApi<TRequest>, IApi<TRequest, TResponse>
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private ApiServer<TRequest, TResponse> server;
        private NamedPipeServerStream pipe;
        private readonly CancellationTokenSource cancellationToken;

        /// <summary>
        /// The current Api interface for creating Api requests
        /// </summary>
        public TRequest RequestApi => server.RequestApi;

        /// <summary>
        /// The current Api interface for responding Api requests
        /// </summary>
        public TResponse ResponseApi => server.ResponseApi;

        TRequest IApi<TRequest>.Api => server.RequestApi;

        /// <summary>
        /// The name of the pipe for this server
        /// </summary>
        public string PipeName { get; }

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
        /// Create a <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a 
        /// <see cref="NamedPipeServerStream"/>
        /// </summary>
        /// <param name="pipeName">the name of the pipe for this server</param>
        public NamedApiServer(string pipeName)
            : this(pipeName, null, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiServer{TRequest, TResponse}"/> wrapper for a 
        /// <see cref="NamedPipeServerStream"/>
        /// </summary>
        /// <param name="pipeName">the name of the pipe for this server</param>
        /// <param name="setupRequestApi">the initializer for the api interface before the client is started</param>
        /// <param name="setupResponseApi">the initializer for the api interface before the client is started</param>
        public NamedApiServer(string pipeName,
            Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            cancellationToken = new CancellationTokenSource();
            _ = Connect();
        }

        private async Task Connect()
        {
            pipe = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);
            await pipe.WaitForConnectionAsync(cancellationToken.Token);
            if (cancellationToken.IsCancellationRequested)
                return;
            server = new ApiServer<TRequest, TResponse>(pipe, pipe);
            server.Disconnected += Server_Disconnected;
            SetupRequestApi?.Invoke(server.RequestApi);
            SetupResponseApi?.Invoke(server.ResponseApi);
            server.Start();
        }

        private void Server_Disconnected(ApiBase api, IOException ex)
        {
            if (api != server)
                return;
            server.Dispose(); // the messages will be discarded, the client has to send the requests again
            pipe.Dispose();
            _ = Connect();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public void Dispose()
        {
            cancellationToken.Dispose();
            pipe?.Dispose();
            server?.Dispose();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Dispose();
            if (pipe != null)
                await pipe.DisposeAsync();
            if (server != null)
                await server.DisposeAsync();
        }
    }
}
