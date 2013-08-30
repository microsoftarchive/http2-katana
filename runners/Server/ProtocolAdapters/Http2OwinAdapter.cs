using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Owin.Types;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;

namespace ProtocolAdapters
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Http2OwinAdapter : AbstractHttp2Adapter
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
            var owinRequest = new OwinRequest(environment);
            var owinResponse = new OwinResponse(environment);

            owinRequest.Method = headers.GetValue(":method");
            owinRequest.Path = headers.GetValue(":path");
            owinRequest.CallCancelled = CancellationToken.None;

            owinRequest.OwinVersion = Constants.OwinVersion;
            owinRequest.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            owinRequest.RemoteIpAddress = _transportInfo.RemoteIpAddress;
            owinRequest.RemotePort = _transportInfo.RemotePort;
            owinRequest.LocalIpAddress = _transportInfo.LocalIpAddress;
            owinRequest.LocalPort = _transportInfo.LocalPort;
            owinRequest.IsLocal = string.Equals(_transportInfo.RemoteIpAddress, _transportInfo.LocalPort);

            owinResponse.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            owinResponse.Body = new MemoryStream();

            foreach (var header in headers)
            {
                owinRequest.Headers.Add(header.Key, new[] {header.Value});
            }

            return environment;
        }

        protected override async Task ProcessRequest(Http2Stream stream)
        {
            var env = PopulateEnvironment(stream.Headers);
            Exception exception = null;

            try
            {
                await _next(env);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            await EndResponse(stream, env, exception);
        }

        protected override async Task ProcessIncomingData(Http2Stream stream)
        {
            //Do nothing... handling data is not supported by the server yet
        }

        private async Task EndResponse(Http2Stream stream, IDictionary<string, object> environment, Exception ex)
        {
            Stream responseBody = null;
            IDictionary<string, string[]> owinResponseHeaders = null;
            HeadersList responseHeaders = null;
            if (environment.ContainsKey(OwinConstants.ResponseBody))
                responseBody = (Stream) environment[OwinConstants.ResponseBody];

            if (environment.ContainsKey(OwinConstants.ResponseHeaders))
                owinResponseHeaders = (IDictionary<string, string[]>) environment[OwinConstants.ResponseHeaders];

            var responseStatusCode = (int) environment[OwinConstants.ResponseStatusCode];

            if (owinResponseHeaders != null)
                responseHeaders = new HeadersList(owinResponseHeaders);

            if (ex != null)
            {
                WriteStatus(stream, responseStatusCode, false, responseHeaders);
                return;
            }

            WriteStatus(stream, responseStatusCode, responseBody == null, responseHeaders);

            long contentLen = long.Parse(responseHeaders.GetValue("Content-Length"));
            int sent = 0;

            Http2Logger.LogDebug("Transfer begin");
            while (sent < contentLen)
            {
                //If thread is empty then do not send anything
                if (responseBody.Position != 0)
                {
                    var responseDataBuffer = new byte[responseBody.Position];

                    responseBody.Seek( sent, SeekOrigin.Begin );
                    //Get data from stream, chunk it and send
                    int read = await responseBody.ReadAsync(responseDataBuffer, 0, responseDataBuffer.Length);

                    SendDataTo(stream, responseDataBuffer, sent + read == contentLen);
                    sent += read;
                }
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
