// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol.Framing
{
    public struct SettingsPair
    {
        //TODO Make sure, that 8 bytes instead of 7. (3 bytes to settings Id, 4 for value)
        public const int PairSize = 8; // Bytes

        private readonly ArraySegment<byte> _bufferSegment;

        // Incoming
        public SettingsPair(ArraySegment<byte> bufferSegment)
        {
            _bufferSegment = bufferSegment;
        }

        // Outgoing
        public SettingsPair(SettingsFlags flags, SettingsIds id, int value)
        {
            _bufferSegment = new ArraySegment<byte>(new byte[PairSize], 0, PairSize);
            Flags = flags;
            Id = id;
            Value = value;
        }

        public ArraySegment<byte> BufferSegment
        {
            get
            {
                return _bufferSegment;
            }
        }

        public SettingsFlags Flags
        {
            get
            {
                return (SettingsFlags)_bufferSegment.Array[_bufferSegment.Offset];
            }
            set
            {
                _bufferSegment.Array[_bufferSegment.Offset] = (byte)value;
            }
        }

        public SettingsIds Id
        {
            get
            {
                return (SettingsIds)FrameHelpers.Get24BitsAt(_bufferSegment.Array, _bufferSegment.Offset + 1);
            }
            set
            {
                FrameHelpers.Set24BitsAt(_bufferSegment.Array, _bufferSegment.Offset + 1, (int)value);
            }
        }

        public int Value
        {
            get
            {
                return FrameHelpers.Get32BitsAt(_bufferSegment.Array, _bufferSegment.Offset + 4);
            }
            set
            {
                FrameHelpers.Set32BitsAt(_bufferSegment.Array, _bufferSegment.Offset + 4, value);
            }
        }
    }
}
