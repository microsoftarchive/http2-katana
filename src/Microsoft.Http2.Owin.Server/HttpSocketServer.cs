// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
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
using Owin.Types;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IOwinContext, Task>;

    /// <summary>
    /// Http2 socket server implementation that uses raw socket.
    /// </summary>
    public class HttpSocketServer : IDisposable
    {
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));

        private readonly Thread _listenThread;
        private readonly CancellationTokenSource _cancelAccept;
        private readonly AppFunc _next;
        private readonly int _port;
        private readonly bool _isDirectEnabled;
        private readonly string _serverName;
        private readonly string _certificateFilename;
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
            var addresses = (IList<IDictionary<string, object>>)properties[OwinConstants.CommonKeys.Addresses];

            var address = addresses.First();
            _port = Int32.Parse(address.Get<string>(Strings.Port));

            _cancelAccept = new CancellationTokenSource();

            _isDirectEnabled = (bool)properties[Strings.DirectEnabled];
            _serverName = (string)properties[Strings.ServerName];

            _certificateFilename = Strings.CertName;
            try
            {
                _serverCert = LoadPKCS12Certificate(AssemblyName + _certificateFilename, Strings.CertPassword);
            }
            catch (Exception ex)
            {
                Http2Logger.LogInfo("Unable to start server. Check certificate. Exception: " + ex.Message);
                return;
            }

            _isSecure = _port == ServerOptions.SecurePort;

            _server = new TcpListener(IPAddress.Any, _port);

            ThreadPool.SetMaxThreads(30, 10);

            _listenThread = new Thread(Listen);
            _listenThread.Start();
        }

        private void Listen()
        {
            Http2Logger.LogInfo("Server running at port " + _port);
            _server.Start();
            while (!_disposed)
            {
                try
                {
                    var client = new HttpConnectingClient(_server, _next.Invoke, _serverCert,
                                                            _serverName, _isSecure, _isDirectEnabled);
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
