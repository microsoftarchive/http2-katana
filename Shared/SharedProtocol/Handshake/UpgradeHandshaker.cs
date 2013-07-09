using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Exceptions;
using SharedProtocol.Framing;

namespace SharedProtocol.Handshake
{
    public class UpgradeHandshaker
    {
        private const int HandshakeResponseSizeLimit = 1024;
        private static readonly byte[] CRLFCRLF = new [] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        private readonly ConnectionEnd _end;
        private Dictionary<string, string> _headers;

        public SecureSocket InternalSocket { get; private set; }

        public UpgradeHandshaker(IDictionary<string, object> handshakeEnvironment)
        {
            InternalSocket = (SecureSocket) handshakeEnvironment["secureSocket"];
            _end = (ConnectionEnd) handshakeEnvironment["end"];

            if (_end == ConnectionEnd.Client)
            {
                if (handshakeEnvironment.ContainsKey(":path") || (handshakeEnvironment[":path"] is string)
                    || handshakeEnvironment.ContainsKey(":method") || (handshakeEnvironment[":method"] is string)
                    || handshakeEnvironment.ContainsKey(":host") || (handshakeEnvironment[":host"] is string)
                    || handshakeEnvironment.ContainsKey(":version") || (handshakeEnvironment[":version"] is string))
                {
                    _headers = new Dictionary<string, string>();

                    _headers.Add(":path", (string)handshakeEnvironment[":path"]);
                    _headers.Add(":method", (string)handshakeEnvironment[":method"]);
                    _headers.Add(":host", (string)handshakeEnvironment[":host"]);
                    _headers.Add(":version", (string)handshakeEnvironment[":version"]);       
                }
                else
                {
                    throw new ArgumentException("Incorrect header for upgrade handshake");
                }
            }
        }

        public void Handshake()
        {
            HandshakeResponse handshakeResponse;
            if (_end == ConnectionEnd.Client)
            {
                // Build the request
                var builder = new StringBuilder();
                builder.AppendFormat("{0} {1} {2}\r\n", _headers[":method"], _headers[":path"], _headers[":version"]);
                builder.AppendFormat("Host: {0}\r\n", _headers[":host"]);
                builder.Append("Connection: Upgrade\r\n");
                builder.Append("Upgrade: HTTP/2.0\r\n");

                if (_headers != null)
                {
                    foreach (var key in _headers.Keys)
                    {
                        builder.AppendFormat("{0}: {1}\r\n", key, _headers[key]);
                    }
                }
                builder.Append("\r\n");

                byte[] requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
                InternalSocket.Send(requestBytes, 0, requestBytes.Length, SocketFlags.None);
                handshakeResponse = Read11Headers(InternalSocket);
            }
            else
            {
                handshakeResponse = Read11Headers(InternalSocket);

                if (_end == ConnectionEnd.Server && handshakeResponse.Result == HandshakeResult.Upgrade)
                {
                    const string status = "101";
                    const string protocol = "HTTP/1.1";
                    const string postfix = "Switching Protocols";

                    var builder = new StringBuilder();
                    builder.AppendFormat("{0} {1} {2}\r\n", protocol, status, postfix);
                    builder.Append("Connection: Upgrade\r\n");
                    builder.Append("Upgrade: HTTP/2.0\r\n");
                    builder.Append("\r\n");

                    byte[] requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
                    InternalSocket.Send(requestBytes, 0, requestBytes.Length, SocketFlags.None);
                }
            }
            if (handshakeResponse.Result == HandshakeResult.NonUpgrade)
            {
                throw new Http2HandshakeFailed();
            }
        }

        public HandshakeResponse Read11Headers(SecureSocket socket)
        {
            var buffer = new byte[HandshakeResponseSizeLimit];
            int lastInspectionOffset = 0;
            int readOffset = 0;
            int read = -1;
            do
            {
                try
                {
                    read = socket.Receive(buffer, readOffset, buffer.Length - readOffset, SocketFlags.None);
                }
                catch (IOException)
                {
                    return new HandshakeResponse { Result = HandshakeResult.UnexpectedConnectionClose };
                }

                if (read == 0)
                {
                    return new HandshakeResponse { Result = HandshakeResult.UnexpectedConnectionClose };
                }

                readOffset += read;
                int matchIndex;
                if (TryFindRangeMatch(buffer, lastInspectionOffset, readOffset, CRLFCRLF, out matchIndex))
                {
                    return InspectHanshake(buffer, matchIndex + CRLFCRLF.Length, readOffset);
                }

                lastInspectionOffset = Math.Max(0, readOffset - CRLFCRLF.Length);

                if (FrameHelpers.GetHighBitAt(buffer, 0))
                {
                    return new HandshakeResponse
                    {
                        Result = HandshakeResult.UnexpectedControlFrame,
                        ExtraData = new ArraySegment<byte>(buffer, 0, readOffset),
                    };
                }

            } while (readOffset < HandshakeResponseSizeLimit);

            throw new NotImplementedException("Handshake response size limit exceeded");
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
            string response = FrameHelpers.GetAsciiAt(buffer, 0, split).ToUpperInvariant();
            if (_end == ConnectionEnd.Client)
            {
                if (response.StartsWith("HTTP/1.1 101 SWITCHING PROTOCOLS")
                    && response.Contains("\r\nCONNECTION: UPGRADE\r\n")
                    && response.Contains("\r\nUPGRADE: HTTP/2.0\r\n"))
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
                if (response.Contains("\r\nCONNECTION: UPGRADE\r\n")
                    && response.Contains("\r\nUPGRADE: HTTP/2.0\r\n"))
                {
                    handshake.Result = HandshakeResult.Upgrade;
                }
                else
                {
                    handshake.Result = HandshakeResult.NonUpgrade;
                }
            }
            return handshake;
        }
    }
}
