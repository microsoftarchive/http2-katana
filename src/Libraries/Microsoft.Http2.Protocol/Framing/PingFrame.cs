// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// PING frame class
    /// see 14 -> 6.7
    /// </summary>
    internal class PingFrame : Frame
    {
        // 8 bytes Opaque Data
        public const int OpaqueDataLength = 8;

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

        // for incoming
        public PingFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public PingFrame(bool isAck, byte[] opaqueData = null)
            : base(new byte[Constants.FramePreambleSize + OpaqueDataLength])
        {
            FrameType = FrameType.Ping;
            PayloadLength = OpaqueDataLength;
            IsAck = isAck;
            StreamId = 0;

            if (opaqueData != null)
            {
                System.Buffer.BlockCopy(Buffer, Constants.FramePreambleSize, Buffer,
                    Constants.FramePreambleSize, OpaqueDataLength);
            }
        }
    }
}
