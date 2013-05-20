using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.Framing
{
    public class SettingsFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

        // Incoming
        public SettingsFrame(Frame preamble)
            : base(preamble)
        {
            
        }

        // Outgoing
        public SettingsFrame(IList<SettingsPair> settings)
            : base(new byte[InitialFrameSize + settings.Count * SettingsPair.PairSize])
        {
            FrameType = FrameType.Settings;
            FrameLength = (settings.Count * SettingsPair.PairSize) + InitialFrameSize - Constants.FramePreambleSize;
            EntryCount = settings.Count;
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
            get
            {
                return FrameHelpers.Get32BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 8, value);
            }
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
