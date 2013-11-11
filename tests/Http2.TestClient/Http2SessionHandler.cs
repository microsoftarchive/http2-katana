using System.IO;
using System.Reflection;
using Http2.TestClient.Adapters;
using Http2.TestClient.Handshake;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.SSL;
using OpenSSL.X509;

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
        private Stream _clientStream;
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private const string CertificatePath = @"\certificate.pfx";
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly bool _usePriorities;
        private readonly bool _useHandshake;
        private readonly bool _useFlowControl;
        private bool _isSecure;
        private bool _isDisposed;
        private string _path;
        private int _port;
        private string _version;
        private string _scheme;
        private string _host;
        private readonly IDictionary<string, object> _environment;

        private X509Chain _chain;
        private X509Certificate _certificate;

        #endregion

        #region Events

        /// <summary>
        /// Session closed event.
        /// </summary>
        public event EventHandler<EventArgs> OnClosed;

        #endregion

        #region Properties

        public string ServerUri { get; private set; }

        public bool WasHttp1Used 
        {
            get { return !_useHttp20; }
        }

        public SslProtocols Protocol { get; private set; }

        #endregion

        #region Methods

        private X509Certificate LoadPKCS12Certificate(string certFilename, string password)
        {
            using (BIO certFile = BIO.File(certFilename, "r"))
            {
                return X509Certificate.FromPKCS12(certFile, password);
            }
        }

        public Http2SessionHandler(IDictionary<string, object> environment)
        {
            Protocol = SslProtocols.None;
            
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

        private void MakeHandshakeEnvironment()
        {
            _environment.AddRange(new Dictionary<string, object>
			{
                {CommonHeaders.Path, _path},
		        {CommonHeaders.Version, _version},
                {CommonHeaders.Scheme, _scheme},
                {CommonHeaders.Host, _host},
                {HandshakeKeys.Stream, _clientStream},
                {HandshakeKeys.ConnectionEnd, ConnectionEnd.Client}
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
                _isSecure = port == securePort;

                //var socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, Options);
                var tcpClnt = new TcpClient(connectUri.Host, port);

                _clientStream = tcpClnt.GetStream();

                if (_useHandshake)
                {
                    if (_isSecure)
                    {
                        _clientStream = new SslStream(_clientStream, true);
                        _certificate = LoadPKCS12Certificate(AssemblyName + CertificatePath, String.Empty);

                        _chain = new X509Chain {_certificate};
                        var certList = new X509List { _certificate };
                        
                        (_clientStream as SslStream).AuthenticateAsClient(connectUri.AbsoluteUri/*, certList, _chain,
                                                                          SslProtocols.Tls, SslStrength.All, false*/);
                        
                        _selectedProtocol = (_clientStream as SslStream).AlpnSelectedProtocol;
                    }

                    if (!_isSecure || _selectedProtocol == Protocols.Http1)
                    {
                        MakeHandshakeEnvironment();
                        try
                        {
                            var handshakeResult = new UpgradeHandshaker(_environment).Handshake();
                            _environment.Add(HandshakeKeys.Result, handshakeResult);
                            _useHttp20 = handshakeResult[HandshakeKeys.Successful] as string == HandshakeKeys.True;

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

                Http2Logger.LogDebug("Handshake finished");

                Protocol = _isSecure ? SslProtocols.Tls : SslProtocols.None;

                if (_useHttp20)
                {
                    _sessionAdapter = new Http2ClientMessageHandler(_clientStream, ConnectionEnd.Client, _isSecure, CancellationToken.None);
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
                Dictionary<string, string> initialRequest = null;
                if (!_isSecure)
                {
                    initialRequest = new Dictionary<string,string>
                        {
                            {CommonHeaders.Path, _path},
                        };
                }
 
                await _sessionAdapter.StartSessionAsync(initialRequest);

                GC.Collect();
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
                    new KeyValuePair<string, string>(CommonHeaders.Method, method),
                    new KeyValuePair<string, string>(CommonHeaders.Path, request.PathAndQuery),
                    new KeyValuePair<string, string>(CommonHeaders.Version, _version),
                    new KeyValuePair<string, string>(CommonHeaders.Host, _host),
                    new KeyValuePair<string, string>(CommonHeaders.Scheme, _scheme),
                };

            //Put and post handling
            /*if (!String.IsNullOrEmpty(localPath))
            {
                headers.Add(new KeyValuePair<string, string>(":localPath".ToLower(), localPath));
            }

            if (!String.IsNullOrEmpty(serverPostAct))
            {
                headers.Add(new KeyValuePair<string, string>(":serverPostAct".ToLower(), serverPostAct));
            }*/
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
                _clientStream = null;
            }

            if (_certificate != null)
            {
                _certificate.Dispose();
                _certificate = null;
            }

            if (_chain != null)
            {
                _chain.Dispose();
                _chain = null;
            }

            _isDisposed = true;
        }

        #endregion
    }
}
