namespace Microsoft.Http2.Protocol.EventArgs
{
    /// <summary>
    /// This class is designed for future usage
    /// </summary>
    public class Http11ResourceDownloadedEventArgs : System.EventArgs
    {
        public int ByteCount { get; private set; }
        public string Name { get; private set; }

        public Http11ResourceDownloadedEventArgs(int byteCount, string name)
        {
            ByteCount = byteCount;
            Name = name;
        }
    }
}
