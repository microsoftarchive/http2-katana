// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Client.IO;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Http2.Protocol.Exceptions;
using OpenSSL;

namespace Http2.TestClient.Adapters
{
    public class Http2ClientMessageHandler : Http2MessageHandler
    {
        private readonly FileHelper _fileHelper;
        private const string Index = @"index.html";
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public bool IsDisposed { get { return _isDisposed; } }

        public Http2ClientMessageHandler(Stream stream, ConnectionEnd end, bool isSecure, CancellationToken cancel)
            : base(stream, end, isSecure, cancel)
        {
            _fileHelper = new FileHelper(ConnectionEnd.Client);
            _session.OnSessionDisposed += delegate { Dispose(); };
        }

        private void SaveDataFrame(Http2Stream stream, DataFrame dataFrame)
        {
            string originalPath = stream.Headers.GetValue(CommonHeaders.Path.ToLower());
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
               
                stream.Close(ResetStatusCode.InternalError);
                return;
            }

            stream.ReceivedDataAmount += dataFrame.Data.Count;

            if (dataFrame.IsEndStream)
            {
                if (stream.HalfClosedRemote)
                {
                    //send terminator
                    stream.WriteDataFrame(new ArraySegment<byte>(new byte[0]), true);
                    Http2Logger.LogConsole("Terminator was sent");
                }
                _fileHelper.RemoveStream(path);
                Http2Logger.LogConsole("Bytes received: " + stream.ReceivedDataAmount);
            }
        }

        protected override void ProcessIncomingData(Http2Stream stream, Frame frame)
        {
            //wont process incoming non data frames for now.
            if (!(frame is DataFrame))
                return;

            var dataFrame = frame as DataFrame;

            SaveDataFrame(stream, dataFrame);
        }

        protected override void ProcessRequest(Http2Stream stream, Frame frame)
        {
            //spec 09 -> 8.1.3.2.  Response Header Fields
            //A single ":status" header field is defined that carries the HTTP
            //status code field (see [HTTP-p2], Section 6).  This header field MUST
            //be included in all responses, otherwise the response is malformed
            if (stream.Headers.GetValue(CommonHeaders.Status) == null)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "no status header in response. StreamId = " + stream.Id);
            }

            int code;
            if (!int.TryParse(stream.Headers.GetValue(CommonHeaders.Status), out code))
            {
                stream.WriteRst(ResetStatusCode.ProtocolError);  //Got something strange in the status field
                stream.Close(ResetStatusCode.ProtocolError);
            }

            //got some king of error
            if (code != StatusCode.Code200Ok)
            {
                //Close server's stream
                stream.Close(ResetStatusCode.Cancel); //will dispose client's stream and close server's one.
            }
            //Do nothing. Client may not process requests for now
        }

        public TimeSpan Ping()
        {
            return _session.Ping();
        }

        public void SendRequest(HeadersList pairs, int priority, bool isEndStream)
        {
            if (_wereFirstSettingsSent)
            {
                _session.SendRequest(pairs, priority, isEndStream);
            }
            else
            {
                OnFirstSettingsSent += (o, args) =>
                    {
                        //unsec handled via upgrade handshake
                        if (_isSecure)
                            _session.SendRequest(pairs, priority, isEndStream);
                    };
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
