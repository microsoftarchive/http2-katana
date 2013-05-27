using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedProtocol.Framing;
using SharedProtocol.IO;

namespace SharedProtocol
{
    public class FlowControlMonitor : IMonitor
    {
        private WriteQueue _writeQueue;
        private FrameReader _frameReader;

        private Action<object, DataFrameSentEventArgs> _sentHandler;
        private Action<object, DataFrameReceivedEventArgs> _receivedHandler;

        public FlowControlMonitor(Action<object, DataFrameSentEventArgs> sentHandler, 
                                    Action<object, DataFrameReceivedEventArgs> receivedHandler)
        {
            _sentHandler = sentHandler;
            _receivedHandler = receivedHandler;
        }

        public void Attach(object ioPairforMonitoring)
        {
            if (_writeQueue != null || _frameReader != null)
                throw new MonitorIsBusyException();

            if (!(ioPairforMonitoring is Tuple<FrameReader, WriteQueue>))
                throw new InvalidCastException("Flow control monitor can be attached only to a Tuple<FrameReader, WriteQueue> object");

            _frameReader = ((Tuple<FrameReader, WriteQueue>)ioPairforMonitoring).Item1;
            _writeQueue = ((Tuple<FrameReader, WriteQueue>)ioPairforMonitoring).Item2;

            //_writeQueue.OnDataFrameSent += DataFrameSentHandler;
            //_frameReader.OnDataFrameReceived += DataFrameReceivedHandler;
        }

        public void Detach()
        {
            if (_writeQueue == null && _frameReader == null)
                throw new NoNullAllowedException("You are called Detach for non-attached monitor!");
        }

        private void DataFrameSentHandler(object sender, DataFrameSentEventArgs args)
        {
            _sentHandler.Invoke(sender, args);
        }

        private void DataFrameReceivedHandler(object sender, DataFrameReceivedEventArgs args)
        {
            _receivedHandler.Invoke(sender, args);
        }
    }
}
