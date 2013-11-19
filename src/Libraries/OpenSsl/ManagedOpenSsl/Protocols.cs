namespace OpenSsl.Protocols
{
    //TODO move it from the http2 proto
    /// <summary>
    /// Enum of supported protocols.
    /// </summary>
    public static class Protocols
    {
        public static string Http2 = "HTTP-draft-06/2.0";
        public static string Http1 = "http/1.1";
        public static string Http204 = "HTTP-draft-04/2.0"; //For chrome. It does not support 06 version and alpn fails to 11
    }
}
