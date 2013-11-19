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
        protected Http2Session _session;
        protected bool _isDisposed;
        protected readonly Stream _stream;
        protected readonly CancellationToken _cancToken;
        protected readonly ConnectionEnd _end;
        protected readonly bool _isSecure;
        protected event EventHandler<SettingsSentEventArgs> OnFirstSettingsSent;
        protected bool _wereFirstSettingsSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Http2MessageHandler"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="end">TODO</param>
        /// <param name="isSecure"></param>
        /// <param name="transportInfo">The transport information.</param>
        /// <param name="cancel">The cancel.</param>
        protected Http2MessageHandler(Stream stream, ConnectionEnd end, bool isSecure, CancellationToken cancel)
        {
            _isSecure = isSecure;
            _isDisposed = false;
            _cancToken = cancel;
            _stream = stream;
            _end = end;
            _wereFirstSettingsSent = false;

            _session = new Http2Session(_stream, _end, true, true, _isSecure, _cancToken);
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
                    ProcessRequest(stream, frame);
                    break;
                case FrameType.Data:
                    ProcessIncomingData(stream, frame);
                    break;
            }
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
        /// <returns></returns>
        protected abstract void ProcessIncomingData(Http2Stream stream, Frame frame);

        /// <summary>
        /// Starts the session.
        /// </summary>
        /// <param name="end">The connection end.</param>
        /// <param name="initRequest">The initialize request params.</param>
        /// <returns></returns>
        public Task StartSessionAsync(IDictionary<string, string> initRequest = null)
        {
            int initialWindowSize = 200000;
            int maxStreams = 100;

            if (initRequest != null && initRequest.ContainsKey(CommonHeaders.InitialWindowSize))
            {
                initialWindowSize = int.Parse(initRequest[CommonHeaders.InitialWindowSize]);
            }

            if (initRequest != null && initRequest.ContainsKey(CommonHeaders.MaxConcurrentStreams))
            {
                maxStreams = int.Parse(initRequest[CommonHeaders.MaxConcurrentStreams]);
            }

            _session.OnFrameReceived += OnFrameReceivedHandler;
            _session.OnSettingsSent += OnSettingsSentHandler;

            _session.InitialWindowSize = initialWindowSize;
            _session.OurMaxConcurrentStreams = maxStreams;

            return Task.Run(async () => await _session.Start(initRequest));
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
