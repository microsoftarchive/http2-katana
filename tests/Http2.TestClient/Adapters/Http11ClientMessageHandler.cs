using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Http2.Protocol.Utils;

namespace Http2.TestClient.Adapters
{
    public class Http11ClientMessageHandler : IDisposable
    {
        private readonly Stream _client;
        private readonly string _path;
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));

        public Http11ClientMessageHandler(Stream clientStream, string path)
        {
            _path = path;
            _client = clientStream;
        }

        private KeyValuePair<IDictionary<string, string>, byte[]> ParseHeadersAndReadResponseBody(byte[] headersBytes)
        {
            var rawHeadersString = Encoding.UTF8.GetString(headersBytes);

            var rawHeaders = rawHeadersString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var firstStr = rawHeaders[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var method = firstStr[0];
            var statusCode = int.Parse(firstStr[1]);
            var reasonPhrase = firstStr[2];

            var respHeaders = new Dictionary<string, string>
                {
                    {":method", method},
                    {":status", statusCode.ToString()},
                    {":reasonPhrase", reasonPhrase}
                };

            for (int i = 1; i < rawHeaders.Length; i++)
            {
                string name = String.Empty;
                string value = String.Empty;

                int colon = rawHeaders[i].IndexOf(':');
                if (colon == -1)
                {
                    name = rawHeaders[i];
                }
                else
                {
                    name = rawHeaders[i].Substring(0, colon).Trim();
                    value = rawHeaders[i].Substring(colon + 1).Trim();
                }

                if (!respHeaders.ContainsKey(name))
                {
                    Http2Logger.LogDebug("Incoming header: {0} : {1}", name, value);
                    respHeaders.Add(name, value);
                }
            }

            Http2Logger.LogDebug("Parsed headers");

            byte[] respBody = new byte[0];
            if (statusCode == 200)
            {
                int contLen = int.Parse(respHeaders["Content-Length"]);
                respBody = new byte[contLen];
                int totalReceived = 0;
                while (totalReceived < contLen)
                {
                    int received = _client.Read(respBody, totalReceived, respBody.Length - totalReceived);   
                    Debug.Assert(received > 0);
                    totalReceived += received;
                }

                Http2Logger.LogDebug("Received: {0}", totalReceived);
            }

            return new KeyValuePair<IDictionary<string, string>, byte[]>(respHeaders, respBody);
        }

        public void HandleHttp11Response(byte[] responseBinaryHeaders, int offset, int length)
        {
            var bytes = new byte[offset + length];
            Buffer.BlockCopy(responseBinaryHeaders, 0, bytes, offset, length);
            var response = ParseHeadersAndReadResponseBody(bytes);
            //TODO Handle headers somehow if it's needed
            using (var stream = new FileStream(AssemblyName + _path, FileMode.Create))
            {
                stream.Write(response.Value, 0, response.Value.Length);
            }

            Http2Logger.LogDebug("Response was saved as {0}", _path);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
