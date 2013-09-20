using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol.IO
{
    /// <summary>
    /// This class is based on SecureSocket and represents input/output stream.
    /// </summary>
    public class DuplexStream : Stream
    {
        private StreamBuffer _writeBuffer;
        private StreamBuffer _readBuffer;
        private SecureSocket _socket;
        private bool _isClosed;
        private readonly bool _ownsSocket;
        private readonly object _locker;

        public override int ReadTimeout
        {
            get { return 600000; } // if local ep will get nothing from the remote ep in 10 minutes it will close connection
        }

        public bool IsSecure 
        { 
            get
            {
                return _socket.SecureProtocol == SecureProtocol.Ssl3
                       || _socket.SecureProtocol == SecureProtocol.Tls1;
            }
        }

        public DuplexStream(SecureSocket socket, bool ownsSocket = false)
        {
            _writeBuffer = new StreamBuffer(1024);
            _readBuffer = new StreamBuffer(1024);
            _ownsSocket = ownsSocket;
            _socket = socket;
            _isClosed = false;
            _locker = new object();

            Task.Run(async () => 
                {
                    Thread.CurrentThread.Name = "Duplex listening thread";
                    await PumpIncomingData();
                });
        }

        /// <summary>
        /// Pumps the incoming data into read buffer and signal that data for reading is available then.
        /// </summary>
        /// <returns></returns>
        private async Task PumpIncomingData()
        {
            while (!_isClosed)
            {
                var tmpBuffer = new byte[1024];
                int received = 0;
                try
                {
                    received = await Task.Factory.FromAsync<int>(_socket.BeginReceive(tmpBuffer, 0, tmpBuffer.Length, SocketFlags.None, null, null),
                            _socket.EndReceive, TaskCreationOptions.None, TaskScheduler.Default);
                }
                catch (Org.Mentalis.Security.SecurityException)
                {
                    Http2Logger.LogInfo("Connection was closed by the remote endpoint");
                }
                catch (Exception ex)
                {
                    Http2Logger.LogInfo("Connection was lost. Closing io stream");

                    Close();
                    return;
                }
                //TODO Connection was lost
                if (received == 0)
                {
                    Close();
                    return;
                }

                _readBuffer.Write(tmpBuffer, 0, received);

                // TODO SG - we should pass num received or new buffer since tmpBuffer could be filled  partially
                //Signal data available and it can be read
                lock (_locker)
                {
                    // lock is required since another thread can be updating OnDataAvailable
                    if (OnDataAvailable != null)
                    {
                        try
                        {
                            OnDataAvailable(this, new DataAvailableEventArgs(tmpBuffer));
                        }
                        catch (NullReferenceException ex)
                        {

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method receives bytes from socket until match predicate returns false.
        /// Usable for receiving headers. Header block finishes with \r\n\r\n
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private bool WaitForDataAvailable(int timeout, Predicate<byte[]> match = null)
        {
            bool result = false;

            if (Available != 0)
            {
                return true;
            }

            using (var wait = new ManualResetEvent(false))
            {
                EventHandler<DataAvailableEventArgs> dataReceivedHandler = delegate(object sender, DataAvailableEventArgs args)
                {
                    lock (_locker)
                    {
                        var receivedBuffer = args.ReceivedBytes;
                        if (match == null || match.Invoke(receivedBuffer))
                        {
                            wait.Set();
                        }
                    }
                };

                EventHandler<System.EventArgs> closeHandler = (s, arg) => wait.Set();

                //TODO think about if wait was already disposed

                lock (_locker) // lock is required since several threads access/change OnDataAvailable/OnClose events
                {
                    // check if the data has become available while we were creating handlers and sync event
                    // this could happen since data is pumped by separate thread
                    if (Available != 0)
                    {
                        return true;
                    }

                    OnDataAvailable += dataReceivedHandler;
                    OnClose += closeHandler;
                }


                result = wait.WaitOne(timeout) && Available != 0;

                lock (_locker)
                {
                    OnDataAvailable -= dataReceivedHandler;
                    OnClose -= closeHandler;
                }
            }
            return result;
        }

        public override void Flush()
        {
            if (_isClosed)
                return;

            if (_writeBuffer.Available == 0)
                return;

            var bufferLen = _writeBuffer.Available;
            var flushBuffer = new byte[bufferLen];

            int read = _writeBuffer.Read(flushBuffer, 0, bufferLen);
            
            if (read == 0)
                return;

            if (read != bufferLen)
            {
                int a = 1;
            }

            _socket.Send(flushBuffer, 0, flushBuffer.Length, SocketFlags.None);
        }

        public async override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_isClosed)
                return;

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            if (_writeBuffer.Available == 0)
                return;

            var bufferLen = _writeBuffer.Available;
            var flushBuffer = new byte[bufferLen];

            int read = _writeBuffer.Read(flushBuffer, 0, bufferLen);

            if (read == 0)
                return;

            if (read != bufferLen)
            {
                int a = 1;
            }

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
                return 0;

            if (!WaitForDataAvailable(ReadTimeout))
            {
                // TODO consider throwing appropriate timeout exception
                return 0;
            }

            return _readBuffer.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isClosed)
                return;

            _writeBuffer.Write(buffer, offset, count);
        }

        // TODO to extension methods ?? + check for args
        public int Write(byte[] buffer)
        {
            if (_isClosed)
                return 0;

            _writeBuffer.Write(buffer, 0, buffer.Length);

            return buffer.Length;
        }

        public override void WriteByte(byte value)
        {
            if (_isClosed)
                return;

            Write(new [] {value}, 0, 1);
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isClosed)
                return;

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();
            
            //Refactor. Do not use lambda
            await Task.Factory.StartNew(() => _writeBuffer.Write(buffer, offset, count));
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isClosed)
                return 0;

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            if (!WaitForDataAvailable(ReadTimeout))
            {
                return 0;
            }

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

        public event EventHandler<System.EventArgs> OnClose;

        public override void Close()
        {
            lock (_locker)
            {
                //Return instead of throwing exception because external code calls Close and 
                //it knows nothing about defined exception.
                if (_isClosed)
                    return;

                _isClosed = true;

                if (_ownsSocket && _socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }

                base.Close();

                if (OnClose != null)
                    OnClose(this, null);

                OnClose = null;
            }
        }
    }
}
