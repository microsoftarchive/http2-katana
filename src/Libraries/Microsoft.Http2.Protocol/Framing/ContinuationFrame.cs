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
    /// CONTINUATION frame class
    /// see 12 -> 6.10
    /// </summary>
    internal class ContinuationFrame : Frame, IHeadersFrame, IPaddingFrame
    {
        // 1 byte Pad High, 1 byte Pad Low field
        private const int PadHighLowLength = 2;

        private HeadersList _headers = new HeadersList();

        // for incoming
        public ContinuationFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public ContinuationFrame(int streamId, byte[] headers, bool hasPadding, bool isEndHeaders)
        {
            /* 12 -> 6.10
            The CONTINUATION frame includes optional padding.  Padding fields and
            flags are identical to those defined for DATA frames. */

            if (hasPadding)
            {
                // generate padding
                var padHigh = (byte) 1;
                var padLow = (byte) new Random().Next(1, 7);
                int padLength = padHigh * 256 + padLow;

                // construct frame with padding
                Buffer = new byte[Constants.FramePreambleSize + PadHighLowLength + headers.Length + padLength];
                HasPadHigh = true;
                HasPadLow = true;
                PadHigh = padHigh;
                PadLow = padLow;
                PayloadLength = PadHighLowLength + headers.Length + padLength;

                System.Buffer.BlockCopy(headers, 0, Buffer, Constants.FramePreambleSize + PadHighLowLength, headers.Length);
            }
            else
            {
                // construct frame without padding
                Buffer = new byte[Constants.FramePreambleSize + headers.Length];
                PayloadLength = headers.Length;

                System.Buffer.BlockCopy(headers, 0, Buffer, Constants.FramePreambleSize, headers.Length);
            }

            StreamId = streamId;
            FrameType = FrameType.PushPromise;
            IsEndHeaders = isEndHeaders;
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

        public bool HasPadding
        {
            get { return HasPadHigh && HasPadLow; }
        }

        public byte PadHigh
        {
            get
            {
                return HasPadding ? Buffer[Constants.FramePreambleSize] : (byte)0;
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

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                int padLength = PadHigh * 256 + PadLow;
                int offset = Constants.FramePreambleSize;

                if (HasPadding) offset += PadHighLowLength;

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
