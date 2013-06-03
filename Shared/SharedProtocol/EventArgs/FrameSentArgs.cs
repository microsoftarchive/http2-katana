using System;
using SharedProtocol.Framing;

namespace SharedProtocol
{
    public class FrameSentArgs : EventArgs
    {
        public Frame Frame { get; private set; }

        public FrameSentArgs(Frame frame)
        {
            Frame = frame;
        }
    }
}
