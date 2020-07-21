using Microsoft.Win32.SafeHandles;
using System;
using System.IO.Pipes;

namespace sRPC.Pipes
{
    /// <summary>
    /// The <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>s.
    /// There is no automatic reconnection because of the nature of anonymous pipes.
    /// </summary>
    /// <typeparam name="T">the type of the API interface</typeparam>
    public class AnonymousApiClient<T> : IDisposable
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
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(string inputPipeHandle, string outputPipeHandle)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            client = new ApiClient<T>(input, output);
            client.Start();
        }

        /// <summary>
        /// Create a <see cref="ApiClient{T}"/> wrapper for <see cref="AnonymousPipeClientStream"/>.
        /// </summary>
        /// <param name="inputPipeHandle">the input pipe handle</param>
        /// <param name="outputPipeHandle">the output pipe handle</param>
        public AnonymousApiClient(SafePipeHandle inputPipeHandle, string outputPipeHandle)
        {
            _ = inputPipeHandle ?? throw new ArgumentNullException(nameof(inputPipeHandle));
            _ = outputPipeHandle ?? throw new ArgumentNullException(nameof(outputPipeHandle));
            input = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            output = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            client = new ApiClient<T>(input, output);
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
    }
}
