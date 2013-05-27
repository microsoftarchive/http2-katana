using System;

namespace SharedProtocol.Exceptions
{
    public class HTTP2HandshakeFailed : Exception
    {
        public HTTP2HandshakeFailed()
            : base("Back to http11")
        {
            
        }
    }
}
