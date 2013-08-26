using System;

namespace Server
{
    /// <summary>
    /// Server start arguments
    /// </summary>
    public class ServerStartEventArgs : EventArgs
    {
        public int Port { get; private set; }

        public ServerStartEventArgs(int port)
        {
            Port = port;
        }
    }
}
