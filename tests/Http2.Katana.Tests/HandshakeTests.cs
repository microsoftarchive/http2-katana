using System.Globalization;
using System.IO;
using Http2.TestClient.Handshake;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.Middleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Tests;
using System;
using System.Collections.Generic;
using System.Configuration;
using OpenSSL.SSL;
using Xunit;

namespace Http2.Katana.Tests
{
    public class HandshakeSetup : IDisposable
    {
        public HttpSocketServer Http2SecureServer { get; private set; }
        public HttpSocketServer Http2UnsecureServer { get; private set; }

        private IDictionary<string, object> GetProperties(bool useSecurePort)
        {
            var appSettings = ConfigurationManager.AppSettings;

            string address = useSecurePort ? appSettings["secureAddress"] : appSettings["unsecureAddress"];

            Uri uri;
            Uri.TryCreate(address, UriKind.Absolute, out uri);

            var properties = new Dictionary<string, object>();
            var addresses = new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object>
                        {
                            {"host", uri.Host},
                            {"scheme", uri.Scheme},
                            {"port", uri.Port.ToString(CultureInfo.InvariantCulture)},
                            {"path", uri.AbsolutePath}
                        }
                };

            properties.Add("host.Addresses", addresses);

            const bool useHandshake = true;
            const bool usePriorities = false;
            const bool useFlowControl = false;

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

            return properties;
        }

        public HandshakeSetup()
        {
            var secureProperties = GetProperties(true);
            var unsecureProperties = GetProperties(false);

            Http2SecureServer = new HttpSocketServer(new Http2Middleware(TestHelpers.AppFunction).Invoke, secureProperties);
            Http2UnsecureServer = new HttpSocketServer(new Http2Middleware(TestHelpers.AppFunction).Invoke, unsecureProperties);
        }

        public void Dispose()
        {
            Http2SecureServer.Dispose();
            Http2UnsecureServer.Dispose();
        }
    }
    public class HandshakeTests : IUseFixture<HandshakeSetup>, IDisposable
    {
        void IUseFixture<HandshakeSetup>.SetFixture(HandshakeSetup setupInstance)
        {
            
        }

        [StandardFact]
        public void AlpnSelectionHttp2Successful()
        {
            const string requestStr = @"https://localhost:8443/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            Stream clientStream = null;
            var selectedProtocol = String.Empty;
            bool gotSecurityError = false;
            try
            {
                clientStream = TestHelpers.GetHandshakedStream(uri);
                if (!(clientStream is SslStream))
                    gotSecurityError = true;
                else
                    selectedProtocol = (clientStream as SslStream).AlpnSelectedProtocol;
            }
            finally
            {
                Assert.Equal(gotSecurityError, false);
                Assert.NotEqual(clientStream, null);
                clientStream.Dispose();
                Assert.Equal(Protocols.Http2, selectedProtocol);
            }
        }

        [StandardFact]
        public void UpgradeHandshakeSuccessful()
        {
            const string requestStr = @"http://localhost:8080/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            bool gotException = false;
            try
            {
                var stream = TestHelpers.GetHandshakedStream(uri);
                stream.Write(TestHelpers.ClientSessionHeader);
                stream.Flush();
            }
            catch (Http2HandshakeFailed)
            {
                gotException = true;
            }

            Assert.Equal(gotException, false);
        }

        public void Dispose()
        {

        }
    }
}