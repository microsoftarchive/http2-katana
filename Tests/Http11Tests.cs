using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using SharedProtocol;
using SharedProtocol.Http11;
using SocketServer;
using Xunit;
using Xunit.Extensions;
using SharedProtocol.Handshake;
using SharedProtocol.Exceptions;

namespace HandshakeTests
{
    public class Http11Tests
    {
        private HttpSocketServer _http2Server;

        private async Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            var handshakeAction = (Action)environment["HandshakeAction"];
            handshakeAction.Invoke();
        }

        public Http11Tests()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var secureAddress = appSettings["secureAddress"];
            Uri uri;
            Uri.TryCreate(secureAddress, UriKind.Absolute, out uri);

            var properties = new Dictionary<string, object>();
            var addresses = new List<IDictionary<string, object>>()
                {
                    new Dictionary<string, object>()
                        {
                            {"host", uri.Host},
                            {"scheme", uri.Scheme},
                            {"port", uri.Port.ToString()},
                            {"path", uri.AbsolutePath}
                        }
                };

            properties.Add(OwinConstants.CommonKeys.Addresses, addresses);

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootPath = @"\root";
            var serverRootDir = assemblyPath + rootPath;
            var serverSmallRootFile = serverRootDir + ConfigurationManager.AppSettings["smallTestFile"];
            var server10mbRootFile = serverRootDir + ConfigurationManager.AppSettings["10mbTestFile"];

            Directory.CreateDirectory(serverRootDir);

            var content = Encoding.UTF8.GetBytes("HelloWorld"); //10 bytes

            using (var stream = new FileStream(server10mbRootFile, FileMode.Create))
            {
                //Write 10 000 000 bytes or 10 mb
                for (int i = 0; i < 1000000; i++)
                {
                    stream.Write(content, 0, content.Length);
                }
            }

            using (var stream = new FileStream(serverSmallRootFile, FileMode.Create))
            {
                stream.Write(content, 0, content.Length);
            }

            new Thread((ThreadStart)delegate
                {
                    _http2Server = new HttpSocketServer(InvokeMiddleWare, properties);
                }).Start();

            using (var waitForServersStart = new ManualResetEvent(false))
            {
                waitForServersStart.WaitOne(3000);
            }
        }

        private static SecureSocket GetHandshakedSocket(Uri uri)
        {
            string selectedProtocol = null;

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            //Consciously fail the handshake to http/1.1
            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new [] {"http/1.1"}, ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);

            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };

                sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);
                HandshakeManager.GetHandshakeAction(sessionSocket, options).Invoke();
            }

            return sessionSocket;
        }

        [Fact]
        public void GetHttp11ResourceSuccessful()
        {
            string requestStr = ConfigurationManager.AppSettings["secureAddress"] + ConfigurationManager.AppSettings["10mbTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool wasServerSocketClosed = false;
            bool wasClientSocketClosed = false;
            bool wasResourceDownloaded = false;

            var socketClosedRaisedEvent = new ManualResetEvent(false);
            var resourceDownloadedRaisedEvent = new ManualResetEvent(false);
            SecureSocket socket = null;

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

            socket = GetHandshakedSocket(uri);

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
