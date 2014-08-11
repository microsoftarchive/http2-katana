// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// see 14 -> 6.5.1
    /// </summary>
    public struct SettingsPair
    {
        /* The payload of a SETTINGS frame consists of zero or more parameters,
        each consisting of an unsigned 16-bit identifier and an unsigned 32-bit value. */

        // 2 bytes for identifier, 4 bytes for value
        public const int PairSize = 6;
        private const int IdSize = 2;

        private readonly ArraySegment<byte> _bufferSegment;

        // for incoming
        public SettingsPair(ArraySegment<byte> bufferSegment)
        {
            _bufferSegment = bufferSegment;
        }

        // for outgoing
        public SettingsPair(SettingsIds id, int value)
        {
            _bufferSegment = new ArraySegment<byte>(new byte[PairSize], 0, PairSize);
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

        public SettingsIds Id
        {
            get
            {
                return (SettingsIds)FrameHelper.Get16BitsAt(_bufferSegment.Array, _bufferSegment.Offset);
            }
            set
            {
                FrameHelper.Set16BitsAt(_bufferSegment.Array, _bufferSegment.Offset, (int)value);
            }
        }

        public int Value
        {
            get
            {
                return FrameHelper.Get32BitsAt(_bufferSegment.Array, _bufferSegment.Offset + IdSize);
            }
            set
            {
                FrameHelper.Set32BitsAt(_bufferSegment.Array, _bufferSegment.Offset + IdSize, value);
            }
        }
    }
}
