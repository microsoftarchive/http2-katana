// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class contains the most commonly used constants
    /// </summary>
    public static class Constants
    {
        // There are always at least 8 bytes in a control frame or data frame
        public const int FramePreambleSize = 8;
        public const int DefaultClientCertVectorSize = 8;
        public const int MaxFramePayloadSize = 0x3fff; // Spec 09 defines max frame size to be 16383 bytes.
        public const int MaxFramePaddingSize = 300; // in bytes
        public const int InitialFlowControlOptionsValue = 0;
        public const string DefaultMethod = Verbs.Get;
        public const string DefaultHost = "localhost";

        public const int DefaultMaxConcurrentStreams = 100;

        //09 -> 6.9.1.  The Flow Control Window
        //A sender MUST NOT allow a flow control window to exceed 2^31 - 1 bytes.
        public const int MaxWindowSize = 0x7FFFFFFF;
        public const int MaxPriority = 0x7fffffff;

        //09 -> 6.9.2.  Initial Flow Control Window Size
        //When a HTTP/2.0 connection is first established, new streams are
        //created with an initial flow control window size of 65535 bytes.  The
        //connection flow control window is 65535 bytes.  
        public const int InitialFlowControlWindowSize = 0xFFFF;
        public const int DefaultStreamPriority = 1 << 30;
    }
}
