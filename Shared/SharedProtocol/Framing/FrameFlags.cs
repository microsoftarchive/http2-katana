using System;

namespace SharedProtocol.Framing
{
    [Flags]
    public enum FrameFlags
    {
        None = 0x00,
        EndStream = 0x01,
        Pong = 0x02,
        EndHeaders = 0x04,
        Priority = 0x08,
    }
}
