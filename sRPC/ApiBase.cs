﻿using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace sRPC
{
    /// <summary>
    /// The base definitions for an Api handler
    /// </summary>
    public abstract class ApiBase : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The input stream
        /// </summary>
        public Stream Input { get; }

        /// <summary>
        /// The output stream
        /// </summary>
        public Stream Output { get; }

        private CancellationTokenSource cancellationToken;

        private readonly ConcurrentQueue<IMessage> queue;
        private readonly SemaphoreSlim mutex;

        protected virtual IMessage[] GetMessages()
            => queue.ToArray();

        protected virtual void PushMessage(IMessage[] messages)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            if (messages.Length == 0)
                return;
            foreach (var m in messages)
                queue.Enqueue(m);
            try { mutex.Release(); }
            catch (SemaphoreFullException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Create a new Api handler with the specified input and output <see cref="Stream"/>s
        /// </summary>
        /// <param name="input">the input <see cref="Stream"/> to use</param>
        /// <param name="output">the output <see cref="Stream"/> to use</param>
        public ApiBase(Stream input, Stream output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            queue = new ConcurrentQueue<IMessage>();
            mutex = new SemaphoreSlim(0, 1);
        }

        /// <summary>
        /// This fires if a <see cref="IOException"/> was fired when
        /// working with the streams. This happens the most time
        /// if the connection was closed.
        /// </summary>
        public event Action<ApiBase, IOException> Disconnected;

        /// <summary>
        /// Start the Api handler to listen and send messages
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
                    try
                    {
                        if (await Input.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token) != buffer.Length)
                            continue;
                    }
                    catch (IOException e)
                    {
                        Disconnected?.Invoke(this, e);
                        continue;
                    }
                    catch (TaskCanceledException) { continue; }
                    var length = BitConverter.ToInt32(buffer, 0);
                    if (length < 0)
                        continue;
                    buffer = new byte[length];
                    try
                    {
                        var readed = 0;
                        while (readed < length)
                        {
                            var r = await Input.ReadAsync(
                                buffer, 
                                readed, 
                                buffer.Length - readed, 
                                cancellationToken.Token);
                            readed += r;
                            if (r == 0)
                                break;
                        }
                    }
                    catch (IOException e)
                    {
                        Disconnected?.Invoke(this, e);
                        continue;
                    }
                    catch (TaskCanceledException) { continue; }
                    HandleReceived(buffer);
                }
            });
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (queue.TryDequeue(out IMessage request))
                    {
                        var buffer = request.ToByteArray();
                        try
                        {
                            await Output.WriteAsync(BitConverter.GetBytes(buffer.Length), 0, 4, cancellationToken.Token);
                            await Output.WriteAsync(buffer, 0, buffer.Length, cancellationToken.Token);
                        }
                        catch (IOException e)
                        {
                            Disconnected?.Invoke(this, e);
                            queue.Enqueue(request);
                            continue;
                        }
                        catch (TaskCanceledException) { }
                    }
                    else
                    {
                        await mutex.WaitAsync(cancellationToken.Token);
                    }
                }
            });
        }

        /// <summary>
        /// Stop the Api handler from listening and sending messages
        /// </summary>
        public void Stop()
        {
            cancellationToken?.Dispose();
            cancellationToken = null;
        }

        /// <summary>
        /// Handle the received message data
        /// </summary>
        /// <param name="data">the binary data that was received</param>
        protected abstract void HandleReceived(byte[] data);

        /// <summary>
        /// Enqueue the specified message to the output queue
        /// </summary>
        /// <param name="message">the message to enqueue</param>
        protected void EnqueueMessage(IMessage message)
        {
            queue.Enqueue(message);
            try { mutex.Release(); }
            catch (SemaphoreFullException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Dispose the Api handler and release its resources.
        /// </summary>
        public virtual void Dispose()
        {
            cancellationToken?.Dispose();
            mutex.Dispose();
            Input.Dispose();
            Output.Dispose();
        }

        public virtual async ValueTask DisposeAsync()
        {
            cancellationToken.Dispose();
            mutex.Dispose();
            await Input.DisposeAsync();
            await Output.DisposeAsync();
        }
    }
}
