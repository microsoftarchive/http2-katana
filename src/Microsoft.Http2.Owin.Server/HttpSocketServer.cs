using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Certificates;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Http2 socket server implementation that uses raw socket.
    /// </summary>
    public class HttpSocketServer : IDisposable
    {
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private const string CertificateFilename = @"\certificate.pfx";

        private readonly Thread _listenThread;
        private readonly CancellationTokenSource _cancelAccept;
        private readonly AppFunc _next;
        private readonly int _port;
        private readonly string _scheme;
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private bool _disposed;
        private readonly SecurityOptions _options;
        private readonly SecureTcpListener _server;

        public HttpSocketServer(Func<IDictionary<string, object>, Task> next, IDictionary<string, object> properties)
        {
            _next = next;
            var addresses = (IList<IDictionary<string, object>>)properties["host.Addresses"];

            var address = addresses.First();
            _port = Int32.Parse(address.Get<string>("port"));
            _scheme = address.Get<string>("scheme");

            _cancelAccept = new CancellationTokenSource();

            _useHandshake = (bool)properties["use-handshake"];
            _usePriorities = (bool)properties["use-priorities"];
            _useFlowControl = (bool)properties["use-flowControl"];

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            // protocols should be in order of their priority
            _options = _scheme == Uri.UriSchemeHttps ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http2, Protocols.Http204, Protocols.Http1 }, ConnectionEnd.Server)
                                : new SecurityOptions(SecureProtocol.None, extensions, new[] { Protocols.Http2, Protocols.Http1 }, ConnectionEnd.Server);

            _options.VerificationType = CredentialVerification.None;
            _options.Certificate = Certificate.CreateFromCerFile(AssemblyName + CertificateFilename);
            _options.Flags = SecurityFlags.Default;
            _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION;

            _server = new SecureTcpListener(_port, _options);

            ThreadPool.SetMaxThreads(30, 10);

            _listenThread = new Thread(Listen);
            _listenThread.Start();
        }

        private void Listen()
        {
            Http2Logger.LogInfo("Started on port " + _port);
            _server.Start();
            while (!_disposed)
            {
                try
                {
                    var client = new HttpConnectingClient(_server, _options, _next, _useHandshake, _usePriorities, _useFlowControl);
                    client.Accept(_cancelAccept.Token);
                }
                catch (Exception ex)
                {
                    Http2Logger.LogError("Unhandled exception was caught: " + ex.Message);
                }
            }

            Http2Logger.LogDebug("Listen thread was finished");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cancelAccept.Cancel();
            _cancelAccept.Dispose();

            _disposed = true;
            if (_server != null)
            {
                _server.Stop();
            }
        }
    }
}
