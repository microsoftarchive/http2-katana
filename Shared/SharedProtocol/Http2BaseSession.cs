using System.Reflection;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace SharedProtocol
{
    public abstract class Http2BaseSession
    {
        protected bool _goAwayReceived;
        protected ConcurrentDictionary<int, Http2BaseStream> _activeStreams;
        protected FrameReader _frameReader;
        protected WriteQueue _writeQueue;
        protected SecureSocket _sessionSocket;
        protected Ping _currentPing;
        protected int _nextPingId;
        protected CancellationToken _cancel;
        protected bool _disposed;
        protected SettingsManager _settingsManager;
        protected HeaderWriter _headerWriter;
        protected CompressionProcessor _decompressor;
        protected static string assemblyPath;
        protected FlowControlOptions _options;
        protected List<IMonitor> _monitors;
        protected List<IMonitor> _trafficMonitors;

        public Int32 MaxConcurrentStreams { get; set; }

        protected Http2BaseSession(SecureSocket sessionSocket, bool validateFirstFrameIsControl)
        {
            _goAwayReceived = false;
            _activeStreams = new ConcurrentDictionary<int, Http2BaseStream>();
            _settingsManager = new SettingsManager();
            _decompressor = new CompressionProcessor();
            _options = new FlowControlOptions();
            _sessionSocket = sessionSocket;

            //_monitors = new List<IMonitor>(1) { new SessionMonitor(this, FrameSentHandler) };

            _writeQueue = new WriteQueue(_sessionSocket, null);
            _headerWriter = new HeaderWriter(_writeQueue);
            _frameReader = new FrameReader(_sessionSocket, validateFirstFrameIsControl);

            assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        protected CompressionProcessor Decompressor
        {
            get { return _decompressor; }
        }

        protected Task StartPumps()
        {
            // TODO: Assert not started

            // Listen for incoming Http/2.0 frames
            //Task incomingTask = PumpIncommingData();
            // Send outgoing Http/2.0 frames
            //Task outgoingTask = PumpOutgoingData();
            Task incomingTask = new Task(() => PumpIncommingData());
            Task outgoingTask = new Task(() => PumpOutgoingData());
            incomingTask.Start();
            outgoingTask.Start();
            return Task.WhenAll(incomingTask, outgoingTask);
        }

        // Read HTTP/2.0 frames from the raw stream and dispatch them to the appropriate virtual streams for processing.
        private void PumpIncommingData()
        {
            while (!_goAwayReceived && !_disposed)
            {
                Frame frame;
                try
                {
                    frame = _frameReader.ReadFrame();
                }
                catch (Exception)
                {
                    // Read failure, abort the connection/session.
                    Dispose();
                    throw;
                }

                if (frame == null)
                {
                    // Stream closed
                    Dispose();
                    break;
                }
               
                DispatchIncomingFrame(frame);
            }
        }

        protected virtual void DispatchIncomingFrame(Frame frame)
        {
            Http2BaseStream stream;
            switch (frame.FrameType)
            {
                case FrameType.Ping:
                    PingFrame pingFrame = (PingFrame)frame;
                    ReceivePing(pingFrame.Id);
                    break;
                case FrameType.Settings:
                    _settingsManager.ProcessSettings((SettingsFrame)frame, _options, _activeStreams[frame.StreamId]);
                    break;
                case FrameType.WindowUpdate:
                    WindowUpdateFrame windowFrame = (WindowUpdateFrame)frame;
                    stream = GetStream(windowFrame.StreamId);
                    stream.UpdateWindowSize(windowFrame.Delta);
                    break;
                case FrameType.Headers:
                    HeadersFrame headersFrame = (HeadersFrame)frame;
                    stream = GetStream(headersFrame.StreamId);
                    byte[] decompressedHeaders = Decompressor.Decompress(headersFrame.CompressedHeaders);
                    IList<KeyValuePair<string, string>> headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);
                    stream.ReceiveExtraHeaders(headersFrame, headers);
                    break;
                default:
                    throw new NotImplementedException(frame.FrameType.ToString());
            }
        }

        // Manage the outgoing queue of requests.
        private Task PumpOutgoingData()
        {
            return _writeQueue.PumpToStreamAsync();
        }

        public Http2BaseStream GetStream(int id)
        {
            Http2BaseStream stream;
            if (!_activeStreams.TryGetValue(id, out stream))
            {
                // TODO: Session already gone? Send a reset?
                throw new NotImplementedException("Stream id not found: " + id);
            }
            return stream;
        }

        public Task<TimeSpan> PingAsync()
        {
            Contract.Assert(_currentPing == null || _currentPing.Task.IsCompleted);
            Ping ping = new Ping(_nextPingId);
            _nextPingId += 2;
            _currentPing = ping;
            PingFrame pingFrame = new PingFrame(_currentPing.Id);
            _writeQueue.WriteFrameAsync(pingFrame, Priority.Ping);
            return ping.Task;
        }

        public void ReceivePing(int id)
        {
            // Even or odd?
            if (id % 2 != _nextPingId % 2)
            {
                // Not one of ours, response ASAP
                _writeQueue.WriteFrameAsync(new PingFrame(id), Priority.Ping);
                return;
            }

            Ping currentPing = _currentPing;
            if (currentPing != null && id == currentPing.Id)
            {
                currentPing.Complete();
            }
            // Ignore extra pings
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
            if (!disposing)
            {
                return;
            }

            if (_writeQueue != null)
            {
                _writeQueue.Dispose();
            }

            Ping currentPing = _currentPing;
            if (currentPing != null)
            {
                currentPing.Cancel();
            }

            // Dispose of all streams
            foreach (Http2BaseStream stream in _activeStreams.Values)
            {
                stream.Reset(ResetStatusCode.Cancel);
                stream.Dispose();
            }

            _sessionSocket.Close();

            _headerWriter.Dispose();
            _decompressor.Dispose();
        }

        public void FrameSentHandler(object sender, EventArgs args)
        {

        }
    }
}
