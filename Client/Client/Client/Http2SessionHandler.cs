using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Client.Handshake;
using Client.Handshake.Exceptions;
using Microsoft.Http2.Protocol;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using ProtocolAdapters;
using Org.Mentalis.Security;

namespace Client
{
    /// <summary>
    /// This class expresses client's logic.
    /// It can create client socket, accept server responses, make handshake and choose how to send requests to server.
    /// </summary>
    public sealed class Http2SessionHandler : IDisposable
    {
        #region Fields
        private Http2ClientProtocolAdapter _sessionAdapter;
        private const string CertificatePath = @"certificate.pfx";
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly bool _usePriorities;
        private readonly bool _useHandshake;
        private readonly bool _useFlowControl;
        private bool _isDisposed;
        private string _path;
        private int _port;
        private string _version;
        private string _scheme;
        private string _host;
        private readonly IDictionary<string, object> _environment; 
        #endregion

        #region Events

        /// <summary>
        /// Session closed event.
        /// </summary>
        public event EventHandler<EventArgs> OnClosed;

        #endregion

        #region Properties

        public string ServerUri { get; private set; }

        public SecurityOptions Options { get; private set; }

        public bool IsHttp2WillBeUsed 
        {
            get { return _useHttp20; }
        }

        #endregion

        #region Methods

        public Http2SessionHandler(IDictionary<string, object> environment)
        {
            _environment = new Dictionary<string, object>();
            //Copy environment
            _environment.AddRange(environment);
            if (_environment["useFlowControl"] is bool)
            {
                _useFlowControl = (bool) environment["useFlowControl"];
            }
            else
            {
                _useFlowControl = true;
            }
            if (_environment["usePriorities"] is bool)
            {
                _usePriorities = (bool) environment["usePriorities"];
            }
            else
            {
                _usePriorities = true;
            }
            if (_environment["useHandshake"] is bool)
            {
                _useHandshake = (bool) environment["useHandshake"];
            }
            else
            {
                _useHandshake = true;
            }
        }

        private void MakeHandshakeEnvironment(SecureSocket socket)
        {
            _environment.AddRange(new Dictionary<string, object>
			{
                    {":path", _path},
					{":version", _version},
                    {":scheme", _scheme},
                    {":host", _host},
                    {"securityOptions", Options},
                    {"secureSocket", socket},
                    {"end", ConnectionEnd.Client}
			});
        }

        public bool Connect(Uri connectUri)
        {
            _path = connectUri.PathAndQuery;
            _version = Protocols.Http2;
            _scheme = connectUri.Scheme;
            _host = connectUri.Host;
            _port = connectUri.Port;
            ServerUri = connectUri.Authority;

            if (_sessionAdapter != null)
            {
                return false;
            }

            try
            {
                int port = connectUri.Port;

                int securePort;

                if (!int.TryParse(ConfigurationManager.AppSettings["securePort"], out securePort))
                {
                    Http2Logger.LogError("Incorrect port in the config file!");
                    return false;
                }


                //Connect alpn extension, set known protocols
                var extensions = new[] {ExtensionType.Renegotiation, ExtensionType.ALPN};

                Options = port == securePort
                               ? new SecurityOptions(SecureProtocol.Tls1, extensions, new[] { Protocols.Http1, Protocols.Http2 },
                                                     ConnectionEnd.Client)
                               : new SecurityOptions(SecureProtocol.None, extensions, new[] { Protocols.Http1, Protocols.Http2 },
                                                     ConnectionEnd.Client);

                Options.VerificationType = CredentialVerification.None;
                Options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(CertificatePath);
                Options.Flags = SecurityFlags.Default;
                Options.AllowedAlgorithms = SslAlgorithms.RSA_AES_256_SHA | SslAlgorithms.NULL_COMPRESSION;

                var socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, Options);
                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += (o, args) => { _selectedProtocol = args.SelectedProtocol; };
                    socket.Connect(new DnsEndPoint(connectUri.Host, connectUri.Port), monitor);
                    
                    if (_useHandshake)
                    {
                        MakeHandshakeEnvironment(socket);
                        //Handshake manager determines what handshake must be used: upgrade or secure

                        if (socket.SecureProtocol != SecureProtocol.None)
                        {
                            socket.MakeSecureHandshake(Options);
                        }

                        Http2Logger.LogDebug("Handshake finished");

                        if (_selectedProtocol == Protocols.Http1)
                        {
                            //TODO Handle double handshake
                            var handshakeResult = new UpgradeHandshaker(_environment).Handshake();
                            _environment.Add("HandshakeResult", handshakeResult);
                            _useHttp20 = false;
                            return true;
                        }
                    }
                }

                _useHttp20 = true;
                var stream = new DuplexStream(socket, true);

                //TODO provide transport info
                _sessionAdapter = new Http2ClientProtocolAdapter(stream, default(TransportInformation), CancellationToken.None);
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
            if (_useHttp20 && !_sessionAdapter.IsDisposed && !_isDisposed)
            {
                await _sessionAdapter.StartSession();
            }
            else if (_sessionAdapter.IsDisposed)
            {
                Http2Logger.LogError("Connection was aborted by the remote side. Check your session header.");
                Dispose(true);
            }
        }

        //localPath should be provided only for post and put cmds
        //serverPostAct should be provided only for post cmd
        private void SubmitRequest(Uri request, string method, string localPath = null, string serverPostAct = null)
        {
            var headers = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", method),
                    new KeyValuePair<string, string>(":path", request.PathAndQuery),
                    new KeyValuePair<string, string>(":version", _version),
                    new KeyValuePair<string, string>(":host", _host),
                    new KeyValuePair<string, string>(":scheme", _scheme),
                };

            if (!String.IsNullOrEmpty(localPath))
            {
                headers.Add(new KeyValuePair<string, string>(":localPath".ToLower(), localPath));
            }

            if (!String.IsNullOrEmpty(serverPostAct))
            {
                headers.Add(new KeyValuePair<string, string>(":serverPostAct".ToLower(), serverPostAct));
            }

            //Sending request with average priority
            _sessionAdapter.SendRequest(headers, Priority.None, false);
        }

        public void SendRequestAsync(Uri request, string method, string localPath = null, string serverPostAct = null)
        {
            if (!_sessionAdapter.IsDisposed)
            {
                if (_host != request.Host || _port != request.Port || _scheme != request.Scheme)
                {
                    throw new InvalidOperationException("Trying to send request to non connected address");
                }

                if (_useHttp20 == false)
                {
                    Http2Logger.LogConsole("Download with Http/1.1");

                    //Download with http11 in another thread.
                    //Http11Manager.Http11DownloadResource(_socket, request);
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
            if (_sessionAdapter != null)
            {
                return Task.Run(new Func<TimeSpan>(_sessionAdapter.Ping)).Result;
            }

            return TimeSpan.Zero;
        }

        /*private void SendDataTo(Http2Stream stream, byte[] binaryData)
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
                string fileName = string.IsNullOrEmpty(Path.GetFileName(originalPath)) ? Index : Path.GetFileName(originalPath);
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
        }*/

        /*private void RequestSentHandler(object sender, RequestSentEventArgs args)
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
                        else if (args.Frame is HeadersFrame)
                        {
                            Http2Logger.LogConsole("Headers received for stream: " + args.Frame.StreamId + " status:" + ((HeadersFrame)args.Frame).Headers.GetValue(":status"));
                        }
                        break;
                }
            }
            catch (Exception)
            {
                stream.WriteRst(ResetStatusCode.InternalError);
                stream.Dispose();
            }
        }*/

        public void Dispose(bool wasErrorOccurred)
        {
            Dispose();

            if (wasErrorOccurred && OnClosed != null)
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

            if (_sessionAdapter != null)
            {
                _sessionAdapter.Dispose();
            }

            _isDisposed = true;
        }

        #endregion
    }
}
