﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiClient{T}"/> wrapper for a <see cref="NamedPipeClientStream"/>. This
    /// manages the connection and automatic reconnection of it.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class NamedApiClient<T> : IDisposable, IAsyncDisposable, IApi<T>
        where T : class, IApiClientDefinition, new()
    {
        private ApiClient<T> client;
        private NamedPipeClientStream pipe;
        private readonly CancellationTokenSource cancellationToken;

        /// <summary>
        /// The current API interface. Do not cache this object, because it can
        /// change after a reconnection
        /// </summary>
        public T Api => client?.Api;

        /// <summary>
        /// The name of the server. "." is for the local computer.
        /// </summary>
        public string ServerName { get; }

        /// <summary>
        /// The name of the pipe on the server
        /// </summary>
        public string PipeName { get; }

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// A task that finishes when the first <see cref="Api"/> has been created.
        /// </summary>
        public Task WaitConnect { get; }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for a <see cref="NamedPipeClientStream"/>.
        /// </summary>
        /// <param name="serverName">the server name. "." for the local computer</param>
        /// <param name="pipeName">the name of the pipe</param>
        public NamedApiClient(string serverName, string pipeName)
            : this(serverName, pipeName, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for a <see cref="NamedPipeClientStream"/>.
        /// </summary>
        /// <param name="serverName">the server name. "." for the local computer</param>
        /// <param name="pipeName">the name of the pipe</param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public NamedApiClient(string serverName, string pipeName, Action<T> setupApi)
        {
            ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            SetupApi = setupApi;
            cancellationToken = new CancellationTokenSource();
            WaitConnect = Connect();
        }

        private async Task Connect(ApiClient<T> oldApi = null)
        {
            pipe = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(cancellationToken.Token);
            if (cancellationToken.IsCancellationRequested)
                return;
            client = new ApiClient<T>(pipe, pipe, oldApi);
            client.Disconnected += Client_Disconnected;
            if (oldApi == null)
                SetupApi?.Invoke(client.Api);
            client.Start();
        }

        private void Client_Disconnected(ApiBase api, IOException ex)
        {
            if (api != client)
                return;
            pipe.Dispose();
            _ = Connect((ApiClient<T>)api);
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public void Dispose()
        {
            cancellationToken.Dispose();
            pipe?.Dispose();
            client?.Dispose();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Dispose();
            if (pipe != null)
                await pipe.DisposeAsync();
            if (client != null)
                await client.DisposeAsync();
        }
    }

    /// <summary>
    /// The <see cref="ApiClient{TRequest, TResponse}"/> wrapper for a <see cref="NamedPipeClientStream"/>. This
    /// manages the connection and automatic reconnection of it.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API inteface for making requests</typeparam>
    /// <typeparam name="TResponse">the type of the API interface for responding</typeparam>
    public class NamedApiClient<TRequest, TResponse> : IDisposable, IAsyncDisposable, IApi<TRequest>, IApi<TRequest, TResponse>
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private ApiClient<TRequest, TResponse> client;
        private NamedPipeClientStream pipe;
        private readonly CancellationTokenSource cancellationToken;

        /// <summary>
        /// The current Api interface for creating Api requests. Do not cache this object, because it can
        /// change after a reconnection
        /// </summary>
        public TRequest RequestApi => client.RequestApi;

        /// <summary>
        /// The current Api interface for responding Api requests. Do not cache this object, because it can
        /// change after a reconnection
        /// </summary>
        public TResponse ResponseApi => client.ResponseApi;

        TRequest IApi<TRequest>.Api => client.RequestApi;

        /// <summary>
        /// The name of the server. "." is for the local computer.
        /// </summary>
        public string ServerName { get; }

        /// <summary>
        /// The name of the pipe on the server
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
        /// A task that finishes when the first <see cref="Api"/> has been created.
        /// </summary>
        public Task WaitConnect { get; }

        /// <summary>
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for a <see cref="NamedPipeClientStream"/>.
        /// </summary>
        /// <param name="serverName">the server name. "." for the local computer</param>
        /// <param name="pipeName">the name of the pipe</param>
        public NamedApiClient(string serverName, string pipeName)
            : this(serverName, pipeName, null, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for a <see cref="NamedPipeClientStream"/>.
        /// </summary>
        /// <param name="serverName">the server name. "." for the local computer</param>
        /// <param name="pipeName">the name of the pipe</param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public NamedApiClient(string serverName, string pipeName,
            Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            cancellationToken = new CancellationTokenSource();
            WaitConnect = Connect();
        }

        private async Task Connect(ApiClient<TRequest, TResponse> oldApi = null)
        {
            pipe = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(cancellationToken.Token);
            if (cancellationToken.IsCancellationRequested)
                return;
            client = new ApiClient<TRequest, TResponse>(pipe, pipe, oldApi);
            client.Disconnected += Client_Disconnected;
            if (oldApi == null)
            {
                SetupRequestApi?.Invoke(client.RequestApi);
                SetupResponseApi?.Invoke(client.ResponseApi);
            }
            client.Start();
        }

        private void Client_Disconnected(ApiBase api, IOException ex)
        {
            if (api != client)
                return;
            pipe.Dispose();
            _ = Connect((ApiClient<TRequest, TResponse>)api);
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public void Dispose()
        {
            cancellationToken.Dispose();
            pipe?.Dispose();
            client?.Dispose();
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Dispose();
            if (pipe != null)
                await pipe.DisposeAsync();
            if (client != null)
                await client.DisposeAsync();
        }
    }
}
