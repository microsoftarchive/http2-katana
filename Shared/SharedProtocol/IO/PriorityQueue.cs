using SharedProtocol.Framing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    public class PriorityQueue
    {
        private readonly object _queueLock;
        private readonly LinkedList<PriorityQueueEntry> _queue;

        public PriorityQueue()
        {
            _queueLock = new object();
            _queue = new LinkedList<PriorityQueueEntry>();
        }

        // TODO: How can we make enqueue faster than O(n) and still maintain O(1) dequeue?
        public void Enqueue(PriorityQueueEntry entry)
        {
            lock (_queueLock)
            {
                // Scan backwards, so we end up in order behind other items of the same priority.
                LinkedListNode<PriorityQueueEntry> current = _queue.Last;
                /* Disabled until it can be refactored. Header frames must never be re-ordered due to compression.
                while (current != null && current.Value.Priority > entry.Priority)
                {
                    current = current.Previous;
                }
                */

                if (current == null)
                {
                    // New entry is highest priority (the list may be empty).
                    _queue.AddFirst(entry);
                }
                else
                {
                    _queue.AddAfter(current, entry);
                }
            }
        }

        public bool TryDequeue(out PriorityQueueEntry entry)
        {
            lock (_queueLock)
            {
                if (_queue.Count == 0)
                {
                    entry = null;
                    return false;
                }

                entry = _queue.First.Value;
                _queue.RemoveFirst();
                return true;
            }
        }

        public bool IsDataAvailable
        {
            get
            {
                return _queue.Count > 0;
            }
        }
    }
}
