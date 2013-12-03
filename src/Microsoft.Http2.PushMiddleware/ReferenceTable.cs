using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Http2.Push
{
    internal class ReferenceTable
    {
        private readonly object _modificationLock = new object();
        private Dictionary<string, string[]> _collection;

        public Dictionary<string, string[]>.KeyCollection Keys {
            get { return _collection.Keys; }
        }

        public string[] this[string index]
        {
            get { return _collection[index]; }
            set { _collection[index] = value; }
        }

        public ReferenceTable(IEnumerable<KeyValuePair<string, string[]>> initialCollection = null)
        {
            _collection = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            if (initialCollection == null)
                return;

            foreach (var keyValuePair in initialCollection)
            {
                _collection.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public ReferenceTable(ReferenceTable table)
        {
            _collection = new Dictionary<string, string[]>(table._collection);
        }

        public bool ContainsKey(string key)
        {
            return _collection.ContainsKey(key);
        }

        public void AddVertex(KeyValuePair<string, string[]> pair)
        {
            if (!ValidateNewChild(pair))
                return; //TODO throw something

            lock (_modificationLock)
            {
                _collection.Add(pair.Key, pair.Value);
            }
        }

        public void AddVertex(string key, string[] value)
        {
            if (!ValidateNewChild(key, value))
                return; //TODO throw something

            lock (_modificationLock)
            {
                _collection.Add(key, value);
            }
        }

        public void RemoveVertex()
        {
            //TODO impl bridge search
        }

        private bool ValidateNewChild(string key, string[] value)
        {
            return _collection.ContainsKey(key) && value.All(val => _collection.ContainsKey(val));
        }

        private bool ValidateNewChild(KeyValuePair<string, string[]> pair)
        {
            return _collection.ContainsKey(pair.Key) && pair.Value.All(val => _collection.ContainsKey(val));
        }
    }
}
