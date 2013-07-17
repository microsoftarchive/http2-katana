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
using SharedProtocol.Pages;

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
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;

        internal string SelectedProtocol { get; private set; }

        internal HttpConnetingClient(SecureTcpListener server, SecurityOptions options,
                                     AppFunc next, bool useHandshake, bool usePriorities, bool useFlowControl)
        {
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            SelectedProtocol = String.Empty;
            _server = server;
            _next = next;
            _options = options;
            _fileHelper = new FileHelper(ConnectionEnd.Server);
        }

        private IDictionary<string, object> MakeHandshakeEnvironment(SecureSocket incomingClient)
        {
            var result = new Dictionary<string, object>();

            result.Add("securityOptions", _options);
            result.Add("secureSocket", incomingClient);
            result.Add("end", ConnectionEnd.Server);

            return result;
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

                var handshakeEnvironment = MakeHandshakeEnvironment(incomingClient);

                if (_useHandshake)
                {
                    IDictionary<string, object> environment = new Dictionary<string, object>();

                    //Sets the handshake action depends on port.
                    environment.Add("HandshakeAction", HandshakeManager.GetHandshakeAction(handshakeEnvironment));

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
            _session = new Http2Session(incomingClient, ConnectionEnd.Server, _usePriorities, _useFlowControl);

            _session.OnFrameReceived += FrameReceivedHandler;

            try
            {
                await _session.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Client was disconnected");
            }
        }

        private void SendDataTo(Http2Stream stream, byte[] binaryData)
        {
            int i = 0;

            Console.WriteLine("Transfer begin");

            while (binaryData.Length > i)
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                ?
                                    MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize, stream.WindowSize)
                                :
                                    MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            }

            //It was not send exactly. Some of the data frames could be pushed to the unshipped frames collection
            Console.WriteLine("File sent: " + stream.Headers.GetValue(":path"));
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                string path = stream.Headers.GetValue(":path".ToLower());

                try
                {
                    if (dataFrame.Data.Count != 0)
                    {
                        _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                               assemblyPath + @"\Root" + path, stream.ReceivedDataAmount != 0);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("File is still downloading. Repeat request later");
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
                        Console.WriteLine("Terminator was sent");
                    }
                    _fileHelper.RemoveStream(assemblyPath + @"\Root" + path);
                }
            }
        }

        private void FrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var method = stream.Headers.GetValue(":method");

            try
            {
                if (args.Frame is DataFrame)
                {
                    switch (method)
                    {
                        case "post":
                            //Task.Run(() => PerformPostAction(stream, (DataFrame)args.Frame));
                            //break;
                        case "put":
                            Task.Run(() => SaveDataFrame(stream, (DataFrame)args.Frame));
                            break;
                    }
                } 
                else if (args.Frame is Headers)
                {
                    switch (method)
                    {
                        case "get":
                            Task.Run(() =>
                                {
                                    byte[] binary = null;

                                    try
                                    {
                                        binary = _fileHelper.GetFile(stream.Headers.GetValue(":path"));
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        binary = new NotFound404().Bytes;
                                    }
                                    SendDataTo(stream, binary);
                                });
                            break;
                        case "delete":
                            Task.Run(() =>
                            {
                                var binary = new AccessDenied401().Bytes;
                                SendDataTo(stream, binary);
                            });
                            break;
                    }
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
