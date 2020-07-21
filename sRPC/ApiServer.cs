using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    /// <summary>
    /// The Api Server handler to serve requests
    /// </summary>
    /// <typeparam name="T">the api interface to use</typeparam>
    public class ApiServer<T> : ApiBase, IApi<T>
        where T : IApiServerDefinition, new()
    {
        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api { get; }

        /// <summary>
        /// Create a new Api server handler out of an <see cref="NetworkStream"/>
        /// </summary>
        /// <param name="networkStream">the <see cref="NetworkStream"/> to use</param>
        public ApiServer(NetworkStream networkStream)
            : this(networkStream, networkStream)
        { }

        /// <summary>
        /// Create a new Api server handler with specific input and output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="input">the input <see cref="Stream"/> to use</param>
        /// <param name="output">the output <see cref="Stream"/> to use</param>
        public ApiServer(Stream input, Stream output)
            : base(input, output)
        {
            Api = new T();
        }

        protected async override void HandleReceived(byte[] data)
        {
            var request = new NetworkRequest();
            request.MergeFrom(data);
            var response = await Api.HandleMessage(request);
            if (response == null)
                return;
            EnqueueMessage(response);
        }

    }
}
