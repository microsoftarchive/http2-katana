using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.EventArgs;

namespace SharedProtocol.IO
{
    public class DuplexStream : Stream
    {
        private StreamBuffer _writeBuffer;
        private StreamBuffer _readBuffer;
        private SecureSocket _socket;
        private bool _isClosed;
        private readonly bool _ownsSocket;
        private readonly object _closeLock;

        public SecureSocket Socket { get { return _socket; } }

        public DuplexStream(SecureSocket socket, bool ownsSocket = false)
        {
            _writeBuffer = new StreamBuffer(1024);
            _readBuffer = new StreamBuffer(1024);
            _ownsSocket = ownsSocket;
            _socket = socket;
            _isClosed = false;
            _closeLock = new object();

            Task.Run(() => PumpIncomingData());
        }

        private async Task PumpIncomingData()
        {
            while (!_isClosed)
            {
                var tmpBuffer = new byte[1024];
                int received = await Task.Factory.FromAsync<int>(_socket.BeginReceive(tmpBuffer, 0, tmpBuffer.Length, SocketFlags.None, null, null),
                        _socket.EndReceive, TaskCreationOptions.None, TaskScheduler.Default);

                //TODO Connection was lost
                if (received == 0)
                {
                    Close();
                    break;
                }

                _readBuffer.Write(tmpBuffer, 0, received);

                //Signal data available and it can be read
                if (OnDataAvailable != null)
                    OnDataAvailable(this, new DataAvailableEventArgs(tmpBuffer));
            }
        }

        //Method receives bytes from socket until match predicate returns false.
        //Usable for receiving headers. Header block finishes with \r\n\r\n
        public bool WaitForDataAvailable(int timeout, Predicate<byte[]> match = null)
        {
            if (Available != 0)
            {
                return true;
            }
            
            bool result;

            using (var wait = new ManualResetEvent(false))
            {
                EventHandler<DataAvailableEventArgs> dataReceivedHandler = delegate (object sender, DataAvailableEventArgs args)
                {
                    var receivedBuffer = args.ReceivedBytes;
                    if (match != null && match.Invoke(receivedBuffer))
                    {
                        wait.Set();
                    }
                    else if (match == null)
                    {
                        wait.Set();
                    }
                };

                //TODO think about if wait was already disposed
                OnDataAvailable += dataReceivedHandler;

                result = wait.WaitOne(timeout);

                OnDataAvailable -= dataReceivedHandler;
            }
            return result;
        }

        public override void Flush()
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            var bufferLen = _writeBuffer.BufferedDataSize;
            var flushBuffer = new byte[bufferLen];
            _writeBuffer.Read(flushBuffer, 0, bufferLen);

            _socket.Send(flushBuffer, 0, flushBuffer.Length, SocketFlags.None);
        }

        public async override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            var bufferLen = _writeBuffer.BufferedDataSize;
            var flushBuffer = new byte[bufferLen];
            _writeBuffer.Read(flushBuffer, 0, bufferLen);

            await Task.Factory.FromAsync<int>(_socket.BeginSend(flushBuffer, 0, flushBuffer.Length, SocketFlags.None, null, null),
                                                _socket.EndSend, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            return _readBuffer.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            _writeBuffer.Write(buffer, offset, count);

        }

        // TODO to extension methods ?? + check for args
        public int Write(byte[] buffer)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            _writeBuffer.Write(buffer, 0, buffer.Length);

            return buffer.Length;
        }

        public override void WriteByte(byte value)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            Write(new [] {value}, 0, 1);
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();
            
            //Refactor. Do not use lambda
            await Task.Factory.StartNew(() => _writeBuffer.Write(buffer, offset, count));
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isClosed)
                throw new ObjectDisposedException("Duplex stream was already closed");

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            //Refactor. Do not use lambda
            return await Task.Factory.StartNew(() => _readBuffer.Read(buffer, offset, count));
        }

        public int Available { get { return _readBuffer.Available; } }

        public override bool CanRead
        {
            get { return true; }
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

        private event EventHandler<DataAvailableEventArgs> OnDataAvailable; 

        public override void Close()
        {
            lock (_closeLock)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("Trying to close stream twice");

                _isClosed = true;

                if (_ownsSocket && _socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }

                base.Close();
            }
        }
    }
}
