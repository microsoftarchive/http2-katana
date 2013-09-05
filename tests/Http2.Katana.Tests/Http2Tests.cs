using System.IO;
using System.Linq;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;
using Moq;
using Org.Mentalis;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using ServerOwinMiddleware;
using SocketServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;
using StatusCode = Microsoft.Http2.Protocol.StatusCode;

namespace Http2Tests
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


        private async static Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            // emulate some dummy response
            var owinResponse = new OwinResponse(environment);
            owinResponse.Body = new MemoryStream(Encoding.UTF8.GetBytes(TestHelpers.FileContent10MbTest));
            // set position to stream end
            owinResponse.Body.Position = owinResponse.Body.Length - 1;
            owinResponse.ContentLength = owinResponse.Body.Length;
        }

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
                            {"port", uri.Port.ToString()},
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

            Server = new HttpSocketServer(new Http2Middleware(InvokeMiddleWare).Invoke, properties);
        }

        public void Dispose()
        {
            Server.Dispose();
        }
    }
    
    public class Http2Tests : IUseFixture<Http2Setup>, IDisposable
    {
        private const string ClientSessionHeader = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        private static bool _useSecurePort;
        private static bool _useHandshake;
        private static IDictionary<string, object> _environment;

        void IUseFixture<Http2Setup>.SetFixture(Http2Setup setupInstance)
        {
            _useSecurePort = setupInstance.UseSecurePort;
            _useHandshake = setupInstance.UseHandshake;
        }

        protected static string GetAddress()
        {

            if (_useSecurePort)
            {
                return ConfigurationManager.AppSettings["secureAddress"];
            }

            return ConfigurationManager.AppSettings["unsecureAddress"];
        }

        protected static void SendSessionHeader(SecureSocket socket)
        {
            socket.Send(Encoding.UTF8.GetBytes(ClientSessionHeader));
        }

        protected static DuplexStream GetHandshakedDuplexStream(Uri uri, bool useMock = false, bool doRequestInUpgrade = false)
        {
            _environment = new Dictionary<string, object>();
            string selectedProtocol = null;

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var protocols = new List<string> {Protocols.Http1};
            if (!doRequestInUpgrade)
            {
                protocols.Add(Protocols.Http2);
            }

            var options = _useSecurePort
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

            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };

                sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);


                string path = uri.PathAndQuery;
                if (_useHandshake)
                {
                    var handshakeEnv = new Dictionary<string, object>
                    {
                        {":method", "get"},
                        {":version", Protocols.Http2},
                        {":path", path},
                        {":scheme", uri.Scheme},
                        {":host", uri.Host},
                        {"securityOptions", options},
                        {"secureSocket", sessionSocket},
                        {"end", ConnectionEnd.Client}
                    };

                    sessionSocket.MakeSecureHandshake(options);
                }
            }

            //SendSessionHeader(sessionSocket);

            return useMock ? new Mock<DuplexStream>(sessionSocket, true).Object : new DuplexStream(sessionSocket, true);
        }

        protected static Http2Stream SubmitRequest(Http2Session session, Uri uri)
        {
            const string method = "get";
            string path = uri.PathAndQuery;
            string version = Protocols.Http2;
            string scheme = uri.Scheme;
            string host = uri.Host;

            var pairs = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", method),
                    new KeyValuePair<string, string>(":path", path),
                    new KeyValuePair<string, string>(":version", version),
                    new KeyValuePair<string, string>(":host", host),
                    new KeyValuePair<string, string>(":scheme", scheme),
                };

            session.SendRequest(pairs, Priority.None, false);

            return session.ActiveStreams[1];
        }

        [Fact]
        public void StartSessionAndSendRequestSuccessful()
        {
            string requestStr = GetAddress() + ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool wasSettingsSent = false;
            bool wasHeadersSent = false;
            bool wasSocketClosed = false;

            var settingsSentRaisedEventArgs = new ManualResetEvent(false);
            var headersPlusPriSentRaisedEvent = new ManualResetEvent(false);
            var socketClosedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = GetHandshakedDuplexStream(uri);

            duplexStream.Socket.OnClose += (sender, args) =>
            {
                socketClosedRaisedEvent.Set();
                wasSocketClosed = true;
            };

            var session = new Http2Session(duplexStream, ConnectionEnd.Client, true, true, new CancellationToken());

            session.OnSettingsSent += (o, args) =>
            {
                wasSettingsSent = true;

                Assert.Equal(args.SettingsFrame.StreamId, 0);

                settingsSentRaisedEventArgs.Set();
            };

            session.OnFrameSent += (sender, args) =>
            {
                if (wasHeadersSent == false)
                {
                    wasHeadersSent = args.Frame is HeadersFrame;

                    headersPlusPriSentRaisedEvent.Set();
                }
            };

            Task.Run(() => session.Start());

            settingsSentRaisedEventArgs.WaitOne(60000);

            var stream = SubmitRequest(session, uri);

            headersPlusPriSentRaisedEvent.WaitOne(60000);

            //Settings frame does not contain flow control settings in this test. 
            Assert.Equal(session.ActiveStreams.Count, 1);
            Assert.Equal(session.ActiveStreams.FlowControlledStreams.Count, 1);
            Assert.Equal(stream.IsFlowControlBlocked, false);
            Assert.Equal(stream.Id, 1);
            Assert.Equal(stream.IsFlowControlEnabled, true);
            Assert.Equal(stream.EndStreamSent, false);
            Assert.Equal(stream.Disposed, false);
            Assert.Equal(wasHeadersSent, true);
            Assert.Equal(wasSettingsSent, true);

            headersPlusPriSentRaisedEvent.Dispose();
            settingsSentRaisedEventArgs.Dispose();
            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasSocketClosed, true);
        }

        [Fact]
        public void StartAndSuddenlyCloseSessionSuccessful()
        {
            string requestStr = GetAddress() + ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool gotException = false;

            var socketClosedRaisedEvent = new ManualResetEvent(false);
            var stream = GetHandshakedDuplexStream(uri);

            stream.Socket.OnClose += (sender, args) => socketClosedRaisedEvent.Set();

            try
            {
                var session = new Http2Session(stream, ConnectionEnd.Client, true, true, new CancellationToken());
                Task.Run(() => session.Start());
                session.Dispose();
            }
            catch (Exception)
            {
                gotException = true;
            }

            Assert.Equal(gotException, false);
        }

        [Fact]
        public void StartMultipleSessionAndSendMultipleRequests()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndSendRequestSuccessful();
            }
        }

        [Fact]
        public void StartSessionAndGet10MbDataSuccessful()
        {
            string requestStr = GetAddress() + ConfigurationManager.AppSettings["10mbTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool wasSocketClosed = false;
            bool wasFinalFrameReceived = false;

            var socketClosedRaisedEvent = new ManualResetEvent(false);
            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = GetHandshakedDuplexStream(uri);

            duplexStream.Socket.OnClose += (sender, args) =>
            {
                socketClosedRaisedEvent.Set();
                wasSocketClosed = true;
            };

            var session = new Http2Session(duplexStream, ConnectionEnd.Client, true, true, new CancellationToken());

            session.OnFrameReceived += (sender, args) =>
            {
                if ((args.Frame.Flags & FrameFlags.EndStream) != FrameFlags.None)
                {
                    finalFrameReceivedRaisedEvent.Set();
                    wasFinalFrameReceived = true;
                }
            };

            Task.Run(() => session.Start());

            SubmitRequest(session, uri);

            finalFrameReceivedRaisedEvent.WaitOne(60000);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(true, wasFinalFrameReceived);
            Assert.Equal(true, wasSocketClosed);
        }

        [Fact]
        public void StartMultipleSessionsAndGet40MbDataSuccessful()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndGet10MbDataSuccessful();
            }
        }

        [Fact]
        public void StartSessionAndDoRequestInUpgrade()
        {
            string requestStr = GetAddress() + ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);

            bool wasSocketClosed = false;
            int countOfFinalFrames = 0;

            var socketClosedRaisedEvent = new ManualResetEvent(false);
            var finalFrameReceivedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = GetHandshakedDuplexStream(uri, false, true);

            var http11Headers = "GET " + uri.AbsolutePath + " HTTP/1.1\r\n" +
                                "Host: " + uri.Host + "\r\n" +
                                "Connection: Upgrade, HTTP2-Settings\r\n" +
                                "Upgrade: " + Protocols.Http2 + "\r\n" +
                                "HTTP2-Settings: \r\n" + // TODO send any valid settings
                                "\r\n";
            duplexStream.Write(Encoding.UTF8.GetBytes(http11Headers));
            duplexStream.Flush();
            var response = Http11Manager.ReadHeaders(duplexStream);
            Assert.Equal("HTTP/1.1 " + StatusCode.Code101SwitchingProtocols + " " + StatusCode.Reason101SwitchingProtocols, response[0]);
            var headers = Http11Manager.ParseHeaders(response.Skip(1));
            Assert.Contains("Connection", headers.Keys);
            Assert.Equal("Upgrade", headers["Connection"][0]);
            Assert.Contains("Upgrade", headers.Keys);
            Assert.Equal(Protocols.Http2, headers["Upgrade"][0]);

            duplexStream.Socket.OnClose += (sender, args) =>
            {
                socketClosedRaisedEvent.Set();
                wasSocketClosed = true;
            };

            var session = new Http2Session(duplexStream, ConnectionEnd.Client, true, true, new CancellationToken());

            session.OnFrameReceived += (sender, args) =>
            {
                if ((args.Frame.Flags & FrameFlags.EndStream) != FrameFlags.None)
                {
                    countOfFinalFrames++;
                    if (countOfFinalFrames == 2)
                        finalFrameReceivedRaisedEvent.Set();
                }
            };

            Task.Run(() => session.Start());

            SubmitRequest(session, uri);

            finalFrameReceivedRaisedEvent.WaitOne(60000);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(2, countOfFinalFrames);
            Assert.Equal(true, wasSocketClosed);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void StartMultipleStreamsInOneSessionSuccessful(bool usePriorities, bool useFlowControl)
        {
            string requestStr = GetAddress() + ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            int finalFramesCounter = 0;
            int streamsQuantity = _useSecurePort ? 50 : 49;

            bool wasAllResourcesDownloaded = false;
            bool wasSocketClosed = false;

            var allResourcesDowloadedRaisedEvent = new ManualResetEvent(false);
            var socketClosedRaisedEvent = new ManualResetEvent(false);

            var duplexStream = GetHandshakedDuplexStream(uri);

            duplexStream.Socket.OnClose += (sender, args) =>
            {
                wasSocketClosed = true;
                socketClosedRaisedEvent.Set();
            };

            var session = new Http2Session(duplexStream, ConnectionEnd.Client, usePriorities, useFlowControl, new CancellationToken());

            session.OnFrameReceived += (sender, args) =>
            {
                if ((args.Frame.Flags & FrameFlags.EndStream) != FrameFlags.None)
                {
                    finalFramesCounter++;
                    if (finalFramesCounter == streamsQuantity)
                    {
                        allResourcesDowloadedRaisedEvent.Set();
                        wasAllResourcesDownloaded = true;
                    }
                }
            };

            Task.Run(() => session.Start());

            for (int i = 0; i < streamsQuantity; i++)
            {
                SubmitRequest(session, uri);
            }

            allResourcesDowloadedRaisedEvent.WaitOne(120000);
            //One stream is superfluous for request in upgrade
            Assert.Equal(session.ActiveStreams.Count, _useSecurePort ? streamsQuantity : streamsQuantity + 1);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(true, wasAllResourcesDownloaded);
            Assert.Equal(true, wasSocketClosed);
        }

        public void Dispose()
        {

        }
    }
}
