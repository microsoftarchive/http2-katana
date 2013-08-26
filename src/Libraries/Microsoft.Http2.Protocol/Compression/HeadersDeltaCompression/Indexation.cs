namespace SharedProtocol.Compression.HeadersDeltaCompression
{
    public class Indexation : IAdditionalHeaderInfo
    {
        /// <summary>
        /// Indexation is additional info used in headers delta compression algorithm.
        /// See latest compression spec in https://github.com/http2/compression-spec
        /// </summary>
        public IndexationType Type { get; private set; }

        public Indexation(IndexationType type)
        {
            Type = type;
        }
    }
}
