using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    // This takes two QueueStreams and positions them side by side to form a duplex channel
    // like a socket.
    public class DuplexStream : Stream
    {
        private Stream _incoming;
        private Stream _outgoing;

        public DuplexStream()
            : this (new QueueStream(), new QueueStream())
        {
        }

        public DuplexStream(Stream incoming, Stream outgoing)
        {
            _incoming = incoming;
            _outgoing = outgoing;
        }

        public override bool CanRead
        {
            get { return _incoming.CanRead; }
        }

        public override bool CanWrite
        {
            get { return _outgoing.CanWrite; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return _incoming.CanTimeout | _outgoing.CanTimeout; }
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

        public override int ReadTimeout
        {
            get { return _incoming.ReadTimeout; }
            set { _incoming.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return _outgoing.WriteTimeout; }
            set { _outgoing.WriteTimeout = value; }
        }

        public DuplexStream GetOpositeStream()
        {
            return new DuplexStream(_outgoing, _incoming);
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
            return _incoming.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _incoming.Read(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _incoming.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _incoming.EndRead(asyncResult);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _incoming.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _outgoing.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _outgoing.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _outgoing.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _outgoing.EndWrite(asyncResult);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _outgoing.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
            _outgoing.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _outgoing.FlushAsync(cancellationToken);
        }

        public override string ToString()
        {
            return _incoming.ToString() + ";" + _outgoing.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            _incoming.Dispose();
            _outgoing.Dispose();
            base.Dispose(disposing);
        }

        public void Abort()
        {
            QueueStream incoming = _incoming as QueueStream;
            if (incoming == null)
            {
                _incoming.Dispose();
            }
            else
            {
                incoming.Abort(new IOException().Message);
            }

            QueueStream outgoing = _outgoing as QueueStream;
            if (outgoing == null)
            {
                _outgoing.Dispose();
            }
            else
            {
                outgoing.Abort(new IOException().Message);
            }
        }
    }
}
