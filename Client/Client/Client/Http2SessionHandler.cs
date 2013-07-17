using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharedProtocol.Http2HeadersCompression;
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

namespace Client
{
    public sealed class Http2SessionHandler : IDisposable
    {
        private SecurityOptions _options;
        private Http2Session _clientSession;
        private const string _certificatePath = @"certificate.pfx";
        private const string _notFound = @"notFound.html";
        private SecureSocket _socket;
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly bool _usePriorities;
        private readonly bool _useHandshake;
        private readonly bool _useFlowControl;
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private const string _clientSessionHeader = @"FOO * HTTP/2.0\r\n\r\nBA\r\n\r\n";

        private int _port;
        private string _version;
        private string _scheme;
        private string _host;

        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public bool IsHttp2WillBeUsed {
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
            _version = "http/1.1";
            _scheme = connectUri.Scheme;
            _host = connectUri.Host;
            _port = connectUri.Port;

            bool gotException = false;

            if (_clientSession != null)
            {
                return gotException;
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
                    gotException = true;
                    Console.WriteLine("Incorrect port in the config file!");
                    return gotException;
                }

                //Connect alpn extension, set known protocols
                var extensions = new[] {ExtensionType.Renegotiation, ExtensionType.ALPN};

                _options = port == securePort
                               ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] {"http/2.0", "http/1.1"},
                                                     ConnectionEnd.Client)
                               : new SecurityOptions(SecureProtocol.None, extensions, new[] {"http/2.0", "http/1.1"},
                                                     ConnectionEnd.Client);

                _options.VerificationType = CredentialVerification.None;
                _options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(_certificatePath);
                _options.Flags = SecurityFlags.Default;
                _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION;

                var sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, _options);

                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += (o, args) => { _selectedProtocol = args.SelectedProtocol; };
                    sessionSocket.Connect(new DnsEndPoint(connectUri.Host, connectUri.Port), monitor);

                    if (_useHandshake)
                    {
                        var handshakeEnvironment = MakeHandshakeEnvironment(sessionSocket);
                        //Handshake manager determines what handshake must be used: upgrade or secure
                        HandshakeManager.GetHandshakeAction(handshakeEnvironment).Invoke();

                        Console.WriteLine("Handshake finished");

                        if (_selectedProtocol == "http/1.1")
                        {
                            _useHttp20 = false;
                            return gotException;
                        }
                    }
                }

                _socket = sessionSocket;
                SendSessionHeader();
                _useHttp20 = true;
                _clientSession = new Http2Session(_socket, ConnectionEnd.Client, _usePriorities, _useFlowControl);

                //For saving incoming data
                _clientSession.OnFrameReceived += FrameReceivedHandler;
                _clientSession.OnRequestSent += RequestSentHandler;
            }
            catch (Http2HandshakeFailed)
            {
                _useHttp20 = false;
            }
            catch (SocketException)
            {
                gotException = true;
                Console.WriteLine("Check if any server listens port " + connectUri.Port);
                Dispose();
                return gotException;
            }
            catch (Exception ex)
            {
                gotException = true;
                Console.WriteLine("Unknown connection exception was caught: " + ex.Message);
                Dispose();
                return gotException;
            }

            return gotException;
        }

        public async void StartConnection()
        {
            await _clientSession.Start();
        }

        private void SendSessionHeader()
        {
            _socket.Send(Encoding.UTF8.GetBytes(_clientSessionHeader));
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
            if (_host != request.Host || _port != request.Port || _scheme != request.Scheme)
            {
                throw new InvalidOperationException("Trying to send request to non connected address");
            }

            if (_useHttp20 == false)
            {
                Console.WriteLine("Download with Http/1.1");

                //Download with http11 in another thread.
                Task.Run(() => Http11Manager.Http11DownloadResource(_socket, request));
                return;
            }

            //Submit request if http2 was chosen
            Console.WriteLine("Submitting request");

            //Submit request in the current thread, responce will be handled in the session thread.
            SubmitRequest(request, method, localPath, serverPostAct);
        }

        public TimeSpan Ping()
        {
            if (_clientSession != null)
            {
                return Task.Run(() => _clientSession.Ping()).Result;
            }

            return default(TimeSpan);
        }

        //Method for future usage in server push 
        private void SendDataTo(Http2Stream stream, byte[] binaryData)
        {
            int i = 0;

            Console.WriteLine("Transfer begin");

            while (binaryData.Length > i)
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                ?
                                    MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize, stream.WindowSize)
                                :
                                    MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            }

            //It was not send exactly. Some of the data frames could be pushed to the unshipped frames collection
            Console.WriteLine("File sent: " + stream.Headers.GetValue(":path"));
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                string path = stream.Headers.GetValue(":path".ToLower());

                try
                {
                    if (dataFrame.Data.Count != 0)
                    {
                        _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                               assemblyPath + path, stream.ReceivedDataAmount != 0);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("File is still downloading. Repeat request later");
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
                        Console.WriteLine("Terminator was sent");
                    }
                    _fileHelper.RemoveStream(assemblyPath + path);
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
                Task.Run(() =>
                    {
                        byte[] binary = null;
                        bool gotException = false;
                        try
                        {
                            binary = _fileHelper.GetFile(localPath);
                        }
                        catch (FileNotFoundException)
                        {
                            gotException = true;
                            Console.WriteLine("Specified file not found: {0}", localPath);
                        }
                        if (!gotException)
                        {
                            SendDataTo(args.Stream, binary);
                        }
                    });
            }
        }

        private void FrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var method = stream.Headers.GetValue(":method");

            try
            {
                if (args.Frame is DataFrame)
                {
                    Task.Run(() => SaveDataFrame(stream, (DataFrame)args.Frame));
                }
                else if (args.Frame is Headers && method == "get")
                {
                    Task.Run(() =>
                    {
                        string path = stream.Headers.GetValue(":path".ToLower());
                        byte[] binary = null;

                        try
                        {
                            binary = _fileHelper.GetFile(path);
                        }
                        catch (FileNotFoundException)
                        {
                            binary = new NotFound404().Bytes;
                        }
                        SendDataTo(stream, binary);
                    });
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
            if (_socket != null)
            {
                _socket.Close();
            }
            if (_fileHelper != null)
            {
                _fileHelper.Dispose();
            }
        }
    }
}
