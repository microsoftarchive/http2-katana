// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// RstStream frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.4
    /// </summary>
    internal class RstStreamFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

        // Incoming
        public RstStreamFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public RstStreamFrame(int id, ResetStatusCode statusCode)
            : base(new byte[InitialFrameSize])
        {
            StreamId = id;//32 bit
            FrameType = FrameType.RstStream;//8bit
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 16bit
            StatusCode = statusCode;//32bit
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelpers.Get32BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 8, (int)value);
            }
        }
    }
}
