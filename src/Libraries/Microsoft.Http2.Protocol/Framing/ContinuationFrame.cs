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
    /// see 14 -> 6.10
    /// </summary>
    internal class ContinuationFrame : Frame, IHeadersFrame
    {
        private HeadersList _headers = new HeadersList();

        // for incoming
        public ContinuationFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public ContinuationFrame(int streamId, byte[] headers, bool isEndHeaders)
        {
            Buffer = new byte[Constants.FramePreambleSize + headers.Length];
            PayloadLength = headers.Length;

            System.Buffer.BlockCopy(headers, 0, Buffer, Constants.FramePreambleSize, headers.Length);

            StreamId = streamId;
            FrameType = FrameType.Continuation;
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

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                int count = Buffer.Length - Constants.FramePreambleSize;
                return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize, count);
            }
        }

        /* Headers List will be compressed and added to frame Buffer later,
        in OutgoingQueue.PumpToStream() method. */
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
