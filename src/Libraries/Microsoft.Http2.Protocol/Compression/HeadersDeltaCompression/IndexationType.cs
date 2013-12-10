namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    public enum IndexationType : byte
    {
        Incremental = 0x00,         //05: Literal with incremental indexing     | 0 | 0 |      Index (6+)       | 
        WithoutIndexation = 0x40,   //05: Literal without indexation            | 0 | 1 |      Index (6+)       |
        Indexed = 0x80              //05: Indexed                               | 1 |        Index (7+)         |
    }
}
