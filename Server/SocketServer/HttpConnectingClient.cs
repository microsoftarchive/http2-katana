// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.Compression;
using SharedProtocol.Compression.Http2DeltaHeadersCompression;
using SharedProtocol.Exceptions;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Extensions;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;
using SharedProtocol.IO;
using SharedProtocol.Pages;
using SharedProtocol.Utils;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// This class handles incoming clients. It can accept them, make handshake and chose how to give a response.
    /// It encouraged to response with http11 or http20 
    /// </summary>
    internal sealed class HttpConnectingClient : IDisposable
    {
        private const string IndexHtml = "\\index.html";
        private const string Root = "\\Root";
        private const string ClientSessionHeader = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));


        private readonly SecureTcpListener _server;
        private readonly SecurityOptions _options;
        private readonly AppFunc _next;
        private Http2Session _session;
        //Remove file:// from Assembly.GetExecutingAssembly().CodeBase
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly List<string> _listOfRootFiles;
        private readonly object _listWriteLock = new object();

        internal HttpConnectingClient(SecureTcpListener server, SecurityOptions options,
                                     AppFunc next, bool useHandshake, bool usePriorities, 
                                     bool useFlowControl, List<string> listOfRootFiles)
        {
            _listOfRootFiles = listOfRootFiles;
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            _server = server;
            _next = next;
            _options = options;
            _fileHelper = new FileHelper(ConnectionEnd.Server);
        }

        private IDictionary<string, object> MakeHandshakeEnvironment(SecureSocket incomingClient)
        {
            var result = new Dictionary<string, object>
                {
                    {"securityOptions", _options},
                    {"secureSocket", incomingClient},
                    {"end", ConnectionEnd.Server}
                };

            return result;
        }

	private void AddFileToRootFileList(string fileName)
        {
            lock (_listWriteLock)
            {
                //if file is located in root directory then add it to the index
                if (!_listOfRootFiles.Contains(fileName) && !fileName.Contains("/"))
                {
                    using (var indexFile = new StreamWriter(AssemblyPath + Root + IndexHtml, true))
                    {
                        _listOfRootFiles.Add(fileName);
                        indexFile.Write(fileName + "<br>\n");
                    }
                }
            }
        }
		
        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal void Accept()
        {
            SecureSocket incomingClient;
            using (var monitor = new ALPNExtensionMonitor())
            {
                incomingClient = _server.AcceptSocket(monitor);
            }
            Http2Logger.LogDebug("New connection accepted");
            Task.Run(() => HandleAcceptedClient(incomingClient));
        }

        private void HandleAcceptedClient(SecureSocket incomingClient)
        {
            bool backToHttp11 = false;
            string alpnSelectedProtocol = Protocols.Http2;
            var handshakeEnvironment = MakeHandshakeEnvironment(incomingClient);
            IDictionary<string, object> handshakeResult = null;

            //Think out smarter way to get handshake result.
            //DO NOT change Middleware function. If you will do so, server will not even launch. (It's owin's problem)
            Func<Task> handshakeAction = () =>
                {
                    var handshakeTask = new Task(() =>
                        {
                            handshakeResult = HandshakeManager.GetHandshakeAction(handshakeEnvironment).Invoke();
                        });
                    return handshakeTask;
                };
            
            if (_useHandshake)
            {
                var environment = new Dictionary<string, object>
                    {
                        //Sets the handshake action depends on port.
                        {"HandshakeAction", handshakeAction},
                    };

                try
                {
                    var handshakeTask = _next(environment);
                    
                    handshakeTask.Start();
                    if (!handshakeTask.Wait(6000))
                    {
                        incomingClient.Close();
                        Http2Logger.LogError("Handshake timeout. Connection dropped.");
                        return;
                    }
                    
                    alpnSelectedProtocol = incomingClient.SelectedProtocol;
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
                    Http2Logger.LogError("Exception occured. Closing client's socket. " + e.Message);
                    incomingClient.Close();
                    return;
                }
            }
            try
            {
                HandleRequest(incomingClient, alpnSelectedProtocol, backToHttp11, handshakeResult);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occured. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(SecureSocket incomingClient, string alpnSelectedProtocol, 
                                   bool backToHttp11, IDictionary<string, object> handshakeResult)
        {
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Sending with http11");
                Http11Manager.Http11SendResponse(incomingClient);
                return;
            }

            if (GetSessionHeaderAndVerifyIt(incomingClient))
            {
                OpenHttp2Session(incomingClient, handshakeResult);
            }
            else
            {
                Http2Logger.LogError("Client has wrong session header. It was disconnected");
                incomingClient.Close();
            }
        }

        private bool GetSessionHeaderAndVerifyIt(SecureSocket incomingClient)
        {
            var sessionHeaderBuffer = new byte[ClientSessionHeader.Length];

            int received = incomingClient.Receive(sessionHeaderBuffer, 0,
                                                   sessionHeaderBuffer.Length, SocketFlags.None);


            var receivedHeader = Encoding.UTF8.GetString(sessionHeaderBuffer);

            return string.Equals(receivedHeader, ClientSessionHeader, StringComparison.OrdinalIgnoreCase);
        }

        private async void OpenHttp2Session(SecureSocket incomingClient, IDictionary<string, object> handshakeResult)
        {
            Http2Logger.LogDebug("Handshake successful");
            _session = new Http2Session(incomingClient, ConnectionEnd.Server, _usePriorities,_useFlowControl, handshakeResult);
            _session.OnFrameReceived += FrameReceivedHandler;

            try
            {
                await _session.Start();
            }
            catch (Exception)
            {
                Http2Logger.LogError("Client was disconnected");
            }
        }

        private void SendDataTo(Http2Stream stream, byte[] binaryData)
        {
            int i = 0;

            Http2Logger.LogDebug("Transfer begin");

            do
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                    ? MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize,
                                                 stream.WindowSize)
                                    : MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            } while (binaryData.Length > i);
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            lock (_writeLock)
            {
                string path = stream.Headers.GetValue(":path".ToLower());

                try
                {
                    string pathToSave = AssemblyPath + Root + path;
                    if (!Directory.Exists(Path.GetDirectoryName(pathToSave)))
                    {
                        throw new DirectoryNotFoundException("Access denied");
                    }
                     _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                               pathToSave, stream.ReceivedDataAmount != 0);
                }
                catch (Exception ex)
                {
                    Http2Logger.LogError(ex.Message);
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
                        Http2Logger.LogDebug("Terminator was sent");
                    }
                    _fileHelper.RemoveStream(AssemblyPath + Root + path);
                }
            }
        }

        private void FrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var method = stream.Headers.GetValue(":method");
            if (!string.IsNullOrEmpty(method)) 
                method = method.ToLower();

            try
            {
                if (args.Frame is DataFrame)
                {
                    switch (method)
                    {
                        case "post":
                        case "put":
                            SaveDataFrame(stream, (DataFrame) args.Frame);
                            //Avoid leading \ at the filename
                            AddFileToRootFileList(stream.Headers.GetValue(":path").Substring(1));
                            break;
                    }
                } 
                else if (args.Frame is Headers)
                {
                    byte[] binary;
                    switch (method)
                    {
                        case "get":
                        case "dir":
                            try
                            {
                                string path = stream.Headers.GetValue(":path");
                                // check if root is requested, in which case send index.html
                                if (path == "/")
                                    path = IndexHtml;

                                binary = _fileHelper.GetFile(path);
                                WriteStatus(stream, StatusCode.Code200Ok, false);
                                SendDataTo(stream, binary);
                                Http2Logger.LogDebug("File sent: " + path);
                            }
                            catch (FileNotFoundException)
                            {
                                WriteStatus(stream, StatusCode.Code404NotFound, true);
                            }

                            break;
                        case "delete":
                            WriteStatus(stream, StatusCode.Code401Forbidden, true);
                            break;

                        default:
                            Http2Logger.LogDebug("Received headers with Status: " + stream.Headers.GetValue(":status"));
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

        private void WriteStatus(Http2Stream stream, int statusCode, bool final)
        {
            var headers = new List<Tuple<string, string, IAdditionalHeaderInfo>>
            {
                new Tuple<string, string, IAdditionalHeaderInfo>(":status", statusCode.ToString(),
                                                    new Indexation(IndexationType.Indexed)),
            };
            stream.WriteHeadersFrame(headers, final);
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
