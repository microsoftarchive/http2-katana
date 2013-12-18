// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// Settings frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.5
    /// </summary>
    public class SettingsFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 8;

        // Incoming
        public SettingsFrame(Frame preamble)
            : base(preamble)
        {
            
        }

        // Outgoing
        public SettingsFrame(IList<SettingsPair> settings, bool isAck)
            : base(new byte[InitialFrameSize + settings.Count * SettingsPair.PairSize])
        {
            FrameType = FrameType.Settings;
            FrameLength = (settings.Count * SettingsPair.PairSize) + InitialFrameSize - Constants.FramePreambleSize;
            StreamId = 0;

            if (isAck)
                Flags |= FrameFlags.Ack;
            
            for (int i = 0; i < settings.Count; i++)
            {
                ArraySegment<byte> segment = settings[i].BufferSegment;
                System.Buffer.BlockCopy(segment.Array, segment.Offset, Buffer,
                    InitialFrameSize + i * SettingsPair.PairSize, SettingsPair.PairSize);
            }
        }

        // 32 bits
        public int EntryCount
        {
            get { return (Buffer.Length - InitialFrameSize) / SettingsPair.PairSize; }
        }

        public bool IsAck
        {
            get { return (Flags & FrameFlags.Ack) == FrameFlags.Ack; }
        }

        public SettingsPair this[int index]
        {
            get
            {
                Contract.Assert(index < EntryCount);
                return new SettingsPair(new ArraySegment<byte>(Buffer, 
                    InitialFrameSize + index * SettingsPair.PairSize, SettingsPair.PairSize));
            }
        }
    }
}
