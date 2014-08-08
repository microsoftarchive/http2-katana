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
    /// see 13 -> 6.2
    /// </summary>
    public class HeadersFrame : Frame, IEndStreamFrame, IHeadersFrame, IPaddingFrame
    {
        // 1 byte PadLength field
        private const int PadLengthSize = 1;

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
        public HeadersFrame(int streamId, bool hasPadding, int streamDependency = -1, byte weight = 0, bool exclusive = false)
        {
            /* 13 -> 5.3 
            A client can assign a priority for a new stream by including
            prioritization information in the HEADERS frame */
            bool hasPriority = (streamDependency != -1 && weight != 0);

            int preambleLength = Constants.FramePreambleSize;
            if (hasPadding) preambleLength += PadLengthSize;
            if (hasPriority) preambleLength += DependencyLength + WeightLength;

            // construct frame without Headers Block and Padding bytes
            Buffer = new byte[preambleLength];

            /* 13 -> 6.2 
            The HEADERS frame includes optional padding.  Padding fields and
            flags are identical to those defined for DATA frames. */

            if (hasPadding)
            {
                // generate padding
                var padLength = (byte) new Random().Next(1, 7);
                HasPadding = true;
                PadLength = padLength;
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
                        return FrameHelper.GetBit(Buffer[Constants.FramePreambleSize + PadLengthSize], 7);
                    }
                    return FrameHelper.GetBit(Buffer[Constants.FramePreambleSize], 7);
                }
                return false;
            }
            set
            {
                if (HasPadding)
                {
                    FrameHelper.SetBit(ref Buffer[Constants.FramePreambleSize + PadLengthSize], value, 7);
                    return;
                }
                FrameHelper.SetBit(ref Buffer[Constants.FramePreambleSize], value, 7);
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
                        return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize + PadLengthSize);
                    }
                    return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize);
                }
                return 0;
            }
            set
            {
                if (HasPadding)
                {
                    FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize + PadLengthSize, value);
                    return;
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
                        return Buffer[Constants.FramePreambleSize + PadLengthSize + DependencyLength];
                    }
                    return Buffer[Constants.FramePreambleSize + DependencyLength];
                }
                return 0;
            }
            set
            {
                if (HasPadding)
                {
                    Buffer[Constants.FramePreambleSize + PadLengthSize + DependencyLength] = value;
                    return;
                }
                Buffer[Constants.FramePreambleSize + DependencyLength] = value;
            }
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                int offset = Constants.FramePreambleSize;

                if (HasPadding) offset += PadLengthSize;
                if (HasPriority) offset += DependencyLength + WeightLength;

                int count = Buffer.Length - offset - PadLength;

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
