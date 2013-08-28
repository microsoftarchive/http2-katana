namespace SharedProtocol.EventArgs
{
    internal class RequestSentEventArgs : System.EventArgs
    {
        public Http2Stream Stream { get; private set; }

        public RequestSentEventArgs(Http2Stream stream)
        {
            Stream = stream;
        }
    }
}
