using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.IO
{
    internal interface IPriorityItem : IQueueItem
    {
        Priority Priority { get; }
    }
}
