// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Http2.Protocol.IO
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
