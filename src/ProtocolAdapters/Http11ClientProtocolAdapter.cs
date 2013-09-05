using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Http2.Protocol.Utils;

namespace ProtocolAdapters
{
    public class Http11ClientProtocolAdapter : IDisposable
    {
        private readonly Stream _client;
        private readonly string _path;
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));

        public Http11ClientProtocolAdapter(Stream clientStream, string path)
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

                var nameValue = rawHeaders[i].Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                switch (nameValue.Length)
                {
                    case 1:
                        name = nameValue[0].Trim();
                        break;
                    case 2:
                        name = nameValue[0].Trim();
                        value = nameValue[1].Trim();
                        break;
                    default:
                        break;
                }

                if (!respHeaders.ContainsKey(name))
                    respHeaders.Add(name, value);
            }

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
            }
            return new KeyValuePair<IDictionary<string, string>, byte[]>(respHeaders, respBody);
        }

        public void HandleHttp11Response(byte[] responseBinaryHeaders, int offset, int length)
        {
            var bytes = new byte[responseBinaryHeaders.Length];
            Buffer.BlockCopy(responseBinaryHeaders, 0, bytes, 0, bytes.Length);
            var response = ParseHeadersAndReadResponseBody(bytes);
            //TODO Handle headers somehow if it's needed
            using (var stream = new FileStream(AssemblyName + _path, FileMode.Create))
            {
                stream.Write(response.Value, 0, response.Value.Length);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
