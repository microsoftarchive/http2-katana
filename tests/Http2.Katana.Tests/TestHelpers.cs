using System.Configuration;
using Http2.TestClient.Handshake;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Owin;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.SSL;
using OpenSSL.X509;
using Xunit;

namespace Microsoft.Http2.Protocol.Tests
{
    public static class TestHelpers
    {
        public static readonly byte[] ClientSessionHeader = Encoding.UTF8.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
        public static readonly bool UseSecurePort = ConfigurationManager.AppSettings["useSecurePort"] == "true";

        private static readonly int SecurePort = int.Parse(ConfigurationManager.AppSettings["securePort"]);

        public static readonly string IndexFileName = "index.html";
        public static readonly string SimpleTestFileName = "simpleTest.txt";

        public static readonly string FileContent5bTest =
                                          new StreamReader(new FileStream(@"root\5mbTest.txt", FileMode.Open)).ReadToEnd(),
                                      FileContentSimpleTest =
                                          new StreamReader(new FileStream(@"root\"+SimpleTestFileName, FileMode.Open))
                                              .ReadToEnd(),
                                      FileContentIndex =
                                          new StreamReader(new FileStream(@"root\"+IndexFileName, FileMode.Open))
                                              .ReadToEnd(),
                                      FileContentEmptyFile = string.Empty,
                                      FileContentAnyFile = "some text";


        private static Dictionary<string, object> MakeHandshakeEnvironment(Uri uri, Stream stream)
        {
            return new Dictionary<string, object>
                {
                    {CommonHeaders.Path, uri.PathAndQuery},
                    {CommonHeaders.Version, Protocols.Http2},
                    {CommonHeaders.Scheme, Uri.UriSchemeHttp},
                    {CommonHeaders.Host, uri.Host},
                    {HandshakeKeys.Stream, stream},
                    {HandshakeKeys.ConnectionEnd, ConnectionEnd.Client}
                };
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static void MakeUnsecureHandshake(Uri uri, Stream stream)
        {
            var env = MakeHandshakeEnvironment(uri, stream);
            var result = new UpgradeHandshaker(env).Handshake();
            var success = result[HandshakeKeys.Successful] as string == HandshakeKeys.True;

            if (!success)
                throw new Http2HandshakeFailed(HandshakeFailureReason.InternalError);
        }

        public static X509Certificate LoadPKCS12Certificate(string certFilename, string password)
        {
            using (var certFile = BIO.File(certFilename, "r"))
            {
                return X509Certificate.FromPKCS12(certFile, password);
            }
        }

        public static Stream CreateStream(Uri uri, bool useMock = false)
        {
            var tcpClnt = new TcpClient(uri.Host, uri.Port);

            return tcpClnt.GetStream();
        }

        public static Http11ProtocolOwinAdapter CreateHttp11Adapter(Stream iostream, Func<IOwinContext, Task> appFunc)
        {
            if (iostream == null)
                throw new ArgumentNullException("stream is null");

            var headers = "GET / HTTP/1.1\r\n" +
                          "Host: localhost\r\n" +
                          "Connection: Upgrade\r\n" +
                          "Upgrade: " + Protocols.Http2 + "\r\n" +
                          "HTTP2-Settings: \r\n" + // TODO send any valid parameters
                          "\r\n";

            var requestBytes = Encoding.UTF8.GetBytes(headers);
            var mock = Mock.Get(iostream);
            int position = 0;

            var modifyBufferData = new Action<byte[], int, int>((buffer, offset, count) =>
                {
                    for (int i = offset; count > 0; --count, ++i)
                    {
                        buffer[i] = requestBytes[position];
                        ++position;
                    }
                });

            mock.Setup(stream => stream.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback(modifyBufferData)
                .Returns<byte[], int, int>((buffer, offset, count) => count); // read our requestBytes
            mock.Setup(stream => stream.CanRead).Returns(true);

            return new Http11ProtocolOwinAdapter(mock.Object, SslProtocols.Tls, appFunc);
        }


        public static Stream GetHandshakedStream(Uri uri, bool allowHttp2Communication = true, bool useMock = false)
        {
            var protocols = new List<string> {Protocols.Http1};
            if (allowHttp2Communication)
            {
                protocols.Add(Protocols.Http2);
            }
            var clientStream = CreateStream(uri);
            string selectedProtocol = Protocols.Http1;
            bool gotException = false;

            if (uri.Port == SecurePort)
            {
                clientStream = new SslStream(clientStream, false);

                (clientStream as SslStream).AuthenticateAsClient(uri.AbsoluteUri);

                selectedProtocol = (clientStream as SslStream).AlpnSelectedProtocol;
            }

            if (uri.Port != SecurePort || selectedProtocol == Protocols.Http1)
            {
                try
                {
                    MakeUnsecureHandshake(uri, clientStream);
                }
                catch (Exception ex)
                {
                    gotException = true;
                }
            }

            Assert.Equal(gotException, false);
            return useMock ? new Mock<Stream>(clientStream).Object : clientStream;
        }

        public static string GetAddress()
        {
            if (UseSecurePort)
            {
                return ConfigurationManager.AppSettings["secureAddress"];
            }

            return ConfigurationManager.AppSettings["unsecureAddress"];
        }
    }
}