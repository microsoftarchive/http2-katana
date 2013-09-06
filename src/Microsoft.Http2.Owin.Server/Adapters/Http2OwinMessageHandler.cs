using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;

namespace Microsoft.Http2.Owin.Server.Adapters
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Http2OwinAdapter : Http2MessageHandler
    {
        private readonly AppFunc _next;

        public Http2OwinAdapter(DuplexStream stream, TransportInformation transportInfo,
                                AppFunc next, CancellationToken cancel)
            : base(stream, transportInfo, cancel)
        {
            _next = next;
        }

        private IDictionary<string, object> PopulateEnvironment(HeadersList headers)
        {
            var environment = new Dictionary<string, object>();

            var headersAsDict = headers.ToDictionary(header => header.Key, header => new[] {header.Value});

            environment["owin.RequestHeaders"] = headersAsDict;
            environment["owin.ResponseHeaders"] = new Dictionary<string, string[]>();

            var owinRequest = new OwinRequest(environment);
            var owinResponse = new OwinResponse(environment);

            owinRequest.Method = headers.GetValue(":method");
            owinRequest.Path = headers.GetValue(":path");
            owinRequest.CallCancelled = CancellationToken.None;

            owinRequest.PathBase = String.Empty;
            owinRequest.QueryString = String.Empty;
            owinRequest.Body = new MemoryStream();
            owinRequest.Protocol = Protocols.Http1;
            owinRequest.Scheme = headers.GetValue(":scheme") == Uri.UriSchemeHttp ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
            owinRequest.RemoteIpAddress = _transportInfo.RemoteIpAddress;
            owinRequest.RemotePort = Convert.ToInt32(_transportInfo.RemotePort);
            owinRequest.LocalIpAddress = _transportInfo.LocalIpAddress;
            owinRequest.LocalPort = _transportInfo.LocalPort;

            owinResponse.Body = new MemoryStream();

            return environment;
        }

        protected override async Task ProcessRequest(Http2Stream stream)
        {
            var env = PopulateEnvironment(stream.Headers);

            try
            {
                await _next(env);
            }
            catch (Exception ex)
            {   
                EndResponse(stream, ex);
                return;
            }

            await EndResponse(stream, env);
        }

        private void EndResponse(Http2Stream stream, Exception ex)
        {
            WriteStatus(stream, StatusCode.Code500InternalServerError, false);
        }

        protected override async Task ProcessIncomingData(Http2Stream stream)
        {
            //Do nothing... handling data is not supported by the server yet
        }

        private async Task EndResponse(Http2Stream stream, IDictionary<string, object> environment)
        {
            Stream responseBody = null;
            IDictionary<string, string[]> owinResponseHeaders = null;
            HeadersList responseHeaders = null;

            var response = new OwinResponse(environment);

            if (response.Body != null)
                responseBody = response.Body;

            if (response.Headers != null)
                owinResponseHeaders = response.Headers;

            var responseStatusCode = response.StatusCode;

            if (owinResponseHeaders != null)
                responseHeaders = new HeadersList(owinResponseHeaders);

            WriteStatus(stream, responseStatusCode, responseBody == null, responseHeaders);

            //Memory stream contains all response data and can be read by one iteration.
            if (responseBody != null && responseBody.Position != 0)
            {
                Http2Logger.LogDebug("Transfer begin");
                int contentLen = int.Parse(response.Headers["Content-Length"]);

                Debug.Assert(contentLen == responseBody.Length);
                //If stream is empty then do not send anything
                var responseDataBuffer = new byte[responseBody.Position];
                responseBody.Seek(0, SeekOrigin.Begin);
                //Get data from stream, chunk it and send
                int read = await responseBody.ReadAsync(responseDataBuffer, 0, responseDataBuffer.Length);

                Debug.Assert(read > 0);

                SendDataTo(stream, responseDataBuffer, true);

                Http2Logger.LogDebug("Transfer end");
            }
        }

        private void WriteStatus(Http2Stream stream, int statusCode, bool final, HeadersList additionalHeaders = null)
        {
            var headers = new HeadersList
                {
                    new KeyValuePair<string, string>(":status", statusCode.ToString()),
                };

            if (additionalHeaders != null)
            {
                headers.AddRange(additionalHeaders);
            }

            stream.WriteHeadersFrame(headers, final, true);
        }

        private void SendDataTo(Http2Stream stream, byte[] binaryData, bool isLastChunk)
        {
            int i = 0;

            do
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                    ? MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize,
                                                    stream.WindowSize)
                                    : MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData && isLastChunk);

                i += chunkSize;
            } while (binaryData.Length > i);
        }
    }
}
