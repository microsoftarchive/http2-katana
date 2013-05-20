using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Security.Certificates;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using System.Net;
using System.Globalization;
using System.Net.Sockets;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using System.Net.Security;
    using System.IO;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using ServerProtocol;
    using System.Threading;
    using Org.Mentalis;
    using Org.Mentalis.Security;

    public class Http2SocketServer : IDisposable
    {
        private AppFunc _next;
        private bool _enableSsl;
        private int _port;
        private bool _disposed;
        private SecurityOptions _options;
        private SecureTcpListener _server;
        private string certificateFilename = @"certificate.pfx";

        public Http2SocketServer(Func<IDictionary<string, object>, Task> next, IDictionary<string, object> properties)
        {
            _next = next;
            ExtensionType[] extensions = new ExtensionType[] { ExtensionType.Renegotiation, ExtensionType.ALPN };
            _options = new SecurityOptions(SecureProtocol.Tls1, extensions, ConnectionEnd.Server);

            _options.VerificationType = CredentialVerification.None;
            _options.Certificate = Certificate.CreateFromCerFile(certificateFilename);
            _options.Flags = SecurityFlags.Default;
            _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            IList<IDictionary<string, object>> addresses =
                (IList<IDictionary<string, object>>)properties[OwinConstants.CommonKeys.Addresses];

            IDictionary<string, object> address = addresses.First();
            _enableSsl = !string.Equals((address.Get<string>("scheme") ?? "http"), "http", StringComparison.OrdinalIgnoreCase);
            _port = Int32.Parse(address.Get<string>("port") ?? (_enableSsl ? "443" : "80"), CultureInfo.InvariantCulture);

            _server = new SecureTcpListener(_port, _options);

            Listen();
        }

        private void Listen()
        {
            Console.WriteLine("Started");
            _server.Start();

            while (!_disposed)
            {
                try
                {
                    var client = new Http2ConnetingClient(this._server, "\\", _next);
                    client.Accept();
                    Console.WriteLine("New client accepted");
                }
                catch (ProtocolViolationException)
                {
                    Console.WriteLine("Handshake failed!");
                }
                catch (SocketException)
                {
                    // Disconnect?
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                }
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _server.Stop();
        }
    }
}
