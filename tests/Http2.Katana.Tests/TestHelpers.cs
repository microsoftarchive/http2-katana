using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol.IO;
using Moq;
using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Http2.Protocol.Tests
{
    public static class TestHelpers
    {
        public static readonly string FileContent10MbTest = "some text";

        public static DuplexStream CreateStream()
        {
            SecurityOptions options = new SecurityOptions(SecureProtocol.Tls1, null, new[] { Protocols.Http1 }, ConnectionEnd.Client);
            SecureSocket socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, options);
            
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
                .Returns<byte[], int, int>((buffer, offset, count) => { return count; }); // read our requestBytes
            mock.Setup(stream => stream.CanRead).Returns(true);

            return new Http11ProtocolOwinAdapter(mock.Object, mock.Object.Socket.SecureProtocol, appFunc);
        }
    }
}
