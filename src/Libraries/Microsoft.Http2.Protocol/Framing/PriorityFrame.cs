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
    /// PRIORITY frame class
    /// see 12 -> 6.3
    /// </summary>
    internal class PriorityFrame : Frame
    {
        // 4 bytes Stream Dependency field
        private const int DependencyLength = 4;

        // 1 byte Weight field
        private const int WeightLength = 1;

        // for incoming
        public PriorityFrame(Frame preamble)
            : base(preamble)
        {
        }

        // for outgoing
        public PriorityFrame(int streamId, int streamDependency, bool isExclusive, byte weight)
        {
            Contract.Assert(streamId != 0);

            // construct frame
            Buffer = new byte[Constants.FramePreambleSize + DependencyLength + WeightLength];

            StreamId = streamId;
            FrameType = FrameType.Priority;
            Exclusive = isExclusive;
            StreamDependency = streamDependency;
            Weight = weight;
        }

        public bool Exclusive
        {
            get
            {
                return FrameHelper.GetBit(Buffer[Constants.FramePreambleSize], 7);
            }
            set
            {
                FrameHelper.SetBit(ref Buffer[Constants.FramePreambleSize], value, 7);
            }
        }

        public int StreamDependency
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

        public byte Weight
        {
            get
            {
                return Buffer[Constants.FramePreambleSize + DependencyLength];
            }
            set
            {
                Buffer[Constants.FramePreambleSize + DependencyLength] = value;
            }
        }
    }
}
