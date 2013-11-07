using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.IO
{
    internal interface IQueueItem
    {
        byte[] Buffer { get; }
        Frame Frame { get; }
    }
}
