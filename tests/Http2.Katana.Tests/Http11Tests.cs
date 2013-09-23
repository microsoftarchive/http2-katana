using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;
using Moq;
using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Http2.Katana.Tests
{
    public class Http11Setup : IDisposable
    {
        public HttpSocketServer Server { get; private set; }
        public bool UseSecurePort { get; private set; }
        public bool UseHandshake { get; private set; }

        public Http11Setup()
        {
            var appSettings = ConfigurationManager.AppSettings;

            UseSecurePort = appSettings["useSecurePort"] == "true";
            UseHandshake = appSettings["handshakeOptions"] != "no-handshake";

            string address = UseSecurePort ? appSettings["secureAddress"] : appSettings["unsecureAddress"];

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

            bool useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            bool usePriorities = ConfigurationManager.AppSettings["prioritiesOptions"] != "no-priorities";
            bool useFlowControl = ConfigurationManager.AppSettings["flowcontrolOptions"] != "no-flowcontrol";

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

            Server = new HttpSocketServer(new Http2Middleware(TestHelpers.AppFunction).Invoke, properties);
        }

        public void Dispose()
        {
            Server.Dispose();
        }
    }

    public class Http11Tests : IUseFixture<Http11Setup>, IDisposable
    {
        public void SetFixture(Http11Setup setupInstance)
        {
            
        }

        public void Dispose()
        {
        }

        [StandardFact]
        public void EnvironmentCreatedCorrect()
        {
            var creator = typeof(Http11ProtocolOwinAdapter).GetMethod("CreateOwinEnvironment", BindingFlags.NonPublic | BindingFlags.Static);
            const string method = "GET",
                         scheme = "https",
                         host = "localhost:80",
                         pathBase = "",
                         path = "/test.txt",
                         queryString = "xunit";

            var requestHeaders = new Dictionary<string, string[]> { { "Host", new[] { host } } };
            var environment = (Dictionary<string, object>)creator.Invoke(null, new object[] { method, scheme, pathBase, path, requestHeaders, queryString, null });

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

        [StandardFact]
        public void OpaqueEnvironmentCreatedCorrect()
        {
            var adapter = TestHelpers.CreateHttp11Adapter(TestHelpers.GetHandshakedDuplexStream("/", false, true),
                e => new Task(() => { }));
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

        [StandardFact]
        public void HeadersParsedCorrect()
        {
            const string request = "GET / HTTP/1.1\r\n" +
                                   "Host: localhost:80\r\n" +
                                   "User-Agent: xunit\r\n" +
                                   "Connection: close\r\n" +
                                   "X-Multiple-Header: value1, value2\r\n" +
                                   "\r\n"; // five lines total

            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int position = 0;

            Mock<DuplexStream> mockStream = Mock.Get(TestHelpers.CreateStream());

            mockStream.Setup(stream =>
                stream.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())
            )
            .Callback<byte[], int, int>((buffer, offset, count) =>
                {
                    for (int i = offset; count > 0; --count, ++i)
                    {
                        buffer[i] = requestBytes[position];
                        ++position;
                    }
                })
            .Returns<byte[], int, int>((buf, offset, count) => count);

            var rawHeaders = Http11Helper.ReadHeaders(mockStream.Object);
            Assert.Equal(rawHeaders.Length, 5);

            string[] splittedRequestString = rawHeaders[0].Split(' ');
            Assert.Equal(splittedRequestString[0], "GET");
            Assert.Equal(splittedRequestString[1], "/");
            Assert.Equal(splittedRequestString[2], "HTTP/1.1");

            var headers = Http11Helper.ParseHeaders(rawHeaders.Skip(1).ToArray());
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

        [StandardFact]
        public void ResponseSentCorrect()
        {
            var headers = new Dictionary<string, string[]>
            {
                {"Connection", new[] {"close"}},
                {"Content-Type", new [] {"text/plain"}}
            };
            const string dataString = "test";
            byte[] data = Encoding.UTF8.GetBytes(dataString);

            var mock = Mock.Get(TestHelpers.CreateStream());
            var written = new List<byte>();

            mock.Setup(stream =>
                stream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())
            ).Callback<byte[], int, int>((buffer, offset, count) => written.AddRange((buffer.Skip(offset).Take(count))));

            Http11Helper.SendResponse(mock.Object, data, StatusCode.Code200Ok, headers);

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
            Assert.Contains(StatusCode.Code200Ok.ToString(CultureInfo.InvariantCulture), splittedResponse[0]);
            Assert.Contains(StatusCode.Reason200Ok, splittedResponse[0]);

            Assert.Contains("Connection: close", splittedResponse);
            Assert.Contains("Content-Type: " + "text/plain", splittedResponse);
            Assert.Contains("Content-Length: " + data.Length, splittedResponse);
            Assert.Contains(string.Empty, splittedResponse);

            int dataStart = Array.FindIndex(splittedResponse, s => s == string.Empty);
            string responseData = string.Join("\r\n", splittedResponse.Skip(dataStart + 1));
            Assert.Equal(dataString, responseData);
        }

        [StandardFact]
        public void ResponseWithExceptionHasNoBody()
        {
            var mock = Mock.Get(TestHelpers.CreateStream());

            var written = new List<byte>();
            mock.Setup(stream => stream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((buffer, offset, count) => written.AddRange((buffer.Skip(offset).Take(count))));

            var adapter = new Http11ProtocolOwinAdapter(mock.Object, SecureProtocol.Tls1, null);
            var endResponseMethod = adapter.GetType().GetMethod("EndResponse", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Exception) }, null);

            endResponseMethod.Invoke(adapter, new object[] { new Exception() });

            string[] response = Encoding.UTF8.GetString(written.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.InRange(response.Length, 3, int.MaxValue);
            Assert.Contains("HTTP/1.1", response[0]);
            Assert.Contains(StatusCode.Code500InternalServerError.ToString(CultureInfo.InvariantCulture), response[0]);
            Assert.Contains(StatusCode.Reason500InternalServerError, response[0]);
            Assert.Equal(string.Empty, response.Last());

            written.Clear();

            endResponseMethod.Invoke(adapter, new object[] { new NotSupportedException() });

            response = Encoding.UTF8.GetString(written.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.InRange(response.Length, 3, int.MaxValue);
            Assert.Contains("HTTP/1.1", response[0]);
            Assert.Contains(StatusCode.Code501NotImplemented.ToString(CultureInfo.InvariantCulture), response[0]);
            Assert.Contains(StatusCode.Reason501NotImplemented, response[0]);
            Assert.Equal(string.Empty, response.Last());
        }

        [StandardFact]
        public void Http11CommunicationSuccessful()
        {
            var address = ConfigurationManager.AppSettings["smallTestFile"];
            var duplexStream = TestHelpers.GetHandshakedDuplexStream(address, false);
            var requestString = "GET /" + address + " HTTP/1.1\r\n" +
                                "Host: localhost\r\n" +
                                "\r\n";

            duplexStream.Write(Encoding.UTF8.GetBytes(requestString));
            duplexStream.Flush();

            var rawHeaders = Http11Helper.ReadHeaders(duplexStream);
            Assert.True(rawHeaders.Length > 2); // response string, content-type and content-length headers at least
            Assert.Equal("HTTP/1.1 " + StatusCode.Code200Ok + " " + StatusCode.Reason200Ok, rawHeaders[0]);
            var headers = Http11Helper.ParseHeaders(rawHeaders.Skip(1));
            Assert.Contains("Content-Type", headers.Keys);
            Assert.Contains("Content-Length", headers.Keys);
            Assert.Equal(headers["Content-Length"][0], TestHelpers.FileContentSimpleTest.Length.ToString(CultureInfo.InvariantCulture));

            var size = int.Parse(headers["Content-Length"][0]);
            var responseBody = new byte[size];
            int read = int.MaxValue, total = 0;
            while (read > 0)
            {
                read = duplexStream.Read(responseBody, total, size - total);
                total += read;
            }

            duplexStream.Close();

            Assert.Equal(responseBody.Length, total);
            Assert.Equal(TestHelpers.FileContentSimpleTest, Encoding.UTF8.GetString(responseBody));

        }
    }
}