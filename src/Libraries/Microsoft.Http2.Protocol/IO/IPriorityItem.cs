namespace Microsoft.Http2.Protocol.IO
{
    internal interface IPriorityItem : IQueueItem
    {
        int Priority { get; }
    }
}
