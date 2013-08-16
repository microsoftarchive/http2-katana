namespace SharedProtocol.EventArgs
{
    public class StreamClosedEventArgs : System.EventArgs
    {
        public int Id { get; private set; }

        public StreamClosedEventArgs(int id)
        {
            Id = id;
        }
    }
}
