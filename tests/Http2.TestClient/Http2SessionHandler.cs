// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

/*
*LICENSE ISSUES
*==============
 
*  The OpenSSL toolkit stays under a dual license, i.e. both the conditions of
*  the OpenSSL License and the original SSLeay license apply to the toolkit.
*  See below for the actual license texts. Actually both licenses are BSD-style
*  Open Source licenses. In case of any license issues related to OpenSSL
*  please contact openssl-core@openssl.org.
 
*  OpenSSL License
*  ---------------
 
*====================================================================
* Copyright (c) 1998-2011 The OpenSSL Project.  All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met:
*
* 1. Redistributions of source code must retain the above copyright
*    notice, this list of conditions and the following disclaimer.
*
* 2. Redistributions in binary form must reproduce the above copyright
*    notice, this list of conditions and the following disclaimer in
*    the documentation and/or other materials provided with the
*    distribution.
*
* 3. All advertising materials mentioning features or use of this
*    software must display the following acknowledgment:
*    "This product includes software developed by the OpenSSL Project
*    for use in the OpenSSL Toolkit. (http://www.openssl.org/)"
*
* 4. The names "OpenSSL Toolkit" and "OpenSSL Project" must not be used to
*    endorse or promote products derived from this software without
*    prior written permission. For written permission, please contact
*    openssl-core@openssl.org.
*
* 5. Products derived from this software may not be called "OpenSSL"
*    nor may "OpenSSL" appear in their names without prior written
*    permission of the OpenSSL Project.
*
* 6. Redistributions of any form whatsoever must retain the following
*    acknowledgment:
*    "This product includes software developed by the OpenSSL Project
*    for use in the OpenSSL Toolkit (http://www.openssl.org/)"
*
* THIS SOFTWARE IS PROVIDED BY THE OpenSSL PROJECT ``AS IS'' AND ANY
* EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
* PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE OpenSSL PROJECT OR
* ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
* SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
* NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
* HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
* STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
* ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
* OF THE POSSIBILITY OF SUCH DAMAGE.
* ====================================================================
*
* This product includes cryptographic software written by Eric Young
* (eay@cryptsoft.com).  This product includes software written by Tim
* Hudson (tjh@cryptsoft.com).
*
*
 
Original SSLeay License
-----------------------
 
* Copyright (C) 1995-1998 Eric Young (eay@cryptsoft.com)
* All rights reserved.
*
* This package is an SSL implementation written
* by Eric Young (eay@cryptsoft.com).
* The implementation was written so as to conform with Netscapes SSL.
*
* This library is free for commercial and non-commercial use as long as
* the following conditions are aheared to.  The following conditions
* apply to all code found in this distribution, be it the RC4, RSA,
* lhash, DES, etc., code; not just the SSL code.  The SSL documentation
* included with this distribution is covered by the same copyright terms
* except that the holder is Tim Hudson (tjh@cryptsoft.com).
*
* Copyright remains Eric Young's, and as such any Copyright notices in
* the code are not to be removed.
* If this package is used in a product, Eric Young should be given attribution
* as the author of the parts of the library used.
* This can be in the form of a textual message at program startup or
* in documentation (online or textual) provided with the package.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met:
* 1. Redistributions of source code must retain the copyright
*    notice, this list of conditions and the following disclaimer.
* 2. Redistributions in binary form must reproduce the above copyright
*    notice, this list of conditions and the following disclaimer in the
*    documentation and/or other materials provided with the distribution.
* 3. All advertising materials mentioning features or use of this software
*    must display the following acknowledgement:
*    "This product includes cryptographic software written by
*     Eric Young (eay@cryptsoft.com)"
*    The word 'cryptographic' can be left out if the rouines from the library
*    being used are not cryptographic related :-).
* 4. If you include any Windows specific code (or a derivative thereof) from
*    the apps directory (application code) you must include an acknowledgement:
*    "This product includes software written by Tim Hudson (tjh@cryptsoft.com)"
*
* THIS SOFTWARE IS PROVIDED BY ERIC YOUNG ``AS IS'' AND
* ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
* FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
* DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
* OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
* HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
* LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
* OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
* SUCH DAMAGE.
*
* The licence and distribution terms for any publically available version or
* derivative of this code cannot be changed.  i.e. this code cannot simply be
* copied and put under another distribution licence
* [including the GNU Public Licence.]
*/

using System.IO;
using System.Reflection;
using Http2.TestClient.Adapters;
using Http2.TestClient.Handshake;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.SSL;
using OpenSSL.X509;

namespace Http2.TestClient
{
    /// <summary>
    /// This class expresses client's logic.
    /// It can create client socket, accept server responses, make handshake and choose how to send requests to server.
    /// </summary>
    public sealed class Http2SessionHandler : IDisposable
    {
        #region Fields
        private Http2ClientMessageHandler _sessionAdapter;
        private Stream _clientStream;
        private static readonly string AssemblyName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private readonly string _certificatePath;
        private string _selectedProtocol;
        private bool _useHttp20 = true;
        private readonly bool _isDirectEnabled;
        private bool _isSecure;
        private bool _isDisposed;
        private string _path;
        private int _port;
        private string _version;
        private string _scheme;
        private string _host;
        private readonly IDictionary<string, object> _environment;
        private readonly string _serverName;
        private X509Chain _chain;
        private X509Certificate _certificate;

        #endregion

        #region Events

        /// <summary>
        /// Session closed event.
        /// </summary>
        public event EventHandler<EventArgs> OnClosed;

        #endregion

        #region Properties

        public string ServerUri { get; private set; }

        public bool WasHttp1Used 
        {
            get { return !_useHttp20; }
        }

        public SslProtocols Protocol { get; private set; }

        #endregion

        #region Methods

        private X509Certificate LoadPKCS12Certificate(string certFilename, string password)
        {
            using (BIO certFile = BIO.File(certFilename, "r"))
            {
                return X509Certificate.FromPKCS12(certFile, password);
            }
        }

        public Http2SessionHandler(IDictionary<string, object> environment)
        {
            Protocol = SslProtocols.None;
            _certificatePath = Strings.ClientCertName;
            _environment = new Dictionary<string, object>();
            //Copy environment
            _environment.AddRange(environment);
            if (_environment[Strings.DirectEnabled] is bool)
            {
                _isDirectEnabled = (bool)environment[Strings.DirectEnabled];
            }
            else
            {
                _isDirectEnabled = false;
            }
            var s = _environment[Strings.ServerName] as string;
            _serverName = s ?? Strings.Localhost;
        }

        private void MakeHandshakeEnvironment()
        {
            _environment.AddRange(new Dictionary<string, object>
			{
                {PseudoHeaders.Path, _path},
                {PseudoHeaders.Scheme, _scheme},
                {CommonHeaders.Host, _host},
                {HandshakeKeys.Stream, _clientStream},
                {HandshakeKeys.ConnectionEnd, ConnectionEnd.Client}
			});
        }

        public bool Connect(Uri connectUri)
        {
            _path = connectUri.PathAndQuery;
            _version = Protocols.Http2;
            _scheme = connectUri.Scheme;
            _host = connectUri.Host;
            _port = connectUri.Port;
            ServerUri = connectUri.Authority;

            if (_sessionAdapter != null)
            {
                return false;
            }

            try
            {
                int port = connectUri.Port;

                int securePort = ClientOptions.SecurePort;

                _isSecure = port == securePort;

                var tcpClnt = new TcpClient(connectUri.Host, port);

                _clientStream = tcpClnt.GetStream();

                if (!_isDirectEnabled)
                {
                    if (_isSecure)
                    {
                        _clientStream = new SslStream(_clientStream, false, _serverName);
                        _certificate = LoadPKCS12Certificate(AssemblyName + _certificatePath, String.Empty);

                        _chain = new X509Chain {_certificate};
                        var certList = new X509List { _certificate };
                        
                        (_clientStream as SslStream).AuthenticateAsClient(connectUri.AbsoluteUri, certList, _chain,
                                                                          SslProtocols.Tls, SslStrength.All, false);
                        
                        _selectedProtocol = (_clientStream as SslStream).AlpnSelectedProtocol;
                    }

                    if (!_isSecure || _selectedProtocol == Protocols.Http1)
                    {
                        MakeHandshakeEnvironment();
                        try
                        {
                            var handshakeResult = new UpgradeHandshaker(_environment).Handshake();
                            _environment.Add(HandshakeKeys.Result, handshakeResult);
                            _useHttp20 = handshakeResult[HandshakeKeys.Successful] as string == HandshakeKeys.True;

                            if (!_useHttp20)
                            {
                                Dispose(false);
                                return true;
                            }
                        }
                        catch (Http2HandshakeFailed ex)
                        {
                            if (ex.Reason == HandshakeFailureReason.InternalError)
                            {
                                _useHttp20 = false;
                            }
                            else
                            {
                                Http2Logger.Error("Specified server did not respond");
                                Dispose(true);
                                return false;
                            }
                        }
                    }
                }

                Http2Logger.Info("Handshake finished");

                Protocol = _isSecure ? SslProtocols.Tls : SslProtocols.None;

                if (_useHttp20)
                {
                    _sessionAdapter = new Http2ClientMessageHandler(_clientStream, ConnectionEnd.Client, _isSecure, CancellationToken.None);
                }
            }
            catch (SocketException)
            {
                Http2Logger.Error("Check if any server listens port " + connectUri.Port);
                Dispose(true);
                return false;
            }
            catch (Exception ex)
            {
                Http2Logger.Error("Unknown connection exception was caught: " + ex.Message);
                Dispose(true);
                return false;
            }

            return true;
        }

        public async void StartConnection()
        {
            Http2Logger.Info("Start connection called");
            if (_useHttp20 && !_sessionAdapter.IsDisposed && !_isDisposed)
            {
                Dictionary<string, string> initialRequest = null;
                if (!_isSecure)
                {
                    initialRequest = new Dictionary<string,string>
                        {
                            {PseudoHeaders.Path, _path},
                        };
                }
 
                await _sessionAdapter.StartSessionAsync(initialRequest);
            }

            if (!_sessionAdapter.IsDisposed) 
                return;

            Http2Logger.Error("Connection was aborted by the remote side. Check your connection preface.");
            Dispose(true);
        }

        //localPath should be provided only for post and put cmds
        //serverPostAct should be provided only for post cmd
        private void SubmitRequest(Uri request, string method)
        {
            //Submit request if http2 was chosen
            Http2Logger.Info("Submitting request");

            var headers = new HeadersList
                {
                    new KeyValuePair<string, string>(PseudoHeaders.Method, method.ToLower()),
                    new KeyValuePair<string, string>(PseudoHeaders.Path, request.PathAndQuery.ToLower()),
                    new KeyValuePair<string, string>(PseudoHeaders.Authority, _host.ToLower()),
                    new KeyValuePair<string, string>(PseudoHeaders.Scheme, _scheme.ToLower()),
                };
            
            //Sending request with default  priority
            _sessionAdapter.SendRequest(headers, Constants.DefaultStreamPriority, true);
        }

        public void SendRequestAsync(Uri request, string method)
        {
            if (!_sessionAdapter.IsDisposed)
            {
                if (_host != request.Host || _port != request.Port || _scheme != request.Scheme)
                {
                    throw new InvalidOperationException("Trying to send request to non connected address");
                }

                if (!_useHttp20)
                {
                    Http2Logger.Info("Download with Http/1.1");
                }

                //Submit request in the current thread, response will be handled in the session thread.
                SubmitRequest(request, method);
            }
        }

        public TimeSpan Ping()
        {
            if (_sessionAdapter != null)
            {
                return Task.Run(new Func<TimeSpan>(_sessionAdapter.Ping)).Result;
            }

            return TimeSpan.Zero;
        }

        public void Dispose(bool wasErrorOccurred)
        {
            Dispose();

            if (OnClosed != null)
            {
                OnClosed(this, null);
            }

            OnClosed = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_sessionAdapter != null)
            {
                _sessionAdapter.Dispose();
            }

            if (_clientStream != null)
            {
                _clientStream.Dispose();
                _clientStream = null;
            }

            if (_certificate != null)
            {
                _certificate.Dispose();
                _certificate = null;
            }

            if (_chain != null)
            {
                _chain.Dispose();
                _chain = null;
            }

            _isDisposed = true;
        }

        #endregion
    }
}
