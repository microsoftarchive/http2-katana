namespace Microsoft.Http2.Protocol.IO
{
    internal interface IQueueItem
    {
        byte[] Buffer { get; }
    }
}
