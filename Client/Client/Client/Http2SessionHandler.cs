using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;
using SharedProtocol.IO;

namespace Client
{
    public sealed class Http2SessionHandler : IDisposable
    {
        private SecurityOptions _options;
        private Http2Session _clientSession;
        private const string _certificatePath = @"certificate.pfx";
        private readonly Uri _requestUri;
        private SecureSocket _socket;
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();

        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public bool IsHttp2WillBeUsed {
            get { return _useHttp20; }
        }

        public Http2SessionHandler(Uri requestUri)
        {
            _requestUri = requestUri;
            _fileHelper = new FileHelper();
        }

        public async void Connect()
        {
            if (_clientSession != null)
            {
                return;
            }

            SecureSocket sessionSocket = null;

            try
            {
                int port = _requestUri.Port;

                int securePort;

                try
                {
                    securePort = int.Parse(ConfigurationManager.AppSettings["securePort"]);
                }
                catch (Exception)
                {
                    Console.WriteLine("Incorrect port in the config file!");
                    return;
                }

                var extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

                _options = port == securePort ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Client)
                    : new SecurityOptions(SecureProtocol.None, extensions, new[] { "http/2.0", "http/1.1" }, ConnectionEnd.Client);

                _options.VerificationType = CredentialVerification.None;
                _options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(_certificatePath);
                _options.Flags = SecurityFlags.Default;
                _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

                sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                 ProtocolType.Tcp, _options);

                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += (o, args) => { _selectedProtocol = args.SelectedProtocol; };
                    sessionSocket.Connect(new DnsEndPoint(_requestUri.Host, _requestUri.Port), monitor);

                    HandshakeManager.GetHandshakeAction(sessionSocket, _options).Invoke();

                    Console.WriteLine("Handshake finished");

                    _socket = sessionSocket;

                    if (_selectedProtocol == "http/1.1")
                    {
                        _useHttp20 = false;
                        return;
                    }

                }
                _useHttp20 = true;
                _clientSession = new Http2Session(_socket, ConnectionEnd.Client);

                _clientSession.OnFrameReceived += FrameReceivedHandler;

                await _clientSession.Start();
            }
            catch (HTTP2HandshakeFailed)
            {
                _useHttp20 = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled session exception was caught: " + ex.Message);
                if (sessionSocket != null)
                {
                    sessionSocket.Close();
                }
                if (_clientSession != null)
                {
                    _clientSession.Dispose();
                    _clientSession = null;
                }
            }
        }

        private Http2Stream SubmitRequest()
        {
            var pairs = new Dictionary<string, string>(10);
            const string method = "GET";
            string path = _requestUri.PathAndQuery;
            const string version = "HTTP/2.0";
            string scheme = _requestUri.Scheme;
            string host = _requestUri.Host;

            pairs.Add(":method", method);
            pairs.Add(":path", path);
            pairs.Add(":version", version);
            pairs.Add(":host", host);
            pairs.Add(":scheme", scheme);
            
            //TODO Calc priority

            var clientStream = _clientSession.SendRequest(pairs, 3, true);

            return clientStream;
        }
        
        public async Task SendRequestAsync()
        {
            if (_useHttp20 == false)
            {
                Console.WriteLine("Download with Http/1.1");
                Http11Manager.Http11DownloadResource(_socket, _requestUri);
                return;
            }

            Http2Stream stream;
            Console.WriteLine("Submitting request");
            stream = SubmitRequest();
        }

        public TimeSpan Ping()
        {
            if (_clientSession != null)
            {
                return Task.Run(() => _clientSession.Ping()).Result;
            }

            return default(TimeSpan);
        }

        private void SendResponce(Http2Stream stream)
        {
            byte[] binaryFile = _fileHelper.GetFile(stream.Headers[":path"]);
            int i = 0;

            Console.WriteLine("Transfer begin");

            while (binaryFile.Length > i)
            {
                bool isLastData = binaryFile.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                ?
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize, stream.WindowSize)
                                :
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryFile, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            }
        }

        private void SaveToFile(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                var path = stream.Headers[":path"];
                _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                       assemblyPath + path,
                                       stream.ReceivedDataAmount != 0);

                stream.ReceivedDataAmount += dataFrame.FrameLength;

                if (dataFrame.IsFin)
                {
                    _fileHelper.Dispose();
                    Console.WriteLine("File downloaded: " + path);
                    stream.Dispose();
                }
            }
        }

        private void FrameReceivedHandler(object handler, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            try
            {
                if (args.Frame is DataFrame)
                {
                    Task.Run(() => SaveToFile(stream, (DataFrame)args.Frame));
                }

                if (args.Frame is HeadersPlusPriority)
                {
                    Task.Run(() => SendResponce(stream));
                }
            }
            catch (Exception)
            {
                stream.WriteRst(ResetStatusCode.InternalError);

                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (_clientSession != null)
            {
                _clientSession.Dispose();
            }

            _fileHelper.Dispose();
        }
    }
}
