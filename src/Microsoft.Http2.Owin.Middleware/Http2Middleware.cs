using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using ProtocolAdapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;

namespace ServerOwinMiddleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;
    // Http-01/2.0 uses a similar upgrade handshake to WebSockets. This middleware answers upgrade requests
    // using the Opaque Upgrade OWIN extension and then switches the pipeline to HTTP/2.0 binary framing.
    // Interestingly the HTTP/2.0 handshake does not need to be the first HTTP/1.1 request on a connection, only the last.
    public class Http2Middleware
    {
        // Pass requests onto this pipeline if not upgrading to HTTP/2.0.
        private readonly AppFunc _next;

        public Http2Middleware(AppFunc next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the specified environment.
        /// This method is used for handshake.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            var request = new OwinRequest(environment);
            
            //After upgrade happened upgrade delegate should be null for next requests in a single connection
            if (environment.ContainsKey("opaque.Upgrade") && environment["opaque.Upgrade"] is UpgradeDelegate)
            {
                var upgradeDelegate = environment["opaque.Upgrade"] as UpgradeDelegate;
                //Should open session here
                upgradeDelegate.Invoke(environment, opaque =>
                    {
                        //Copy Dictionary
                        //use the same stream which was used during upgrade

                        var opaqueStream = opaque["opaque.Stream"] as DuplexStream;
                        var trInfo = CreateTransportInfo(request);

                        //Provide cancellation token here
                        var http2Adapter = new Http2OwinAdapter(opaqueStream, trInfo, _next, CancellationToken.None);

                        return http2Adapter.StartSession(GetInitialRequestParams(opaque));
                    });

                environment["opaque.Upgrade"] = null;
                return;
            }

            //If we dont have upgrade delegate then pass request to the next layer
            await _next(environment);
        }

        private IDictionary<string, string> GetInitialRequestParams(IDictionary<string, object> environment)
        {
            var request = new OwinRequest(environment);

            var path = !String.IsNullOrEmpty(request.Path)
                            ? request.Path
                            : "/index.html";
            var method =  !String.IsNullOrEmpty(request.Method)
                            ? request.Method
                            : "get";

            return new Dictionary<string, string>
                {
                    //Add more headers
                    {":path", path},
                    {":method", method}
                };
        }
        

        private TransportInformation CreateTransportInfo(OwinRequest owinRequest)
        {
            return new TransportInformation
            {
                RemoteIpAddress = owinRequest.RemoteIpAddress,
                RemotePort = owinRequest.RemotePort != null ? (int) owinRequest.RemotePort : 8080,
                LocalIpAddress = owinRequest.LocalIpAddress,
                LocalPort = owinRequest.LocalPort != null ? (int) owinRequest.LocalPort : 8080,
            };
        }
    }
}
