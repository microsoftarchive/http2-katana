// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Http2.TestClient
{
    internal enum CommandType : byte
    {
        None = 0,
        Connect = 1,
        Get = 2,
        Disconnect = 3,
        CaptureStatsOn = 4,
        CaptureStatsOff = 5,
        Dir = 6,
        Exit = 7,
        Help = 8,
        Unknown = 9,
        Empty = 10,
        Ping = 11,
        Post = 12,
        Put = 13,
        Delete = 14,
    }
}
