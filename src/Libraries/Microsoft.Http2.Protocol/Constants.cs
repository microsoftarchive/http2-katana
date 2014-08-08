// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class contains the most commonly used constants.
    /// </summary>
    public static class Constants
    {
        /* 14 -> 4.1 
        All frames begin with a fixed 9-octet header followed by a variable-
        length payload. */
        public const int FramePreambleSize = 9; // bytes
        public const int DefaultClientCertVectorSize = 8;
        /* 13 -> 4.2 
        The absolute maximum size of a frame payload is 2^14-1 (16,383) octets,
        meaning that the maximum frame size is 16,391 octets. */
        public const int MaxFramePayloadSize = 0x3fff; // 16383 bytes.
        public const int MaxFramePaddingSize = 300; // bytes
        public const int InitialFlowControlOptionsValue = 0;
        public const string DefaultMethod = Verbs.Get;
        public const string DefaultHost = "localhost";
        /* 13 -> 6.5.2 
        It is recommended that this value be no smaller than 100, so as to not
        unnecessarily limit parallelism. */
        public const int DefaultMaxConcurrentStreams = 100;
        /* 13 -> 6.9.1
        A sender MUST NOT allow a flow control window to exceed 2^31 - 1 bytes. */
        public const int MaxWindowSize = 0x7FFFFFFF;
        public const int MaxPriority = 0x7fffffff;

        /* 13 -> 6.9.2
        When a HTTP/2 connection is first established, new streams are
        created with an initial flow control window size of 65535 bytes.
        The connection flow control window is 65535 bytes. */
        public const int InitialFlowControlWindowSize = 0xFFFF;
        public const int DefaultStreamPriority = 1 << 30;

        /* 13 -> 5.3.5
        Streams are assigned a default dependency on stream 0x0.Pushed streams
        initially depend on their associated stream. In both cases, streams
        are assigned a default weight of 16. */
        public const int DefaultStreamDependency = 0;
        public const int DefaultStreamWeight = 16;
    }
}
