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
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class Http2Session : IDisposable
    {
        private bool _goAwayReceived; 
        private FrameReader _frameReader;
        private WriteQueue _writeQueue;
        private SecureSocket _sessionSocket;
        private Ping _currentPing;
        private int _nextPingId;
        private bool _disposed;
        private SettingsManager _settingsManager;
        private CompressionProcessor _comprProc;
        private FlowControlManager _flowControlManager;
        private ConnectionEnd _end;
        private int _lastId;

        internal ActiveStreams ActiveStreams { get; private set; }

        //TODO take into account this variable
        public Int32 MaxConcurrentStreams { get; set; }

        public Int32 SessionWindowSize { get; set; }

        public Http2Session(SecureSocket sessionSocket, ConnectionEnd end)
        {
            _end = end;

            if (_end == ConnectionEnd.Client)
            {
                _lastId = -1;
            }
            else
            {
                _lastId = 0;
            }

            _goAwayReceived = false;
            _settingsManager = new SettingsManager();
            _comprProc = new CompressionProcessor();
            _sessionSocket = sessionSocket;

            _writeQueue = new WriteQueue(_sessionSocket);

            _frameReader = new FrameReader(_sessionSocket);

            ActiveStreams = new ActiveStreams();

            _flowControlManager = new FlowControlManager(this);

            SessionWindowSize = 0;
            /*var ioPair = new Tuple<FrameReader, WriteQueue>(_frameReader, _writeQueue);
            Dictionary<IMonitor, object> monitorPairs = new Dictionary<IMonitor, object>(1);

            monitorPairs.Add(new FlowControlMonitor(_flowControlManager.DataFrameSentHandler, 
                                                    _flowControlManager.DataFrameReceivedHandler), 
                                                    ioPair);

            SessionMonitor monitor = new SessionMonitor(monitorPairs);
            monitor.Attach(this);*/
        }

        #region Pumps
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

        // Manage the outgoing queue of requests.
        private Task PumpOutgoingData()
        {
            return _writeQueue.PumpToStreamAsync();
        }
        #endregion

        #region Frame handling
        private void DispatchIncomingFrame(Frame frame)
        {
            Http2Stream stream = null;
            byte[] decompressedHeaders;
            Dictionary<string, string> headers;

            try
            {
                switch (frame.FrameType)
                {
                    case FrameType.HeadersPlusPriority:
                        Console.WriteLine("New headers + priority with id = " + frame.StreamId);
                        var headersPlusPriorityFrame = (HeadersPlusPriority) frame;
                        decompressedHeaders = _comprProc.Decompress(headersPlusPriorityFrame.CompressedHeaders);
                        headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);

                        stream = new Http2Stream(headers, headersPlusPriorityFrame.StreamId,
                                                 headersPlusPriorityFrame.Priority, _writeQueue,
                                                 _flowControlManager, _comprProc);

                        ActiveStreams[stream.Id] = stream;

                        DispatchNewStream(stream);
                        break;

                    case FrameType.RstStream:
                        var resetFrame = (RstStreamFrame) frame;
                        stream = GetStream(resetFrame.StreamId);

                        //TODO rework
                        stream.WriteRst(resetFrame.StatusCode);
                        break;
                    case FrameType.Data:
                        var dataFrame = (DataFrame) frame;
                        stream = GetStream(dataFrame.StreamId);
                        stream.ProcessIncomingData(dataFrame);
                        break;
                    case FrameType.Ping:
                        //TODO Process ping correctly
                        var pingFrame = (PingFrame) frame;
                        ReceivePing(pingFrame.Id);
                        break;
                    case FrameType.Settings:
                        _settingsManager.ProcessSettings((SettingsFrame) frame, _flowControlManager);
                        break;
                    case FrameType.WindowUpdate:
                        var windowFrame = (WindowUpdateFrame) frame;
                        stream = GetStream(windowFrame.StreamId);
                        stream.UpdateWindowSize(windowFrame.Delta);

                        stream.PumpUnshippedFrames();
                        break;
                    case FrameType.Headers:
                        var headersFrame = (HeadersFrame) frame;
                        stream = GetStream(headersFrame.StreamId);
                        decompressedHeaders = _comprProc.Decompress(headersFrame.CompressedHeaders);
                        headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);
                        //TODO Process headers
                        break;
                    default:
                        throw new NotImplementedException(frame.FrameType.ToString());
                }

                //Tell the stream that it was the last frame
                if (stream != null && frame.IsFin)
                {
                    stream.FinReceived = true;
                }
            }
                //Frame came for already closed stream. Ignore it.
            catch (KeyNotFoundException)
            {
                
            }
        }
        #endregion

        #region Responce
        private void DispatchNewStream(Http2Stream stream)
        {
            stream.OnClose += StreamCloseHandler;
            Task.Run(() => stream.Run());
        }
        #endregion

        #region Stream Handling
        private int GetNextId()
        {
            _lastId += 2;
            return _lastId;
        }

        private Http2Stream CreateStream(Priority priority)
        {
            var stream = new Http2Stream(GetNextId(), priority, _writeQueue, _flowControlManager, _comprProc);
            stream.OnClose += StreamCloseHandler;

            ActiveStreams[stream.Id] = stream;
            return stream;
        }

        public Http2Stream SendRequest(Dictionary<string, string> pairs, int priority, bool isFin)
        {
            Contract.Assert(priority >= 0 && priority <= 7);
            Http2Stream stream = CreateStream((Priority)priority);

            stream.WriteHeadersPlusPriorityFrame(pairs, isFin);
            return stream;
        }

        public Http2Stream GetStream(int id)
        {
            Http2Stream stream;
            if (!ActiveStreams.TryGetValue(id, out stream))
            {
                // TODO: Session already gone? Send a reset?
                throw new KeyNotFoundException("Stream id not found: " + id);
            }
            return stream;
        }
        #endregion

        public Task Start()
        {
            WriteSettings(new[] { new SettingsPair(0, SettingsIds.InitialWindowSize, Constants.DefaultFlowControlCredit) });
            
            // Listen for incoming Http/2.0 frames
            Task incomingTask = new Task(() => PumpIncommingData());
            // Send outgoing Http/2.0 frames
            Task outgoingTask = new Task(() => PumpOutgoingData());
            incomingTask.Start();
            outgoingTask.Start();

            return Task.WhenAll(incomingTask, outgoingTask);
        }

        //Because settings must be sent once session was opened. Looks like Settings is not stream specific. 
        public void WriteSettings(SettingsPair[] settings)
        {
            var frame = new SettingsFrame(new List<SettingsPair>(settings));

            _writeQueue.WriteFrameAsync(frame, Priority.Pri3);
        }

        #region Ping
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
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
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
            foreach (Http2Stream stream in ActiveStreams.Values)
            {
                //TODO Is it correct?
                stream.WriteRst(ResetStatusCode.Cancel);
                stream.Dispose();
            }
            
            _comprProc.Dispose();
            _sessionSocket.Close();
        }
        #endregion

        private void StreamCloseHandler(object sender, StreamClosedEventArgs args)
        {
            var stream = ActiveStreams[args.Id];
            if (ActiveStreams.Remove(stream) == false)
            {
                throw new ArgumentException("Cant remove stream from ActiveStreams");
            }
        }
    }
}
