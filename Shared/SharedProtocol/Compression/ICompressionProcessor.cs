using System;
using System.Collections.Generic;

namespace SharedProtocol.Compression
{
    public interface ICompressionProcessor : IDisposable
    {
        byte[] Compress(IList<Tuple<string, string, IAdditionalHeaderInfo> > headers, bool isRequest);
        List<Tuple<string, string, IAdditionalHeaderInfo> > Decompress(byte[] serializedBytes, bool isRequest);
    }
}
