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
        private AppFunc _upgradeDelegate;

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
            
            //Provide this delegate from somewhere?
            _upgradeDelegate = UpgradeHandshaker.Handshake;
        }

        private IDictionary<string, object> MakeUpgradeEnvironment(DuplexStream incomingClient, string selectedProtocol)
        {
            //Http1 layer will call middle. middle will call upgrade delegate
            if (selectedProtocol == Protocols.Http1)
            {
                var upgradeEnv = new Dictionary<string, object>
                    {
                        {"opaque.upgrade", _upgradeDelegate},
                        {"opaque.Stream", incomingClient},
                        //Provide canc token
                        {"opaque.CallCancelled", CancellationToken.None}
                    };

                return upgradeEnv;
            }

            return new Dictionary<string, object>();
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
            var environmentCopy = new Dictionary<string, object>(_environment);
            
            if (_useHandshake)
            {
                try
                {
                    if (_options.Protocol != SecureProtocol.None)
                    {
                        //TODO Make securehandshaker methods static
                        new SecureHandshaker(environmentCopy).Handshake();
                    }

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

            var clientStream = new DuplexStream(incomingClient, true);
            environmentCopy.AddRange(MakeUpgradeEnvironment(clientStream, selectedProtocol));

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

        //Should be moved into application layer
        /*private void AddFileToRootFileList(string fileName)
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
        }*/
        /*private void SendDataTo(Http2Stream stream, byte[] binaryData)
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
        }*/

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
