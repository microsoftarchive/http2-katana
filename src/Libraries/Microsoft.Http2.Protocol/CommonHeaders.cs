// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol
{
    public static class CommonHeaders
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
        public const string ContentLength = "content-length";
    }
}
