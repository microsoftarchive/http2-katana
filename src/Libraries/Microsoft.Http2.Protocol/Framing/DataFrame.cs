// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// DATA frame class
    /// see 13 -> 6.1
    /// </summary>
    public class DataFrame : Frame, IEndStreamFrame, IEndSegmentFrame, IPaddingFrame
    {
        // 1 byte PadLength field
        private const int PadLengthSize = 1;

        // for incoming
        public DataFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public DataFrame(int streamId, ArraySegment<byte> data, bool isEndStream, bool hasPadding)
        {
            Contract.Assert(data.Array != null);

            /* 13 -> 6.1
            DATA frames MAY also contain arbitrary padding. Padding can be added
            to DATA frames to obscure the size of messages. */

            if (hasPadding)
            {
                // generate padding
                int padLength = new Random().Next(1, 7);

                // construct frame with padding
                Buffer = new byte[Constants.FramePreambleSize + PadLengthSize + data.Count + padLength];
                HasPadding = true;
                PadLength = (byte) padLength;
                PayloadLength = PadLengthSize + data.Count + padLength;

                System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Constants.FramePreambleSize + PadLengthSize, data.Count);
            }
            else
            {
                // construct frame without padding
                Buffer = new byte[Constants.FramePreambleSize + data.Count];
                PayloadLength = data.Count;               

                System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Constants.FramePreambleSize, data.Count);
            }
           
            IsEndStream = isEndStream;
            FrameType = FrameType.Data;
            StreamId = streamId;
        }

        public bool IsEndStream
        {
            get
            {
                return (Flags & FrameFlags.EndStream) == FrameFlags.EndStream;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndStream;
                }
            }
        }

        public bool IsEndSegment
        {
            get
            {
                return (Flags & FrameFlags.EndSegment) == FrameFlags.EndSegment;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndSegment;
                }
            }
        }

        public bool HasPadding
        {
            get
            {
                return (Flags & FrameFlags.Padded) == FrameFlags.Padded;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Padded;
                }
            }
        }

        public byte PadLength
        {
            get
            {
                return HasPadding ? Buffer[Constants.FramePreambleSize] : (byte) 0;
            }
            set { Buffer[Constants.FramePreambleSize] = value; }
        }    

        public ArraySegment<byte> Data
        {
            get
            {
                if (HasPadding)
                {
                    return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize + PadLengthSize,
                        Buffer.Length - Constants.FramePreambleSize - PadLengthSize - PadLength);
                }
                return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize,
                        Buffer.Length - Constants.FramePreambleSize);
            }
        }
    }
}
