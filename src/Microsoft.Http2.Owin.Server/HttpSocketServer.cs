using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly TcpListener _server;

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
                    var client = new HttpConnectingClient(_server, _next, _useHandshake, _usePriorities, _useFlowControl);
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
        }
    }
}
