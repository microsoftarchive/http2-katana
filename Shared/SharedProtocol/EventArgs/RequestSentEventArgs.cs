using System;

namespace SharedProtocol
{
    public class RequestSentEventArgs : EventArgs
    {
        public Http2Stream Stream { get; private set; }

        public RequestSentEventArgs(Http2Stream stream)
        {
            Stream = stream;
        }
    }
}
