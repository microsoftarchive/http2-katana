using System;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Framing
{
    public class Headers : Frame, IEndStreamFrame
    {
        // The number of bytes in the frame, not including the compressed headers.
        private const int InitialFrameSize = 12;

        // 8 bits, 24-31
        public bool IsEndStream
        {
            get
            {
                return (Flags & FrameFlags.EndStream) == FrameFlags.EndStream;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndStream;
                }
            }
        }

        public bool IsPriority
        {
            get { return (Flags & FrameFlags.Priority) == FrameFlags.Priority; }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Priority;
                }
            }
        }

        public bool IsEndHeaders
        {
            get
            {
                return (Flags & FrameFlags.EndHeaders) == FrameFlags.EndHeaders;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndHeaders;
                }
            }
        }

        // Create an outgoing frame
        public Headers(int streamId, byte[] headerBytes)
            : base(new byte[InitialFrameSize + headerBytes.Length])
        {
            StreamId = streamId;
            FrameType = FrameType.Headers;
            FrameLength = Buffer.Length - Constants.FramePreambleSize;

            // Copy in the headers
            System.Buffer.BlockCopy(headerBytes, 0, Buffer, InitialFrameSize, headerBytes.Length);
        }

        // Create an incoming frame
        public Headers(Frame preamble)
            : base(preamble)
        {
        }

        public Priority Priority
        {
            get
            {
                Contract.Assert(IsPriority);
                return (Priority) FrameHelpers.Get32BitsAt(Buffer, 8); 
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
