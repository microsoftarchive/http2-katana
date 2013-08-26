using System.Collections.Generic;
using System.Linq;

namespace SharedProtocol.IO
{
    internal class QueueWrapper : IQueue
    {
        private readonly Queue<IQueueItem> _queue;

        public bool IsDataAvailable
        {
            get { return _queue.Count != 0; }
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public QueueWrapper(int capacity)
        {
            _queue = new Queue<IQueueItem>(capacity);
        }

        public QueueWrapper()
            :this(16)
        {
        }

        public void Enqueue(IQueueItem item)
        {
            _queue.Enqueue(item);
        }

        public IQueueItem Dequeue()
        {
            return _queue.Dequeue();
        }

        public IQueueItem Peek()
        {
            return _queue.Peek();
        }

        public IQueueItem First()
        {
            return _queue.First();
        }

        public IQueueItem Last()
        {
            return _queue.Last();
        }
    }
}
