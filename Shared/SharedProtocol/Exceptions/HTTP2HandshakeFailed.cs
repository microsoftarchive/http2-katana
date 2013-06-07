using System;

namespace SharedProtocol.Exceptions
{
    public class Http2HandshakeFailed : Exception
    {
        public Http2HandshakeFailed()
            : base("Back to http11")
        {
            
        }
    }
}
