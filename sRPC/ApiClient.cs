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
    /// The Api Client handler to create requests
    /// </summary>
    /// <typeparam name="T">the api interface to use</typeparam>
    public class ApiClient<T> : ApiBase
        where T : IApiClientDefinition, new()
    {

        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api { get; }

        private readonly ConcurrentDictionary<long, CancellationTokenSource> waiter;
        private readonly ConcurrentDictionary<long, NetworkResponse> response;
        private long nextId;
        private readonly object nextIdLock = new object();

        /// <summary>
        /// Create a new Api client handler out of an <see cref="NetworkStream"/>
        /// </summary>
        /// <param name="networkStream">the <see cref="NetworkStream"/> to use</param>
        public ApiClient(NetworkStream networkStream)
            : this(networkStream, networkStream) 
        { }

        /// <summary>
        /// Create a new Api client handler with specific input and output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="input">the input <see cref="Stream"/> to use</param>
        /// <param name="output">the output <see cref="Stream"/> to use</param>
        public ApiClient(Stream input, Stream output)
            : base(input, output)
        {
            waiter = new ConcurrentDictionary<long, CancellationTokenSource>();
            response = new ConcurrentDictionary<long, NetworkResponse>();
            Api = new T();
            Api.PerformMessage += Api_PerformMessage;
        }

        private async Task<NetworkResponse> Api_PerformMessage(NetworkRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            long id;
            lock (nextIdLock)
                id = nextId++;
            request.Token = id;
            using var cancel = new CancellationTokenSource();
            waiter.AddOrUpdate(id, cancel, (_, __) => cancel);
            EnqueueMessage(request);
            try { await Task.Delay(-1, cancel.Token); }
            catch (TaskCanceledException) { }
            if (!this.response.TryRemove(id, out NetworkResponse response))
                response = null;
            waiter.TryRemove(id, out _);
            return response;

        }

        protected override void HandleReceived(byte[] data)
        {
            var nr = new NetworkResponse();
            nr.MergeFrom(data);
            if (waiter.TryGetValue(nr.Token, out CancellationTokenSource cancellation))
            {
                response.TryAdd(nr.Token, nr);
                cancellation.Cancel();
            }
        }

        /// <summary>
        /// Dispose the Api Client handler and release its resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            foreach (var x in waiter.Values)
                x.Cancel();
        }
    }
}
