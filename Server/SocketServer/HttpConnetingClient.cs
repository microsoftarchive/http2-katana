// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using ServerProtocol;
using SharedProtocol;
using SharedProtocol.Exceptions;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Extensions;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;
using SharedProtocol.IO;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// This class handles incoming clients. It can accept them, make handshake and chose how to give a responce.
    /// It encouraged to responce with http11 or http20 
    /// </summary>
    internal sealed class HttpConnetingClient : IDisposable
    {
        private readonly SecureTcpListener _server;
        private readonly SecurityOptions _options;
        private readonly AppFunc _next;
        private string _alpnSelectedProtocol;
        private Http2Session _session = null;
        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private readonly string _clientSessionHeader = @"FOO * HTTP/2.0\r\n\r\nBA\r\n\r\n";
        private readonly bool _useHandshake;

        public string SelectedProtocol { get; private set; }

        internal HttpConnetingClient(SecureTcpListener server, SecurityOptions options, AppFunc next, bool useHandshake)
        {
            SelectedProtocol = String.Empty;
            _server = server;
            _next = next;
            _options = options;
            _useHandshake = useHandshake;
            _fileHelper = new FileHelper();
        }

        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal async void Accept()
        {
            bool backToHttp11 = false;
            SecureSocket incomingClient = null;
            using (var monitor = new ALPNExtensionMonitor())
            {
                monitor.OnProtocolSelected += (sender, args) => { _alpnSelectedProtocol = args.SelectedProtocol; };

                incomingClient = _server.AcceptSocket(monitor);
                Console.WriteLine("New client accepted");

                if (_useHandshake)
                {
                    IDictionary<string, object> environment = new Dictionary<string, object>();

                    //Sets the handshake action depends on port.
                    environment.Add("HandshakeAction", HandshakeManager.GetHandshakeAction(incomingClient, _options));

                    try
                    {
                        await _next(environment);
                    }
                    catch (Http2HandshakeFailed)
                    {
                        backToHttp11 = true;
                    }
                }

            }
            Task.Run(() => HandleRequest(incomingClient, backToHttp11));
        }

        private void HandleRequest(SecureSocket incomingClient, bool backToHttp11)
        {
            if (backToHttp11 || _alpnSelectedProtocol == "http/1.1")
            {
                Console.WriteLine("Sending with http11");
                Http11Manager.Http11SendResponse(incomingClient);
                return;
            }

            GetSessionHeader(incomingClient);
            OpenHttp2Session(incomingClient);
        }

        private void GetSessionHeader(SecureSocket incomingClient)
        {
            var sessionHeaderBuffer = new byte[_clientSessionHeader.Length];
            int received = 0;
            while (received < _clientSessionHeader.Length)
            {
                received = incomingClient.Receive(sessionHeaderBuffer, received, sessionHeaderBuffer.Length - received, SocketFlags.None);
            }

            var receivedHeader = Encoding.UTF8.GetString(sessionHeaderBuffer);

            if (receivedHeader != _clientSessionHeader)
            {
                Dispose();
            }
        }

        //This method is never used, but it's useful for future
        private TransportInformation GetSocketTranspInfo(SecureSocket incomingClient)
        {
            var localEndPoint = (IPEndPoint)incomingClient.LocalEndPoint;
            var remoteEndPoint = (IPEndPoint)incomingClient.RemoteEndPoint;

            var transportInfo = new TransportInformation()
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

        private async void OpenHttp2Session(SecureSocket incomingClient)
        {
            Console.WriteLine("Handshake successful");
            _session = new Http2Session(incomingClient, ConnectionEnd.Server);

            _session.OnFrameReceived += FrameReceivedHandler;

            await _session.Start();
        }

        private void SendResponce(Http2Stream stream)
        {
            byte[] binaryFile = _fileHelper.GetFile(stream.Headers.GetValue(":path"));
            int i = 0;

            Console.WriteLine("Transfer begin");

            while (binaryFile.Length > i)
            {
                bool isLastData = binaryFile.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                ?
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize, stream.WindowSize)
                                :
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryFile, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            }

            //It was not send exactly. Some of the data frames could be pushed to the unshipped frames collection
            Console.WriteLine("File sent: " + stream.Headers.GetValue(":path"));
        }

        private void SaveToFile(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                var path = stream.Headers.GetValue(":path");

                if (dataFrame.Data.Count != 0)
                {
                    _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                           assemblyPath + path, stream.ReceivedDataAmount != 0);
                }

                stream.ReceivedDataAmount += dataFrame.FrameLength;
            }
        }

        private void FrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            try
            {
                if (args.Frame is DataFrame)
                {
                    Task.Run(() => SaveToFile(stream, (DataFrame)args.Frame));
                }

                if (args.Frame is Headers)
                {
                    Task.Run(() => SendResponce(stream));
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
            if (_session != null)
            {
                _session.Dispose();
            }

            _fileHelper.Dispose();
        }
    }
}
