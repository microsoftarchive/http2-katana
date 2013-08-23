using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin.Types;

namespace ServerOwinMiddleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using HandshakeAction = Func<IDictionary<string, object>>;
    // Http-01/2.0 uses a similar upgrade handshake to WebSockets. This middleware answers upgrade requests
    // using the Opaque Upgrade OWIN extension and then switches the pipeline to HTTP/2.0 binary framing.
    // Interestingly the HTTP/2.0 handshake does not need to be the first HTTP/1.1 request on a connection, only the last.
    public class Http2Middleware
    {
        // Pass requests onto this pipeline if not upgrading to HTTP/2.0.
        private readonly AppFunc _next;
        // Pass requests onto this pipeline if upgraded to HTTP/2.0.
        private AppFunc _nextHttp2;

        public Http2Middleware(AppFunc next)
        {
            _next = next;
            _nextHttp2 = _next;
        }

        public Http2Middleware(AppFunc next, AppFunc branch)
        {
            _next = next;
            _nextHttp2 = branch;
        }

        /// <summary>
        /// Invokes the specified environment.
        /// This method is used for handshake.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            var req = new OwinRequest(environment);

            if (req.UpgradeDelegate != null)
            {
                //_next is next layer call (Application?) it fill owinResponce
                //need to think about which environment should be passed
                req.UpgradeDelegate.Invoke(environment, _next);
                return;
            }

            //If we dont have upgrade delegate then pass request to the next layer
            await _next(environment);
        }
    }
}
