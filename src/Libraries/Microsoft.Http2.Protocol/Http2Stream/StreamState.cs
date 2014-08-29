// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;

namespace Microsoft.Http2.Protocol
{
    /* 14 -> 5.1 Stream States */
    public enum StreamState
    {
        [Description(Name = "idle")]
        Idle,

        [Description(Name = "half closed (remote)")]
        HalfClosedRemote,

        [Description(Name = "half closed (local)")]
        HalfClosedLocal,

        [Description(Name = "opened")]
        Opened,

        [Description(Name = "closed")]
        Closed,

        [Description(Name = "reserved (local)")]
        ReservedLocal,

        [Description(Name = "reserved (remote)")]
        ReservedRemote
    }

    internal class Description : Attribute
    {
        public string Name { get; set; }
    }
}
