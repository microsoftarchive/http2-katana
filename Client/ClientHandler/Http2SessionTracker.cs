using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientHandler
{
    // TODO: Have this layer sort requests based on scheme, host, and port.
    public class Http2SessionTracker : HttpMessageHandler
    {
        private HttpMessageInvoker _fixedInvoker;
        private Http2SessionHandler _fixedHandler;
        private HttpMessageHandler _fallbackhandler;

        public Http2SessionTracker(bool do11Handshake, HttpMessageHandler fallbackHandler)
        {
            _fallbackhandler = fallbackHandler;
            _fixedHandler = new Http2SessionHandler(do11Handshake, _fallbackhandler);
            _fixedInvoker = new HttpMessageInvoker(_fixedHandler);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Stubbed to always delegate to a single connection.
            // The first request will determine the host.
            return _fixedInvoker.SendAsync(request, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            _fallbackhandler.Dispose();
            _fixedInvoker.Dispose();
            base.Dispose(disposing);
        }
    }
}
