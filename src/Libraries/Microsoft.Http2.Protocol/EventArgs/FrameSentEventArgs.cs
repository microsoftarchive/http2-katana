using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.EventArgs
{
    /// <summary>
    /// This class is designed for future usage
    /// </summary>
    public class FrameSentEventArgs : System.EventArgs
    {
        public Frame Frame { get; private set; }

        public FrameSentEventArgs(Frame frame)
        {
            Frame = frame;
        }
    }
}
