// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// WINDOW_UPDATE frame class
    /// see 14 -> 6.9
    /// </summary>
    internal class WindowUpdateFrame : Frame
    {
        // 4 bytes Error Code field
        private const int PayloadSize = 4;
                
        // for incoming
        public WindowUpdateFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public WindowUpdateFrame(int id, int delta)
            : base(new byte[Constants.FramePreambleSize + PayloadSize])
        {
            StreamId = id;
            FrameType = FrameType.WindowUpdate;
            PayloadLength = PayloadSize;
            Delta = delta;
        }

        // 31 bits
        public int Delta
        {
            get
            {
                return FrameHelper.Get31BitsAt(Buffer, Constants.FramePreambleSize);
            }
            set
            {
                FrameHelper.Set31BitsAt(Buffer, Constants.FramePreambleSize, value);
            }
        }
    }
}
