// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Exceptions;
using OpenSSL;
using Microsoft.Http2.Protocol.Compression;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.FlowControl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Utils;
using OpenSSL.SSL;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class creates and closes session, pumps incoming and outcoming frames and dispatches them.
    /// It defines events for request handling by subscriber. Also it is responsible for sending some frames.
    /// </summary>
    public partial class Http2Session : IDisposable
    {
        private bool _goAwayReceived;
        private FrameReader _frameReader;
        private WriteQueue _writeQueue;
        private Stream _ioStream;
        private ManualResetEvent _pingReceived = new ManualResetEvent(false);
        private ManualResetEvent _settingsAckReceived = new ManualResetEvent(false);
        private bool _disposed;
        private ICompressionProcessor _comprProc;
        private readonly FlowControlManager _flowControlManager;
        private readonly ConnectionEnd _ourEnd;
        private readonly ConnectionEnd _remoteEnd;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly bool _isSecure;
        private int _lastId;            //streams creation
        private int _lastPromisedId;    //check pushed  (server) streams ids
        private bool _wasSettingsReceived;
        private bool _wasResponseReceived;
        private Frame _lastFrame;
        private readonly CancellationToken _cancelSessionToken;
        private readonly HeadersSequenceList _headersSequences; 
        private const string ClientSessionHeader = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        private Dictionary<int, string> _promisedResources; 

        /// <summary>
        /// Occurs when settings frame was sent.
        /// </summary>
        public event EventHandler<SettingsSentEventArgs> OnSettingsSent;

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
        /// Gets the stream dictionary.
        /// </summary>
        /// <value>
        /// The stream dictionary.
        /// </value>
        internal StreamDictionary StreamDictionary { get; private set; }

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
        internal bool IsPushEnabled { get; private set; }

        public Http2Session(Stream stream, ConnectionEnd end, 
                            bool usePriorities, bool useFlowControl, bool isSecure,
                            CancellationToken cancel,
                            int initialWindowSize = Constants.InitialFlowControlWindowSize,
                            int maxConcurrentStreams = Constants.DefaultMaxConcurrentStreams)
        {

            if (stream == null)
                throw new ArgumentNullException("stream is null");

            if (cancel == null)
                throw new ArgumentNullException("cancellation token is null");

            if (maxConcurrentStreams <= 0)
                throw new ArgumentOutOfRangeException("maxConcurrentStreams cant be less or equal then 0");

            if (initialWindowSize <= 0 && useFlowControl)
                throw new ArgumentOutOfRangeException("initialWindowSize cant be less or equal then 0");

            _ourEnd = end;
            _usePriorities = usePriorities;
            _useFlowControl = useFlowControl;
            _isSecure = isSecure;

            _cancelSessionToken = cancel;

            if (_ourEnd == ConnectionEnd.Client)
            {
                _remoteEnd = ConnectionEnd.Server;
                _lastId = -1; // Streams opened by client are odd

                //if we got unsecure connection then server will respond with id == 1. We cant initiate 
                //new stream with id == 1.
                if (!(stream is SslStream))
                {
                    _lastId = 3;
                }
            }
            else
            {
                _remoteEnd = ConnectionEnd.Client;
                _lastId = 0; // Streams opened by server are even
            }

            _goAwayReceived = false;
            _comprProc = new CompressionProcessor(_ourEnd);
            _ioStream = stream;

            _frameReader = new FrameReader(_ioStream);

            _writeQueue = new WriteQueue(_ioStream, _comprProc, _usePriorities);
            OurMaxConcurrentStreams = maxConcurrentStreams;
            RemoteMaxConcurrentStreams = maxConcurrentStreams;
            InitialWindowSize = initialWindowSize;

            _flowControlManager = new FlowControlManager(this);

            if (!_useFlowControl)
            {
                _flowControlManager.Options = (byte) FlowControlOptions.DontUseFlowControl;
            }

            SessionWindowSize = 0;
            _headersSequences = new HeadersSequenceList();
            _promisedResources = new Dictionary<int, string>();

            StreamDictionary = new StreamDictionary();
            for (byte i = 0; i < OurMaxConcurrentStreams; i++)
            {
                var http2Stream = new Http2Stream(new HeadersList(), i + 1, _writeQueue, _flowControlManager)
                {
                    Idle = true
                };
                StreamDictionary.Add(new KeyValuePair<int, Http2Stream>(i + 1, http2Stream));
            }

            _flowControlManager.SetStreamDictionary(StreamDictionary);
            _writeQueue.SetStreamDictionary(StreamDictionary);
        }

        private void SendSessionHeader()
        {
            var bytes = Encoding.UTF8.GetBytes(ClientSessionHeader);
            _ioStream.Write(bytes, 0 , bytes.Length);
        }

        private async Task<bool> GetSessionHeaderAndVerifyIt(Stream incomingClient)
        {
            var sessionHeaderBuffer = new byte[ClientSessionHeader.Length];

            int read = await incomingClient.ReadAsync(sessionHeaderBuffer, 0, 
                                            sessionHeaderBuffer.Length,
                                            _cancelSessionToken);
            if (read == 0)
            {
                throw new TimeoutException(String.Format("Session header was not received in timeout {0}", incomingClient.ReadTimeout));
            }

            var receivedHeader = Encoding.UTF8.GetString(sessionHeaderBuffer);

            return string.Equals(receivedHeader, ClientSessionHeader, StringComparison.OrdinalIgnoreCase);
        }

        //Calls only in unsecure connection case
        private void DispatchInitialRequest(IDictionary<string, string> initialRequest)
        {
            if (!initialRequest.ContainsKey(CommonHeaders.Path))
            {
                initialRequest.Add(CommonHeaders.Path, "/");
            }

            var initialStream = CreateStream(new HeadersList(initialRequest), 1);

            //09 -> 5.1.1.  Stream Identifiers
            //A stream identifier of one (0x1) is used to respond to the HTTP/1.1
            //request which was specified during Upgrade (see Section 3.2).  After
            //the upgrade completes, stream 0x1 is "half closed (local)" to the
            //client.  Therefore, stream 0x1 cannot be selected as a new stream
            //identifier by a client that upgrades from HTTP/1.1.
            if (_ourEnd == ConnectionEnd.Client)
            {
                GetNextId();
                initialStream.HalfClosedRemote = true;
            }
            else
            {
                initialStream.HalfClosedLocal = true;
                if (OnFrameReceived != null)
                {
                    OnFrameReceived(this, new FrameReceivedEventArgs(initialStream, new HeadersFrame(1)));
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
            // Listen for incoming Http/2.0 frames
            var incomingTask = new Task(() =>
                {
                    Thread.CurrentThread.Name = "Frame listening thread started";
                    PumpIncommingData();
                });

            // Send outgoing Http/2.0 frames
            var outgoingTask = new Task(() =>
                {
                    Thread.CurrentThread.Name = "Frame writing thread started";
                    PumpOutgoingData();
                });

            outgoingTask.Start();
            incomingTask.Start();

            //Write settings. Settings must be the first frame in session.
            if (_ourEnd == ConnectionEnd.Client)
            {
                if (_useFlowControl)
                {
                    WriteSettings(new[]
                        {
                            new SettingsPair(SettingsFlags.None, SettingsIds.InitialWindowSize,
                                             Constants.MaxFrameContentSize)
                        }, false);
                }
                else
                {
                    WriteSettings(new[]
                        {
                            new SettingsPair(SettingsFlags.None, SettingsIds.InitialWindowSize,
                                             Constants.MaxFrameContentSize),
                            new SettingsPair(SettingsFlags.None, SettingsIds.FlowControlOptions,
                                             (byte) FlowControlOptions.DontUseFlowControl)
                        }, false);
                }
            }

            //Handle upgrade handshake headers.
            if (initialRequest != null && !_isSecure)
                DispatchInitialRequest(initialRequest);
            
            var endPumpsTask = Task.WhenAll(incomingTask, outgoingTask);

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
                Frame frame;
                try
                {
                    frame = _frameReader.ReadFrame();

                    if (!_wasResponseReceived)
                    {
                        _wasResponseReceived = true;
                    }
                }
                catch (IOException)
                {
                    //Connection was closed by the remote endpoint
                    Http2Logger.LogInfo("Connection was closed by the remote endpoint");
                    Dispose();
                    break;
                }
                catch (Exception)
                {
                    // Read failure, abort the connection/session.
                    Http2Logger.LogInfo("Read failure, abort the connection/session");
                    Dispose();
                    break;
                }

                if (frame != null)
                {
                    DispatchIncomingFrame(frame);
                }
                else
                {
                    //Looks like connection was lost
                    Dispose();
                    break;
                }
            }

            Http2Logger.LogDebug("Read thread finished");
        }

        /// <summary>
        /// Pumps the outgoing data to write queue
        /// </summary>
        /// <returns></returns>
        private void PumpOutgoingData()
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

                Http2Logger.LogDebug("Write thread finished");
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
                    throw new ProtocolError(ResetStatusCode.FrameSizeError,
                                            String.Format("Frame too large: Type: {0} {1}", frame.FrameType,
                                                          frame.FrameLength));
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

                Http2Logger.LogDebug("Incoming frame: " + frame.FrameType);

                switch (frame.FrameType)
                {
                    case FrameType.Headers:
                        HandleHeaders(frame as HeadersFrame, out stream);
                        break;
                    case FrameType.Continuation:
                        HandleContinuation(frame as ContinuationFrame, out stream);
                        break;
                    case FrameType.Priority:
                        HandlePriority(frame as PriorityFrame, out stream);
                        break;
                    case FrameType.RstStream:
                        HandleRstFrame(frame as RstStreamFrame, out stream);
                        break;
                    case FrameType.Data:
                        HandleDataFrame(frame as DataFrame, out stream);
                        break;
                    case FrameType.Ping:
                        HandlePingFrame(frame as PingFrame);
                        break;
                    case FrameType.Settings:
                        HandleSettingsFrame(frame as SettingsFrame);

                        if (!(frame as SettingsFrame).IsAck)
                        {
                            //Send ack
                            WriteSettings(new SettingsPair[0], true);
                        }
                        break;
                    case FrameType.WindowUpdate:
                        HandleWindowUpdateFrame(frame as WindowUpdateFrame, out stream);
                        break;
                    case FrameType.GoAway:
                        HandleGoAwayFrame(frame as GoAwayFrame);
                        break;
                    case FrameType.PushPromise:
                        HandlePushPromiseFrame(frame as PushPromiseFrame, out stream);

                        if (stream != null) //This means that sequence is complete
                        {
                            _promisedResources.Add(stream.Id, stream.Headers.GetValue(CommonHeaders.Path));
                        }

                        break;
                    default:
                        /* 12 -> 4.1
                        Implementations MUST treat the receipt of an unknown frame type
                        (any frame types not defined in this document) as a connection
                        error of type PROTOCOL_ERROR. */
                        throw new ProtocolError(ResetStatusCode.ProtocolError, "Unknown frame type detected");
                }

                _lastFrame = frame;

                if (frame is IEndStreamFrame && ((IEndStreamFrame) frame).IsEndStream)
                {
                    //Tell the stream that it was the last frame
                    Http2Logger.LogDebug("Final frame received for StreamId = " + stream.Id);
                    stream.HalfClosedRemote = true;

                    //Promised resource has been pushed
                    if (_promisedResources.ContainsKey(stream.Id))
                        _promisedResources.Remove(stream.Id);
                }

                if (stream == null || OnFrameReceived == null) 
                    return;

                OnFrameReceived(this, new FrameReceivedEventArgs(stream, frame));
                stream.FramesReceived++;
            }

            //09
            //5.1.  Stream States
            //An endpoint MUST NOT send frames on a closed stream.  An endpoint
            //that receives a frame after receiving a RST_STREAM [RST_STREAM] or
            //a frame containing a END_STREAM flag on that stream MUST treat
            //that as a stream error (Section 5.4.2) of type STREAM_CLOSED
            //[STREAM_CLOSED].
            catch (Http2StreamNotFoundException ex)
            {
                Http2Logger.LogDebug("Frame for already Closed stream with StreamId = {0}", ex.Id);
                _writeQueue.WriteFrame(new RstStreamFrame(ex.Id, ResetStatusCode.StreamClosed));
                stream.WasRstSent = true;
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
            catch (MaxConcurrentStreamsLimitException)
            {
                //Remote side tries to open more streams than allowed
                Dispose();
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
        public Http2Stream CreateStream(HeadersList headers, int streamId, int priority = -1)
        {

            if (headers == null)
                throw new ArgumentNullException("pairs is null");

            if (priority == -1)
                priority = Constants.DefaultStreamPriority;

            if (priority < 0 || priority > Constants.MaxPriority)
                throw new ArgumentOutOfRangeException("priority is not between 0 and MaxPriority");

            if (StreamDictionary.GetOpenedStreamsBy(_remoteEnd) + 1 > OurMaxConcurrentStreams)
            {
                throw new MaxConcurrentStreamsLimitException();
            }

            //var stream = new Http2Stream(headers, streamId,
            //                             _writeQueue, _flowControlManager, priority);

            var streamSequence = new HeadersSequence(streamId, (new HeadersFrame(streamId, priority){Headers = headers}));
            _headersSequences.Add(streamSequence);

            var stream = StreamDictionary[streamId];

            stream.OnFrameSent += (o, args) =>
                {
                    if (!(args.Frame is IHeadersFrame))
                        return;

                    var streamSeq = _headersSequences.Find(stream.Id);
                    streamSeq.AddHeaders(args.Frame as IHeadersFrame);
                };

            stream.OnClose += (o, args) =>
                {
                    var streamSeq = _headersSequences.Find(stream.Id);

                    if (streamSeq != null)
                        _headersSequences.Remove(streamSeq);
                };
            stream.Priority = priority;
            stream.Headers = headers;
            stream.Opened = true;

            return stream;
        }

        internal Http2Stream CreateStream(HeadersSequence sequence)
        {

            if (sequence == null)
                throw new ArgumentNullException("sequence is null");

            if (sequence.Priority < 0 || sequence.Priority > Constants.MaxPriority)
                throw new ArgumentOutOfRangeException("priority is not between 0 and MaxPriority");

            if (StreamDictionary.GetOpenedStreamsBy(_remoteEnd) + 1 > OurMaxConcurrentStreams)
            {
                throw new MaxConcurrentStreamsLimitException();
            }

            int id = sequence.StreamId;
            int priority = sequence.Priority;
            var headers = sequence.Headers;

            var stream = StreamDictionary[id];

            if (sequence.WasEndStreamReceived)
                stream.HalfClosedLocal = sequence.WasEndStreamReceived;

            stream.OnFrameSent += (o, args) =>
            {
                if (!(args.Frame is IHeadersFrame))
                    return;

                var streamSeq = _headersSequences.Find(stream.Id);
                streamSeq.AddHeaders(args.Frame as IHeadersFrame);
            };

            stream.OnClose += (o, args) =>
            {
                var streamSeq = _headersSequences.Find(stream.Id);

                if (streamSeq != null)
                    _headersSequences.Remove(streamSeq);
            };

            stream.Headers = headers;
            stream.Priority = priority;
            stream.Opened = true;

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
        public Http2Stream CreateStream(int priority)
        {
            if (priority < 0 || priority > Constants.MaxPriority)
                throw new ArgumentOutOfRangeException("priority is not between 0 and MaxPriority");

            if (StreamDictionary.GetOpenedStreamsBy(_ourEnd) + 1 > RemoteMaxConcurrentStreams)
            {
                throw new MaxConcurrentStreamsLimitException();
            }
            int nextId = GetNextId();
            var stream = StreamDictionary[nextId];

            var streamSequence = new HeadersSequence(nextId, (new HeadersFrame(nextId, priority)));
            _headersSequences.Add(streamSequence);

            stream.OnFrameSent += (o, args) =>
            {
                if (!(args.Frame is IHeadersFrame))
                    return;

                var streamSeq = _headersSequences.Find(nextId);
                streamSeq.AddHeaders(args.Frame as IHeadersFrame);
            };

            stream.OnClose += (o, args) =>
                {
                    var streamSeq = _headersSequences.Find(stream.Id);

                    if (streamSeq != null)
                        _headersSequences.Remove(streamSeq);
                };

            stream.Priority = priority;
            stream.Opened = true;

            return stream;
        }

        /// <summary>
        /// Sends the headers with request headers.
        /// </summary>
        /// <param name="pairs">The header pairs.</param>
        /// <param name="priority">The stream priority.</param>
        /// <param name="isEndStream">True if initial headers+priority is also the final frame from endpoint.</param>
        public void SendRequest(HeadersList pairs, int priority, bool isEndStream)
        {
            if (_ourEnd == ConnectionEnd.Server)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Server should not initiate request");

            if (pairs == null)
                throw new ArgumentNullException("pairs is null");

            if (priority < 0 || priority > Constants.MaxPriority)
                throw new ArgumentOutOfRangeException("priority is not between 0 and MaxPriority");

            var path = pairs.GetValue(CommonHeaders.Path);

            if (path == null)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Invalid request ex");

            //09 -> 8.2.2.  Push Responses
            //Once a client receives a PUSH_PROMISE frame and chooses to accept the
            //pushed resource, the client SHOULD NOT issue any requests for the
            //promised resource until after the promised stream has closed.
            if (_promisedResources.ContainsValue(path))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Resource has been promised. Client should not request it.");

            var stream = CreateStream(priority);

            stream.WriteHeadersFrame(pairs, isEndStream, true);

            var streamSequence = _headersSequences.Find(stream.Id);
            streamSequence.AddHeaders(new HeadersFrame(stream.Id, stream.Priority) { Headers = pairs });

            if (OnRequestSent != null)
            {
                OnRequestSent(this, new RequestSentEventArgs(stream));
            }
        }

        /// <summary>
        /// Gets the stream from stream dictionary.
        /// </summary>
        /// <param name="id">The stream id.</param>
        /// <returns></returns>
        internal Http2Stream GetStream(int id)
        {
            Http2Stream stream;
            if (!StreamDictionary.TryGetValue(id, out stream))
            {
                return null;
            }
            return stream;
        }

        /// <summary>
        /// Writes the settings frame.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void WriteSettings(SettingsPair[] settings, bool isAck)
        {
            if (settings == null)
                throw new ArgumentNullException("settings array is null");

            var frame = new SettingsFrame(new List<SettingsPair>(settings), isAck);

            _writeQueue.WriteFrame(frame);


            if (!isAck && !_settingsAckReceived.WaitOne(60000))
            {
                WriteGoAway(ResetStatusCode.SettingsTimeout);
                Dispose();
            }
            
            _settingsAckReceived.Reset();

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
            Http2Logger.LogDebug("Writing GoAway with code = {0}", code);
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

            if (!_pingReceived.WaitOne(3000))
            {
                //Remote endpoint was not answer at time.
                Dispose();
            }
            _pingReceived.Reset();

            var newNow = DateTime.UtcNow;
            Http2Logger.LogDebug("Ping: " + (newNow - now).Milliseconds);
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
            foreach (var stream in StreamDictionary.Values)
            {
                //Cancel all opened streams
                stream.Close(ResetStatusCode.None);
            }

            if (!_goAwayReceived)
            {
                WriteGoAway(status);

                //TODO fix delay. wait for goAway send and then dispose WriteQueue
                //Wait for GoAway send
                using (var goAwayDelay = new ManualResetEvent(false))
                {
                    goAwayDelay.WaitOne(500);
                }
            }
            OnSettingsSent = null;
            OnFrameReceived = null;

            if (_frameReader != null)
            {
                _frameReader.Dispose();
                _frameReader = null;
            }

            if (_writeQueue != null)
            {
                _writeQueue.Flush();
                _writeQueue.Dispose();
            }

            if (_comprProc != null)
            {
                _comprProc.Dispose();
                _comprProc = null;
            }

            if (_ioStream != null)
            {
                _ioStream.Close();
                _ioStream = null;
            }

            if (_pingReceived != null)
            {
                _pingReceived.Dispose();
                _pingReceived = null;
            }

            if (_settingsAckReceived != null)
            {
                _settingsAckReceived.Dispose();
                _settingsAckReceived = null;
            }

            if (OnSessionDisposed != null)
            {
                OnSessionDisposed(this, null);
            }

            OnSessionDisposed = null;

            Http2Logger.LogDebug("Session closed");
        }
    }
}
