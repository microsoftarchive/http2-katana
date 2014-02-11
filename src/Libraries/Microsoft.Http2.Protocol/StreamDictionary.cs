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
    /// This collection consists of two collection - flow controlled and nonflowcontrolled streams.
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
            private Dictionary<int, Http2Stream>.Enumerator _nonControlledEnum;
            private Dictionary<int, Http2Stream>.Enumerator _controlledEnum;

            public StreamDictionaryEnumerator(StreamDictionary collection)
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

        public StreamDictionary()
        {
            NonFlowControlledStreams = new Dictionary<int, Http2Stream>();
            FlowControlledStreams = new Dictionary<int, Http2Stream>();
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
                
            }
        }

        public void Add(Http2Stream item)
        {
            if (item.IsFlowControlEnabled)
            {
                FlowControlledStreams.Add(item.Id, item);
                return;
            }

            NonFlowControlledStreams.Add(item.Id, item);
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
            FlowControlledStreams.Clear();
            NonFlowControlledStreams.Clear();
        }

        public bool ContainsKey(int id)
        {
            return NonFlowControlledStreams.ContainsKey(id) || FlowControlledStreams.ContainsKey(id);
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
            if (item.Value == null)
                throw new ArgumentNullException("value is null");

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
            return true; //Nothing to delete. Item was already deleted.
        }

        public bool Remove(Http2Stream item)
        {
            return item != null && Remove(item.Id);
        }

        public bool IsStreamFlowControlled(Http2Stream stream)
        {
            return FlowControlledStreams.ContainsKey(stream.Id);
        }

        public void DisableFlowControl(Http2Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream is null");

            if (IsStreamFlowControlled(stream))
            {
                Remove(stream);
                stream.IsFlowControlEnabled = false;
                Add(stream);
            }
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
                return FlowControlledStreams.Count(element => element.Key % 2 != 0) +
                       NonFlowControlledStreams.Count(element => element.Key % 2 != 0);
            }

            return FlowControlledStreams.Count(element => element.Key % 2 == 0) +
                   NonFlowControlledStreams.Count(element => element.Key % 2 == 0);
        }
    }
}
