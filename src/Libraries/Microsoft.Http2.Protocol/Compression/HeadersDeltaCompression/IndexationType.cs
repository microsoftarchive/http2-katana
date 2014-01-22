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
        Incremental = 0x00,         //05: Literal with incremental indexing     | 0 | 0 |      Index (6+)       | 
        WithoutIndexation = 0x40,   //05: Literal without indexation            | 0 | 1 |      Index (6+)       |
        Indexed = 0x80              //05: Indexed                               | 1 |        Index (7+)         |
    }
}
