namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    public enum IndexationType : byte
    {
        Substitution = 0x00,
        Incremental = 0x40,
        WithoutIndexation = 0x60,
        Indexed = 0x80
    }
}
