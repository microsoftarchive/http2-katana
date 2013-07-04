using SharedProtocol.Framing;

namespace SharedProtocol.IO
{
    internal interface IPriorityItem : IQueueItem
    {
        Priority Priority { get; }
    }
}
