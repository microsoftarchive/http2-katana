using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Http2HeadersCompression;
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

namespace Http2Tests
{
    public class Http2Tests
    {
        private HttpSocketServer _http2Server;
        private const string _clientSessionHeader = @"FOO * HTTP/2.0\r\n\r\nBA\r\n\r\n";

        private async Task InvokeMiddleWare(IDictionary<string, object> environment)
        {
            var handshakeAction = (Action)environment["HandshakeAction"];
            handshakeAction.Invoke();
        }

        public Http2Tests()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var secureAddress = appSettings["secureAddress"];
            Uri uri;
            Uri.TryCreate(secureAddress, UriKind.Absolute, out uri);

            var properties = new Dictionary<string, object>();
            var addresses = new List<IDictionary<string, object>>()
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

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootPath = @"\root";
            var serverRootDir = assemblyPath + rootPath;
            var serverSmallRootFile = serverRootDir + ConfigurationManager.AppSettings["smallTestFile"];
            var server10mbRootFile = serverRootDir + ConfigurationManager.AppSettings["10mbTestFile"];

            Directory.CreateDirectory(serverRootDir);

            var content = Encoding.UTF8.GetBytes("HelloWorld"); //10 bytes

            using (var stream = new FileStream(server10mbRootFile, FileMode.Create))
            {
                //Write 10 000 000 bytes or 10 mb
                for (int i = 0; i < 1000000; i++)
                {
                    stream.Write(content, 0, content.Length);
                }
            }

            using (var stream = new FileStream(serverSmallRootFile, FileMode.Create))
            {
                stream.Write(content, 0, content.Length);
            }

            new Thread((ThreadStart)delegate
                {
                    _http2Server = new HttpSocketServer(InvokeMiddleWare, properties);
                }).Start();

            using (var waitForServersStart = new ManualResetEvent(false))
            {
                waitForServersStart.WaitOne(3000);
            }
        }

        private static void SendSessionHeader(SecureSocket socket)
        {
            socket.Send(Encoding.UTF8.GetBytes(_clientSessionHeader));
        }

        private static SecureSocket GetHandshakedSocket(Uri uri)
        {
            string selectedProtocol = null;

            var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            var options = new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Client);

            options.VerificationType = CredentialVerification.None;
            options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(@"certificate.pfx");
            options.Flags = SecurityFlags.Default;
            options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                ProtocolType.Tcp, options);

            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { selectedProtocol = args.SelectedProtocol; };

                sessionSocket.Connect(new DnsEndPoint(uri.Host, uri.Port), monitor);

                HandshakeManager.GetHandshakeAction(sessionSocket, options).Invoke();
            }

            SendSessionHeader(sessionSocket);

            return sessionSocket;
        }

        private static Http2Stream SubmitRequest(Http2Session session, Uri uri)
        {
            const string method = "GET";
            string path = uri.PathAndQuery;
            const string version = "HTTP/2.0";
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
            string requestStr = ConfigurationManager.AppSettings["secureAddress"] + ConfigurationManager.AppSettings["smallTestFile"];
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            
            bool wasSettingsSent = false;
            bool wasHeadersPlusPrioritySent = false;
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

            var session = new Http2Session(socket, ConnectionEnd.Client);

            session.OnSettingsSent += (o, args) =>
            {
                wasSettingsSent = true;

                Assert.Equal(args.SettingsFrame.StreamId, 0);

                settingsSentRaisedEventArgs.Set();
            };

            session.OnFrameSent += (sender, args) =>
            {
                if (wasHeadersPlusPrioritySent == false)
                {
                    wasHeadersPlusPrioritySent = args.Frame is HeadersPlusPriority;

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
            Assert.Equal(stream.FinSent, false);
            Assert.Equal(stream.Disposed, false);
            Assert.Equal(wasHeadersPlusPrioritySent, true);
            Assert.Equal(wasSettingsSent, true);

            headersPlusPriSentRaisedEvent.Dispose();
            settingsSentRaisedEventArgs.Dispose();
            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasSocketClosed, true);
        }

        [Fact]
        public void StartMultipleSessionAndSendMultipleRequests()
        {
            for (int i = 0; i < 5; i++)
            {
                StartSessionAndSendRequestSuccessful();
            }
        }

        [Fact]
        public void StartSessionAndGet10MbDataSuccessful()
        {
            string requestStr = ConfigurationManager.AppSettings["secureAddress"] + ConfigurationManager.AppSettings["10mbTestFile"];
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

            var session = new Http2Session(socket, ConnectionEnd.Client);

            session.OnFrameReceived += (sender, args) =>
            {
                if (args.Frame.IsFin)
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
        public void StartMultipleSessionsAndGet50MbDataSuccessful()
        {
            for (int i = 0; i < 5; i++ )
            {
                StartSessionAndGet10MbDataSuccessful();
            }
        }

        [Fact]
        public void StartMultipleStreamsInOneSessionSuccessful()
        {
            string requestStr = ConfigurationManager.AppSettings["secureAddress"] + ConfigurationManager.AppSettings["smallTestFile"];
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

            var session = new Http2Session(socket, ConnectionEnd.Client);

            session.OnFrameReceived += (sender, args) =>
            {
                if (args.Frame.IsFin)
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

            allResourcesDowloadedRaisedEvent.WaitOne(60000);

            Assert.Equal(session.ActiveStreams.Count, streamsQuantity);

            session.Dispose();

            socketClosedRaisedEvent.WaitOne(60000);

            Assert.Equal(wasAllResourcesDownloaded, true);
            Assert.Equal(wasSocketClosed, true);
        }

    }
}
