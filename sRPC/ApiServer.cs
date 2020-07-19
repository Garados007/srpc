using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    public class ApiServer<T> : IDisposable
        where T : IApiServerDefinition, new()
    {
        public Stream Input { get; }

        public Stream Output { get; }

        private CancellationTokenSource cancellationToken;

        private readonly ConcurrentQueue<NetworkResponse> queue;
        private readonly T api;
        private readonly SemaphoreSlim mutex;

        public ApiServer(NetworkStream networkStream)
            : this(networkStream, networkStream)
        { }

        public ApiServer(Stream input, Stream output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            queue = new ConcurrentQueue<NetworkResponse>();
            api = new T();
            mutex = new SemaphoreSlim(0, 1);
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

        public void Stop()
        {
            cancellationToken?.Dispose();
            cancellationToken = null;
        }

        public async void Run(NetworkRequest request)
        {
            var response = await api.HandleMessage(request);
            if (response == null)
                return;
            queue.Enqueue(response);
            try { mutex.Release(); }
            catch (SemaphoreFullException) { }
        }

        public void Dispose()
        {
            cancellationToken?.Dispose();
            mutex.Dispose();
            Input.Dispose();
            Output.Dispose();
        }
    }
}
