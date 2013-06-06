using System.Reflection;
using SharedProtocol.Compression;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace SharedProtocol
{
    public class Http2Stream : IDisposable
    {
        private readonly int _id;
        private StreamState _state;
        private readonly WriteQueue _writeQueue;
        private readonly Priority _priority;
        private readonly CompressionProcessor _compressionProc;
        private readonly FlowControlManager _flowCrtlManager;

        private int windowUpdateReceivedCount = 0;
        private int dataFrameSentCount = 0;

        private readonly Queue<DataFrame> _unshippedFrames;

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
            windowUpdateReceivedCount++;
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

        public void PumpUnshippedFrames()
        {
            while (_unshippedFrames.Count > 0 && IsFlowControlBlocked == false)
            {
                var dataFrame = _unshippedFrames.Dequeue();
                WriteDataFrame(dataFrame);
            }

            //Do not dispose if unshipped frames are still here
            if (FinSent && FinReceived)
            {
                Console.WriteLine("Received {0} window update frames", windowUpdateReceivedCount);
                Console.WriteLine("Sent {0} data frames", dataFrameSentCount);
                Console.WriteLine("Sent data {0}", SentDataAmount);
                Console.WriteLine("File sent: " + Headers[":path"]);
                Dispose();
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

        public void WriteDataFrame(byte[] data, bool isFin)
        {
            dataFrameSentCount++;
            var dataFrame = new DataFrame(_id, new ArraySegment<byte>(data), isFin);
            dataFrame.IsFin = isFin;

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

        public void WriteDataFrame(DataFrame dataFrame)
        {
            dataFrameSentCount++;
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

        public void WriteHeaders(SettingsPair[] settings)
        {
            //TODO process write headers
            if (OnFrameSent != null)
            {
                OnFrameSent(this, new FrameSentArgs(new HeadersFrame(1, null)));
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
            if (OnClose != null)
                OnClose(this, new StreamClosedEventArgs(_id));

            OnClose = null;
            _flowCrtlManager.StreamClosedHandler(this);
            Disposed = true;
        }

        public event EventHandler<FrameSentArgs> OnFrameSent;

        public event EventHandler<StreamClosedEventArgs> OnClose;
    }
}
