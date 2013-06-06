using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using SharedProtocol.Exceptions;
using SharedProtocol.Handshake;
using SocketServer;
using Xunit;

namespace HandshakeTests
{
    public class HandshakeTests
    {
        private HttpSocketServer _http2SecureServer;
        private HttpSocketServer _http2UnsecureServer;

        private async Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            var handshakeAction = (Action)environment["HandshakeAction"];
            handshakeAction.Invoke();
        }

        private HttpSocketServer RunServer(string address)
        {
            Uri uri;
            Uri.TryCreate(address, UriKind.Absolute, out uri);

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

            HttpSocketServer server = null;

            new Thread((ThreadStart)delegate
            {
                server = new HttpSocketServer(InvokeMiddleWare, properties);
            }).Start();

            return server;
        }

        public HandshakeTests()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var secureAddress = appSettings["secureAddress"];
            var unsecureAddress = appSettings["unsecureAddress"];

            _http2UnsecureServer = RunServer(unsecureAddress);
            _http2SecureServer = RunServer(secureAddress);

            using (var waitForServersStart = new ManualResetEvent(false))
            {
                waitForServersStart.WaitOne(3000);
            }
        }

        [Fact]
        public void AlpnSelectionHttp2Successful()
        {
            const string requestStr = @"http://localhost:8443/";
            string selectedProtocol = null;
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Client);

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

            sessionSocket.Close();
            Assert.Equal("http/2.0", selectedProtocol);
        }

        [Fact]
        public void UpgradeHandshakeSuccessful()
        {
            const string requestStr = @"http://localhost:8080/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.None, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);

            sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port));

            bool gotFailedException = false;
            try
            {
                HandshakeManager.GetHandshakeAction(sessionSocket, options).Invoke();
            }
            catch (HTTP2HandshakeFailed)
            {
                gotFailedException = true;
            }

            sessionSocket.Close();
            Assert.Equal(gotFailedException, false);
        }

        ~HandshakeTests()
        {
            _http2UnsecureServer.Dispose();
            _http2SecureServer.Dispose();
        }
    }
}
