using System;

namespace SharedProtocol
{
    public class StreamClosedEventArgs : EventArgs
    {
        public int Id { get; private set; }

        public StreamClosedEventArgs(int id)
        {
            this.Id = id;
        }
    }
}
