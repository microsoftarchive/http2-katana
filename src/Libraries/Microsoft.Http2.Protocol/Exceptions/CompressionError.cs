using System;

namespace Microsoft.Http2.Protocol.Exceptions
{
    public class CompressionError : Exception
    {
        public CompressionError(Exception e): base("", e)
        {
        }

        public override string Message
        {
            get
            {
                return InnerException != null ? InnerException.Message : base.Message;
            }
        }
    }
}
