using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;
using OpenSSL.Core;
using OpenSSL.X509;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IOwinContext, Task>;

    /// <summary>
    /// Http2 socket server implementation that uses raw socket.
    /// </summary>
    public class HttpSocketServer : IDisposable
    {
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private const string CertificateFilename = @"\server.pfx";

        private readonly Thread _listenThread;
        private readonly CancellationTokenSource _cancelAccept;
        private readonly AppFunc _next;
        private readonly int _port;
        private readonly string _scheme;
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private bool _disposed;
        private readonly TcpListener _server;
        private X509Certificate _serverCert;
        private bool _isSecure;

        private X509Certificate LoadPKCS12Certificate(string certFilename, string password)
        {
            using (var certFile = BIO.File(certFilename, "r"))
            {
                return X509Certificate.FromPKCS12(certFile, password);
            }
        }

        public HttpSocketServer(Func<IOwinContext, Task> next, IDictionary<string, object> properties)
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

            int securePort;

            if (!int.TryParse(ConfigurationManager.AppSettings["securePort"], out securePort))
            {
                Http2Logger.LogError("Incorrect port in the config file!");
                return;
            }

            try
            {
                _serverCert = LoadPKCS12Certificate(AssemblyName + CertificateFilename, "p@ssw0rd");
            }
            catch (Exception ex)
            {
                Http2Logger.LogInfo("Unable to start server. Check certificate. Exception: " + ex.Message);
                return;
            }
            

            _isSecure = _port == securePort;

            _server = new TcpListener(IPAddress.Any, _port);

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
                    var client = new HttpConnectingClient(_server, _next.Invoke, _serverCert, _isSecure, _useHandshake, _usePriorities, _useFlowControl);
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

            if (_cancelAccept != null)
            {
                _cancelAccept.Cancel();
                _cancelAccept.Dispose();
            }

            _disposed = true;
            if (_server != null)
            {
                _server.Stop();
            }

            if (_serverCert != null)
            {
                _serverCert.Dispose();
                _serverCert = null;
            }
        }
    }
}
