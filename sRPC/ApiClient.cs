using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    public class ApiClient<T> : IDisposable
        where T : IApiClientDefinition, new()
    {
        public Stream Input { get; }

        public Stream Output { get; }

        public T Api { get; }

        private CancellationTokenSource cancellationToken;

        private readonly ConcurrentDictionary<long, CancellationTokenSource> waiter;
        private readonly ConcurrentDictionary<long, NetworkResponse> response;
        private readonly ConcurrentQueue<NetworkRequest> queue;
        private readonly SemaphoreSlim mutex;
        private long nextId;
        private readonly object nextIdLock = new object();

        public ApiClient(NetworkStream networkStream)
            : this(networkStream, networkStream) 
        { }

        public ApiClient(Stream input, Stream output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            waiter = new ConcurrentDictionary<long, CancellationTokenSource>();
            Api = new T();
            Api.PerformMessage += Api_PerformMessage;
            response = new ConcurrentDictionary<long, NetworkResponse>();
            queue = new ConcurrentQueue<NetworkRequest>();
            mutex = new SemaphoreSlim(0, 1);
        }

        private async Task<NetworkResponse> Api_PerformMessage(NetworkRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            long id;
            lock (nextIdLock)
                id = nextId++;
            request.Token = id;
            using (var cancel = new CancellationTokenSource())
            {
                waiter.AddOrUpdate(id, cancel, (_, __) => cancel);
                queue.Enqueue(request);
                try { mutex.Release(); }
                catch (SemaphoreFullException) { }
                try { await Task.Delay(-1, cancel.Token); }
                catch (TaskCanceledException) { }
                if (!this.response.TryRemove(id, out NetworkResponse response))
                    response = null;
                waiter.TryRemove(id, out _);
                return response;
            }

        }

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
                    if (length < 0)
                        continue;
                    buffer = new byte[length];
                    await Input.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token);
                    var nr = new NetworkResponse();
                    nr.MergeFrom(buffer);
                    if (waiter.TryGetValue(nr.Token, out CancellationTokenSource cancellation))
                    {
                        response.TryAdd(nr.Token, nr);
                        cancellation.Cancel();
                    }
                }
            });
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (queue.TryDequeue(out NetworkRequest request))
                    {
                        var buffer = request.ToByteArray();
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

        public void Stop()
        {
            cancellationToken?.Dispose();
            cancellationToken = null;
        }

        public void Dispose()
        {
            cancellationToken?.Dispose();
            mutex.Dispose();
            foreach (var x in waiter.Values)
                x.Cancel();
            Input.Dispose();
            Output.Dispose();
        }
    }
}
