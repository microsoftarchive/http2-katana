using Microsoft.Http1.Protocol;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Tests;
using Moq;
using Org.Mentalis;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using SocketServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using StatusCode = Microsoft.Http1.Protocol.StatusCode;

namespace HandshakeTests
{
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    public class HandshakeSetup : IDisposable
    {
        public HttpSocketServer Http2SecureServer { get; private set; }
        public HttpSocketServer Http2UnsecureServer { get; private set; }

        private async static Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            //bool wasHandshakeFinished = true;
            //var handshakeTask = new Task<IDictionary<string, object>>(() => new Dictionary<string, object>());

            //if (environment["HandshakeAction"] is HandshakeAction)
            //{
            // var handshakeAction = (HandshakeAction)environment["HandshakeAction"];
            // handshakeTask = Task.Factory.StartNew(handshakeAction);

            // if (!handshakeTask.Wait(6000))
            // {
            // wasHandshakeFinished = false;
            // }

            // environment.Add("HandshakeResult", handshakeTask.Result);
            //}

            //environment.Add("WasHandshakeFinished", wasHandshakeFinished);

            //return handshakeTask;
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

            Http2SecureServer = new HttpSocketServer(InvokeMiddleWare, secureProperties);
            Http2UnsecureServer = new HttpSocketServer(InvokeMiddleWare, unsecureProperties);
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

                    sessionSocket.MakeSecureHandshake(options);
                }
            }
            finally{
            sessionSocket.Close();
            Assert.Equal(Protocols.Http2, selectedProtocol);}
        }

        [Fact]
        public void UpgradeHandshakeSuccessful()
        {
            using (var stream = TestHelpers.CreateStream())
            {
                List<byte> written = new List<byte>();
                AssertException assertException = null;
                var writeHandler = new Action<byte[], int, int>((buffer, offset, count) => written.AddRange(buffer.Skip(offset).Take(count)));
                Mock.Get(stream).Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Callback(writeHandler);

                var adapter = TestHelpers.CreateHttp11Adapter(stream, new Func<IDictionary<string, object>, Task>(
                    async (env) =>
                    {
                        // we have access to environment so check it here
                        Assert.Contains("opaque.Upgrade", env.Keys);
                        Assert.True(env["opaque.Upgrade"] is UpgradeDelegate);

                        ((UpgradeDelegate)env["opaque.Upgrade"]).Invoke(new Dictionary<string, object>(), async opaque =>
                            {
                                Assert.Contains("opaque.Stream", opaque.Keys);
                                Assert.True(opaque["opaque.Stream"] is Stream);
                                Assert.Contains("opaque.Version", opaque.Keys);

                                // check headers were set correct
                                string headersString = Encoding.UTF8.GetString(written.ToArray());
                                written.Clear();
                                string[] rawHeaders = headersString.Split(new[] { "\r\n" }, StringSplitOptions.None);
                                var headers = Http11Manager.ParseHeaders(rawHeaders.Skip(1));
                                try
                                {
                                    Assert.Equal(
                                        "HTTP/1.1 " + StatusCode.Code101SwitchingProtocols + " " +
                                        StatusCode.Reason101SwitchingProtocols, rawHeaders[0]);
                                    Assert.Contains("Connection", headers.Keys);
                                    Assert.Contains("Upgrade", headers["Connection"]);
                                    //Assert.Contains("Upgrade", headers.Keys);
                                    //var protocolForUpgrade = headers["Upgrade"][0];
                                    //Assert.Contains("HTTP", protocolForUpgrade);
                                    //Assert.Contains("2.0", protocolForUpgrade);
                                }
                                catch (AssertException e)
                                {
                                    assertException = e;
                                    throw;
                                }
                            });
                    }
                ));
                adapter.ProcessRequest();

                // pass exception to xunit runtime
                if (assertException != null)
                {
                    throw assertException;
                }
            }
        }

        public void Dispose()
        {

        }
    }
}