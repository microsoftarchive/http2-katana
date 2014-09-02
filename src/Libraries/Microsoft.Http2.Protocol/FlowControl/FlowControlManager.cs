// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol.FlowControl
{
    /// <summary>
    /// 14 -> 5.2  Flow Control
    /// 14 -> 6.9  WINDOW_UPDATE
    /// </summary>
    internal class FlowControlManager
    {
        private StreamDictionary _streamDictionary;

        public Int32 InitialWindowSize { get; set; }
        public Int32 ConnectionWindowSize { get; set; }
        public bool IsSessionBlocked { get; set; }

        public FlowControlManager(StreamDictionary streamDictionary)
        {
            if (streamDictionary == null)
                throw new ArgumentNullException("streamDictionary");

            _streamDictionary = streamDictionary;
            IsSessionBlocked = false;

            /* 14 -> 6.9.2
            When a HTTP/2.0 connection is first established, new streams are
            created with an initial flow control window size of 65535 bytes. 
            The connection flow control window is 65535 bytes. */
            InitialWindowSize = Constants.InitialWindowSize;

            /* 14 ->  6.9.2 
            The connection flow control window is 65,535 bytes. */
            ConnectionWindowSize = Constants.InitialWindowSize;
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
            if (!_streamDictionary.ContainsKey(id))
            {
                return;
            }

            var stream = _streamDictionary[id];
            int dataAmount = args.DataAmount;

            stream.UpdateWindowSize(-dataAmount);
            ConnectionWindowSize += -dataAmount;

            /* 14 -> 6.9.1
            The sender MUST NOT
            send a flow controlled frame with a length that exceeds the space
            available in either of the flow control windows advertised by the
            receiver. */
            if (stream.WindowSize <= 0)
            {
                stream.IsBlocked = true;
                Http2Logger.Debug("Flow control for stream id={0} blocked", id);
            }
            if (stream.WindowSize > 0 && stream.IsBlocked)
            {
                stream.IsBlocked = false;
                Http2Logger.Debug("Flow control for stream id={0} unblocked", id);
            }
            if (ConnectionWindowSize < 0)
            {
                IsSessionBlocked = true;
                Http2Logger.Debug("Flow control for connection blocked");
            }
        }
    }
}
