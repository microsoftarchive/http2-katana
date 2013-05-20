using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    // This class buffers data written into it so it can be read out at the other end.
    // Useful for emulating a socket type transport.
    public class QueueStream : Stream
    {
        private bool _disposed;
        private bool _aborted;
        private string _abortMessage;
        private ConcurrentQueue<byte[]> _bufferedData;
        private ArraySegment<byte> _topBuffer;
        private SemaphoreSlim _readLock;
        private SemaphoreSlim _writeLock;
        private TaskCompletionSource<object> _readWaitingForData;

        public QueueStream()
        {
            _readLock = new SemaphoreSlim(1, 1);
            _writeLock = new SemaphoreSlim(1, 1);
            _bufferedData = new ConcurrentQueue<byte[]>();
            _readWaitingForData = new TaskCompletionSource<object>();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            VerifyBuffer(buffer, offset, count, allowEmpty: false);
            _readLock.Wait();
            try
            {
                int totalRead = 0;
                do
                {
                    if (_topBuffer.Count <= 0)
                    {
                        byte[] topBuffer = null;
                        while (!_bufferedData.TryDequeue(out topBuffer))
                        {
                            // Let buffered data get drained before signaling an abort.
                            CheckAborted();
                            if (_disposed) return 0;
                            WaitForDataAsync().Wait();
                        }
                        _topBuffer = new ArraySegment<byte>(topBuffer);
                    }
                    int actualCount = Math.Min(count, _topBuffer.Count);
                    Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                    _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                        _topBuffer.Offset + actualCount,
                        _topBuffer.Count - actualCount);
                    totalRead += actualCount;
                    offset += actualCount;
                    count -= actualCount;
                }
                while (count > 0 && (_topBuffer.Count > 0  || _bufferedData.Count > 0));
                // Keep reading while there is more data available and we have more space to put it in.
                return totalRead;
            }
            finally
            {
                _readLock.Release();
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // TODO: This option doesn't preserve the state object.
            // return ReadAsync(buffer, offset, count);
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            // return ((Task<int>)asyncResult).Result;
            return base.EndRead(asyncResult);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VerifyBuffer(buffer, offset, count, allowEmpty: false);
            await _readLock.WaitAsync();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                int totalRead = 0;
                do
                {
                    if (_topBuffer.Count <= 0)
                    {
                        byte[] topBuffer = null;
                        while (!_bufferedData.TryDequeue(out topBuffer))
                        {
                            // Let buffered data get drained before signaling an abort.
                            CheckAborted();
                            if (_disposed) return 0;
                            await WaitForDataAsync();
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        _topBuffer = new ArraySegment<byte>(topBuffer);
                    }
                    int actualCount = Math.Min(count, _topBuffer.Count);
                    Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                    _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                        _topBuffer.Offset + actualCount,
                        _topBuffer.Count - actualCount);
                    totalRead += actualCount;
                    offset += actualCount;
                    count -= actualCount;
                }
                while (count > 0 && (_topBuffer.Count > 0 || _bufferedData.Count > 0));
                // Keep reading while there is more data available and we have more space to put it in.
                return totalRead;
            }
            finally
            {
                _readLock.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            VerifyBuffer(buffer, offset, count, allowEmpty: true);
            if (count == 0)
            {
                return;
            }
            _writeLock.Wait();
            try
            {
                // Copies are necessary because we don't know what the caller is going to do with the buffer afterwards.
                byte[] internalBuffer = new byte[count];
                Buffer.BlockCopy(buffer, offset, internalBuffer, 0, count);
                _bufferedData.Enqueue(internalBuffer);

                SignalDataAvailable();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Write(buffer, offset, count);
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(state);
            tcs.TrySetResult(null);
            IAsyncResult result = tcs.Task;
            if (callback != null)
            {
                callback(result);
            }
            return result;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CancelledTask();
            }

            Write(buffer, offset, count);
            return Task.FromResult<object>(null);
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CancelledTask();
            }
            return Task.FromResult<object>(null);
        }

        private Task CancelledTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();
            return tcs.Task;
        }

        private void VerifyBuffer(byte[] buffer, int offset, int count, bool allowEmpty)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
            }
            if (count < 0 || count > buffer.Length - offset
                || (!allowEmpty && count == 0))
            {
                throw new ArgumentOutOfRangeException("count", count, string.Empty);
            }
        }

        private void SignalDataAvailable()
        {
            // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
            Task.Run(() => _readWaitingForData.TrySetResult(null));
        }

        private Task WaitForDataAsync()
        {
            _readWaitingForData = new TaskCompletionSource<object>();

            if (!_bufferedData.IsEmpty || _disposed)
            {
                // Race, data could have arrived before we created the TCS.
                _readWaitingForData.TrySetResult(null);
            }

            return _readWaitingForData.Task;
        }

        public void Abort(string message)
        {
            _aborted = true;
            _abortMessage = message;
            Dispose();
        }

        private void CheckAborted()
        {
            if (_aborted)
            {
                throw new IOException(_abortMessage);
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            _readWaitingForData.TrySetResult(null);
            base.Dispose(disposing);
        }
    }
}
