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
    /// SETTINGS frame class
    /// see 13 -> 6.5
    /// </summary>
    public class SettingsFrame : Frame
    {
        // for incoming
        public SettingsFrame(Frame preamble)
            : base(preamble)
        {            
        }

        // for outgoing
        public SettingsFrame(IList<SettingsPair> settings, bool isAck)
            : base(new byte[Constants.FramePreambleSize + (settings.Count * SettingsPair.PairSize)])
        {
            FrameType = FrameType.Settings;
            PayloadLength = settings.Count * SettingsPair.PairSize;
            StreamId = 0;
            IsAck = isAck;
            
            for (int i = 0; i < settings.Count; i++)
            {
                ArraySegment<byte> segment = settings[i].BufferSegment;
                System.Buffer.BlockCopy(segment.Array, segment.Offset, Buffer,
                    Constants.FramePreambleSize + i * SettingsPair.PairSize, SettingsPair.PairSize);
            }
        }
      
        public bool IsAck
        {
            get
            {
                return (Flags & FrameFlags.Ack) == FrameFlags.Ack;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Ack;
                }
            }
        }

        public int EntryCount
        {
            get { return (Buffer.Length - Constants.FramePreambleSize) / SettingsPair.PairSize; }
        }

        public SettingsPair this[int index]
        {
            get
            {
                Contract.Assert(index < EntryCount);

                return new SettingsPair(new ArraySegment<byte>(Buffer,
                    Constants.FramePreambleSize + index * SettingsPair.PairSize, SettingsPair.PairSize));
            }
        }
    }
}
