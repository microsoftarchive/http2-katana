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
    /// PUSH_PROMISE frame class
    /// see 13 -> 6.6
    /// </summary>
    internal class PushPromiseFrame : Frame, IHeadersFrame, IPaddingFrame
    {
        // 1 byte PadLength field
        private const int PadLengthSize = 1;

        // 4 bytes Promised Stream Id field
        private const int PromisedIdLength = 4;

        private HeadersList _headers = new HeadersList();

        // for incoming
        public PushPromiseFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public PushPromiseFrame(Int32 streamId, Int32 promisedStreamId, bool hasPadding, bool isEndHeaders,
            HeadersList headers = null)
        {
            Contract.Assert(streamId > 0 && promisedStreamId > 0);

            int preambleLength = Constants.FramePreambleSize + PromisedIdLength;
            if (hasPadding) preambleLength += PadLengthSize;

            // construct frame without Headers Block and Padding bytes
            Buffer = new byte[preambleLength];

            /* 13 -> 6.6 
            The PUSH_PROMISE frame includes optional padding. Padding fields and
            flags are identical to those defined for DATA frames. */

            if (hasPadding)
            {
                // generate padding
                var padLength = (byte)new Random().Next(1, 7);
                HasPadding = true;
                PadLength = padLength;
            }

            PayloadLength = Buffer.Length - Constants.FramePreambleSize;
            FrameType = FrameType.PushPromise;
            StreamId = streamId;

            PromisedStreamId = promisedStreamId;
            IsEndHeaders = isEndHeaders;

            if (headers != null) Headers = headers;
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
                return HasPadding ? Buffer[Constants.FramePreambleSize] : (byte)0;
            }
            set { Buffer[Constants.FramePreambleSize] = value; }
        }

        public Int32 PromisedStreamId
        {
            get
            {
                if (HasPadding)
                {
                    return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize + PadLengthSize);
                }
                return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize);
            }
            set
            {
                Contract.Assert(value >= 0 && value <= 255);

                if (HasPadding)
                {
                    FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize + PadLengthSize, value);
                    return;
                }
                FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize, value);
            }
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                int offset = Constants.FramePreambleSize + PromisedIdLength;
                if (HasPadding) offset += PadLengthSize;
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
