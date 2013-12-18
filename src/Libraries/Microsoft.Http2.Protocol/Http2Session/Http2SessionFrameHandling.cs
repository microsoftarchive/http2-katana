// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
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

            var sequence = _headersSequences.Find(headersFrame.StreamId);
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
                stream = CreateStream(sequence);
            }
            else
            {
                stream.Headers = sequence.Headers;//Modify by the last accepted frame
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
            var sequence = _headersSequences.Find(contFrame.StreamId);
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
                stream = CreateStream(sequence);
            }
            else
            {
                stream.Headers = sequence.Headers;
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

            //Receipt of a SETTINGS frame with the ACK flag set and a length
            //field value other than 0 MUST be treated as a connection error
            //(Section 5.4.1) of type FRAME_SIZE_ERROR.
            if (settingsFrame.IsAck)
            {
                if (settingsFrame.FrameLength != 0)
                    throw new ProtocolError(ResetStatusCode.FrameTooLarge, "ack settings frame is not 0");
                
                return;
            }

            for (int i = 0; i < settingsFrame.EntryCount; i++)
            {

                switch (settingsFrame[i].Id)
                {
                    case SettingsIds.SettingsHeadersTableSize:
                        if (_comprProc is CompressionProcessor)
                            (_comprProc as CompressionProcessor).NotifySettingsChanges(settingsFrame[i].Value);
                        break;
                    case SettingsIds.SettingsEnableServerPush:
                        IsPushEnabled = settingsFrame[i].Value != 0;
                        break;
                    case SettingsIds.MaxConcurrentStreams:
                        RemoteMaxConcurrentStreams = settingsFrame[i].Value;
                        break;
                    case SettingsIds.InitialWindowSize:
                        int newInitWindowSize = settingsFrame[i].Value;
                        int windowSizeDiff = newInitWindowSize - _flowControlManager.StreamsInitialWindowSize;

                        foreach (var stream in ActiveStreams.FlowControlledStreams.Values)
                        {
                            stream.WindowSize += windowSizeDiff;
                        }

                        _flowControlManager.StreamsInitialWindowSize = newInitWindowSize;
                        InitialWindowSize = newInitWindowSize;
                        break;
                    case SettingsIds.FlowControlOptions:
                        _flowControlManager.Options = settingsFrame[i].Value;
                        break;
                }
            }
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
                if (!(0 < windowUpdateFrame.Delta && windowUpdateFrame.Delta <= Constants.MaxPriority))
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

        private void HandlePushPromiseFrame(PushPromiseFrame frame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("New push_promise with id = " + frame.StreamId);
            Http2Logger.LogDebug("Promised id = " + frame.PromisedStreamId);

            //spec 06:
            //PUSH_PROMISE frames MUST be associated with an existing, peer-
            //initiated stream.  If the stream identifier field specifies the value
            //0x0, a recipient MUST respond with a connection error (Section 5.4.1)
            //of type PROTOCOL_ERROR.
            if (frame.StreamId == 0)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming headers frame with id = 0");
            }

            var serializedHeaders = new byte[frame.CompressedHeaders.Count];

            Buffer.BlockCopy(frame.CompressedHeaders.Array,
                             frame.CompressedHeaders.Offset,
                             serializedHeaders, 0, serializedHeaders.Length);

            var decompressedHeaders = _comprProc.Decompress(serializedHeaders);
            var headers = new HeadersList(decompressedHeaders);
            foreach (var header in headers)
            {
                Http2Logger.LogDebug("Stream {0} header: {1}={2}", frame.StreamId, header.Key, header.Value);
                frame.Headers.Add(header);
            }

            var sequence = _headersSequences.Find(frame.PromisedStreamId);
            if (sequence == null)
            {
                sequence = new HeadersSequence(frame.PromisedStreamId, frame);
                _headersSequences.Add(sequence);
            }
            else
            {
                //06
                //A receiver MUST
                //treat the receipt of a PUSH_PROMISE on a stream that is neither
                //"open" nor "half-closed (local)" as a connection error
                //(Section 5.4.1) of type PROTOCOL_ERROR.

                //This means that we already got push_promise with the same PromisedId.
                //Hence Stream is in the reserved state.
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Got multiple push_promises with same Promised id's");
            }

            //06
            //A PUSH_PROMISE frame without the END_PUSH_PROMISE flag set MUST be
            //followed by a CONTINUATION frame for the same stream.  A receiver
            //MUST treat the receipt of any other type of frame or a frame on a
            //different stream as a connection error (Section 5.4.1) of type
            //PROTOCOL_ERROR.
            if (!sequence.IsComplete)
            {
                stream = null;
                return;
            }
            
            //06
            //The server MUST include a method in the ":method"
            //header field that is safe (see [HTTP-p2], Section 4.2.1).  If a
            //client receives a PUSH_PROMISE that does not include a complete and
            // valid set of header fields, or the ":method" header field identifies
            //a method that is not safe, it MUST respond with a stream error
            //(Section 5.4.2) of type PROTOCOL_ERROR.

            //Lets think that only GET method is safe for now
            var method = sequence.Headers.GetValue(CommonHeaders.Method);
            if (method == null || !method.Equals(Verbs.Get, StringComparison.OrdinalIgnoreCase))
            {
                var frameReceiveStream = GetStream(frame.StreamId);
                frameReceiveStream.WriteRst(ResetStatusCode.ProtocolError);
                frameReceiveStream.Dispose(ResetStatusCode.None);

                stream = null;
                return;
            }

            stream = GetStream(frame.PromisedStreamId);
            if (stream == null)
            {
                stream = CreateStream(sequence);
                stream.EndStreamSent = true;
            }
            else
            {
                //Similarly, a receiver MUST
                //treat the receipt of a PUSH_PROMISE that promises an illegal stream
                //identifier (Section 5.1.1) (that is, an identifier for a stream that
                //is not currently in the "idle" state) as a connection error
                //(Section 5.4.1) of type PROTOCOL_ERROR

                throw new ProtocolError(ResetStatusCode.ProtocolError, "Remote ep tried to promise incorrect stream id");
            }
        }
    }
}
