using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Http11
{
    public static class Http11Manager
    {
        private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        private static string GetFileName(string[] headers)
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

            string[] getRequestSplitted = getRequest.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in getRequestSplitted)
            {
                if (token.StartsWith("/")) //Filename starts with "/"
                    return token;
            }

            return String.Empty;
        }

        private static void SaveFile(string directory, string fileName, byte[] fileBytes)
        {
            string newfilepath = String.Empty;

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
                    Console.WriteLine("Cant overwrite file: " + newfilepath);
                }
            }
            using (var fs = new FileStream(newfilepath, FileMode.Create))
            {
                fs.Write(fileBytes, 0, fileBytes.Length);
            }

            Console.WriteLine("File saved: " + fileName);
        }

        private static string[] ReadHeaders(SecureSocket socket)
        {
            byte[] lineBuffer = new byte[256];
            string header = String.Empty;
            int totalBytesCame = 0;
            bool gotException;
            int bytesOfLastHeader = 0;
            List<string> headers = new List<string>(10);

            while (true)
            {
                gotException = false;
                byte[] bf = new byte[1];
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

            headers.RemoveAll(s => String.IsNullOrEmpty(s));

            return headers.ToArray();
        }

        public static void Http11DownloadResource(SecureSocket socket, Uri requestUri)
        {
            byte[] headersBytes = Encoding.UTF8.GetBytes(ConstructHeaders(requestUri));
            int sent = socket.Send(headersBytes);

            string[] responseHeaders = ReadHeaders(socket);

            byte[] buffer = new byte[128 * 1024]; //128 kb
            int received;
            using (var stream = new MemoryStream(128 * 1024))
            {
                while (true)
                {
                    received = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (received == 0)
                        break;

                    stream.Write(buffer, 0, received);
                }

                byte[] fileBuffer = new byte[stream.Position];
                Buffer.BlockCopy(stream.GetBuffer(), 0, fileBuffer, 0, fileBuffer.Length);
                int fileNameIndex = requestUri.AbsolutePath.LastIndexOf("/");
                string fileName = requestUri.AbsolutePath.Substring(fileNameIndex);
                //string directory = this.Uri.AbsolutePath.Substring(0, fileNameIndex);

                //TODO Saving not only in client exe folder
                string directory = assemblyPath;
                SaveFile(directory, fileName, fileBuffer);
            }
        }

        public static void Http11SendResponse(SecureSocket socket)
        {
            string[] headers = GetHttp11Headers(socket);
            string filename = GetFileName(headers);

            string path = Path.GetFullPath(assemblyPath + @"\root" + filename);

            if (!File.Exists(path))
            {
                Console.WriteLine("File " + filename + " not found");
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
                    Console.WriteLine("Sent: " + sent);
                    Console.WriteLine("File sent: " + filename);

                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                var msgBytes = Encoding.UTF8.GetBytes(ex.Message);
                socket.Send(msgBytes);

                Console.WriteLine(ex.Message);
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
            List<string> headers = new List<string>(5);

            byte[] lineBuffer = new byte[256];
            string header = String.Empty;
            int totalBytesCame = 0;
            bool gotException;
            int bytesOfLastHeader = 0;

            while (true)
            {
                gotException = false;
                byte[] bf = new byte[1];
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
            headers.RemoveAll(s => String.IsNullOrEmpty(s));

            return headers.ToArray();
        }
    }
}
