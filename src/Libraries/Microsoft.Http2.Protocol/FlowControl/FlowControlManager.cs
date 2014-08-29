// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol.FlowControl
{
    /// <summary>
    /// 14 -> 5.2  Flow Control
    /// 14 -> 6.9  WINDOW_UPDATE
    /// </summary>
    internal class FlowControlManager
    {
        private readonly Http2Session.Http2Session _session;
        private StreamDictionary _streamDictionary;
        private Int32 _options;
        private bool _wasFlowControlSet;

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
                if (_wasFlowControlSet)
                    throw new ProtocolError(ResetStatusCode.FlowControlError, "Trying to reenable flow control");

                _wasFlowControlSet = true;
                _options = value;

                if (!IsFlowControlEnabled)
                {
                    foreach (var stream in _streamDictionary.Values)
                    {
                        DisableStreamFlowControl(stream);
                    }
                }

                if (IsFlowControlEnabled)
                {
                    //TODO Disable session flow control
                }
            }
        }

        public bool IsFlowControlEnabled
        {
            get
            {
                return _options % 2 == 0;
            }
        }

        public Int32 SessionInitialWindowSize { get; set; }
        public Int32 StreamInitialWindowSize { get; set; }
        public bool IsSessionBlocked { get; set; }

        public FlowControlManager(Http2Session.Http2Session session)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            /* 14 -> 6.9.2
            When a HTTP/2.0 connection is first established, new streams are
            created with an initial flow control window size of 65535 bytes. 
            The connection flow control window is 65535 bytes. */
            SessionInitialWindowSize = Constants.InitialFlowControlWindowSize;
            StreamInitialWindowSize = Constants.InitialFlowControlWindowSize;

            _session = session;
            _streamDictionary = _session.StreamDictionary;

            Options = Constants.InitialFlowControlOptionsValue;
            _wasFlowControlSet = false;
            IsSessionBlocked = false;
        }

        public void SetStreamDictionary(StreamDictionary streams)
        {
            _streamDictionary = streams;
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
            if (stream == null)
                throw new ArgumentNullException("stream is null");

            return _streamDictionary.IsStreamFlowControlled(stream);
        }

        public void NewStreamOpenedHandler(Http2Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream is null");

            _session.SessionWindowSize += StreamInitialWindowSize;
        }

        public void StreamClosedHandler(Http2Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream is null");

            _session.SessionWindowSize -= stream.WindowSize;
        }

        /// <summary>
        /// Disables the stream flow control.
        /// Flow control cant be enabled once disabled
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void DisableStreamFlowControl(Http2Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream is null");

            _streamDictionary.DisableFlowControl(stream);
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
            if (!stream.IsFlowControlEnabled)
            {
                return;
            }

            int dataAmount = args.DataAmount;

            stream.UpdateWindowSize(-dataAmount);
            _session.SessionWindowSize += -dataAmount;

            if (_session.SessionWindowSize <= 0)
            {
                IsSessionBlocked = true;
                //TODO What to do next?
            }

            /* 14 -> 6.9.1
            The sender MUST NOT
            send a flow controlled frame with a length that exceeds the space
            available in either of the flow control windows advertised by the
            receiver. */
            if (stream.WindowSize <= 0)
            {
                stream.IsBlocked = true;
                Http2Logger.LogDebug("Flow control for stream id={0} blocked", id);
            }
        }
    }
}
