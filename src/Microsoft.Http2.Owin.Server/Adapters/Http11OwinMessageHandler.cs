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

namespace Microsoft.Http2.Owin.Server.Adapters
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Environment = IDictionary<string, object>;
    using UpgradeDelegate = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    /// <summary>
    /// This class overrides http11 request/response processing logic as owin requires
    /// Converts request to OWIN environment, triggers OWIN pipilene and then sends response back to the client.
    /// </summary>
    public class Http11ProtocolOwinAdapter
    {
        private readonly Stream _client;
        private readonly SecureProtocol _protocol;
        private readonly AppFunc _next;
        private Environment _environment;
        private IOwinRequest _request;
        private IOwinResponse _response;
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

                var rawHeaders = Http11Helper.ReadHeaders(_client);
                
                Http2Logger.LogDebug("Http1.1 Protocol Handler. Process request " + string.Join(" ", rawHeaders));
                
                // invalid connection, skip
                if (rawHeaders == null || rawHeaders.Length == 0) return;

                // headers[0] contains METHOD, URI and VERSION like "GET /api/values/1 HTTP/1.1"
                var headers = Http11Helper.ParseHeaders(rawHeaders.Skip(1).ToArray());

                // client MUST include Host header due to HTTP/1.1 spec 
                if (!headers.ContainsKey("Host"))
                {
                    throw new ApplicationException("Host header is missing");
                }

                // parse request parameters: method, path, host, etc
                var splittedRequestString = rawHeaders[0].Split(' ');
                var method = splittedRequestString[0];

                if (!IsMethodSupported(method))
                {
                    throw new NotSupportedException(method + " method is not currently supported via HTTP/1.1");
                }

                var scheme = _protocol == SecureProtocol.None ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;

                var path = splittedRequestString[1];

                // main OWIN environment components
                // OWIN request and response below shares the same environment dictionary instance
                _environment = CreateOwinEnvironment(method, scheme, "", path, headers);
                _request = new OwinRequest(_environment);
                _response = new OwinResponse(_environment);

                // we may need to populate additional fields if request supports UPGRADE
                AddOpaqueUpgradeIfNeeded();

                await _next(_environment);

                if (_opaqueCallback == null)
                {
                    EndResponse();
                }
                else
                {
                    // do not close connection
                    EndResponse(false);

                    var opaqueEnvironment = CreateOpaqueEnvironment();
                    await _opaqueCallback(opaqueEnvironment);
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
            var supported = new[] {Verbs.Get, Verbs.Delete };

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
            _response.StatusCode = StatusCode.Code101SwitchingProtocols;
            _response.ReasonPhrase = StatusCode.Reason101SwitchingProtocols;
            _response.Protocol = Protocols.Http1;
            _response.Headers.Add(CommonHeaders.Connection, new[] {CommonHeaders.Upgrade});

            _opaqueCallback = opaqueCallback;
        }

        /// <summary>
        /// Inspects request headers to see if supported UPGRADE method is defined; if so, specifies Opague.Upgarde delegate as per 'OWIN Opaque Stream Extension'.
        /// </summary>
        private void AddOpaqueUpgradeIfNeeded()
        {

            var headers = _request.Headers;

            if (headers.ContainsKey(CommonHeaders.Connection) && headers.ContainsKey(CommonHeaders.Upgrade))
            {
                _environment[CommonOwinKeys.OpaqueUpgrade] = new UpgradeDelegate(OpaqueUpgradeDelegate);
            }
       
        }

        /// <summary>
        /// Creates new Opaque Environment dictionary.
        /// </summary>
        /// <returns>New instance of the Opaque Environment dictionary</returns>
        private Environment CreateOpaqueEnvironment()
        {
            var env = new Dictionary<string, object>(StringComparer.Ordinal);
            env[CommonOwinKeys.OpaqueStream] = _client;
            env[CommonOwinKeys.OpaqueVersion] = CommonOwinKeys.OwinVersion;
            env[CommonOwinKeys.OpaqueCallCancelled] = new CancellationToken();

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

            Http11Helper.SendResponse(_client, new byte[0], statusCode);
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

            Http11Helper.SendResponse(_client, bytes, _response.StatusCode, _response.Headers, closeConnection);

            if (closeConnection)
            {
                Http2Logger.LogDebug("Closing connection");
                _client.Close();
            }
        }

        /// <summary>
        /// Creates request OWIN respresentation. 
        /// </summary>
        /// <param name="method">The request method.</param>
        /// <param name="scheme">The request scheme.</param>
        /// <param name="pathBase">The request base path.</param>
        /// <param name="path">The request path.</param>
        /// <param name="headers">The request headers.</param>
        /// <param name="queryString">The request query string</param>
        /// <param name="requestBody">The body of request.</param>
        /// <returns>OWIN representation for provided request parameters.</returns>
        private static Dictionary<string, object> CreateOwinEnvironment(string method, string scheme, string pathBase, 
                                                                        string path, IDictionary<string, string[]> headers, string queryString = "", byte[] requestBody = null)
        {
            var environment = new Dictionary<string, object>(StringComparer.Ordinal);
            environment[CommonOwinKeys.OwinCallCancelled] = new CancellationToken();

            #region OWIN request params

            var request = new OwinRequest(environment)
                {
                    Method = method,
                    Scheme = scheme,
                    Path = new PathString(path),
                    PathBase = new PathString(pathBase),
                    QueryString = new QueryString(queryString),
                    Body = new MemoryStream(requestBody ?? new byte[0]),
                    Protocol = Protocols.Http1
                };

            // request.Headers is readonly
            request.Set(CommonOwinKeys.RequestHeaders, headers);

            #endregion


            #region set default OWIN response params

            var response = new OwinResponse(environment) {Body = new MemoryStream(), StatusCode = StatusCode.Code200Ok};
            //response.Headers is readonly
            response.Set(CommonOwinKeys.ResponseHeaders, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

            #endregion


            return environment;
        }
        
    }
}