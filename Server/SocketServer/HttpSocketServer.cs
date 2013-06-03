using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Certificates;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using System.Net;
using System.Net.Sockets;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class HttpSocketServer : IDisposable
    {
        private const int HttpsPort = 8443;
        private const int HttpPort = 8080;
        private AppFunc _next;
        private int _port;

        private bool _disposed;
        private readonly SecurityOptions _options;
        private readonly  SecureTcpListener _server;
        private readonly string _certificateFilename = @"certificate.pfx";

        public HttpSocketServer(Func<IDictionary<string, object>, Task> next, IDictionary<string, object> properties)
        {
            _next = next;

            var addresses = (IList<IDictionary<string, object>>)properties[OwinConstants.CommonKeys.Addresses];

            var address = addresses.First();
            _port = Int32.Parse(address.Get<string>("port"));

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            switch (_port)
            {
                case HttpsPort:
                    _options = new SecurityOptions(SecureProtocol.Tls1, extensions, ConnectionEnd.Server);
                    break;
                default:
                    _options = new SecurityOptions(SecureProtocol.None, extensions, ConnectionEnd.Server);
                    return;
            }

            _options.VerificationType = CredentialVerification.None;
            _options.Certificate = Certificate.CreateFromCerFile(_certificateFilename);
            _options.Flags = SecurityFlags.Default;
            _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            _server = new SecureTcpListener(_port, _options);

            ThreadPool.SetMaxThreads(10,10);

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
                    var client = new HttpConnetingClient(this._server, _options, _next);
                    client.Accept();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled exception was caught: " + ex.Message);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _server.Stop();
        }
    }
}
