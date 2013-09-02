using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Http2.Protocol.Http11
{
    /// <summary>
    /// This class is designed for http11 handling.
    /// </summary>
    public static class Http11Manager
    {

        public static int Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0,buffer.Length);
            return buffer.Length;
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

            if (!headers.ContainsKey("Conection") && closeConnection)
            {
                headers.Add("Connection", new[] { "Close" });
            }

            if (!headers.ContainsKey("Content-Lenght"))
            {
                headers.Add("Content-Lenght", new [] { Convert.ToString(data.Length) });
            }

            headersPack = headers.Aggregate(headersPack, (current, header) => current + (header.Key + ": " + String.Join(",", header.Value) + "\r\n")) + "\r\n";

            int sent = stream.Write(Encoding.UTF8.GetBytes(headersPack));
            //SendHeaders(stream, headers, data.Length);

            if (data.Length > 0)
                sent += stream.Write(data);

            stream.Flush();
            return sent;
        }

        public static string[] ReadHeaders(Stream socket)
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
                int bytesCame = socket.Read(bf, 0, 1);
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

        // TODO find better way
        public static IDictionary<string, string[]> ParseHeaders(IEnumerable<string> headers)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                int colonIndex = header.IndexOf(':');
                if (colonIndex == -1)
                {
                    dict.Add(header, new string[0]);
                }
                else
                {
                    string headerName = header.Substring(0, colonIndex);
                    string[] values = header.Substring(colonIndex + 2).Split(','); // colon and space are skipped
                    for (int i = 0; i < values.Length; ++i)
                    {
                        values[i] = values[i].Trim();
                    }

                    dict.Add(headerName, values);
                }
            }

            return dict;
        }
    }
}
