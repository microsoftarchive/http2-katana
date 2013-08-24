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
      
   
        //public static void Http11SendResponse(SecureSocket socket)
        //{
        //    string[] headers = GetHttp11Headers(socket);
        //    string filename = GetFileName(headers);


        //    if (headers.Length == 0)
        //    {
        //        Http2Logger.LogError("Request headers empty!");
        //    }

        //    string path = Path.GetFullPath(AssemblyPath + @"\root" + filename);
        //    string contentType = ContentTypes.GetTypeFromFileName(filename);
        //    if (!File.Exists(path))
        //    {
        //        Http2Logger.LogError("File " + filename + " not found");
        //        SendResponse(socket, new byte[0], StatusCode.Code404NotFound, contentType);
        //        socket.Close();
        //        return;
        //    }

        //    try
        //    {
        //        using (var sr = new StreamReader(path))
        //        {
        //            string file = sr.ReadToEnd();

        //            var fileBytes = Encoding.UTF8.GetBytes(file);

        //            int sent = SendResponse(socket, fileBytes, StatusCode.Code200Ok, contentType);
        //            Http2Logger.LogDebug(string.Format("Sent: {0} bytes", sent));
        //            Http2Logger.LogInfo("File sent: " + filename);

        //            socket.Close();

        //            if (OnSocketClosed != null)
        //            {
        //                OnSocketClosed(null, new SocketCloseEventArgs());
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var msgBytes = Encoding.UTF8.GetBytes(ex.Message);
        //        SendResponse(socket, msgBytes, StatusCode.Code500InternalServerError, contentType);
        //        Http2Logger.LogError(ex.Message);
        //    }
        //}

        public static int SendResponse(DuplexStream socket, byte[] data, int statusCode, string contentType, Dictionary<string,string> headers = null)
        {
            string initialLine = "HTTP/1.1 " + statusCode + " " + StatusCode.GetReasonPhrase(statusCode) + "\r\n";

            if (headers == null)
                headers = new Dictionary<string,string>();

            if (data.Length > 0)
            {
                headers.Add("Content-Type", contentType);
            }
            else
            {
                initialLine += "\r\n";
            }

            int sent = socket.Write(Encoding.UTF8.GetBytes(initialLine));
            SendHeaders(socket, headers, data.Length);
            if (data.Length > 0)
                sent += socket.Write(data);

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
    }
}
