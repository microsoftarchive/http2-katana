using System;

namespace Server
{
    public class ServerStartedEventArgs : EventArgs
    {
        public int Port { get; private set; }

        public ServerStartedEventArgs(int port)
        {
            Port = port;
        }
    }
}
