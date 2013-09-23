namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// RstStream frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.4
    /// </summary>
    internal class RstStreamFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

        // Incoming
        public RstStreamFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public RstStreamFrame(int id, ResetStatusCode statusCode)
            : base(new byte[InitialFrameSize])
        {
            Flags |= FrameFlags.EndStream;
            StreamId = id;//32 bit
            FrameType = FrameType.RstStream;//8bit
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 16bit
            StatusCode = statusCode;//32bit
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelpers.Get32BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 8, (int)value);
            }
        }
    }
}
