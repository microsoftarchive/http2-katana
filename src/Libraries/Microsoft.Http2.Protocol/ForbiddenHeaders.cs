// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
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
