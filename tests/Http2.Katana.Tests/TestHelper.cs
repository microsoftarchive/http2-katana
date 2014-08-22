using Http2.TestClient.Handshake;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Owin;
using Moq;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.SSL;
using OpenSSL.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Http2.Protocol.Tests
{
    public static class TestHelper
    {
        public static readonly byte[] ClientSessionHeader = Encoding.UTF8.GetBytes(Constants.ConnectionPreface);
        public static readonly bool UseSecurePort = ServerOptions.UseSecureAddress;

        private static readonly int SecurePort = ServerOptions.SecurePort;

        public static readonly string IndexFileName = "index.html";
        public static readonly string SimpleTestFileName = "simpleTest.txt";
        public static readonly string RootDir = "root";

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
                    {PseudoHeaders.Path, uri.PathAndQuery},
                    {PseudoHeaders.Scheme, Uri.UriSchemeHttp},
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


        public static Stream GetHandshakedStream(Uri uri, bool allowHttp2 = true, bool useMock = false)
        {
            var protocols = new List<string> {Protocols.Http1};
            if (allowHttp2)
            {
                protocols.Add(Protocols.Http2);
            }
            var clientStream = CreateStream(uri);
            string selectedProtocol = Protocols.Http1;
            bool gotException = false;

            if (uri.Port == SecurePort)
            {
                clientStream = new SslStream(clientStream, false, "localhost");

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

        /// <summary>
        /// Gets the address depending on useSecurePort parameter.
        /// </summary>
        /// <param name="useSecurePort"></param>
        /// <returns></returns>
        public static string GetAddress(bool useSecurePort)
        {
            return useSecurePort
                       ? FormatAddress(ServerOptions.SecureAddress)
                       : FormatAddress(ServerOptions.UnsecureAddress);
        }

        public static string Address
        {
            get
            {
                return FormatAddress(ServerOptions.Address);
            }
        }

        private static string FormatAddress(string address)
        {
            return String.Format("{0}{1}/", address, RootDir);
        }
    }
}