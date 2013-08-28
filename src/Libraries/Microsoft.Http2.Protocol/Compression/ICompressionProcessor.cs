using System;

namespace SharedProtocol.Compression
{
    internal interface ICompressionProcessor : IDisposable
    {
        byte[] Compress(HeadersList headers);
        HeadersList Decompress(byte[] serializedBytes);
    }
}
