using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;
using OpenSSL;
using Owin;

namespace Microsoft.Http2.Owin.Server.Adapters
{
    using AppFunc = Func<IOwinContext, Task>;
    using PushFunc = Action<IDictionary<string, string[]> >;

    /// <summary>
    /// This class overrides http2 request/response processing logic as owin requires
    /// </summary>
    public class Http2OwinMessageHandler : Http2MessageHandler
    {
        private readonly AppFunc _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="Http2OwinMessageHandler"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="end"></param>
        /// <param name="isSecure"></param>
        /// <param name="next">The next layer delegate.</param>
        /// <param name="cancel">The cancellation token.</param>
        public Http2OwinMessageHandler(Stream stream, ConnectionEnd end, bool isSecure,
                                AppFunc next, CancellationToken cancel)
            : base(stream, end, isSecure, cancel)
        {
            _next = next;
            _session.OnSessionDisposed += delegate { Dispose(); };
        }

        /// <summary>
        /// Overrides request processing logic.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="frame">The request header frame.</param>
        /// <returns></returns>
        protected override void ProcessRequest(Http2Stream stream, Frame frame)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var context = new Http2OwinMessageContext(stream);
                    var contextEnv = context.OwinContext.Environment;

                    PushFunc pushDelegate = null;
                    pushDelegate = async pairs =>
                        {
                            var promisedStream = CreateStream();

                            // We need check stream, as it used outside of session.
                            if (promisedStream == null)
                            {
                                return;
                            }

                            promisedStream.WritePushPromise(pairs, stream.Id);

                            var headers = new HeadersList(pairs);
                            promisedStream.Headers.AddRange(headers);

                            var http2MsgCtx = new Http2OwinMessageContext(promisedStream);
                            var http2PushCtx = http2MsgCtx.OwinContext;

                            http2PushCtx.Set(CommonOwinKeys.ServerPushFunc, pushDelegate);

                            //pass add info from parent to child context. This info can store 
                            //reference table for example or something els that should be passed from
                            //client request into child push requests.
                            if (contextEnv.ContainsKey(CommonOwinKeys.AdditionalInfo))
                                http2PushCtx.Set(CommonOwinKeys.AdditionalInfo, contextEnv[CommonOwinKeys.AdditionalInfo]);

                            await _next(http2PushCtx);

                            http2MsgCtx.FinishResponse();
                        };
                    
                    context.OwinContext.Set(CommonOwinKeys.ServerPushFunc, pushDelegate);
                    context.OwinContext.Set(CommonOwinKeys.EnableServerPush, _isPushEnabled);

                    await _next(context.OwinContext);
                    context.FinishResponse();
                }
                catch (Exception ex)
                {   
                    EndResponse(stream, ex);
                }
            });
            
        }

        /// <summary>
        /// Overrides data processing logic.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="frame"></param>
        /// <returns></returns>
        protected override void ProcessIncomingData(Http2Stream stream, Frame frame)
        {
            //Do nothing... handling data is not supported by the server yet
        }

        /// <summary>
        /// Ends the response in case of error.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="ex">The caught exception.</param>
        private void EndResponse(Http2Stream stream, Exception ex)
        {
            Http2Logger.LogDebug("Error processing request:\r\n" + ex);
            // TODO: What if the response has already started?
            WriteStatus(stream, StatusCode.Code500InternalServerError, true);
        }

        /// <summary>
        /// Writes the status header to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="final">if set to <c>true</c> then marks headers frame as final.</param>
        /// <param name="headers">Additional headers</param>
        private void WriteStatus(Http2Stream stream, int statusCode, bool final, HeadersList headers = null)
        {
            if (headers == null)
                {
                headers = new HeadersList();
            }
            headers.Add(new KeyValuePair<string, string>(PseudoHeaders.Status, statusCode.ToString(CultureInfo.InvariantCulture)));

            stream.WriteHeadersFrame(headers, final, true);
           
        }
    }
}
