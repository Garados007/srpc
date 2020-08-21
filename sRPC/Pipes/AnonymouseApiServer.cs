using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AnonymouseApiServer<T> : IDisposable, IAsyncDisposable, IApi<T>
        where T : IApiServerDefinition, new()
    {
        private readonly ApiServer<T> apiServer;
        private readonly AnonymousPipeServerStream inputPipe;
        private readonly AnonymousPipeServerStream outputPipe;

        /// <summary>
        /// The current API interface
        /// </summary>
        public T Api => apiServer.Api;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the input pipe.
        /// </summary>
        public SafePipeHandle InputPipe => inputPipe.ClientSafePipeHandle;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the output pipe.
        /// </summary>
        public SafePipeHandle OutputPipe => outputPipe.ClientSafePipeHandle;

        /// <summary>
        /// Get the string representation of the input and output pipe handles.
        /// This can be used to create a <see cref="AnonymousApiClient{T}"/>.
        /// You need to call <see cref="DisposeLocalCopyOfClientHandle"/> if
        /// the client received the handles.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public void GetPipeHandles(out string inputPipeHandle, out string outputPipeHandle)
        {
            inputPipeHandle = inputPipe.GetClientHandleAsString();
            outputPipeHandle = outputPipe.GetClientHandleAsString();
        }

        /// <summary>
        /// Dispose the local copys of the pipe handles that are needed to create
        /// a <see cref="AnonymousApiClient{T}"/>.
        /// </summary>
        public void DisposeLocalCopysOfPipeHandle()
        {
            /* The code is commented because of two bugs.
             *   inputPipe.DisposeLocalCopyOfClientHandle();
             *     This will close the pipes itself and make them
             *     unusable.
             *   outputPipe.DisposeLocalCopyOfClientHandle();
             *     This blocks the execution and never completes.
             *     
             * As of the official documentation this step is required
             * to ensure there a no dupplicate pipe client handles and the
             * server can notice the pipe closing.
             * 
             * https://docs.microsoft.com/en-us/dotnet/api/system.io.pipes.anonymouspipeserverstream?view=netcore-3.1
             * 
             * I will leave this commented - maybe there is
             * a better solution
             */

            //inputPipe.DisposeLocalCopyOfClientHandle();
            //outputPipe.DisposeLocalCopyOfClientHandle();
        }

        /// <summary>
        /// The initializer that whould be used to initialize
        /// the <typeparamref name="T"/> Api.
        /// </summary>
        public Action<T> SetupApi { get; }

        /// <summary>
        /// Create a new <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>.
        /// </summary>
        public AnonymouseApiServer()
            : this(null)
        {
        }

        /// <summary>
        /// Create a new <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>.
        /// </summary>
        /// <param name="setupApi">the initializer for the api interface before the client is started</param>
        public AnonymouseApiServer(Action<T> setupApi)
        {
            inputPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            outputPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            SetupApi = setupApi;
            apiServer = new ApiServer<T>(inputPipe, outputPipe);
            apiServer.Disconnected += (_, __) => Disconnected?.Invoke();
            setupApi?.Invoke(apiServer.Api);
            apiServer.Start();
        }

        /// <summary>
        /// This event fires if the connection is broken. You need to create a new 
        /// <see cref="AnonymousApiServer{T}"/> to create new pipe handles.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public void Dispose()
        {
            apiServer.Dispose();
            inputPipe.Dispose();
            outputPipe.Dispose();
        }

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await apiServer.DisposeAsync();
            await inputPipe.DisposeAsync();
            await outputPipe.DisposeAsync();
        }
    }

    /// <summary>
    /// The <see cref="ApiServer{TRequest, TResponse}"/> wrapper for <see cref="AnonymousPipeServerStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API inteface for making requests</typeparam>
    /// <typeparam name="TResponse">the type of the API interface for responding</typeparam>
    public class AnonymouseApiServer<TRequest, TResponse> : IDisposable, IAsyncDisposable, IApi<TRequest>, IApi<TRequest, TResponse>
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private readonly ApiServer<TRequest, TResponse> apiServer;
        private readonly AnonymousPipeServerStream inputPipe;
        private readonly AnonymousPipeServerStream outputPipe;

        /// <summary>
        /// The current Api interface for creating Api requests
        /// </summary>
        public TRequest RequestApi => apiServer.RequestApi;

        /// <summary>
        /// The current Api interface for responding Api requests
        /// </summary>
        public TResponse ResponseApi => apiServer.ResponseApi;

        TRequest IApi<TRequest>.Api => apiServer.RequestApi;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the input pipe.
        /// </summary>
        public SafePipeHandle InputPipe => inputPipe.ClientSafePipeHandle;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the output pipe.
        /// </summary>
        public SafePipeHandle OutputPipe => outputPipe.ClientSafePipeHandle;

        /// <summary>
        /// Get the string representation of the input and output pipe handles.
        /// This can be used to create a <see cref="AnonymousApiClient{T}"/>.
        /// You need to call <see cref="DisposeLocalCopyOfClientHandle"/> if
        /// the client received the handles.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public void GetPipeHandles(out string inputPipeHandle, out string outputPipeHandle)
        {
            inputPipeHandle = inputPipe.GetClientHandleAsString();
            outputPipeHandle = outputPipe.GetClientHandleAsString();
        }

        /// <summary>
        /// Dispose the local copys of the pipe handles that are needed to create
        /// a <see cref="AnonymousApiClient{T}"/>.
        /// </summary>
        public void DisposeLocalCopysOfPipeHandle()
        {
            /* The code is commented because of two bugs.
             *   inputPipe.DisposeLocalCopyOfClientHandle();
             *     This will close the pipes itself and make them
             *     unusable.
             *   outputPipe.DisposeLocalCopyOfClientHandle();
             *     This blocks the execution and never completes.
             *     
             * As of the official documentation this step is required
             * to ensure there a no dupplicate pipe client handles and the
             * server can notice the pipe closing.
             * 
             * https://docs.microsoft.com/en-us/dotnet/api/system.io.pipes.anonymouspipeserverstream?view=netcore-3.1
             * 
             * I will leave this commented - maybe there is
             * a better solution
             */

            //inputPipe.DisposeLocalCopyOfClientHandle();
            //outputPipe.DisposeLocalCopyOfClientHandle();
        }

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
        /// Create a new <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>.
        /// </summary>
        public AnonymouseApiServer()
            : this(null, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>.
        /// </summary>
        /// <param name="setupRequestApi">the initializer for the api interface before the server is started</param>
        /// <param name="setupResponseApi">the initializer for the api interface before the server is started</param>
        public AnonymouseApiServer(Action<TRequest> setupRequestApi, Action<TResponse> setupResponseApi)
        {
            inputPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            outputPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            SetupRequestApi = setupRequestApi;
            SetupResponseApi = setupResponseApi;
            apiServer = new ApiServer<TRequest, TResponse>(inputPipe, outputPipe);
            apiServer.Disconnected += (_, __) => Disconnected?.Invoke();
            setupRequestApi?.Invoke(apiServer.RequestApi);
            setupResponseApi?.Invoke(apiServer.ResponseApi);
            apiServer.Start();
        }

        /// <summary>
        /// This event fires if the connection is broken. You need to create a new 
        /// <see cref="AnonymousApiServer{T}"/> to create new pipe handles.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public void Dispose()
        {
            apiServer.Dispose();
            inputPipe.Dispose();
            outputPipe.Dispose();
        }

        /// <summary>
        /// Dispose all used resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await apiServer.DisposeAsync();
            await inputPipe.DisposeAsync();
            await outputPipe.DisposeAsync();
        }
    }
}
