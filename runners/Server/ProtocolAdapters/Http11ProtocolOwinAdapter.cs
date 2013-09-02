using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.Http11;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.IO;
using Microsoft.Http2.Protocol.Utils;

namespace ProtocolAdapters
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Environment = IDictionary<string, object>;
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    /// <summary>
    /// Implements Http11 protocol handler.
    /// Converts request to OWIN environment, triggers OWIN pipilene and then sends response back to the client.
    /// </summary>
    public class Http11ProtocolOwinAdapter
    {
        private readonly Stream _client;
        private readonly SecureProtocol _protocol;
        private readonly AppFunc _next;
        private Environment _environment;
        private AppFunc _opaqueCallback = null;

        public Http11ProtocolOwinAdapter(Stream client, SecureProtocol protocol, AppFunc next)
        {
            // args checking
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            _client = client;
            _protocol = protocol;
            _next = next;
        }

        /// <summary>
        /// Processes incoming request.
        /// </summary>
        /// <param name="client">The client connection.</param>
        /// <param name="protocol">Security protocol which is used for connection.</param>
        /// <param name="next">The next component in the pipeline.</param>
        public async void ProcessRequest()
        {
            // args checking
            if (_client == null)
            {
                throw new ArgumentNullException("client");
            }

            try
            {
                // invalid connection, skip
                if (!_client.CanRead) return;

                var rawHeaders = Http11Manager.ReadHeaders(_client);
                
                Http2Logger.LogDebug("Http1.1 Protocol Handler. Process request " + string.Join(" ", rawHeaders));
                
                // invalid connection, skip
                if (rawHeaders == null || rawHeaders.Length == 0) return;

                // headers[0] contains METHOD, URI and VERSION like "GET /api/values/1 HTTP/1.1"
                var headers = Http11Manager.ParseHeaders(rawHeaders.Skip(1).ToArray());

                // parse request parameters: method, path, host, etc
                string[] splittedRequestString = rawHeaders[0].Split(' ');
                string method = splittedRequestString[0];

                if (!IsMethodSupported(method))
                {
                    throw new NotSupportedException(method + " method is not currently supported via HTTP/1.1");
                }

                string scheme = _protocol == SecureProtocol.None ? "http" : "https";
                string host = headers["Host"][0]; // client MUST include Host header due to HTTP/1.1 spec
                if (host.IndexOf(':') == -1)
                {
                    host += (scheme == "http" ? ":80" : ":443"); // use default port
                }
                string path = splittedRequestString[1];

                // main owin environment components
                _environment = CreateOwinEnvironment(method, scheme, host, "", path, headers);

                // we may need to populate additional fields if request supports UPGRADE
                AddOpaqueUpgrade();
                
                if (_next != null)
                {
                    await _next(_environment);
                }

                if (_opaqueCallback == null)
                {
                    EndResponse(new OwinResponse(_environment));
                }
                else
                {
                    var request = new OwinRequest(_environment);
                    EndResponse(new OwinResponse(_environment), false);
                    var env = new Dictionary<string, object>();
                    env["opaque.Stream"] = _client;
                    env["opaque.Version"] = "1.0";
                    env["opaque.CallCancelled"] = new CancellationToken();

                    env["owin.RequestHeaders"] = request.Headers;
                    env["owin.RequestPath"] = request.Path;
                    env["owin.RequestPathBase"] = request.PathBase;
                    env["owin.RequestMethod"] = request.Method;
                    env["owin.RequestProtocol"] = request.Protocol;
                    env["owin.RequestScheme"] = request.Scheme;
                    env["owin.RequestBody"] = request.Body;
                    env["owin.RequestQueryString"] = request.QueryString;

                    _opaqueCallback(env);
                }
            }
            catch (Exception ex)
            {
                EndResponse(_client, ex);
                Http2Logger.LogDebug("Closing connection");
                _client.Close();
            }
        }

        /// <summary>
        /// Checks if request method is supported by current protocol implementation.
        /// </summary>
        /// <param name="method">Http method to perform check for.</param>
        /// <returns>True if method is supported, otherwise False.</returns>
        private bool IsMethodSupported(string method)
        {
            var supported = new[] {"GET","DELETE" };

            return supported.Contains(method.ToUpper());
        }

        #region Opague Upgrade

        /// <summary>
        /// Implements Http1 to Http2 apgrade delegate according to 'OWIN Opaque Stream Extension'.
        /// </summary>
        /// <param name="env">The OWIN environment.</param>
        /// <param name="next">The callback delegate to be executed after Opague Upgarde is done.</param>
        private void OpaqueUpgradeDelegate(Environment settings, AppFunc opaqueCallback)
        {
            var resp = new OwinResponse(_environment)
            {
                StatusCode = 101,
                Protocol = "HTTP/1.1"
            };
            resp.Headers.Add("Connection", new[] {"Upgrade"});
            resp.Headers.Add("Upgrade", new[] {Protocols.Http2});

            _opaqueCallback = opaqueCallback;
        }

        /// <summary>
        /// Inspects request headers to see if supported UPGRADE method is defined; if so, specifies Opague.Upgarde delegate as per 'OWIN Opaque Stream Extension'.
        /// </summary>
        /// <param name="headers">Parsed request headers.</param>
        /// <param name="env">The OWIN request paremeters to update.</param>
        private void AddOpaqueUpgrade()
        {
            _environment["opaque.Upgrade"] = new UpgradeDelegate(OpaqueUpgradeDelegate);
        }

        #endregion

        /// <summary>
        /// Completes response by sending result back to the client.
        /// </summary>
        /// <param name="client">The client connection.</param>
        /// <param name="ex">The error occured.</param>
        private void EndResponse(Stream client, Exception ex)
        {
            int statusCode = (ex is NotSupportedException) ? StatusCode.Code501NotImplemented : StatusCode.Code500InternalServerError;

            Http11Manager.SendResponse(client, new byte[0], statusCode, ContentTypes.TextPlain); 
            client.Flush();
        }

        private void EndResponse(OwinResponse owinResponse, bool closeConnection = true)
        {
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

            Http11Manager.SendResponse(_client, bytes, owinResponse.StatusCode, owinResponse.ContentType, owinResponse.Headers, closeConnection);

            _client.Flush();

            if (closeConnection)
            {
                Http2Logger.LogDebug("Closing connection");
                _client.Close();
            }
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
        private Dictionary<string, object> CreateOwinEnvironment(string method, string scheme, string hostHeaderValue, string pathBase, 
                                                                        string path, IDictionary<string, string[]> headers, byte[] requestBody = null)
        {
            var environment = new Dictionary<string, object>(StringComparer.Ordinal);
            environment["owin.RequestMethod"] = method;
            environment["owin.RequestScheme"] = scheme;
            environment["owin.RequestHeaders"] = headers;
            environment["owin.RequestPathBase"] = pathBase;
            environment["owin.RequestPath"] = path;
            environment["owin.RequestQueryString"] = "";
            environment["owin.RequestBody"] = new MemoryStream(requestBody ?? new byte[0]);

            environment["owin.CallCancelled"] = new CancellationToken();

            environment["owin.ResponseHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment["owin.ResponseBody"] = new MemoryStream();
            environment["owin.ResponseStatusCode"] = StatusCode.Code200Ok;
            environment["owin.ResponseProtocol"] = "HTTP/1.1";

            return environment;
        }
        
    }
}