using System;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Framing
{
    public class HeadersPlusPriority : Frame
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

        // Create an outgoing frame
        public HeadersPlusPriority(int streamId, byte[] headerBytes)
            : base(new byte[InitialFrameSize + headerBytes.Length])
        {
            StreamId = streamId;
            FrameType = FrameType.HeadersPlusPriority;
            FrameLength = Buffer.Length - Constants.FramePreambleSize;

            // Copy in the headers
            System.Buffer.BlockCopy(headerBytes, 0, Buffer, InitialFrameSize, headerBytes.Length);
        }

        // Create an incoming frame
        public HeadersPlusPriority(Frame preamble)
            : base(preamble)
        {
        }

        public Priority Priority
        {
            get
            {
                return (Priority)FrameHelpers.Get32BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 8, (int)value);
            }
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get
            {
                return new ArraySegment<byte>(Buffer, 12, Buffer.Length - 12);
            }
        }
    }
}
