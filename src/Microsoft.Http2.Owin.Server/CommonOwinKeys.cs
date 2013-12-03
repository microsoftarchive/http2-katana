namespace Owin
{
    public static class CommonOwinKeys
    {
        public const string RequestHeaders = "owin.RequestHeaders";
        public const string ResponseHeaders = "owin.ResponseHeaders";

        public const string OwinCallCancelled = "owin.CallCancelled";
        public const string OwinVersion = "1.0";

        public const string OpaqueUpgrade = "opaque.Upgrade";
        public const string OpaqueStream = "opaque.Stream";
        public const string OpaqueVersion = "opaque.Version";
        public const string OpaqueCallCancelled = "opaque.CallCancelled";

        public const string ServerPushFunc = "server.push";
        public const string RefTable = "reference.table";
    }
}
