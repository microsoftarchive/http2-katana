// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
 
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

/*
* LICENSE ISSUES
* ==============
* The OpenSSL toolkit stays under a dual license, i.e. both the conditions of
* the OpenSSL License and the original SSLeay license apply to the toolkit.
* See below for the actual license texts. Actually both licenses are BSD-style
* Open Source licenses. In case of any license issues related to OpenSSL
* please contact openssl-core@openssl.org.

* OpenSSL License
* ---------------
 
* ====================================================================
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
 
* Original SSLeay License
* -----------------------
 
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

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Owin.Server.Adapters;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Utils;
using Microsoft.Owin;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.SSL;
using OpenSSL.X509;

namespace Microsoft.Http2.Owin.Server
{
    using AppFunc = Func<IOwinContext, Task>;
    /// <summary>
    /// This class handles incoming clients. It can accept them, make handshake and choose how to give a response.
    /// It encouraged to response with http11 or http20 
    /// </summary>
    internal sealed class HttpConnectingClient : IDisposable
    {
        private readonly TcpListener _server;
        private readonly AppFunc _next;     
        private readonly bool _useHandshake;
        private readonly bool _usePriorities;
        private readonly bool _useFlowControl;
        private readonly CancellationTokenSource _cancelClientHandling;
        private bool _isDisposed;
        private X509Certificate _cert;
        private bool _isSecure;
        private TcpClient client;

        internal HttpConnectingClient(TcpListener server, AppFunc next, X509Certificate cert, bool isSecure,
                                     bool useHandshake, bool usePriorities, bool useFlowControl)
        {
            _isDisposed = false;
            _usePriorities = usePriorities;
            _useHandshake = useHandshake;
            _useFlowControl = useFlowControl;
            _server = server;
            _isSecure = isSecure;
            _next = next;
            _cert = cert;
            _cancelClientHandling = new CancellationTokenSource();
        }

        /// <summary>
        /// Accepts client and deals handshake with it.
        /// </summary>
        internal void Accept(CancellationToken cancel)
        {
            try
            {
                client = _server.AcceptTcpClient();
            }
            catch (OperationCanceledException)
            {
                Http2Logger.LogDebug("Listen was cancelled");
                return;
            }  
            Http2Logger.LogDebug("New connection accepted");
            Task.Run(() => HandleAcceptedClient(client.GetStream()));
        }

        private void HandleAcceptedClient(Stream incomingClient)
        {
            bool backToHttp11 = false;
            string selectedProtocol = Protocols.Http1;

            if (_useHandshake)
            {
                try
                {
                    if (_isSecure)
                    {
                        incomingClient = new SslStream(incomingClient, false);

                        (incomingClient as SslStream).AuthenticateAsServer(_cert);

                        selectedProtocol = (incomingClient as SslStream).AlpnSelectedProtocol;
                    }
                }
                catch (OpenSslException)
                {
                    backToHttp11 = true;
                }
                catch (Exception e)
                {
                    Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                    incomingClient.Close();
                    return;
                }
            }
            try
            {
                HandleRequest(incomingClient, selectedProtocol, backToHttp11);
            }
            catch (Exception e)
            {
                Http2Logger.LogError("Exception occurred. Closing client's socket. " + e.Message);
                incomingClient.Close();
            }
        }

        private void HandleRequest(Stream incomingClient, string alpnSelectedProtocol, bool backToHttp11)
        {
            //Server checks selected protocol and calls http2 or http11 layer
            if (backToHttp11 || alpnSelectedProtocol == Protocols.Http1)
            {
                Http2Logger.LogDebug("Ssl chose http11");

                new Http11ProtocolOwinAdapter(incomingClient, SslProtocols.Tls, _next.Invoke).ProcessRequest();
                return;
            }

            //ALPN selected http2. No need to perform upgrade handshake.
            OpenHttp2Session(incomingClient);
        }

        private async void OpenHttp2Session(Stream incomingClientStream)
        {
            Http2Logger.LogDebug("Handshake successful");
            using (var messageHandler = new Http2OwinMessageHandler(incomingClientStream, ConnectionEnd.Server, 
                                                                    incomingClientStream is SslStream, _next, 
                                                                    _cancelClientHandling.Token))
            {
                try
                {
                    await messageHandler.StartSessionAsync();
                }
                catch (Exception)
                {
                    Http2Logger.LogError("Client was disconnected");
                }
            }

            GC.Collect();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }
    }
}
