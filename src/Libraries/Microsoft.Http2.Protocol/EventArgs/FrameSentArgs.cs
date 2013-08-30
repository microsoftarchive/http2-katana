using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.EventArgs
{
    /// <summary>
    /// This class is designed for future usage
    /// </summary>
    public class FrameSentArgs : System.EventArgs
    {
        public Frame Frame { get; private set; }

        public FrameSentArgs(Frame frame)
        {
            Frame = frame;
        }
    }
}
