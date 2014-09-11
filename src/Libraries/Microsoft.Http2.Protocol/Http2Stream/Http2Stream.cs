// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.FlowControl;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// Class represents http2 stream.
    /// </summary>
    public class Http2Stream
    {
        #region Fields

        private readonly int _id;
        private StreamState _state;
        private readonly OutgoingQueue _outgoingQueue;
        private readonly FlowControlManager _flowCtrlManager;

        private readonly Queue<DataFrame> _unshippedFrames;
        private readonly object _unshippedDeliveryLock = new object();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when stream was sent frame.
        /// </summary>
        public event EventHandler<FrameSentEventArgs> OnFrameSent;

        /// <summary>
        /// Occurs when stream closes.
        /// </summary>
        public event EventHandler<StreamClosedEventArgs> OnClose;
        #endregion

        #region Constructors

        //Incoming
        internal Http2Stream(HeadersList headers, int id,
                           OutgoingQueue outgoingQueue, FlowControlManager flowCrtlManager, int priority = Constants.DefaultStreamPriority)
            : this(id, outgoingQueue, flowCrtlManager, priority)
        {
            Headers = headers;
        }

        //Outgoing
        internal Http2Stream(int id, OutgoingQueue outgoingQueue, FlowControlManager flowCtrlManager, int priority = Constants.DefaultStreamPriority)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException("invalid id for stream");

            if (priority < 0 || priority > Constants.MaxPriority)
                throw  new ArgumentOutOfRangeException("priority out of range");

            _id = id;
            Priority = priority;
            _outgoingQueue = outgoingQueue;
            _flowCtrlManager = flowCtrlManager;

            _unshippedFrames = new Queue<DataFrame>(16);
            Headers = new HeadersList();

            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsBlocked = false;
            UpdateWindowSize(_flowCtrlManager.InitialWindowSize, false);
            WasHeadersFrameRecived = false;
            WasRstOnStream = false;

            OnFrameSent += (sender, args) => FramesSent++;
        }

        #endregion

        #region Properties
        public int Id
        {
            get { return _id; }
        }

        public int FramesSent { get; set; }
        public int FramesReceived { get; set; }
        public int Priority { get; set; }
        public bool WasRstOnStream { get; set; }
        public Int32 MaxFrameSize { get; set; }
        public bool WasHeadersFrameRecived { get; set; }

        public bool Opened
        {
            get { return _state == StreamState.Opened; }
            set 
            { 
                Contract.Assert(value);
                Http2Logger.StreamStateTransition(Id, _state, StreamState.Opened);
                _state = StreamState.Opened; 
            }
        }

        public bool Idle
        {
            get { return _state == StreamState.Idle; }
            set { Contract.Assert(value); _state = StreamState.Idle; }
        }

        public bool HalfClosedRemote
        {
            get { return _state == StreamState.HalfClosedRemote; }
            set
            {
                Contract.Assert(value);
                StreamState previousState = _state;

                _state = HalfClosedLocal ? StreamState.Closed : StreamState.HalfClosedRemote;

                Http2Logger.StreamStateTransition(Id, previousState, _state);
            }
        }

        public bool HalfClosedLocal
        {
            get { return _state == StreamState.HalfClosedLocal; }
            set
            {
                Contract.Assert(value);
                StreamState previousState = _state;

                _state = HalfClosedRemote ? StreamState.Closed : StreamState.HalfClosedLocal;

                Http2Logger.StreamStateTransition(Id, previousState, _state);
            }
        }

        public bool ReservedLocal
        {
            get { return _state == StreamState.ReservedLocal; }
            set
            {
                Contract.Assert(value);
                StreamState previousState = _state;

                _state = ReservedRemote ? StreamState.Closed : StreamState.ReservedLocal;

                Http2Logger.StreamStateTransition(Id, previousState, _state);
            }
        }

        public bool ReservedRemote
        {
            get { return _state == StreamState.ReservedRemote; }
            set
            {
                Contract.Assert(value);
                StreamState previousState = _state;

                _state = HalfClosedLocal ? StreamState.Closed : StreamState.ReservedRemote;

                Http2Logger.StreamStateTransition(Id, previousState, _state);
            }
        }

        public bool Closed
        {
            get { return _state == StreamState.Closed; }
            set 
            { 
                Contract.Assert(value);
                Http2Logger.StreamStateTransition(Id, _state, StreamState.Closed);
                _state = StreamState.Closed;
            }
        }

        public HeadersList Headers { get; internal set; }

        #endregion

        #region FlowControl

        public void UpdateWindowSize(Int32 delta, bool needToLog = true)
        {
            /* 14 -> 6.9.1
            A sender MUST NOT allow a flow control window to exceed 2^31 - 1
            bytes. If a sender receives a WINDOW_UPDATE that causes a flow
            control window to exceed this maximum it MUST terminate either the
            stream or the connection, as appropriate.  For streams, the sender
            sends a RST_STREAM with the error code of FLOW_CONTROL_ERROR code;
            for the connection, a GOAWAY frame with a FLOW_CONTROL_ERROR code. */
            if (WindowSize > Constants.MaxWindowSize)
            {
                Http2Logger.Error("Incorrect window size : {0}", WindowSize);
                throw new ProtocolError(ResetStatusCode.FlowControlError,
                    String.Format("Incorrect window size : {0}", WindowSize));
            }


            WindowSize += delta;

            if (needToLog && delta != 0)
                Http2Logger.Debug("Window size changed: stream id={0}, from={1}, to={2}, delta={3}", 
                    Id, WindowSize - delta, WindowSize, Math.Abs(delta));
        }

        /// <summary>
        /// Pumps the unshipped frames.
        /// Calls after window update was received for stream. 
        /// Method tried to deliver as many unshipped data frames as it can.
        /// </summary>
        public void PumpUnshippedFrames()
        {
            if (Closed)
                return;

            // Handle window update one at a time
            lock (_unshippedDeliveryLock)
            {
                while (_unshippedFrames.Count > 0 && !IsBlocked && !_flowCtrlManager.IsSessionBlocked)
                {
                    var dataFrame = _unshippedFrames.Dequeue();
                    WriteDataFrame(dataFrame);
                }

                //Do not dispose if unshipped frames are still here
                if (HalfClosedRemote && HalfClosedLocal && !Closed)
                {
                    Close(ResetStatusCode.None);
                }
            }
        }

        //It should be Int64 becouse if it can be greater 2^31, it will be < 0
        public Int64 WindowSize { get; private set; }

        public Int64 SentDataAmount { get; private set; }

        public Int64 ReceivedDataAmount { get; set; }

        public bool IsBlocked { get; set; }
        #endregion

        #region WriteMethods

        public void WriteHeadersFrame(HeadersList headers, bool isEndStream, bool isEndHeaders)
        {
            if (headers == null)
                throw new ArgumentNullException("headers is null");

            if (Closed)
                return;

            var frame = new HeadersFrame(_id, true)
                {
                    IsEndHeaders = isEndHeaders,
                    IsEndStream = isEndStream,
                    Headers = headers,
                };

            Http2Logger.FrameSend(frame);

            _outgoingQueue.WriteFrame(frame);

            if (frame.IsEndStream)
            {
                HalfClosedLocal = true;
            }
            else if (ReservedLocal)
            {
                HalfClosedRemote = true;
            }

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        /// <summary>
        /// Writes the data frame.
        /// If flow control manager has blocked stream, frames are adding to the unshippedFrames collection.
        /// After window update for that stream they will be delivered.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="isEndStream">if set to <c>true</c> [is fin].</param>
        public void WriteDataFrame(ArraySegment<byte> data, bool isEndStream)
        {
            if (data.Array == null)
                throw new ArgumentNullException("data is null");

            if (Closed)
                return;

            /* 14 -> 6.1
            If the length of the padding is greater than the
            length of the remainder of the frame payload, the recipient MUST
            treat this as a connection error (Section 5.4.1) of type
            PROTOCOL_ERROR. */
            bool hasPadding = !isEndStream;
            var dataFrame = new DataFrame(_id, data, isEndStream, hasPadding);

            // We cant let lesser frame that were passed through flow control window
            // be sent before greater frames that were not passed through flow control window

            /* 14 -> 6.9.1
            The sender MUST NOT
            send a flow controlled frame with a length that exceeds the space
            available in either of the flow control windows advertised by the receiver. */
            if (_unshippedFrames.Count != 0 || WindowSize - dataFrame.Data.Count < 0)
            {
                _unshippedFrames.Enqueue(dataFrame);
                return;
            }

            if (!IsBlocked && !_flowCtrlManager.IsSessionBlocked)
            {
                Http2Logger.FrameSend(dataFrame);

                _outgoingQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.Data.Count;
                _flowCtrlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.Info("Sent for stream id={0}: {1} bytes", dataFrame.StreamId, SentDataAmount);
                    HalfClosedLocal = true;
                }

                if (OnFrameSent != null)
                {
                    OnFrameSent(this, new FrameSentEventArgs(dataFrame));
                }
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

        /// <summary>
        /// Writes the data frame. Method is used for pushing unshipped frames.
        /// If flow control manager has blocked stream, frames are adding to the unshippedFrames collection.
        /// After window update for that stream they will be delivered.
        /// </summary>
        /// <param name="dataFrame">The data frame.</param>
        private void WriteDataFrame(DataFrame dataFrame)
        {
            if (dataFrame == null)
                throw new ArgumentNullException("dataFrame is null");

            if (Closed)
                return;

            if (!IsBlocked && !_flowCtrlManager.IsSessionBlocked)
            {
                Http2Logger.FrameSend(dataFrame);

                _outgoingQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.Data.Count;
                _flowCtrlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.Info("Bytes sent for stream id={0}: {1}", dataFrame.StreamId, SentDataAmount);
                    HalfClosedLocal = true;
                }

                if (OnFrameSent != null)
                {
                    OnFrameSent(this, new FrameSentEventArgs(dataFrame));
                }
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

        public void WriteWindowUpdate(Int32 windowSize)
        {
            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException("windowSize should be greater than 0");

            if (windowSize > Constants.MaxWindowSize)
                throw new ProtocolError(ResetStatusCode.FlowControlError, "window size is too large");

            if (Closed)
                return;

            // TODO: handle idle state

            var frame = new WindowUpdateFrame(_id, windowSize);

            Http2Logger.FrameSend(frame);

            _outgoingQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        public void WriteRst(ResetStatusCode code)
        {
            if (Closed)
                return;

            var frame = new RstStreamFrame(_id, code);

            Http2Logger.FrameSend(frame);

            _outgoingQueue.WriteFrame(frame);
            WasRstOnStream = true;

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        // TODO: writing push_promise is available in any time now, need to handle it
        public void WritePushPromise(IDictionary<string, string[]> pairs, Int32 streamId)
        {
            if (streamId % 2 != 0 && Id % 2 != 0)
                throw new InvalidOperationException("Client can't send PUSH_PROMISE frames");

            if (Closed)
                return;

            var headers = new HeadersList(pairs);

            //TODO: Do not add padding for PUSH_PROMISE frame to support Chromium
            var frame = new PushPromiseFrame(streamId, Id, false, true, headers);

            /* 14 -> 5.1
            Sending a PUSH_PROMISE frame marks the associated stream for later use.
            The stream state for the reserved stream transitions to reserved (local). */        
            ReservedLocal = true;

            Http2Logger.FrameSend(frame);

            _outgoingQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        #endregion

        public void Close(ResetStatusCode code)
        {
            if (Closed || Idle) return;

            OnFrameSent = null;

            Http2Logger.Info("Total outgoing data frames volume " + SentDataAmount);
            Http2Logger.Info("Total frames sent: {0}", FramesSent);
            Http2Logger.Info("Total frames received: {0}", FramesReceived);

            if (code == ResetStatusCode.Cancel || code == ResetStatusCode.InternalError)
                WriteRst(code);

            Closed = true;

            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            OnClose = null;

            Http2Logger.Info("Stream closed " + _id);
        }
    }
}
