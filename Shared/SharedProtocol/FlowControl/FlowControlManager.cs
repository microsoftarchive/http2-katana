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

        //TODO Avoid 2 stream collections: activeStreams and flowControlledStreams
        private List<Http2Stream> _flowControlledStreams;

        public Int32 Options { get; set; }

        public bool IsSessionFlowControlEnabled
        {
            get { return Options % 2 == 0; }
        }
        public bool IsNewStreamsFlowControlled
        {
            get
            {
                return Options >> 1 % 2 == 0;
            }
        }

        public Int32 SessionInitialWindowSize { get; set; }
        public Int32 StreamsInitialWindowSize { get; set; }

        public bool IsSessionFlowControlled { get; set; }
        public bool IsSessionBlocked { get; set; }

        public FlowControlManager(Http2Session flowControlledSession)
        {
            SessionInitialWindowSize = Constants.DefaultFlowControlCredit;
            StreamsInitialWindowSize = Constants.DefaultFlowControlCredit;

            _flowControlledSession = flowControlledSession;
            _flowControlledStreams = new List<Http2Stream>(16);

            Options = Constants.InitialFlowControlOptionsValue;

            IsSessionBlocked = false;
        }

        public bool IsStreamFlowControlled(Http2Stream stream)
        {
            return _flowControlledStreams.Contains(stream);
        }

        public void NewStreamOpenedHandler(Http2Stream stream)
        {
            if (IsNewStreamsFlowControlled)
            {
                _flowControlledStreams.Add(stream);
            }

            _flowControlledSession.SessionWindowSize += StreamsInitialWindowSize;
        }
        public void StreamClosedHandler(Http2Stream stream)
        {
            if (IsStreamFlowControlled(stream))
            {
                _flowControlledStreams.Remove(stream);
            }
            _flowControlledSession.SessionWindowSize -= stream.WindowSize;
        }

        public void EnableStreamFlowControl(Http2Stream stream)
        {
            if (IsStreamFlowControlled(stream) == false)
            {
                _flowControlledStreams.Add(stream);
            }
            stream.IsFlowControlEnabled = true;
        }

        public void DisableStreamFlowControl(Http2Stream stream)
        {
            if (IsStreamFlowControlled(stream))
            {
                _flowControlledStreams.Remove(stream);
            }
            stream.IsFlowControlEnabled = false;
        }

        public void DataFrameSentHandler(object sender, DataFrameSentEventArgs args)
        {
            int id = args.Id;

            var stream = _flowControlledSession.ActiveStreams[id];
            if (_flowControlledStreams.Contains(stream) == false)
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
