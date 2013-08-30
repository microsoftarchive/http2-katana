namespace Microsoft.Http2.Protocol.Exceptions
{
    internal class DuplexStreamAlreadyClosedException : System.Exception
    {
        internal DuplexStreamAlreadyClosedException(string msg)
            : base(msg)
        {
            
        }
    }
}
