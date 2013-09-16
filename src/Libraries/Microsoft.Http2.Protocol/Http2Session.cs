using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Exceptions;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.Compression;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Settings;
using Microsoft.Http2.Protocol.FlowControl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    /// <summary>
    /// This class creates and closes session, pumps incoming and outcoming frames and dispatches them.
    /// It defines events for request handling by subscriber. Also it is responsible for sending some frames.
    /// </summary>
    public class Http2Session : IDisposable
    {
        private bool _goAwayReceived;
        private readonly FrameReader _frameReader;
        private readonly WriteQueue _writeQueue;
        private readonly DuplexStream _ioStream;
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
        private bool _wasSettingsReceived;
        private bool _wasPingReceived;
        private bool _wasResponseReceived;
        private Frame _lastFrame;
        private readonly CancellationToken _cancelSessionToken;
        private readonly List<HeadersSequence> _headersSequences; 
        private const string ClientSessionHeader = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
 
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
        public event EventHandler<System.EventArgs> OnSessionDisposed;

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
        internal Int32 InitialWindowSize { get; set; }
        internal Int32 SessionWindowSize { get; set; }
 
        public Http2Session(DuplexStream stream, ConnectionEnd end, 
                            bool usePriorities, bool useFlowControl,
                            CancellationToken cancel,
                            int initialWindowSize = 200000,
                            int maxConcurrentStream = 100)
        {
            _ourEnd = end;
            _usePriorities = usePriorities;
            _useFlowControl = useFlowControl;
            _cancelSessionToken = cancel;

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
            _ioStream = stream;

            _ioStream.OnClose += IoStreamClosedHandler;

            _frameReader = new FrameReader(_ioStream);

            ActiveStreams = new ActiveStreams();

            _writeQueue = new WriteQueue(_ioStream, ActiveStreams, _usePriorities);
            OurMaxConcurrentStreams = maxConcurrentStream;
            RemoteMaxConcurrentStreams = maxConcurrentStream;
            InitialWindowSize = initialWindowSize;

            _flowControlManager = new FlowControlManager(this);

            if (!_useFlowControl)
            {
                _flowControlManager.Options = (byte) FlowControlOptions.DontUseFlowControl;
            }

            SessionWindowSize = 0;
            _headersSequences = new List<HeadersSequence>();
        }

        private void SendSessionHeader()
        {
            _ioStream.Write(Encoding.UTF8.GetBytes(ClientSessionHeader));
        }

        private async Task<bool> GetSessionHeaderAndVerifyIt(DuplexStream incomingClient)
        {
            var sessionHeaderBuffer = new byte[ClientSessionHeader.Length];

            if(!incomingClient.WaitForDataAvailable(60000))
            {
                throw new TimeoutException(String.Format("Session header was not received in timeout {0}", 60000));
            }
            
            await incomingClient.ReadAsync(sessionHeaderBuffer, 0, 
                                           sessionHeaderBuffer.Length,
                                           _cancelSessionToken);


            var receivedHeader = Encoding.UTF8.GetString(sessionHeaderBuffer);

            return string.Equals(receivedHeader, ClientSessionHeader, StringComparison.OrdinalIgnoreCase);
        }

        //Calls only in unsecure connection case
        private void DispatchInitialRequest(IDictionary<string, string> initialRequest)
        {
            if (!initialRequest.ContainsKey(":path"))
            {
                initialRequest.Add(":path", "/index.html");
            }


            var initialStream = CreateStream(new HeadersList(initialRequest), 1);

            //spec 06:
            //A stream identifier of one (0x1) is used to respond to the HTTP/1.1
            //request which was specified during Upgrade (see Section 3.2).  After
            //the upgrade completes, stream 0x1 is "half closed (local)" to the
            //client.  Therefore, stream 0x1 cannot be selected as a new stream
            //identifier by a client that upgrades from HTTP/1.1.
            if (_ourEnd == ConnectionEnd.Client)
            {
                GetNextId();
                initialStream.EndStreamSent = true;
            }
            else
            {
                initialStream.EndStreamReceived = true;
                if (OnFrameReceived != null)
                {
                    OnFrameReceived(this, new FrameReceivedEventArgs(initialStream, new HeadersFrame(1, new byte[0])));
                }
            }
        }

        /// <summary>
        /// Starts session.
        /// </summary>
        /// <returns></returns>
        public async Task Start(IDictionary<string, string> initialRequest = null)
        {
            Http2Logger.LogDebug("Session start");

            if (_ourEnd == ConnectionEnd.Server)
            {

                if (!await GetSessionHeaderAndVerifyIt(_ioStream))
                {
                    Dispose();
                    //throw something?
                    return;
                }
            }
            else
            {
                SendSessionHeader();
            }

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
            var incomingTask = new Task(() =>
                {
                    Thread.CurrentThread.Name = "Frame listening thread";
                    PumpIncommingData();
                });

            // Send outgoing Http/2.0 frames
            var outgoingTask = new Task(() =>
                {
                    Thread.CurrentThread.Name = "Frame writing thread";
                    PumpOutgoingData();
                });

            incomingTask.Start();
            outgoingTask.Start();
            var endPumpsTask = Task.WhenAll(incomingTask, outgoingTask);

            //Handle upgrade handshake headers.
            if (initialRequest != null && !_ioStream.IsSecure)
                DispatchInitialRequest(initialRequest);

            //Cancellation token
            endPumpsTask.Wait();
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
                catch (Exception ex)
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
                         _writeQueue.PumpToStream(_cancelSessionToken);
                     }
                     catch (OperationCanceledException)
                     {
                         Http2Logger.LogError("Handling session was cancelled");
                         Dispose();
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
            
            try
            {
                if (frame.FrameLength > Constants.MaxFrameContentSize)
                {
                    throw new ProtocolError(ResetStatusCode.FrameTooLarge, 
                                String.Format("Frame too large: Type: {0} {1}", frame.FrameType, frame.FrameLength));
                }

                //Settings MUST be first frame in the session from server and 
                //client MUST send settings immediately after connection header.
                //This means that settings ALWAYS first frame in the session.
                //This block checks if it doesnt.
                if (frame.FrameType != FrameType.Settings && !_wasSettingsReceived)
                {
                    throw new ProtocolError(ResetStatusCode.ProtocolError,
                                            "Settings was not the first frame in the session");
                }

                HeadersSequence sequence;
                switch (frame.FrameType)
                {
                    case FrameType.Headers:
                        Http2Logger.LogDebug("New headers with id = " + frame.StreamId);
                        var headersFrame = (HeadersFrame)frame;

                        //spec 06:
                        //If a HEADERS frame
                        //is received whose stream identifier field is 0x0, the recipient MUST
                        //respond with a connection error (Section 5.4.1) of type
                        //PROTOCOL_ERROR [PROTOCOL_ERROR].
                        if (headersFrame.StreamId == 0)
                        {
                            throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming headers frame with id = 0");
                        }

                        var serializedHeaders = new byte[headersFrame.CompressedHeaders.Count];

                        Buffer.BlockCopy(headersFrame.CompressedHeaders.Array,
                                         headersFrame.CompressedHeaders.Offset,
                                         serializedHeaders, 0, serializedHeaders.Length);

                        var decompressedHeaders = _comprProc.Decompress(serializedHeaders);
                        var headers = new HeadersList(decompressedHeaders);
                        foreach (var header in headers)
                        {
                            Http2Logger.LogDebug("Stream {0} header: {1}={2}", frame.StreamId, header.Key, header.Value);
                        }
                        headersFrame.Headers.AddRange(headers);

                        sequence = _headersSequences.Find(seq => seq.StreamId == headersFrame.StreamId);
                        if (sequence == null)
                        {
                            sequence = new HeadersSequence(headersFrame.StreamId, headersFrame);
                            _headersSequences.Add(sequence);
                        }
                        else
                        {
                            sequence.AddHeaders(headersFrame);
                        }

                        if (!sequence.IsComplete)
                        {
                            return;
                        }

                        stream = GetStream(headersFrame.StreamId) ?? CreateStream(sequence.Headers, frame.StreamId);

                        break;
                    case FrameType.Continuation:

                        if (!(_lastFrame is ContinuationFrame || _lastFrame is HeadersFrame))
                            throw new ProtocolError(ResetStatusCode.ProtocolError, "Last frame was not headers or continuation");

                        Http2Logger.LogDebug("New continuation with id = " + frame.StreamId);
                        var contFrame = (HeadersFrame)frame;

                        if (contFrame.StreamId == 0)
                        {
                            throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming continuation frame with id = 0");
                        }

                        var serHeaders = new byte[contFrame.CompressedHeaders.Count];

                        Buffer.BlockCopy(contFrame.CompressedHeaders.Array,
                                         contFrame.CompressedHeaders.Offset,
                                         serHeaders, 0, serHeaders.Length);

                        var decomprHeaders = _comprProc.Decompress(serHeaders);
                        var contHeaders = new HeadersList(decomprHeaders);
                        foreach (var header in contHeaders)
                        {
                            Http2Logger.LogDebug("Stream {0} header: {1}={2}", frame.StreamId, header.Key, header.Value);
                        }
                        contFrame.Headers.AddRange(contHeaders);
                        sequence = _headersSequences.Find(seq => seq.StreamId == contFrame.StreamId);
                        if (sequence == null)
                        {
                            sequence = new HeadersSequence(contFrame.StreamId, contFrame);
                            _headersSequences.Add(sequence);
                        }
                        else
                        {
                            sequence.AddHeaders(contFrame);
                        }

                        if (!sequence.IsComplete)
                        {
                            return;
                        }

                        stream = GetStream(contFrame.StreamId) ?? CreateStream(sequence.Headers, frame.StreamId);
                        break;
                    case FrameType.Priority:
                        var priorityFrame = (PriorityFrame)frame;

                        //spec 06:
                        //The PRIORITY frame is associated with an existing stream.  If a
                        //PRIORITY frame is received with a stream identifier of 0x0, the
                        //recipient MUST respond with a connection error (Section 5.4.1) of
                        //type PROTOCOL_ERROR [PROTOCOL_ERROR].
                        if (priorityFrame.StreamId == 0)
                        {
                            throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming headers frame with id = 0");
                        }

                        Http2Logger.LogDebug("Priority frame. StreamId: {0} Priority: {1}", priorityFrame.StreamId, priorityFrame.Priority);
                        stream = GetStream(priorityFrame.StreamId);
                        if (_usePriorities)
                        {
                            //06
                            //A receiver can ignore WINDOW_UPDATE [WINDOW_UPDATE] or PRIORITY
                            //[PRIORITY] frames in this state.
                            if (stream != null && !stream.EndStreamReceived)
                            {
                                stream.Priority = priorityFrame.Priority;
                            }
                            //Do not signal an error because (06)
                            //WINDOW_UPDATE [WINDOW_UPDATE], PRIORITY [PRIORITY], or RST_STREAM
                            //[RST_STREAM] frames can be received in this state for a short
                            //period after a frame containing an END_STREAM flag is sent.
                        }
                        break;

                    case FrameType.RstStream:
                        var resetFrame = (RstStreamFrame)frame;
                        stream = GetStream(resetFrame.StreamId);

                        if (stream != null)
                        {
                            Http2Logger.LogDebug("RST frame with code {0} for id ", resetFrame.StatusCode, resetFrame.StreamId);
                            stream.Dispose(resetFrame.StatusCode);
                        }
                        //Do not signal an error because (06)
                        //WINDOW_UPDATE [WINDOW_UPDATE], PRIORITY [PRIORITY], or RST_STREAM
                        //[RST_STREAM] frames can be received in this state for a short
                        //period after a frame containing an END_STREAM flag is sent.
                        break;
                    case FrameType.Data:
                        var dataFrame = (DataFrame)frame;

                        stream = GetStream(dataFrame.StreamId);

                        //Aggressive window update
                        if (stream != null)
                        {
                            Http2Logger.LogDebug("Data frame. StreamId: {0} Length: {1}", dataFrame.StreamId, dataFrame.FrameLength);
                            if (stream.IsFlowControlEnabled)
                            {
                                stream.WriteWindowUpdate(200000);
                            }
                            stream.EnqueueDataFrame(dataFrame);
                        }
                        else
                        {
                            throw new Http2StreamNotFoundException(dataFrame.StreamId);
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
                        _wasSettingsReceived = true;
                        var settingFrame = (SettingsFrame)frame;
                        Http2Logger.LogDebug("Settings frame. Entry count: {0} StreamId: {1}", settingFrame.EntryCount, settingFrame.StreamId);
                        _settingsManager.ProcessSettings(settingFrame, this, _flowControlManager);
                        break;
                    case FrameType.WindowUpdate:
                        if (_useFlowControl)
                        {
                            var windowFrame = (WindowUpdateFrame)frame;
                            Http2Logger.LogDebug("WindowUpdate frame. Delta: {0} StreamId: {1}", windowFrame.Delta, windowFrame.StreamId);
                            stream = GetStream(windowFrame.StreamId);

                            //06
                            //A receiver can ignore WINDOW_UPDATE [WINDOW_UPDATE] or PRIORITY
                            //[PRIORITY] frames in this state.
                            //Tsyshnatiy: it can ignore, but it can not ignore also. I'm using it for
                            //initial request handling
                            if (stream != null)
                            {
                                stream.UpdateWindowSize(windowFrame.Delta);
                                stream.PumpUnshippedFrames();
                            }
                            //Do not signal an error because (06)
                            //WINDOW_UPDATE [WINDOW_UPDATE], PRIORITY [PRIORITY], or RST_STREAM
                            //[RST_STREAM] frames can be received in this state for a short
                            //period after a frame containing an END_STREAM flag is sent.
                        }
                        break;

                    case FrameType.GoAway:
                        _goAwayReceived = true;
                        Http2Logger.LogDebug("GoAway frame received");
                        Dispose();
                        break;
                    default:
                        //Item 4.1 in 06 spec: Implementations MUST ignore frames of unsupported or unrecognized types
                        Http2Logger.LogDebug("Unknown frame received. Ignoring it");
                        break;
                }

                _lastFrame = frame;

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

            //An endpoint MUST NOT send frames on a closed stream.  An endpoint
            //that receives a frame after receiving a RST_STREAM [RST_STREAM] or
            //a frame containing a END_STREAM flag on that stream MUST treat
            //that as a stream error (Section 5.4.2) of type STREAM_CLOSED
            //[STREAM_CLOSED].
            catch (Http2StreamNotFoundException ex)
            {
                Http2Logger.LogDebug("Frame for already closed stream with Id = {0}", ex.Id);
                _writeQueue.WriteFrame(new RstStreamFrame(ex.Id, ResetStatusCode.StreamClosed));
            }
            catch (CompressionError ex)
            {
                //The endpoint is unable to maintain the compression context for the connection.
                Http2Logger.LogError("Compression error occurred: " + ex.Message);
                Close(ResetStatusCode.CompressionError);
            }
            catch (ProtocolError pEx)
            {
                Http2Logger.LogError("Protocol error occurred: " + pEx.Message);
                Close(pEx.Code);
            }
            catch (Exception ex)
            {
                Http2Logger.LogError("Unknown error occurred: " + ex.Message);
                Close(ResetStatusCode.InternalError);
            }
        }

        /// <summary>
        /// Creates stream.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="streamId"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        private Http2Stream CreateStream(HeadersList headers, int streamId, Priority priority = Priority.None)
        {
            if (ActiveStreams.GetOpenedStreamsBy(_remoteEnd) + 1 > OurMaxConcurrentStreams)
            {
                //Remote side tries to open more streams than allowed
                Dispose();
                throw new InvalidOperationException("Trying to create more streams than allowed!");
            }

            if (priority == Priority.None)
                priority = Constants.DefaultStreamPriority;

            var stream = new Http2Stream(headers, streamId,
                                         _writeQueue, _flowControlManager,
                                         _comprProc, priority);

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

                    var streamSequence = _headersSequences.Find(seq => seq.StreamId == args.Id);

                    if (streamSequence != null)
                        _headersSequences.Remove(streamSequence);
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

            stream.WriteHeadersFrame(pairs, isEndStream, true);

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

        private void IoStreamClosedHandler(object sender, System.EventArgs args)
        {
            //If ioStream was closed, then session should be closed too
            Dispose();
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
            foreach (var stream in ActiveStreams.Values)
            {
                //Cancel all opened streams
                //stream.WriteRst(ResetStatusCode.Cancel);
                stream.Dispose(ResetStatusCode.Cancel);
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

            _ioStream.Close();
            
            if (OnSessionDisposed != null)
            {
                OnSessionDisposed(this, null);
            }
            OnSessionDisposed = null;

            Http2Logger.LogDebug("Session closed");
        }
    }
}
