namespace Microsoft.Http2.Protocol
{
    public class CommonHeaders
    {
        public const string Version = ":version";
        public const string Status = ":status";
        public const string Path = ":path";
        public const string Method = ":method";
        public const string MaxConcurrentStreams = ":max_concurrent_streams";
        public const string Scheme = ":scheme";
        public const string InitialWindowSize = ":initial_window_size";
        public const string Host = ":host";
        public const string Http2Settings = "Http2-Settings";
        public const string Connection = "Connection";
        public const string Upgrade = "Upgrade";
        public const string ContentLength = "Content-Length";
    }
}
