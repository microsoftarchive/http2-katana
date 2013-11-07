// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using OpenSSL;
using OpenSSL.SSL;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// This class handles incoming clients. It can accept them, make handshake and choose how to give a response.
    /// It encouraged to response with http11 or http20 
    /// </summary>
    internal sealed class HttpConnectingClient : IDisposable
    {
        private readonly TcpListener _server;
        private readonly AppFunc _next;     
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly CancellationTokenSource _cancelClientHandling;
        private bool _isDisposed;

        internal HttpConnectingClient(TcpListener server, AppFunc next, 
                                     bool useHandshake, bool usePriorities, bool useFlowControl)
        {
            _isDisposed = false;
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            _server = server;
            _next = next;
            _cancelClientHandling = new CancellationTokenSource();
        }

        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal void Accept(CancellationToken cancel)
        {
            TcpClient incomingClient;

            try
            {
                incomingClient = _server.AcceptTcpClient();
            }
            catch (OperationCanceledException)
            {
                Http2Logger.LogDebug("Listen was cancelled");
                return;
            }  
            Http2Logger.LogDebug("New connection accepted");
            Task.Run(() => HandleAcceptedClient(incomingClient.GetStream()));
        }

        private void HandleAcceptedClient(Stream incomingClient)
        {
            bool backToHttp11 = false;
            string selectedProtocol = Protocols.Http1;
            
            if (_useHandshake)
            {
                try
                {

                }
                catch (Exception e)
                {
                    Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                    incomingClient.Close();
                    return;
                }
            }

            var clientStream = new SslStream(incomingClient, false);

            try
            {
                HandleRequest(clientStream, selectedProtocol, backToHttp11);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(Stream incomingClient, string alpnSelectedProtocol, bool backToHttp11)
        {
            //Server checks selected protocol and calls http2 or http11 layer
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Ssl chose http11");

                new Http11ProtocolOwinAdapter(incomingClient, SslProtocols.Tls, _next).ProcessRequest();
                return;
            }

            //ALPN selected http2. No need to perform upgrade handshake.
            OpenHttp2Session(incomingClient);
        }

        private async void OpenHttp2Session(Stream incomingClientStream)
        {
            Http2Logger.LogDebug("Handshake successful");
            using (var messageHandler = new Http2OwinMessageHandler(incomingClientStream, ConnectionEnd.Server, 
                                                                    incomingClientStream is SslStream, _next, 
                                                                    _cancelClientHandling.Token))
            {
                try
                {
                    await messageHandler.StartSessionAsync();
                }
                catch (Exception)
                {
                    Http2Logger.LogError("Client was disconnected");
                }
            }

            GC.Collect();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }
    }
}
