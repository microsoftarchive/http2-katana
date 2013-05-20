using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientHandler.Transport
{
    public interface IConnectionResolver
    {
        Task<Stream> ConnectAsync(string host, int port, CancellationToken cancel);
    }
}
