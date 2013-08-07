using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharedProtocol.Compression.HeadersDeltaCompression
{
    internal class SizedHeadersList : HeadersList
    {
        public SizedHeadersList(){}

        public SizedHeadersList(HeadersList headers)
            : base(headers)
        {
        }

        public int StoredHeadersSize { get; private set; }
        public bool IsReadOnly { get { return true; } }

        public new void AddRange(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                Add(header);
            }
        }

        public new void Add(KeyValuePair<string, string> header)
        {
            base.Add(header);
            StoredHeadersSize += header.Key.Length + header.Value.Length;
        }

        public new bool Remove(KeyValuePair<string, string> header)
        {
            bool wasRemoved = base.Remove(header);
            if (wasRemoved)
            {
                StoredHeadersSize -= header.Key.Length + header.Value.Length;
            }
            return wasRemoved;
        }

        public new void RemoveAt(int index)
        {
            Contract.Assert(index >= 0 && index < Count);
            var header = this[index];
            base.RemoveAt(index);
            StoredHeadersSize -= header.Key.Length + header.Value.Length;
        }

        public new void Insert(int offset, KeyValuePair<string, string> header)
        {
            base.Insert(offset, header);
            StoredHeadersSize += header.Key.Length + header.Value.Length;
        }

        public new void Clear()
        {
            Clear();
            StoredHeadersSize = 0;
        }
    }
}
