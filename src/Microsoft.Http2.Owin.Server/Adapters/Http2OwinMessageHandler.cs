using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
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
            //spec 09
            //8.1.3.1.  Request Header Fields

            //HTTP/2.0 defines a number of header fields starting with a colon ':'
            //character that carry information about the request target:

            //o  The ":method" header field includes the HTTP method ([HTTP-p2],
            //Section 4).

            //o  The ":scheme" header field includes the scheme portion of the
            //target URI ([RFC3986], Section 3.1).

            //o  The ":authority" header field includes the authority portion of
            //the target URI ([RFC3986], Section 3.2).

            //To ensure that the HTTP/1.1 request line can be reproduced
            //accurately, this header field MUST be omitted when translating
            //from an HTTP/1.1 request that has a request target in origin or
            //asterisk form (see [HTTP-p1], Section 5.3).  Clients that generate
            //HTTP/2.0 requests directly SHOULD instead omit the "Host" header
            //field.  An intermediary that converts a request to HTTP/1.1 MUST
            //create a "Host" header field if one is not present in a request by
            //copying the value of the ":authority" header field.

            //o  The ":path" header field includes the path and query parts of the
            //target URI (the "path-absolute" production from [RFC3986] and
            //optionally a '?' character followed by the "query" production, see
            //[RFC3986], Section 3.3 and [RFC3986], Section 3.4).  This field
            //MUST NOT be empty; URIs that do not contain a path component MUST
            //include a value of '/', unless the request is an OPTIONS in
            //asterisk form, in which case the ":path" header field MUST include
            //'*'.

        //All HTTP/2.0 requests MUST include exactly one valid value for all of
        //these header fields, unless this is a CONNECT request (Section 8.3).
        //An HTTP request that omits mandatory header fields is malformed
        //(Section 8.1.3.5).

        //Header field names that contain a colon are only valid in the
        //HTTP/2.0 context.  These are not HTTP header fields.  Implementations
        //MUST NOT generate header fields that start with a colon, but they
        //MUST ignore any header field that starts with a colon.  In
        //particular, header fields with names starting with a colon MUST NOT
        //be exposed as HTTP header fields.

            if (stream.Headers.GetValue(CommonHeaders.Method) == null
                || stream.Headers.GetValue(CommonHeaders.Path) == null
                || stream.Headers.GetValue(CommonHeaders.Scheme) == null
                || stream.Headers.GetValue(CommonHeaders.Authority) == null)
            {
                stream.WriteRst(ResetStatusCode.ProtocolError);
                stream.Close(ResetStatusCode.ProtocolError);
                return;
            }

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
                            //assume that we have already received endStream
                            promisedStream.HalfClosedLocal = true;
                            stream.WritePushPromise(pairs, promisedStream.Id);

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
            headers.Add(new KeyValuePair<string, string>(CommonHeaders.Status, statusCode.ToString(CultureInfo.InvariantCulture)));

            stream.WriteHeadersFrame(headers, final, true);
           
        }

        //private void PushHeaders(IDictionary<string, string[]> pairs)
        //{
            
        //}
    }
}
