using System;
using System.Collections.Generic;

namespace SharedProtocol.Compression
{
    public interface ICompressionProcessor : IDisposable
    {
        byte[] Compress(HeadersList headers);
        HeadersList Decompress(byte[] serializedBytes);
    }
}
