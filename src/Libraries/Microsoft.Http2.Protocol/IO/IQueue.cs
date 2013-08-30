namespace Microsoft.Http2.Protocol.IO
{
    internal interface IQueue
    {
        void Enqueue(IQueueItem item);
        IQueueItem Dequeue();
        IQueueItem Peek();
        IQueueItem First();
        IQueueItem Last();
        bool IsDataAvailable { get; }
        int Count { get; }
    }
}
