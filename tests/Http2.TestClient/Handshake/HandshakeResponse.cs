using System;

namespace Http2.TestClient.Handshake
{
    public struct HandshakeResponse
    {
        // Response data up through the \r\n\r\n terminator
        public ArraySegment<byte> ResponseBytes;
        // Any data we accidently read past the terminator, pass on to the frame parser
        public ArraySegment<byte> ExtraData;
        public HandshakeResult Result;
    }
}
