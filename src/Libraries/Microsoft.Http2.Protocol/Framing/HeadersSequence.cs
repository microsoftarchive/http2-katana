// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using Microsoft.Http2.Protocol.Exceptions;

namespace Microsoft.Http2.Protocol.Framing
{
    internal class HeadersSequence
    {
        private readonly HeadersList headers = new HeadersList();
        private bool _wasFirstFrameReceived;
        public int StreamId { get; private set; }
        public bool IsComplete { get; private set; }
        public int Priority { get; set; }
        public bool WasEndStreamReceived { get; private set; }

        public HeadersList Headers
        {
            get { return headers; }
        }

        internal HeadersSequence(int streamId, IHeadersFrame initialFrame = null)
        {
            Priority = Constants.DefaultStreamPriority;
            StreamId = streamId;
            IsComplete = false;
            _wasFirstFrameReceived = false;
            WasEndStreamReceived = false;
            AddHeaders(initialFrame);
        }

        internal void AddHeaders(IHeadersFrame newFrame)
        {
            if (newFrame == null)
                return;

            if (!_wasFirstFrameReceived && !(newFrame is HeadersFrame) && !(newFrame is PushPromiseFrame))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Continuation was not precessed by the headers");

            _wasFirstFrameReceived = true;

            if (newFrame is IEndStreamFrame && !WasEndStreamReceived)
                WasEndStreamReceived = (newFrame as IEndStreamFrame).IsEndStream;

            headers.AddRange(newFrame.Headers);

            if ((newFrame is HeadersFrame && newFrame.IsEndHeaders)
                || (newFrame is ContinuationFrame && newFrame.IsEndHeaders)
                || newFrame is PushPromiseFrame && (newFrame as PushPromiseFrame).IsEndPushPromise)
            {
                IsComplete = true;
            }
        }
    }
}
