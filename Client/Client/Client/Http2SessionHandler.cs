using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharedProtocol.Compression.Http2DeltaHeadersCompression;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using SharedProtocol;
using SharedProtocol.Compression;
using SharedProtocol.Exceptions;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Extensions;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;
using SharedProtocol.IO;
using SharedProtocol.Pages;
using SharedProtocol.Utils;

namespace Client
{
    public sealed class Http2SessionHandler : IDisposable
    {
        private const string CertificatePath = @"certificate.pfx";
        private const string NotFound = @"NotFound.html";
        private const string ClientSessionHeader = @"PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";

        private SecurityOptions _options;
        private Http2Session _clientSession;

        private SecureSocket _socket;
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly bool _usePriorities;
        private readonly bool _useHandshake;
        private readonly bool _useFlowControl;
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private bool _isDisposed;

        private int _port;
        private string _version;
        private string _scheme;
        private string _host;

        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public event EventHandler<EventArgs> OnClosed;

        public string ServerUri { get; private set; }

        public bool IsHttp2WillBeUsed 
        {
            get { return _useHttp20; }
        }

        public Http2SessionHandler(IDictionary<string, object> environment)
        {
            if (environment["useFlowControl"] is bool)
            {
                _useFlowControl = (bool) environment["useFlowControl"];
            }
            else
            {
                _useFlowControl = true;
            }
            if (environment["usePriorities"] is bool)
            {
                _usePriorities = (bool) environment["usePriorities"];
            }
            else
            {
                _usePriorities = true;
            }
            if (environment["useHandshake"] is bool)
            {
                _useHandshake = (bool) environment["useHandshake"];
            }
            else
            {
                _useHandshake = true;
            }

            _fileHelper = new FileHelper(ConnectionEnd.Client);
        }

        private IDictionary<string, object> MakeHandshakeEnvironment(SecureSocket socket)
        {
            var result = new Dictionary<string, object>
			{
					{":version", _version},
                    {":scheme", _scheme},
                    {":host", _host},
                    {"securityOptions", _options},
                    {"secureSocket", socket},
                    {"end", ConnectionEnd.Client}
			};

            return result;
        }

        public bool Connect(Uri connectUri)
        {
            _version = Protocols.Http1;
            _scheme = connectUri.Scheme;
            _host = connectUri.Host;
            _port = connectUri.Port;
            ServerUri = connectUri.Authority;

            if (_clientSession != null)
            {
                return false;
            }

            try
            {
                int port = connectUri.Port;

                int securePort;

                try
                {
                    securePort = int.Parse(ConfigurationManager.AppSettings["securePort"]);
                }
                catch (Exception)
                {
                    Http2Logger.LogError("Incorrect port in the config file!");
                    return false;
                }

                //Connect alpn extension, set known protocols
                var extensions = new[] {ExtensionType.Renegotiation, ExtensionType.ALPN};

                _options = port == securePort
                               ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http1, Protocols.Http2 },
                                                     ConnectionEnd.Client)
                               : new SecurityOptions(SecureProtocol.None, extensions, new[] { Protocols.Http1, Protocols.Http2 },
                                                     ConnectionEnd.Client);

                _options.VerificationType = CredentialVerification.None;
                _options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(CertificatePath);
                _options.Flags = SecurityFlags.Default;
                _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION;

                _socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, _options);
                IDictionary<string, object> handshakeResult = null;
                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += (o, args) => { _selectedProtocol = args.SelectedProtocol; };
                    _socket.Connect(new DnsEndPoint(connectUri.Host, connectUri.Port), monitor);
                    
                    if (_useHandshake)
                    {
                        var handshakeEnvironment = MakeHandshakeEnvironment(_socket);
                        //Handshake manager determines what handshake must be used: upgrade or secure
                        handshakeResult = HandshakeManager.GetHandshakeAction(handshakeEnvironment).Invoke();

                        Http2Logger.LogDebug("Handshake finished");

                        if (_selectedProtocol == Protocols.Http1)
                        {
                            _useHttp20 = false;
                            return true;
                        }
                    }
                }

                SendSessionHeader();
                _useHttp20 = true;
                _clientSession = new Http2Session(_socket, ConnectionEnd.Client, _usePriorities, _useFlowControl, handshakeResult);

                //For saving incoming data
                _clientSession.OnFrameReceived += FrameReceivedHandler;
                _clientSession.OnRequestSent += RequestSentHandler;
                _clientSession.OnSessionDisposed += (sender, args) => Dispose(false);
            }
            catch (Http2HandshakeFailed ex)
            {
                if (ex.Reason == HandshakeFailureReason.InternalError)
                {
                    _useHttp20 = false;
                }
                else
                {
                    Http2Logger.LogError("Specified server did not respond");
                    Dispose(true);
                    return false;
                }
            }
            catch (SocketException)
            {
                Http2Logger.LogError("Check if any server listens port " + connectUri.Port);
                Dispose(true);
                return false;
            }
            catch (Exception ex)
            {
                Http2Logger.LogError("Unknown connection exception was caught: " + ex.Message);
                Dispose(true);
                return false;
            }

            return true;
        }

        public async void StartConnection()
        {
            if (_useHttp20 && !_socket.IsClosed && !_isDisposed)
            {
                await _clientSession.Start();
            }
            else if (_socket.IsClosed || _isDisposed)
            {
                Http2Logger.LogError("Connection was aborted by the remote side. Check your session header.");
                Dispose(true);
            }
        }

        private void SendSessionHeader()
        {
            _socket.Send(Encoding.UTF8.GetBytes(ClientSessionHeader));
        }

        //localPath should be provided only for post and put cmds
        //serverPostAct should be provided only for post cmd
        private void SubmitRequest(Uri request, string method, string localPath = null, string serverPostAct = null)
        {
            var headers = new List<Tuple<string, string, IAdditionalHeaderInfo>>
                {
                    new Tuple<string, string, IAdditionalHeaderInfo>(":method", method,
                                                                     new Indexation(IndexationType.Indexed)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":path", request.PathAndQuery,
                                                                     new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":version", _version,
                                                                     new Indexation(IndexationType.Incremental)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":host", _host,
                                                                     new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":scheme", _scheme,
                                                                     new Indexation(IndexationType.Substitution)),
                };

            if (!String.IsNullOrEmpty(localPath))
            {
                headers.Add(new Tuple<string, string, IAdditionalHeaderInfo>(":localPath".ToLower(), localPath,
                                                                     new Indexation(IndexationType.Substitution)));
            }

            if (!String.IsNullOrEmpty(serverPostAct))
            {
                headers.Add(new Tuple<string, string, IAdditionalHeaderInfo>(":serverPostAct".ToLower(), serverPostAct,
                                                                     new Indexation(IndexationType.Substitution)));
            }

            //Sending request with average priority
            _clientSession.SendRequest(headers, (int)Priority.Pri3, false);
        }

        public void SendRequestAsync(Uri request, string method, string localPath = null, string serverPostAct = null)
        {
            if (!_socket.IsClosed)
            {
                if (_host != request.Host || _port != request.Port || _scheme != request.Scheme)
                {
                    throw new InvalidOperationException("Trying to send request to non connected address");
                }

                if (_useHttp20 == false)
                {
                    Http2Logger.LogConsole("Download with Http/1.1");

                    //Download with http11 in another thread.
                    Http11Manager.Http11DownloadResource(_socket, request);
                    return;
                }

                //Submit request if http2 was chosen
                Http2Logger.LogConsole("Submitting request");

                //Submit request in the current thread, response will be handled in the session thread.
                SubmitRequest(request, method, localPath, serverPostAct);
            }
        }

        public TimeSpan Ping()
        {
            if (_clientSession != null)
            {
                return Task.Run(() => _clientSession.Ping()).Result;
            }

            return TimeSpan.Zero;
        }

        //Method for future usage in server push 
        private void SendDataTo(Http2Stream stream, byte[] binaryData)
        {
            int i = 0;

            Http2Logger.LogConsole("Transfer begin");

            do
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                    ? MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize,
                                                 stream.WindowSize)
                                    : MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            } while (binaryData.Length > i);

            //It was not send exactly. Some of the data frames could be pushed to the unshipped frames collection
            Http2Logger.LogConsole("File sent: " + stream.Headers.GetValue(":path"));
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                string originalPath = stream.Headers.GetValue(":path".ToLower()); 
                //If user sets the empty file in get command we return notFound webpage
                string fileName = string.IsNullOrEmpty(Path.GetFileName(originalPath)) ? NotFound : Path.GetFileName(originalPath);
                string path = Path.Combine(AssemblyPath, fileName);

                try
                {
                        _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                           path, stream.ReceivedDataAmount != 0);
                }
                catch (IOException)
                {
                    Http2Logger.LogError("File is still downloading. Repeat request later");
                    stream.WriteDataFrame(new byte[0], true);
                    stream.Dispose();
                }

                stream.ReceivedDataAmount += dataFrame.FrameLength;

                if (dataFrame.IsEndStream)
                {
                    if (!stream.EndStreamSent)
                    {
                        //send terminator
                        stream.WriteDataFrame(new byte[0], true);
                        Http2Logger.LogConsole("Terminator was sent");
                    }
                    _fileHelper.RemoveStream(path);
                    Http2Logger.LogConsole("Bytes received " + stream.ReceivedDataAmount);
#if DEBUG
                    const string wayToServerRoot1 = @"..\..\..\..\..\Drop\Root";
                    const string wayToServerRoot2 = @".\Root";
                    var areFilesEqual = _fileHelper.CompareFiles(path, wayToServerRoot1 + originalPath) ||
                                        _fileHelper.CompareFiles(path, wayToServerRoot2 + originalPath);
                    if (!areFilesEqual)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Http2Logger.LogError("Files are NOT EQUAL!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Http2Logger.LogConsole("Files are EQUAL!");
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
#endif
                }
            }
        }

        private void RequestSentHandler(object sender, RequestSentEventArgs args)
        {
            var stream = args.Stream;
            var method = stream.Headers.GetValue(":method");
            if (method == "put" || method == "post")
            {
                var localPath = stream.Headers.GetValue(":localPath".ToLower());
                byte[] binary = null;
                bool gotException = false;
                try
                {
                    binary = _fileHelper.GetFile(localPath);
                }
                catch (FileNotFoundException)
                {
                    gotException = true;
                    Http2Logger.LogError("Specified file not found: " + localPath);
                }
                if (!gotException)
                {
                    SendDataTo(args.Stream, binary);
                }
            }
        }

        private void FrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var method = stream.Headers.GetValue(":method").ToLower();

            try
            {
                switch (method)
                {
                    case "dir":
                    case "get":
                        if (args.Frame is DataFrame)
                        {
                            SaveDataFrame(stream, (DataFrame) args.Frame);
                        }
                        else if (args.Frame is Headers)
                        {
                            string path = stream.Headers.GetValue(":path".ToLower());
                            byte[] binary;

                            try
                            {
                                binary = _fileHelper.GetFile(path);
                            }
                            catch (FileNotFoundException)
                            {
                                binary = new NotFound404().Bytes;
                            }
                            SendDataTo(stream, binary);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                stream.WriteRst(ResetStatusCode.InternalError);
                stream.Dispose();
            }
        }

        public void Dispose(bool wasErrorOccured)
        {
            Dispose();

            if (wasErrorOccured && OnClosed != null)
            {
                OnClosed(this, null);
            }

            OnClosed = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_clientSession != null)
            {
                _clientSession.Dispose();
            }
            if (_socket != null && !_socket.IsClosed)
            {
                _socket.Close();
            }
            if (_fileHelper != null)
            {
                _fileHelper.Dispose();
            }

            _isDisposed = true;
        }
    }
}
