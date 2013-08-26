
namespace SharedProtocol.Framing
{
    /// <summary>
    /// Ping frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.7
    /// </summary>
    public class PingFrame : Frame
    {
        /// <summary>
        /// Ping frame expected payload length
        /// </summary>
        public const int PayloadLength = 8;

        /// <summary>
        /// The number of bytes in the frame.
        /// </summary>
        public const int FrameSize = PayloadLength + Constants.FramePreambleSize;

        public bool IsPong 
        {
            get
            {
                return (Flags & FrameFlags.Pong) == FrameFlags.Pong;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.Pong;
                }
            }
        }

        // Incoming
        public PingFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public PingFrame(bool isPong, byte[] payload = null)
            : base(new byte[FrameSize])
        {
            FrameType = FrameType.Ping;
            FrameLength = FrameSize - Constants.FramePreambleSize; // 4

            IsPong = isPong;
            StreamId = 0;

            if (payload != null)
            {
                System.Buffer.BlockCopy(Buffer, Constants.FramePreambleSize, Buffer,
                    Constants.FramePreambleSize, FrameSize - Constants.FramePreambleSize);
            }
        }
    }
}
