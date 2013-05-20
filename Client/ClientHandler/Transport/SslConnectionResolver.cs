using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ClientHandler.Transport
{
    public class SslConnectionResolver : ISecureConnectionResolver
    {
        private IConnectionResolver _connectionResolver;
        private SslProtocols _protocols = SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        public SslConnectionResolver(IConnectionResolver connectionResolver)
        {
            _connectionResolver = connectionResolver;
        }

        public Task<Stream> ConnectAsync(string host, int port, CancellationToken cancel)
        {
            return ConnectAsync(host, port, null, cancel);
        }

        public async Task<Stream> ConnectAsync(string host, int port, X509Certificate clientCert, CancellationToken cancel)
        {
            Stream lowerStream = null;
            SslStream sslStream = null;
            X509CertificateCollection certCollection = null;;
            if (clientCert != null)
            {
                certCollection = new X509CertificateCollection(new[] { clientCert });
            }
            try
            {
                lowerStream = await _connectionResolver.ConnectAsync(host, port, cancel);
                sslStream = new SslStream(lowerStream);
                await sslStream.AuthenticateAsClientAsync(host, certCollection, _protocols, checkCertificateRevocation: true);
                return sslStream;
            }
            catch (Exception)
            {
                if (sslStream != null)
                {
                    sslStream.Dispose();
                }
                if (lowerStream != null)
                {
                    lowerStream.Dispose();
                }
                throw;
            }
        }
    }
}
