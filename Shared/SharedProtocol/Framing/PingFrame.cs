
namespace SharedProtocol.Framing
{
    /// <summary>
    /// Ping frame class
    /// </summary>
    public class PingFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

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
        public PingFrame(bool isPong)
            : base(new byte[InitialFrameSize])
        {
            FrameType = FrameType.Ping;
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 4

            IsPong = isPong;
            StreamId = 0;
        }     
    }
}
