using System.Diagnostics.Contracts;

namespace SharedProtocol.IO
{
    internal struct StreamBuffer
    {
        private byte[] _buffer;
        private int _position;
        private readonly object _writeLock;
        private readonly object _readLock;

        internal byte[] Buffer { get { return _buffer; } }
        internal bool Available { get { return _position != 0; } }

        public int BufferedDataSize {get { return _position; } }

        internal StreamBuffer(int initialSize)
        {
            _buffer = new byte[initialSize];
            _position = 0;
            _writeLock = new object();
            _readLock = new object();
        }

        private void ReallocateMemory(long length)
        {
            var temp = new byte[length];

            //This means that buffer can only grow. 
            //It's not clear what to do if we will try to reduce buffer size.
            //I think that data should be read and buffer size should be reduced then.
            //TODO buffer size reduce case
            if (length > _buffer.Length)
            {
                System.Buffer.BlockCopy(_buffer, 0, temp, 0, _position);
            }
            
            _buffer = temp;
        }

        private void CyclicShiftLeft(int length)
        {
            if (length > _buffer.Length)
                length = _buffer.Length;

            var result = new byte[_buffer.Length];

            if (length >= _position)
            {
                _buffer = new byte[_buffer.Length];
            }
            else
            {
                System.Buffer.BlockCopy(_buffer, length, result, 0, (int)_position - (length - 1));
            }

            _buffer = result;
            _position -= length - 1;
        }

        internal int Read(byte[] buffer, int offset, int count)
        {
            Contract.Assert(offset + count <= buffer.Length);
            if (!Available)
                return 0;

            lock (_readLock)
            {
                if (count > _position)
                    count = _position;

                System.Buffer.BlockCopy(_buffer, 0, buffer, offset, count);

                CyclicShiftLeft(count);
            }
            return count;
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            Contract.Assert(offset + count <= buffer.Length);

            lock (_writeLock)
            {
                if (_position + count > _buffer.Length)
                    ReallocateMemory(_position + count);

                System.Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                _position += count;
            }
        }
    }
}
