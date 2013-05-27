// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using ServerProtocol;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal sealed class HttpConnetingClient
    {
        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private SecureTcpListener server;
        private SecurityOptions options;
        private AppFunc next;
        private string _alpnSelectedProtocol;

        public string SelectedProtocol { get; private set; }
        public SecureSocket InternalSocket { get; private set; }

        internal HttpConnetingClient(SecureTcpListener server, SecurityOptions options, AppFunc next)
        {
            this.SelectedProtocol = String.Empty;
            this.server = server;
            this.next = next;
            this.options = options;
        }

        internal async void Accept()
        {
            bool backToHttp11 = false;
            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += ProtocolSelectedHandler;

                this.InternalSocket = server.AcceptSocket(monitor);
                Console.WriteLine("New client accepted");

                IDictionary<string, object> environment = new Dictionary<string, object>();

                environment.Add("HandshakeAction",
                                HandshakeManager.GetHandshakeAction(this.InternalSocket, this.options));

                try
                {
                    await next(environment);
                }
                catch (HTTP2HandshakeFailed)
                {
                    backToHttp11 = true;
                }

            }
            HandleRequest(backToHttp11);
        }

        private void HandleRequest(bool backToHttp11)
        {
            if (backToHttp11 || _alpnSelectedProtocol == "http/1.1")
            {
                Console.WriteLine("Sending with http11");
                Http11Manager.Http11SendResponse(this.InternalSocket);
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                OpenHttp2Session();
            });
        }

        private TransportInformation GetSocketTranspInfo()
        {
            IPEndPoint localEndPoint = (IPEndPoint)InternalSocket.LocalEndPoint;
            IPEndPoint remoteEndPoint = (IPEndPoint)InternalSocket.RemoteEndPoint;

            TransportInformation transportInfo = new TransportInformation()
            {
                LocalPort = localEndPoint.Port.ToString(CultureInfo.InvariantCulture),
                RemotePort = remoteEndPoint.Port.ToString(CultureInfo.InvariantCulture),
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

        private async void OpenHttp2Session()
        {
            Console.WriteLine("Handshake successful");
            var session = new Http2Session(this.InternalSocket, ConnectionEnd.Server);
            await session.Start();
        }

        private void ProtocolSelectedHandler(object sender, ProtocolSelectedArgs args)
        {
            _alpnSelectedProtocol = args.SelectedProtocol;
        }
    }
}
