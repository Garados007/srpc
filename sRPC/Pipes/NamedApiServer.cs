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
            Connect();
        }

        private async void Connect()
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
            Connect();
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
