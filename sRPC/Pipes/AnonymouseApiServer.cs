using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Pipes;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiServer{T}"/> wrapper for <see cref="AnonymousPipeServerStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AnonymouseApiServer<T> : IDisposable, IApi<T>
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
        public SafePipeHandle InputPipe => inputPipe.SafePipeHandle;

        /// <summary>
        /// The current <see cref="SafePipeHandle"/> of the output pipe.
        /// </summary>
        public SafePipeHandle OutputPipe => outputPipe.SafePipeHandle;

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
            inputPipe.DisposeLocalCopyOfClientHandle();
            outputPipe.DisposeLocalCopyOfClientHandle();
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
    }
}
