using SharedProtocol.Compression;

namespace SharedProtocol.Compression.Http2DeltaHeadersCompression
{
    public class Indexation : IAdditionalHeaderInfo
    {
        public IndexationType Type { get; private set; }

        public Indexation(IndexationType type)
        {
            Type = type;
        }
    }
}
