using System.Threading;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class Http2Session : IDisposable
    {
        private bool _goAwayReceived; 
        private readonly FrameReader _frameReader;
        private readonly WriteQueue _writeQueue;
        private readonly SecureSocket _sessionSocket;
        private readonly ManualResetEvent _pingReceived = new ManualResetEvent(false);
        private bool _disposed;
        private readonly SettingsManager _settingsManager;
        private readonly CompressionProcessor _comprProc;
        private readonly FlowControlManager _flowControlManager;
        private readonly ConnectionEnd _ourEnd;
        private readonly ConnectionEnd _remoteEnd;
        private int _lastId;
        private bool _wasSettingsReceived = false;
        private bool _wasPingReceived = false;

        /// <summary>
        /// Gets the active streams.
        /// </summary>
        /// <value>
        /// The active streams collection.
        /// </value>
        internal ActiveStreams ActiveStreams { get; private set; }

        /// <summary>
        /// How many parallel streams can our endpoint support
        /// Gets or sets our max concurrent streams.
        /// </summary>
        /// <value>
        /// Our max concurrent streams.
        /// </value>
        internal Int32 OurMaxConcurrentStreams { get; set; }

        /// <summary>
        /// How many parallel streams can our endpoint support
        /// Gets or sets the remote max concurrent streams.
        /// </summary>
        /// <value>
        /// The remote max concurrent streams.
        /// </value>
        internal Int32 RemoteMaxConcurrentStreams { get; set; }

        internal Int32 SessionWindowSize { get; set; }

        public Http2Session(SecureSocket sessionSocket, ConnectionEnd end)
        {
            _ourEnd = end;

            if (_ourEnd == ConnectionEnd.Client)
            {
                _remoteEnd = ConnectionEnd.Server;
                _lastId = -1; // Streams opened by client are odd
            }
            else
            {
                _remoteEnd = ConnectionEnd.Client;
                _lastId = 0; // Streams opened by server are even
            }

            _goAwayReceived = false;
            _settingsManager = new SettingsManager();
            _comprProc = new CompressionProcessor();
            _sessionSocket = sessionSocket;

            _writeQueue = new WriteQueue(_sessionSocket);

            _frameReader = new FrameReader(_sessionSocket);

            ActiveStreams = new ActiveStreams();

            OurMaxConcurrentStreams = 100; //Spec recommends value 100 by default
            RemoteMaxConcurrentStreams = 100;

            _flowControlManager = new FlowControlManager(this);

            SessionWindowSize = 0;
        }

        /// <summary>
        /// Pumps the incomming data and calls dispatch for it
        /// </summary>
        private void PumpIncommingData()
        {
            while (!_goAwayReceived && !_disposed)
            {
                Frame frame = null;
                try
                {
                    frame = _frameReader.ReadFrame();
                }
                catch (Exception)
                {
                    // Read failure, abort the connection/session.
                    Dispose();
                }
               
                if (frame == null)
                {
                    continue;
                }

                DispatchIncomingFrame(frame);
            }
        }

        /// <summary>
        /// Pumps the outgoing data to write queue
        /// </summary>
        /// <returns></returns>
        private Task PumpOutgoingData()
        {
            return _writeQueue.PumpToStreamAsync();
        }

        /// <summary>
        /// Dispatches the incoming frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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

                        //Remote side tries to open more streams than allowed
                        if (ActiveStreams.GetOpenedStreamsBy(_remoteEnd) + 1 > OurMaxConcurrentStreams)
                        {
                            Dispose();
                            return;
                        }

                        stream = new Http2Stream(headers, headersPlusPriorityFrame.StreamId,
                                                 headersPlusPriorityFrame.Priority, _writeQueue,
                                                 _flowControlManager, _comprProc);

                        ActiveStreams[stream.Id] = stream;

                        stream.OnClose += (o, args) =>
                        {
                            if (ActiveStreams.Remove(ActiveStreams[args.Id]) == false)
                            {
                                throw new ArgumentException("Cant remove stream from ActiveStreams");
                            }
                        };
                        break;

                    case FrameType.RstStream:
                        var resetFrame = (RstStreamFrame) frame;
                        stream = GetStream(resetFrame.StreamId);

                        //Frame came to already closed or unopened stream
                        if (stream == null || stream.Disposed)
                        {
                            //Ignore it
                            return;
                        }

                        Console.WriteLine("Got rst with code {0}", resetFrame.StatusCode);
                        stream.Dispose();
                        break;
                    case FrameType.Data:
                        var dataFrame = (DataFrame) frame;
                        stream = GetStream(dataFrame.StreamId);

                        //Frame came to already closed or unopened stream
                        if (stream == null || stream.Disposed)
                        {
                            //Ignore it
                            return;
                        }

                        //Aggressive window update
                        stream.WriteWindowUpdate(2000000);
                        break;
                    case FrameType.Ping:
                        var pingFrame = (PingFrame) frame;
                        if (pingFrame.Flags == FrameFlags.Pong)
                        {
                            _wasPingReceived = true;
                            _pingReceived.Set();
                        }
                        else
                        {
                            var pingResonseFrame = new PingFrame(true);
                            _writeQueue.WriteFrameAsync(pingResonseFrame, Priority.Pri0);
                        }
                        break;
                    case FrameType.Settings:
                        //Not first frame in the session.
                        //Client initiates connection and it send settings before request. 
                        //It means that if server will send settings then it will not be a first frame,
                        //because client initiates connection.
                        if (_ourEnd == ConnectionEnd.Server && !_wasSettingsReceived && ActiveStreams.Count != 0)
                        {
                            Dispose();
                        }

                        _wasSettingsReceived = true;
                        Task.Run(() => _settingsManager.ProcessSettings((SettingsFrame)frame, this,_flowControlManager));
                        break;
                    case FrameType.WindowUpdate:
                        var windowFrame = (WindowUpdateFrame) frame;

                        stream = GetStream(windowFrame.StreamId);
                        //Frame came to already closed or unopened stream
                        if (stream == null || stream.Disposed)
                        {
                            //Ignore it
                            return;
                        }
                        stream.UpdateWindowSize(windowFrame.Delta);

                        Task.Run(() => stream.PumpUnshippedFrames());
                        break;
                    case FrameType.Headers:
                        var headersFrame = (HeadersFrame) frame;
                        stream = GetStream(headersFrame.StreamId);

                        //Frame came to already closed or unopened stream
                        if (stream == null || stream.Disposed)
                        {
                            //Ignore it
                            return;
                        }

                        decompressedHeaders = _comprProc.Decompress(headersFrame.CompressedHeaders);
                        headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);

                        foreach (var key in headers.Keys)
                        {
                            stream.Headers.Add(key, headers[key]);
                        }

                        break;
                    case FrameType.GoAway:
                        _goAwayReceived = true;
                        Dispose();
                        break;
                    default:
                        throw new NotImplementedException(frame.FrameType.ToString());
                }

                if (stream != null)
                {
                    if (OnFrameReceived != null)
                    {
                        OnFrameReceived(this, new FrameReceivedEventArgs(stream, frame));
                    } 
                    //Tell the stream that it was the last frame
                    if (frame.IsFin)
                    {
                        Console.WriteLine("Final frame received");
                        stream.FinReceived = true;
                    }
                }
            }
            //Frame came for already closed stream. Ignore it.
            catch (KeyNotFoundException)
            {
                
            }
        }

        /// <summary>
        /// Gets the next id.
        /// </summary>
        /// <returns>Next stream id</returns>
        private int GetNextId()
        {
            _lastId += 2;
            return _lastId;
        }

        /// <summary>
        /// Creates new http2 stream.
        /// </summary>
        /// <param name="priority">The stream priority.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Thrown when trying to create more streams than allowed by the remote side</exception>
        private Http2Stream CreateStream(Priority priority)
        {
            if (ActiveStreams.GetOpenedStreamsBy(_ourEnd) + 1 > RemoteMaxConcurrentStreams)
            {
                Dispose();
                throw new InvalidOperationException("Trying to create more streams than allowed by the remote side!");
            }
            var stream = new Http2Stream(GetNextId(), priority, _writeQueue, _flowControlManager, _comprProc);
            
            stream.OnClose += (o, args) =>
                {
                    if (ActiveStreams.Remove(ActiveStreams[args.Id]) == false)
                    {
                        throw new ArgumentException("Cant remove stream from ActiveStreams");
                    }
                };

            stream.OnFrameSent += (o, args) =>
                {
                    if (OnFrameSent != null)
                    {
                        OnFrameSent(o, args);
                    }
                };

            ActiveStreams[stream.Id] = stream;
            return stream;
        }

        /// <summary>
        /// Sends the headers+priority with request headers.
        /// </summary>
        /// <param name="pairs">The header pairs.</param>
        /// <param name="priority">The stream priority.</param>
        /// <param name="isFin">True if initial headers+priority is also the final frame from endpoint.</param>
        public void SendRequest(Dictionary<string, string> pairs, int priority, bool isFin)
        {
            Contract.Assert(priority >= 0 && priority <= 7);
            var stream = CreateStream((Priority)priority);

            stream.WriteHeadersPlusPriorityFrame(pairs, isFin);
        }

        /// <summary>
        /// Gets the stream from active streams.
        /// </summary>
        /// <param name="id">The stream id.</param>
        /// <returns></returns>
        public Http2Stream GetStream(int id)
        {
            Http2Stream stream;
            if (!ActiveStreams.TryGetValue(id, out stream))
            {
                //Do nothing. It will be handled in dispatch stream method.
            }
            return stream;
        }

        /// <summary>
        /// Starts session.
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            //Writing settings. Settings must be the first frame in session.
            WriteSettings(new[] { new SettingsPair(0, SettingsIds.InitialWindowSize, 200000) });

            // Listen for incoming Http/2.0 frames
            Task incomingTask = new Task(() => PumpIncommingData());
            // Send outgoing Http/2.0 frames
            Task outgoingTask = new Task(() => PumpOutgoingData());
            incomingTask.Start();
            outgoingTask.Start();

            return Task.WhenAll(incomingTask, outgoingTask);
        }

        /// <summary>
        /// Writes the settings frame.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void WriteSettings(SettingsPair[] settings)
        {
            var frame = new SettingsFrame(new List<SettingsPair>(settings));

            _writeQueue.WriteFrameAsync(frame, Priority.Pri3);

            if (OnSettingsSent != null)
            {
                OnSettingsSent(this, new SettingsSentEventArgs(frame));
            }
        }

        /// <summary>
        /// Writes the go away frame.
        /// </summary>
        /// <param name="code">The code.</param>
        public void WriteGoAway(GoAwayStatusCode code)
        {
            var frame = new GoAwayFrame(_lastId, code);

            _writeQueue.WriteFrameAsync(frame, Priority.Pri3);
        }

        /// <summary>
        /// Pings session.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Ping()
        {
            var pingFrame = new PingFrame(false);
            _writeQueue.WriteFrameAsync(pingFrame, Priority.Pri0);
            var now = DateTime.UtcNow;

            _pingReceived.WaitOne(60000);
            _pingReceived.Reset();

            if (!_wasPingReceived)
            {
                //Remote endpoint was not answer at time.
                Dispose();
            }

            var newNow = DateTime.UtcNow;
            Console.WriteLine("Ping: {0}", (newNow - now).Milliseconds);
            _wasPingReceived = false;
            return newNow - now;
        }

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

            // Dispose of all streams
            foreach (Http2Stream stream in ActiveStreams.Values)
            {
                //Cancel all opened streams
                stream.WriteRst(ResetStatusCode.Cancel);
                stream.Dispose();
            }

            WriteGoAway(GoAwayStatusCode.Ok);

            OnSettingsSent = null;
            OnFrameReceived = null;
            OnFrameSent = null;

            _comprProc.Dispose();
            _sessionSocket.Close();

            Console.WriteLine("Session closed");
        }

        /// <summary>
        /// Occurs when settings frame was sent.
        /// </summary>
        public event EventHandler<SettingsSentEventArgs> OnSettingsSent;

        /// <summary>
        /// Occurs when frame was sent.
        /// </summary>
        public event EventHandler<FrameSentArgs> OnFrameSent;

        /// <summary>
        /// Occurs when frame was received.
        /// </summary>
        public event EventHandler<FrameReceivedEventArgs> OnFrameReceived;
    }
}
