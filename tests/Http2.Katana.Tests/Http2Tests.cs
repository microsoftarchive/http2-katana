using System.Linq;
using Http2.TestClient.Adapters;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Http2.Push;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenSSL;
using OpenSSL.SSL;
using Xunit;
using Xunit.Extensions;

namespace Http2.Katana.Tests
{
    /// <summary>
    /// This class is a server setup for a future interaction
    /// No handshake can be switched on by changing config file handshakeOptions to no-handshake
    /// If this setting was set to no-handshake then server and incoming client created by test methods
    /// will work in the no handshake mode.
    /// No priority and no flow control modes can be switched on in the .config file
    /// ONLY for server. Clients and server can interact even if these modes are different 
    /// for client and server.
    /// </summary>
    public class Http2Setup : IDisposable
    {
        public HttpSocketServer SecureServer { get; protected set; }
        public HttpSocketServer UnsecureServer { get; protected set; }
        public bool UseSecurePort { get; protected set; }
        public bool UseHandshake { get; protected set; }

        public Http2Setup()
        {
            UseHandshake = GetHandshakeNeed();
            SecureServer =
                new HttpSocketServer(new Http2Middleware(new PushMiddleware(new ResponseMiddleware(null))).Invoke,
                                     GetProperties(true));
            UnsecureServer =
                new HttpSocketServer(new Http2Middleware(new PushMiddleware(new ResponseMiddleware(null))).Invoke,
                                     GetProperties(false));
        }

        private static Dictionary<string, object> GetProperties(bool useSecurePort)
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

            var useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            properties.Add("use-handshake", useHandshake);

            string serverName = appSettings[Strings.ServerName];
            properties.Add(Strings.ServerName, serverName);

            return properties;
        }

        private static bool GetHandshakeNeed()
        {
            return ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
        }

        public void Dispose()
        {
            SecureServer.Dispose();
            UnsecureServer.Dispose();
        }
    }

    public class Http2Tests : IUseFixture<Http2Setup>, IDisposable
    {
        private static bool _useSecurePort;

        void IUseFixture<Http2Setup>.SetFixture(Http2Setup setupInstance)
        {
            _useSecurePort = setupInstance.UseSecurePort;
        }

        public static void SendRequest(Http2ClientMessageHandler adapter, Uri uri)
        {
            var pairs = GetHeadersList(uri);

            adapter.SendRequest(pairs, Constants.DefaultStreamPriority, true);
        }

        [StandardFact]
        public void StartHttp2Session()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasHeadersReceived = false;
            var headersReceivedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};
            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessRequest", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 if (frame is HeadersFrame)
                                 {
                                     wasHeadersReceived = true;
                                     headersReceivedEvent.Set();
                                 }
                             });

            try
            {
                adapter.StartSessionAsync();

                SendRequest(adapter, uri);

                headersReceivedEvent.WaitOne(10000);
                Assert.Equal(wasHeadersReceived, true);

                headersReceivedEvent.Dispose();
            }
            finally
            {
                adapter.Dispose();
                iostream.Dispose();
            }
        }

        [StandardFact]
        public void StartAndCloseSession()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            bool gotException = false;
            var stream = TestHelpers.GetHandshakedStream(uri);

            try
            {
                using (
                    var adapter = new Http2ClientMessageHandler(stream, ConnectionEnd.Client, TestHelpers.UseSecurePort,
                                                                new CancellationToken()))
                {
                    adapter.StartSessionAsync();
                }
            }
            catch (Exception)
            {
                gotException = true;
            }

            Assert.Equal(gotException, false);
        }

        [VeryLongTaskFact]
        public void StartMultipleSessions()
        {
            for (int i = 0; i < 4; i++)
            {
                StartHttp2Session();
            }
        }

        [VeryLongTaskFact]
        public void SimpleTestDownload()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;
            var response = new StringBuilder();

            var finalFrameReceivedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 var dataFrame = frame as DataFrame;
                                 var chunkBytes = dataFrame.Data.Array
                                                           .Skip(dataFrame.Data.Offset)
                                                           .Take(dataFrame.Data.Count).ToArray();

                                 var chunkStr = Encoding.UTF8.GetString(chunkBytes);
                                 response.Append(chunkStr);

                                 if (!dataFrame.IsEndStream)
                                     return;

                                 wasFinalFrameReceived = true;
                                 finalFrameReceivedEvent.Set();
                             });

            try
            {
                adapter.StartSessionAsync();

                // server will answer on unsecure connection without request
                if (iostream is SslStream) 
                    SendRequest(adapter, uri);

                // wait 1 min
                finalFrameReceivedEvent.WaitOne(60*1000);

                Assert.Equal(true, wasFinalFrameReceived);
                Assert.Equal(TestHelpers.FileContentSimpleTest, response.ToString());
            }
            finally
            {
                iostream.Dispose();
                adapter.Dispose();
            }
        }

        [VeryLongTaskFact]
        public void UpgradeAndFileDownload()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(ConfigurationManager.AppSettings["unsecureAddress"] + requestStr, UriKind.Absolute, out uri);

            bool finalFrameReceived = false;
            var responseBody = new StringBuilder();
            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var clientStream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(clientStream, ConnectionEnd.Client,
                                                                    clientStream is SslStream,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 bool isFin;
                                 do
                                 {
                                     var dataFrame = frame as DataFrame;
                                     responseBody.Append(Encoding.UTF8.GetString(
                                         dataFrame.Data.Array.Skip(dataFrame.Data.Offset)
                                                  .Take(dataFrame.Data.Count)
                                                  .ToArray()));
                                     isFin = dataFrame.IsEndStream;
                                 } while (!isFin && stream.ReceivedDataAmount > 0);
                                 if (isFin)
                                 {
                                     finalFrameReceived = true;
                                     finalFrameReceivedRaisedEvent.Set();
                                 }
                             });

            try
            {
                adapter.StartSessionAsync();

                finalFrameReceivedRaisedEvent.WaitOne(180 * 1000);

                Assert.True(finalFrameReceived);
                Assert.Equal(TestHelpers.FileContentSimpleTest, responseBody.ToString());
            }
            finally
            {
                adapter.Dispose();
                clientStream.Dispose();
            }
        }

        [VeryLongTaskFact]
        public void MultipleStreamsInOneSession()
        {
            string requestStr = string.Empty;
            // do not request file, test only request sending
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);
            int finalFramesCounter = 0;
            int streamsQuantity = _useSecurePort ? 50 : 49;

            bool wasAllResourcesDownloaded = false;
            var allResourcesDowloadedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};
            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 var dataFrame = frame as DataFrame;
                                 if (dataFrame.IsEndStream)
                                 {
                                     if (++finalFramesCounter == streamsQuantity)
                                     {
                                         wasAllResourcesDownloaded = true;
                                         allResourcesDowloadedEvent.Set();
                                     }
                                 }
                             });

            try
            {
                adapter.StartSessionAsync();

                // wait until session will be created
                using (var delay = new ManualResetEvent(false))
                {
                    delay.WaitOne(2000);

                    for (int i = 0; i < streamsQuantity; i++)
                    {
                        SendRequest(adapter, uri);
                        // send requests with little delay
                        delay.WaitOne(200); 
                    }
                }

                // wait 1 min
                allResourcesDowloadedEvent.WaitOne(60000);
                Assert.True(wasAllResourcesDownloaded);
            }
            finally
            {
                adapter.Dispose();
                iostream.Dispose();         
            }
        }

        [StandardFact]
        public void EmptyFileDownload()
        {
            const string requestStr = "emptyFile.txt";
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;

            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessRequest", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 // TODO discuss whether we should expect empty data or just header with zero content-length
                                 wasFinalFrameReceived = true;
                                 if (frame is IEndStreamFrame && (frame as IEndStreamFrame).IsEndStream)
                                     finalFrameReceivedRaisedEvent.Set();
                             });

            try
            {
                adapter.StartSessionAsync();

                SendRequest(adapter, uri);
                finalFrameReceivedRaisedEvent.WaitOne(10000);

                Assert.Equal(true, wasFinalFrameReceived);
            }
            finally
            {
                adapter.Dispose();
                iostream.Dispose();
            }
        }

        [VeryLongTaskFact]
        public void ParallelDownload()
        {
            const int tasksCount = 2;
            var tasks = new Task[tasksCount];
            for (int i = 0; i < tasksCount; ++i)
            {
                tasks[i] = Task.Run(() =>
                    {
                        try
                        {
                            SimpleTestDownload();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            Assert.Equal(ex, null);
                        }
                    });
            }

            Task.WhenAll(tasks).Wait();
        }

        [Fact]
        public void ServerPush()
        {
            var requestStr = TestHelpers.IndexFileName;
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;

            var resources = new List<StringBuilder>();
            var streamIds = new List<int>();

            var finalFrameReceivedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};
            var adapter = mockedAdapter.Object;
            
            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 if (frame is DataFrame)
                                 {
                                     var dataFrame = frame as DataFrame;

                                     if (!streamIds.Contains(dataFrame.StreamId))
                                     {
                                         streamIds.Add(frame.StreamId);
                                         resources.Add(new StringBuilder());
                                     }                                        

                                     var index = streamIds.IndexOf(frame.StreamId);
                                     var chunkBytes = dataFrame.Data.Array
                                                           .Skip(dataFrame.Data.Offset)
                                                           .Take(dataFrame.Data.Count).ToArray();

                                     var chunkStr = Encoding.UTF8.GetString(chunkBytes);
                                     resources[index].Append(chunkStr);
                                   
                                 }
                             });

            try
            {
                Dictionary<string, string> initialRequest = null;
                if (!(iostream is SslStream))
                {
                    initialRequest = GetHeadersList(uri).ToDictionary(p => p.Key, p => p.Value);
                }

                adapter.StartSessionAsync(initialRequest);

                SendRequest(adapter, uri);

                // wait for manual reset event
                finalFrameReceivedEvent.WaitOne(30000);

                Assert.True(streamIds.Count == 2);
                var str1 = resources[0].ToString();
                var str2 = resources[1].ToString();

                Assert.Equal(TestHelpers.FileContentSimpleTest, str1);
                Assert.Equal(TestHelpers.FileContentIndex, str2);
            }
            finally
            {
                iostream.Dispose();
                adapter.Dispose();
            }
        }

        [Fact]
        public void RequestPushedResource()
        {
            var requestStr = TestHelpers.IndexFileName;
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;
            var response = new StringBuilder();

            var protocolErrorRaisedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessRequest", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 if (frame is HeadersFrame && stream.Id.Equals(2))
                                 {
                                     Uri pushUri;
                                     Uri.TryCreate(
                                         TestHelpers.GetAddress() +
                                         stream.Headers.GetValue(CommonHeaders.Path).Replace("/", ""), UriKind.Absolute,
                                         out pushUri);

                                     try
                                     {
                                         SendRequest(adapter, pushUri);
                                     }
                                     catch (ProtocolError e)
                                     {
                                         protocolErrorRaisedEvent.Set();
                                     }
                                 }
                             });

            try
            {
                Dictionary<string, string> initialRequest = null;
                if (!(iostream is SslStream))
                {
                    initialRequest = GetHeadersList(uri).ToDictionary(p => p.Key, p => p.Value);
                }

                adapter.StartSessionAsync(initialRequest);
                SendRequest(adapter, uri);
                protocolErrorRaisedEvent.WaitOne(10000);
            }
            finally
            {
                iostream.Dispose();
                adapter.Dispose();
            }
        }

        private static HeadersList GetHeadersList(Uri uri)
        {
            const string method = "get";
            var path = uri.PathAndQuery;
            var version = Protocols.Http2;
            var scheme = uri.Scheme;
            var host = uri.Host;

            var pairs = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", method),
                    new KeyValuePair<string, string>(":path", path),
                    new KeyValuePair<string, string>(":version", version),
                    new KeyValuePair<string, string>(":authority", host + ":" + uri.Port),
                    new KeyValuePair<string, string>(":scheme", scheme),
                };
            return pairs;
        }

        public void Dispose()
        {
        }
    }
}