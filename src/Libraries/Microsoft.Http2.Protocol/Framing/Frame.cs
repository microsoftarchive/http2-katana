// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol.Framing
{
    //0                   1                   2                   3
    //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //|         Length (16)           |   Type (8)    |   Flags (8)   |
    //+-+-------------+---------------+-------------------------------+
    //|R|                 Stream Identifier (31)                      |
    //+-+-------------------------------------------------------------+
    //|                     Frame Payload (0...)                    ...
    //+---------------------------------------------------------------+
    /// <summary>
    /// Represents the initial frame fields on every frame.
    /// </summary>
    public class Frame
    {
        protected byte[] _buffer;

        // For reading the preamble to determine the frame type and length
        public Frame()
            : this(new byte[Constants.FramePreambleSize])
        {
        }

        // For incoming frames
        protected Frame(Frame preamble)
            : this(new byte[Constants.FramePreambleSize + preamble.PayloadLength])
        {
            System.Buffer.BlockCopy(preamble.Buffer, 0, Buffer, 0, Constants.FramePreambleSize);
        }

        // For outgoing frames
        protected Frame(byte[] buffer)
        {
            _buffer = buffer;
        }

        public byte[] Buffer
        {
            get { return _buffer; }
            set { _buffer = value; }
        }

        public ArraySegment<byte> Payload
        {
            get
            {
                if (_buffer != null && _buffer.Length > 0)
                {
                    return new ArraySegment<byte>(_buffer, Constants.FramePreambleSize, _buffer.Length - Constants.FramePreambleSize);  
                }
                return new ArraySegment<byte>();
            }
        }

        public bool IsControl
        {
            get { return FrameType != FrameType.Data; }
        }

        // 16 bits, 0-15
        /* 12 -> 4.1.
        The length of the frame payload. The 8 octets of the frame header
        are not included in this value */
        public int PayloadLength
        {
            get
            {
                return FrameHelper.Get16BitsAt(Buffer, 0);
            }
            set
            {
                FrameHelper.Set16BitsAt(Buffer, 0, value);
            }
        }

        // 8 bits, 16-23
        public FrameType FrameType
        {
            get
            {
                return (FrameType) Buffer[2];
            }
            set 
            { 
                Buffer[2] = (byte)value;
            }
        }

        // 8 bits, 24-31
        public FrameFlags Flags
        {
            get
            {
                return (FrameFlags) Buffer[3];
            }
            set
            {
                Buffer[3] = (byte) value;
            }
        }

        // 31 bits, 33-63
        public Int32 StreamId
        {
            get
            {
                return FrameHelper.Get31BitsAt(Buffer, 4);
            }
            set
            {
                FrameHelper.Set31BitsAt(Buffer, 4, value);
            }
        }
    }
}
