using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using Owin.Types;
using SharedProtocol.EventArgs;
using SharedProtocol.Extensions;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using SharedProtocol.Utils;

namespace SharedProtocol
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    //TODO Remove Owin.Types dependency
    public class Http2OwinAdapter : IDisposable
    {
        private Http2Session _session;
        private bool _isDisposed;
        private DuplexStream _stream;
        private CancellationToken _cancToken;
        private readonly AppFunc _next;
        private TransportInformation _transportInfo;

        public Http2OwinAdapter(DuplexStream stream, TransportInformation transportInfo, 
                                IDictionary<string, object> environment, AppFunc next, 
                                CancellationToken cancel)
        {
            _transportInfo = transportInfo;
            _isDisposed = false;
            _cancToken = cancel;
            _next = next;
            _stream = stream;
            DepopulateEnvironment(environment);
        }

        private void DepopulateEnvironment(IDictionary<string, object> environment)
        {
            //_cancToken = (CancellationToken) environment[OwinConstants.CallCancelled];
        }

        private IDictionary<string, object> PopulateEnvironment(HeadersList headers)
        {
            var environment = new Dictionary<string, object>();
            var owinRequest = new OwinRequest(environment);
            var owinResponse = new OwinResponse(environment);

            owinRequest.CallCancelled = CancellationToken.None;
            owinRequest.OwinVersion = Constants.OwinVersion;
            owinRequest.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            owinRequest.RemoteIpAddress = _transportInfo.RemoteIpAddress;
            owinRequest.RemotePort = _transportInfo.RemotePort;
            owinRequest.LocalIpAddress = _transportInfo.LocalIpAddress;
            owinRequest.LocalPort = _transportInfo.LocalPort;
            owinRequest.IsLocal = string.Equals(_transportInfo.RemoteIpAddress, _transportInfo.LocalPort);

            owinResponse.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
               owinRequest.Headers.Add(header.Key, new []{header.Value});
            }

            return environment;
        }

        private void ProcessRequest(Http2Stream stream, HeadersList headers)
        {
            var env = PopulateEnvironment(headers);
            Exception exception = null;

            try
            {
                //TODO await?
               _next(env);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            EndResponse(stream, env, exception);
        }

        private void EndResponse(Http2Stream stream, IDictionary<string, object> environment, Exception ex)
        {
            Stream responseBody = null;
            IDictionary<string, string[]> owinResponseHeaders = null;
            HeadersList responseHeaders = null;
            if (environment.ContainsKey(OwinConstants.ResponseBody))
                responseBody = (Stream)environment[OwinConstants.ResponseBody];

            if (environment.ContainsKey(OwinConstants.ResponseHeaders))
                owinResponseHeaders = (IDictionary<string, string[]>) environment[OwinConstants.ResponseHeaders];

            var responseStatusCode = (int)environment[OwinConstants.ResponseStatusCode];

            if (owinResponseHeaders != null)
                responseHeaders = new HeadersList(owinResponseHeaders);

            if (ex != null)
            {
                WriteStatus(stream, responseStatusCode, false, responseHeaders);
                return;
            }

            WriteStatus(stream, responseStatusCode, responseBody == null, responseHeaders);

            if (responseBody != null)
            {
                var responseDataBuffer = new byte[responseBody.Position];

                //If thread is empty then do not send anything
                if (responseBody.Position != 0)
                {
                    //Get data from stream, chunk it and send
                    responseBody.ReadAsync(responseDataBuffer, 0, responseDataBuffer.Length);
                    SendDataTo(stream, responseDataBuffer);
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

        private void SendDataTo(Http2Stream stream, byte[] binaryData)
        {
            int i = 0;

            Http2Logger.LogDebug("Transfer begin");

            do
            {
                bool isLastData = binaryData.Length - i < Constants.MaxDataFrameContentSize;

                int chunkSize = stream.WindowSize > 0
                                    ? MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize,
                                                 stream.WindowSize)
                                    : MathEx.Min(binaryData.Length - i, Constants.MaxDataFrameContentSize);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryData, i, chunk, 0, chunk.Length);

                stream.WriteDataFrame(chunk, isLastData);

                i += chunkSize;
            } while (binaryData.Length > i);
        }

        private void OnFrameReceivedHandler(object sender, FrameReceivedEventArgs args)
        {
            var stream = args.Stream;
            var frame = args.Frame;

            switch (frame.FrameType)
            {
                case FrameType.Headers:
                    ProcessRequest(stream, stream.Headers);
                    break;
            }
        }

        public Task StartSession()
        {
            //TODO provide cancellation token
            _session = new Http2Session(_stream, ConnectionEnd.Server, true, true, _cancToken);
            _session.OnFrameReceived += OnFrameReceivedHandler;
            return _session.Start();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (_session != null)
            {
                _session.Dispose();
            }

            _isDisposed = true;
        }
    }
}
