using System;

namespace SharedProtocol.Exceptions
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
