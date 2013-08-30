using System;

namespace Microsoft.Http2.Protocol.Compression
{
    internal interface ICompressionProcessor : IDisposable
    {
        byte[] Compress(HeadersList headers);
        HeadersList Decompress(byte[] serializedBytes);
    }
}
