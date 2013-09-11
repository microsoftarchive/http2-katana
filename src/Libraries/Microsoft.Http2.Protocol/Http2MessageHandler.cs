using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.EventArgs;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;

namespace Microsoft.Http2.Protocol
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// This class defines basic http2 request/response processing logic.
    /// </summary>
    public abstract class Http2MessageHandler : IDisposable
    {
        protected Http2Session _session;
        protected bool _isDisposed;
        protected readonly DuplexStream _stream;
        protected readonly CancellationToken _cancToken;
        protected readonly TransportInformation _transportInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="Http2MessageHandler"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="transportInfo">The transport information.</param>
        /// <param name="cancel">The cancel.</param>
        protected Http2MessageHandler(DuplexStream stream, TransportInformation transportInfo, CancellationToken cancel)
        {
            _transportInfo = transportInfo;
            _isDisposed = false;
            _cancToken = cancel;
            _stream = stream;
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
                    ProcessIncomingData(stream);
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
        protected abstract void ProcessIncomingData(Http2Stream stream);

        /// <summary>
        /// Starts the session.
        /// </summary>
        /// <param name="end">The connection end.</param>
        /// <param name="initRequest">The initialize request params.</param>
        /// <returns></returns>
        public Task StartSession(ConnectionEnd end, IDictionary<string, string> initRequest = null)
        {
            int initialWindowSize = 200000;
            int maxStreams = 100;

            if (initRequest != null && initRequest.ContainsKey(":initial_window_size"))
            {
                initialWindowSize = int.Parse(initRequest[":initial_window_size"]);
            }

            if (initRequest != null && initRequest.ContainsKey(":max_concurrent_streams"))
            { 
                maxStreams = int.Parse(initRequest[":max_concurrent_streams"]);
            }

            //TODO provide cancellation token and transport info
            _session = new Http2Session(_stream, end, true, true, _cancToken, initialWindowSize, maxStreams);
            _session.OnFrameReceived += OnFrameReceivedHandler;

            return _session.Start(initRequest);
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
                return;

            if (_session != null)
            {
                _session.Dispose();
            }

            _isDisposed = true;
        }
    }
}
