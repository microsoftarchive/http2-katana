// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol
{
    [Flags]
    // Normal state events on a HTTP/2.0 stream.
    // Using flags as these may happen in various orders due to race conditions.
    internal enum StreamState : ushort
    {
        Idle = 0x00,
        HalfClosedRemote = 0x01,
        HalfClosedLocal = 0x02,
        Reserved = 0x04,
        Opened = 0x08,
        Closed = 0x10,
    }
}
