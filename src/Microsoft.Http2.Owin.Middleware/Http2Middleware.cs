using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Org.Mentalis.Security.Ssl;

namespace Microsoft.Http2.Owin.Middleware
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
        /// Inspect if request if http2 upgrade
        /// If so starts http2 session via provider.
        /// Calls next layer if not.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            var context = new OwinContext(environment);

            if (IsOpaqueUpgradePossible(context.Request) && IsRequestForHttp2Upgrade(context.Request))
            {
                var upgradeDelegate = environment["opaque.Upgrade"] as UpgradeDelegate;
                Debug.Assert(upgradeDelegate != null, "upgradeDelegate is not null");

                var trInfo = CreateTransportInfo(context.Request);

                // save original request parameters; used to complete request after upaque upgrade is done
                var requestCopy = GetInitialRequestParams(context.Request);

                upgradeDelegate.Invoke(new Dictionary<string, object>(), opaque =>
                    {
                        //use the same stream which was used during upgrade
                        var opaqueStream = opaque["opaque.Stream"] as DuplexStream;

                        //TODO Provide cancellation token here
                        // Move to method
                        return new Task(async () =>
                            {
                                try
                                {
                                    using (var http2MessageHandler = new Http2OwinMessageHandler(opaqueStream,
                                                                                                 ConnectionEnd.Server,
                                                                                                 trInfo, _next,
                                                                                                 CancellationToken.None)
                                        )
                                    {
                                        await http2MessageHandler.StartSessionAsync(requestCopy);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Http2Logger.LogError(ex.Message);
                                }
                            });
                    });

                // specify Upgrade protocol
                context.Response.Headers.Add("Upgrade", new[] { Protocols.Http2 });
                return;
            }

            //If we dont have upgrade delegate then pass request to the next layer
            await _next(environment);
        }

        private static bool IsRequestForHttp2Upgrade(IOwinRequest request)
        {
            var headers = request.Headers as IDictionary<string, string[]>;
            return  headers.ContainsKey("Connection")
                    && headers.ContainsKey("HTTP2-Settings")
                    && headers.ContainsKey("Upgrade") 
                    && headers["Upgrade"].FirstOrDefault(it =>
                                         it.ToUpper().IndexOf("HTTP", StringComparison.Ordinal) != -1 &&
                                         it.IndexOf("2.0", StringComparison.Ordinal) != -1) != null;
        }

        private static bool IsOpaqueUpgradePossible(IOwinRequest request)
        {
            var environment = request.Environment;

            return environment.ContainsKey("opaque.Upgrade")
                   && environment["opaque.Upgrade"] is UpgradeDelegate;
        }

        private IDictionary<string, string> GetInitialRequestParams(IOwinRequest request)
        {
            var defaultWindowSize = 200000.ToString(CultureInfo.InvariantCulture);
            var defaultMaxStreams = 100.ToString(CultureInfo.InvariantCulture);

            bool areSettingsOk = true;

            var path = !String.IsNullOrEmpty(request.Path)
                            ? request.Path
                            : "/index.html";
            var method = !String.IsNullOrEmpty(request.Method)
                            ? request.Method
                            : "get";

            var scheme = !String.IsNullOrEmpty(request.Scheme)
                            ? request.Scheme
                            : "http";

            var splittedSettings = new string[0];
            try
            {
                var settingsBytes = Convert.FromBase64String(request.Headers["Http2-Settings"]);
                var http2Settings = Encoding.UTF8.GetString(settingsBytes);
                if (http2Settings.IndexOf(',') != -1)
                {
                    splittedSettings = http2Settings.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    areSettingsOk = false;
                }

                if (splittedSettings.Length < 2)
                {
                    areSettingsOk = false;
                }
            }
            catch (Exception)
            {
                areSettingsOk = false;
            }

            var windowSize = areSettingsOk ? splittedSettings[0].Trim() : defaultWindowSize;
            var maxStreams = areSettingsOk ? splittedSettings[1].Trim() : defaultMaxStreams;

            return new Dictionary<string, string>
                {
                    //Add more headers
                    {":path", path},
                    {":method", method},
                    {":initial_window_size", windowSize},
                    {":max_concurrent_streams", maxStreams},
                    {":scheme", scheme}
                };
        }
        

        private TransportInformation CreateTransportInfo(IOwinRequest owinRequest)
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
