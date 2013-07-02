using SharedProtocol.Compression;

namespace Http2HeadersCompression
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
