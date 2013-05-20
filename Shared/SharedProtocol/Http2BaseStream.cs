using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

namespace SharedProtocol
{
    public abstract class Http2BaseStream : IDisposable
    {
        protected int _id;
        protected StreamState _state;
        protected WriteQueue _writeQueue;
        protected CancellationToken _cancel;
        protected Priority _priority;
        protected Stream _incomingStream;
        protected OutputStream _outputStream;
        protected HeaderWriter _headerWriter;
        protected Int32 _initialWindowSize;
        protected bool _isDataFrameReceived;

        protected Http2BaseStream(int id, WriteQueue writeQueue, HeaderWriter headerWriter, int initialWindowSize, CancellationToken cancel)
        {
            _id = id;
            _writeQueue = writeQueue;
            _cancel = cancel;
            _headerWriter = headerWriter;
            _initialWindowSize = initialWindowSize;
            _isDataFrameReceived = false;
        }

        public bool IsDataFrameReceived
        {
            get { return _isDataFrameReceived; }
            set { _isDataFrameReceived = value; }
        }

        public int Id
        {
            get { return _id; }
        }

        protected bool FinSent
        {
            get { return (_state & StreamState.FinSent) == StreamState.FinSent; }
            set { Contract.Assert(value); _state |= StreamState.FinSent; }
        }

        protected bool FinReceived
        {
            get { return (_state & StreamState.FinReceived) == StreamState.FinReceived; }
            set { Contract.Assert(value); _state |= StreamState.FinReceived; }
        }

        protected bool ResetSent
        {
            get { return (_state & StreamState.ResetSent) == StreamState.ResetSent; }
            set { Contract.Assert(value); _state |= StreamState.ResetSent; }
        }

        protected bool ResetReceived
        {
            get { return (_state & StreamState.ResetReceived) == StreamState.ResetReceived; }
            set { Contract.Assert(value); _state |= StreamState.ResetReceived; }
        }

        protected bool Disposed
        {
            get { return (_state & StreamState.Disposed) == StreamState.Disposed; }
            set { Contract.Assert(value); _state |= StreamState.Disposed; }
        }

        protected Dictionary<string, string> Headers { get; set; }
        
        public string GetHeader(string key)
        {
            foreach (var header in Headers)
            {
                if (header.Key == key)
                    return header.Value;
            }

            return null;
        }
        // Additional data has arrived for the request stream.  Add it to our request stream buffer, 
        // update any necessary state (e.g. FINs), and trigger any waiting readers.
        public void ReceiveData(DataFrame dataFrame)
        {
            if (Disposed)
            {
                // TODO: Send reset?
                return;
            }

            Contract.Assert(_incomingStream != null);
            ArraySegment<byte> data = dataFrame.Data;
            // TODO: Decompression?
            _incomingStream.Write(data.Array, data.Offset, data.Count);
            if (dataFrame.IsFin)
            {
                FinReceived = true;
                _incomingStream.Dispose();
            }
        }

        // Make sure the request/response has been started, and that the headers have been sent.
        // Start it if possible, throw otherwise.
        public abstract void EnsureStarted();

        public void SendExtraHeaders(Dictionary<string, string> headers, bool endOfMessage)
        {
            // Make sure the initial headers have been sent.
            EnsureStarted();
            // Assert the body is incomplete.
            Contract.Assert(!FinSent && !ResetSent && !ResetReceived);

            // Set end-of-message state so we don't try to send an empty fin data frame.
            if (endOfMessage)
            {
                FinSent = true;
            }

            _headerWriter.WriteHeaders(headers, _id, _priority, endOfMessage, _cancel);
        }

        public void ReceiveExtraHeaders(HeadersFrame headerFrame, IList<KeyValuePair<string, string>> headers)
        {
            if (Disposed)
            {
                return;
            }

            // TODO: Where do we put them? How do we notify the stream owner that they're here?

            if (headerFrame.IsFin)
            {
                FinReceived = true;
                _incomingStream.Dispose();
            }
        }

        public void UpdateWindowSize(int delta)
        {
            Contract.Assert(_outputStream != null);
            _outputStream.AddFlowControlCredit(delta);
        }

        protected void SendWindowUpdate(int delta)
        {
            WindowUpdateFrame windowUpdate = new WindowUpdateFrame(Id, delta);
            _writeQueue.WriteFrameAsync(windowUpdate, Priority.Control);
        }

        public virtual void Reset(ResetStatusCode statusCode)
        {
            ResetReceived = true;
            if (_outputStream != null)
            {
                _outputStream.Dispose();
            }
            if (_incomingStream != null && _incomingStream != Stream.Null && !FinReceived)
            {
                InputStream inputStream = (InputStream)_incomingStream;
                inputStream.Abort(statusCode.ToString());
            }

            // Not disposing here because many of the resources may still be in use.
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
            if (disposing)
            {
                if (_incomingStream != null)
                {
                    _incomingStream.Dispose();
                }

                if (_outputStream != null)
                {
                    _outputStream.Dispose();
                }
            }
        }
    }
}
