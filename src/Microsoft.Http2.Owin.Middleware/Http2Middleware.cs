using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Owin.Types;
using ProtocolAdapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;

namespace ServerOwinMiddleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
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
            if (request.UpgradeDelegate != null)
            {
                //Should open session here
                request.UpgradeDelegate.Invoke(environment, opaque =>
                    {
                        //Copy Dictionary
                        var envCopy = CopyEnvironment(opaque);

                        //use the same stream which was used during upgrade
                        var opaqueStream = opaque[OwinConstants.Opaque.Stream] as DuplexStream;
                        var trInfo = CreateTransportInfo(request);

                        //Provide cancellation token here
                        var http2Adapter = new Http2OwinAdapter(opaqueStream, trInfo, _next, CancellationToken.None);

                        return http2Adapter.StartSession(GetInitialRequestParams(envCopy));
                    });

                environment[OwinConstants.Opaque.Upgrade] = null;
                return;
            }

            //If we dont have upgrade delegate then pass request to the next layer
            await _next(environment);
        }

        private IDictionary<string, object> CopyEnvironment(IDictionary<string, object> original)
        {
            //May add other headers
            return new Dictionary<string, object>(original);
        }

        private IDictionary<string, string> GetInitialRequestParams(IDictionary<string, object> environment)
        {
            var path =  environment.ContainsKey(OwinConstants.RequestPath)
                            ? (string) environment[OwinConstants.RequestPath]
                            : "/index.html";
            var method =  environment.ContainsKey(OwinConstants.RequestMethod)
                            ? (string) environment[OwinConstants.RequestMethod]
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
                RemotePort = owinRequest.RemotePort,
                LocalIpAddress = owinRequest.LocalIpAddress,
                LocalPort = owinRequest.LocalPort,
            };
        }
    }
}
