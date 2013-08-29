namespace SharedProtocol.Exceptions
{
    internal class DuplexStreamAlreadyClosedException : System.Exception
    {
        internal DuplexStreamAlreadyClosedException(string msg)
            : base(msg)
        {
            
        }
    }
}
