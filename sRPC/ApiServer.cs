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
    public class ApiServer<T> : IDisposable
        where T : IApiServerDefinition, new()
    {
        /// <summary>
        /// The input stream that is used to read the requests.
        /// </summary>
        public Stream Input { get; }

        /// <summary>
        /// The output stream that is used to write responses.
        /// </summary>
        public Stream Output { get; }

        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api { get; }

        private CancellationTokenSource cancellationToken;

        private readonly ConcurrentQueue<NetworkResponse> queue;
        private readonly SemaphoreSlim mutex;

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
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            queue = new ConcurrentQueue<NetworkResponse>();
            Api = new T();
            mutex = new SemaphoreSlim(0, 1);
        }

        /// <summary>
        /// Start the Api Server handler and listen to requests and send responses.
        /// </summary>
        public void Start()
        {
            if (cancellationToken != null)
                return;
            cancellationToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = new byte[4];
                    if (await Input.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token) != buffer.Length)
                        continue;
                    var length = BitConverter.ToInt32(buffer, 0);
                    if (length <= 0)
                        continue;
                    buffer = new byte[length];
                    await Input.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token);
                    var nr = new NetworkRequest();
                    nr.MergeFrom(buffer);
                    Run(nr);
                }
            });
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (queue.TryDequeue(out NetworkResponse response))
                    {
                        var buffer = response.ToByteArray();
                        await Output.WriteAsync(BitConverter.GetBytes(buffer.Length), 0, 4, cancellationToken.Token);
                        await Output.WriteAsync(buffer, 0, buffer.Length, cancellationToken.Token);
                    }
                    else
                    {
                        await mutex.WaitAsync(cancellationToken.Token);
                    }
                }
            });
        }

        /// <summary>
        /// Stop the Api Server handler from listening to requests and sending responses.
        /// </summary>
        public void Stop()
        {
            cancellationToken?.Dispose();
            cancellationToken = null;
        }

        protected async void Run(NetworkRequest request)
        {
            var response = await Api.HandleMessage(request);
            if (response == null)
                return;
            queue.Enqueue(response);
            try { mutex.Release(); }
            catch (SemaphoreFullException) { }
        }


        /// <summary>
        /// Dispose the Api Server handler and release its resources.
        /// </summary>
        public void Dispose()
        {
            cancellationToken?.Dispose();
            mutex.Dispose();
            Input.Dispose();
            Output.Dispose();
        }
    }
}
