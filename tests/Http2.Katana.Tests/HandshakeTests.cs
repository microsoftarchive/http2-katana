using System.Globalization;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Tests;
using Moq;
using Org.Mentalis;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HandshakeTests
{
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    public class HandshakeSetup : IDisposable
    {
        public HttpSocketServer Http2SecureServer { get; private set; }
        public HttpSocketServer Http2UnsecureServer { get; private set; }

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
                            {"port", uri.Port.ToString(CultureInfo.InvariantCulture)},
                            {"path", uri.AbsolutePath}
                        }
                };

            properties.Add("host.Addresses", addresses);

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

            Http2SecureServer = new HttpSocketServer(new Http2Middleware(TestHelpers.AppFunction).Invoke, secureProperties);
            Http2UnsecureServer = new HttpSocketServer(new Http2Middleware(TestHelpers.AppFunction).Invoke, unsecureProperties);
        }

        public void Dispose()
        {
            Http2SecureServer.Dispose();
            Http2UnsecureServer.Dispose();
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

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http2, Protocols.Http1 }, ConnectionEnd.Client)
            {
                VerificationType = CredentialVerification.None,
                Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx"),
                Flags = SecurityFlags.Default,
                AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION
            };

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);
            try
            {
                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };
                    sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);
                    sessionSocket.MakeSecureHandshake(options);
                }
            }
            finally
            {
                sessionSocket.Close();
                Assert.Equal(Protocols.Http2, selectedProtocol);
            }
        }

        [Fact]
        public void UpgradeHandshakeSuccessful()
        {
            const string address = "/";
            var duplexStream = TestHelpers.GetHandshakedDuplexStream(address, false);
            var requestString = "GET " + address + " HTTP/1.1\r\n" +
                                "Host: localhost\r\n" +
                                "Connection: Upgrade,HTTP2-Settings\r\n" +
                                "Upgrade: " + Protocols.Http2 + "\r\n" +
                                "HTTP2-Settings: \r\n" + // TODO send any valid parameters
                                "\r\n";

            duplexStream.Write(Encoding.UTF8.GetBytes(requestString));
            duplexStream.Flush();

            var rawHeaders = Http11Helper.ReadHeaders(duplexStream);
            Assert.Equal("HTTP/1.1 " + StatusCode.Code101SwitchingProtocols + " " + StatusCode.Reason101SwitchingProtocols, rawHeaders[0]);
            var headers = Http11Helper.ParseHeaders(rawHeaders.Skip(1));
            Assert.Contains("Connection", headers.Keys);
            Assert.Contains("Upgrade", headers["Connection"]);
            Assert.Contains("Upgrade", headers.Keys);
            Assert.Contains(Protocols.Http2, headers["Upgrade"]);
        }

        public void Dispose()
        {

        }
    }
}