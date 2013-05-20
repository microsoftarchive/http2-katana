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
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using ServerProtocol;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal sealed class Http2ConnetingClient
    {
        private ALPNExtensionMonitor monitor;

        private string assemblyPath;
        private SecureTcpListener server;
        private AppFunc next;

        public string SelectedProtocol { get; private set; }
        public SecureSocket InternalSocket { get; private set; }

        internal Http2ConnetingClient(SecureTcpListener server, string assemblyPath, AppFunc next)
        {
            this.assemblyPath = assemblyPath;
            this.SelectedProtocol = String.Empty;
            this.server = server;
            this.next = next;

            this.monitor = new ALPNExtensionMonitor();
            this.monitor.OnProtocolSelected += this.ProtocolSelectionHandler;
        }

        internal async void Accept()
        {
            this.InternalSocket = server.AcceptSocket(this.monitor);
            this.InternalSocket.OnHandshakeFinish += this.HandshakeFinishedHandler;

            IDictionary<string, object> environment = new Dictionary<string, object>();

            environment.Add("socket", this.InternalSocket);
            try
            {
                await next(environment);
            }
            catch (Exception)
            {
                throw new ProtocolViolationException("Handshake failed!");
            }
        }


        private void HandshakeFinishedHandler(object sender, EventArgs args)
        {
            switch (this.SelectedProtocol)
            {
                case "http/1.1":
                    ThreadPool.QueueUserWorkItem(delegate
                        {
                            ;
                        });
                    break;
                case "spdy/3":
                case "spdy/2":
                    ThreadPool.QueueUserWorkItem(delegate
                        {
                            OpenSession();
                        });
                    break;
                default:
                    break;
            }
            
            this.monitor.Dispose();
        }

        private void ProtocolSelectionHandler(object sender, ProtocolSelectedArgs args)
        {
            this.SelectedProtocol = args.SelectedProtocol;
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

        private async void OpenSession()
        {
            Console.WriteLine("Handshake successful");
            var session = new Http2ServerSession(this.InternalSocket, next, GetSocketTranspInfo());
            await session.Start();
        }
    }
}
