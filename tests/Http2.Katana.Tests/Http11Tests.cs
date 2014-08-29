using System.IO;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;
using Moq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenSSL.SSL;
using Xunit;

namespace Http2.Katana.Tests
{
    public class Http11Setup : IDisposable
    {
        public HttpSocketServer Server { get; private set; }

        public static Uri Uri;

        public Http11Setup()
        {
            string address = TestHelper.Address;

            Uri uri;
            Uri.TryCreate(address, UriKind.Absolute, out uri);

            Uri = uri;

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
            properties.Add(Owin.Types.OwinConstants.CommonKeys.Addresses, addresses);

            bool isDirectEnabled = ServerOptions.IsDirectEnabled;
            properties.Add(Strings.DirectEnabled, isDirectEnabled);

            string serverName = ServerOptions.ServerName;
            properties.Add(Strings.ServerName, serverName);

            Server = new HttpSocketServer(new Http2Middleware(new ResponseMiddleware(null)).Invoke, properties);
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
        public void CreateEnvironment()
        {
            var creator = typeof(Http11OwinMessageHandler).GetMethod("CreateOwinEnvironment", BindingFlags.NonPublic | BindingFlags.Static);
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
            Assert.Equal(owinRequest.PathBase.Value, pathBase);
            Assert.Equal(owinRequest.Path.Value, path);
            Assert.Equal(owinRequest.Body.Length, 0);
            Assert.Equal(owinRequest.Headers.ContainsKey("Host"), true);
            Assert.Equal(owinRequest.Headers["Host"], host);
            Assert.Equal(owinRequest.QueryString.Value, queryString);
            Assert.Equal(owinRequest.CallCancelled.IsCancellationRequested, false);

            var owinResponse = new OwinResponse(environment);
            Assert.Equal(owinResponse.Headers.Count, 0);
            Assert.Equal(owinResponse.Body.Length, 0);
            Assert.Equal(owinResponse.StatusCode, StatusCode.Code200Ok);
        }

        [StandardFact]
        public void HeadersParsing()
        {
            const string request = "GET / HTTP/1.1\r\n" +
                                   "Host: localhost:80\r\n" +
                                   "User-Agent: xunit\r\n" +
                                   "Connection: close\r\n" +
                                   "X-Multiple-Header: value1, value2\r\n" +
                                   "\r\n"; // five lines total

            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int position = 0;

            using (var stream = new MemoryStream())
            {
                stream.Write(requestBytes, 0, requestBytes.Length);

                stream.Seek(0, SeekOrigin.Begin);
                var rawHeaders = Http11Helper.ReadHeaders(stream);

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
            
            using (var stream = new MemoryStream())
            {
                Http11Helper.SendResponse(stream, data, StatusCode.Code200Ok, headers);
                stream.Seek(0, SeekOrigin.Begin);
                string[] splittedResponse = Http11Helper.ReadHeaders(stream);

                byte[] response = new byte[data.Length];//test - 4 bytes
                int read = stream.Read(response, 0, response.Length);

                //Need to loop here... 4 bytes only - let's think that C# is in the good mood today. :-)
                Assert.Equal(read, response.Length);

                // let count number of items:
                // response string
                // Connection header
                // Content-Type header
                // Content-Length header
                // delimiter between headers and body - empty string
                // lines in response body
                Assert.Contains("HTTP/1.1", splittedResponse[0]);
                Assert.Contains(StatusCode.Code200Ok.ToString(CultureInfo.InvariantCulture), splittedResponse[0]);
                Assert.Contains(StatusCode.Reason200Ok, splittedResponse[0]);

                Assert.Contains("Connection: close", splittedResponse);
                Assert.Contains("Content-Type: " + "text/plain", splittedResponse);
                Assert.Contains("Content-Length: " + data.Length, splittedResponse);

                Assert.Equal(read, data.Length);
            }
        }
    }
}