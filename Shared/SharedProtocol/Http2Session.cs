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
        private readonly bool _goAwayReceived; 
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

        internal ActiveStreams ActiveStreams { get; private set; }

        //TODO take into account this variable
        internal Int32 OurMaxConcurrentStreams { get; set; }
        internal Int32 RemoteMaxConcurrentStreams { get; set; }

        public Int32 SessionWindowSize { get; set; }

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

                        Console.WriteLine("Got rst with code {0}", resetFrame.StatusCode);
                        stream.Dispose();
                        break;
                    case FrameType.Data:
                        var dataFrame = (DataFrame) frame;
                        stream = GetStream(dataFrame.StreamId);

                        //Aggressive window update
                        stream.WriteWindowUpdate(2000000);
                        break;
                    case FrameType.Ping:
                        //TODO Process ping correctly
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
                        stream.UpdateWindowSize(windowFrame.Delta);

                        Task.Run(() => stream.PumpUnshippedFrames());
                        break;
                    case FrameType.Headers:
                        var headersFrame = (HeadersFrame) frame;
                        stream = GetStream(headersFrame.StreamId);
                        decompressedHeaders = _comprProc.Decompress(headersFrame.CompressedHeaders);
                        headers = FrameHelpers.DeserializeHeaderBlock(decompressedHeaders);

                        foreach (var key in headers.Keys)
                        {
                            stream.Headers.Add(key, headers[key]);
                        }

                        break;
                    case FrameType.GoAway:
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
        #endregion

        #region Stream Handling
        private int GetNextId()
        {
            _lastId += 2;
            return _lastId;
        }

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

        public Http2Stream SendRequest(Dictionary<string, string> pairs, int priority, bool isFin)
        {
            Contract.Assert(priority >= 0 && priority <= 7);
            var stream = CreateStream((Priority)priority);

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
            WriteSettings(new[] { new SettingsPair(0, SettingsIds.InitialWindowSize, 200000) });

            Task.Run(() => Ping());
  
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

            if (OnSettingsSent != null)
            {
                OnSettingsSent(this, new SettingsSentEventArgs(frame));
            }
        }

        public void WriteGoAway(GoAwayStatusCode code)
        {
            var frame = new GoAwayFrame(_lastId, code);

            _writeQueue.WriteFrameAsync(frame, Priority.Pri3);
        }

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

            // Dispose of all streams
            foreach (Http2Stream stream in ActiveStreams.Values)
            {
                //TODO Is it correct?
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
        #endregion

        public event EventHandler<SettingsSentEventArgs> OnSettingsSent;

        public event EventHandler<FrameSentArgs> OnFrameSent;

        public event EventHandler<FrameReceivedEventArgs> OnFrameReceived;
    }
}
