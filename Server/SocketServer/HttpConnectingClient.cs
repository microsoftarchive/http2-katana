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
    /// This class handles incoming clients. It can accept them, make handshake and chose how to give a response.
    /// It encouraged to response with http11 or http20 
    /// </summary>
    internal sealed class HttpConnectingClient : IDisposable
    {
        private readonly SecureTcpListener _server;
        private readonly SecurityOptions _options;
        private readonly AppFunc _next;
        private Http2Session _session;
        //Remove file:// from Assembly.GetExecutingAssembly().CodeBase
        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private readonly FileHelper _fileHelper;
        private readonly object _writeLock = new object();
        private const string clientSessionHeader = @"PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
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
                    using (var indexFile = new StreamWriter(assemblyPath + @"\Root\index.html", true))
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
            Console.WriteLine("New client accepted");
            Task.Run(() => HandleAcceptedClient(incomingClient));
        }

        private void HandleAcceptedClient(SecureSocket incomingClient)
        {
            bool backToHttp11 = false;
            string alpnSelectedProtocol = "http/2.0";
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
                    handshakeTask.Wait();
                    
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
                        Console.WriteLine("Handshake timeout. Client was disconnected.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Some kind of exception occured. Closing client's socket");
                    incomingClient.Close();
                    return;
                }
            }
            try
            {
                HandleRequest(incomingClient, alpnSelectedProtocol, backToHttp11, handshakeResult);
            }
            catch (Exception)
            {
                Console.WriteLine("Some kind of exception occured. Closing client's socket");
                incomingClient.Close();
            }
        }

        private void HandleRequest(SecureSocket incomingClient, string alpnSelectedProtocol, 
                                   bool backToHttp11, IDictionary<string, object> handshakeResult)
        {
            if (backToHttp11 || alpnSelectedProtocol == "http/1.1")
            {
                Console.WriteLine("Sending with http11");
                Http11Manager.Http11SendResponse(incomingClient);
                return;
            }

            if (GetSessionHeaderAndVerifyIt(incomingClient))
            {
                OpenHttp2Session(incomingClient, handshakeResult);
            }
            else
            {
                Console.WriteLine("Client has wrong session header. It was disconnected");
                incomingClient.Close();
            }
        }

        private bool GetSessionHeaderAndVerifyIt(SecureSocket incomingClient)
        {
            var sessionHeaderBuffer = new byte[clientSessionHeader.Length];
            using (var sessionHeaderReceived = new ManualResetEvent(false))
            {
                var receivedThread = new Thread( 
                    (() =>
                        {
                            int received = incomingClient.Receive(sessionHeaderBuffer, 0,
                                                   sessionHeaderBuffer.Length, SocketFlags.None);
                            if (received != 0)
                            {
                                sessionHeaderReceived.Set();
                            }
                        }));
                receivedThread.Start();
                sessionHeaderReceived.WaitOne(30000);

                if (receivedThread.IsAlive)
                {
                    receivedThread.Abort();
                    receivedThread.Join();
                }

                var receivedHeader = Encoding.UTF8.GetString(sessionHeaderBuffer);

                if (receivedHeader != clientSessionHeader)
                {
                    return false;
                }
            }
            return true;
        }

        private async void OpenHttp2Session(SecureSocket incomingClient, IDictionary<string, object> handshakeResult)
        {
            Console.WriteLine("Handshake successful");
            _session = new Http2Session(incomingClient, ConnectionEnd.Server, _usePriorities,_useFlowControl, handshakeResult);
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
            var method = stream.Headers.GetValue(":method").ToLower();

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
                                binary = _fileHelper.GetFile(stream.Headers.GetValue(":path"));
                            }
                            catch (FileNotFoundException)
                            {
                                binary = new NotFound404().Bytes;
                            }
                            SendDataTo(stream, binary);
                            break;
                        case "delete":
                            binary = new AccessDenied401().Bytes;
                            SendDataTo(stream, binary);
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
