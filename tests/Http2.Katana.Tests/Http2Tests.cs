using System.IO;
using System.Linq;
using Http2.TestClient.Adapters;
using Microsoft.Http1.Protocol;
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
    public class PropertiesProvider
    {
    }

    //This class is a server setup for a future interaction
    //No handshake can be switched on by changing config file handshakeOptions to no-handshake
    //If this setting was set to no-handshake then server and incoming client created by test methods
    //will work in the no handshake mode.
    //No priority and no flow control modes can be switched on in the .config file
    //ONLY for server. Clients and server can interact even if these modes are different 
    //for client and server.
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

            bool useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            bool usePriorities = ConfigurationManager.AppSettings["prioritiesOptions"] != "no-priorities";
            bool useFlowControl = ConfigurationManager.AppSettings["flowcontrolOptions"] != "no-flowcontrol";

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

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
        public void StartSessionAndSendRequestSuccessful()
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
            }
        }

        [StandardFact]
        public void StartAndSuddenlyCloseSessionSuccessful()
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
        public void StartMultipleSessionAndSendMultipleRequests()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndSendRequestSuccessful();
            }
        }

        [VeryLongTaskFact]
        public void StartSessionAndGet5MbDataSuccessful()
        {
            var requestStr = ConfigurationManager.AppSettings["5mbTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;
            var response = new StringBuilder();

            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 var dataFrame = frame as DataFrame;
                                 //response.Append(Encoding.UTF8.GetString(
                                 //   dataFrame.Payload.Array.Skip(dataFrame.Payload.Offset).Take(dataFrame.Payload.Count).ToArray()));

                                 if (!dataFrame.IsEndStream)
                                     return;

                                 wasFinalFrameReceived = true;
                                 finalFrameReceivedRaisedEvent.Set();
                             });

            try
            {
                adapter.StartSessionAsync();

                if (iostream is SslStream) //Server will answer on unsecure connection without request.
                    SendRequest(adapter, uri);

                finalFrameReceivedRaisedEvent.WaitOne(120000);

                //Assert.Equal(true, wasFinalFrameReceived);
                // Assert.Equal(TestHelpers.FileContent10MbTest, response.ToString());
            }
            finally
            {
                finalFrameReceivedRaisedEvent.Dispose();
                finalFrameReceivedRaisedEvent = null;

                iostream.Dispose();
                iostream = null;

                adapter.Dispose();
                adapter = null;

                GC.Collect();
            }
        }

        [StandardFact]
        public void StartSessionAndDoRequestInUpgrade()
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
                                         dataFrame.Payload.Array.Skip(dataFrame.Payload.Offset)
                                                  .Take(dataFrame.Payload.Count)
                                                  .ToArray()));
                                     isFin = dataFrame.IsEndStream;
                                 } while (!isFin && stream.ReceivedDataAmount > 0);
                                 if (isFin)
                                 {
                                     finalFrameReceived = true;
                                     finalFrameReceivedRaisedEvent.Set();
                                 }
                             });

            // process http/1.1 headers manually
            var http11Headers = "GET " + uri.AbsolutePath + " HTTP/1.1\r\n" +
                                "Host: " + uri.Host + "\r\n" +
                                "Connection: Upgrade, HTTP2-Settings\r\n" +
                                "Upgrade: " + Protocols.Http2 + "\r\n" +
                                "HTTP2-Settings: \r\n" + // TODO send any valid settings
                                "\r\n";
            clientStream.Write(Encoding.UTF8.GetBytes(http11Headers));
            clientStream.Flush();
            try
            {
                var response = Http11Helper.ReadHeaders(clientStream);
                Assert.Equal(
                    "HTTP/1.1 " + StatusCode.Code101SwitchingProtocols + " " + StatusCode.Reason101SwitchingProtocols,
                    response[0]);
                var headers = Http11Helper.ParseHeaders(response.Skip(1));
                Assert.Contains("Connection", headers.Keys);
                Assert.Equal("Upgrade", headers["Connection"][0]);
                Assert.Contains("Upgrade", headers.Keys);
                Assert.Equal(Protocols.Http2, headers["Upgrade"][0]);


                adapter.StartSessionAsync();

                // there are http2 frames after upgrade headers - we don't need to send request explicitly
                finalFrameReceivedRaisedEvent.WaitOne(10000);

                Assert.True(finalFrameReceived);
                Assert.Equal(TestHelpers.FileContentSimpleTest, responseBody.ToString());
            }
            catch (Exception)
            {
                int a = 1;
            }
            finally
            {
                adapter.Dispose();
            }
        }

        [Theory(Timeout = 70000)]
        [InlineData(true, true)] //We are going to use priorities and flow control
        public void StartMultipleStreamsInOneSessionSuccessful(bool usePriorities, bool useFlowControl)
        {
            string requestStr = string.Empty;
            // do not request file, test only request sending, do not test if response correct
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);
            int finalFramesCounter = 0;
            int streamsQuantity = _useSecurePort ? 50 : 49;

            bool wasAllResourcesDownloaded = false;

            var allResourcesDowloadedRaisedEvent = new ManualResetEvent(false);

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
                                         allResourcesDowloadedRaisedEvent.Set();
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
                        delay.WaitOne(200); //Send requests with little delay
                    }
                }

                allResourcesDowloadedRaisedEvent.WaitOne(60000);
                Assert.True(wasAllResourcesDownloaded);
            }
            finally
            {
                adapter.Dispose();
                adapter = null;

                allResourcesDowloadedRaisedEvent.Dispose();
                allResourcesDowloadedRaisedEvent = null;

                iostream.Dispose();
                iostream = null;
            }
        }

        [StandardFact]
        public void EmptyFileReceivedSuccessful()
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
        public void ParallelDownloadSuccefful()
        {
            //Assert.DoesNotThrow(delegate
            //{
            const int tasksCount = 2;
            var tasks = new Task[tasksCount];
            for (int i = 0; i < tasksCount; ++i)
            {
                tasks[i] = Task.Run(() =>
                    {
                        try
                        {
                            StartSessionAndGet5MbDataSuccessful();
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
            //});
        }


        [Fact]
        public void StartSessionAndSendRequestSuccessfulPush()
        {
            var requestStr = TestHelpers.IndexFileName;
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;
            var response = new StringBuilder();

            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var iostream = TestHelpers.GetHandshakedStream(uri);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(iostream, ConnectionEnd.Client,
                                                                    TestHelpers.UseSecurePort,
                                                                    new CancellationToken()) {CallBase = true};

            var adapter = mockedAdapter.Object;

            var streamIds = new List<int>();
            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>(), ItExpr.IsAny<Frame>())
                         .Callback<Http2Stream, Frame>((stream, frame) =>
                             {
                                 if (frame is DataFrame)
                                 {
                                     var dataFrame = frame as DataFrame;

                                     if (!streamIds.Contains(dataFrame.StreamId))
                                     {
                                         streamIds.Add(frame.StreamId);
                                         CheckReceivedDataAuth(stream, dataFrame);
                                     }

                                     if (dataFrame.IsEndStream
                                         &&
                                         stream.Headers.GetValue(CommonHeaders.Path)
                                               .Equals("/" + TestHelpers.SimpleTestFileName))
                                     {
                                         finalFrameReceivedRaisedEvent.Set();
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

                Http2Tests.SendRequest(adapter, uri);

                finalFrameReceivedRaisedEvent.WaitOne(120000);

                Assert.True(streamIds.Count == 2);
            }
            finally
            {
                finalFrameReceivedRaisedEvent.Dispose();
                finalFrameReceivedRaisedEvent = null;

                iostream.Dispose();
                iostream = null;

                adapter.Dispose();
                adapter = null;

                GC.Collect();
            }
        }

        [Fact]
        public void SendRequestOnAlreadyPushedResource()
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
                                         Http2Tests.SendRequest(adapter, pushUri);
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
                Http2Tests.SendRequest(adapter, uri);
                protocolErrorRaisedEvent.WaitOne(10000);
            }
            finally
            {
                protocolErrorRaisedEvent.Dispose();
                protocolErrorRaisedEvent = null;

                iostream.Dispose();
                iostream = null;

                adapter.Dispose();
                adapter = null;

                GC.Collect();
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
                    new KeyValuePair<string, string>(":host", host + ":" + uri.Port),
                    new KeyValuePair<string, string>(":scheme", scheme),
                };
            return pairs;
        }

        private void CheckReceivedDataAuth(Http2Stream stream, DataFrame dataFrame)
        {
            if (stream.Headers.GetValue(CommonHeaders.Path).Equals("/" + TestHelpers.IndexFileName))
                CheckArraysEquality(TestHelpers.FileContentIndex, dataFrame);

            if (stream.Headers.GetValue(CommonHeaders.Path).Equals("/" + TestHelpers.SimpleTestFileName))
                CheckArraysEquality(TestHelpers.FileContentSimpleTest, dataFrame);
        }

        private void CheckArraysEquality(String fileContent, DataFrame dataFrame)
        {
            using (Stream s = TestHelpers.GenerateStreamFromString(fileContent))
            {
                var etalonBytes = new byte[dataFrame.FrameLength];
                s.Read(etalonBytes, 0, dataFrame.FrameLength);

                Assert.Equal(etalonBytes,
                             dataFrame.Payload.Array.Skip(8)
                                      .Take(dataFrame.FrameLength)
                                      .ToArray());
            }
        }

        public void Dispose()
        {
        }
    }
}