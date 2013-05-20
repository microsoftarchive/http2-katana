
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
        public PingFrame(int pingId)
            : base(new byte[InitialFrameSize])
        {
            FrameType = FrameType.Ping;
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 4
            Id = pingId;
        }

        // 32 bits
        public int Id
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
    }
}
