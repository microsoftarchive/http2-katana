using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Http2.Push
{
    public class ReferenceTable
    {
        private readonly object _modificationLock = new object();
        private Dictionary<string, string[]> _collection;

        public Dictionary<string, string[]>.KeyCollection Keys {
            get { return _collection.Keys; }
        }

        public string[] this[string index]
        {
            get
            {
                if (!_collection.ContainsKey(index))
                    return null; //TODO Throw something

                return _collection[index];
            }
            set
            {
                if (!_collection.ContainsKey(index) || !value.All(val => _collection.ContainsKey(val)))
                    return; //TODO Throw something

                _collection[index] = value;
            }
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
            _collection = new Dictionary<string, string[]>(table._collection, StringComparer.OrdinalIgnoreCase);
        }

        public bool ContainsKey(string key)
        {
            return _collection.ContainsKey(key);
        }

        public void AddVertex(string key, string[] value)
        {
            lock (_modificationLock)
            {
                if (!ValidateNewChild(key, value))
                    return; //TODO throw something

                _collection.Add(key, value);
            }
        }

        public void RemoveVertex(string key)
        {
            lock (_modificationLock)
            {
                if (!_collection.ContainsKey(key)) 
                    return;
                
                _collection.Remove(key);

                var egdesToDel = _collection.Where(item => item.Value.Contains(key)).ToArray();

                for (int i = 0; i < egdesToDel.Length; i++)
                {
                    egdesToDel[i] = 
                        new KeyValuePair<string, string[]>
                            (egdesToDel[i].Key, egdesToDel[i].Value.Where(val => !val.Equals(key)).ToArray());
                }
            }
        }

        private bool ValidateNewChild(string key, string[] value)
        {
            return !_collection.ContainsKey(key) && value.All(val => _collection.ContainsKey(val));
        }
    }
}
