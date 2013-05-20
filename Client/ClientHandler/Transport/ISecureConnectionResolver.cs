using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientHandler.Transport
{
    public interface ISecureConnectionResolver : IConnectionResolver
    {
        Task<Stream> ConnectAsync(string host, int port, X509Certificate clientCert, CancellationToken cancel);
    }
}
