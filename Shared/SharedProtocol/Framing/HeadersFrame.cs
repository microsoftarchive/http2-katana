using System;

namespace SharedProtocol.Framing
{
    public class HeadersFrame : Frame
    {
        // The number of bytes in the frame, not including the compressed headers.
        private const int InitialFrameSize = 12;

        public bool IsContinues 
        {
            get
            {
                return (Flags & FrameFlags.Continues) == FrameFlags.Continues;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Continues;
                }
            }
        }

        // Incoming
        public HeadersFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public HeadersFrame(int streamId, byte[] compressedHeaders)
            : base(new byte[InitialFrameSize + compressedHeaders.Length])
        {
            StreamId = streamId;
            FrameType = FrameType.Headers;
            FrameLength = InitialFrameSize - Constants.FramePreambleSize + compressedHeaders.Length;

            // Copy in the headers
            System.Buffer.BlockCopy(compressedHeaders, 0, Buffer, InitialFrameSize, compressedHeaders.Length);
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                return new ArraySegment<byte>(Buffer, InitialFrameSize, Buffer.Length - InitialFrameSize);
            }
        }
    }
}
