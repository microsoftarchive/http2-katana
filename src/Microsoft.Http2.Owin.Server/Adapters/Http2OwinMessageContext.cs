using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Owin;

namespace Microsoft.Http2.Owin.Server.Adapters
{
    // Tracks per-request state.
    internal class Http2OwinMessageContext
    {
        private readonly Http2Stream _protocolStream;
        private IOwinContext _owinContext;
        private IHeaderDictionary _responseHeaders;
        private bool _responseStarted;

        internal Http2OwinMessageContext(Http2Stream protocolStream)
        {
            _protocolStream = protocolStream;
            PopulateEnvironment();
        }

        internal IOwinContext OwinContext
        {
            get { return _owinContext; }
        }

        /// <summary>
        /// Adopts protocol terms into owin environment.
        /// </summary>
        private void PopulateEnvironment()
        {
            var headers = _protocolStream.Headers;
            _owinContext = new OwinContext();
            _responseHeaders = _owinContext.Response.Headers;
            var headersAsDict = new Dictionary<string, string[]>();

            //Handles multiple headers entries
            foreach (var kv in headers)
            {
                foreach (var toAddKv in headersAsDict)
                {
                    if (headersAsDict.ContainsKey(toAddKv.Key))
                    {
                        var value = new List<string>(headersAsDict[toAddKv.Key]);
                        value.AddRange(toAddKv.Value);
                        headersAsDict[toAddKv.Key] = value.ToArray();
                    }
                    else
                    {
                        headersAsDict.Add(kv.Key, new[] {kv.Value});
                    }
                } 
            }

            _owinContext.Environment[CommonOwinKeys.RequestHeaders] = headersAsDict;

            var owinRequest = _owinContext.Request;
            var owinResponse = _owinContext.Response;

            owinRequest.Method = headers.GetValue(PseudoHeaders.Method);

            var path = headers.GetValue(PseudoHeaders.Path);
            owinRequest.Path = path.StartsWith(@"/") ? new PathString(path) : new PathString(@"/" + path);

            owinRequest.CallCancelled = CancellationToken.None;

            owinRequest.Host = new HostString(headers.GetValue(PseudoHeaders.Authority));
            owinRequest.PathBase = PathString.Empty;
            owinRequest.QueryString = QueryString.Empty;
            owinRequest.Body = Stream.Null;
            owinRequest.Protocol = Protocols.Http2;
            owinRequest.Scheme = headers.GetValue(PseudoHeaders.Scheme) == Uri.UriSchemeHttp ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;

            owinResponse.Body = new ResponseStream(_protocolStream, StartResponse);
        }

        private void StartResponse()
        {
            Debug.Assert(!_responseStarted, "Response started more than once");
            _responseStarted = true;

            SendHeaders(final: false);
        }

        // If no response headers have been sent yet, send them.
        internal void FinishResponse()
        {
            if (!_responseStarted)
            {
                SendHeaders(final: true);
            }
            else
            {
                // End the data stream.
                _protocolStream.WriteDataFrame(new ArraySegment<byte>(new byte[0]), isEndStream: true);
            }
        }

        /// <summary>
        /// Writes the status header to the stream.
        /// </summary>
        /// <param name="final">if set to <c>true</c> then marks headers frame as final.</param>
        private void SendHeaders(bool final)
        {
            var responseHeaders = new HeadersList(_responseHeaders);

            /* 14 -> 8.1.2.4
            A single ":status" header field is defined that carries the HTTP
            status code field. This header field MUST be included in all responses. */
            var status = new KeyValuePair<string, string>(PseudoHeaders.Status,
                                                          _owinContext.Response.StatusCode.ToString());

            /* 14 -> 8.1.2.1
            All pseudo-header fields MUST appear in the header block before
            regular header fields. */
            responseHeaders.Insert(0, status);

            _protocolStream.WriteHeadersFrame(responseHeaders, final, true);
        }
    }
}
