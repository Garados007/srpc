using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    /// <summary>
    /// The Api Client handler to create requests
    /// </summary>
    /// <typeparam name="T">the api interface to use</typeparam>
    public class ApiClient<T> : ApiBase, IApi<T>
        where T : IApiClientDefinition, new()
    {

        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api { get; }

        private readonly ConcurrentDictionary<long, CancellationTokenSource> waiter;
        private readonly ConcurrentDictionary<long, NetworkResponse> response;
        private readonly ConcurrentDictionary<long, IMessage> outgoingMessages;
        private long nextId;
        private readonly object nextIdLock = new object();

        protected override IMessage[] GetMessages()
        {
            return base.GetMessages()
                .Concat(outgoingMessages.Values)
                .ToArray();
        }

        protected override void PushMessage(IMessage[] messages)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            foreach (var m in messages)
            {
                if (m is NetworkRequest request)
                {
                    long id;
                    lock (nextIdLock)
                        id = nextId++;
                    request.Token = id;
                }
            }
            base.PushMessage(messages);
        }

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
            outgoingMessages = new ConcurrentDictionary<long, IMessage>();
            Api = new T();
            Api.PerformMessage += Api_PerformMessage;
        }

        protected internal ApiClient(Stream input, Stream output, ApiClient<T> oldClient)
            : base(input, output)
        {
            waiter = new ConcurrentDictionary<long, CancellationTokenSource>();
            response = oldClient?.response ?? new ConcurrentDictionary<long, NetworkResponse>();
            outgoingMessages = oldClient?.outgoingMessages ?? new ConcurrentDictionary<long, IMessage>();
            if (oldClient != null)
            {
                Api = oldClient.Api;
                lock (nextIdLock)
                    lock (oldClient.nextIdLock)
                    {
                        Api.PerformMessage += Api_PerformMessage;
                        oldClient.DisconnectHook();
                        nextId = oldClient.nextId;
                    }
                foreach (var p in oldClient.waiter)
                    waiter.TryAdd(p.Key, p.Value);
                oldClient.waiter.Clear();
                PushMessage(oldClient.GetMessages());
                oldClient.Dispose();
            }
            else
            {
                Api = new T();
                Api.PerformMessage += Api_PerformMessage;
            }
        }

        private void DisconnectHook()
        {
            Api.PerformMessage -= Api_PerformMessage;
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
            outgoingMessages.TryAdd(id, request);
            EnqueueMessage(request);
            try { await Task.Delay(-1, cancel.Token); }
            catch (TaskCanceledException) { }
            if (!this.response.TryRemove(id, out NetworkResponse response))
                response = null;
            outgoingMessages.TryRemove(id, out _);
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
