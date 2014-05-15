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
    /// see 12 -> 6.1.
    /// </summary>
    public class DataFrame : Frame, IEndStreamFrame, IEndSegmentFrame, IPaddingFrame
    {
        // 1 byte Pad High, 1 byte Pad Low field
        private const int PadHighLowLength = 2;

        // for incoming
        public DataFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public DataFrame(int streamId, ArraySegment<byte> data, bool isEndStream, byte padHigh = 0, byte padLow = 0)
        {
            Contract.Assert(data.Array != null);

            /* 12 -> 6.1
            DATA frames MAY also contain arbitrary padding.  Padding can be added
            to DATA frames to hide the size of messages. The total number of padding
            octets is determined by multiplying the value of the Pad High field by 256 
            and adding the value of the Pad Low field. */
            
            int padLength = padHigh * 256 + padLow;
            if (padLength != 0)
            {
                // construct frame with padding
                Buffer = new byte[Constants.FramePreambleSize + PadHighLowLength + data.Count + padLength];
                HasPadHigh = true;
                HasPadLow = true;
                PadHigh = padHigh;
                PadLow = padLow;
                PayloadLength = PadHighLowLength + data.Count + padLength;

                System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Constants.FramePreambleSize + PadHighLowLength, data.Count);
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

            //TODO: add optional gzip compression
            /* 12 -> 6.1
            Data frames are optionally compressed using GZip compression.
            Each frame is individually compressed; the state of the compressor is
            reset for each frame.*/
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

        public bool HasPadLow
        {
            get
            {
                return (Flags & FrameFlags.PadLow) == FrameFlags.PadLow;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.PadLow;
                }
            }
        }

        public bool HasPadHigh
        {
            get
            {
                return (Flags & FrameFlags.PadHight) == FrameFlags.PadHight;
            } 
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.PadHight;
                }
            }
        }

        public bool HasPadding
        {
            get { return HasPadHigh && HasPadLow; }
        }

        public byte PadHigh
        {
            get
            {
                return HasPadding ? Buffer[Constants.FramePreambleSize] : (byte) 0;
            }
            set { Buffer[Constants.FramePreambleSize] = value; }
        }

        public byte PadLow
        {
            get
            {
                return HasPadding ? Buffer[Constants.FramePreambleSize + 1] : (byte) 0;
            }
            set { Buffer[Constants.FramePreambleSize + 1] = value; }
        }

        public bool IsCompressed
        {
            get
            {
                return (Flags & FrameFlags.Compressed) == FrameFlags.Compressed;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Compressed;
                }
            }
        }      

        public ArraySegment<byte> Data
        {
            get
            {
                if (HasPadding)
                {
                    int padLength = PadHigh * 256 + PadLow;

                    return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize + PadHighLowLength,
                        Buffer.Length - Constants.FramePreambleSize - PadHighLowLength - padLength);
                }
                return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize,
                        Buffer.Length - Constants.FramePreambleSize);
            }
        }
    }
}
