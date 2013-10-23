using System;

namespace Microsoft.Http2.Protocol.Framing
{
    internal interface IHeadersFrame
    {
        HeadersList Headers { get; set; }
        bool IsEndHeaders { get; set; }
        ArraySegment<byte> CompressedHeaders { get; }
    }
}
