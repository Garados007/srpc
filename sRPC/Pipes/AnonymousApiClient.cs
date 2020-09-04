using Microsoft.Win32.SafeHandles;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class AnonymousApiClient<T> : IDisposable, IAsyncDisposable, IApi<T>
        where T : IApiClientDefinition, new()
    {
        private readonly ApiClient<T> client;
        private readonly AnonymousPipeClientStream input;
        private readonly AnonymousPipeClientStream output;

        /// <summary>
        /// The current API interface.
        /// </summary>
        public T Api => client.Api;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the input pipe.
        /// </summary>
        public SafePipeHandle InputPipe => input.SafePipeHandle;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the output pipe.
        /// </summary>
        public SafePipeHandle OutputPipe => output.SafePipeHandle;

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(string inputPipeHandle, string outputPipeHandle)
            : this(inputPipeHandle, outputPipeHandle, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(SafePipeHandle inputPipeHandle, SafePipeHandle outputPipeHandle)
            : this(inputPipeHandle, outputPipeHandle, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public AnonymousApiClient(string inputPipeHandle, string outputPipeHandle, Action<T> setupApi)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            SetupApi = setupApi;
            client = new ApiClient<T>(input, output);
            setupApi?.Invoke(client.Api);
            client.Start();
        }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public AnonymousApiClient(SafePipeHandle inputPipeHandle, SafePipeHandle outputPipeHandle, Action<T> setupApi)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            SetupApi = setupApi;
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            client = new ApiClient<T>(input, output);
            client.Disconnected += (_, __) => Disconnected?.Invoke();
            setupApi?.Invoke(client.Api);
            client.Start();
        }

        /// <summary>
        /// This event fires if the connection is broken. You need to create a new 
        /// <see cref="AnonymousApiClient{T}"/> with new pipe handles.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public void Dispose()
        {
            client.Dispose();
            input.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
            await input.DisposeAsync();
            await output.DisposeAsync();
        }
    }

    /// <summary>
    /// The <see cref="ApiClient{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeClientStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API inteface for making requests</typeparam>
    /// <typeparam name="TResponse">the type of the API interface for responding</typeparam>
    public class AnonymousApiClient<TRequest, TResponse> : IDisposable, IAsyncDisposable, IApi<TRequest>, IApi<TRequest, TResponse>
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private readonly ApiClient<TRequest, TResponse> client;
        private readonly AnonymousPipeClientStream input;
        private readonly AnonymousPipeClientStream output;

        /// <summary>
        /// The current Api interface for creating Api requests
        /// </summary>
        public TRequest RequestApi => client.RequestApi;

        /// <summary>
        /// The current Api interface for responding Api requests
        /// </summary>
        public TResponse ResponseApi => client.ResponseApi;

        TRequest IApi<TRequest>.Api => client.RequestApi;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the input pipe.
        /// </summary>
        public SafePipeHandle InputPipe => input.SafePipeHandle;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the output pipe.
        /// </summary>
        public SafePipeHandle OutputPipe => output.SafePipeHandle;

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
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(string inputPipeHandle, string outputPipeHandle)
            : this(inputPipeHandle, outputPipeHandle, null, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(SafePipeHandle inputPipeHandle, SafePipeHandle outputPipeHandle)
            : this(inputPipeHandle, outputPipeHandle, null, null)
        {
        }

        /// <summary>
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        /// <param name="setupRequestApi">the initializer for the api interface before the client is started</param>
        /// <param name="setupResponseApi">the initializer for the api interface before the client is started</param>
        public AnonymousApiClient(string inputPipeHandle, string outputPipeHandle, 
            Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            client = new ApiClient<TRequest, TResponse>(input, output);
            setupRequestApi?.Invoke(client.RequestApi);
            setupResponseApi?.Invoke(client.ResponseApi);
            client.Start();
        }

        /// <summary>
        /// Create a <see cref="ApiClient{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public AnonymousApiClient(SafePipeHandle inputPipeHandle, SafePipeHandle outputPipeHandle,
            Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            client = new ApiClient<TRequest, TResponse>(input, output);
            client.Disconnected += (_, __) => Disconnected?.Invoke();
            setupRequestApi?.Invoke(client.RequestApi);
            setupResponseApi?.Invoke(client.ResponseApi);
            client.Start();
        }

        /// <summary>
        /// This event fires if the connection is broken. You need to create a new 
        /// <see cref="AnonymousApiClient{T}"/> with new pipe handles.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public void Dispose()
        {
            client.Dispose();
            input.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
            await input.DisposeAsync();
            await output.DisposeAsync();
        }
    }
}
