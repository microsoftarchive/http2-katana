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
    /// HEADERS frame class
    /// see 12 -> 6.2
    /// </summary>
    public class HeadersFrame : Frame, IEndStreamFrame, IEndSegmentFrame, IHeadersFrame, IPaddingFrame
    {
        // 1 byte Pad High, 1 byte Pad Low field
        private const int PadHighLowLength = 2;

        // 4 bytes Stream Dependency field
        private const int DependencyLength = 4;

        // 1 byte Weight field
        private const int WeightLength = 1;

        private HeadersList _headers = new HeadersList();

        // for incoming
        public HeadersFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public HeadersFrame(int streamId, int streamDependency = -1, byte weight = 0, bool exclusive = false, 
            byte padHigh = 0, byte padLow = 0)
        {
            /* 12 -> 5.3 
            A client can assign a priority for a new stream by including
            prioritization information in the HEADERS frame */
            bool hasPriority = (streamDependency != -1 && weight != 0);

            /* 12 -> 6.2 
            The HEADERS frame includes optional padding.  Padding fields and
            flags are identical to those defined for DATA frames. */
            int padLength = padHigh * 256 + padLow;

            int preambleLength = Constants.FramePreambleSize;
            if (padLength != 0) preambleLength += PadHighLowLength;
            if (hasPriority) preambleLength += DependencyLength + WeightLength;

            // construct frame without Headers Block and Padding bytes
            Buffer = new byte[preambleLength];

            if (padLength != 0)
            {
                PadHigh = padHigh;
                PadLow = padLow;
                HasPadHigh = true;
                HasPadLow = true;
            }

            if (hasPriority)
            {
                HasPriority = true; 
                Exclusive = exclusive;
                StreamDependency = streamDependency;
                Weight = weight;
            }

            PayloadLength = Buffer.Length - Constants.FramePreambleSize;
            FrameType = FrameType.Headers;
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

        public bool IsEndHeaders
        {
            get
            {
                return (Flags & FrameFlags.EndHeaders) == FrameFlags.EndHeaders;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndHeaders;
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
                return HasPadding ? Buffer[Constants.FramePreambleSize + 1] : (byte)0;
            }
            set { Buffer[Constants.FramePreambleSize + 1] = value; }
        }

        public bool HasPriority
        {
            get
            {
                return (Flags & FrameFlags.Priority) == FrameFlags.Priority;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Priority;
                }
            }
        }

        public bool Exclusive
        {
            get
            {
                if (HasPriority)
                {
                    if (HasPadding)
                    {
                        return FrameHelper.GetBit(Buffer[Constants.FramePreambleSize + PadHighLowLength], 7);
                    }
                    return FrameHelper.GetBit(Buffer[Constants.FramePreambleSize], 7);
                }
                return false;
            }
            set
            {
                if (HasPadding)
                {
                    Buffer[Constants.FramePreambleSize + PadHighLowLength] = 
                        FrameHelper.SetBit(Buffer[Constants.FramePreambleSize + PadHighLowLength], value, 7);
                }
                Buffer[Constants.FramePreambleSize] = 
                    FrameHelper.SetBit(Buffer[Constants.FramePreambleSize], value, 7);
            }
        }

        public int StreamDependency
        {
            get
            {
                if (HasPriority)
                {
                    if (HasPadding)
                    {
                        return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize + PadHighLowLength);
                    }
                    return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize);
                }
                return 0;
            }
            set
            {
                if (HasPadding)
                {
                    FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize + PadHighLowLength, value);
                }
                FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize, value);
            }
        }

        public byte Weight
        {
            get
            {
                if (HasPriority)
                {
                    if (HasPadding)
                    {
                        return Buffer[Constants.FramePreambleSize + PadHighLowLength + DependencyLength];
                    }
                    return Buffer[Constants.FramePreambleSize + DependencyLength];
                }
                return 0;
            }
            set
            {
                if (HasPadding)
                {
                    Buffer[Constants.FramePreambleSize + PadHighLowLength + DependencyLength] = value;
                }
                Buffer[Constants.FramePreambleSize + DependencyLength] = value;
            }
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                int padLength = PadHigh * 256 + PadLow;
                int offset = Constants.FramePreambleSize;

                if (HasPadding) offset += PadHighLowLength;
                if (HasPriority) offset += DependencyLength + WeightLength;

                int count = Buffer.Length - offset - padLength;

                return new ArraySegment<byte>(Buffer, offset, count);
            }
        }

        /* Headers List will be compressed and added to frame Buffer later,
        in WriteQueue.PumpToStream() method. */
        public HeadersList Headers
        {
            get
            {
                return _headers;
            }
            set
            {
                if (value == null)
                    return;

                _headers = value;
            }
        }      
    }
}
