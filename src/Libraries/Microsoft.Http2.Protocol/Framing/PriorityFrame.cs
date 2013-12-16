// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// Priority frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.3
    /// </summary>
    internal class PriorityFrame : Frame
    {
        public int Priority
        {
            get { return  FrameHelpers.Get31BitsAt(Buffer, 8); }
            set { FrameHelpers.Set31BitsAt(Buffer, 8, value); }
        }

        public PriorityFrame(int priority, int streamId)
        {
            Contract.Assert(streamId != 0);
            StreamId = streamId;
            Priority = priority;
            FrameType = FrameType.Priority;
        }
    }
}
