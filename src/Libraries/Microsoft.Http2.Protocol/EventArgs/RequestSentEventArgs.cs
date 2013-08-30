namespace Microsoft.Http2.Protocol.EventArgs
{
    public class RequestSentEventArgs : System.EventArgs
    {
        public Http2Stream Stream { get; private set; }

        public RequestSentEventArgs(Http2Stream stream)
        {
            Stream = stream;
        }
    }
}
