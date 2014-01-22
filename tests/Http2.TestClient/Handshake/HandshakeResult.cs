// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Http2.TestClient.Handshake
{
    public enum HandshakeResult
    {
        None,
        // Successful 101 Switching Protocols response
        Upgrade,
        // Some other status code, fall back to pre-HTTP/2.0 framing
        NonUpgrade,
        // We got back a HTTP/2.0 control frame (presumably a reset frame).
        // The server apparently only understands 2.0 on this port.
        UnexpectedControlFrame,
        // The underlying stream ended gracefully or abortively without completing the handshake.
        UnexpectedConnectionClose,
    }
}
