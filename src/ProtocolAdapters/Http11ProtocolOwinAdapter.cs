using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http1.Protocol;
using Microsoft.Owin;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Utils;
using StatusCode = Microsoft.Http2.Protocol.StatusCode;

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
        private OwinRequest _request;
        private OwinResponse _response;
        private AppFunc _opaqueCallback;

        /// <summary>
        /// Creates new isntaces of the Http11ProtocolOwinAdapter class
        /// </summary>
        /// <param name="client">The client connection.</param>
        /// <param name="protocol">Security protocol which is used for connection.</param>
        /// <param name="next">The next component in the OWIN pipeline.</param>
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
        public async void ProcessRequest()
        {
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
                var splittedRequestString = rawHeaders[0].Split(' ');
                var method = splittedRequestString[0];

                if (!IsMethodSupported(method))
                {
                    throw new NotSupportedException(method + " method is not currently supported via HTTP/1.1");
                }

                var scheme = _protocol == SecureProtocol.None ? "http" : "https";
                var host = headers[":host"][0]; // client MUST include Host header due to HTTP/1.1 spec
                if (host.IndexOf(':') == -1)
                {
                    host += (scheme == "http" ? ":80" : ":443"); // use default port
                }
                headers[":host"] = new[] {host};

                var path = splittedRequestString[1];

                // main owin environment components
                _environment = CreateOwinEnvironment(method, scheme, "", path, headers);
                
               // OWIN request and reponse below shares the same environment dictionary 
                _request = new OwinRequest(_environment);
                _response = new OwinResponse(_environment);

                // we may need to populate additional fields if request supports UPGRADE
                AddOpaqueUpgradeIfNeeded();
                
                if (_next != null)
                {
                    await _next(_environment);
                }

                if (_opaqueCallback == null)
                {
                    EndResponse();
                }
                else
                {
                    // do not close connection
                    EndResponse(false);

                    var opaqueEnvironment = CreateOpaqueEnvironment();
                    _opaqueCallback(opaqueEnvironment);
                }
            }
            catch (Exception ex)
            {

                Http2Logger.LogError(ex.Message);
                EndResponse(ex);
                Http2Logger.LogDebug("Closing connection");
                _client.Close();
            }
        }

        /// <summary>
        /// Checks if request method is supported by current protocol implementation.
        /// </summary>
        /// <param name="method">Http method to perform check for.</param>
        /// <returns>True if method is supported, otherwise False.</returns>
        private static bool IsMethodSupported(string method)
        {
            var supported = new[] {"GET","DELETE" };

            return supported.Contains(method.ToUpper());
        }

        #region Opague Upgrade

        /// <summary>
        /// Implements OWIN upgrade delegate according to 'OWIN Opaque Stream Extension'.
        /// http://owin.org/extensions/owin-OpaqueStream-Extension-v0.3.0.htm
        /// </summary>
        /// <param name="settings">The opaque parameters. Not currently used.</param>
        /// <param name="opaqueCallback">The OpaqueFunc callback</param>
        private void OpaqueUpgradeDelegate(Environment settings, AppFunc opaqueCallback)
        {
            // TODO 101 to constants
            _response.StatusCode = 101;
            _response.ReasonPhrase = "Switching Protocols"; // TODO is ReasonPhrase necessary here
            _response.Protocol = "HTTP/1.1";
            _response.Headers.Add("Connection", new[] {"Upgrade"});
            _response.Headers.Add("Upgrade", new[] {Protocols.Http2});

            _opaqueCallback = opaqueCallback;
        }

        /// <summary>
        /// Inspects request headers to see if supported UPGRADE method is defined; if so, specifies Opague.Upgarde delegate as per 'OWIN Opaque Stream Extension'.
        /// </summary>
        private void AddOpaqueUpgradeIfNeeded()
        {

            var headers = _request.Headers;

            if (headers.ContainsKey("Connection") && headers.ContainsKey("Upgrade"))
            {
                _environment["opaque.Upgrade"] = new UpgradeDelegate(OpaqueUpgradeDelegate);
            }
       
        }

        /// <summary>
        /// Creates new Opaque Environment dictionary.
        /// </summary>
        /// <returns>New instance of the Opaque Environment dictionary</returns>
        private Environment CreateOpaqueEnvironment()
        {
            var env = new Dictionary<string, object>(StringComparer.Ordinal);
            env["opaque.Stream"] = _client;
            env["opaque.Version"] = "1.0";
            env["opaque.CallCancelled"] = new CancellationToken();

            env["owin.RequestHeaders"] = _request.Headers;
            env["owin.RequestPath"] = _request.Path;
            env["owin.RequestPathBase"] = _request.PathBase;
            env["owin.RequestMethod"] = _request.Method;
            env["owin.RequestProtocol"] = _request.Protocol;
            env["owin.RequestScheme"] = _request.Scheme;
            env["owin.RequestBody"] = _request.Body;
            env["owin.RequestQueryString"] = _request.QueryString;

            return env;
        }

        #endregion

        /// <summary>
        /// Completes response by sending result back to the client.
        /// </summary>
        /// <param name="ex">The error occured.</param>
        private void EndResponse(Exception ex)
        {
            int statusCode = (ex is NotSupportedException) ? StatusCode.Code501NotImplemented : StatusCode.Code500InternalServerError;

            Http11Manager.SendResponse(_client, new byte[0], statusCode, ContentTypes.TextPlain);
        }

        private void EndResponse(bool closeConnection = true)
        {
            byte[] bytes;

            // response body
            if (_response.Body is MemoryStream)
            {
                bytes = (_response.Body as MemoryStream).ToArray();
            }
            else
            {
                bytes = new byte[0];
            }

            Http11Manager.SendResponse(_client, bytes, _response.StatusCode, _response.ContentType, _response.Headers, closeConnection);

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
        /// <param name="pathBase">The request base path.</param>
        /// <param name="path">The request path.</param>
        /// <param name="headers">The request headers.</param>
        /// <param name="requestBody">The body of request.</param>
        /// <returns>OWIN representation for provided request parameters.</returns>
        private static Dictionary<string, object> CreateOwinEnvironment(string method, string scheme, string pathBase, 
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