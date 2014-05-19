// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol
{
    public partial class Http2Session
    {
        private const string lowerCasePattern = "^((?![A-Z]).)*$";
        private readonly Regex _matcher = new Regex(lowerCasePattern);

        private void ValidateHeaders(Http2Stream stream)
        {
            /* 12 -> 8.1.3
            Header field names MUST be converted to lowercase prior to their 
            encoding in HTTP/2.0. A request or response containing uppercase 
            header field names MUST be treated as malformed. */
            foreach (var header in stream.Headers)
            {
                string key = header.Key;
                if (!_matcher.IsMatch(key) || key == ":")
                {                    
                    stream.WriteRst(ResetStatusCode.RefusedStream);
                    stream.Close(ResetStatusCode.RefusedStream);
                    break;
                }
            }
        }

        private void HandleHeaders(HeadersFrame headersFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("HEADERS frame: stream id={0}, payload len={1}, has pad={2}, pad high={3}, pad low={4}, " +
                                 "end stream={5}, has priority={6}, exclusive={7}, dependency={8}, weight={9}", 
                                 headersFrame.StreamId, headersFrame.PayloadLength,
                                 headersFrame.HasPadding, headersFrame.PadHigh, headersFrame.PadLow, headersFrame.IsEndStream,
                                 headersFrame.HasPriority, headersFrame.Exclusive, headersFrame.StreamDependency, headersFrame.Weight);

            /* 12 -> 6.2 
            HEADERS frames MUST be associated with a stream.  If a HEADERS frame
            is received whose stream identifier field is 0x0, the recipient MUST
            respond with a connection error of type PROTOCOL_ERROR. */
            if (headersFrame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming headers frame with stream id=0");

            var serializedHeaders = new byte[headersFrame.CompressedHeaders.Count];

            Buffer.BlockCopy(headersFrame.CompressedHeaders.Array,
                             headersFrame.CompressedHeaders.Offset,
                             serializedHeaders, 0, serializedHeaders.Length);

            var decompressedHeaders = _comprProc.Decompress(serializedHeaders);
            var headers = new HeadersList(decompressedHeaders);
            foreach (var header in headers)
            {
                Http2Logger.LogDebug("{1}={2}", headersFrame.StreamId, header.Key, header.Value);
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
                //TODO: Priority was deprecated, now we need to use Dependency and Weight
                //sequence.Priority = headersFrame.Priority;
            }

            if (!sequence.IsComplete)
            {
                stream = null;
                return;
            }

            stream = GetStream(headersFrame.StreamId);
            if (stream.Idle)
            {
                stream = CreateStream(sequence);

                ValidateHeaders(stream);
            }
            else if (stream.ReservedRemote)
            {
                stream = CreateStream(sequence);
                stream.HalfClosedLocal = true;
                ValidateHeaders(stream);
            }
            else if(stream.Opened || stream.HalfClosedLocal)
            {
                stream.Headers = sequence.Headers;//Modify by the last accepted frame

                ValidateHeaders(stream);
            }
            else if (stream.HalfClosedRemote)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError, "headers for half closed remote stream");
            }
            else
            {
                throw new Http2StreamNotFoundException(headersFrame.StreamId);
            }
        }

        private void HandleContinuation(ContinuationFrame contFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("CONTINUATION frame: stream id={0}, payload len={1}, has pad={2}, pad high={3}," +
                                 " pad low={4}, end headers={5}", contFrame.StreamId, contFrame.PayloadLength,
                                 contFrame.HasPadding, contFrame.PadHigh, contFrame.PadLow, contFrame.IsEndHeaders);

            if (!(_lastFrame is ContinuationFrame || _lastFrame is HeadersFrame))
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Last frame was not headers or continuation");

            /* 12 -> 6.10
            CONTINUATION frames MUST be associated with a stream. If a CONTINUATION 
            frame is received whose stream identifier field is 0x0, the recipient MUST
            respond with a connection error of type PROTOCOL_ERROR. */
            if (contFrame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Incoming continuation frame with stream id=0");

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
            if (stream.Idle || stream.ReservedRemote)
            {
                stream = CreateStream(sequence);

                ValidateHeaders(stream);
            }
            else if (stream.Opened || stream.HalfClosedLocal)
            {
                stream.Headers = sequence.Headers;//Modify by the last accepted frame

                ValidateHeaders(stream);
            }
            else if (stream.HalfClosedRemote)
            {
                throw new ProtocolError(ResetStatusCode.ProtocolError, "continuation for half closed remote stream");
            }
            else
            {
                throw new Http2StreamNotFoundException(contFrame.StreamId);
            }
        }

        private void HandlePriority(PriorityFrame priorityFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("PRIORITY frame: stream id={0}, exclusive={1}, dependency={2}, weight={3}",
                priorityFrame.StreamId, priorityFrame.Exclusive, priorityFrame.StreamDependency,
                priorityFrame.Weight);

            /* 12 -> 6.3
            The PRIORITY frame is associated with an existing stream. If a
            PRIORITY frame is received with a stream identifier of 0x0, the
            recipient MUST respond with a connection error of type PROTOCOL_ERROR. */
            if (priorityFrame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Incoming priority frame with stream id=0");
           
            stream = GetStream(priorityFrame.StreamId);

            /* 12 -> 6.3
            The PRIORITY frame can be sent on a stream in any of the "reserved
            (remote)", "open", "half-closed (local)", or "half closed (remote)"
            states, though it cannot be sent between consecutive frames that
            comprise a single header block. */

            if (stream.Closed)
                throw new Http2StreamNotFoundException(priorityFrame.StreamId);

            if (!(stream.Opened || stream.ReservedRemote || stream.HalfClosedLocal))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "priority for non opened or reserved stream");

            if (!_usePriorities) 
                return;

            stream.Priority = priorityFrame.Weight;
        }

        private void HandleRstFrame(RstStreamFrame resetFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("RST_STREAM frame: stream id={0}, status code={1}",
               resetFrame.StreamId, resetFrame.StatusCode);

            /* 12 -> 6.4
            RST_STREAM frames MUST be associated with a stream.  If a RST_STREAM
            frame is received with a stream identifier of 0x0, the recipient MUST
            treat this as a connection error of type PROTOCOL_ERROR. */
            if (resetFrame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Rst frame with stream id=0");

            stream = GetStream(resetFrame.StreamId);
            
            if (stream.Closed)
            {
                /* 12 -> 5.4.2
                An endpoint MUST NOT send a RST_STREAM in response to an RST_STREAM
                frame, to avoid looping. */
                if (!stream.WasRstSent)
                {
                    throw new Http2StreamNotFoundException(resetFrame.StreamId);
                }
                return;
            }

            if (!(stream.ReservedRemote || stream.Opened || stream.HalfClosedLocal))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Rst for non opened or reserved stream");

            stream.Close(ResetStatusCode.None);
        }

        private void HandleDataFrame(DataFrame dataFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("DATA frame: stream id={0}, payload len={1}, has pad={2}, pad high={3}, pad low={4}, " +
                                 "end stream={5}", dataFrame.StreamId, dataFrame.PayloadLength, 
                                 dataFrame.HasPadding, dataFrame.PadHigh, dataFrame.PadLow, dataFrame.IsEndStream);
            /* 12 -> 6.1
            DATA frames MUST be associated with a stream. If a DATA frame is
            received whose stream identifier field is 0x0, the recipient MUST 
            respond with a connection error of type PROTOCOL_ERROR. */
            if (dataFrame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Incoming continuation frame with stream id=0");

            stream = GetStream(dataFrame.StreamId);

            if (stream.Closed)
                throw new Http2StreamNotFoundException(dataFrame.StreamId);

            if (!(stream.Opened || stream.HalfClosedLocal))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "data in non opened or half closed local stream");

            
            if (stream.IsFlowControlEnabled && !dataFrame.IsEndStream)
            {
                stream.WriteWindowUpdate(Constants.MaxFramePayloadSize);
            }
        }

        private void HandlePingFrame(PingFrame pingFrame)
        {
            Http2Logger.LogDebug("PING frame: stream id={0}, payload={1}", pingFrame.StreamId,
                pingFrame.Payload.Count);

            /* 12 -> 6.7
            PING frames are not associated with any individual stream.  If a PING
            frame is received with a stream identifier field value other than
            0x0, the recipient MUST respond with a connection error of type PROTOCOL_ERROR. */
            if (pingFrame.StreamId != 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Incoming ping frame with stream id != 0");            

            if (pingFrame.PayloadLength != PingFrame.DefPayloadLength)
            {
                throw new ProtocolError(ResetStatusCode.FrameSizeError, "Ping payload size is not equal to 8");
            }

            if (pingFrame.IsAck)
            {
                _pingReceived.Set();
            }
            else
            {
                var pingAckFrame = new PingFrame(true, pingFrame.Payload.ToArray());
                _writeQueue.WriteFrame(pingAckFrame);
            }
        }

        private void HandleSettingsFrame(SettingsFrame settingsFrame)
        {
            Http2Logger.LogDebug("SETTINGS frame: stream id={0}, entry count={1}", settingsFrame.StreamId,
                settingsFrame.EntryCount);

            _wasSettingsReceived = true;
            
            /* 12 -> 6.5
            If an endpoint receives a SETTINGS frame whose stream identifier 
            field is other than 0x0, the endpoint MUST respond with a connection
            error of type PROTOCOL_ERROR. */
            if (settingsFrame.StreamId != 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Settings frame stream id is not 0");

            /* 12 -> 6.5
            Receipt of a SETTINGS frame with the ACK flag set and a length
            field value other than 0 MUST be treated as a connection error
            of type FRAME_SIZE_ERROR. */
            if (settingsFrame.IsAck)
            {
                _settingsAckReceived.Set();

                if (settingsFrame.PayloadLength != 0)
                    throw new ProtocolError(ResetStatusCode.FrameSizeError, "ACK settings frame is not 0");      
          
                return;
            }

            for (int i = 0; i < settingsFrame.EntryCount; i++)
            {
                switch (settingsFrame[i].Id)
                {
                    case SettingsIds.HeadersTableSize:
                        if (_comprProc is CompressionProcessor)
                            (_comprProc as CompressionProcessor).NotifySettingsChanges(settingsFrame[i].Value);
                        break;
                    case SettingsIds.EnablePush:
                        IsPushEnabled = settingsFrame[i].Value != 0;
                        break;
                    case SettingsIds.MaxConcurrentStreams:
                        RemoteMaxConcurrentStreams = settingsFrame[i].Value;
                        /* A client can use the SETTINGS_MAX_CONCURRENT_STREAMS setting to limit
                        the number of resources that can be concurrently pushed by a server.
                        Advertising a SETTINGS_MAX_CONCURRENT_STREAMS value of zero disables
                        server push by preventing the server from creating the necessary
                        streams.  This does not prohibit a server from sending PUSH_PROMISE
                        frames; clients need to reset any promised streams that are not
                        wanted. */
                        IsPushEnabled = settingsFrame[i].Value == 0;
                        break;
                    case SettingsIds.InitialWindowSize:
                        int newInitWindowSize = settingsFrame[i].Value;
                        int windowSizeDiff = newInitWindowSize - _flowControlManager.StreamsInitialWindowSize;

                        foreach (var stream in StreamDictionary.FlowControlledStreams.Values)
                        {
                            stream.WindowSize += windowSizeDiff;
                        }

                        _flowControlManager.StreamsInitialWindowSize = newInitWindowSize;
                        InitialWindowSize = newInitWindowSize;
                        break;
                    case SettingsIds.FlowControlOptions:
                        _flowControlManager.Options = settingsFrame[i].Value;
                        break;
                    default:
                        /* 12 -> 6.5.2 
                        An endpoint that receives a SETTINGS frame with any other identifier
                        MUST treat this as a connection error of type PROTOCOL_ERROR. */
                        throw new ProtocolError(ResetStatusCode.ProtocolError, "Unknown setting identifier detected");
                }
            }
        }

        private void HandleWindowUpdateFrame(WindowUpdateFrame windowUpdateFrame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("WINDOW_UPDATE frame: stream id={0}, delta={1}",windowUpdateFrame.StreamId,
                 windowUpdateFrame.Delta);

            if (!_useFlowControl)
            {
                stream = null;
                return;
            }

            // TODO Remove this hack
            /* The WINDOW_UPDATE frame can be specific to a stream or to the entire
            connection.  In the former case, the frame's stream identifier
            indicates the affected stream; in the latter, the value "0" indicates
            that the _entire connection_ is the subject of the frame. */
            if (windowUpdateFrame.StreamId == 0)
            {
                _flowControlManager.StreamsInitialWindowSize += windowUpdateFrame.Delta;
                stream = null;
                return; 
            }

            stream = GetStream(windowUpdateFrame.StreamId);

            /* 12 -> 6.9 
            WINDOW_UPDATE can be sent by a peer that has sent a frame bearing the
            END_STREAM flag.  This means that a receiver could receive a
            WINDOW_UPDATE frame on a "half closed (remote)" or "closed" stream.
            A receiver MUST NOT treat this as an error. */

            if (!(stream.Opened || stream.HalfClosedRemote || stream.HalfClosedLocal || stream.Closed))
                throw new ProtocolError(ResetStatusCode.ProtocolError, "window update in incorrect state");

            //09 -> 6.9.  WINDOW_UPDATE
            //The payload of a WINDOW_UPDATE frame is one reserved bit, plus an
            //unsigned 31-bit integer indicating the number of bytes that the
            //sender can transmit in addition to the existing flow control window.
            //The legal range for the increment to the flow control window is 1 to
            //2^31 - 1 (0x7fffffff) bytes.
            if (!(0 < windowUpdateFrame.Delta && windowUpdateFrame.Delta <= Constants.MaxPriority))
            {
                Http2Logger.LogDebug("Incorrect window update delta : {0}", windowUpdateFrame.Delta);
                throw new ProtocolError(ResetStatusCode.FlowControlError, String.Format("Incorrect window update delta : {0}", windowUpdateFrame.Delta));
            }           

            stream.UpdateWindowSize(windowUpdateFrame.Delta);
            stream.PumpUnshippedFrames();
        }

        private void HandleGoAwayFrame(GoAwayFrame goAwayFrame)
        {
            Http2Logger.LogDebug("GOAWAY frame: stream id={0}, status code={1}", goAwayFrame.StreamId,
                goAwayFrame.StatusCode);

            if (goAwayFrame.StreamId != 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "GoAway Stream id should always be null");

            _goAwayReceived = true;
           
            Http2Logger.LogDebug("last successful id = {0}", goAwayFrame.LastGoodStreamId);
            Dispose();
        }

        private void HandlePushPromiseFrame(PushPromiseFrame frame, out Http2Stream stream)
        {
            Http2Logger.LogDebug("PUSH_PROMISE frame: stream id={0}, payload len={1}, promised id={2}, " +
                                 "has pad={3}, pad high={4}, pad low={5}, end headers={6}",
                                 frame.StreamId, frame.PayloadLength, frame.PromisedStreamId, frame.HasPadding,
                                 frame.PadHigh, frame.PadLow, frame.IsEndHeaders);

            /* 12 -> 6.6. 
            PUSH_PROMISE frames MUST be associated with an existing, peer- initiated stream.
            If the stream identifier field specifies the value 0x0, a recipient MUST respond
            with a connection error of type PROTOCOL_ERROR. */
            if (frame.StreamId == 0)
                throw new ProtocolError(ResetStatusCode.ProtocolError, "push promise frame with stream id=0");

            /* 12 -> 5.1
            An endpoint that receives any frame after receiving a RST_STREAM MUST treat
            that as a stream error of type STREAM_CLOSED. */
            if (StreamDictionary[frame.StreamId].Closed)
                throw new Http2StreamNotFoundException(frame.StreamId);


            //... a receiver MUST
            //treat the receipt of a PUSH_PROMISE that promises an illegal stream
            //identifier (Section 5.1.1) (that is, an identifier for a stream that
            //is not currently in the "idle" state) as a connection error
            //(Section 5.4.1) of type PROTOCOL_ERROR, unless the receiver recently
            //sent a RST_STREAM frame to cancel the associated stream (see
            //Section 5.1).  
            if (frame.StreamId % 2 == 0
                || frame.PromisedStreamId == 0
                || (frame.PromisedStreamId % 2) != 0
                || frame.PromisedStreamId < _lastPromisedId
                || !((StreamDictionary[frame.StreamId].Opened || StreamDictionary[frame.StreamId].HalfClosedLocal)))
            { 
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Incorrect Promised Stream id");
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
                //09 -> 6.6.  PUSH_PROMISE
                //A receiver MUST
                //treat the receipt of a PUSH_PROMISE on a stream that is neither
                //"open" nor "half-closed (local)" as a connection error
                //(Section 5.4.1) of type PROTOCOL_ERROR.

                //This means that we already got push_promise with the same PromisedId.
                //Hence Stream is in the reserved state.
                throw new ProtocolError(ResetStatusCode.ProtocolError,
                                        "Got multiple push promises with same Promised Stream id's");
            }

            //09 -> 6.6.  PUSH_PROMISE
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

            //09 -> 8.2.1.  Push Requests
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
                frameReceiveStream.Close(ResetStatusCode.None);

                stream = null;
                return;
            }

            stream = GetStream(frame.PromisedStreamId);

            if (stream.Idle)
            {
                stream = CreateStream(sequence);
                stream.ReservedRemote = true;

                ValidateHeaders(stream);
                _lastPromisedId = stream.Id;
            }
            else {
                /* 12 -> 6.6
                Similarly, a receiver MUST treat the receipt of a PUSH_PROMISE that
                promises an illegal stream identifier (that is, an identifier for a stream that
                is not currently in the "idle" state) as a connection error of type PROTOCOL_ERROR. */
                throw new ProtocolError(ResetStatusCode.ProtocolError, "Remote endpoint tried to Promise incorrect Stream id");
            }
        }
    }
}
