using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using SharedProtocol.Http11;
using SocketServer;
using Xunit;
using SharedProtocol.Handshake;
using SharedProtocol;
using HandshakeAction = System.Func<System.Collections.Generic.IDictionary<string, object>>;

namespace Http11Tests
{
    public class Http11Setup : IDisposable
    {
        public Thread ServerThread { get; private set; }

        private static Task InvokeMiddleWare(IDictionary<string, object> environment)
        {

            bool wasHandshakeFinished = true;
            var handshakeTask = new Task<IDictionary<string, object>>(() => new Dictionary<string, object>());

            if (environment["HandshakeAction"] is HandshakeAction)
            {
                var handshakeAction = (HandshakeAction)environment["HandshakeAction"];
                handshakeTask = Task.Factory.StartNew(handshakeAction);

                if (!handshakeTask.Wait(6000))
                {
                    wasHandshakeFinished = false;
                }

                environment.Add("HandshakeResult", handshakeTask.Result);
            }

            environment.Add("WasHandshakeFinished", wasHandshakeFinished);

            return handshakeTask;
        }

        public Http11Setup()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var secureAddress = appSettings["secureAddress"];
            Uri uri;
            Uri.TryCreate(secureAddress, UriKind.Absolute, out uri);

            var properties = new Dictionary<string, object>();
            var addresses = new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object>
                        {
                            {"host", uri.Host},
                            {"scheme", uri.Scheme},
                            {"port", uri.Port.ToString(CultureInfo.InvariantCulture)},
                            {"path", uri.AbsolutePath}
                        }
                };

            properties.Add("use-handshake", true);
            properties.Add("use-priorities", true);
            properties.Add("use-flowControl", true);

            properties.Add(OwinConstants.CommonKeys.Addresses, addresses);

            ServerThread = new Thread((ThreadStart)delegate
            {
                new HttpSocketServer(InvokeMiddleWare, properties);
            }) { Name = "Http11ServerThread" };
            ServerThread.Start();

            using (var waitForServersStart = new ManualResetEvent(false))
            {
                waitForServersStart.WaitOne(3000);
            }
        }

        public void Dispose()
        {
            if (ServerThread.IsAlive)
            {
                ServerThread.Abort();
            }
        }
    }

    public class Http11TestSuite : IUseFixture<Http11Setup>, IDisposable
    {
        public void SetFixture(Http11Setup data)
        {
        }

        public void Dispose()
        {
        }

        private static SecureSocket GetHandshakedSocket(Uri uri)
        {
            string selectedProtocol = null;

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            //Consciously fail the handshake to http/1.1
            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http1 },
                                              ConnectionEnd.Client)
            {
                VerificationType = CredentialVerification.None,
                Certificate =
                    Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx"),
                Flags = SecurityFlags.Default,
                AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION
            };

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                 ProtocolType.Tcp, options);

            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };

                sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);

                var handshakeEnv = new Dictionary<string, object>
                    {
                        {":method", "get"},
                        {":version", Protocols.Http1},
                        {":path", uri.PathAndQuery},
                        {":scheme", uri.Scheme},
                        {":host", uri.Host},
                        {"securityOptions", options},
                        {"secureSocket", sessionSocket},
                        {"end", ConnectionEnd.Client}
                    };

                HandshakeManager.GetHandshakeAction(handshakeEnv).Invoke();
            }

            return sessionSocket;
        }

        [Fact]
        public void GetHttp11ResourceSuccessful()
        {
            string requestStr = ConfigurationManager.AppSettings["secureAddress"] +
                                ConfigurationManager.AppSettings["10mbTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool wasServerSocketClosed = false;
            bool wasClientSocketClosed = false;
            bool wasResourceDownloaded = false;

            var socketClosedRaisedEvent = new ManualResetEvent(false);
            var resourceDownloadedRaisedEvent = new ManualResetEvent(false);

            Http11Manager.OnDownloadSuccessful += (sender, args) =>
            {
                wasResourceDownloaded = true;
                resourceDownloadedRaisedEvent.Set();
            };

            //First time event will be raised when server closes it's socket
            Http11Manager.OnSocketClosed += (sender, args) =>
            {
                if (!wasServerSocketClosed)
                {
                    wasServerSocketClosed = true;
                }
                if (!wasClientSocketClosed && wasServerSocketClosed)
                {
                    wasClientSocketClosed = true;
                }
                socketClosedRaisedEvent.Set();
            };

            var socket = GetHandshakedSocket(uri);

            //Http11 was selected
            Http11Manager.Http11DownloadResource(socket, uri);

            resourceDownloadedRaisedEvent.WaitOne(60000);
            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasResourceDownloaded, true);
            Assert.Equal(wasClientSocketClosed, true);
            Assert.Equal(wasServerSocketClosed, true);
        }

        [Fact]
        public void GetHttp11MultipleResourcesSuccessful()
        {
            for (int i = 0; i < 10; i++)
            {
                GetHttp11ResourceSuccessful();
            }
        }
    }
}