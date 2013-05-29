using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class FlowControlManager
    {
        private Http2Session _flowControlledSession;
        private ActiveStreams _streamCollection;
        private Int32 _options;

        public Int32 Options 
        { 
           get
            {
                return _options;
            }
           set
           {
               _options = value;

               if (IsStreamsFlowControlledEnabled == false)
               {
                   foreach (var stream in _streamCollection.Values)
                   {
                       DisableStreamFlowControl(stream);
                   }
               }

               if (IsSessionFlowControlEnabled)
               {
                   //TODO Disable session flow control
               }
           }
        }

        public bool IsSessionFlowControlEnabled
        {
            get
            {
                return _options % 2 == 0;
            }
        }
        public bool IsStreamsFlowControlledEnabled
        {
            get
            {
                return _options >> 1 % 2 == 0;
            }
        }

        public Int32 SessionInitialWindowSize { get; set; }
        public Int32 StreamsInitialWindowSize { get; set; }

        public bool IsSessionBlocked { get; set; }

        public FlowControlManager(Http2Session flowControlledSession)
        {
            SessionInitialWindowSize = Constants.DefaultFlowControlCredit;
            StreamsInitialWindowSize = Constants.DefaultFlowControlCredit;

            _flowControlledSession = flowControlledSession;
            _streamCollection = _flowControlledSession.ActiveStreams;

            Options = Constants.InitialFlowControlOptionsValue;

            IsSessionBlocked = false;
        }

        public bool IsStreamFlowControlled(Http2Stream stream)
        {
            return _streamCollection.IsStreamFlowControlled(stream);
        }

        public void NewStreamOpenedHandler(Http2Stream stream)
        {
            _flowControlledSession.SessionWindowSize += StreamsInitialWindowSize;
        }
        public void StreamClosedHandler(Http2Stream stream)
        {
            _flowControlledSession.SessionWindowSize -= stream.WindowSize;
        }

        //Flow control cant be enabled once disabled
        public void DisableStreamFlowControl(Http2Stream stream)
        {
            _streamCollection.DisableFlowControl(stream);
        }

        public void DataFrameSentHandler(object sender, DataFrameSentEventArgs args)
        {
            int id = args.Id;

            var stream = _streamCollection[id];
            if (stream.IsFlowControlEnabled == false)
            {
                return;
            }

            int dataAmount = args.DataAmount;
          
            stream.UpdateWindowSize(-dataAmount);
            _flowControlledSession.SessionWindowSize += -dataAmount;

            if (_flowControlledSession.SessionWindowSize < 0)
            {
                IsSessionBlocked = true;
                //TODO What to do next?
            }

            if (stream.WindowSize <= 0)
            {
                stream.IsFlowControlBlocked = true;
            }
        }
        public void DataFrameReceivedHandler(object sender, DataFrameReceivedEventArgs args)
        {

        }
    }
}
