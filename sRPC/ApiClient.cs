using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private ClientMessageManager<T> manager;

        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api => manager.Api;

        protected override IMessage[] GetMessages()
        {
            return manager.GetPendingRequests()
                .Union(base.GetMessages())
                .Distinct()
                .Cast<IMessage>()
                .ToArray();
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
            manager = new ClientMessageManager<T>();
            manager.EnqueueNewMessage += Manager_EnqueueNewMessage;
            manager.NotifyRequestCancelled += Manager_NotifyRequestCancelled;
        }

        protected internal ApiClient(Stream input, Stream output, ApiClient<T> oldClient)
            : base(input, output)
        {
            if (oldClient != null)
            {
                manager = oldClient.manager;
                manager.EnqueueNewMessage += Manager_EnqueueNewMessage;
                manager.NotifyRequestCancelled += Manager_NotifyRequestCancelled;
                oldClient.DisconnectHook();
                PushMessage(oldClient.GetMessages());
                oldClient.manager = null;
                oldClient.Dispose();
            }
            else
            {
                manager = new ClientMessageManager<T>();
                manager.EnqueueNewMessage += Manager_EnqueueNewMessage;
                manager.NotifyRequestCancelled += Manager_NotifyRequestCancelled;
            }
        }

        private void DisconnectHook()
        {
            manager.EnqueueNewMessage -= Manager_EnqueueNewMessage;
            manager.NotifyRequestCancelled -= Manager_NotifyRequestCancelled;
        }

        private void Manager_NotifyRequestCancelled(long id)
        {
            var request = new NetworkRequest();
            request.CancelRequests.Add(id);
            EnqueueMessage(request);
        }

        private void Manager_EnqueueNewMessage(NetworkRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            EnqueueMessage(request);
        }

        protected override void HandleReceived(byte[] data)
        {
            var nr = new NetworkResponse();
            nr.MergeFrom(data);
            manager.SetResponse(nr);
        }

        /// <summary>
        /// Dispose the Api Client handler and release its resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            manager?.Dispose();
        }

        /// <summary>
        /// Dispose the Api Client handler and release its resources.
        /// </summary>
        public async override ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            manager?.Dispose();
        }
    }
}
