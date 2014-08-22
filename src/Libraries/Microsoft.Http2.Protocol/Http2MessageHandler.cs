// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Utils;
using OpenSSL;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class defines basic http2 request/response processing logic.
    /// </summary>
    public abstract class Http2MessageHandler : IDisposable
    {
        protected Http2Session.Http2Session _session;
        protected bool _isDisposed;
        protected readonly Stream _stream;
        protected readonly CancellationToken _cancToken;
        protected readonly ConnectionEnd _end;
        protected readonly bool _isSecure;
        protected event EventHandler<SettingsSentEventArgs> OnFirstSettingsSent;
        protected bool _wereFirstSettingsSent;
        protected bool _isPushEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="Http2MessageHandler"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="end">TODO</param>
        /// <param name="isSecure"></param>
        /// <param name="cancel">The cancel.</param>
        protected Http2MessageHandler(Stream stream, ConnectionEnd end, bool isSecure, CancellationToken cancel)
        {
            _isSecure = isSecure;

            /* 14 -> 6.5.2
            This setting can be use to disable server push. An endpoint MUST NOT 
            send a PUSH_PROMISE frame if it receives this parameter set to a value of 0. */
            _isPushEnabled = true;
            _isDisposed = false;
            _cancToken = cancel;
            _stream = stream;
            _end = end;
            _wereFirstSettingsSent = false;

            _session = new Http2Session.Http2Session(_stream, _end, _isSecure, _cancToken);
        }

        /// <summary>
        /// Called when frame receives by the listener.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="FrameReceivedEventArgs"/> instance containing the event data.</param>
        protected void OnFrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var frame = args.Frame;

            switch (frame.FrameType)
            {
                case FrameType.Headers:
                    if (!ForbiddenHeaders.IsValid(stream.Headers))
                    {
                        stream.WriteRst(ResetStatusCode.ProtocolError);
                        return;
                    }
                    ProcessRequest(stream, frame);
                    break;
                case FrameType.Data:
                    ProcessIncomingData(stream, frame);
                    break;
                case FrameType.Settings:
                    ProcessSettings(frame as SettingsFrame);
                    break;
            }
        }

        protected virtual void ProcessSettings(SettingsFrame frame)
        {
            _isPushEnabled = _session.IsPushEnabled;
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="frame">The request header frame.</param>
        /// <returns></returns>
        protected abstract void ProcessRequest(Http2Stream stream, Frame frame);

        /// <summary>
        /// Processes the incoming data.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="frame"></param>
        /// <returns></returns>
        protected abstract void ProcessIncomingData(Http2Stream stream, Frame frame);

        protected Http2Stream CreateStream(int priority = Constants.DefaultStreamPriority)
        {
            return _session.CreateStream(priority);
        }

        /// <summary>
        /// Starts the session.
        /// </summary>
        /// <param name="initRequest">The initialize request params.</param>
        /// <returns></returns>
        public Task StartSessionAsync(IDictionary<string, string> initRequest = null)
        {
            int initialWindowSize = Constants.InitialFlowControlWindowSize;
            int maxStreams = Constants.DefaultMaxConcurrentStreams;

            _session.OnFrameReceived += OnFrameReceivedHandler;
            _session.OnSettingsSent += OnSettingsSentHandler;

            _session.InitialWindowSize = initialWindowSize;
            _session.OurMaxConcurrentStreams = maxStreams;

            return Task.Run(async () =>
                {
                    try
                    {
                        await _session.Start(initRequest);
                    }
                    catch (Exception ex)
                    {                        
                    }                   
                });
        }

        private void OnSettingsSentHandler(object sender, SettingsSentEventArgs e)
        {
            _wereFirstSettingsSent = true;

            if (OnFirstSettingsSent != null)
            {
                OnFirstSettingsSent(sender, e);
            }

            _session.OnSettingsSent -= OnSettingsSentHandler;
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
                return;

            if (_session != null)
            {
                _session.Dispose();
            }

            OnFirstSettingsSent = null;

            _isDisposed = true;

            Http2Logger.LogDebug("Adapter disposed");
        }
    }
}
