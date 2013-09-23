using System;

namespace Security.Ssl
{
    public class SecureHandshakeException : Exception
    {
        public SecureHandshakeFailureReason Reason { get; private set; }

        public SecureHandshakeException(SecureHandshakeFailureReason reason)
        {
            Reason = reason;
        }
    }
}
