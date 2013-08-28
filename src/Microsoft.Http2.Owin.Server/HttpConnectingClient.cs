// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Owin.Types;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.Extensions;
using SharedProtocol.Handshake;
using SharedProtocol.IO;
using SharedProtocol.Utils;

namespace SocketServer
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
        private Http2Session _session;
        private readonly IDictionary<string, object> _environment;        
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;

        internal HttpConnectingClient(SecureTcpListener server, SecurityOptions options,
                                     AppFunc next, bool useHandshake, bool usePriorities, 
                                     bool useFlowControl, IDictionary<string, object> environment)
        {
            _environment = environment;
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            _server = server;
            _next = next;
            _options = options;
        }

        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal void Accept(CancellationToken cancel)
        {
            SecureSocket incomingClient = null;

            using (var monitor = new ALPNExtensionMonitor())
            {
                try
                {
                    incomingClient = _server.AcceptSocket(cancel, monitor);
                }
                catch (OperationCanceledException ex)
                {
                    Http2Logger.LogDebug("Listen was cancelled");
                    return;
                }  
            }
            Http2Logger.LogDebug("New connection accepted");
            Task.Run(() => HandleAcceptedClient(incomingClient));
        }

        private void HandleAcceptedClient(SecureSocket incomingClient)
        {
            bool backToHttp11 = false;
            string selectedProtocol = Protocols.Http2;
            var environmentCopy = new Dictionary<string, object>(_environment);
            
            if (_useHandshake)
            {
                try
                {
                    if (_options.Protocol != SecureProtocol.None)
                    {
                        incomingClient.MakeSecureHandshake(_options);
                    }

                    selectedProtocol = incomingClient.SelectedProtocol;

                    // TODO investigate why selectedProtocol is null after Handshake;
                    if (selectedProtocol == null)
                    {
                        selectedProtocol = Protocols.Http1;
                        backToHttp11 = true;
                    }
                }
                catch (Http2HandshakeFailed ex)
                {
                    if (ex.Reason == HandshakeFailureReason.InternalError)
                    {
                        backToHttp11 = true;
                    }
                    else
                    {
                        incomingClient.Close();
                        Http2Logger.LogError("Handshake timeout. Client was disconnected.");
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

            try
            {
                HandleRequest(clientStream, selectedProtocol, backToHttp11, environmentCopy);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(DuplexStream incomingClient, string alpnSelectedProtocol, 
                                   bool backToHttp11, IDictionary<string, object> environment)
        {
            //Server checks selected protocol and calls http2 or http11 layer
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Ssl chose http11");
                
                //Http11 should get initial headers (they can contain upgrade) 
                //after it got headers it should call middleware. 
                //Environment should contain upgrade delegate
                //Http11Manager.Http11SendResponse(incomingClient);
                Http11ProtocolOwinAdapter.ProcessRequest(incomingClient, environment, _next);
                return;
            }

            //ALPN selected http2. No need to perform upgrade handshake.
            OpenHttp2Session(incomingClient, environment);
        }

        private async void OpenHttp2Session(DuplexStream incomingClient, IDictionary<string, object> environment)
        {
            Http2Logger.LogDebug("Handshake successful");
            _session = new Http2Session(incomingClient, ConnectionEnd.Server, _usePriorities, _useFlowControl, _next, environment);

            try
            {
                await _session.Start();
            }
            catch (Exception)
            {
                Http2Logger.LogError("Client was disconnected");
            }
        }

        public void Dispose()
        {
            if (_session != null)
            {
                _session.Dispose();
            }
        }
    }
}
