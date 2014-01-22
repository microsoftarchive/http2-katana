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
    // |C|       Stream-ID (31bits)       |
    // +----------------------------------+
    // | Flags (8)  |  Length (24 bits)   |
    public class DataFrame : Frame, IEndStreamFrame
    {
        // For incoming
        public DataFrame(Frame preamble)
            : base(preamble)
        {
        }

        // For outgoing
        public DataFrame(int streamId, ArraySegment<byte> data, bool isEndStream)
            : base(new byte[Constants.FramePreambleSize + data.Count])
        {
            IsEndStream = isEndStream;
            FrameLength = data.Count;
            StreamId = streamId;
            Contract.Assert(data.Array != null);
            System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Constants.FramePreambleSize, data.Count);
        }

        // 8 bits, 24-31
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

        public ArraySegment<byte> Data
        {
            get
            {
                return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize, 
                    Buffer.Length - Constants.FramePreambleSize);
            }
        }
    }
}
