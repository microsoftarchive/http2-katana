// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
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
            }
        }

        private bool ValidateNewChild(string key, string[] value)
        {
            return !_collection.ContainsKey(key) && value.All(val => _collection.ContainsKey(val));
        }
    }
}
