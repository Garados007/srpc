using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    class ClientMessageManager<T> : IApi<T>, IDisposable
        where T : IApiClientDefinition, new()
    {
        class Call : IDisposable
        {
            public long Id { get; }

            public CancellationTokenSource CancellationToken { get; }

            public CancellationToken UserToken { get; }

            public NetworkRequest Request { get; }

            public NetworkResponse Response { get; set; }

            public Call(long id, CancellationToken userToken, NetworkRequest request)
            {
                Id = id;
                CancellationToken = new CancellationTokenSource();
                UserToken = userToken;
                Request = request ?? throw new ArgumentNullException(nameof(request));
            }

            public async Task Wait()
            {
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, UserToken);
                try { await Task.Delay(-1, combined.Token); }
                catch (TaskCanceledException) { }
            }

            public void Dispose()
            {
                CancellationToken.Dispose();
            }
        }

        public T Api { get; }


        private readonly ConcurrentDictionary<long, Call> calls;
        private long nextId;
        private readonly object nextIdLock = new object();

        public ClientMessageManager()
        {
            calls = new ConcurrentDictionary<long, Call>();
            Api = new T();
            if (Api is IApiClientDefinition2 definition2)
                definition2.PerformMessage2 += Api_PerformMessage2;
            else Api.PerformMessage += Api_PerformMessage;
        }

        private async Task<NetworkResponse> Api_PerformMessage2(NetworkRequest request, CancellationToken cancellationToken)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            long id;
            lock (nextIdLock)
                id = nextId++;
            request.Token = id;
            using var call = new Call(id, cancellationToken, request);
            calls.TryAdd(id, call);
            EnqueueNewMessage?.Invoke(call.Request);
            try { await call.Wait(); }
            catch (TaskCanceledException) { }
            calls.TryRemove(id, out _);
            if (call.UserToken.IsCancellationRequested)
            {
                NotifyRequestCancelled?.Invoke(call.Id);
                throw new TaskCanceledException();
            }
            return call.Response ?? throw new TaskCanceledException();
        }

        private Task<NetworkResponse> Api_PerformMessage(NetworkRequest request)
            => Api_PerformMessage2(request, CancellationToken.None);

        public event Action<NetworkRequest> EnqueueNewMessage;

        public event Action<long> NotifyRequestCancelled;

        public IEnumerable<NetworkRequest> GetPendingRequests()
            => calls.Select(x => x.Value.Request);

        public void SetResponse(NetworkResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));
            if (calls.TryGetValue(response.Token, out Call call))
            {
                call.Response = response;
                call.CancellationToken.Cancel();
            }
        }

        public void Dispose()
        {
            foreach (var call in calls)
                call.Value.Dispose();
        }
    }
}
