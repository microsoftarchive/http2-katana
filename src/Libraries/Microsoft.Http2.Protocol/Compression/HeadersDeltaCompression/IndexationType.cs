// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    public enum IndexationType : byte
    {
        // See spec 07 -> 4.3.2.  Literal Header Field without Indexing
        // The literal header field without indexing representation starts with
        // the '0000' 4-bit pattern.
        WithoutIndexation = 0x00,    //07: Literal without indexation            | 0 | 0 | 0 | 0 | Index (4+)       |

        // See spec 07 -> 4.3.3.  Literal Header Field never Indexed
        // The literal header field never indexed representation starts with the
        // '0001' 4-bit pattern.
        NeverIndexed = 0x10,         //07: Literal Never Indexed                 | 0 | 0 | 0 | 1 | Index (4+)       |

        // See spec 07 -> 4.4.  Encoding Context Update
        // An encoding context update starts with the '001' 3-bit pattern.
        EncodingContextUpdate = 0x20, //07:Encoding Context Update                | 0 | 0 | 1 |    Index (4+)       |

        // See spec 07 -> 4.3.1.  Literal Header Field with Incremental Indexing
        // This representation starts with the '01' 2-bit pattern.
        Incremental = 0x40,          //07: Literal with incremental indexing     | 0 | 1 |         Index (6+)       |

        Indexed = 0x80               //05: Indexed                                | 1 |            Index (7+)       |
    }
}
