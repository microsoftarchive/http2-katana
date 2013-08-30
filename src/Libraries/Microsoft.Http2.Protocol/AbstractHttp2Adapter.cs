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
    //TODO Remove Owin.Types dependency
    public abstract class AbstractHttp2Adapter : IDisposable
    {
        protected Http2Session _session;
        protected bool _isDisposed;
        protected readonly DuplexStream _stream;
        protected readonly CancellationToken _cancToken;
        protected readonly TransportInformation _transportInfo;

        protected AbstractHttp2Adapter(DuplexStream stream, TransportInformation transportInfo, CancellationToken cancel)
        {
            _transportInfo = transportInfo;
            _isDisposed = false;
            _cancToken = cancel;
            _stream = stream;
        }

        protected void OnFrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var frame = args.Frame;

            switch (frame.FrameType)
            {
                case FrameType.Headers:
                    ProcessRequest(stream);
                    break;
                case FrameType.Data:
                    ProcessIncomingData(stream);
                    break;
            }
        }

        protected abstract Task ProcessRequest(Http2Stream stream);

        protected abstract Task ProcessIncomingData(Http2Stream stream);

        public Task StartSession(IDictionary<string, string> initRequest = null)
        {
            //TODO provide cancellation token and transport info
            _session = new Http2Session(_stream, ConnectionEnd.Server, true, true, _cancToken);
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
