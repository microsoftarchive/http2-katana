using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ServerProtocol
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Http2ServerSession : Http2BaseSession, IDisposable
    {
        private AppFunc _next;
        private X509Certificate[] _clientCerts;
        private TransportInformation _transportInfo;

        public Http2ServerSession(SecureSocket sessionSocket, AppFunc next, TransportInformation transportInfo)
            :base(sessionSocket, true)
        {
            _next = next;
            _transportInfo = transportInfo;
            _clientCerts = new X509Certificate[Constants.DefaultClientCertVectorSize];
            _clientCerts[0] = _transportInfo.ClientCertificate;
            _nextPingId = 2; // Server pings are even
        }

        public Task Start()
        {
            // Listen for incoming Http/2.0 frames
            // Send outgoing Http/2.0 frames
            // Complete the returned task only at the end of the session.  The connection will be terminated.
            return StartPumps();
        }

        private void DispatchNewStream(int id, Http2ServerStream stream)
        {
            _activeStreams[id] = stream;
            Task.Run(() => stream.Run(_next))
                .ContinueWith(task =>
                {
                    CompleteResponse(stream, task);
                });
        }

        // Remove the stream from _activeStreams
        private void CompleteResponse(Http2BaseStream stream, Task appFuncTask)
        {
            if (_activeStreams.TryRemove(stream.Id, out stream) == false)
            {
                throw new ArgumentException("Cant remove stream from _activeStreams");
            }
        }

        protected override void DispatchIncomingFrame(Frame frame)
        {
            switch (frame.FrameType)
            {
                // New incoming request stream
                case FrameType.HeadersPlusPriority:
                    Console.WriteLine("New headers + priority" + frame.StreamId);
                    HeadersPlusPriority headersPlusPriorityFrame = (HeadersPlusPriority)frame;
                    // TODO: Validate this stream ID is in the correct sequence and not already in use.
                    byte[] decompressedHeaders = Decompressor.Decompress(headersPlusPriorityFrame.CompressedHeaders);
                    IList<KeyValuePair<string, string>> headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);
                    Http2ServerStream stream = new Http2ServerStream(headersPlusPriorityFrame, headers, _transportInfo, _writeQueue, _headerWriter, _cancel);
                    DispatchNewStream(headersPlusPriorityFrame.StreamId, stream);
                    break;

                case FrameType.RstStream:
                    RstStreamFrame resetFrame = (RstStreamFrame)frame;
                    stream = (Http2ServerStream)GetStream(resetFrame.StreamId);
                    stream.Reset(resetFrame.StatusCode);
                    Task.Run(() =>
                    {
                        // Trigger the cancellation token in a Task.Run so we don't block the message pump.
                        stream.Reset(resetFrame.StatusCode);
                    });
                    break;

                default:
                    base.DispatchIncomingFrame(frame);
                    break;
            }
        }
    }
}
