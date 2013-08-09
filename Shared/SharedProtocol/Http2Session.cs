using System.Linq;
using System.Threading;
using SharedProtocol.Exceptions;
using SharedProtocol.Compression.Http2DeltaHeadersCompression;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using SharedProtocol.Settings;
using SharedProtocol.FlowControl;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using SharedProtocol.Extensions;
using SharedProtocol.Utils;

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
        private readonly ICompressionProcessor _comprProc;
        private readonly FlowControlManager _flowControlManager;
        private readonly ConnectionEnd _ourEnd;
        private readonly ConnectionEnd _remoteEnd;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private int _lastId;
        private bool _wasSettingsReceived = false;
        private bool _wasPingReceived = false;
        private bool _wasResponseReceived = false;
        private IList<KeyValuePair<string, string>> _toBeContinuedHeaders = null;
        private Frame _toBeContinuedFrame = null;
        private readonly Dictionary<string, string> _handshakeHeaders;

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

        /// <summary>
        /// Request sent event.
        /// </summary>
        public event EventHandler<RequestSentEventArgs> OnRequestSent;

        /// <summary>
        /// Session closed event.
        /// </summary>
        public event EventHandler<EventArgs> OnSessionDisposed;


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
 
        public Http2Session(SecureSocket sessionSocket, ConnectionEnd end, 
                            bool usePriorities, bool useFlowControl,
                            IDictionary<string, object> handshakeResult = null)
        {
            _ourEnd = end;
            _usePriorities = usePriorities;
            _useFlowControl = useFlowControl;
            _handshakeHeaders = new Dictionary<string, string>(16);
            ApplyHandshakeResults(handshakeResult);

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
            _comprProc = new CompressionProcessor(_ourEnd);
            _sessionSocket = sessionSocket;

            _frameReader = new FrameReader(_sessionSocket);

            ActiveStreams = new ActiveStreams();

            _writeQueue = new WriteQueue(_sessionSocket, ActiveStreams, _usePriorities);

            OurMaxConcurrentStreams = 100; //Spec recommends value 100 by default
            RemoteMaxConcurrentStreams = 100;

            _flowControlManager = new FlowControlManager(this);

            if (!_useFlowControl)
            {
                _flowControlManager.Options = (byte) FlowControlOptions.DontUseFlowControl;
            }

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

                    if (!_wasResponseReceived)
                    {
                        _wasResponseReceived = true;
                    }
                }
                catch (Exception)
                {
                    // Read failure, abort the connection/session.
                    Dispose();
                }

                if (frame == null)
                {
                    Thread.Sleep(5);
                }
                else
                {
                    DispatchIncomingFrame(frame);
                }
            }
        }

        /// <summary>
        /// Pumps the outgoing data to write queue
        /// </summary>
        /// <returns></returns>
        private Task PumpOutgoingData()
        {
             return Task.Run(() =>
                 {
                     try
                     {
                         _writeQueue.PumpToStream();
                     }
                     catch (Exception)
                     {
                         Http2Logger.LogError("Sending frame was cancelled because connection was lost");
                         Dispose();
                     }
                 });
        }

        /// <summary>
        /// Dispatches the incoming frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void DispatchIncomingFrame(Frame frame)
        {
            Http2Stream stream = null;

            //Spec 03 tells that frame with continues flag MUST be followed by a frame with the same type
            //and the same stread id.
            if (_toBeContinuedHeaders != null)
            {
                if (_toBeContinuedFrame.FrameType != frame.FrameType
                    || _toBeContinuedFrame.StreamId != frame.StreamId)
                {
                    //If not, we must close the session.
                    Dispose();
                    return;
                }
            }

            try
            {
                switch (frame.FrameType)
                {
                    case FrameType.Headers:
                        Http2Logger.LogDebug("New headers with id = " + frame.StreamId);
                        var headersFrame = (HeadersFrame)frame;
                        var serializedHeaders = new byte[headersFrame.CompressedHeaders.Count];

                        Buffer.BlockCopy(headersFrame.CompressedHeaders.Array,
                                         headersFrame.CompressedHeaders.Offset,
                                         serializedHeaders, 0, serializedHeaders.Length);

                        var decompressedHeaders = _comprProc.Decompress(serializedHeaders);
                        var headers = new HeadersList(decompressedHeaders);

                        if (!headersFrame.IsEndHeaders)
                        {
                            _toBeContinuedHeaders = decompressedHeaders;
                            _toBeContinuedFrame = headersFrame;
                            break;
                        }

                        if (_toBeContinuedHeaders != null)
                        {
                            headers.AddRange(_toBeContinuedHeaders);
                        }

                        headersFrame.Headers.AddRange(headers);
                        foreach (var header in headers)
                        {
                            Http2Logger.LogDebug("Stream {0} header: {1}={2}", frame.StreamId, header.Key, header.Value);
                        }

                        stream = GetStream(headersFrame.StreamId);

                        if (stream == null)
                        {
                            if (_ourEnd == ConnectionEnd.Server)
                            {
                                string path = headers.GetValue(":path");
                                if (path == null)
                                {
                                    path = _handshakeHeaders.ContainsKey(":path")
                                               ? _handshakeHeaders[":path"]
                                               : @"\index.html";
                                    headers.Add(new KeyValuePair<string, string>(":path", path));
                                }
                            }
                            else
                            {
                                headers.AddRange(_handshakeHeaders);
                            }
                            stream = CreateStream(headers, frame.StreamId);

                            _toBeContinuedFrame = null;
                            _toBeContinuedHeaders = null;
                        }

                        break;

                    case FrameType.Priority:
                        var priorityFrame = (PriorityFrame)frame;
                        Http2Logger.LogDebug("Priority frame. StreamId: {0} Priority: {1}", priorityFrame.StreamId, priorityFrame.Priority);
                        stream = GetStream(priorityFrame.StreamId);
                        if (_usePriorities)
                        {
                            stream.Priority = priorityFrame.Priority;
                        }
                        break;

                    case FrameType.RstStream:
                        var resetFrame = (RstStreamFrame)frame;
                        stream = GetStream(resetFrame.StreamId);

                        if (stream != null)
                        {
                            Http2Logger.LogDebug("RST frame with code " + resetFrame.StatusCode);
                            stream.Dispose();
                        }
                        break;
                    case FrameType.Data:
                        var dataFrame = (DataFrame)frame;
                        Http2Logger.LogDebug("Data frame. StreamId:{0} Length:{1}", dataFrame.StreamId, dataFrame.FrameLength);
                        stream = GetStream(dataFrame.StreamId);

                        //Aggressive window update
                        if (stream != null && stream.IsFlowControlEnabled)
                        {
                            stream.WriteWindowUpdate(2000000);
                        }
                        break;
                    case FrameType.Ping:
                        var pingFrame = (PingFrame)frame;
                        Http2Logger.LogDebug("Ping frame with StreamId:{0} Payload:{1}", pingFrame.StreamId, pingFrame.Payload.Count);

                        if (pingFrame.FrameLength != PingFrame.PayloadLength)
                        {
                            throw new ProtocolError(ResetStatusCode.ProtocolError, "Ping payload size is not equal to 8");
                        }

                        if (pingFrame.IsPong)
                        {
                            _wasPingReceived = true;
                            _pingReceived.Set();
                        }
                        else
                        {
                            var pongFrame = new PingFrame(true, pingFrame.Payload.ToArray());
                            _writeQueue.WriteFrame(pongFrame);
                        }
                        break;
                    case FrameType.Settings:
                        //Not first frame in the session.
                        //Client initiates connection and sends settings before request. 
                        //It means that if server sent settings before it will not be a first frame,
                        //because client initiates connection.
                        if (_ourEnd == ConnectionEnd.Server && !_wasSettingsReceived
                            && (ActiveStreams.Count > 0))
                        {
                            Dispose();
                            return;
                        }

                        var settingFrame = (SettingsFrame)frame;
                        Http2Logger.LogDebug("Settings frame. Entry count: {0} StreamId: {1}", settingFrame.EntryCount, settingFrame.StreamId);
                        _wasSettingsReceived = true;
                        _settingsManager.ProcessSettings(settingFrame, this, _flowControlManager);

                        if (_ourEnd == ConnectionEnd.Server && _sessionSocket.SecureProtocol == SecureProtocol.None)
                        {
                            //The HTTP/1.1 request that is sent prior to upgrade is associated with
                            //stream 1 and is assigned the highest possible priority.  Stream 1 is
                            //implicitly half closed from the client toward the server, since the
                            //request is completed as an HTTP/1.1 request.  After commencing the
                            //HTTP/2.0 connection, stream 1 is used for the response.
                            stream = CreateStream(Priority.Pri0);
                            stream.EndStreamReceived = true;
                            stream.Headers.Add(new KeyValuePair<string, string>(":method", _handshakeHeaders[":method"]));
                            stream.Headers.Add(new KeyValuePair<string, string>(":path", _handshakeHeaders[":path"]));
                            OnFrameReceived(this, new FrameReceivedEventArgs(stream, new HeadersFrame(stream.Id, true)));
                        }

                        break;
                    case FrameType.WindowUpdate:
                        if (_useFlowControl)
                        {
                            var windowFrame = (WindowUpdateFrame)frame;
                            Http2Logger.LogDebug("WindowUpdate frame. Delta: {0} StreamId: {1}", windowFrame.Delta, windowFrame.StreamId);
                            stream = GetStream(windowFrame.StreamId);

                            if (stream != null)
                            {
                                stream.UpdateWindowSize(windowFrame.Delta);
                                stream.PumpUnshippedFrames();
                            }
                        }
                        break;

                    case FrameType.GoAway:
                        _goAwayReceived = true;
                        Http2Logger.LogDebug("GoAway frame received");
                        Dispose();
                        break;
                    default:
                        throw new NotImplementedException(frame.FrameType.ToString());
                }

                if (stream != null && frame is IEndStreamFrame && ((IEndStreamFrame)frame).IsEndStream)
                {
                    //Tell the stream that it was the last frame
                    Http2Logger.LogDebug("Final frame received for stream with id = " + stream.Id);
                    stream.EndStreamReceived = true;
                }

                if (stream != null && OnFrameReceived != null)
                {
                    OnFrameReceived(this, new FrameReceivedEventArgs(stream, frame));
                }
            }

            //Frame came for already closed stream. Ignore it.
            //Spec:
            //An endpoint that sends RST_STREAM MUST ignore
            //frames that it receives on closed streams if it sends RST_STREAM.
            //
            //An endpoint MUST NOT send frames on a closed stream.  An endpoint
            //that receives a frame after receiving a RST_STREAM or a frame
            //containing a END_STREAM flag on that stream MUST treat that as a
            //stream error (Section 5.4.2) of type PROTOCOL_ERROR.
            catch (Http2StreamNotFoundException)
            {
                if (stream != null)
                {
                    stream.WriteRst(ResetStatusCode.ProtocolError);
                }
                else
                {
                    //GoAway?
                }
            }
            catch (CompressionError ex)
            {
                //The endpoint is unable to maintain the compression context for the connection.
                Http2Logger.LogError("Compression error occured: " + ex.Message);
                Close(ResetStatusCode.CompressionError);
            }
            catch (ProtocolError pEx)
            {
                Http2Logger.LogError("Protocol error occured: " + pEx.Message);
                Close(pEx.Code);
            }
        }

        /// <summary>
        /// Creates stream.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="streamId"></param>
        /// <returns></returns>
        private Http2Stream CreateStream(HeadersList headers, int streamId)
        {
            if (ActiveStreams.GetOpenedStreamsBy(_remoteEnd) + 1 > OurMaxConcurrentStreams)
            {
                //Remote side tries to open more streams than allowed
                Dispose();
                throw new InvalidOperationException("Trying to create more streams than allowed!");
            }

            Http2Stream stream = new Http2Stream(headers, streamId,
                                      _writeQueue, _flowControlManager,
                                      _comprProc);

            ActiveStreams[stream.Id] = stream;

            stream.OnClose += (o, args) =>
            {
                if (!ActiveStreams.Remove(ActiveStreams[args.Id]))
                {
                    throw new ArgumentException("Cant remove stream from ActiveStreams");
                }
            };

            return stream;
        }

        private void ApplyHandshakeResults(IDictionary<string, object> handshakeResult)
        {
            foreach (var entry in handshakeResult.Keys.Where(entry => handshakeResult[entry] is string))
            {
                _handshakeHeaders.Add(entry, handshakeResult[entry] as string);
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
                throw new InvalidOperationException("Trying to create more streams than allowed!");
            }

            var id = GetNextId();
            if (_usePriorities)
            {
                ActiveStreams[id] = new Http2Stream(id, _writeQueue, _flowControlManager, _comprProc, priority);
            }
            else
            {
                ActiveStreams[id] = new Http2Stream(id, _writeQueue, _flowControlManager, _comprProc);
            }

            ActiveStreams[id].OnClose += (o, args) =>
                {
                    if (!ActiveStreams.Remove(ActiveStreams[args.Id]))
                    {
                        throw new ArgumentException("Can't remove stream from ActiveStreams.");
                    }
                };

            ActiveStreams[id].OnFrameSent += (o, args) =>
                {
                    if (OnFrameSent != null)
                    {
                        OnFrameSent(o, args);
                    }
                };

            return ActiveStreams[id];
        }

        /// <summary>
        /// Sends the headers with request headers.
        /// </summary>
        /// <param name="pairs">The header pairs.</param>
        /// <param name="priority">The stream priority.</param>
        /// <param name="isEndStream">True if initial headers+priority is also the final frame from endpoint.</param>
        public void SendRequest(HeadersList pairs, Priority priority, bool isEndStream)
        {
            var stream = CreateStream(priority);

            stream.WriteHeadersFrame(pairs, isEndStream);

            if (OnRequestSent != null)
            {
                OnRequestSent(this, new RequestSentEventArgs(stream));
            }
        }

        /// <summary>
        /// Gets the stream from active streams.
        /// </summary>
        /// <param name="id">The stream id.</param>
        /// <returns></returns>
        internal Http2Stream GetStream(int id)
        {
            Http2Stream stream;
            if (!ActiveStreams.TryGetValue(id, out stream))
            {
                return null;
            }
            return stream;
        }

        /// <summary>
        /// Starts session.
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            Http2Logger.LogDebug("Session start");
            //Write settings. Settings must be the first frame in session.

            if (_useFlowControl)
            {
                WriteSettings(new[]
                    {
                        new SettingsPair(SettingsFlags.None, SettingsIds.InitialWindowSize, 200000)
                    });
            }
            else
            {
                WriteSettings(new[]
                    {
                        new SettingsPair(SettingsFlags.None, SettingsIds.InitialWindowSize, 200000),
                        new SettingsPair(SettingsFlags.None, SettingsIds.FlowControlOptions, (byte) FlowControlOptions.DontUseFlowControl)
                    });
            }
            // Listen for incoming Http/2.0 frames
            var incomingTask = new Task(PumpIncommingData);
            // Send outgoing Http/2.0 frames
            var outgoingTask = new Task(() => PumpOutgoingData());
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

            _writeQueue.WriteFrame(frame);

            if (OnSettingsSent != null)
            {
                OnSettingsSent(this, new SettingsSentEventArgs(frame));
            }
        }

        /// <summary>
        /// Writes the go away frame.
        /// </summary>
        /// <param name="code">The code.</param>
        public void WriteGoAway(ResetStatusCode code)
        {
            //if there were no streams opened
            if (_lastId == -1)
            {
                _lastId = 0; //then set lastId to 0 as spec tells. (See GoAway chapter)
            }

            var frame = new GoAwayFrame(_lastId, code);

            _writeQueue.WriteFrame(frame);
        }

        /// <summary>
        /// Pings session.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Ping()
        {
            var pingFrame = new PingFrame(false);
            _writeQueue.WriteFrame(pingFrame);
            var now = DateTime.UtcNow;

            _pingReceived.WaitOne(3000);
            _pingReceived.Reset();

            if (!_wasPingReceived)
            {
                //Remote endpoint was not answer at time.
                Dispose();
            }

            var newNow = DateTime.UtcNow;
            Http2Logger.LogDebug("Ping: " + (newNow - now).Milliseconds);
            _wasPingReceived = false;
            return newNow - now;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Close(ResetStatusCode.None);
        }

        private void Close(ResetStatusCode status)
        {
            if (_disposed)
                return;

            Http2Logger.LogDebug("Session closing");
            _disposed = true;

            // Dispose of all streams
            foreach (Http2Stream stream in ActiveStreams.Values)
            {
                //Cancel all opened streams
                stream.WriteRst(ResetStatusCode.Cancel);
                stream.Dispose();
            }

            OnSettingsSent = null;
            OnFrameReceived = null;
            OnFrameSent = null;

            if (!_goAwayReceived)
            {
                WriteGoAway(status);

                if (_writeQueue != null)
                {
                    _writeQueue.Flush();
                    _writeQueue.Dispose();
                }
            }

            _comprProc.Dispose();
            _sessionSocket.Close();

            if (OnSessionDisposed != null)
            {
                OnSessionDisposed(this, null);
            }
            OnSessionDisposed = null;

            Http2Logger.LogDebug("Session closed");
        }
    }
}
