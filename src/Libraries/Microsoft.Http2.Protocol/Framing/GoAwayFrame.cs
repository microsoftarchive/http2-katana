
namespace SharedProtocol.Framing
{
    /// <summary>
    /// This class defines GoAway frame
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.8
    /// </summary>
    public class GoAwayFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 24;

        // Incoming
        public GoAwayFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public GoAwayFrame(int lastStreamId, ResetStatusCode statusCode)
            : base(new byte[InitialFrameSize])
        {
            FrameType = FrameType.GoAway;
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 16 bytes
            LastGoodStreamId = lastStreamId;
            StatusCode = statusCode;
        }

        // 31 bits
        public int LastGoodStreamId
        {
            get
            {
                return FrameHelpers.Get31BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set31BitsAt(Buffer, 8, value);
            }
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelpers.Get32BitsAt(Buffer, 12);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 12, (int)value);
            }
        }
    }
}
