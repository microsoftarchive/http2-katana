using System;
using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ClientProtocol
{
    public class Http2ClientSession : Http2BaseSession, System.IDisposable
    {
        private int _lastId = -1;

        public Http2ClientSession(SecureSocket sessionSocket, CancellationToken cancel)
            : base(sessionSocket, false)
        {
            _nextPingId = 1; // Client pings are odd
            _cancel = cancel;
        }

        public Task Start()
        {
            Console.WriteLine("Session started");
            return StartPumps();
        }

        protected override void DispatchIncomingFrame(Frame frame)
        {
            Http2ClientStream stream;
            Console.WriteLine("Incoming frame :" + frame.FrameType.ToString());
            switch (frame.FrameType)
            {
                case FrameType.RstStream:
                    RstStreamFrame resetFrame = (RstStreamFrame)frame;
                    stream = (Http2ClientStream)GetStream(resetFrame.StreamId);
                    stream.Reset(resetFrame.StatusCode);
                    break;
                case FrameType.Data:
                    DataFrame dataFrame = (DataFrame)frame;
                    stream = (Http2ClientStream)GetStream(dataFrame.StreamId);
                    string path = stream.GetHeader(":path");
                    FileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count, assemblyPath + path, stream.IsDataFrameReceived);
                    stream.IsDataFrameReceived = true;
                    break;
                default:
                    base.DispatchIncomingFrame(frame);
                    break;
            }
        }

        public Http2ClientStream SendRequest(Dictionary<string, string> pairs, X509Certificate clientCert, int priority, bool hasRequestBody, CancellationToken cancel)
        {
            Contract.Assert(priority >= 0 && priority <= 7);
            Http2ClientStream clientStream = CreateStream((Priority)priority, cancel);
            int certIndex = UpdateClientCertificates(clientCert);

            clientStream.StartRequest(pairs, certIndex, hasRequestBody);

            return clientStream;
        }

        // Maintain the list of client certificates in sync with the server
        // Send a cert update if the server doesn't have this specific client cert yet.
        private int UpdateClientCertificates(X509Certificate clientCert)
        {
            // throw new NotImplementedException();
            return 0;
        }

        private Http2ClientStream CreateStream(Priority priority, CancellationToken cancel)
        {
            Http2ClientStream handshakeStream = new Http2ClientStream(GetNextId(), priority, _writeQueue, _headerWriter, cancel);
            _activeStreams[handshakeStream.Id] = handshakeStream;
            return handshakeStream;
        }

        private int GetNextId()
        {
            _lastId += 2;
            return _lastId;
        }
    }
}
