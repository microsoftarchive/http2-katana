namespace Microsoft.Http2.Protocol.EventArgs
{
    internal class DataAvailableEventArgs : System.EventArgs
    {
        internal byte[] ReceivedBytes { get; private set; }

        internal DataAvailableEventArgs(byte[] receivedBytes)
        {
            ReceivedBytes = receivedBytes;
        }
    }
}
