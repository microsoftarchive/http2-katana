using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    // This class manages flow control on the incoming data stream.
    public class InputStream : QueueStream
    {
        private int _windowSize;
        private Action<int> _sendWindowUpdate;

        public InputStream(int initialWindowSize, Action<int> sendWindowUpdate)
        {
            _windowSize = initialWindowSize;
            _sendWindowUpdate = sendWindowUpdate;
        }

        private void IncreaseWindowSize(int delta)
        {
            _windowSize += delta;
            if (_windowSize > 0)
            {
                // TODO: This is where any smart flow control could hook in.
                // E.g. should we really send a flow control update if the app only read one byte?
                // Should we scale the window based on round trip time? Bandwidth? Rate of app drainage?
                _sendWindowUpdate(delta);
            }
        }

        private void DecreaseWindowSize(int delta)
        {
            _windowSize -= delta;
            if (_windowSize < 0)
            {
                // TODO: throw new Stream Reset exception, status code: flow control error
            }
        }

        public override int ReadByte()
        {
            int data = base.ReadByte();
            IncreaseWindowSize(1);
            return data;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            IncreaseWindowSize(read);
            return read;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            int read = base.EndRead(asyncResult);
            IncreaseWindowSize(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await base.ReadAsync(buffer, offset, count, cancellationToken);
            IncreaseWindowSize(read);
            return read;
        }

        public override void WriteByte(byte value)
        {
            DecreaseWindowSize(1);
            base.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            DecreaseWindowSize(count);
            base.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            DecreaseWindowSize(count);
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            DecreaseWindowSize(count);
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
