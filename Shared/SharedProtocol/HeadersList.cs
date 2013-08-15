using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SharedProtocol
{
    /// <summary>
    /// Headers list class.
    /// </summary>
    public class HeadersList : IList<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _collection;

        public int StoredHeadersSize { get; private set; }

        public HeadersList() 
            :this(16)
        { }

        public HeadersList(IEnumerable<KeyValuePair<string, string>> list)
        {
            _collection = new List<KeyValuePair<string, string>>();
            AddRange(list);
        }

        public HeadersList(int capacity)
        {
            _collection = new List<KeyValuePair<string, string>>(capacity);
        }

        public string GetValue(string key)
        {
            var headerFound = _collection.Find(header => header.Key == key);

            if (!headerFound.Equals(default(KeyValuePair<string, string>)))
            {
                return headerFound.Value;
            }

            return null;
        }

        public void AddRange(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                Add(header);
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> header)
        {
            _collection.Add(header);
            StoredHeadersSize += header.Key.Length + header.Value.Length + sizeof(Int32);
        }

        public void Clear()
        {
            _collection.Clear();
            StoredHeadersSize = 0;
        }

        public bool Contains(KeyValuePair<string, string> header)
        {
            return _collection.Contains(header);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            Contract.Assert(arrayIndex >= 0 && arrayIndex < Count && array != null);
            _collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> header)
        {
            StoredHeadersSize -= header.Key.Length + header.Value.Length + sizeof(Int32);
            return _collection.Remove(header);
        }

        public int Count
        {
            get { return _collection.Count; }
        }
    
        public bool IsReadOnly
        {
            get { return true; }
        }

        public int FindIndex(Predicate<KeyValuePair<string, string>> predicate)
        {
            return _collection.FindIndex(predicate);
        }

        public int IndexOf(KeyValuePair<string, string> header)
        {
            return _collection.IndexOf(header);
        }

        public void Insert(int index, KeyValuePair<string, string> header)
        {
            Contract.Assert(index >= 0 && index < Count);
            _collection.Insert(index, header);
        }

        public void RemoveAt(int index)
        {
            Contract.Assert(index >= 0 && index < Count);
            _collection.RemoveAt(index);
        }

        public int RemoveAll(Predicate<KeyValuePair<string,string>> predicate)
        {
            return _collection.RemoveAll(predicate);
        }

        public KeyValuePair<string, string> this[int index]
        {
            get
            {
                Contract.Assert(index >= 0 && index < Count);
                return _collection[index];
            }
            set
            {
                Contract.Assert(index >= 0 && index < Count);
                _collection[index] = value;
            }
        }
    }
}
