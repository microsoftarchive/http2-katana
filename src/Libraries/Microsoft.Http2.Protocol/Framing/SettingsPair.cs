using System;

namespace SharedProtocol.Framing
{
    internal struct SettingsPair
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
