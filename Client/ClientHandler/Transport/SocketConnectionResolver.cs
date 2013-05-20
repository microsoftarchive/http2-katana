using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientHandler.Transport
{
    public class SocketConnectionResolver : IConnectionResolver
    {
        public SocketConnectionResolver()
        {
        }

        public async Task<Stream> ConnectAsync(string host, int port, CancellationToken cancel)
        {
            TcpClient client = new TcpClient(AddressFamily.InterNetworkV6);
            client.Client.DualMode = true;
            try
            {
                await client.ConnectAsync(host, port);
                return client.GetStream();
            }
            catch (SocketException)
            {
                client.Close();
                throw;
            }
        }
    }
}
