using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Exceptions;
using SharedProtocol.Framing;
using SharedProtocol.Utils;
using SharedProtocol.Extensions;

namespace SharedProtocol.Handshake
{
    /// <summary>
    ///This class is used for upgrade handshake handling
    /// </summary>
    public static class UpgradeHandshakeInspector
    {
        private static readonly byte[] CRLFCRLF = { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        /*public UpgradeHandshaker(IDictionary<string, object> handshakeEnvironment)
        {
            InternalSocket = (SecureSocket)handshakeEnvironment["secureSocket"];
            _end = (ConnectionEnd)handshakeEnvironment["end"];
            _handshakeResult = new Dictionary<string, object>();

            if (_end == ConnectionEnd.Client)
            {
                if (handshakeEnvironment.ContainsKey(":host") || (handshakeEnvironment[":host"] is string)
                    || handshakeEnvironment.ContainsKey(":version") || (handshakeEnvironment[":version"] is string))
                {
                    _headers = new Dictionary<string, object>
                        {
                            {":path",  handshakeEnvironment[":path"]},
                            {":host",  handshakeEnvironment[":host"]},
                            {":version",  handshakeEnvironment[":version"]},
                            {":max_concurrent_streams", 100},
                            {":initial_window_size", 2000000},
                        };
                }
                else
                {
                    throw new InvalidConstraintException("Incorrect header for upgrade handshake");
                }
            }
        }*/

        private static bool TryFindRangeMatch(byte[] buffer, int offset, int limit, byte[] matchSequence, out int matchIndex)
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

        private static bool TryRangeMatch(byte[] buffer, int offset, int limit, byte[] matchSequence)
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
        /*private static bool InspectHanshake(Dictionary<string, object> headersToInspect)
        {
            var headersCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            headersCopy.AddRange(headersToInspect);

            if (headersCopy.ContainsKey("CONNECTION")
                && headersCopy["CONNECTION"] == "UPGRADE, HTTP2-SETTINGS"
                && headersCopy.ContainsKey("UPGRADE")
                && headersCopy["UPGRADE"] == Protocols.Http2
                && headersCopy.ContainsKey("HTTP2 - SETTINGS"))
            {
                //TODO get headers and return it somehow (add to environment?)
                return true;
            }

            return false;
        }*/

        private static void GetHeaders(string clientResponse)
        {
            /*int methodIndex = clientResponse.IndexOf("GET", StringComparison.OrdinalIgnoreCase);
            int pathIndex = clientResponse.IndexOf("/", methodIndex, StringComparison.OrdinalIgnoreCase);
            int endPathIndex = clientResponse.IndexOf(" ", pathIndex, StringComparison.OrdinalIgnoreCase);
            string path = clientResponse.Substring(pathIndex, endPathIndex - pathIndex);
            string method = clientResponse.Substring(methodIndex, pathIndex).Trim().ToLower();
            _handshakeResult.Add(":path", path);
            _handshakeResult.Add(":method", method);

            string clientHeadersInBase64 = clientResponse.Substring(clientResponse.LastIndexOf(' ') + 1);
            byte[] buffer = Convert.FromBase64String(clientHeadersInBase64);
            string response = Encoding.UTF8.GetString(buffer);
            var headers = Regex.Matches(response, "^:.*$", RegexOptions.Multiline | RegexOptions.Compiled);
            foreach (Match header in headers)
            {
                string[] nameValue = header.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                _handshakeResult.Add(nameValue[0].ToLower().TrimEnd(':'), nameValue[1].TrimEnd('\r', '\n'));
            }*/
        }
    }
}
