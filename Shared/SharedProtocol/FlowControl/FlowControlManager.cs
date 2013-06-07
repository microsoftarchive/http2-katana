using System;

namespace SharedProtocol
{
    /// <summary>
    /// This class is designed for flow control monitoring and processing.
    /// Flow control handles only dataframes.
    /// </summary>
    public class FlowControlManager
    {
        private readonly Http2Session _flowControlledSession;
        private readonly ActiveStreams _streamCollection;
        private Int32 _options;

        /// <summary>
        /// Gets or sets the flow control options property.
        /// </summary>
        /// <value>
        /// The options. 
        /// The first bit indicated all streams flow control enabled.
        /// The second bit indicated session flow control enabled.
        /// </value>
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

        /// <summary>
        /// Check if stream is flowcontrolled.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        /// <returns>
        ///   <c>true</c> if the stream is flow controlled; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Disables the stream flow control.
        /// Flow control cant be enabled once disabled
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void DisableStreamFlowControl(Http2Stream stream)
        {
            _streamCollection.DisableFlowControl(stream);
        }

        /// <summary>
        /// Handles data frame sent event.
        /// This method can set flow control block to stream exceeded window size limit.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DataFrameSentEventArgs"/> instance containing the event data.</param>
        public void DataFrameSentHandler(object sender, DataFrameSentEventArgs args)
        {
            int id = args.Id;

            //Stream was closed after a data final frame.
            if (_streamCollection.ContainsKey(id) == false)
            {
                return;
            }

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
    }
}
