// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// See draft 12 -> 7. Error Codes
    /// </summary>
    public enum ResetStatusCode : uint
    {
        None = 0,
        ProtocolError = 1,
        InternalError = 2,
        FlowControlError = 3,
        SettingsTimeout = 4,
        StreamClosed = 5,
        FrameSizeError = 6,
        RefusedStream = 7,
        Cancel = 8,
        CompressionError = 9,
        ConnectError = 10,
        EnhanceYourCalm = 11,
        InadequateSecurity = 12
    }
}
