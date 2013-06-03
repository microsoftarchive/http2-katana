using System;
using SharedProtocol.Framing;

namespace SharedProtocol
{
    public class FrameReceivedEventArgs : EventArgs
    {
        public Frame Frame { get; private set; }
        public Http2Stream Stream { get; private set; }

        public FrameReceivedEventArgs(Http2Stream stream, Frame frame)
        {
            Stream = stream;
            Frame = frame;
        }
    }
}
