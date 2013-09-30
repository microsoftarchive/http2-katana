using System;
using System.Linq;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol
{
    public partial class Http2Session
    {
        private void HandleHeaders(HeadersFrame headersFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("New headers with id = " + headersFrame.StreamId);

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
                Http2Logger.LogDebug("Stream {0} header: {1}={2}", headersFrame.StreamId, header.Key, header.Value);
            }
            headersFrame.Headers.AddRange(headers);

            var sequence = _headersSequences.Find(seq => seq.StreamId == headersFrame.StreamId);
            if (sequence == null)
            {
                sequence = new HeadersSequence(headersFrame.StreamId, headersFrame);
                _headersSequences.Add(sequence);
            }
            else
            {
                sequence.AddHeaders(headersFrame);
            }

            if (headersFrame.HasPriority)
            {
                sequence.Priority = headersFrame.Priority;
            }

            if (!sequence.IsComplete)
            {
                stream = null;
                return;
            }

            stream = GetStream(headersFrame.StreamId);
            if (stream == null)
            {
                stream = CreateStream(sequence.Headers, headersFrame.StreamId, sequence.Priority);
            }
            else
            {
                stream.Headers.AddRange(sequence.Headers);
            }
        }

        private void HandleContinuation(ContinuationFrame contFrame, out Http2Stream stream)
        {
            if (!(_lastFrame is ContinuationFrame || _lastFrame is HeadersFrame))
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Last frame was not headers or continuation");

            Http2Logger.LogDebug("New continuation with id = " + contFrame.StreamId);

            if (contFrame.StreamId == 0)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Incoming continuation frame with id = 0");
            }

            var serHeaders = new byte[contFrame.CompressedHeaders.Count];

            Buffer.BlockCopy(contFrame.CompressedHeaders.Array,
                                contFrame.CompressedHeaders.Offset,
                                serHeaders, 0, serHeaders.Length);

            var decomprHeaders = _comprProc.Decompress(serHeaders);
            var contHeaders = new HeadersList(decomprHeaders);
            foreach (var header in contHeaders)
            {
                Http2Logger.LogDebug("Stream {0} header: {1}={2}", contFrame.StreamId, header.Key, header.Value);
            }
            contFrame.Headers.AddRange(contHeaders);
            var sequence = _headersSequences.Find(seq => seq.StreamId == contFrame.StreamId);
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
                stream = null;
                return;
            }

            stream = GetStream(contFrame.StreamId);
            if (stream == null)
            {
                stream = CreateStream(sequence.Headers, contFrame.StreamId, sequence.Priority);
            }
            else
            {
                stream.Headers.AddRange(sequence.Headers);
            }
        }

        private void HandlePriority(PriorityFrame priorityFrame, out Http2Stream stream)
        {
            //spec 06:
            //The PRIORITY frame is associated with an existing stream.  If a
            //PRIORITY frame is received with a stream identifier of 0x0, the
            //recipient MUST respond with a connection error (Section 5.4.1) of
            //type PROTOCOL_ERROR [PROTOCOL_ERROR].
            if (priorityFrame.StreamId == 0)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming priority frame with id = 0");
            }

            Http2Logger.LogDebug("Priority frame. StreamId: {0} Priority: {1}", priorityFrame.StreamId,
                                 priorityFrame.Priority);

            stream = GetStream(priorityFrame.StreamId);

            if (!_usePriorities) 
                return;

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

        private void HandleRstFrame(RstStreamFrame resetFrame, out Http2Stream stream)
        {
            stream = GetStream(resetFrame.StreamId);

            //Spec 06 tells that impl MUST not answer with rst on rst to avoid loop.
            if (stream != null)
            {
                Http2Logger.LogDebug("RST frame with code {0} for id {1}", resetFrame.StatusCode,
                                        resetFrame.StreamId);
                stream.Dispose(ResetStatusCode.None);
            }
            //Do not signal an error because (06)
            //WINDOW_UPDATE [WINDOW_UPDATE], PRIORITY [PRIORITY], or RST_STREAM
            //[RST_STREAM] frames can be received in this state for a short
            //period after a frame containing an END_STREAM flag is sent.
        }

        private void HandleDataFrame(DataFrame dataFrame, out Http2Stream stream)
        {
            stream = GetStream(dataFrame.StreamId);

            //Aggressive window update
            if (stream != null)
            {
                Http2Logger.LogDebug("Data frame. StreamId: {0} Length: {1}", dataFrame.StreamId,
                                     dataFrame.FrameLength);
                if (stream.IsFlowControlEnabled)
                {
                    stream.WriteWindowUpdate(Constants.MaxFrameContentSize);
                }
            }
            else
            {
                throw new Http2StreamNotFoundException(dataFrame.StreamId);
            }
        }

        private void HandlePingFrame(PingFrame pingFrame)
        {
            Http2Logger.LogDebug("Ping frame with StreamId:{0} Payload:{1}", pingFrame.StreamId,
                                             pingFrame.Payload.Count);

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
        }

        private void HandleSettingsFrame(SettingsFrame settingsFrame)
        {
            _wasSettingsReceived = true;
            Http2Logger.LogDebug("Settings frame. Entry count: {0} StreamId: {1}", settingsFrame.EntryCount,
                                 settingsFrame.StreamId);
            _settingsManager.ProcessSettings(settingsFrame, this, _flowControlManager);
        }

        private void HandleWindowUpdateFrame(WindowUpdateFrame windowUpdateFrame, out Http2Stream stream)
        {
            if (_useFlowControl)
            {
                Http2Logger.LogDebug("WindowUpdate frame. Delta: {0} StreamId: {1}", windowUpdateFrame.Delta,
                                     windowUpdateFrame.StreamId);
                stream = GetStream(windowUpdateFrame.StreamId);

                //06
                //The legal range for the increment to the flow control window is 1 to
                //2^31 - 1 (0x7fffffff) bytes.
                if (!(0 < windowUpdateFrame.Delta && windowUpdateFrame.Delta <= 0x7fffffff))
                {
                    Http2Logger.LogDebug("Incorrect window update delta : {0}", windowUpdateFrame.Delta);
                    throw new ProtocolError(ResetStatusCode.FlowControlError, String.Format("Incorrect window update delta : {0}", windowUpdateFrame.Delta));
                }

                //06
                //A receiver can ignore WINDOW_UPDATE [WINDOW_UPDATE] or PRIORITY
                //[PRIORITY] frames in this state.
                if (stream != null)
                {
                    stream.UpdateWindowSize(windowUpdateFrame.Delta);
                    stream.PumpUnshippedFrames();
                }
                //Do not signal an error because (06)
                //WINDOW_UPDATE [WINDOW_UPDATE], PRIORITY [PRIORITY], or RST_STREAM
                //[RST_STREAM] frames can be received in this state for a short
                //period after a frame containing an END_STREAM flag is sent.
            }
            else
            {
                stream = null;
            }
        }

        private void HandleGoAwayFrame(GoAwayFrame goAwayFrame)
        {
            //TODO handle additional debug info
            _goAwayReceived = true;
            Http2Logger.LogDebug("GoAway frame received");
            Dispose();
        }
    }
}
