// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenSSL;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This collection consists of http2 streams.
    /// </summary>
    internal class StreamDictionary : IDictionary<int, Http2Stream>
    {
        /// <summary>
        /// Collection enumerator class
        /// </summary>
        private class StreamDictionaryEnumerator : IEnumerator<KeyValuePair<int, Http2Stream>>
        {
            private readonly StreamDictionary _collection;
            private KeyValuePair<int, Http2Stream> _curPair;
            private Dictionary<int, Http2Stream>.Enumerator _enum;

            public StreamDictionaryEnumerator(StreamDictionary collection)
            {
                _collection = collection;
                _curPair = default(KeyValuePair<int, Http2Stream>);

                _enum = _collection.Streams.GetEnumerator();
            }

            public bool MoveNext()
            {
                if (_enum.MoveNext() == false)
                {
                    return false;
                }

                _curPair = _enum.Current;
                return true;
            }

            public void Reset()
            {
                _curPair = default(KeyValuePair<int, Http2Stream>);
            }

            void IDisposable.Dispose()
            {
                _enum.Dispose();
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

        public Dictionary<int, Http2Stream> Streams { get; private set; }

        public StreamDictionary()
        {
            Streams = new Dictionary<int, Http2Stream>();
        }

        public bool TryGetValue(int key, out Http2Stream value)
        {
            if (Streams.TryGetValue(key, out value))
            {
                return true;
            }

            return false;
        }

        public Http2Stream this[int key]
        {
            get
            {
                if (Streams.ContainsKey(key))
                {
                    return Streams[key];
                }

                return null;
            }
            set
            {
                
            }
        }

        public void Add(Http2Stream item)
        {
            Streams.Add(item.Id, item);
        }

        public void Add(int key, Http2Stream value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null");

            Add(value);
        }

        public void Add(KeyValuePair<int, Http2Stream> item)
        {
            if (item.Value == null)
                throw new ArgumentNullException("value is null");

            Add(item.Value);
        }

        public void Clear()
        {
            Streams.Clear();
        }

        public bool ContainsKey(int id)
        {
            return Streams.ContainsKey(id);
        }

        public bool Contains(KeyValuePair<int, Http2Stream> item)
        {
            if (item.Value == null)
                throw new ArgumentNullException("value is null");

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
                var keys = Streams.Keys.ToArray();
                var result = new int[keys.Length];

                Array.Copy(keys, 0, result, 0, keys.Length);

                return result;
            }
        }

        public ICollection<Http2Stream> Values
        {
            get
            {
                var values = Streams.Values.ToArray();
                var result = new Http2Stream[values.Length];

                Array.Copy(values, 0, result, 0, values.Length);

                return result;
            }
        }

        public int Count
        {
            get { return Streams.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<int, Http2Stream> item)
        {
            if (item.Value == null)
                throw new ArgumentNullException("value is null");

            return Remove(item.Key);
        }

        public bool Remove(int itemId)
        {
            if (Streams.ContainsKey(itemId))
            {
                return Streams.Remove(itemId);
            }

            return true; //Nothing to delete. Item was already deleted.
        }

        public bool Remove(Http2Stream item)
        {
            return item != null && Remove(item.Id);
        }

        public IEnumerator<KeyValuePair<int, Http2Stream>> GetEnumerator()
        {
            return new StreamDictionaryEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the streams opened by the specified endpoint.
        /// </summary>
        /// <param name="end">The endpoint.</param>
        /// <returns></returns>
        public int GetOpenedStreamsBy(ConnectionEnd end)
        {
            if (end == ConnectionEnd.Client)
            {
                return Streams.Count(element => element.Key%2 != 0);
            }

            return Streams.Count(element => element.Key%2 == 0);
        }
    }
}
