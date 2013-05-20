using System;
using SharedProtocol.Framing;

namespace SharedProtocol
{
    public class FrameSentEventArgs : EventArgs
    {
        public Frame Frame { get; private set; }

        public FrameSentEventArgs(Frame frame)
        {
            this.Frame = frame;
        }
    }
}
