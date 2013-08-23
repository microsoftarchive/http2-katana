using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using Owin.Types;
using SharedProtocol.Http11;
using SharedProtocol;
using SharedProtocol.IO;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Environment = IDictionary<string, object>;

    public static class Http11ProtocolOwinAdapter
    {
        public static async Task ProcessRequest(DuplexStream client, Environment environment, AppFunc next)
        {
            try
            {
                if (!client.CanRead) return;

 
                // TODO check for upgrade and add coresponding settings

                // upgrade 1.1->2.0 detection must be there

                string[] rawHeaders = Http11Manager.GetHttp11Headers(client);
                // TODO - review if OwinRequest constructor can do this for us.
 
                // headers[0] contains METHOD, URI and VERSION like "GET /api/values/1 HTTP/1.1"
                var headers = ParseHeaders(rawHeaders.Skip(1).ToArray());

                string[] splittedRequestString = rawHeaders[0].Split(' ');
                string method = splittedRequestString[0];
                string scheme = client.Socket.SecureProtocol == SecureProtocol.None ? "http" : "https";
                string host = headers["Host"][0]; // client MUST include Host header due to HTTP/1.1 spec
                if (host.IndexOf(':') == -1)
                {
                    host += ":80"; // use default port
                }

                string path = splittedRequestString[1];

                // TODO get body from request
                var env = CreateOwinEnvironment(method, scheme, host, "", path);

                await next(env);

                EndResponse(client, env);
            }
            catch (Exception ex)
            {
                EndResponse(client, ex);
            }
        }

        // TODO find better way
        private static IDictionary<string, string[]> ParseHeaders(string[] headers)
        {
            var dict = new Dictionary<string, string[]>();
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

        private static void EndResponse(DuplexStream socket, Exception ex)
        {
            var msgBytes = Encoding.UTF8.GetBytes(ex.Message);
            // todo add content type
            Http11Manager.SendResponse(socket, msgBytes, StatusCode.Code500InternalServerError, "");
            socket.Flush();
        }

        private static Dictionary<string, object> CreateOwinEnvironment(string method, string scheme, string hostHeaderValue, string pathBase, string path, byte[] requestBody = null)
        {
            var environment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            environment["owin.RequestMethod"] = method;
            environment["owin.RequestScheme"] = scheme;
            environment["owin.RequestHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "Host", new string[] { hostHeaderValue } } };
            environment["owin.RequestPathBase"] = pathBase;
            environment["owin.RequestPath"] = path;
            environment["owin.RequestQueryString"] = "";
            environment["owin.RequestBody"] = new MemoryStream(requestBody ?? new byte[0]);
            environment["owin.CallCancelled"] = new CancellationToken();
            environment["owin.ResponseHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment["owin.ResponseBody"] = new MemoryStream();
            return environment;
        }

        private static void EndResponse(DuplexStream socket, IDictionary<string, object> env)
        {
            OwinResponse owinResponse = new OwinResponse(env);

            // TODO constants, better check
            var bytes = (owinResponse.Body as MemoryStream).ToArray();

            Http11Manager.SendResponse(socket, bytes, owinResponse.StatusCode, owinResponse.ContentType);

            socket.Flush();

        }
    }
}
