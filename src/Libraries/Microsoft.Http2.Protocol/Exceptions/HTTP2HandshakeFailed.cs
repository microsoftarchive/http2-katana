using System;
using SharedProtocol.Handshake;

namespace SharedProtocol.Exceptions
{
    public class Http2HandshakeFailed : Exception
    {
        public HandshakeFailureReason Reason { get; private set; }

        public Http2HandshakeFailed(HandshakeFailureReason reason)
            : base(String.Format("Handshake failed with reason code {0}", reason))
        {
            Reason = reason;
        }
    }
}
