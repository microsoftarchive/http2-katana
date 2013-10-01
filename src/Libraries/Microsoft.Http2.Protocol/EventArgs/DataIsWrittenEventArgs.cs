namespace Microsoft.Http2.Protocol.EventArgs
{
    public class DataIsWrittenEventArgs : System.EventArgs
    {
        public int Count { get; private set; }

        public DataIsWrittenEventArgs(int count)
        {
            Count = count;
        }
    }
}
