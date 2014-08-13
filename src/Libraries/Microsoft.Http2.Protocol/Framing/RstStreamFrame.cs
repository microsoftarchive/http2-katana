// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// RST_STREAM frame class
    /// see 14 -> 6.4
    /// </summary>
    internal class RstStreamFrame : Frame
    {
        // 4 bytes Error Code field
        private const int PayloadSize = 4;

        // for incoming
        public RstStreamFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public RstStreamFrame(int id, ResetStatusCode statusCode)
            : base(new byte[Constants.FramePreambleSize + PayloadSize])
        {
            StreamId = id;
            FrameType = FrameType.RstStream;
            PayloadLength = PayloadSize;
            StatusCode = statusCode;
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelper.Get32BitsAt(Buffer, Constants.FramePreambleSize);
            }
            set
            {
                FrameHelper.Set32BitsAt(Buffer, Constants.FramePreambleSize, (int)value);
            }
        }
    }
}
