using System.Globalization;
using System.IO;
using Http2.TestClient.Handshake;
using Microsoft.Http1.Protocol;
using Microsoft.Http2.Owin.UpgradeMiddleware;
using Microsoft.Http2.Owin.Server;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Tests;
using System;
using System.Collections.Generic;
using OpenSSL.SSL;
using Xunit;
using Owin.Types;

namespace Http2.Katana.Tests
{
    public class HandshakeSetup : IDisposable
    {
        public HttpSocketServer Http2SecureServer { get; private set; }
        public HttpSocketServer Http2UnsecureServer { get; private set; }

        public HandshakeSetup()
        {
            var secureProperties = TestHelper.GetProperties(true);
            var unsecureProperties = TestHelper.GetProperties(false);

            Http2SecureServer = new HttpSocketServer(new UpgradeMiddleware(new ResponseMiddleware(null)).Invoke, secureProperties);
            Http2UnsecureServer = new HttpSocketServer(new UpgradeMiddleware(new ResponseMiddleware(null)).Invoke, unsecureProperties);
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
        public void AlpnSelectedHttp2()
        {
            const string requestStr = @"https://localhost:8443/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            Stream clientStream = null;
            var selectedProtocol = String.Empty;
            bool gotSecurityError = false;
            try
            {
                clientStream = TestHelper.GetHandshakedStream(uri);
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
        public void Upgrade()
        {
            const string requestStr = @"http://localhost:8080/";
            Uri uri;
            Uri.TryCreate(requestStr, UriKind.Absolute, out uri);
            bool gotException = false;
            try
            {
                var stream = TestHelper.GetHandshakedStream(uri);
                stream.Write(TestHelper.ClientSessionHeader);
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