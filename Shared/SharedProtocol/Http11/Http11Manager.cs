using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Utils;

namespace SharedProtocol.Http11
{
    /// <summary>
    /// This class is designed for http11 handling.
    /// </summary>
    public static class Http11Manager
    {
        /// <summary>
        /// Download Successful event
        /// </summary>
        public static event EventHandler<Http11ResourceDownloadedEventArgs> OnDownloadSuccessful;

        /// <summary>
        /// Socket closed event
        /// </summary>
        public static event EventHandler<SocketCloseEventArgs> OnSocketClosed;


        //Remove file:// from Assembly.GetExecutingAssembly().CodeBase
        private static readonly string AssemblyPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        
        private static string GetFileName(IEnumerable<string> headers)
        {
            string getRequest = String.Empty;

            //Finding Get request - filename is there
            foreach (var header in headers)
            {
                if (header.IndexOf("GET", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    getRequest = header;
                    break;
                }
            }

            var getRequestSplitted = getRequest.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in getRequestSplitted.Where(token => token.StartsWith("/")))
            {
                return token;
            }

            return String.Empty;
        }

        private static void SaveFile(string directory, string fileName, byte[] fileBytes)
        {
            string newfilepath;

            // create local file path
            if (!string.IsNullOrEmpty(directory))
            {
                if (directory[0] == '\\')
                {
                    directory = '.' + directory;
                }

                Directory.CreateDirectory(directory);
                newfilepath = directory + '\\' + fileName;
            }
            else
            {
                newfilepath = fileName;
            }

            if (File.Exists(newfilepath))
            {
                try
                {
                    File.Delete(newfilepath);
                }
                catch (Exception)
                {
                    Http2Logger.LogError("Cant overwrite file: " + newfilepath);
                }
            }
            using (var fs = new FileStream(newfilepath, FileMode.Create))
            {
                fs.Write(fileBytes, 0, fileBytes.Length);
            }

            Http2Logger.LogInfo("File saved: " + fileName);
        }

        private static string[] ReadHeaders(SecureSocket socket)
        {
            var lineBuffer = new byte[256];
            string header = String.Empty;
            int totalBytesCame = 0;
            int bytesOfLastHeader = 0;
            var headers = new List<string>(10);

            while (true)
            {
                bool gotException = false;
                var bf = new byte[1];
                int bytesCame = socket.Receive(bf);
                if (bytesCame == 0) break;

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

        public static void Http11DownloadResource(SecureSocket socket, Uri requestUri)
        {
            byte[] headersBytes = Encoding.UTF8.GetBytes(ConstructHeaders(requestUri));
            int sent = socket.Send(headersBytes);

            string[] responseHeaders = ReadHeaders(socket);

            var buffer = new byte[128 * 1024]; //128 kb
            using (var stream = new MemoryStream(128 * 1024))
            {
                while (true)
                {
                    int received = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (received == 0)
                        break;

                    stream.Write(buffer, 0, received);
                }

                var fileBuffer = new byte[stream.Position];
                Buffer.BlockCopy(stream.GetBuffer(), 0, fileBuffer, 0, fileBuffer.Length);
                int fileNameIndex = requestUri.AbsolutePath.LastIndexOf("/");
                string fileName = requestUri.AbsolutePath.Substring(fileNameIndex);

                string directory = AssemblyPath;
                SaveFile(directory, fileName, fileBuffer);

                if (OnDownloadSuccessful != null)
                {
                    OnDownloadSuccessful(null, new Http11ResourceDownloadedEventArgs(fileBuffer.Length, fileName));
                }

                socket.Close();

                if (OnSocketClosed != null)
                {
                    OnSocketClosed(null, new SocketCloseEventArgs());
                }
            }
        }

        public static void Http11SendResponse(SecureSocket socket)
        {
            string[] headers = GetHttp11Headers(socket);
            string filename = GetFileName(headers);

            //No headers where received
            if (headers.Length == 0)
            {
                socket.Close();
            }

            string path = Path.GetFullPath(AssemblyPath + @"\root" + filename);

            if (!File.Exists(path))
            {
                Http2Logger.LogError("File " + filename + " not found");
                return;
            }

            try
            {
                using (var sr = new StreamReader(path))
                {
                    string file = sr.ReadToEnd();
                    SendHeaders(socket, null, file.Length);

                    var fileBytes = Encoding.UTF8.GetBytes(file);

                    int sent = socket.Send(fileBytes);
                    Http2Logger.LogDebug(string.Format("Sent: {0} bytes", sent));
                    Http2Logger.LogInfo("File sent: " + filename);

                    socket.Close();

                    if (OnSocketClosed != null)
                    {
                        OnSocketClosed(null, new SocketCloseEventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                var msgBytes = Encoding.UTF8.GetBytes(ex.Message);
                socket.Send(msgBytes);

                Http2Logger.LogError(ex.Message);
            }
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
        public static void SendHeaders(SecureSocket socket, Dictionary<string, string> headers, int? contentLength = null)
        {
            Contract.Assert(contentLength != null);

            byte[] headersBytes = Encoding.UTF8.GetBytes(string.Format("Content-Length: {0}\r\n" + "\r\n", contentLength));
            socket.Send(headersBytes);
        }

        public static string[] GetHttp11Headers(SecureSocket socket)
        {
            var headers = new List<string>(5);

            var lineBuffer = new byte[256];
            string header = String.Empty;
            int totalBytesCame = 0;
            int bytesOfLastHeader = 0;

            while (true)
            {
                bool gotException = false;
                var bf = new byte[1];
                int bytesCame = socket.Receive(bf);
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
