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
        private Dictionary<string, string> _headers;
        private StreamState _state;
        private readonly WriteQueue _writeQueue;
        private readonly Priority _priority;
        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private readonly CompressionProcessor _compressionProc;
        private readonly FlowControlManager _flowCrtlManager;

        private readonly Queue<DataFrame> _unshippedFrames;

        //Incoming
        public Http2Stream(Dictionary<string, string> headers, int id,
                           Priority priority, WriteQueue writeQueue,
                           FlowControlManager flowCrtlManager, CompressionProcessor comprProc)
            : this(id, priority, writeQueue, flowCrtlManager, comprProc)
        {
            _headers = headers;
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
                Console.WriteLine("File sent: " + GetHeader(":path"));
                Dispose();
            }
        }

        public Int32 WindowSize { get; private set; }

        public Int64 SentDataAmount { get; private set; }

        public Int64 ReceivedDataAmount { get; private set; }

        public bool IsFlowControlBlocked { get; set; }

        public bool IsFlowControlEnabled { get; set; }
        #endregion

        #region Incoming data processing

        private string GetHeader(string key)
        {
            foreach (var header in _headers)
            {
                if (header.Key == key)
                    return header.Value;
            }

            return null;
        }

        public void ProcessIncomingData(DataFrame dataFrame)
        {
            _flowCrtlManager.DataFrameReceivedHandler(this, new DataFrameReceivedEventArgs(dataFrame));
            string path = GetHeader(":path");
            FileHelper.SaveToFile(dataFrame.Data.Array, dataFrame.Data.Offset, dataFrame.Data.Count, assemblyPath + path,
                                  ReceivedDataAmount != 0);

            ReceivedDataAmount += dataFrame.FrameLength;

            if (dataFrame.IsFin)
            {
                Console.WriteLine("File downloaded: " + GetHeader(":path"));
                Dispose();
            }
            else
            {
                //Aggresive window update
                WriteWindowUpdate(Constants.MaxDataFrameContentSize);  
            }
        }

        #endregion

        #region Responce Methods

        private void SendResponse()
        {
            byte[] binaryFile = FileHelper.GetFile(GetHeader(":path"));
            int i = 0;

            Console.WriteLine("Transfer begin");

            while (binaryFile.Length > i)
            {
                bool isLastData = binaryFile.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = WindowSize > 0 
                                ?
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize, WindowSize)
                                :
                                    MathEx.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize);
                
                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryFile, i, chunk, 0, chunk.Length);

                WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            }

        }

        public void Run()
        {
            try
            {
                SendResponse();
            }
            //TODO Refactor catch
            catch (Exception)
            {
                WriteRst(ResetStatusCode.InternalError);
                Dispose();
            }
        }
        #endregion

        #region WriteMethods
        public void WriteHeadersPlusPriorityFrame(Dictionary<string, string> headers, bool isFin)
        {
            _headers = headers;
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
        }

        public void WriteDataFrame(byte[] data, bool isFin)
        {
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
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

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
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }
        }

        public void WriteHeaders(SettingsPair[] settings)
        {
            //TODO process write headers
        }

        public void WriteWindowUpdate(Int32 windowSize)
        {
            var frame = new WindowUpdateFrame(_id, windowSize);
            _writeQueue.WriteFrameAsync(frame, _priority);
        }

        public void WriteRst(ResetStatusCode code)
        {
            var frame = new RstStreamFrame(_id, code);
            _writeQueue.WriteFrameAsync(frame, _priority);
            ResetSent = true;
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

        public event EventHandler<StreamClosedEventArgs> OnClose;
    }
}
