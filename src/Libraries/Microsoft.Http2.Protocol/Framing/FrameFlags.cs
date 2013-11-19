using System;

namespace Microsoft.Http2.Protocol.Framing
{
    [Flags]
    public enum FrameFlags
    {
        None = 0x00,
        EndStream = 0x01,
        Pong = 0x01,
        EndHeaders = 0x04,
        EndPushPromise = 0x04,
        Priority = 0x08,
    }
}
