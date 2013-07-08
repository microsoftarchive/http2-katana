using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Certificates;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using System.Configuration;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class HttpSocketServer : IDisposable
    {
        private readonly AppFunc _next;
        private readonly int _port;
        private readonly string _scheme;
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private bool _disposed;
        private readonly SecurityOptions _options;
        private readonly  SecureTcpListener _server;
        private const string _certificateFilename = @"certificate.pfx";

        public HttpSocketServer(Func<IDictionary<string, object>, Task> next, IDictionary<string, object> properties)
        {
            _next = next;

            var addresses = (IList<IDictionary<string, object>>)properties[OwinConstants.CommonKeys.Addresses];
            
            var address = addresses.First();
            _port = Int32.Parse(address.Get<string>("port"));
            _scheme = address.Get<string>("scheme");

            int securePort;

            try
            {
                securePort = int.Parse(ConfigurationManager.AppSettings["securePort"]);
            }
            catch (Exception)
            {
                Console.WriteLine("Incorrect port in the config file!");
               
                Console.WriteLine(ConfigurationManager.AppSettings["securePort"]);
                return;
            }

            if (_port == securePort
                &&
                _scheme == Uri.UriSchemeHttp
                ||
                _port != securePort
                &&
                _scheme == Uri.UriSchemeHttps)
            {
                Console.WriteLine("Invalid scheme on port! Use https for secure port");
                return;
            }

            _useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            _usePriorities = ConfigurationManager.AppSettings["prioritiesOptions"] == "use-priorities";
            _useFlowControl = ConfigurationManager.AppSettings["flowcontrolOptions"] != "no-flowcontrol";

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            _options = _port == securePort ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Server)
                                : new SecurityOptions(SecureProtocol.None, extensions, new [] { "http/2.0", "http/1.1" }, ConnectionEnd.Server);

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
            Console.WriteLine("Started on port {0}", _port);
            _server.Start();

            while (!_disposed)
            {
                try
                {
                    var client = new HttpConnetingClient(_server, _options, _next, _useHandshake, _usePriorities, _useFlowControl);
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
            if (_server != null)
            {
                _server.Stop();
            }
        }
    }
}
