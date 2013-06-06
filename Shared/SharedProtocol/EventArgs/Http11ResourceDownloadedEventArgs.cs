using System;

namespace SharedProtocol
{
    public class Http11ResourceDownloadedEventArgs : EventArgs
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
