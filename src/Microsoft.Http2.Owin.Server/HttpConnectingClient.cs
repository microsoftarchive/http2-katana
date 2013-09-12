// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Owin.Server.Adapters;
using Org.Mentalis;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Security.Ssl;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// This class handles incoming clients. It can accept them, make handshake and choose how to give a response.
    /// It encouraged to response with http11 or http20 
    /// </summary>
    internal sealed class HttpConnectingClient : IDisposable
    {
        private readonly SecureTcpListener _server;
        private readonly SecurityOptions _options;
        private readonly AppFunc _next;
        private readonly IDictionary<string, object> _environment;        
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly CancellationTokenSource _cancelClientHandling;
        private bool _isDisposed;

        internal HttpConnectingClient(SecureTcpListener server, SecurityOptions options,
                                     AppFunc next, bool useHandshake, bool usePriorities, 
                                     bool useFlowControl, IDictionary<string, object> environment)
        {
            _isDisposed = false;
            _environment = environment;
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            _server = server;
            _next = next;
            _options = options;
            _cancelClientHandling = new CancellationTokenSource();
        }

        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal void Accept(CancellationToken cancel)
        {
            SecureSocket incomingClient;

            var monitor = new ALPNExtensionMonitor();
            try
            {
                incomingClient = _server.AcceptSocket(cancel, monitor);
            }
            catch (OperationCanceledException)
            {
                Http2Logger.LogDebug("Listen was cancelled");
                return;
            }  
            Http2Logger.LogDebug("New connection accepted");
            Task.Run(() => HandleAcceptedClient(incomingClient, monitor));
        }

        private void HandleAcceptedClient(SecureSocket incomingClient, ALPNExtensionMonitor monitor)
        {
            bool backToHttp11 = false;
            string selectedProtocol = Protocols.Http1;
            var environmentCopy = new Dictionary<string, object>(_environment);
            
            if (_useHandshake)
            {
                try
                {
                    if (_options.Protocol != SecureProtocol.None)
                    {
                        incomingClient.MakeSecureHandshake(_options);
                        selectedProtocol = incomingClient.SelectedProtocol;
                    }

                    // TODO investigate why selectedProtocol is null after Handshake;
                    /*if (selectedProtocol == null)
                    {
                        selectedProtocol = Protocols.Http1;
                        backToHttp11 = true;
                    }*/
                }
                catch (SecureHandshakeException ex)
                {
                    switch (ex.Reason)
                    {
                        case SecureHandshakeFailureReason.HandshakeInternalError:
                            backToHttp11 = true;
                            break;
                        case SecureHandshakeFailureReason.HandshakeTimeout:
                            incomingClient.Close();
                            Http2Logger.LogError("Handshake timeout. Client was disconnected.");
                            return;
                        default:
                            incomingClient.Close();
                            Http2Logger.LogError("Unknown error occurred during secure handshake");
                            return;
                    }
                }
                catch (Exception e)
                {
                    Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                    incomingClient.Close();
                    return;
                }
            }

            var clientStream = new DuplexStream(incomingClient, true);
            var transportInfo = GetTransportInfo(incomingClient);

            monitor.Dispose();

            try
            {
                HandleRequest(clientStream, selectedProtocol, transportInfo, backToHttp11, environmentCopy);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(DuplexStream incomingClient, string alpnSelectedProtocol, 
                                   TransportInformation transportInformation,
                                   bool backToHttp11, IDictionary<string, object> environment)
        {
            //Server checks selected protocol and calls http2 or http11 layer
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Ssl chose http11");

                new Http11ProtocolOwinAdapter(incomingClient, _options.Protocol, _next).ProcessRequest();
                return;
            }

            //ALPN selected http2. No need to perform upgrade handshake.
            OpenHttp2Session(incomingClient, transportInformation, environment);
        }

        private async void OpenHttp2Session(DuplexStream incomingClientStream, 
                                            TransportInformation transportInformation,
                                            IDictionary<string, object> environment)
        {
            Http2Logger.LogDebug("Handshake successful");
            using (var messageHandler = new Http2OwinMessageHandler(incomingClientStream, ConnectionEnd.Server, transportInformation, _next, _cancelClientHandling.Token))
            {
                try
                {
                    await messageHandler.ProcessRequestAsync();
                }
                catch (Exception)
                {
                    Http2Logger.LogError("Client was disconnected");
                }
            }
        }

        private TransportInformation GetTransportInfo(SecureSocket incomingClient)
        {
            var localEndPoint = (IPEndPoint)incomingClient.LocalEndPoint;
            var remoteEndPoint = (IPEndPoint)incomingClient.RemoteEndPoint;

            var transportInfo = new TransportInformation
            {
                LocalPort = localEndPoint.Port,
                RemotePort = remoteEndPoint.Port,
            };

            // Side effect of using dual mode sockets, the IPv4 addresses look like 0::ffff:127.0.0.1.
            if (localEndPoint.Address.IsIPv4MappedToIPv6)
            {
                transportInfo.LocalIpAddress = localEndPoint.Address.MapToIPv4().ToString();
            }
            else
            {
                transportInfo.LocalIpAddress = localEndPoint.Address.ToString();
            }

            if (remoteEndPoint.Address.IsIPv4MappedToIPv6)
            {
                transportInfo.RemoteIpAddress = remoteEndPoint.Address.MapToIPv4().ToString();
            }
            else
            {
                transportInfo.RemoteIpAddress = remoteEndPoint.Address.ToString();
            }

            return transportInfo;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }
    }
}
