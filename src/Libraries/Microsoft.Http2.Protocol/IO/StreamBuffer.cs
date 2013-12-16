// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.IO
{
    /// <summary>
    /// This struct represents buffer on which Stream can be built
    /// </summary>
    internal class StreamBuffer
    {
        private byte[] _buffer;
        private int _position;
        private readonly object _readWriteLock;

        internal byte[] Buffer { get { return _buffer; } }
        internal int Available { get { return _position; } }

        internal StreamBuffer(int initialSize)
        {
            _buffer = new byte[initialSize];
            _position = 0;
            _readWriteLock = new object();
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

        private void MakeLeftShiftBy(int length)
        {
            if (length > _buffer.Length)
                length = _buffer.Length;

            byte[] result;
            
            if (length < _position)
            {
                result = new byte[_position - length];
                System.Buffer.BlockCopy(_buffer, length, result, 0, _position - length);
            }
            else
            {
                result = new byte[_buffer.Length];
            }

            _buffer = result;
            _position -= length;
        }

        internal int Read(byte[] buffer, int offset, int count)
        {
            lock (_readWriteLock)
            {
                Contract.Assert(offset + count <= buffer.Length);
                if (Available == 0)
                    return 0;

                if (count > _position)
                    count = _position;

                System.Buffer.BlockCopy(_buffer, 0, buffer, offset, count);

                MakeLeftShiftBy(count);
            }
            return count;
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            lock (_readWriteLock)
            {
                Contract.Assert(offset + count <= buffer.Length);
                if (_position + count > _buffer.Length)
                    ReallocateMemory(_position + count);

                System.Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                _position += count;
            }
        }
    }
}
