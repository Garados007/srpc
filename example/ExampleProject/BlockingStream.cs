using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleProject
{
    /// <summary>
    /// this streams blocks the read call until some data was written to.
    /// the implementation may not be ideal but it is good for a showcase.
    /// </summary>
    public class BlockingStream : Stream
    {
        readonly Queue<byte> buffer = new Queue<byte>();
        readonly SemaphoreSlim mutex = new SemaphoreSlim(0, 1);
        readonly object bufferLock = new object();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count; ++i)
            {
                bool hasData;
                lock (bufferLock)
                    if (hasData = this.buffer.TryDequeue(out byte nextByte))
                    {
                        buffer[i + offset] = nextByte;
                        continue;
                    }
                if (!hasData)
                {
                    mutex.Wait();
                    if (!this.buffer.TryPeek(out _))
                        return i;
                }
            }
            return count;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count;)
            {
                bool hasData;
                lock (bufferLock)
                    if (hasData = this.buffer.TryDequeue(out byte nextByte))
                    {
                        buffer[i + offset] = nextByte;
                        ++i;
                        continue;
                    }
                if (!hasData)
                {
                    await mutex.WaitAsync(cancellationToken);
                    if (!this.buffer.TryPeek(out _))
                        return i;
                }
            }
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count; ++i)
                lock (bufferLock)
                    this.buffer.Enqueue(buffer[i + offset]);
            try { mutex.Release(); }
            catch (SemaphoreFullException) { }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mutex.Dispose();
        }
    }
}
