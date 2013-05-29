using System.Reflection;
using Org.Mentalis.Security.Ssl;
using Owin.Types;
using SharedProtocol.Compression;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

namespace SharedProtocol
{
    public class Http2Stream : IDisposable
    {
        private int _id;
        private Dictionary<string, string> _headers;
        private StreamState _state;
        private WriteQueue _writeQueue;
        private Priority _priority;
        private static string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private CompressionProcessor _compressionProc;
        private FlowControlManager _flowCrtlManager;

        private Int32 _receivedDataConstraint = Constants.DefaultFlowControlCredit;

        private Queue<DataFrame> _unshippedFrames;

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

            WindowSize = 0;
            SentDataAmount = 0;
            ReceivedDataAmount = 0;
            IsFlowControlBlocked = false;
            IsFlowControlEnabled = _flowCrtlManager.IsStreamsFlowControlledEnabled;

            _flowCrtlManager.NewStreamOpenedHandler(this);
            UpdateWindowSize(_flowCrtlManager.StreamsInitialWindowSize);
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

            Console.WriteLine("File sent: " + _headers[":path"]);
            //Do not dispose if unshipped frames are still here
            if (FinSent && FinReceived)
            {
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
                //Aggressive window update
                if (_receivedDataConstraint - ReceivedDataAmount <= 0)
                {
                    WriteWindowUpdate(Constants.DefaultFlowControlCredit * 10); // For example * 10

                    //Add something to the receive constraint.
                    _receivedDataConstraint += 32768; // constant value is irrelevant. It will be counted somehow later
                }   
            }
        }

        #endregion

        #region Responce Methods

        private void SendResponse()
        {
            byte[] binaryFile = FileHelper.GetFile(_headers[":path"]);
            int i = 0;

            while (binaryFile.Length > i)
            {
                bool isLastData = binaryFile.Length - i < Constants.MaxDataFrameContentSize;
                int chunkSize = Math.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize);

                byte[] chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryFile, i, chunk, 0, chunk.Length);

                WriteDataFrame(chunk, isLastData);

                i += Constants.MaxDataFrameContentSize;
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
            HeadersPlusPriority frame = new HeadersPlusPriority(_id, headerBytes);
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
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }

            if (dataFrame.IsFin)
            {
                FinSent = true;
            }

            _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));
            SentDataAmount += dataFrame.FrameLength;
        }

        public void WriteDataFrame(DataFrame dataFrame)
        {
            if (IsFlowControlBlocked == false)
            {
                _writeQueue.WriteFrameAsync(dataFrame, _priority);
            }
            else
            {
                _unshippedFrames.Enqueue(dataFrame);
            }

            if (dataFrame.IsFin)
            {
                FinSent = true;
            }

            _flowCrtlManager.DataFrameSentHandler(this, new DataFrameSentEventArgs(dataFrame));
            SentDataAmount += dataFrame.FrameLength;
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
