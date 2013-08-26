namespace SharedProtocol.EventArgs
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
