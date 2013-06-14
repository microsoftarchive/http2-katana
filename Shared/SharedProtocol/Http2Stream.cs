using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharedProtocol
{
    /// <summary>
    /// Class represents http2 stream.
    /// </summary>
    public class Http2Stream : IDisposable
    {
        private readonly int _id;
        private StreamState _state;
        private readonly WriteQueue _writeQueue;
        private readonly Priority _priority;
        private readonly CompressionProcessor _compressionProc;
        private readonly FlowControlManager _flowCrtlManager;

        private readonly Queue<DataFrame> _unshippedFrames;
        private readonly object _unshippedDeliveryLock = new object();
        //Incoming
        public Http2Stream(Dictionary<string, string> headers, int id,
                           Priority priority, WriteQueue writeQueue,
                           FlowControlManager flowCrtlManager, CompressionProcessor comprProc)
            : this(id, priority, writeQueue, flowCrtlManager, comprProc)
        {
            Headers = headers;
        }

        //Outgoing
        public Http2Stream(int id, Priority priority, WriteQueue writeQueue, 
                           FlowControlManager flowCrtlManager, CompressionProcessor comprProc)
        {
            _id = id;
            _priority = priority;
            _writeQueue = writeQueue;
            _compressionProc = comprProc;
            _flowCrtlManager = flowCrtlManager;

            _unshippedFrames = new Queue<DataFrame>(16);

            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsFlowControlBlocked = false;
            IsFlowControlEnabled = _flowCrtlManager.IsStreamsFlowControlledEnabled;
            WindowSize = _flowCrtlManager.StreamsInitialWindowSize;

            _flowCrtlManager.NewStreamOpenedHandler(this);
        }

        #region Properties
        public int Id
        {
            get { return _id; }
        }

        public bool FinSent
        {
            get { return (_state & StreamState.FinSent) == StreamState.FinSent; }
            set { Contract.Assert(value); _state |= StreamState.FinSent; }
        }
        public bool FinReceived
        {
            get { return (_state & StreamState.FinReceived) == StreamState.FinReceived; }
            set { Contract.Assert(value); _state |= StreamState.FinReceived; }
        }

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

        public Dictionary<string, string> Headers { get; set; }
        #endregion

        #region FlowControl

        public void UpdateWindowSize(Int32 delta)
        {
            if (IsFlowControlEnabled)
            {
                WindowSize += delta;
            }

            Console.WriteLine(WindowSize);

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
                if (FinSent && FinReceived && !Disposed)
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
        public void WriteHeadersPlusPriorityFrame(Dictionary<string, string> headers, bool isFin)
        {
            Headers = headers;
            // TODO: Prioritization re-ordering will also break decompression. Scrap the priority queue.
            byte[] headerBytes = FrameHelpers.SerializeHeaderBlock(headers);
            headerBytes = _compressionProc.Compress(headerBytes);

            var frame = new HeadersPlusPriority(_id, headerBytes);

            frame.IsFin = isFin;
            frame.Priority = _priority;

            if (frame.IsFin)
            {
                FinSent = true;
            }

            _writeQueue.WriteFrameAsync(frame, _priority);

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
        /// <param name="isFin">if set to <c>true</c> [is fin].</param>
        public void WriteDataFrame(byte[] data, bool isFin)
        {
            var dataFrame = new DataFrame(_id, new ArraySegment<byte>(data), isFin);

            if (IsFlowControlBlocked == false)
            {
                _writeQueue.WriteFrameAsync(dataFrame, _priority);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsFin)
                {
                    Console.WriteLine("Transfer end");
                    FinSent = true;
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
                _writeQueue.WriteFrameAsync(dataFrame, _priority);
                SentDataAmount += dataFrame.FrameLength;

                _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));

                if (dataFrame.IsFin)
                {
                    Console.WriteLine("Transfer end");
                    FinSent = true;
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

        public void WriteHeaders(byte[] compressedHeaders)
        {
            _writeQueue.WriteFrameAsync(new HeadersFrame(_id, compressedHeaders), Priority.Pri3);
            
            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(new HeadersFrame(_id, compressedHeaders)));
            }
        }

        public void WriteWindowUpdate(Int32 windowSize)
        {
            var frame = new WindowUpdateFrame(_id, windowSize);
            _writeQueue.WriteFrameAsync(frame, _priority);

            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(frame));
            }
        }

        public void WriteRst(ResetStatusCode code)
        {
            var frame = new RstStreamFrame(_id, code);
            _writeQueue.WriteFrameAsync(frame, _priority);
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

            Console.WriteLine("Total data frames volume {0}", SentDataAmount);

            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            Console.WriteLine("Stream closed {0}", _id);

            OnClose = null;
            _flowCrtlManager.StreamClosedHandler(this);
            Disposed = true;
        }

        /// <summary>
        /// Occurs when stream was sent frame.
        /// </summary>
        public event EventHandler<FrameSentArgs> OnFrameSent;

        /// <summary>
        /// Occurs when stream closes.
        /// </summary>
        public event EventHandler<StreamClosedEventArgs> OnClose;
    }
}
