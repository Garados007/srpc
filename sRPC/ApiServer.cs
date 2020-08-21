using Google.Protobuf;
using System;
using System.IO;
using System.Net.Sockets;
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
        private readonly ServerMessageManager<T> manager;

        /// <summary>
        /// The current Api interface
        /// </summary>
        public T Api => manager.Api;

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
            manager = new ServerMessageManager<T>();
            manager.SubmitResponse += Manager_SubmitResponse;
        }

        private void Manager_SubmitResponse(NetworkResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));
            EnqueueMessage(response);
        }

        protected override void HandleReceived(byte[] data)
        {
            var request = new NetworkRequest();
            request.MergeFrom(data);
            manager.HandleReceived(request);
        }

        /// <summary>
        /// Dispose the Api Server handler and release its resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            manager?.Dispose();
        }

        /// <summary>
        /// Dispose the Api Server handler and release its resources.
        /// </summary>
        public async override ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            manager?.Dispose();
        }
    }

    /// <summary>
    /// The Api Server handler which handles a single connection to an Api Client handler. It
    /// will establish a bidirectional Api connection.
    /// </summary>
    /// <typeparam name="TRequest">the api interface to create requests</typeparam>
    /// <typeparam name="TResponse">the api interface to responds requests</typeparam>
    public class ApiServer<TRequest, TResponse> : ApiBase, IApi<TRequest>, IApi<TRequest, TResponse>
        where TRequest : IApiClientDefinition, new()
        where TResponse : IApiServerDefinition, new()
    {
        private ClientMessageManager<TRequest> reqManager;
        private readonly ServerMessageManager<TResponse> respManager;

        /// <summary>
        /// The current Api interface for creating Api requests
        /// </summary>
        public TRequest RequestApi => reqManager.Api;

        /// <summary>
        /// The current Api interface for responding Api requests
        /// </summary>
        public TResponse ResponseApi => respManager.Api;

        TRequest IApi<TRequest>.Api => reqManager.Api;

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
            reqManager = new ClientMessageManager<TRequest>();
            reqManager.EnqueueNewMessage += ReqManager_EnqueueNewMessage;
            reqManager.NotifyRequestCancelled += ReqManager_NotifyRequestCancelled;

            respManager = new ServerMessageManager<TResponse>();
            respManager.SubmitResponse += RespManager_SubmitResponse;
        }

        protected internal ApiServer(Stream input, Stream output, ApiServer<TRequest, TResponse> oldServer)
            : base(input, output)
        {
            if (oldServer != null)
            {
                reqManager = oldServer.reqManager;
                reqManager.EnqueueNewMessage += ReqManager_EnqueueNewMessage;
                reqManager.NotifyRequestCancelled += ReqManager_NotifyRequestCancelled;
                oldServer.DisconnectHook();
                PushMessage(oldServer.GetMessages());
                oldServer.reqManager = null;
                oldServer.Dispose();
            }
            else
            {
                reqManager = new ClientMessageManager<TRequest>();
                reqManager.EnqueueNewMessage += ReqManager_EnqueueNewMessage;
                reqManager.NotifyRequestCancelled += ReqManager_NotifyRequestCancelled;
            }

            respManager = new ServerMessageManager<TResponse>();
            respManager.SubmitResponse += RespManager_SubmitResponse;
        }

        private void DisconnectHook()
        {
            reqManager.EnqueueNewMessage -= ReqManager_EnqueueNewMessage;
            reqManager.NotifyRequestCancelled -= ReqManager_NotifyRequestCancelled;
        }

        private void ReqManager_NotifyRequestCancelled(long id)
        {
            var request = new NetworkRequest();
            request.CancelRequests.Add(id);
            request.Reverse = true;
            EnqueueMessage(request);
        }

        private void ReqManager_EnqueueNewMessage(NetworkRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            request.Reverse = true;
            EnqueueMessage(request);
        }

        private void RespManager_SubmitResponse(NetworkResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));
            EnqueueMessage(response);
        }

        protected override void HandleReceived(byte[] data)
        {
            var nr = new NetworkResponse();
            nr.MergeFrom(data);
            if (nr.Reverse)
                reqManager.SetResponse(nr);
            else
            {
                var req = new NetworkRequest();
                req.MergeFrom(data);
                respManager.HandleReceived(req);
            }
        }

        /// <summary>
        /// Dispose the Api Server handler and release its resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            reqManager?.Dispose();
            respManager?.Dispose();
        }

        /// <summary>
        /// Dispose the Api Server handler and release its resources.
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            reqManager?.Dispose();
            respManager?.Dispose();
        }
    }
}
