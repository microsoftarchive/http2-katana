using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    public enum UVarIntPrefix : byte
    {
        WithoutIndexing = 4,
        NeverIndexed = 4,
        EncodingContextUpdate = 4,
        Incremental = 6,
        Indexed = 7
    }
}
