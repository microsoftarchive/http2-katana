using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Server;
using ServerOwinMiddleware;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.Handshake;
using Xunit;
using Xunit.Extensions;
using Microsoft.Owin.Hosting;

namespace HandshakeTests
{
    public class HandshakeTests
    {
        private readonly Thread _secureServerThread;
        private readonly Thread _unsecureServerThread;

        public HandshakeTests()
        {
            //Secure Server
            _secureServerThread = new Thread((ThreadStart)delegate
                {
                    using (WebApplication.Start<Startup>(options =>
                    {
                        options.Url = "http://localhost:8443/";
                        options.Server = "SocketServer";
                    }))
                    {
                    }
                });
            _secureServerThread.Start();


            //Unsecure server
            _unsecureServerThread = new Thread((ThreadStart)delegate
            {
                using (WebApplication.Start<Startup>(options =>
                {
                    options.Url = "http://localhost:8080/";
                    options.Server = "SocketServer";
                }))
                {
                }
            });

            _unsecureServerThread.Start();

             using (var waitForServersStart = new ManualResetEvent(false))
             {
                 waitForServersStart.WaitOne(5000);
             }
        }

        [Fact]
        public void AlpnSelectionHttp2Successful()
        {
            string requestStr = @"http://localhost:8443/";
            string selectedProtocol = null;
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            SecureSocket sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
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
            string requestStr = @"http://localhost:8080/";
            string selectedProtocol = null;
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.None, extensions, ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            SecureSocket sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
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
            _unsecureServerThread.Abort();
            _secureServerThread.Abort();
        }
    }
}
