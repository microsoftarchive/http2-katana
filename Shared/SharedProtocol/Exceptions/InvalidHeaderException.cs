using System;
using SharedProtocol.Compression;

namespace SharedProtocol.Exceptions
{
    public class InvalidHeaderException : Exception
    {
        public Tuple<string, string, IAdditionalHeaderInfo> Header { get; private set; }

        public InvalidHeaderException(Tuple<string, string, IAdditionalHeaderInfo> header)
            :base("Incorrect header was provided for compression")
        {
            Header = header;
        }
    }
}
