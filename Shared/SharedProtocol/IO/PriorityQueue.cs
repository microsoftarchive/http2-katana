using System;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol.Framing;

namespace SharedProtocol.IO
{
    internal class PriorityQueue : IQueue
    {
        private readonly Dictionary<Priority, Queue<IPriorityItem>> _storage;
        private const byte _possiblePriValues = 8; //0..7
        private int _highestPri;
        private int _lowestPri;
        private readonly object _lock = new object();

        public int Count { get; private set; }

        public bool IsDataAvailable 
        { 
            get { return Count != 0; }
        }

        public PriorityQueue()
        {
            _storage = new Dictionary<Priority, Queue<IPriorityItem>>(_possiblePriValues);
            for (int i = 0; i < _possiblePriValues; i++)
            {
                _storage[(Priority) i] = new Queue<IPriorityItem>(16);
            }

            _highestPri = -1;
            _lowestPri = 1000000;
        }

        public PriorityQueue(IEnumerable<IPriorityItem> initialCollection)
        {
            EnqueueRange(initialCollection);
        }

        private void RecalcHighestPriority()
        {
            foreach (var pri in _storage.Keys)
            {
                if (_storage[pri].Count != 0)
                {
                    _highestPri = (int) pri;
                }
            }
        }

        private void RecalcLowestPriority()
        {
            foreach (var pri in _storage.Keys)
            {
                if (_storage[pri].Count != 0)
                {
                    _lowestPri = (int) pri;
                    break;
                }
            }
        }

        private void RecalcPriorities()
        {
            _lowestPri = 100000;
            foreach (var pri in _storage.Keys)
            {
                if (_storage[pri].Count != 0)
                {
                    if (_lowestPri == 100000)
                    {
                        _lowestPri = (int) pri;
                    }
                    _highestPri = (int)pri;   
                }
            }
        }

        public void Enqueue(IQueueItem item)
        {
            lock (_lock)
            {
                if (!(item is IPriorityItem))
                {
                    throw new ArgumentException("Cant enqueue item into priority queue. Argument should be IPriorityItem");
                }
                var pri = (item as IPriorityItem).Priority;

                if ((int) pri > _highestPri)
                {
                    _highestPri = (int) pri;
                }
                if ((int) pri < _lowestPri)
                {
                    _lowestPri = (int) pri;
                }

                Count++;

                _storage[pri].Enqueue((IPriorityItem)item);
            }
        }

        public void EnqueueRange(IEnumerable<IPriorityItem> items)
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public IQueueItem Dequeue()
        {
            lock (_lock)
            {
                if (_storage.Count == 0)
                {
                    return null;
                }

                var result = _storage[(Priority)_highestPri].Dequeue();

                if (--Count == 0)
                {
                    _highestPri = -1;
                    _lowestPri = 100000;
                }
                else
                {
                    RecalcPriorities();
                }

                return result;
            }
        }

        public IQueueItem Peek()
        {
            if (_storage.Count == 0)
            {
                return null;
            }

            return _storage[(Priority) _highestPri].Peek();
        }

        public IQueueItem First()
        {
            return _storage[(Priority) _lowestPri].First();
        }

        public IQueueItem Last()
        {
            return _storage[(Priority)_highestPri].Last();
        }
    }
}
