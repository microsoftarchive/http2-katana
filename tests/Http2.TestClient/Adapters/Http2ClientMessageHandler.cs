using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using Org.Mentalis.Security.Ssl;

namespace Http2.TestClient.Adapters
{
    public class Http2ClientMessageHandler : Http2MessageHandler
    {
        private readonly FileHelper _fileHelper;
        private const string Index = @"index.html";
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public bool IsDisposed { get { return _isDisposed; } }

        public Http2ClientMessageHandler(DuplexStream stream, ConnectionEnd end,  TransportInformation transportInfo, CancellationToken cancel) 
            : base(stream, end, transportInfo, cancel)
        {
            _fileHelper = new FileHelper(ConnectionEnd.Client);
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            string originalPath = stream.Headers.GetValue(":path".ToLower());
            //If user sets the empty file in get command we return notFound webpage
            string fileName = string.IsNullOrEmpty(Path.GetFileName(originalPath)) ? Index : Path.GetFileName(originalPath);
            string path = Path.Combine(AssemblyPath, fileName);

            try
            {
               _fileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count,
                                    path, stream.ReceivedDataAmount != 0);
            }
            catch (IOException)
            {
                Http2Logger.LogError("File is still downloading. Repeat request later");
                //stream.WriteDataFrame(new byte[0], true);

                //RST always has endstream flag
                //_fileHelper.RemoveStream(path);
                stream.Dispose(ResetStatusCode.InternalError);
                return;
            }

            stream.ReceivedDataAmount += dataFrame.FrameLength;

            if (dataFrame.IsEndStream)
            {
                if (!stream.EndStreamSent)
                {
                    //send terminator
                    stream.WriteDataFrame(new byte[0], true);
                    Http2Logger.LogConsole("Terminator was sent");
                }
                _fileHelper.RemoveStream(path);
                Http2Logger.LogConsole("Bytes received " + stream.ReceivedDataAmount);
#if DEBUG
                const string wayToServerRoot1 = @"..\..\..\..\Drop\Root";
                const string wayToServerRoot2 = @".\Root";
                var areFilesEqual = _fileHelper.CompareFiles(path, wayToServerRoot1 + originalPath) ||
                                    _fileHelper.CompareFiles(path, wayToServerRoot2 + originalPath);
                if (!areFilesEqual)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Http2Logger.LogError("Files are NOT EQUAL!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Http2Logger.LogConsole("Files are EQUAL!");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
#endif
            }
        }

        protected override void ProcessIncomingData(Http2Stream stream, Frame frame)
        {
            //wont process incoming data for now.
            if (!(frame is DataFrame))
                return;

            var dataFrame = frame as DataFrame;

            SaveDataFrame(stream, dataFrame);

            if (dataFrame.IsEndStream)
                stream.EndStreamReceived = true;
        }

        protected override void OnSessionDisposedHandler(object sender, EventArgs e)
        {
            Dispose();
        }

        protected override void ProcessRequest(Http2Stream stream, Frame frame)
        {
            //spec 06
            //A client
            //MUST treat the absence of the ":status" header field, the presence of
            //multiple values, or an invalid value as a stream error
            //(Section 5.4.2) of type PROTOCOL_ERROR [PROTOCOL_ERROR].

            if (stream.Headers.GetValue(":status") == null)
            {
                stream.WriteRst(ResetStatusCode.ProtocolError); 
            }
            //Do nothing. Client may not process requests for now
        }

        public TimeSpan Ping()
        {
            return _session.Ping();
        }

        public void SendRequest(HeadersList pairs, Priority priority, bool isEndStream)
        {
            if (_wereFirstSettingsSent)
            {
                _session.SendRequest(pairs, priority, isEndStream);
            }
            else
            {
                OnFirstSettingsSent += (o, args) => _session.SendRequest(pairs, priority, isEndStream);
            }
        }

        public override void Dispose()
        {
            if (_isDisposed)
                return;

            _fileHelper.Dispose();
            base.Dispose();
        }
    }
}
