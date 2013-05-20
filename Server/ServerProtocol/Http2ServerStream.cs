using Owin.Types;
using SharedProtocol;
using SharedProtocol.Framing;
using SharedProtocol.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServerProtocol
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Http2ServerStream : Http2BaseStream
    {
        private TransportInformation _transportInfo;
        private IDictionary<string, object> _environment;
        private OwinRequest _owinRequest;
        private OwinResponse _owinResponse;
        private object _responseStarted;

        private HeadersPlusPriority _headersPlusPriority;
        private IList<KeyValuePair<string, string>> _requestHeaderPairs;
        private IDictionary<string, object> _upgradeEnvironment;

        private CancellationTokenSource _streamCancel;
        
        private Http2ServerStream(int id, TransportInformation transportInfo, WriteQueue writeQueue, HeaderWriter headerWriter, CancellationToken sessionCancel)
            : base(id, writeQueue, headerWriter, Constants.DefaultFlowControlCredit, sessionCancel)
        {
            _streamCancel = CancellationTokenSource.CreateLinkedTokenSource(sessionCancel);
            _cancel = _streamCancel.Token;
            _transportInfo = transportInfo;
        }

        // For use with HTTP/1.1 upgrade handshakes
        public Http2ServerStream(int id, TransportInformation transportInfo, IDictionary<string, object> upgradeEnvironment,
            WriteQueue writeQueue, HeaderWriter headerWriter, CancellationToken sessionCancel)
            : this(id, transportInfo, writeQueue, headerWriter, sessionCancel)
        {
            Contract.Assert(id == 1, "This constructor is only used for the initial HTTP/1.1 handshake request.");
            _upgradeEnvironment = upgradeEnvironment;

            // Environment will be populated on another thread in Run
        }

        // For use with incoming HTTP2 binary frames
        public Http2ServerStream(HeadersPlusPriority headersPlusPriorityFrame, IList<KeyValuePair<string, string>> headerPairs, TransportInformation transportInfo, WriteQueue writeQueue, HeaderWriter headerWriter, CancellationToken cancel)
            : this(headersPlusPriorityFrame.StreamId, transportInfo, writeQueue, headerWriter, cancel)
        {
            _headersPlusPriority = headersPlusPriorityFrame;
            _requestHeaderPairs = headerPairs;
            if (_headersPlusPriority.IsFin)
            {
                // Set stream state to request body complete and RST the stream if additional frames arrive.
                FinReceived = true;
                _incomingStream = Stream.Null;
            }
            else
            {
                _incomingStream = new InputStream(Constants.DefaultFlowControlCredit, SendWindowUpdate);
            }
        }

        public IDictionary<string, object> Environment { get { return _environment; } }

        private bool RequestHeadersReceived
        {
            get { return (_state & StreamState.RequestHeaders) == StreamState.RequestHeaders; }
            set { Contract.Assert(value); _state |= StreamState.RequestHeaders; }
        }

        private bool ResponseHeadersSent
        {
            get { return (_state & StreamState.ResponseHeaders) == StreamState.ResponseHeaders; }
            set { Contract.Assert(value); _state |= StreamState.ResponseHeaders; }
        }

        // We've been offloaded onto a new thread. Decode the headers, invoke next, and do cleanup processing afterwards
        public void Run(AppFunc next)
        {
            try
            {
                PopulateEnvironment();

                //await next(Environment);
                StartResponse();
                EndResponse(); 
            }
            catch (Exception ex)
            {
                EndResponse(ex);
            }
        }

        private void PopulateEnvironment()
        {
            RequestHeadersReceived = true;

            _environment = new Dictionary<string, object>();
            _owinRequest = new OwinRequest(_environment);
            _owinResponse = new OwinResponse(_environment);

            _owinRequest.CallCancelled = _cancel;
            _owinRequest.OwinVersion = Constants.OwinVersion;

            _owinRequest.RemoteIpAddress = _transportInfo.RemoteIpAddress;
            _owinRequest.RemotePort = _transportInfo.RemotePort;
            _owinRequest.LocalIpAddress = _transportInfo.LocalIpAddress;
            _owinRequest.LocalPort = _transportInfo.LocalPort;
            _owinRequest.IsLocal = string.Equals(_transportInfo.RemoteIpAddress, _transportInfo.LocalPort);

            DeserializeRequestHeaders();
            _priority = _headersPlusPriority.Priority;

            _owinRequest.Body = _incomingStream;
            _headersPlusPriority = null;

            _owinRequest.Set("http2.Priority", (int)_priority);

            _outputStream = new OutputStream(_id, _priority, _writeQueue, _initialWindowSize);
            _owinResponse.Body = _outputStream;
        }

        // Includes method, path&query, version, host, scheme.
        private void DeserializeRequestHeaders()
        {
            Contract.Assert(_headersPlusPriority != null);

            _owinRequest.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
   
            foreach (KeyValuePair<string, string> pair in _requestHeaderPairs)
            {
                if (pair.Key[0] == ':')
                {
                    MapRequestProperty(pair.Key, pair.Value);
                }
                else
                {
                    // Null separated list of values
                    _owinRequest.Headers[pair.Key] = pair.Value.Split('\0');
                }
            }

            VerifyRequiredRequestsPropertiesSet();
        }

        // HTTP/2.0 sends HTTP/1.1 request properties like path, query, etc, as headers prefixed with ':'
        private void MapRequestProperty(string key, string value)
        {
            // keys are required to be lower case
            if (":scheme".Equals(key, StringComparison.Ordinal))
            {
                _owinRequest.Scheme = value;
            }
            else if (":host".Equals(key, StringComparison.Ordinal))
            {
                _owinRequest.Host = value;
            }
            else if (":path".Equals(key, StringComparison.Ordinal))
            {
                // Split off query
                int queryIndex = value.IndexOf('?');
                string query = string.Empty;
                if (queryIndex >= 0)
                {
                    _owinRequest.QueryString = value.Substring(queryIndex + 1); // No leading '?'
                    value = value.Substring(0, queryIndex);
                }
                _owinRequest.Path = Uri.UnescapeDataString(value); // TODO: Is this the correct escaping?
                _owinRequest.PathBase = string.Empty;
            }
            else if (":method".Equals(key, StringComparison.Ordinal))
            {
                _owinRequest.Method = value;
            }
            else if (":version".Equals(key, StringComparison.Ordinal))
            {
                _owinRequest.Protocol = value;
            }
        }

        // Verify at least the minimum request properties were set:
        // scheme, host&port, path&query, method, version
        private void VerifyRequiredRequestsPropertiesSet()
        {
            // Set bitflags in MapRequestProperty?
            // TODO:
            // throw new NotImplementedException();
        }

        public override void EnsureStarted()
        {
            StartResponse();
        }

        private void StartResponse()
        {
            StartResponse(null, headersOnly: false);
        }

        // First write, or stack unwind without writes
        private bool StartResponse(Exception ex, bool headersOnly)
        {
            //Place for frame testing
            if (Interlocked.CompareExchange(ref _responseStarted, new object(), null) != null)
            {
                // Already started
                return false;
            }

            if (ex != null)
            {
                Contract.Assert(headersOnly);
                _owinResponse.StatusCode = StatusCode.Code500InternalServerError;
                _owinResponse.ReasonPhrase = StatusCode.Reason500InternalServerError;
                _owinResponse.Headers.Clear();
                // TODO: Should this be a RST_STREAM InternalError instead?
                // TODO: trigger the CancellationToken?
            }
            else
            {
                // TODO: Fire OnSendingHeaders event
            }

            //IList<KeyValuePair<string, string>> pairs = SerializeResponseHeaders();

            if (headersOnly)
            {
                FinSent = true;
            }
            ResponseHeadersSent = true;

            //_headerWriter.WriteSynReply(pairs, _id, _priority, headersOnly, _cancel);
            SettingsFrame frame =
                new SettingsFrame(new List<SettingsPair>() { new SettingsPair(0, SettingsIds.InitialWindowSize, _initialWindowSize) }) { StreamId = this.Id };
            _writeQueue.WriteFrameAsync(frame, Priority.Pri3);

            Console.WriteLine("Settings sent: " + frame.StreamId);

            return true;
        }

        private IList<KeyValuePair<string, string>> SerializeResponseHeaders()
        {
            IList<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

            int statusCode = Get<int>("owin.ResponseStatusCode", 200);
            string statusCodeString = statusCode.ToString(CultureInfo.InvariantCulture);
            string reasonPhrase = Get<string>("owin.ResponseReasonPhrase", StatusCode.GetReasonPhrase(statusCode));
            string statusHeader = statusCodeString + (reasonPhrase != null ? " " + reasonPhrase : string.Empty);
            pairs.Add(new KeyValuePair<string, string>(":status", statusHeader));

            string version = Get<string>("owin.ResponseProtocol", "HTTP/1.1");
            pairs.Add(new KeyValuePair<string, string>(":version", version));

            IDictionary<string, string[]> responseHeaders = Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            foreach (KeyValuePair<string, string[]> pair in responseHeaders)
            {
                pairs.Add(new KeyValuePair<string, string>(pair.Key.ToLowerInvariant(),
                    string.Join("\0", pair.Value)));
            }

            return pairs;
        }

        private void EndResponse()
        {
            byte[] binaryFile = FileHelper.GetFile(_owinRequest.PathBase, _owinRequest.Path);
            int i = 0;

            while (binaryFile.Length > i)
            {
                bool isLastData = binaryFile.Length - i < Constants.MaxDataFrameContentSize;
                int chunkSize = Math.Min(binaryFile.Length - i, Constants.MaxDataFrameContentSize);

                byte[] chunk = new byte[chunkSize];
                Buffer.BlockCopy(binaryFile, i, chunk, 0, chunk.Length);

                DataFrame dataFrame = new DataFrame(this.Id, new ArraySegment<byte>(chunk), isLastData);
                _writeQueue.WriteFrameAsync(dataFrame, Priority.Pri3);

                i += Constants.MaxDataFrameContentSize;
            }

            Console.WriteLine("File sent: " + _owinRequest.PathBase + _owinRequest.Path);

            EndResponse(null);
        }

        private void EndResponse(Exception ex)
        {
            if (ex != null)
            {
                // TODO: trigger the CancellationToken?
                if (!ResetSent)
                {
                    RstStreamFrame reset = new RstStreamFrame(Id, ResetStatusCode.InternalError);
                    ResetSent = true;
                    _writeQueue.WriteFrameAsync(reset, Priority.Control);
                }
            }
            else
            {
                // Fin may have been sent with a n extra headers frame.
                /*if (!FinSent && !ResetSent)
                {
                    DataFrame terminator = new DataFrame(_id);
                    FinSent = true;
                    _writeQueue.WriteFrameAsync(terminator, _priority);
                }*/
            }
            Dispose();
        }

        public override void Reset(ResetStatusCode statusCode)
        {
            try
            {
                _streamCancel.Cancel(false);
            }
            catch (AggregateException)
            {
                // TODO: Log
            }
            catch (ObjectDisposedException)
            {
            }
            base.Reset(statusCode);
        }

        private T Get<T>(string key, T fallback = default(T))
        {
            object obj;
            if (Environment.TryGetValue(key, out obj)
                   && obj is T)
            {
                return (T)obj;
            }
            return fallback;
        }
            
        protected override void Dispose(bool disposing)
        {
            if (!FinReceived && !ResetReceived && !ResetSent)
            {
                // The request body hasn't finished yet, and nobody is going to read it. Send a reset.
                // Note this may be put in the output queue after a successful response and FIN.
                ResetSent = true;
                RstStreamFrame reset = new RstStreamFrame(Id, ResetStatusCode.Cancel);
                _writeQueue.WriteFrameAsync(reset, _priority);
            }

            _streamCancel.Dispose();
            base.Dispose(disposing);
        }
    }
}
