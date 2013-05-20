using SharedProtocol.Framing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    /// <summary>
    /// This stream generates HTTP/2.0 binary data frames, one per write.
    /// It also tracks flow control and causes backpressure by not completing writes
    /// when the flow control credit runs out.
    /// </summary>
    public class OutputStream : Stream
    {
        private readonly WriteQueue _writeQueue;
        private readonly int _streamId;
        private readonly Priority _priority;
        private readonly Action _onStart;

        private volatile int _flowControlCredit;
        private TaskCompletionSource<object> _flowCreditAvailable;
        private bool _disposed;

        public OutputStream(int streamId, Priority priority, WriteQueue writeQueue)
            : this(streamId, priority, writeQueue, Constants.DefaultFlowControlCredit, () => { })
        {
        }

        public OutputStream(int streamId, Priority priority, WriteQueue writeQueue, int flowControlCredit)
            : this(streamId, priority, writeQueue, flowControlCredit, () => { })
        {
        }

        public OutputStream(int streamId, Priority priority, WriteQueue writeQueue, int flowControlCredit, Action onStart)
        {
            _streamId = streamId;
            _writeQueue = writeQueue;
            _onStart = onStart;
            _priority = priority;
            _flowControlCredit = Constants.DefaultFlowControlCredit; // TODO: Configurable via persisted settings
            _flowCreditAvailable = new TaskCompletionSource<object>();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            _onStart();
            _writeQueue.FlushAsync(_priority).Wait();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            _onStart();
            // TODO: await a backpressure task until we get a window update.
            return _writeQueue.FlushAsync(_priority);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).Wait();
        }

        // Does not support overlapped writes due to flow control backpressure implementation.
        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            _onStart();
            int written = 0;
            do
            {
                if (_flowControlCredit <= 0)
                {
                    // await a backpressure task until we get a window update.
                    await WaitForFlowCreditAsync(cancellationToken);
                    CheckDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                int subCount = Math.Min(Math.Min(_flowControlCredit, count - written), Constants.MaxDataFrameContentSize);
                DataFrame dataFrame = new DataFrame(_streamId, new ArraySegment<byte>(buffer, offset, subCount), true);
                written += subCount;
                offset += subCount;
                _flowControlCredit -= subCount;

                await _writeQueue.WriteFrameAsync(dataFrame, _priority);
            }
            while (written < count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // TODO:
            // throw new NotImplementedException();
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            // TODO:
            // throw new NotImplementedException();
            base.EndWrite(asyncResult);
        }

        private Task WaitForFlowCreditAsync(CancellationToken cancellationToken)
        {
            // TODO: Cancel _flowCreditAvailable if cancellationToken becomes cancelled.
            cancellationToken.ThrowIfCancellationRequested();

            _flowCreditAvailable = new TaskCompletionSource<object>();

            if (_disposed)
            {
                _flowCreditAvailable.TrySetCanceled();
            }
            else if (_flowControlCredit > 0)
            {
                _flowCreditAvailable.TrySetResult(null);
            }

            return _flowCreditAvailable.Task;
        }

        // Credit can go negative if the settings change and data has already been sent.
        public void AddFlowControlCredit(int delta)
        {
            _flowControlCredit += delta;
            if (_disposed)
            {
                _flowCreditAvailable.TrySetCanceled();
            }
            else if (_flowControlCredit > 0)
            {
                _flowCreditAvailable.TrySetResult(null);
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(OutputStream).FullName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            if (disposing)
            {
                _flowCreditAvailable.TrySetCanceled();
            }

            base.Dispose(disposing);
        }
    }
}
