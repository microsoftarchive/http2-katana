using System;

namespace SharedProtocol.Framing
{
    [Flags]
    public enum FrameFlags
    {
        None = 0x00,
        Fin = 0x01,
        Pong = 0x02,
        Continues = 0x02
    }
}
