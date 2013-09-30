using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Http2.TestClient.Adapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Utils;
using Org.Mentalis.Security.Ssl;

namespace Http2.TestClient.Handshake
{
    /// <summary>
    ///This class is used for upgrade handshake handling
    /// </summary>
    public class UpgradeHandshaker
    {
        private const int HandshakeResponseSizeLimit = 4096;
        private static readonly byte[] CRLFCRLF = {(byte) '\r', (byte) '\n', (byte) '\r', (byte) '\n'};


        private readonly ConnectionEnd _end;
        private readonly Dictionary<string, object> _headers;
        private bool _wasResponseReceived;
        private IDictionary<string, object> _handshakeResult;
        private HandshakeResponse _response;
        public Stream IoStream { get; private set; }

        public UpgradeHandshaker(IDictionary<string, object> handshakeEnvironment)
        {
            IoStream = (Stream)handshakeEnvironment[HandshakeKeys.Stream];
            _end = (ConnectionEnd)handshakeEnvironment[HandshakeKeys.ConnectionEnd];
            _handshakeResult = new Dictionary<string, object>();

            if (_end == ConnectionEnd.Client)
            {
                if (handshakeEnvironment.ContainsKey(CommonHeaders.Host) || (handshakeEnvironment[CommonHeaders.Host] is string)
                    || handshakeEnvironment.ContainsKey(CommonHeaders.Version) || (handshakeEnvironment[CommonHeaders.Version] is string))
                {
                    _headers = new Dictionary<string, object>
                        {
                            {CommonHeaders.Path, handshakeEnvironment[CommonHeaders.Path]},
                            {CommonHeaders.Host, handshakeEnvironment[CommonHeaders.Host]},
                            {CommonHeaders.Version, handshakeEnvironment[CommonHeaders.Version]},
                            {CommonHeaders.MaxConcurrentStreams, 100},
                            {CommonHeaders.InitialWindowSize, 2000000},
                        };
                }
                else
                {
                    throw new InvalidConstraintException("Incorrect header for upgrade handshake");
                }
            }
        }

        public void ReadHeadersAndInspectHandshake()
        {
            try
            {
                _response = Read11Headers();
                _wasResponseReceived = true;
            }
            catch (Exception ex)
            {
                Http2Logger.LogError(ex.Message);
                throw;
            }
        }

        public IDictionary<string, object> Handshake()
        {
            if (_end == ConnectionEnd.Client)
            {
                // Build the request
                var builder = new StringBuilder();
                builder.AppendFormat("{0} {1} {2}\r\n", Verbs.Get, _headers[CommonHeaders.Path], Protocols.Http1);
                //TODO pass here requested filename
                builder.AppendFormat("Host: {0}\r\n", _headers[CommonHeaders.Host]);
                builder.Append(String.Format("{0}: {1}, {2}\r\n", CommonHeaders.Connection, CommonHeaders.Upgrade, CommonHeaders.Http2Settings));
                builder.Append(String.Format("{0}: {1}\r\n", CommonHeaders.Upgrade, Protocols.Http2));
                var settingsPayload = String.Format("{0}, {1}", 200000, 100);
                var settingsBytes = Encoding.UTF8.GetBytes(settingsPayload);
                var settingsBase64 = Convert.ToBase64String(settingsBytes);

                builder.Append(String.Format("{0}: {1}", CommonHeaders.Http2Settings, settingsBase64));
                builder.Append("\r\n\r\n");

                byte[] requestBytes = Encoding.UTF8.GetBytes(builder.ToString());
                _handshakeResult = new Dictionary<string, object>(_headers) {{CommonHeaders.Method, Verbs.Get.ToLower()}};
                IoStream.Write(requestBytes, 0, requestBytes.Length);
                IoStream.Flush();
                ReadHeadersAndInspectHandshake();
            }
            else
            {
                ReadHeadersAndInspectHandshake();
                if (_response.Result == HandshakeResult.Upgrade)
                {
                    const int status = StatusCode.Code101SwitchingProtocols;
                    string protocol = Protocols.Http1;
                    string postfix = StatusCode.GetReasonPhrase(status);

                    var builder = new StringBuilder();
                    builder.AppendFormat("{0} {1} {2}\r\n", protocol, status, postfix);
                    builder.Append(String.Format("{0}: {1}\r\n", CommonHeaders.Connection, CommonHeaders.Upgrade));
                    builder.Append(String.Format("{0}: {1}\r\n", CommonHeaders.Upgrade, Protocols.Http2));
                    builder.Append("\r\n");

                    byte[] requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
                    IoStream.Write(requestBytes, 0, requestBytes.Length);
                    IoStream.Flush();
                }
            }

            if (!_wasResponseReceived)
            {
                throw new Http2HandshakeFailed(HandshakeFailureReason.Timeout);
            }

            if (_response.Result != HandshakeResult.Upgrade)
            {
                _handshakeResult.Add(HandshakeKeys.Successful, HandshakeKeys.False);
                var path = _headers[CommonHeaders.Path] as string;
                
                Http2Logger.LogDebug("Handling with http11");
                var http11Adapter = new Http11ClientMessageHandler(IoStream, path);
                http11Adapter.HandleHttp11Response(_response.ResponseBytes.Array, 0, _response.ResponseBytes.Count);

                return _handshakeResult;
            }

            _handshakeResult.Add(HandshakeKeys.Successful, HandshakeKeys.True);
            return _handshakeResult;
        }

        private HandshakeResponse Read11Headers()
        {
            byte[] buffer = new byte[HandshakeResponseSizeLimit];
            int readOffset = 0;
            do
            {
                int read;
                try
                {
                    read = IoStream.Read(buffer, readOffset, buffer.Length - readOffset);
                }
                catch (IOException)
                {
                    return new HandshakeResponse {Result = HandshakeResult.UnexpectedConnectionClose};
                }

                if (read <= 0)
                {
                    return new HandshakeResponse {Result = HandshakeResult.UnexpectedConnectionClose};
                }

                readOffset += read;
                int lastInspectionOffset = Math.Max(0, readOffset - CRLFCRLF.Length);
                int matchIndex;
                if (TryFindRangeMatch(buffer, lastInspectionOffset, readOffset, CRLFCRLF, out matchIndex))
                {
                    return InspectHanshake(buffer, matchIndex + CRLFCRLF.Length, readOffset);
                }

            } while (readOffset < HandshakeResponseSizeLimit);

            throw new InvalidOperationException("Handshake response size limit exceeded");
        }

        private bool TryFindRangeMatch(byte[] buffer, int offset, int limit, byte[] matchSequence, out int matchIndex)
        {
            matchIndex = 0;
            for (int master = offset; master < limit && master + matchSequence.Length <= limit; master++)
            {
                if (TryRangeMatch(buffer, master, limit, matchSequence))
                {
                    matchIndex = master;
                    return true;
                }
            }
            return false;
        }

        private bool TryRangeMatch(byte[] buffer, int offset, int limit, byte[] matchSequence)
        {
            bool matched = (limit - offset) >= matchSequence.Length;
            for (int sequence = 0; sequence < matchSequence.Length && matched; sequence++)
            {
                matched = (buffer[offset + sequence] == matchSequence[sequence]);
            }
            if (matched)
            {
                return true;
            }
            return false;
        }

        // We've found a CRLFCRLF sequence.  Confirm the status code is 101 for upgrade.
        private HandshakeResponse InspectHanshake(byte[] buffer, int split, int limit)
        {
            var handshake = new HandshakeResponse
                {
                    ResponseBytes = new ArraySegment<byte>(buffer, 0, split),
                    ExtraData = new ArraySegment<byte>(buffer, split, limit),
                };
            // Must be at least "HTTP/1.1 101\r\nConnection: Upgrade\r\nUpgrade: HTTP/2.0\r\n\r\n"
            Contract.Assert(split >= 0 && split - 1 < buffer.Length);
            var response = Encoding.ASCII.GetString(buffer, 0, split);
            if (_end == ConnectionEnd.Client)
            {
                int status = StatusCode.Code101SwitchingProtocols;
                string reasonPhrase = StatusCode.GetReasonPhrase(status);
                
                if (response.StartsWith(String.Format("{0} {1} {2}", Protocols.Http1, status.ToString(), reasonPhrase), 
                                            StringComparison.OrdinalIgnoreCase)
                    && response.IndexOf(String.Format("\r\n{0}: {1}\r\n", CommonHeaders.Connection, CommonHeaders.Upgrade), 
                                            StringComparison.OrdinalIgnoreCase) >= 0
                    &&  response.IndexOf(String.Format("\r\n{0}: {1}\r\n", CommonHeaders.Upgrade, Protocols.Http2),
                                            StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    handshake.Result = HandshakeResult.Upgrade;
                }
                else
                {
                    handshake.Result = HandshakeResult.NonUpgrade;
                }
            }
            else
            {
                if (
                    response.IndexOf(String.Format("\r\n{0}: {1}, {2}\r\n", CommonHeaders.Connection, CommonHeaders.Upgrade, CommonHeaders.Http2Settings), 
                                    StringComparison.OrdinalIgnoreCase) >= 0
                    && response.IndexOf(String.Format("\r\n{0}: {1}\r\n", CommonHeaders.Upgrade, Protocols.Http2),
                                    StringComparison.OrdinalIgnoreCase) >= 0
                    && response.IndexOf(String.Format("\r\n{0}:", CommonHeaders.Http2Settings), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GetHeaders(response);
                    handshake.Result = HandshakeResult.Upgrade;
                }
                else
                {
                    handshake.Result = HandshakeResult.NonUpgrade;
                }
            }

            return handshake;
        }

        private void GetHeaders(string clientResponse)
        {
            int methodIndex = clientResponse.IndexOf(Verbs.Get, StringComparison.OrdinalIgnoreCase);
            int pathIndex = clientResponse.IndexOf("/", methodIndex, StringComparison.OrdinalIgnoreCase);
            int endPathIndex = clientResponse.IndexOf(" ", pathIndex, StringComparison.OrdinalIgnoreCase);
            string path = clientResponse.Substring(pathIndex, endPathIndex - pathIndex);
            string method = clientResponse.Substring(methodIndex, pathIndex).Trim().ToLower();
            _handshakeResult.Add(CommonHeaders.Path, path);
            _handshakeResult.Add(CommonHeaders.Method, method);

            string clientHeadersInBase64 = clientResponse.Substring(clientResponse.LastIndexOf(' ') + 1);
            byte[] buffer = Convert.FromBase64String(clientHeadersInBase64);
            string response = Encoding.UTF8.GetString(buffer);
            var headers = Regex.Matches(response, "^:.*$", RegexOptions.Multiline | RegexOptions.Compiled);
            foreach (Match header in headers)
            {
                string[] nameValue = header.Value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                _handshakeResult.Add(nameValue[0].ToLower().TrimEnd(':'), nameValue[1].TrimEnd('\r', '\n'));
            }
        }
    }
}
