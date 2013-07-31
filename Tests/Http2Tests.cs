using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedProtocol.Compression.Http2DeltaHeadersCompression;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Owin.Types;
using SharedProtocol;
using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SocketServer;
using Xunit;
using System.Configuration;
using Xunit.Extensions;

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
        public Thread ServerThread { get; private set; }
        public bool UseSecurePort { get; private set; }
        public bool UseHandshake { get; private set; }


        private Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            if (environment["HandshakeAction"] is Func<Task>)
                {
                var handshakeAction = (Func<Task>)environment["HandshakeAction"];
                return handshakeAction.Invoke();
                }
            return null;
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

            properties.Add(OwinConstants.CommonKeys.Addresses, addresses);

            bool useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            bool usePriorities = ConfigurationManager.AppSettings["prioritiesOptions"] != "no-priorities";
            bool useFlowControl = ConfigurationManager.AppSettings["flowcontrolOptions"] != "no-flowcontrol";

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

            ServerThread = new Thread((ThreadStart)delegate
                {
                    new HttpSocketServer(InvokeMiddleWare, properties);
                }){Name = "Http2ServerThread"};
            ServerThread.Start();

            using (var waitForServersStart = new ManualResetEvent(false))
            {
                waitForServersStart.WaitOne(3000);
            }
        }
   
        public void Dispose()
        {
            if (ServerThread.IsAlive)
            {
                ServerThread.Abort();
            }
        }
    }

    public class Http2TestSuite : IUseFixture<Http2Setup>, IDisposable
    {
        private const string ClientSessionHeader = @"PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        private static bool _useSecurePort;
        private static bool _useHandshake;
        private static IDictionary<string, object> _handshakeResult;

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

        protected static SecureSocket GetHandshakedSocket(Uri uri)
        {
            string selectedProtocol = null;

            var extensions = new[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = _useSecurePort
                              ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http2, Protocols.Http1 },
                                                    ConnectionEnd.Client)
                              : new SecurityOptions(SecureProtocol.None, extensions, new[] { Protocols.Http2, Protocols.Http1 },
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

                if (_useHandshake)
                {
                    var handshakeEnv = new Dictionary<string, object>
                    {
                        {":method", "get"},
                        {":version", "http/1.1"},
                        {":path", uri.PathAndQuery},
                        {":scheme", uri.Scheme},
                        {":host", uri.Host},
                        {"securityOptions", options},
                        {"secureSocket", sessionSocket},
                        {"end", ConnectionEnd.Client}
                    };

                    _handshakeResult = HandshakeManager.GetHandshakeAction(handshakeEnv).Invoke();
                 }
            }

            SendSessionHeader(sessionSocket);

            return sessionSocket;
        }

        protected static Http2Stream SubmitRequest(Http2Session session, Uri uri)
        {
            const string method = "get";
            string path = uri.PathAndQuery;
            const string version = "http/2.0";
            string scheme = uri.Scheme;
            string host = uri.Host;

            var pairs = new List<Tuple<string, string, IAdditionalHeaderInfo>>
                {
                    new Tuple<string, string, IAdditionalHeaderInfo>(":method", method, new Indexation(IndexationType.Indexed)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":path", path, new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":version", version, new Indexation(IndexationType.Incremental)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":host", host, new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":scheme", scheme, new Indexation(IndexationType.Substitution)),
                };

            session.SendRequest(pairs, 3, false);

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

            var socket = GetHandshakedSocket(uri);

            socket.OnClose += (sender, args) =>
            {
                socketClosedRaisedEvent.Set();
                wasSocketClosed = true;
            };

            var session = new Http2Session(socket, ConnectionEnd.Client, true, true, _handshakeResult);

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
                    wasHeadersSent = args.Frame is Headers;

                    headersPlusPriSentRaisedEvent.Set();
                }
            };

            session.Start();

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
            var socket = GetHandshakedSocket(uri);

            socket.OnClose += (sender, args) => socketClosedRaisedEvent.Set();

            try
            {
                var session = new Http2Session(socket, ConnectionEnd.Client, true, true, _handshakeResult);
                session.Start();
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

            var socket = GetHandshakedSocket(uri);

            socket.OnClose += (sender, args) =>
            {
                socketClosedRaisedEvent.Set();
                wasSocketClosed = true;
            };

            var session = new Http2Session(socket, ConnectionEnd.Client, true, true, _handshakeResult);

            session.OnFrameReceived += (sender, args) =>
            {
                if (args.Frame is IEndStreamFrame && ((IEndStreamFrame)args.Frame).IsEndStream)
                {
                    finalFrameReceivedRaisedEvent.Set();
                    wasFinalFrameReceived = true;
                }
            };

            session.Start();

            SubmitRequest(session, uri);

            finalFrameReceivedRaisedEvent.WaitOne(60000);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasFinalFrameReceived, true);
            Assert.Equal(wasSocketClosed, true);
        }

        [Fact]
        public void StartMultipleSessionsAndGet40MbDataSuccessful()
        {
            for (int i = 0; i < 4; i++)
            {
                StartSessionAndGet10MbDataSuccessful();
            }
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

            const int streamsQuantity = 100;

            bool wasAllResourcesDownloaded = false;
            bool wasSocketClosed = false;

            var allResourcesDowloadedRaisedEvent = new ManualResetEvent(false);
            var socketClosedRaisedEvent = new ManualResetEvent(false);

            var socket = GetHandshakedSocket(uri);

            socket.OnClose += (sender, args) =>
            {
                wasSocketClosed = true;
                socketClosedRaisedEvent.Set();
            };

            var session = new Http2Session(socket, ConnectionEnd.Client, usePriorities, useFlowControl, _handshakeResult);

            session.OnFrameReceived += (sender, args) =>
            {
                if (args.Frame is IEndStreamFrame && ((IEndStreamFrame)args.Frame).IsEndStream)
                {
                    finalFramesCounter++;
                    if (finalFramesCounter == streamsQuantity)
                    {
                        allResourcesDowloadedRaisedEvent.Set();
                        wasAllResourcesDownloaded = true;
                    }
                }
            };

            session.Start();

            for (int i = 0; i < streamsQuantity; i++)
            {
                SubmitRequest(session, uri);
            }

            allResourcesDowloadedRaisedEvent.WaitOne(120000);

            Assert.Equal(session.ActiveStreams.Count, streamsQuantity);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasAllResourcesDownloaded, true);
            Assert.Equal(wasSocketClosed, true);
        }

        public void Dispose()
        {

        }
    }
}
