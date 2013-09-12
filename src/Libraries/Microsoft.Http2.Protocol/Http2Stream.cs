using Microsoft.Http2.Protocol.Compression;
using Microsoft.Http2.Protocol.EventArgs;
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
    public class Http2Stream : IDisposable
    {
        #region Fields

        private readonly int _id;
        private StreamState _state;
        private readonly WriteQueue _writeQueue;
        private readonly ICompressionProcessor _compressionProc;
        private readonly FlowControlManager _flowCrtlManager;

        private readonly Queue<DataFrame> _unshippedFrames;
        private readonly object _unshippedDeliveryLock = new object();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when stream was sent frame.
        /// </summary>
        public event EventHandler<FrameSentArgs> OnFrameSent;

        /// <summary>
        /// Occurs when stream closes.
        /// </summary>
        public event EventHandler<StreamClosedEventArgs> OnClose;

        #endregion

        #region Constructors

        //Incoming
        internal Http2Stream(HeadersList headers, int id,
                           WriteQueue writeQueue, FlowControlManager flowCrtlManager, 
                           ICompressionProcessor comprProc, Priority priority = Priority.Pri3)
            : this(id, writeQueue, flowCrtlManager, comprProc, priority)
        {
            Headers = headers;
        }

        //Outgoing
        internal Http2Stream(int id, WriteQueue writeQueue, FlowControlManager flowCrtlManager,
                           ICompressionProcessor comprProc, Priority priority = Priority.Pri3)
        {
            _id = id;
            Priority = priority;
            _writeQueue = writeQueue;
            _compressionProc = comprProc;
            _flowCrtlManager = flowCrtlManager;

            _unshippedFrames = new Queue<DataFrame>(16);
            Headers = new HeadersList();
            ReceivedDataFrames = new Queue<DataFrame>(16);

            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsFlowControlBlocked = false;
            IsFlowControlEnabled = _flowCrtlManager.IsStreamsFlowControlledEnabled;
            WindowSize = _flowCrtlManager.StreamsInitialWindowSize;

            _flowCrtlManager.NewStreamOpenedHandler(this);
        }

        #endregion

        #region Properties

        public Queue<DataFrame> ReceivedDataFrames; 

        public int Id
        {
            get { return _id; }
        }

        public bool EndStreamSent
        {
            get { return (_state & StreamState.EndStreamSent) == StreamState.EndStreamSent; }
            set
            {
                Contract.Assert(value); 
                _state |= StreamState.EndStreamSent;

                if (EndStreamReceived)
                {
                    Dispose(ResetStatusCode.None);
                }
            }
        }
        public bool EndStreamReceived
        {
            get { return (_state & StreamState.EndStreamReceived) == StreamState.EndStreamReceived; }
            set
            {
                Contract.Assert(value); 
                _state |= StreamState.EndStreamReceived;

                if (EndStreamSent)
                {
                    Dispose(ResetStatusCode.None);
                }
            }
        }

        public Priority Priority { get; set; }

        public bool ResetSent
        {
            get { return (_state & StreamState.ResetSent) == StreamState.ResetSent; }
            set { Contract.Assert(value); _state |= StreamState.ResetSent; }
        }
        public bool ResetReceived
        {
            get { return (_state & StreamState.ResetReceived) == StreamState.ResetReceived; }
            set { Contract.Assert(value); _state |= StreamState.ResetReceived; }
        }

        public bool Disposed
        {
            get { return (_state & StreamState.Disposed) == StreamState.Disposed; }
            set { Contract.Assert(value); _state |= StreamState.Disposed; }
        }

        public HeadersList Headers { get; private set; }

        #endregion

        #region FlowControl

        public void UpdateWindowSize(Int32 delta)
        {
            if (IsFlowControlEnabled)
            {
                WindowSize += delta;
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
            if (Disposed)
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
                if (EndStreamSent && EndStreamReceived && !Disposed)
                {
                    Dispose(ResetStatusCode.None);
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
            if (Disposed)
                return;

            Headers.AddRange(headers);

            byte[] headerBytes = _compressionProc.Compress(headers);

            var frame = new HeadersFrame(_id, headerBytes, Priority)
                {
                    IsEndHeaders = isEndHeaders,
                    IsEndStream = isEndStream,
                };

            _writeQueue.WriteFrame(frame);

            if (frame.IsEndStream)
            {
                EndStreamSent = true;
            }

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }

        /// <summary>
        /// Writes the data frame.
        /// If flow control manager has blocked stream, frames are adding to the unshippedFrames collection.
        /// After window update for that stream they will be delivered.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="isEndStream">if set to <c>true</c> [is fin].</param>
        public void WriteDataFrame(byte[] data, bool isEndStream)
        {
            if (Disposed)
                return;

            var dataFrame = new DataFrame(_id, new ArraySegment<byte>(data), isEndStream);

            /*if (isEndStream && _unshippedFrames.Count != 0)
            {
                _unshippedFrames.Enqueue(dataFrame);
                return;
            }*/

            if (!IsFlowControlBlocked)
            {
                _writeQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.LogDebug("Transfer end");
                    EndStreamSent = true;
                }

                if (OnFrameSent != null)
                {
                    OnFrameSent(this, new FrameSentArgs(dataFrame));
                }
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

        /// <summary>
        /// Writes the data frame.
        /// If flow control manager has blocked stream, frames are adding to the unshippedFrames collection.
        /// After window update for that stream they will be delivered.
        /// </summary>
        /// <param name="dataFrame">The data frame.</param>
        public void WriteDataFrame(DataFrame dataFrame)
        {
            if (Disposed)
                return;

            if (!IsFlowControlBlocked)
            {
                _writeQueue.WriteFrame(dataFrame);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsEndStream)
                {
                    Http2Logger.LogDebug("Transfer end");
                    EndStreamSent = true;
                }

                if (OnFrameSent != null)
                {
                    OnFrameSent(this, new FrameSentArgs(dataFrame));
                }
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

        public void WriteWindowUpdate(Int32 windowSize)
        {
            if (Disposed)
                return;

            var frame = new WindowUpdateFrame(_id, windowSize);
            _writeQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }

        public void WriteRst(ResetStatusCode code)
        {
            if (Disposed)
                return;

            var frame = new RstStreamFrame(_id, code);
            
            _writeQueue.WriteFrame(frame);
            ResetSent = true;

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }

        public void EnqueueDataFrame(DataFrame frame)
        {
            ReceivedDataFrames.Enqueue(frame);
        }

        public DataFrame DequeueDataFrame()
        {
            return ReceivedDataFrames.Dequeue();
        }

        #endregion

        public void Dispose()
        {
            Dispose(ResetStatusCode.None);
        }

        public void Dispose(ResetStatusCode code)
        {
            if (Disposed)
            {
                return;
            }

            Http2Logger.LogDebug("Total outgoing data frames volume " + SentDataAmount);

            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            if (code != ResetStatusCode.None)
                WriteRst(code);

            Http2Logger.LogDebug("Stream closed " + _id);

            OnClose = null;
            _flowCrtlManager.StreamClosedHandler(this);
            Disposed = true;
        }
    }
}
