// -----------------------------------------------------------------------
// <copyright file="Http2Client.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.EventArgs;
using SharedProtocol.Exceptions;
using SharedProtocol.Extensions;
using SharedProtocol.Framing;
using SharedProtocol.Handshake;
using SharedProtocol.Http11;
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
        private const string IndexHtml = "\\index.html";
        private const string Root = "\\Root";
        private const string ClientSessionHeader = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        //Remove file:// from Assembly.GetExecutingAssembly().CodeBase
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));

        private readonly SecureTcpListener _server;
        private readonly SecurityOptions _options;
        private readonly AppFunc _next;
        private Http2Session _session;
        private readonly IDictionary<string, object> _environment;        
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly List<string> _listOfRootFiles;
        private readonly object _listWriteLock = new object();

        internal HttpConnectingClient(SecureTcpListener server, SecurityOptions options,
                                     AppFunc next, bool useHandshake, bool usePriorities, 
                                     bool useFlowControl, List<string> listOfRootFiles,
                                     IDictionary<string, object> environment)
        {
            _environment = environment;
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
            var handshakeEnv = new Dictionary<string, object>
                                    {
                                        {"securityOptions", _options},
                                        {"secureSocket", incomingClient},
                                        {"end", ConnectionEnd.Server}
                                    };

            return handshakeEnv;
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
            string selectedProtocol = Protocols.Http2;
            var handshakeEnv = MakeHandshakeEnvironment(incomingClient);
            var environmentCopy = new Dictionary<string, object>(_environment);

            if (_useHandshake)
            {
                try
                {
                    //_next(environmentCopy);

                    bool wasHandshakeFinished = true;
                    var handshakeTask = Task.Factory.StartNew(HandshakeManager.GetHandshakeAction(handshakeEnv));

                    if (!handshakeTask.Wait(6000))
                    {
                        wasHandshakeFinished = false;
                    }

                    if (!wasHandshakeFinished)
                    {
                        throw new Http2HandshakeFailed(HandshakeFailureReason.Timeout);
                    }

                    environmentCopy.Add("HandshakeResult", handshakeTask.Result);
                    selectedProtocol = incomingClient.SelectedProtocol;
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
            try
            {
                HandleRequest(incomingClient, selectedProtocol, backToHttp11, environmentCopy);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(SecureSocket incomingClient, string alpnSelectedProtocol, 
                                   bool backToHttp11, IDictionary<string, object> environment)
        {
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Sending with http11");
                Http11Manager.Http11SendResponse(incomingClient);
                return;
            }

            if (GetSessionHeaderAndVerifyIt(incomingClient))
            {
                OpenHttp2Session(incomingClient, environment);
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

        private static Dictionary<string, object> CreateOwinEnvironment(string method, string scheme, string hostHeaderValue, string pathBase, string path, byte[] requestBody = null)
        {
            var environment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            environment["owin.RequestMethod"] = method;
            environment["owin.RequestScheme"] = scheme;
            environment["owin.RequestHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "Host", new string[] { hostHeaderValue } } };
            environment["owin.RequestPathBase"] = pathBase;
            environment["owin.RequestPath"] = path;
            environment["owin.RequestQueryString"] = "";
            environment["owin.RequestBody"] = new MemoryStream(requestBody ?? new byte[0]);
            environment["owin.CallCancelled"] = new CancellationToken();
            environment["owin.ResponseHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment["owin.ResponseBody"] = new MemoryStream();
            return environment;
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
                            HandlePost(sender as Http2Session, args.Frame, stream);
                            break;
                    }
                }
                else if (args.Frame is HeadersFrame)
                {
                    switch (method)
                    {
                        case "get":
                        case "delete":
                            HandleGet(sender as Http2Session, stream);
                            break;

                        default:
                            Http2Logger.LogDebug("Received headers with Status: " + stream.Headers.GetValue(":status"));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Http2Logger.LogDebug("Error: " + e.Message);
                stream.WriteRst(ResetStatusCode.InternalError);
                stream.Dispose();
            }
        }

        private void HandleGet(Http2Session session, Http2Stream stream)
        {
            string path = stream.Headers.GetValue(":path");
            string scheme = stream.Headers.GetValue(":scheme");
            string method = stream.Headers.GetValue(":method");
            string host = stream.Headers.GetValue(":host") + ":" + (session.Socket.LocalEndPoint as System.Net.IPEndPoint).Port;

            Http2Logger.LogDebug(method.ToUpper() + ": " + path);

            var env = CreateOwinEnvironment(method.ToUpper(), scheme, host, "", path);
            env["HandshakeAction"] = null;

            _next(env).ContinueWith(r =>
            {
                try
                {

                    var bytes = (env["owin.ResponseBody"] as MemoryStream).ToArray();

                    string res = Encoding.UTF8.GetString(bytes);

                    Console.WriteLine("Done: " + res);

                    SendDataTo(stream, bytes);
                }
                catch (Exception ex)
                {
                    Http2Logger.LogError("Error: " + ex.Message);
                }
            });
        }

        private void HandlePost(Http2Session session, Frame frame, Http2Stream stream)
        {
            string path = stream.Headers.GetValue(":path");
            string scheme = stream.Headers.GetValue(":scheme");
            string method = stream.Headers.GetValue(":method");
            string host = stream.Headers.GetValue(":host") + ":" + (session.Socket.LocalEndPoint as System.Net.IPEndPoint).Port;
            byte[] body = new byte[frame.Payload.Count];
            Array.ConstrainedCopy(frame.Payload.Array, frame.Payload.Offset, body, 0, body.Length);

            Http2Logger.LogDebug(method.ToUpper() + ": " + path);

            var env = CreateOwinEnvironment(method.ToUpper(), scheme, host, "", path, frame.FrameType == FrameType.Data ? body : null);
            env["HandshakeAction"] = null;
            (env["owin.RequestHeaders"] as Dictionary<string, string[]>).Add("Content-Type", new string[] { stream.Headers.GetValue(":content-type") });

            _next(env).ContinueWith(r =>
            {
                try
                {

                    var bytes = (env["owin.ResponseBody"] as MemoryStream).ToArray();

                    string res = Encoding.UTF8.GetString(bytes);

                    Console.WriteLine("Done: " + res);

                    SendDataTo(stream, bytes);
                }
                catch (Exception ex)
                {
                    Http2Logger.LogError("Error: " + ex.Message);
                }
            });
        }

        private void SendFile(string path, Http2Stream stream)
        {
            // check if root is requested, in which case send index.html
            if (string.IsNullOrEmpty(path))
                path = IndexHtml;

            byte[] binary = _fileHelper.GetFile(path);
            WriteStatus(stream, StatusCode.Code200Ok, false);
            SendDataTo(stream, binary);
            Http2Logger.LogDebug("File sent: " + path);
        }

        private void WriteStatus(Http2Stream stream, int statusCode, bool final)
        {
            var headers = new HeadersList
            {
                new KeyValuePair<string, string>(":status", statusCode.ToString()),
            };
            stream.WriteHeadersFrame(headers, final, true);
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
