using Client.Adapters;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Tests;
using Moq;
using Moq.Protected;
using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Extensions;

namespace Http2.Katana.Tests
{
    //This class is a server setup for a future interaction
    //No handshake can be switched on by changing config file handshakeOptions to no-handshake
    //If this setting was set to no-handshake then server and incoming client created by test methods
    //will work in the no handshake mode.
    //No priority and no flow control modes can be switched on in the .config file
    //ONLY for server. Clients and server can interact even if these modes are different 
    //for client and server.
    public class Http2Setup : IDisposable
    {
        public HttpSocketServer Server { get; private set; }
        public bool UseSecurePort { get; private set; }
        public bool UseHandshake { get; private set; }

        public Http2Setup()
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
    
    public class Http2Tests : IUseFixture<Http2Setup>, IDisposable
    {
        private static bool _useSecurePort;

        void IUseFixture<Http2Setup>.SetFixture(Http2Setup setupInstance)
        {
            _useSecurePort = setupInstance.UseSecurePort;
        }

        protected void SendRequest(Http2ClientMessageHandler adapter, Uri uri)
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

            adapter.SendRequest(pairs, Priority.None, true);
        }

        [StandardFact]
        public void StartSessionAndSendRequestSuccessful()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasHeadersReceived = false;

            var headersReceivedEvent = new ManualResetEvent(false);

            var duplexStream = TestHelpers.GetHandshakedDuplexStream(requestStr);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(duplexStream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(),
                new CancellationToken()) { CallBase = true };

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
                adapter.ProcessRequestAsync();

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
            var stream = TestHelpers.GetHandshakedDuplexStream(requestStr);
            
            try
            {
                using (var adapter = new Http2ClientMessageHandler(stream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(), new CancellationToken()))
                {
                    adapter.ProcessRequestAsync();
                }
            }
            catch (Exception)
            {
                gotException = true;
            }

            Assert.Equal(gotException, false);
        }

        [LongTaskFact]
        public void StartMultipleSessionAndSendMultipleRequests()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndSendRequestSuccessful();
            }
        }

        [LongTaskFact]
        public void StartSessionAndGet10MbDataSuccessful()
        {
            var requestStr = ConfigurationManager.AppSettings["10mbTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            var wasFinalFrameReceived = false;
            var response = new StringBuilder();

            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = TestHelpers.GetHandshakedDuplexStream(requestStr);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(duplexStream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(),
                new CancellationToken()) { CallBase = true };

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>())
                .Callback<Http2Stream>(stream =>
                {
                    bool isFin;
                    do
                    {
                        var frame = stream.DequeueDataFrame();
                        response.Append(Encoding.UTF8.GetString(
                            frame.Payload.Array.Skip(frame.Payload.Offset).Take(frame.Payload.Count).ToArray()));
                        isFin = frame.IsEndStream;
                    } while (!isFin && stream.ReceivedDataAmount > 0);
                    if (isFin)
                    {
                        wasFinalFrameReceived = true;
                        finalFrameReceivedRaisedEvent.Set();
                    }
                });

            try
            {
                adapter.ProcessRequestAsync();

                // wait until session will be created
                using (var delay = new ManualResetEvent(false))
                {
                    delay.WaitOne(2000);
                }

                SendRequest(adapter, uri);
                finalFrameReceivedRaisedEvent.WaitOne(40000);

                Assert.Equal(true, wasFinalFrameReceived);
                Assert.Equal(TestHelpers.FileContent10MbTest, response.ToString());
            }
            finally
            {
                adapter.Dispose();
            }
        }

        [VeryLongTaskFact]
        public void StartMultipleSessionsAndGet40MbDataSuccessful()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndGet10MbDataSuccessful();
            }
        }

        [StandardFact]
        public void StartSessionAndDoRequestInUpgrade()
        {
            var requestStr = ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);

            bool finalFrameReceived = false;

            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = TestHelpers.GetHandshakedDuplexStream(requestStr, false);

            var responseBody = new StringBuilder();

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(duplexStream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(),
               new CancellationToken()) { CallBase = true };

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>())
                .Callback<Http2Stream>(stream =>
                {
                    bool isFin;
                    do
                    {
                        var frame = stream.DequeueDataFrame();
                        responseBody.Append(Encoding.UTF8.GetString(
                            frame.Payload.Array.Skip(frame.Payload.Offset).Take(frame.Payload.Count).ToArray()));
                        isFin = frame.IsEndStream;
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
            duplexStream.Write(Encoding.UTF8.GetBytes(http11Headers));
            duplexStream.Flush();
            var response = Http11Helper.ReadHeaders(duplexStream);
            Assert.Equal("HTTP/1.1 " + StatusCode.Code101SwitchingProtocols + " " + StatusCode.Reason101SwitchingProtocols, response[0]);
            var headers = Http11Helper.ParseHeaders(response.Skip(1));
            Assert.Contains("Connection", headers.Keys);
            Assert.Equal("Upgrade", headers["Connection"][0]);
            Assert.Contains("Upgrade", headers.Keys);
            Assert.Equal(Protocols.Http2, headers["Upgrade"][0]);

            try
            {
                adapter.ProcessRequestAsync();

                // there are http2 frames after upgrade headers - we don't need to send request explicitly
                finalFrameReceivedRaisedEvent.WaitOne(10000);

                Assert.True(finalFrameReceived);
                Assert.Equal(TestHelpers.FileContentSimpleTest, responseBody.ToString());
            }
            finally
            {
                adapter.Dispose();
            }
        }

        [Theory(Timeout = 70000)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void StartMultipleStreamsInOneSessionSuccessful(bool usePriorities, bool useFlowControl)
        {
            string requestStr = string.Empty; // do not request file, test only request sending, do not test if response correct
            Uri uri;
            Uri.TryCreate(TestHelpers.GetAddress() + requestStr, UriKind.Absolute, out uri);
            int finalFramesCounter = 0;
            int streamsQuantity = _useSecurePort ? 50 : 49;

            bool wasAllResourcesDownloaded = false;

            var allResourcesDowloadedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = TestHelpers.GetHandshakedDuplexStream(requestStr);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(duplexStream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(),
                new CancellationToken()) { CallBase = true };

            var adapter = mockedAdapter.Object;

            mockedAdapter.Protected().Setup("ProcessIncomingData", ItExpr.IsAny<Http2Stream>())
                .Callback<Http2Stream>(stream =>
                {
                    bool isFin;
                    do
                    {
                        var frame = stream.DequeueDataFrame();
                        isFin = frame.IsEndStream;
                    } while (!isFin && stream.ReceivedDataAmount > 0);
                    if (isFin)
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
                adapter.ProcessRequestAsync();

                // wait until session will be created
                using (var delay = new ManualResetEvent(false))
                {
                    delay.WaitOne(2000);
                }

                for (int i = 0; i < streamsQuantity; i++)
                {
                    SendRequest(adapter, uri);
                }

                allResourcesDowloadedRaisedEvent.WaitOne(60000);
                Assert.True(wasAllResourcesDownloaded);
            }
            finally
            {
                adapter.Dispose();
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

            var duplexStream = TestHelpers.GetHandshakedDuplexStream(requestStr);

            var mockedAdapter = new Mock<Http2ClientMessageHandler>(duplexStream, ConnectionEnd.Client, TestHelpers.GetTransportInformation(),
                new CancellationToken()) { CallBase = true };

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
                adapter.ProcessRequestAsync();

                SendRequest(adapter, uri);
                finalFrameReceivedRaisedEvent.WaitOne(10000);

                Assert.Equal(true, wasFinalFrameReceived);
            }
            finally
            {
                adapter.Dispose();
            }
        }

        public void Dispose()
        {
            
        }
    }
}
