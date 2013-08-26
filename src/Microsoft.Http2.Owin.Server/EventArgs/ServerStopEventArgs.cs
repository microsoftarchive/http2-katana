using System;

namespace Server
{
    /// <summary>
    /// Server stop arguments
    /// </summary>
    public class ServerStopEventArgs : EventArgs
    {
        public int Port { get; private set; }

        public ServerStopEventArgs(int port)
        {
            Port = port;
        }
    }
}
