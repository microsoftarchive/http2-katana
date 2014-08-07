// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    public enum UVarIntPrefix : byte
    {
        WithoutIndexing = 4,
        NeverIndexed = 4,

        /* 09 -> 7.3
        A header table size update starts with the '001' 3-bit pattern,
        followed by the new maximum size, represented as an integer with a
        5-bit prefix.
        */
        HeaderTableSizeUpdate = 5,

        Incremental = 6,
        Indexed = 7
    }
}
