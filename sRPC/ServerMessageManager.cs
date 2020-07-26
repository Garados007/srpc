using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    class ServerMessageManager<T> : IApi<T>, IDisposable
        where T : IApiServerDefinition, new()
    {
        class Call : IDisposable
        {
            public long Id { get; }

            public CancellationTokenSource CancellationToken { get; }

            public Call(long id)
            {
                Id = id;
                CancellationToken = new CancellationTokenSource();
            }

            public void Dispose()
            {
                CancellationToken.Dispose();
            }
        }

        public T Api { get; }

        private readonly ConcurrentDictionary<long, Call> calls;

        public ServerMessageManager()
        {
            Api = new T();
            calls = new ConcurrentDictionary<long, Call>();
        }

        public event Action<NetworkResponse> SubmitResponse;

        public void HandleReceived(NetworkRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            Task.Run(async () =>
            {
                foreach (var id in request.CancelRequests)
                    if (calls.TryGetValue(id, out Call call))
                    {
                        call.CancellationToken.Cancel();
                    }
                if (!string.IsNullOrEmpty(request.ApiFunction))
                { 
                    var call = new Call(request.Token);
                    calls.TryAdd(request.Token, call);
                    NetworkResponse response;
                    if (Api is IApiServerDefinition2 api2)
                        response = await api2.HandleMessage2(request, call.CancellationToken.Token);
                    else response = await Api.HandleMessage(request);
                    SubmitResponse?.Invoke(response);
                }
            });
        }

        public void Dispose()
        {
            foreach (var call in calls)
                call.Value.Dispose();
        }
    }
}
