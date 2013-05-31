using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    internal class ActiveStreams : IDictionary<int, Http2Stream>
    {
        private class ActiveStreamsEnumerator : IEnumerator<KeyValuePair<int, Http2Stream>>
        {
            private ActiveStreams _collection;
            private KeyValuePair<int, Http2Stream> _curPair;
            private Dictionary<int, Http2Stream>.Enumerator _nonControlledEnum;
            private Dictionary<int, Http2Stream>.Enumerator _controlledEnum;

            public ActiveStreamsEnumerator(ActiveStreams collection)
            {
                _collection = collection;
                _curPair = default(KeyValuePair<int, Http2Stream>);

                _nonControlledEnum = _collection.NonFlowControlledStreams.GetEnumerator();
                _controlledEnum = _collection.FlowControlledStreams.GetEnumerator();
            }

            public bool MoveNext()
            {
                Dictionary<int, Http2Stream>.Enumerator en = _nonControlledEnum;
                if (_nonControlledEnum.MoveNext() == false)
                {
                    if (_controlledEnum.MoveNext() == false)
                    {
                        return false;
                    }
                    en = _controlledEnum;
                }

                _curPair = en.Current;
                return true;
            }

            public void Reset()
            {
                _curPair = default(KeyValuePair<int, Http2Stream>);
            }

            void IDisposable.Dispose()
            {
                _nonControlledEnum.Dispose();
                _controlledEnum.Dispose();
            }

            public KeyValuePair<int, Http2Stream> Current
            {
                get { return _curPair; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        public Dictionary<int, Http2Stream> NonFlowControlledStreams { get; private set; }
        public Dictionary<int, Http2Stream> FlowControlledStreams { get; private set; }

        public ActiveStreams()
            :this(10)
        {
        }

        public ActiveStreams(int capacity)
        {
            NonFlowControlledStreams = new Dictionary<int, Http2Stream>(capacity / 2);
            FlowControlledStreams = new Dictionary<int, Http2Stream>(capacity / 2);
        }

        public bool TryGetValue(int key, out Http2Stream value)
        {
            if (FlowControlledStreams.TryGetValue(key, out value))
            {
                return true;
            }

            if (NonFlowControlledStreams.TryGetValue(key, out value))
            {
                return true;
            }

            return false;
        }

        public Http2Stream this[int key]
        {
            get
            {
                if (FlowControlledStreams.ContainsKey(key))
                {
                    return FlowControlledStreams[key];
                }
                if (NonFlowControlledStreams.ContainsKey(key))
                {
                    return NonFlowControlledStreams[key];
                }

                return null;
            }
            set
            {
                Add(value);
            }
        }

        public void Add(Http2Stream item)
        {
            if (ContainsKey(item.Id))
            {
                throw new ArgumentException("This key already exists in the collection");
            }

            if (item.IsFlowControlEnabled)
            {
                FlowControlledStreams.Add(item.Id, item);
                return;
            }

            NonFlowControlledStreams.Add(item.Id, item);
        }

        public void Add(int key, Http2Stream value)
        {
            Add(value);
        }

        public void Add(KeyValuePair<int, Http2Stream> item)
        {
            Add(item.Value);
        }

        public void Clear()
        {
            FlowControlledStreams.Clear();
            NonFlowControlledStreams.Clear();
        }

        public bool ContainsKey(int id)
        {
            return NonFlowControlledStreams.ContainsKey(id) || FlowControlledStreams.ContainsKey(id);
        }

        public bool Contains(KeyValuePair<int, Http2Stream> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<int, Http2Stream>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public ICollection<int> Keys
        {
            get
            {
                var fc = FlowControlledStreams.Keys.ToArray();
                var nfc = NonFlowControlledStreams.Keys.ToArray();
                var result = new int[fc.Length + nfc.Length];

                Array.Copy(fc, 0, result, 0, fc.Length);
                Array.Copy(nfc, 0, result, fc.Length, nfc.Length);

                return result;
            }
        }

        public ICollection<Http2Stream> Values
        {
            get
            {
                var fc = FlowControlledStreams.Values.ToArray();
                var nfc = NonFlowControlledStreams.Values.ToArray();
                var result = new Http2Stream[fc.Length + nfc.Length];

                Array.Copy(fc, 0, result, 0, fc.Length);
                Array.Copy(nfc, 0, result, fc.Length, nfc.Length);

                return result;
            }
        }

        public int Count
        {
            get { return NonFlowControlledStreams.Count + FlowControlledStreams.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<int, Http2Stream> item)
        {
            return Remove(item.Key);
        }

        public bool Remove(int itemId)
        {
            if (FlowControlledStreams.ContainsKey(itemId))
            {
                return FlowControlledStreams.Remove(itemId);
            }
            if (NonFlowControlledStreams.ContainsKey(itemId))
            {
                return NonFlowControlledStreams.Remove(itemId);
            }
            return true; //Nothing to delete. We think that item was already deleted.
        }

        public bool Remove(Http2Stream item)
        {
            return Remove(item.Id);
        }

        public bool IsStreamFlowControlled(Http2Stream stream)
        {
            return FlowControlledStreams.ContainsKey(stream.Id);
        }

        public void DisableFlowControl(Http2Stream stream)
        {
            if (IsStreamFlowControlled(stream))
            {
                Remove(stream);
                stream.IsFlowControlEnabled = false;
                Add(stream);
            }
        }

        public IEnumerator<KeyValuePair<int, Http2Stream>> GetEnumerator()
        {
            return new ActiveStreamsEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
