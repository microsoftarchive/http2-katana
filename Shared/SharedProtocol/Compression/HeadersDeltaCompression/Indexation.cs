using SharedProtocol.Compression;

namespace SharedProtocol.Http2HeadersCompression
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
