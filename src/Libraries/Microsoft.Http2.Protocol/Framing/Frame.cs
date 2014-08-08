// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// see 14 -> 4.1 Frame Format
    /// </summary>
    public class Frame
    {
        protected byte[] _buffer;

        // for reading the preamble to determine the frame type and length
        public Frame()
            : this(new byte[Constants.FramePreambleSize])
        {
        }

        // for incoming frames
        protected Frame(Frame preamble)
            : this(new byte[Constants.FramePreambleSize + preamble.PayloadLength])
        {
            System.Buffer.BlockCopy(preamble.Buffer, 0, Buffer, 0, Constants.FramePreambleSize);
        }

        // for outgoing frames
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

        /* 14 -> 4.1
        The length of the frame payload expressed as an unsigned
        24-bit integer. */
        public int PayloadLength
        {
            get
            {
                return FrameHelper.Get24BitsAt(Buffer, 0);
            }
            set
            {
                FrameHelper.Set24BitsAt(Buffer, 0, value);
            }
        }

        /* 14 -> 4.1
        The 8-bit type of the frame. The frame type determines the
        format and semantics of the frame. */
        public FrameType FrameType
        {
            get
            {
                return (FrameType) Buffer[3];
            }
            set 
            { 
                Buffer[3] = (byte)value;
            }
        }

        /* 14 -> 4.1
        An 8-bit field reserved for frame-type specific boolean flags. */
        public FrameFlags Flags
        {
            get
            {
                return (FrameFlags) Buffer[4];
            }
            set
            {
                Buffer[4] = (byte) value;
            }
        }

        /* 14 -> 4.1
        A 31-bit stream identifier. */
        public Int32 StreamId
        {
            get
            {
                return FrameHelper.Get31BitsAt(Buffer, 5);
            }
            set
            {
                FrameHelper.Set31BitsAt(Buffer, 5, value);
            }
        }
    }
}
