using System;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Framing
{
    // |C|       Stream-ID (31bits)       |
    // +----------------------------------+
    // | Flags (8)  |  Length (24 bits)   |
    public class DataFrame : Frame, IEndStreamFrame
    {
        // For incoming
        public DataFrame(Frame preamble)
            : base(preamble)
        {
        }

        // For outgoing
        public DataFrame(int streamId, ArraySegment<byte> data, bool isEndStream)
            : base(new byte[Constants.FramePreambleSize + data.Count])
        {
            IsEndStream = isEndStream;
            FrameLength = data.Count;
            StreamId = streamId;
            Contract.Assert(data.Array != null);
            System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Constants.FramePreambleSize, data.Count);
        }

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

        public ArraySegment<byte> Data
        {
            get
            {
                return new ArraySegment<byte>(Buffer, Constants.FramePreambleSize, 
                    Buffer.Length - Constants.FramePreambleSize);
            }
        }
    }
}
