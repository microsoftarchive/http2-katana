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
using SharedProtocol;

namespace HandshakeTests
{
    public class HandshakeSetup : IDisposable
    {
        public Thread Http2SecureServer { get; private set; }
        public Thread Http2UnsecureServer{ get; private set; }

        private Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            if (environment["HandshakeAction"] is Func<Task>)
            {
                var handshakeAction = (Func<Task>)environment["HandshakeAction"];
                return handshakeAction.Invoke();
                }
            return null;
        }

        private IDictionary<string, object> GetProperties(bool useSecurePort)
        {
            var appSettings = ConfigurationManager.AppSettings;

            string address = useSecurePort ? appSettings["secureAddress"] : appSettings["unsecureAddress"];

            Uri uri;
            Uri.TryCreate(address, UriKind.Absolute, out uri);

            var properties = new Dictionary<string, object>();
            var addresses = new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object>
                        {
                            {"host", uri.Host},
                            {"scheme", uri.Scheme},
                            {"port", uri.Port.ToString()},
                            {"path", uri.AbsolutePath}
                        }
                };

            properties.Add(OwinConstants.CommonKeys.Addresses, addresses);

            const bool useHandshake = true;
            const bool usePriorities = false;
            const bool useFlowControl = false;

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

            return properties;
        }

        public HandshakeSetup()
        {
           var secureProperties = GetProperties(true);
           var unsecureProperties = GetProperties(false);

           Http2SecureServer = new Thread((ThreadStart)delegate
           {
               new HttpSocketServer(InvokeMiddleWare, secureProperties);
           }) { Name = "Http2ServerThread" };


           Http2UnsecureServer = new Thread((ThreadStart)delegate
           {
               new HttpSocketServer(InvokeMiddleWare, unsecureProperties);
           }) { Name = "Http2UnsecureServer" };

           Http2SecureServer.Start();
           Http2UnsecureServer.Start();

           using (var waitForServersStart = new ManualResetEvent(false))
           {
               waitForServersStart.WaitOne(3000);
           }
        }

        public void Dispose()
        {
            if (Http2SecureServer.IsAlive)
            {
                Http2SecureServer.Abort();
            }
            if (Http2UnsecureServer.IsAlive)
            {
                Http2UnsecureServer.Abort();
            }
        }
    }
    public class HandshakeTests : IUseFixture<HandshakeSetup>, IDisposable
    {
        void IUseFixture<HandshakeSetup>.SetFixture(HandshakeSetup setupInstance)
        {
            
        }

        [Fact]
        public void AlpnSelectionHttp2Successful()
        {
            const string requestStr = @"https://localhost:8443/";
            string selectedProtocol = null;
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http2, Protocols.Http1 }, ConnectionEnd.Client)
                {
                    VerificationType = CredentialVerification.None,
                    Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx"),
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
                        {":version", "http/1.1"},
                        {":path", uri.PathAndQuery},
                        {":scheme", uri.Scheme},
                        {":host", uri.Host},
                        {"securityOptions", options},
                        {"secureSocket", sessionSocket},
                        {"end", ConnectionEnd.Client}
                    };
            
                HandshakeManager.GetHandshakeAction(handshakeEnv).Invoke();
            }

            sessionSocket.Close();
            Assert.Equal(Protocols.Http2, selectedProtocol);
        }

        [Fact]
        public void UpgradeHandshakeSuccessful()
        {
            const string requestStr = @"http://localhost:8080/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.None, extensions, new[] { Protocols.Http2, Protocols.Http1 }, ConnectionEnd.Client)
                {
                    VerificationType = CredentialVerification.None,
                    Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx"),
                    Flags = SecurityFlags.Default,
                    AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION
                };

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);

            sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port));

            var handshakeEnv = new Dictionary<string, object>
                {
                    {":method", "get"},
                    {":version", "http/1.1"},
                    {":path", uri.PathAndQuery},
                    {":scheme", uri.Scheme},
                    {":host", uri.Host},
                    {"securityOptions", options},
                    {"secureSocket", sessionSocket},
                    {"end", ConnectionEnd.Client}
                };

            bool gotFailedException = false;
            try
            {
                HandshakeManager.GetHandshakeAction(handshakeEnv).Invoke();
            }
            catch (Http2HandshakeFailed)
            {
                gotFailedException = true;
            }

            sessionSocket.Close();
            Assert.Equal(gotFailedException, false);
        }

        public void Dispose()
        {

        }
    }
}
