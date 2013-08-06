using System;
using System.Collections.Generic;
using SharedProtocol.Compression;

namespace SharedProtocol.Exceptions
{
    public class InvalidHeaderException : Exception
    {
        public KeyValuePair<string, string> Header { get; private set; }

        public InvalidHeaderException(KeyValuePair<string, string> header)
            :base("Incorrect header was provided for compression")
        {
            Header = header;
        }
    }
}
