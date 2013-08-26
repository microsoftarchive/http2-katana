using SharedProtocol.Compression;
using SharedProtocol.EventArgs;
using SharedProtocol.FlowControl;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using SharedProtocol.Utils;

namespace SharedProtocol
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

            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsFlowControlBlocked = false;
            IsFlowControlEnabled = _flowCrtlManager.IsStreamsFlowControlledEnabled;
            WindowSize = _flowCrtlManager.StreamsInitialWindowSize;

            _flowCrtlManager.NewStreamOpenedHandler(this);
        }

        #endregion

        #region Properties

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
                    Dispose();
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
                    Dispose();
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
                    Dispose();
                }
            }
        }

        public Int32 WindowSize { get; private set; }

        public Int64 SentDataAmount { get; private set; }

        public Int64 ReceivedDataAmount { get; set; }

        public bool IsFlowControlBlocked { get; set; }

        public bool IsFlowControlEnabled { get; set; }
        #endregion

        #region WriteMethods

        public void WriteHeadersFrame(HeadersList headers, bool isEndStream, bool isEndHeaders)
        {
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
            var dataFrame = new DataFrame(_id, new ArraySegment<byte>(data), isEndStream);

            if (IsFlowControlBlocked == false)
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
            if (IsFlowControlBlocked == false)
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
            var frame = new WindowUpdateFrame(_id, windowSize);
            _writeQueue.WriteFrame(frame);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }

        public void WriteRst(ResetStatusCode code)
        {
            var frame = new RstStreamFrame(_id, code);
            _writeQueue.WriteFrame(frame);
            ResetSent = true;

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }
        #endregion

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Http2Logger.LogDebug("Total outgoing data frames volume " + SentDataAmount);

            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            Http2Logger.LogDebug("Stream closed " + _id);

            OnClose = null;
            _flowCrtlManager.StreamClosedHandler(this);
            Disposed = true;
        }
    }
}
