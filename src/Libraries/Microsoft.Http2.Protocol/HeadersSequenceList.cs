using System.Collections.Generic;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol
{
    internal class HeadersSequenceList
    {
        private List<HeadersSequence>  _collection;
        private object _modificationLock;

        internal HeadersSequenceList(IEnumerable<HeadersSequence> initialCollection = null)
        {
            _collection = initialCollection != null ? 
                            new List<HeadersSequence>(initialCollection) : 
                            new List<HeadersSequence>(64);

            _modificationLock = new object();
        }

        public HeadersSequence Find(int id)
        {
            return _collection.Find(seq => seq.StreamId == id);
        }

        public void Add(HeadersSequence item)
        {
            lock (_modificationLock)
            {
                _collection.Add(item);
            }
        }

        public void Remove(HeadersSequence item)
        {
            lock (_modificationLock)
            {
                _collection.Remove(item);
            }
        }
    }
}
