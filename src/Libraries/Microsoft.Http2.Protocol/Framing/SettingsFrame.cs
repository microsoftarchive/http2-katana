using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Framing
{
    /// <summary>
    /// Settings frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.5
    /// </summary>
    internal class SettingsFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 8;

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
            StreamId = 0;

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
