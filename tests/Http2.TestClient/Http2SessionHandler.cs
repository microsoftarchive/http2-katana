using Http2.TestClient.Adapters;
using Http2.TestClient.Handshake;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using Org.Mentalis;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Http2.TestClient
{
    /// <summary>
    /// This class expresses client's logic.
    /// It can create client socket, accept server responses, make handshake and choose how to send requests to server.
    /// </summary>
    public sealed class Http2SessionHandler : IDisposable
    {
        #region Fields
        private Http2ClientMessageHandler _sessionAdapter;
        private DuplexStream _clientStream;
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

        public bool WasHttp1Used 
        {
            get { return !_useHttp20; }
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
                    {"stream", _clientStream},
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

                    _clientStream = new DuplexStream(socket, true);

                    if (_useHandshake)
                    {
                        MakeHandshakeEnvironment(socket);
                        //Handshake manager determines what handshake must be used: upgrade or secure

                        if (socket.SecureProtocol != SecureProtocol.None)
                        {
                            socket.MakeSecureHandshake(Options);
                            _selectedProtocol = socket.SelectedProtocol;
                        }

                        if (socket.SecureProtocol == SecureProtocol.None || _selectedProtocol == Protocols.Http1)
                        {
                            try
                            {
                                var handshakeResult = new UpgradeHandshaker(_environment).Handshake();
                                _environment.Add("HandshakeResult", handshakeResult);
                                _useHttp20 = handshakeResult["handshakeSuccessful"] as string == "true";

                                if (!_useHttp20)
                                {
                                    Dispose(false);
                                    return true;
                                }
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
                        }
                    }
                }

                Http2Logger.LogDebug("Handshake finished");

                if (_useHttp20)
                {
                    //TODO provide transport info
                    _sessionAdapter = new Http2ClientMessageHandler(_clientStream, ConnectionEnd.Client, default(TransportInformation),
                                                                     CancellationToken.None);
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
                string initialPath = String.Empty;
                Dictionary<string, string> initialRequest = null;
                if (!_clientStream.IsSecure)
                {
                    initialRequest = new Dictionary<string,string>
                        {
                            {":path", _path},
                        };
                }
 
                await _sessionAdapter.StartSessionAsync(initialRequest);
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
                //Sending request with default  priority
            _sessionAdapter.SendRequest(headers, Constants.DefaultStreamPriority, false);
        }

        public void SendRequestAsync(Uri request, string method, string localPath = null, string serverPostAct = null)
        {
            if (!_sessionAdapter.IsDisposed)
            {
                if (_host != request.Host || _port != request.Port || _scheme != request.Scheme)
                {
                    throw new InvalidOperationException("Trying to send request to non connected address");
                }

                if (!_useHttp20)
                {
                    Http2Logger.LogConsole("Download with Http/1.1");
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

        public void Dispose(bool wasErrorOccurred)
        {
            Dispose();

            if (OnClosed != null)
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

            if (_clientStream != null)
            {
                _clientStream.Dispose();
            }

            _isDisposed = true;
        }

        #endregion
    }
}
