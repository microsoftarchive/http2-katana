using Microsoft.Http2.Protocol.Exceptions;

namespace Microsoft.Http2.Protocol.Framing
{
    internal class HeadersSequence
    {
        private readonly HeadersList headers = new HeadersList();
        private bool _wasFirstFrameReceived;
        public int StreamId { get; private set; }
        public bool IsComplete { get; private set; }

        public HeadersList Headers
        {
            get { return headers; }
        }

        internal HeadersSequence(int streamId, IHeadersFrame initialFrame = null)
        {
            StreamId = streamId;
            IsComplete = false;
            _wasFirstFrameReceived = false;
            AddHeaders(initialFrame);
        }

        internal void AddHeaders(IHeadersFrame newFrame)
        {
            if (newFrame == null)
                return;

            if (!_wasFirstFrameReceived && !(newFrame is HeadersFrame))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Continuation was not precessed by the headers");

            _wasFirstFrameReceived = true;

            headers.AddRange(newFrame.Headers);

            if (newFrame.IsEndHeaders)
                IsComplete = true;
        }
    }
}
