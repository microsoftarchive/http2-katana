using System.Net.Sockets;
using ClientHandler.Transport;
using ClientProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using SharedProtocol;

namespace ClientHandler
{
    // Manages HTTP/2.0 using the System.Net.Http object model.
    // Note: This could also fall back to HTTP/1.1, but that's out of scope for this project.
    public class Http2SessionHandler : HttpMessageHandler
    {
        private SemaphoreSlim _connectingLock = new SemaphoreSlim(1, 1000);
        private SecureSocket _socket;
        private SecurityOptions _options;
        private Http2ClientSession _clientSession;
        private bool _do11Handshake;
        private HttpMessageInvoker _11fallbackInvoker;
        private bool _fallbackTo11;
        private string certificatePath = @"certificate.pfx";

        public Http2SessionHandler(bool do11Handshake, HttpMessageHandler fallbackHandler)
            : this(do11Handshake, fallbackHandler, new SocketConnectionResolver())
        {
        }

        public Http2SessionHandler(bool do11Handshake, HttpMessageHandler fallbackHandler, IConnectionResolver connectionResolver)
        {
            // TODO: Will we need a connection resolver that understands proxies?       
            
            ExtensionType[] extensions = new ExtensionType[] { ExtensionType.Renegotiation, ExtensionType.ALPN };

            _options = new SecurityOptions(SecureProtocol.Tls1, extensions, ConnectionEnd.Client);
            _options.VerificationType = CredentialVerification.None;
            _options.Certificate = Org.Mentalis.Security.Certificates.Certificate.CreateFromCerFile(certificatePath);
            _options.Flags = SecurityFlags.Default;
            _options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;

            _socket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, _options);
            _do11Handshake = do11Handshake;

            _11fallbackInvoker = new HttpMessageInvoker(fallbackHandler, disposeHandler: false);
            _fallbackTo11 = false;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_fallbackTo11)
            {
                return await _11fallbackInvoker.SendAsync(request, cancellationToken);
            }

            Http2ClientStream stream;
            int? streamId;
            // Open the session if it hasn't been.
            if (await ConnectIfNeeded(request, cancellationToken))
            {
                Console.WriteLine("Submitting request");
                stream = SubmitRequest(request, cancellationToken).Result;
                // This request was submitted during the handshake as StreamId 1
                //stream = _clientSession.GetStream(1);
            }
            else if (_fallbackTo11 || _clientSession == null)
            {
                // The handshake failed, or this request wasn't handshake compatible (e.g. upload).
                return await _11fallbackInvoker.SendAsync(request, cancellationToken);
            }
            // Verify the requested resource has not been pushed by the server
            else if ((streamId = CheckForPendingResource(request)).HasValue)
            {
                // TODO: This code path will actually be quite different from the normal flow.
                // The response has already arrived (as a HeadersPlusPriorityFrame), we can't submit a body,
                // and we have to decompress the headers to even execute CheckForPendeingResource.
                throw new NotImplementedException();
            }
            // Submit the request and start uploading any data. (What about 100-Continues?)
            else
            {
                stream = await SubmitRequest(request, cancellationToken);
            }
            
            // Build the response
            return await GetResponseAsync(stream);
        }

        // Returns true if this request was used to perform the initial handshake and does not need to be submitted separately.
        private async Task<bool> ConnectIfNeeded(HttpRequestMessage request, CancellationToken cancel)
        {
            SecureSocket sessionSocket = null;
            // Async lock, only one at a time.
            await _connectingLock.WaitAsync();
            try
            {
                if (_clientSession == null)
                {
                    if (_do11Handshake && !IsHanshakeCompatableRequest(request))
                    {
                        return false;
                    }

                    Uri requestUri = request.RequestUri;

                    using (SecureHandshaker handshaker = new SecureHandshaker(_options, requestUri))
                    {
                        handshaker.Handshake();
                        sessionSocket = handshaker.InternalSocket;
                    }

                    Console.WriteLine("Handshake finished");

                    _clientSession = new Http2ClientSession(sessionSocket, CancellationToken.None);

                    // TODO: Listen to task for errors? Dispose/Abort CT?
                    Task pumpTasks = _clientSession.Start();

                    _connectingLock.Release(999); // Unblock all, this method no longer needs to be one at a time.
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                if (sessionSocket != null)
                {
                    sessionSocket.Close();
                }
                if (_clientSession != null)
                {
                    _clientSession.Dispose();
                    _clientSession = null;
                }
                throw;
            }
            finally
            {
                _connectingLock.Release();
            }
        }

        // TODO: What are the official handshake restrictions?
        private bool IsHanshakeCompatableRequest(HttpRequestMessage request)
        {
            return request.Method.Equals(HttpMethod.Get);
        }

        private SecureSocket ConnectAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            Uri requestUri = request.RequestUri;

            using (SecureHandshaker handshaker = new SecureHandshaker(_options, requestUri))
            {
                handshaker.Handshake();
                _socket = handshaker.InternalSocket;
            }

            return _socket;
        }

        // Verify the requested resource has not been pushed by the server
        private int? CheckForPendingResource(HttpRequestMessage request)
        {
            return null;
            // Keyed on Uri, Method, Version, Cert
            // throw new NotImplementedException();
        }

        private async Task<Http2ClientStream> SubmitRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Dictionary<string,string> pairs = new Dictionary<string, string>(10);
            string method = request.Method.ToString();
            string path = request.RequestUri.PathAndQuery;
            string version = "HTTP/" + request.Version.ToString(2);
            string scheme = request.RequestUri.Scheme;
            string host = request.Headers.Host
                ?? request.RequestUri.GetComponents(UriComponents.Host | UriComponents.StrongPort, UriFormat.UriEscaped);
            request.Headers.Host = null;

            pairs.Add(":method", method);
            pairs.Add(":path", path);
            pairs.Add(":version", version);
            pairs.Add(":host", host);
            pairs.Add(":scheme", scheme);

            foreach (KeyValuePair<string, IEnumerable<string>> pair in request.Headers)
            {
                pairs.Add(pair.Key.ToLowerInvariant(), string.Join("\0", pair.Value));
            }
            if (request.Content != null)
            {
                // Compute lazy content-length via TryComputeLength
                request.Content.Headers.ContentLength = request.Content.Headers.ContentLength;

                // TODO: De-dupe custom headers between request and content.
                foreach (KeyValuePair<string, IEnumerable<string>> pair in request.Content.Headers)
                {
                    pairs.Add(pair.Key.ToLowerInvariant(), string.Join("\0", pair.Value));
                }
            }

            X509Certificate clientCert = GetClientCert(request);

            // Set priority from request.Properties
            object obj;
            int priority = 3; // Neutral by default
            if (request.Properties.TryGetValue("http2.Priority", out obj) && obj is int)
            {
                priority = (int)obj;
                Contract.Assert(priority >= 0 && priority <= 7);
            }

            // TODO: How do I correctly determine if a request has a body?
            bool hasRequestBody = (request.Content != null);

            Http2ClientStream clientStream = _clientSession.SendRequest(pairs, clientCert, priority, hasRequestBody, cancellationToken);            
            
            // TODO: Upload
            if (hasRequestBody)
            {
                // TODO: 100-Continues?
                // TODO: no-blocking upload so we can start consuming the response in parallel?
                Stream requestBody = clientStream.RequestStream;
                await request.Content.CopyToAsync(requestBody);
                // TODO: Send trailer headers
                // Send FIN frame
                //clientStream.EndRequest();
            }

            return clientStream;
        }

        private X509Certificate GetClientCert(HttpRequestMessage request)
        {
            // From the request.Properties?
            // throw new NotImplementedException();
            return null;
        }

        private async Task<HttpResponseMessage> GetResponseAsync(Http2ClientStream stream)
        {
            // TODO: 100 continue? Start data upload (on separate thread so we can do bidirectional).

            // Set up a response content object to receive the reply data.
            IList<KeyValuePair<string, string>> pairs = await stream.GetResponseAsync();
            StreamContent streamContent = new StreamContent(stream.ResponseStream);
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = streamContent;

            // Distribute headers
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                Console.WriteLine("Key: {0}, Value: {1}", pair.Key, pair.Value);
                if (pair.Key[0] == ':')
                {
                    MapResponseProperty(response, pair.Key, pair.Value);
                }
                else
                {
                    // Null separated list of values
                    string[] values = pair.Value.Split('\0');
                    if (!response.Headers.TryAddWithoutValidation(pair.Key, values))
                    {
                        if (!response.Content.Headers.TryAddWithoutValidation(pair.Key, values))
                        {
                            throw new NotSupportedException("Bad header: " + pair.Key);
                        }
                    }
                }
            }

            // TODO: Associate with the original HttpRequestMessage
            // TODO: How do we receive trailer headers?
            // TODO: How do we receive (or discard) pushed resources?
            return response;
        }

        // HTTP/2.0 sends HTTP/1.1 response properties like status, version, etc. as headers prefixed with ':'
        private static void MapResponseProperty(HttpResponseMessage response, string key, string value)
        {
            // keys are required to be lower case
            if (":status".Equals(key, StringComparison.Ordinal))
            {
                string statusCode = value;
                int split = value.IndexOf(' ');
                if (split > 0)
                {
                    response.ReasonPhrase = value.Substring(split + 1);
                    statusCode = value.Substring(0, split);
                }
                response.StatusCode = (HttpStatusCode)Int32.Parse(statusCode);
            }
            else if (":version".Equals(key, StringComparison.Ordinal))
            {
                Contract.Assert(value.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase));
                string version = value.Substring("HTTP/".Length);
                response.Version = Version.Parse(version);
            }
        }

        // Verify at least the minimum request properties were set:
        // Status, version
        private static void VerifyRequiredRequestsPropertiesSet(HttpResponseMessage response)
        {
            // Set bitflags in MapRequestProperty?
            // TODO:
            // throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (_clientSession != null)
            {
                _clientSession.Dispose();
            }
            _connectingLock.Dispose();
            base.Dispose(disposing);
        }
    }
}
