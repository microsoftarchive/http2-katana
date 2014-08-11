// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// This class defines GoAway frame
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-14#section-6.8
    /// </summary>
    internal class GoAwayFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = Constants.FramePreambleSize + 16;//25

        // Incoming
        public GoAwayFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public GoAwayFrame(int lastStreamId, ResetStatusCode statusCode)
            : base(new byte[InitialFrameSize])
        {
            FrameType = FrameType.GoAway;
            PayloadLength = InitialFrameSize - Constants.FramePreambleSize; // 16 bytes
            LastGoodStreamId = lastStreamId;
            StatusCode = statusCode;
        }

        // 31 bits
        public int LastGoodStreamId
        {
            get
            {
                return FrameHelper.Get31BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelper.Set31BitsAt(Buffer, 8, value);
            }
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelper.Get32BitsAt(Buffer, 12);
            }
            set
            {
                FrameHelper.Set32BitsAt(Buffer, 12, (int)value);
            }
        }
    }
}
