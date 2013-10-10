using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Http2.Owin.Server.Adapters
{
    // Tracks per-request state.
    internal class Http2OwinMessageContext
    {
        private Http2Stream _protocolStream;
        private IOwinContext _owinContext;
        private IHeaderDictionary _responseHeaders;
        private TransportInformation _transportInfo;
        private bool _responseStarted = false;

        internal Http2OwinMessageContext(Http2Stream protocolStream, TransportInformation transportInfo)
        {
            _protocolStream = protocolStream;
            _transportInfo = transportInfo;
            PopulateEnvironment();
        }

        internal IDictionary<string, object> Environment
        {
            get { return _owinContext.Environment; }
        }

        /// <summary>
        /// Adopts protocol terms into owin environment.
        /// </summary>
        private void PopulateEnvironment()
        {
            HeadersList headers = _protocolStream.Headers;
            _owinContext = new OwinContext();
            _responseHeaders = _owinContext.Response.Headers;

            var headersAsDict = headers.ToDictionary(header => header.Key, header => new[] { header.Value }, StringComparer.OrdinalIgnoreCase);
            _owinContext.Environment[CommonOwinKeys.RequestHeaders] = headersAsDict;

            var owinRequest = _owinContext.Request;
            var owinResponse = _owinContext.Response;

            owinRequest.Method = headers.GetValue(CommonHeaders.Method);
            owinRequest.Path = headers.GetValue(CommonHeaders.Path);
            owinRequest.CallCancelled = CancellationToken.None;

            owinRequest.Host = headers.GetValue(CommonHeaders.Host);
            owinRequest.PathBase = String.Empty;
            owinRequest.QueryString = String.Empty;
            owinRequest.Body = Stream.Null;
            owinRequest.Protocol = Protocols.Http2;
            owinRequest.Scheme = headers.GetValue(CommonHeaders.Scheme) == Uri.UriSchemeHttp ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
            owinRequest.RemoteIpAddress = _transportInfo.RemoteIpAddress;
            owinRequest.RemotePort = Convert.ToInt32(_transportInfo.RemotePort);
            owinRequest.LocalIpAddress = _transportInfo.LocalIpAddress;
            owinRequest.LocalPort = _transportInfo.LocalPort;

            owinResponse.Body = new ResponseStream(_protocolStream, StartResponse);
        }

        private void StartResponse()
        {
            Debug.Assert(!_responseStarted, "Response started more than once");
            _responseStarted = true;
            IOwinResponse response = _owinContext.Response;
            Http2Logger.LogDebug("Transfer begin");

            SendHeaders(final: false);
        }

        // If no response headers have been sent yet, send them.
        internal void FinishResponse()
        {
            if (!_responseStarted)
            {
                Http2Logger.LogDebug("Transfer begin");

                SendHeaders(final: true);

                Http2Logger.LogDebug("Transfer end");
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
        /// <param name="stream">The stream.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="final">if set to <c>true</c> then marks headers frame as final.</param>
        /// <param name="additionalHeaders">The additional headers.</param>
        private void SendHeaders(bool final)
        {
            HeadersList responseHeaders = new HeadersList(_responseHeaders);
            responseHeaders.Add(new KeyValuePair<string, string>(CommonHeaders.Status, _owinContext.Response.StatusCode.ToString()));
            _protocolStream.WriteHeadersFrame(responseHeaders, final, true);
        }
    }
}
