using System;

namespace Server
{
    public class ServerStoppedEventArgs : EventArgs
    {
        public int Port { get; private set; }

        public ServerStoppedEventArgs(int port)
        {
            Port = port;
        }
    }
}
