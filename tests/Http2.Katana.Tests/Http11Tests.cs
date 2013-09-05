using Microsoft.Http1.Protocol;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;
using Moq;
using ProtocolAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Xunit;

namespace Http11Tests
{
    using StatusCode = Microsoft.Http1.Protocol.StatusCode;
    public class Http11Tests : IDisposable
    {
        public void Dispose()
        {
        }
        private static List<byte> written = new List<byte>();

        private Action<byte[], int, int> WriteHandler
        {
            get
            {
                return new Action<byte[], int, int>((buffer, offset, count) =>
                    written.AddRange(buffer.Skip(offset).Take(count)));
            }
        }

        [Fact]
        public void EnvironmentCreatedCorrect()
        {
            var creator = typeof(Http11ProtocolOwinAdapter).GetMethod("CreateOwinEnvironment", BindingFlags.NonPublic | BindingFlags.Static);
            string method = "GET",
                   scheme = "https",
                   host = "localhost:80",
                   pathBase = "",
                   path = "/test.txt",
                   queryString = "xunit";

            var requestHeaders = new Dictionary<string, string[]> { { "Host", new[] { host } } };
            Dictionary<string, object> environment = (Dictionary<string, object>)creator.Invoke(null, new object[] { method, scheme, pathBase, path, requestHeaders, queryString, null });

            var owinRequest = new OwinRequest(environment);
            Assert.Equal(owinRequest.Method, method);
            Assert.Equal(owinRequest.Scheme, scheme);
            Assert.Equal(owinRequest.PathBase, pathBase);
            Assert.Equal(owinRequest.Path, path);
            Assert.Equal(owinRequest.Body.Length, 0);
            Assert.Equal(owinRequest.Headers.ContainsKey("Host"), true);
            Assert.Equal(owinRequest.Headers["Host"], host);
            Assert.Equal(owinRequest.QueryString, queryString);
            Assert.Equal(owinRequest.CallCancelled.IsCancellationRequested, false);

            var owinResponse = new OwinResponse(environment);
            Assert.Equal(owinResponse.Headers.Count, 0);
            Assert.Equal(owinResponse.Body.Length, 0);
            Assert.Equal(owinResponse.StatusCode, StatusCode.Code200Ok);
        }

        [Fact]
        public void OpaqueEnvironmentCreatedCorrect()
        {
            var adapter = TestHelpers.CreateHttp11Adapter(null, null);
            adapter.ProcessRequest();
            var env = adapter.GetType().InvokeMember("CreateOpaqueEnvironment",
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                null, adapter, null) as IDictionary<string, object>;

            Assert.NotNull(env);
            Assert.Contains("opaque.Stream", env.Keys);
            Assert.Contains("opaque.Version", env.Keys);
            Assert.Contains("opaque.CallCancelled", env.Keys);
            Assert.True(env["opaque.CallCancelled"] is CancellationToken);
        }

        [Fact]
        public void HeadersParsedCorrect()
        {

            string request = "GET / HTTP/1.1\r\n" +
                             "Host: localhost:80\r\n" +
                             "User-Agent: xunit\r\n" +
                             "Connection: close\r\n" +
                             "X-Multiple-Header: value1, value2\r\n" +
                             "\r\n"; // five lines total

            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int position = 0;

            Mock<DuplexStream> mockStream = Mock.Get(TestHelpers.CreateStream());

            var modifyBufferData = new Action<byte[], int, int>((buffer, offset, count) =>
                {
                    for (int i = offset; count > 0; --count, ++i)
                    {
                        buffer[i] = requestBytes[position];
                        ++position;
                    }
                });

            mockStream.Setup(stream =>
                stream.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())
            )
            .Callback(modifyBufferData)
            .Returns<byte[], int, int>((buf, offset, count) => { return count; });

            var rawHeaders = Http11Manager.ReadHeaders(mockStream.Object);
            Assert.Equal(rawHeaders.Length, 5);

            string[] splittedRequestString = rawHeaders[0].Split(' ');
            Assert.Equal(splittedRequestString[0], "GET");
            Assert.Equal(splittedRequestString[1], "/");
            Assert.Equal(splittedRequestString[2], "HTTP/1.1");

            var headers = Http11Manager.ParseHeaders(rawHeaders.Skip(1).ToArray());
            Assert.Equal(headers.Count, 4);
            Assert.Contains("Host", headers.Keys);
            Assert.Contains("User-Agent", headers.Keys);
            Assert.Contains("Connection", headers.Keys);
            Assert.Contains("X-Multiple-Header", headers.Keys);

            Assert.Equal(headers["Host"].Length, 1);
            Assert.Equal(headers["User-Agent"].Length, 1);
            Assert.Equal(headers["Connection"].Length, 1);
            Assert.Equal(headers["X-Multiple-Header"].Length, 2);

            Assert.Equal(headers["Host"][0], "localhost:80");
            Assert.Equal(headers["User-Agent"][0], "xunit");
            Assert.Equal(headers["Connection"][0], "close");
            Assert.Equal(headers["X-Multiple-Header"][0], "value1");
            Assert.Equal(headers["X-Multiple-Header"][1], "value2");
        }

        [Fact]
        public void ResponseSentCorrect()
        {
            Dictionary<string, string[]> headers = new Dictionary<string, string[]>();
            headers.Add("Connection", new[] { "close" });
            string dataString = "test";
            byte[] data = Encoding.UTF8.GetBytes(dataString);

            var mock = Mock.Get(TestHelpers.CreateStream());

            written.Clear();

            mock.Setup(stream =>
                stream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())
            ).Callback(WriteHandler);

            Http11Manager.SendResponse(mock.Object, data, StatusCode.Code200Ok, ContentTypes.TextPlain, headers);

            string response = Encoding.UTF8.GetString(written.ToArray());

            string[] splittedResponse = response.Split(new[] { "\r\n" }, StringSplitOptions.None);

            // let count number of items:
            // response string
            // Connection header
            // Content-Type header
            // Content-Length header
            // delimiter between headers and body - empty string
            // lines in response body
            Assert.Equal(5 + dataString.Split(new[] { "\r\n" }, StringSplitOptions.None).Length, splittedResponse.Length);
            Assert.Contains("HTTP/1.1", splittedResponse[0]);
            Assert.Contains(StatusCode.Code200Ok.ToString(), splittedResponse[0]);
            Assert.Contains(StatusCode.Reason200Ok, splittedResponse[0]);

            Assert.Contains("Connection: close", splittedResponse);
            Assert.Contains("Content-Type: " + ContentTypes.TextPlain, splittedResponse);
            Assert.Contains("Content-Length: " + data.Length, splittedResponse);
            Assert.Contains(string.Empty, splittedResponse);

            int dataStart = Array.FindIndex(splittedResponse, s => { return s == string.Empty; });
            string responseData = string.Join("\r\n", splittedResponse.Skip(dataStart + 1));
            Assert.Equal(dataString, responseData);
        }

        [Fact]
        public void ResponseWithExceptionHasNoBody()
        {
            var mock = Mock.Get(TestHelpers.CreateStream());
            mock.Setup(stream => stream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback(WriteHandler);

            written.Clear();

            Http11ProtocolOwinAdapter adapter = new Http11ProtocolOwinAdapter(mock.Object, mock.Object.Socket.SecureProtocol, null);
            var endResponseMethod = adapter.GetType().GetMethod("EndResponse", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Exception) }, null);

            endResponseMethod.Invoke(adapter, new object[] { new Exception() });

            string[] response = Encoding.UTF8.GetString(written.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.InRange(response.Length, 3, int.MaxValue);
            Assert.Contains("HTTP/1.1", response[0]);
            Assert.Contains(StatusCode.Code500InternalServerError.ToString(), response[0]);
            Assert.Contains(StatusCode.Reason500InternalServerError, response[0]);
            Assert.Equal(string.Empty, response.Last());

            written.Clear();

            endResponseMethod.Invoke(adapter, new object[] { new NotSupportedException() });

            response = Encoding.UTF8.GetString(written.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.InRange(response.Length, 3, int.MaxValue);
            Assert.Contains("HTTP/1.1", response[0]);
            Assert.Contains(StatusCode.Code501NotImplemented.ToString(), response[0]);
            Assert.Contains(StatusCode.Reason501NotImplemented, response[0]);
            Assert.Equal(string.Empty, response.Last());
        }
    }
}