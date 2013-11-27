using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol
{
    //06
    //The semantics of HTTP header fields are not altered by this
    //specification, though header fields relating to connection management
    //or request framing are no longer necessary. An HTTP/2.0 request or
    //response MUST NOT include any of the following header fields:
    //Connection, Host, Keep-Alive, Proxy-Connection, TE, Transfer-
    //Encoding, and Upgrade. A server MUST treat the presence of any of
    //these header fields as a stream error (Section 5.4.2) of type
    //PROTOCOL_ERROR.

    public static class ForbiddenHeaders
    {
        public const string Connection = "Connection";
        public const string Host = "Host";
        public const string KeepAlive = "Keep-Alive";
        public const string ProxyConnection = "Proxy-Connection";
        public const string TE = "TE";
        public const string TransferEncoding = "Transfer-Encoding";
        public const string Upgrade = "Upgrade";

        public static bool HasForbiddenHeader(IEnumerable<KeyValuePair<string, string>> collection)
        {
            foreach (var header in collection)
            {
                if (header.Key.Equals(Connection, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(Host, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(KeepAlive, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(ProxyConnection, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(TE, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(TransferEncoding, StringComparison.OrdinalIgnoreCase)
                    || header.Key.Equals(Upgrade, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
