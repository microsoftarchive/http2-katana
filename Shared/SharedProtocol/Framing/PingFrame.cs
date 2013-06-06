
namespace SharedProtocol.Framing
{
    public class PingFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

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
            if (isPong)
            {
                Flags = FrameFlags.Pong;
            }
            StreamId = 0;
        }     
    }
}
