// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using Microsoft.Http2.Protocol.Compression;
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
        private readonly WriteQueue _writeQueue;
        private readonly FlowControlManager _flowCrtlManager;

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
                           WriteQueue writeQueue, FlowControlManager flowCrtlManager, int priority = Constants.DefaultStreamPriority)
            : this(id, writeQueue, flowCrtlManager, priority)
        {
            Headers = headers;
        }

        //Outgoing
        internal Http2Stream(int id, WriteQueue writeQueue, FlowControlManager flowCrtlManager, int priority = Constants.DefaultStreamPriority)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException("invalid id for stream");

            if (priority < 0 || priority > Constants.MaxPriority)
                throw  new ArgumentOutOfRangeException("priority out of range");

            _id = id;
            Priority = priority;
            _writeQueue = writeQueue;
            _flowCrtlManager = flowCrtlManager;

            _unshippedFrames = new Queue<DataFrame>(16);
            Headers = new HeadersList();

            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsFlowControlBlocked = false;
            IsFlowControlEnabled = _flowCrtlManager.IsFlowControlEnabled;
            WindowSize = _flowCrtlManager.StreamsInitialWindowSize;

            _flowCrtlManager.NewStreamOpenedHandler(this);
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

        public bool Opened
        {
            get { return _state == StreamState.Opened; }
            set { Contract.Assert(value); _state = StreamState.Opened; }
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
                _state = HalfClosedLocal ? StreamState.Closed : StreamState.HalfClosedRemote;

                if (Closed)
                {
                    Close(ResetStatusCode.None);
                }
            }
        }

        public bool HalfClosedLocal
        {
            get { return _state == StreamState.HalfClosedLocal; }
            set
            {
                Contract.Assert(value);
                _state = HalfClosedRemote ? StreamState.Closed : StreamState.HalfClosedLocal;

                if (Closed)
                {
                    Close(ResetStatusCode.None);
                }
            }
        }

        public bool ReservedLocal
        {
            get { return _state == StreamState.ReservedLocal; }
            set
            {
                Contract.Assert(value);
                _state = ReservedRemote ? StreamState.Closed : StreamState.ReservedLocal;

                if (Closed)
                {
                    Close(ResetStatusCode.None);
                }
            }
        }

        public bool ReservedRemote
        {
            get { return _state == StreamState.ReservedRemote; }
            set
            {
                Contract.Assert(value);
                _state = HalfClosedLocal ? StreamState.Closed : StreamState.ReservedRemote;

                if (Closed)
                {
                    Close(ResetStatusCode.None);
                }
            }
        }

        public bool Closed
        {
            get { return _state == StreamState.Closed; }
            set { Contract.Assert(value); _state = StreamState.Closed; }
        }

        public HeadersList Headers { get; internal set; }

        #endregion

        #region FlowControl

        public void UpdateWindowSize(Int32 delta)
        {
            if (IsFlowControlEnabled)
            {
                //09 -> 6.9.1.  The Flow Control Window
                //A sender MUST NOT allow a flow control window to exceed 2^31 - 1
                //bytes.  If a sender receives a WINDOW_UPDATE that causes a flow
                //control window to exceed this maximum it MUST terminate either the
                //stream or the connection, as appropriate.  For streams, the sender
                //sends a RST_STREAM with the error code of FLOW_CONTROL_ERROR code;
                //for the connection, a GOAWAY frame with a FLOW_CONTROL_ERROR code.
                WindowSize += delta;

                if (WindowSize > Constants.MaxWindowSize)
                {
                    Http2Logger.LogDebug("Incorrect window size : {0}", WindowSize);
                    throw new ProtocolError(ResetStatusCode.FlowControlError, String.Format("Incorrect window size : {0}", WindowSize));
                }
            }

            //Unblock stream if it was blocked by flowCtrlManager
            if (WindowSize > 0 && IsFlowControlBlocked)
            {
                IsFlowControlBlocked = false;
            }
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

            //Handle window update one at a time
            lock (_unshippedDeliveryLock)
            {
                while (_unshippedFrames.Count > 0 && IsFlowControlBlocked == false)
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

        public Int32 WindowSize { get; set; }

        public Int64 SentDataAmount { get; private set; }

        public Int64 ReceivedDataAmount { get; set; }

        public bool IsFlowControlBlocked { get; set; }

        public bool IsFlowControlEnabled { get; set; }
        #endregion

        #region WriteMethods

        public void WriteHeadersFrame(HeadersList headers, bool isEndStream, bool isEndHeaders)
        {
            if (headers == null)
                throw new ArgumentNullException("headers is null");

            if (Closed)
                return;

            var frame = new HeadersFrame(_id, Priority)
                {
                    IsEndHeaders = isEndHeaders,
                    IsEndStream = isEndStream,
                    Headers = headers,
                };

            _writeQueue.WriteFrame(frame);

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

            var dataFrame = new DataFrame(_id, data, isEndStream);

            //We cant let lesser frame that were passed through flow control window
            //be sent before greater frames that were not passed through flow control window

            //09 -> 6.9.1.  The Flow Control Window
            //The sender MUST NOT
            //send a flow controlled frame with a length that exceeds the space
            //available in either of the flow control windows advertised by the receiver.
            if (_unshippedFrames.Count != 0 || WindowSize - dataFrame.Data.Count < 0)
            {
                _unshippedFrames.Enqueue(dataFrame);
                return;
            }

            if (!IsFlowControlBlocked)
            {
                _writeQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.LogDebug("Transfer end");
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

            if (!IsFlowControlBlocked)
            {
                _writeQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.LogDebug("Transfer end");
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
                throw new ArgumentOutOfRangeException("windowSize should be greater thasn 0");

            if (windowSize > Constants.MaxWindowSize)
                throw new ProtocolError(ResetStatusCode.FlowControlError, "window size is too large");

            //09 -> 6.9.4.  Ending Flow Control
            //After a receiver reads in a frame that marks the end of a stream (for
            //example, a data stream with a END_STREAM flag set), it MUST cease
	        //transmission of WINDOW_UPDATE frames for that stream.
            if (Closed)
                return;

            //TODO handle idle state

            var frame = new WindowUpdateFrame(_id, windowSize);
            _writeQueue.WriteFrame(frame);

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
            
            _writeQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        //TODO Think about: writing push_promise is available in any time now. Need to handle it.
        public void WritePushPromise(IDictionary<string, string[]> pairs, Int32 promisedId)
        {
            if (Id % 2 != 0 && promisedId % 2 != 0)
                throw new InvalidOperationException("Client cant send push_promise frames");

            if (Closed)
                return;

            var headers = new HeadersList(pairs);

            //TODO IsEndPushPromise should be computationable
            var frame = new PushPromiseFrame(Id, promisedId, true, headers);

            ReservedLocal = true;

            _writeQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentEventArgs(frame));
            }
        }

        #endregion

        public void Close(ResetStatusCode code)
        {
            if (Closed || Idle)
            {
                return;
            }

            OnFrameSent = null;

            Http2Logger.LogDebug("Total outgoing data frames volume " + SentDataAmount);
            Http2Logger.LogDebug("Total frames sent: {0}", FramesSent);
            Http2Logger.LogDebug("Total frames received: {0}", FramesReceived);

            if (code == ResetStatusCode.Cancel || code == ResetStatusCode.InternalError)
                WriteRst(code);

            _flowCrtlManager.StreamClosedHandler(this);

            Closed = true;

            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            OnClose = null;

            Http2Logger.LogDebug("Stream closed " + _id);
        }
    }
}
