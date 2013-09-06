using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Http2.Protocol;

namespace Microsoft.Http1.Protocol
{
    /// <summary>
    /// This class is designed for http11 handling.
    /// </summary>
    public static class Http11Helper
    {
        public static int Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }

        public static void SendRequest(Stream stream, string rawHeaders)
        {
            stream.Write(Encoding.UTF8.GetBytes(rawHeaders));
        }

        /// <summary>
        /// // TODO
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        /// <param name="contentType"></param>
        /// <param name="headers"></param>
        /// <param name="closeConnection">we don’t currently support persistent connection via Http1.1 so closeConnection:true</param>
        /// <returns></returns>
        public static int SendResponse(Stream stream, byte[] data, int statusCode, string contentType, IDictionary<string,string[]> headers = null, bool closeConnection = true)
        {
            string initialLine = "HTTP/1.1 " + statusCode + " " + StatusCode.GetReasonPhrase(statusCode) + "\r\n";
            string headersPack = initialLine;

            if (headers == null)
                headers = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);

            // TODO replace  hardcoded strigns with constants
            if (!headers.ContainsKey("Content-Type") && data.Length > 0)
            {
                headers.Add("Content-Type", new []{contentType});
            }

            if (!headers.ContainsKey("Connection") && closeConnection)
            {
                headers.Add("Connection", new[] { "Close" });
            }

            if (!headers.ContainsKey("Content-Length"))
            {
                headers.Add("Content-Length", new [] { Convert.ToString(data.Length) });
            }

            headersPack = headers.Aggregate(headersPack, (current, header) => current + (header.Key + ": " + String.Join(",", header.Value) + "\r\n")) + "\r\n";

            int sent = stream.Write(Encoding.UTF8.GetBytes(headersPack));
            //Send headers and body separately
            //TODO It's needed for our client. Think out a way to avoid separate sending.
            stream.Flush();

            if (data.Length > 0)
                sent += stream.Write(data);

            Thread.Sleep(200);

            stream.Flush();
            return sent;
        }

        public static string[] ReadHeaders(Stream stream)
        {
            var headers = new List<string>(5);

            var lineBuffer = new byte[1024];
            string header = String.Empty;
            int totalBytesCame = 0;
            int bytesOfLastHeader = 0;

            while (true)
            {
                bool gotException = false;
                var bf = new byte[1];
                int bytesCame = stream.Read(bf, 0, 1);
                if (bytesCame == 0)
                    break;

                Buffer.BlockCopy(bf, 0, lineBuffer, totalBytesCame, bytesCame);
                totalBytesCame += bytesCame;
                try
                {
                    header = Encoding.UTF8.GetString(lineBuffer, bytesOfLastHeader, totalBytesCame - bytesOfLastHeader);
                }
                catch
                {
                    gotException = true;
                }

                if (totalBytesCame != 0 && !gotException && header[header.Length - 1] == '\n')
                {
                    headers.Add(header.TrimEnd('\n', '\r'));
                    bytesOfLastHeader = totalBytesCame;
                }

                // empty header means we got \r\n\r\n which was trimmed. This means end of headers block.
                if (headers.Count >= 2 && String.IsNullOrEmpty(headers.LastOrDefault()))
                {
                    break;
                }
            }
            headers.RemoveAll(String.IsNullOrEmpty);

            return headers.ToArray();
        }

        private static KeyValuePair<string, string[]> GetHeaderNameValues(string header, int colonIndex)
        {
            string headerName = header.Substring(0, colonIndex);
            string[] values = header.Substring(colonIndex + 2).Split(','); // colon and space are skipped

            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = values[i].Trim();
            }

            return new KeyValuePair<string, string[]>(headerName, values);
        }

        // TODO find better way
        public static IDictionary<string, string[]> ParseHeaders(IEnumerable<string> headers)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                string headerName = String.Empty;
                var headerValues = new string[0];
                int colonIndex = header.IndexOf(':');
                KeyValuePair<string, string[]> headerNameValues;

                switch (colonIndex)
                {
                    case -1:
                        headerName = header;
                        break;
                    case 0:
                        colonIndex = header.IndexOf(':', 1);
                        headerNameValues = GetHeaderNameValues(header, colonIndex);
                        headerName = headerNameValues.Key;
                        headerValues = headerNameValues.Value;
                        break;
                    default:
                            headerNameValues = GetHeaderNameValues(header, colonIndex);
                            headerName = headerNameValues.Key;
                            headerValues = headerNameValues.Value;
                        break;
                }

                if (!dict.ContainsKey(headerName))
                    dict.Add(headerName, headerValues);
            }

            return dict;
        }
    }
}
