// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.Collections.Generic;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol
{
    internal class HeadersSequenceList
    {
        private List<HeadersSequence>  _collection;
        private object _modificationLock;

        internal HeadersSequenceList(IEnumerable<HeadersSequence> initialCollection = null)
        {
            _collection = initialCollection != null ? 
                            new List<HeadersSequence>(initialCollection) : 
                            new List<HeadersSequence>(64);

            _modificationLock = new object();
        }

        public HeadersSequence Find(int id)
        {
            return _collection.Find(seq => seq.StreamId == id);
        }

        public void Add(HeadersSequence item)
        {
            lock (_modificationLock)
            {
                _collection.Add(item);
            }
        }

        public void Remove(HeadersSequence item)
        {
            lock (_modificationLock)
            {
                _collection.Remove(item);
            }
        }
    }
}
