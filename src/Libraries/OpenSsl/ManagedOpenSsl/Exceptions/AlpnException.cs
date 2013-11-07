using System;

namespace OpenSSL.Exceptions
{
    public class AlpnException : Exception
    {
        public AlpnException(string msg)
            : base(msg)
        {
        }
    }
}