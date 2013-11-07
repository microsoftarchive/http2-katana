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
        private readonly StreamBuffer _writeBuffer;
        private readonly StreamBuffer _readBuffer;
        private SecureSocket _socket;
        private bool _isClosed;

        //If stream owns socket then it will close this socket when someone will close stream.
        //if stream doesnt own socket then socket will not be closed.
        private readonly bool _ownsSocket;
        private readonly object _waitLock;
        private readonly object _closeLock;
        private ManualResetEvent _streamStateChangeRaised;
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
            if (socket == null)
                throw new ArgumentNullException("socket is null");

            _writeBuffer = new StreamBuffer(1024);
            _readBuffer = new StreamBuffer(1024);
            _ownsSocket = ownsSocket;
            _socket = socket;
            _isClosed = false;
            _waitLock = new object();
            _closeLock = new object();
            _streamStateChangeRaised = new ManualResetEvent(false);

            OnDataAvailable += (sender, args) => _streamStateChangeRaised.Set();

            OnClose += (sender, args) => _streamStateChangeRaised.Set();

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
                catch (Exception)
                {
                    Http2Logger.LogInfo("Connection was lost. Closing io stream");

                    Close();
                    break;
                }

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

            Http2Logger.LogDebug("Listen thread finished");
        }

        /// <summary>
        /// Method receives bytes from socket until match predicate returns false.
        /// Usable for receiving headers. Header block finishes with \r\n\r\n
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        private bool WaitForDataAvailable(int timeout)
        {
            _streamStateChangeRaised.Reset();
            lock (_waitLock)
            {
                if (Available != 0)
                {
                    return true;
                }

                bool wasDataReceived = _streamStateChangeRaised.WaitOne(timeout);
                Thread.Sleep(5);
                bool result = wasDataReceived && Available != 0;

                return result;
            }
        }

        public override void Flush()
        {
            if (_isClosed)
                return;

            if (_writeBuffer.Available == 0)
                return;

            var flushBuffer = new byte[_writeBuffer.Available];

            int read = _writeBuffer.Read(flushBuffer, 0, flushBuffer.Length);
            
            if (read == 0)
                return;

            _socket.Send(flushBuffer, 0, flushBuffer.Length, SocketFlags.None);
        }

        public async override Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Flush();
                    }
                    catch (Exception ex)
                    {
                        Http2Logger.LogDebug("FlushAsync failed :" + ex.Message);
                    }
                });
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
            if (buffer == null)
                throw new ArgumentNullException("buffer is null");

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("offset or count < 0");

            if (_isClosed)
                return 0;

            if (!WaitForDataAvailable(ReadTimeout))
            {
                //We've waited enough and there was no data. Close connection due timeout
                Close();
                return 0;
            }

            return _readBuffer.Read(buffer, offset, count);
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer is null");

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("offset or count < 0");

            //Refactor. Do not use lambda
            return await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return Read(buffer, offset, count);
                    }
                    catch (Exception ex)
                    {
                        Http2Logger.LogDebug("DuplexStream.ReadAsync got exception: " + ex.Message);
                    }

                    return 0;
                });
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer is null");

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("offset or count < 0");

            if (_isClosed)
                return;

            _writeBuffer.Write(buffer, offset, count);
        }

        // TODO to extension methods ?? + check for args
        public int Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer is null");

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
            if (buffer == null)
                throw new ArgumentNullException("buffer is null");

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("offset or count < 0");

            //Refactor. Do not use lambda
            await Task.Factory.StartNew(() =>                 
                {
                    try
                    {
                        Write(buffer, offset, count);
                    }
                    catch (Exception ex)
                    {
                        Http2Logger.LogDebug("DuplexStream.WriteAsync got exception: " + ex.Message);
                    }
                });
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
            lock (_closeLock)
            {
                //Return instead of throwing exception because external code calls Close and 
                //it knows nothing about defined exception.
                if (_isClosed)
                    return;

                Http2Logger.LogDebug("Closing duplex stream");

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

                if (_streamStateChangeRaised != null)
                {
                    _streamStateChangeRaised.Dispose();
                    _streamStateChangeRaised = null;
                }
            }
        }
    }
}
