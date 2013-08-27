using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.EventArgs;
using SharedProtocol.IO;
using SharedProtocol.Utils;

namespace SharedProtocol.Http11
{
    /// <summary>
    /// This class is designed for http11 handling.
    /// </summary>
    public static class Http11Manager
    {
      
   
        public static int SendResponse(DuplexStream stream, byte[] data, int statusCode, string contentType, Dictionary<string,string> headers = null)
        {
            string initialLine = "HTTP/1.1 " + statusCode + " " + StatusCode.GetReasonPhrase(statusCode) + "\r\n";
            string headersPack = initialLine;

            if (headers == null)
                headers = new Dictionary<string,string>();

            if (data.Length > 0)
            {
                headers.Add("Content-Type:", contentType);
            }

            headersPack = headers.Aggregate(headersPack, (current, header) => current + (header.Key + ": " + header.Value + "\r\n")) + "\r\n";

            int sent = stream.Write(Encoding.UTF8.GetBytes(headersPack));
            //SendHeaders(stream, headers, data.Length);

            if (data.Length > 0)
                sent += stream.Write(data);

            stream.Flush();
            return sent;
        }

        public static string ConstructHeaders(Uri uri)
        {
            string requestHeaders = string.Format(
                        "GET {2} HTTP/1.1\r\n"
                        + "Host: {0}:{1}\r\n"
                        + "Connection: Keep-Alive\r\n"
                        + "User-Agent: Http2Client\r\n"
                        + "Accept: {3},application/xml;q=0.9,*/*;q=0.8\r\n"
                        + "\r\n",
                        uri.Host,
                        uri.Port,
                        uri.AbsolutePath, // match what Chrome has in GET request
                        ContentTypes.GetTypeFromFileName(uri.ToString()));

            return requestHeaders;
        }

        //TODO must be reworked
        public static void SendHeaders(DuplexStream socket, Dictionary<string, string> headers, int contentLength = 0)
        {
            var headersString = new StringBuilder();

            foreach (var header in headers)
            {
                headersString.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }
            headersString.AppendFormat("Content-Length: {0}\r\n" + "\r\n", contentLength);
            byte[] headersBytes = Encoding.UTF8.GetBytes(headersString.ToString());
            socket.Write(headersBytes);
        }

        public static string[] ReadHeaders(DuplexStream socket)
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
