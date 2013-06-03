using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;

namespace Client
{
    public sealed class Http2SessionHandler : IDisposable
    {
        private const int HttpsPort = 8443;
        private const int HttpPort = 8080;
        private SecurityOptions _options;
        private Http2Session _clientSession;
        private string _certificatePath = @"certificate.pfx";
        private Uri _requestUri;
        private SecureSocket _socket;
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        
        public bool IsHttp2WillBeUsed {
            get { return _useHttp20; }
        }

        public Http2SessionHandler(Uri requestUri)
        {
            _requestUri = requestUri;
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

                ExtensionType[] extensions = new [] { ExtensionType.Renegotiation, ExtensionType.ALPN };

                switch (port)
                {
                    case HttpsPort:
                        _options = new SecurityOptions(SecureProtocol.Tls1, extensions, ConnectionEnd.Client);
                        break;
                    default:
                        _options = new SecurityOptions(SecureProtocol.None, extensions, ConnectionEnd.Client);
                        return;
                }

                _options.VerificationType = CredentialVerification.None;
                _options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(_certificatePath);
                _options.Flags = SecurityFlags.Default;
                _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

                sessionSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream,
                                                 ProtocolType.Tcp, _options);

                using (var monitor = new ALPNExtensionMonitor())
                {
                    monitor.OnProtocolSelected += ProtocolSelectedHandler;
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
                throw;
            }
        }

        private void ProtocolSelectedHandler(object sender, ProtocolSelectedArgs args)
        {
            _selectedProtocol = args.SelectedProtocol;
        }

        private Http2Stream SubmitRequest()
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>(10);
            string method = "GET";
            string path = _requestUri.PathAndQuery;
            string version = "HTTP/2.0";
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

        public void Dispose()
        {
            if (_clientSession != null)
            {
                _clientSession.Dispose();
            }
        }
    }
}
