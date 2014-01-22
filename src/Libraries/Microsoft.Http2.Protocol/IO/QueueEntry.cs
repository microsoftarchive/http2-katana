// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.IO
{
    internal class QueueEntry : IQueueItem
    {
        private readonly Frame _frame;
      
        public QueueEntry(Frame frame)
        {
            _frame = frame;
        }

        public bool IsFlush { get { return _frame == null; } }

        public Frame Frame { get { return _frame; } }

        public byte[] Buffer { get { return (_frame != null) ? _frame.Buffer : null; } }
    }
}
