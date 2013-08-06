using System;
using System.Collections.Generic;

namespace SharedProtocol.Compression
{
    public interface ICompressionProcessor : IDisposable
    {
        byte[] Compress(IList<KeyValuePair<string, string>> headers);
        IList<KeyValuePair<string, string>> Decompress(byte[] serializedBytes);
    }
}
