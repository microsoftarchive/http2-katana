using System;

namespace SharedProtocol
{
    [Flags]
    // Normal state events on a HTTP/2.0 stream.
    // Using flags as these may happen in various orders due to race conditions.
    internal enum StreamState
    {
        None = 0,
        RequestHeaders = 0x01, // sent (client) or received (server)
        ResponseHeaders = 0x02, // sent (server) or received (client)
        EndStreamSent = 0x04,
        EndStreamReceived = 0x08,
        ResetSent = 0x10,
        ResetReceived = 0x20,
        Disposed = 0x40,
    }
}
