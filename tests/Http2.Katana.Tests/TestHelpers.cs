using System.Configuration;
using System.Net;
using Http2.TestClient.Handshake;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Owin;
using Moq;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Org.Mentalis;

namespace Microsoft.Http2.Protocol.Tests
{
    public static class TestHelpers
    {
        public static readonly byte[] ClientSessionHeader = Encoding.UTF8.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
        private static readonly bool UseSecurePort = ConfigurationManager.AppSettings["useSecurePort"] == "true";

        public static readonly string FileContent5bTest = 
                                            new StreamReader(new FileStream(@"root\5mbTest.txt", FileMode.Open)).ReadToEnd(),
                                      FileContentSimpleTest = 
                                            new StreamReader(new FileStream(@"root\simpleTest.txt", FileMode.Open)).ReadToEnd(),
                                      FileContentEmptyFile = string.Empty,
                                      FileContentAnyFile = "some text";


        private static Dictionary<string, object> MakeHandshakeEnvironment(Uri uri, SecurityOptions options, DuplexStream stream)
        {
            return new Dictionary<string, object>
			{
                    {CommonHeaders.Path, uri.PathAndQuery},
					{CommonHeaders.Version, Protocols.Http2},
                    {CommonHeaders.Scheme, Uri.UriSchemeHttp},
                    {CommonHeaders.Host, uri.Host},
                    {HandshakeKeys.Options, options},
                    {HandshakeKeys.Stream, stream},
                    {HandshakeKeys.ConnectionEnd, ConnectionEnd.Client}
			};
        }

        private static void MakeUnsecureHandshake(Uri uri, SecurityOptions options, DuplexStream stream)
        {
            var env = MakeHandshakeEnvironment(uri, options, stream);
            var result = new UpgradeHandshaker(env).Handshake();
            var success = result[HandshakeKeys.Successful] as string == HandshakeKeys.True;

            if (!success)
                throw new Http2HandshakeFailed(HandshakeFailureReason.InternalError);
        }

        public static DuplexStream CreateStream()
        {
            var options = new SecurityOptions(SecureProtocol.Tls1, null, new[] { Protocols.Http1 }, ConnectionEnd.Client);
            var socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, options);
            
            return new Mock<DuplexStream>(socket, false).Object;
        }

        public static Http11ProtocolOwinAdapter CreateHttp11Adapter(DuplexStream duplexStream, Func<IDictionary<string, object>, Task> appFunc)
        {
            var headers = "GET / HTTP/1.1\r\n" +
                          "Host: localhost\r\n" +
                          "Connection: Upgrade\r\n" +
                          "Upgrade: " + Protocols.Http2 + "\r\n" +
                          "HTTP2-Settings: \r\n" + // TODO send any valid parameters
                          "\r\n";

            var requestBytes = Encoding.UTF8.GetBytes(headers);
            var mock = Mock.Get(duplexStream ?? CreateStream());
            int position = 0;

            var modifyBufferData = new Action<byte[], int, int>((buffer, offset, count) =>
            {
                for (int i = offset; count > 0; --count, ++i)
                {
                    buffer[i] = requestBytes[position];
                    ++position;
                }
            });

            mock.Setup(stream => stream.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Callback(modifyBufferData)
                .Returns<byte[], int, int>((buffer, offset, count) => count); // read our requestBytes
            mock.Setup(stream => stream.CanRead).Returns(true);

            return new Http11ProtocolOwinAdapter(mock.Object, SecureProtocol.Tls1, appFunc);
        }

        public async static Task AppFunction(IDictionary<string, object> environment)
        {
            // process response
            var owinResponse = new OwinResponse(environment) {ContentType = "text/plain"};
            var owinRequest = new OwinRequest(environment);
            var writer = new StreamWriter(owinResponse.Body);
            switch (owinRequest.Path)
            {
                case "/10mbTest.txt":
                    writer.Write(FileContent5bTest);
                    owinResponse.ContentLength = FileContent5bTest.Length;
                    break;
                case "/simpleTest.txt":
                    writer.Write(FileContentSimpleTest);
                    owinResponse.ContentLength = FileContentSimpleTest.Length;
                    break;
                case "/emptyFile.txt":
                    writer.Write(FileContentEmptyFile);
                    owinResponse.ContentLength = FileContentEmptyFile.Length;
                    break;
                default:
                    writer.Write(FileContentAnyFile);
                    owinResponse.ContentLength = FileContentAnyFile.Length;
                    break;
            }

            await writer.FlushAsync();

            
        }

        public static DuplexStream GetHandshakedDuplexStream(string address, bool allowHttp2Communication = true, bool useMock = false)
        {
            string selectedProtocol = null;

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };
            var useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";

            var protocols = new List<string> { Protocols.Http1 };
            if (allowHttp2Communication)
            {
                protocols.Add(Protocols.Http2);
            }

            var options = UseSecurePort
                              ? new SecurityOptions(SecureProtocol.Tls1, extensions, protocols,
                                                    ConnectionEnd.Client)
                              : new SecurityOptions(SecureProtocol.None, extensions, protocols,
                                                    ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION;

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);

            DuplexStream clientStream;
            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };

                var uri = new Uri(GetAddress() + address);

                sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);

                clientStream = new DuplexStream(sessionSocket, true);

                if (useHandshake)
                {
                    if (UseSecurePort)
                        sessionSocket.MakeSecureHandshake(options);
                    else
                        MakeUnsecureHandshake(uri, options, clientStream);
                }
            }

            return useMock ? new Mock<DuplexStream>(sessionSocket, true).Object : clientStream;
        }

        public static string GetAddress()
        {

            if (UseSecurePort)
            {
                return ConfigurationManager.AppSettings["secureAddress"];
            }

            return ConfigurationManager.AppSettings["unsecureAddress"];
        }

        public static TransportInformation GetTransportInformation()
        {
            return new TransportInformation();
        }
    }
}
