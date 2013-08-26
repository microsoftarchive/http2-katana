using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;
using Owin.Types;
using SharedProtocol.Http11;
using SharedProtocol;
using SharedProtocol.IO;
using SharedProtocol.Utils;
using SharedProtocol.Handshake;

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Environment = IDictionary<string, object>;
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    /// <summary>
    /// Implements Http11 protocol handler.
    /// Converts request to OWIN environment, triggers OWIN pipilene and then sends response back to the client.
    /// </summary>
    public static class Http11ProtocolOwinAdapter
    {
        /// <summary>
        /// Request parameters read timeout.
        /// </summary>
        private const int ReadTimeout = 6000; // TODO use global constants instead

        /// <summary>
        /// Processes incoming request.
        /// </summary>
        /// <param name="client">The client connection.</param>
        /// <param name="environment">The OWIN environment</param>
        /// <param name="next">The next component in the pipeline.</param>
        public static async void ProcessRequest(DuplexStream client, Environment environment, AppFunc next)
        {
            // args checking
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            try
            {
                // invalid connection, skip
                if (!client.CanRead || !client.WaitForDataAvailable(ReadTimeout)) return;

                var rawHeaders = Http11Manager.ReadHeaders(client);
                // TODO - review if OwinRequest constructor can do this for us.

                Http2Logger.LogDebug("Http1.1 Protocol Handler. Process request " + string.Join(" ", rawHeaders));
                
                // invalid connection, skip
                if (rawHeaders == null || rawHeaders.Length == 0) return;

                // headers[0] contains METHOD, URI and VERSION like "GET /api/values/1 HTTP/1.1"
                var headers = Http11Manager.ParseHeaders(rawHeaders.Skip(1).ToArray());

                // parse request parameters: method, path, host, etc
                string[] splittedRequestString = rawHeaders[0].Split(' ');
                string method = splittedRequestString[0];
                string scheme = client.Socket.SecureProtocol == SecureProtocol.None ? "http" : "https";
                string host = headers["Host"][0]; // client MUST include Host header due to HTTP/1.1 spec
                if (host.IndexOf(':') == -1)
                {
                    host += (scheme == "http" ? ":80" : ":443"); // use default port
                }
                string path = splittedRequestString[1];

                // TODO get body from request

                // main owin environment components
                var env = CreateOwinEnvironment(method, scheme, host, "", path);

                // we may need to populate additional fields if request supports UPGRADE
                AddOpaqueUpgradeIfNeeded(headers, env);
                
                if (next != null)
                {
                    await next(env);
                }

                EndResponse(client, env);
            }
            catch (Exception ex)
            {
                EndResponse(client, ex);
            }
            finally
            {
                client.Close();
            }
        }

        #region Opague Upgrade

        /// <summary>
        /// Implements Http1 to Http2 apgrade delegate according to 'OWIN Opaque Stream Extension'.
        /// </summary>
        /// <param name="env">The OWIN environment.</param>
        /// <param name="next">The callback delegate to be executed after Opague Upgarde is done.</param>
        private static void OpaqueUpgradeDelegate(Environment env, AppFunc next)
        {
            try
            {
                // TODO UpgradeHandshaker.Handshake method must be refactored
                UpgradeHandshaker.Handshake(env);
            }
            finally
            {
                next(env);
            }
        }

        /// <summary>
        /// Inspects request headers to see if supported UPGRADE method is defined; if so, specifies Opague.Upgarde delegate as per 'OWIN Opaque Stream Extension'.
        /// </summary>
        /// <param name="headers">Parsed request headers.</param>
        /// <param name="env">The OWIN request paremeters to update.</param>
        private static void AddOpaqueUpgradeIfNeeded(IDictionary<string, string[]> headers, IDictionary<string, object> env)
        {
            if (!headers.ContainsKey("Upgrade") || !headers.ContainsKey("HTTP2-Settings"))
            {
                return;
            }

            // make sure we are upgrading to HTTP/2.0 protocol (version may also be HTTP-draft-04/2.0 or similar)
            if (headers["Upgrade"].FirstOrDefault(it =>
                it.ToUpper().IndexOf("HTTP", StringComparison.Ordinal) != -1 &&
                it.IndexOf("2.0", StringComparison.Ordinal) != -1) != null)
            {
                env[OwinConstants.Opaque.Upgrade] = new UpgradeDelegate(OpaqueUpgradeDelegate);
            }

        }

        #endregion

        /// <summary>
        /// Completes response by sending result back to the client.
        /// </summary>
        /// <param name="client">The client connection.</param>
        /// <param name="ex">The error occured.</param>
        private static void EndResponse(DuplexStream client, Exception ex)
        {
            Http11Manager.SendResponse(client, Encoding.UTF8.GetBytes(ex.Message), StatusCode.Code500InternalServerError, ""); // TODO content-type ??
            client.Flush();
        }

        /// <summary>
        /// Completes response by sending result back to the client.
        /// </summary>
        /// <param name="client">The client connection./param>
        /// <param name="env">Reponse instance in form of OWIN dictionaty.</param>
        private static void EndResponse(DuplexStream client, IDictionary<string, object> env)
        {
            var owinResponse = new OwinResponse(env);
            
            byte[] bytes;

            // response body
            if (owinResponse.Body is MemoryStream)
            {
                bytes = (owinResponse.Body as MemoryStream).ToArray();
            }
            else
            {
                bytes = new byte[0];
            }

            // response headers
            var headers = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string[]> pair in owinResponse.Headers)
            {
                var key = pair.Key.ToLowerInvariant();

                // these two are special and passed separatly// TODO move this check to httpmanager
                if (key == "content-length" || key == "content-type") continue;

                headers[key] = string.Join("\0", pair.Value);
            }

            Http11Manager.SendResponse(client, bytes, owinResponse.StatusCode, owinResponse.ContentType, headers);

            client.Flush();
        }

        /// <summary>
        /// Creates request OWIN respresentation. 
        /// TODO move to utils or base class since it could be shared across http11 and http2 protocol adapters.
        /// </summary>
        /// <param name="method">The request method.</param>
        /// <param name="scheme">The request scheme.</param>
        /// <param name="hostHeaderValue">The request host.</param>
        /// <param name="pathBase">The request base path.</param>
        /// <param name="path">The request path.</param>
        /// <param name="requestBody">The body of request.</param>
        /// <returns>OWIN representation for provided request parameters.</returns>
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
        
    }
}