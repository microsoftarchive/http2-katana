// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// Headers list class.
    /// </summary>
    public class HeadersList : IList<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _collection;
        private readonly object _modificationLock = new object();
        /// <summary>
        /// Gets the size of the stored headers in bytes.
        /// </summary>
        /// <value>
        /// The size of the stored headers in bytes.
        /// </value>
        public int StoredHeadersSize { get; private set; }

        public HeadersList() 
            :this(16)
        { }

        public HeadersList(IEnumerable<KeyValuePair<string, string>> list)
        {
            _collection = new List<KeyValuePair<string, string>>();
            AddRange(list);
        }

        public HeadersList(IDictionary<string, string[]> headers)
        {
            _collection = new List<KeyValuePair<string, string>>();

            //Send only first value?
            foreach (var header in headers)
            {
                _collection.Add(new KeyValuePair<string, string>(header.Key.ToLower(), header.Value[0].ToLower()));
            }
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
            lock (_modificationLock)
            {
                foreach (var header in headers)
                {
                    Add(header);
                }
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


        /*The size of an entry is the sum of its name's length in bytes (as
       defined in Section 4.1.2), of its value's length in bytes
       (Section 4.1.3) and of 32 bytes.  The 32 bytes are an accounting for
       the entry structure overhead.  For example, an entry structure using
       two 64-bits pointers to reference the name and the value and the
       entry, and two 64-bits integer for counting the number of references
       to these name and value would use 32 bytes.*/
        public void Add(KeyValuePair<string, string> header)
        {
            lock (_modificationLock)
            {
                _collection.Add(header);
                StoredHeadersSize += header.Key.Length + header.Value.Length + 32;
            }
        }

        public void Clear()
        {
            lock (_modificationLock)
            {
                _collection.Clear();
                StoredHeadersSize = 0;
            }
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

        /*The size of an entry is the sum of its name's length in bytes (as
        defined in Section 4.1.2), of its value's length in bytes
        (Section 4.1.3) and of 32 bytes.  The 32 bytes are an accounting for
        the entry structure overhead.  For example, an entry structure using
        two 64-bits pointers to reference the name and the value and the
        entry, and two 64-bits integer for counting the number of references
        to these name and value would use 32 bytes.*/
        public bool Remove(KeyValuePair<string, string> header)
        {
            lock (_modificationLock)
            {
                StoredHeadersSize -= header.Key.Length + header.Value.Length + 32;
                return _collection.Remove(header);
            }
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

        /*The size of an entry is the sum of its name's length in bytes (as
        defined in Section 4.1.2), of its value's length in bytes
        (Section 4.1.3) and of 32 bytes.  The 32 bytes are an accounting for
        the entry structure overhead.  For example, an entry structure using
        two 64-bits pointers to reference the name and the value and the
        entry, and two 64-bits integer for counting the number of references
        to these name and value would use 32 bytes.*/
        public void Insert(int index, KeyValuePair<string, string> header)
        {
            lock (_modificationLock)
            {
                Contract.Assert(index >= 0 && (index == 0 || index < Count));
                StoredHeadersSize += header.Key.Length + header.Value.Length + 32;
                _collection.Insert(index, header);
            }
        }

        /*The size of an entry is the sum of its name's length in bytes (as
        defined in Section 4.1.2), of its value's length in bytes
        (Section 4.1.3) and of 32 bytes.  The 32 bytes are an accounting for
        the entry structure overhead.  For example, an entry structure using
        two 64-bits pointers to reference the name and the value and the
        entry, and two 64-bits integer for counting the number of references
        to these name and value would use 32 bytes.*/
        public void RemoveAt(int index)
        {
            lock (_modificationLock)
            {
                Contract.Assert(index >= 0 && index < Count);
                var header = _collection[index];
                _collection.RemoveAt(index);
                StoredHeadersSize -= header.Key.Length + header.Value.Length + 32;
            }
        }


        /*The size of an entry is the sum of its name's length in bytes (as
        defined in Section 4.1.2), of its value's length in bytes
        (Section 4.1.3) and of 32 bytes.  The 32 bytes are an accounting for
        the entry structure overhead.  For example, an entry structure using
        two 64-bits pointers to reference the name and the value and the
        entry, and two 64-bits integer for counting the number of references
        to these name and value would use 32 bytes.*/
        public int RemoveAll(Predicate<KeyValuePair<string,string>> predicate)
        {
            lock (_modificationLock)
            {

                var predMatch = _collection.FindAll(predicate);
                int toDeleteSize = predMatch.Sum(header => header.Key.Length + header.Value.Length + 32);
                StoredHeadersSize -= toDeleteSize;

                return _collection.RemoveAll(predicate);
            }
        }

        public bool ContainsName(string name)
        {
            return _collection.FindIndex(kv => kv.Key.Equals(name)) != -1;
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
                lock (_modificationLock)
                {
                    Contract.Assert(index >= 0 && index < Count);
                    _collection[index] = value;
                }
            }
        }
    }
}
