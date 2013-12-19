// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// Ping frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.7
    /// </summary>
    internal class PingFrame : Frame
    {
        /// <summary>
        /// Ping frame expected payload length
        /// </summary>
        public const int PayloadLength = 8;

        /// <summary>
        /// The number of bytes in the frame.
        /// </summary>
        public const int FrameSize = PayloadLength + Constants.FramePreambleSize;

        public bool IsAck 
        {
            get
            {
                return (Flags & FrameFlags.PingAck) == FrameFlags.PingAck;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.PingAck;
                }
            }
        }

        // Incoming
        public PingFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public PingFrame(bool isAck, byte[] payload = null)
            : base(new byte[FrameSize])
        {
            FrameType = FrameType.Ping;
            FrameLength = FrameSize - Constants.FramePreambleSize; // 4

            IsAck = isAck;
            StreamId = 0;

            if (payload != null)
            {
                System.Buffer.BlockCopy(Buffer, Constants.FramePreambleSize, Buffer,
                    Constants.FramePreambleSize, FrameSize - Constants.FramePreambleSize);
            }
        }
    }
}
