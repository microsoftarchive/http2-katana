using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Compression.HeadersDeltaCompression
{
    internal class SizedHeadersList : IList<KeyValuePair<string,string>>
    {
        private readonly List<KeyValuePair<string, string>> _collection;


        public SizedHeadersList()
        {
            _collection = new List<KeyValuePair<string, string>>(64);
        }

        public SizedHeadersList(IEnumerable<KeyValuePair<string, string>> headers)
            :this()
        {
            AddRange(headers);
        }

        public int StoredHeadersSize { get; private set; }
        public int Count { get { return _collection.Count; } }
        public bool IsReadOnly { get { return true; } }

        public int FindIndex(Predicate<KeyValuePair<string, string>> match)
        {
            return _collection.FindIndex(match);
        }

        public KeyValuePair<string,string> this[int index]
        {
            get { return _collection[index]; }
            set { _collection[index] = value; }
        }

        public bool Contains(KeyValuePair<string, string> header)
        {
            return _collection.Contains(header);
        }

        public void AddRange(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                Add(header);
            }
        }

        public void Add(KeyValuePair<string, string> header)
        {
            _collection.Add(header);
            StoredHeadersSize += header.Key.Length + header.Value.Length;
        }

        public void CopyTo(KeyValuePair<string, string>[] dest, int offset)
        {
            _collection.CopyTo(dest, offset);
        }

        public bool Remove(KeyValuePair<string, string> header)
        {
            bool wasRemoved = _collection.Remove(header);
            if (wasRemoved)
            {
                StoredHeadersSize -= header.Key.Length + header.Value.Length;
            }
            return wasRemoved;
        }

        public void RemoveAt(int index)
        {
            Contract.Assert(index >= 0 && index < Count);
            var header = _collection[index];
            _collection.RemoveAt(index);
            StoredHeadersSize -= header.Key.Length + header.Value.Length;
        }

        public void Insert(int offset, KeyValuePair<string, string> header)
        {
            _collection.Insert(offset, header);
            StoredHeadersSize += header.Key.Length + header.Value.Length;
        }

        public int IndexOf(KeyValuePair<string, string> header)
        {
            return _collection.IndexOf(header);
        }

        public void Clear()
        {
            _collection.Clear();
            StoredHeadersSize = 0;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
