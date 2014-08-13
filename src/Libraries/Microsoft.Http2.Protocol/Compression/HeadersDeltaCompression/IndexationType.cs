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
        // see 09 -> 7.2.2
        // The literal header field without indexing representation starts with
        // the '0000' 4-bit pattern.
        WithoutIndexation = 0x00,    //09: Literal without indexation            | 0 | 0 | 0 | 0 | Index (4+)       |

        // see 09 -> 7.2.3
        // The literal header field never indexed representation starts with the
        // '0001' 4-bit pattern.
        NeverIndexed = 0x10,         //09: Literal Never Indexed                 | 0 | 0 | 0 | 1 | Index (4+)       |

        // 09 -> 7.3
        // A header table size update starts with the '001' 3-bit pattern.
        HeaderTableSizeUpdate = 0x20, //09: Header Table Size Update             | 0 | 0 | 1 |    Index (4+)       |

        // see 09 -> 7.2.1
        // This representation starts with the '01' 2-bit pattern.
        Incremental = 0x40,          //09: Literal with incremental indexing     | 0 | 1 |         Index (6+)       |

        Indexed = 0x80               //09: Indexed                               | 1 |            Index (7+)       |
    }
}
