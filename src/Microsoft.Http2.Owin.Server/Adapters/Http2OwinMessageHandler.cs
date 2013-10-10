using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Framing;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using Org.Mentalis.Security.Ssl;

namespace Microsoft.Http2.Owin.Server.Adapters
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

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
        /// <param name="transportInfo">The transport information.</param>
        /// <param name="next">The next layer delegate.</param>
        /// <param name="cancel">The cancellation token.</param>
        public Http2OwinMessageHandler(DuplexStream stream, ConnectionEnd end, TransportInformation transportInfo,
                                AppFunc next, CancellationToken cancel)
            : base(stream, end, stream.IsSecure, transportInfo, cancel)
        {
            _next = next;
            stream.OnClose += delegate { Dispose(); };
        }

        /// <summary>
        /// Overrides request processing logic.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="frame">The request header frame.</param>
        /// <returns></returns>
        protected override void ProcessRequest(Http2Stream stream, Frame frame)
        {
            //spec 06
            //8.1.2.1.  Request Header Fields
            //HTTP/2.0 defines a number of headers starting with a ':' character
            //that carry information about the request target:

            //o  The ":method" header field includes the HTTP method ([HTTP-p2],
            //      Section 4).

            //o  The ":scheme" header field includes the scheme portion of the
            //      target URI ([RFC3986], Section 3.1).

            //o  The ":host" header field includes the authority portion of the
            //      target URI ([RFC3986], Section 3.2).

            //o  The ":path" header field includes the path and query parts of the
            //target URI (the "path-absolute" production from [RFC3986] and
            //optionally a '?' character followed by the "query" production, see
            //[RFC3986], Section 3.3 and [RFC3986], Section 3.4).  This field
            //MUST NOT be empty; URIs that do not contain a path component MUST
            //include a value of '/', unless the request is an OPTIONS request
            //for '*', in which case the ":path" header field MUST include '*'.

            //All HTTP/2.0 requests MUST include exactly one valid value for all of
            //these header fields.  An intermediary MUST ensure that requests that
            //it forwards are correct.  A server MUST treat the absence of any of
            //these header fields, presence of multiple values, or an invalid value
            //as a stream error (Section 5.4.2) of type PROTOCOL_ERROR.

            if (stream.Headers.GetValue(CommonHeaders.Method) == null
                || stream.Headers.GetValue(CommonHeaders.Path) == null
                || stream.Headers.GetValue(CommonHeaders.Scheme) == null
                || stream.Headers.GetValue(CommonHeaders.Host) == null)
            {
                stream.WriteRst(ResetStatusCode.ProtocolError);
                return;
            }

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var context = new Http2OwinMessageContext(stream, _transportInfo);
                    await _next(context.Environment);
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
        /// <param name="additionalHeaders">The additional headers.</param>
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
    }
}
