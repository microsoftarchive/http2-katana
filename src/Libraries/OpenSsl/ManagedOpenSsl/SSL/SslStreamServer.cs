// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

/* LICENSE ISSUES
* ==============
 
* The OpenSSL toolkit stays under a dual license, i.e. both the conditions of
* the OpenSSL License and the original SSLeay license apply to the toolkit.
* See below for the actual license texts. Actually both licenses are BSD-style
* Open Source licenses. In case of any license issues related to OpenSSL
* please contact openssl-core@openssl.org.
 
* OpenSSL License
* ---------------
*/

/* ====================================================================
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
*/

//Original SSLeay License
//-----------------------

/* Copyright (C) 1995-1998 Eric Young (eay@cryptsoft.com)
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

// Copyright (c) 2009 Ben Henderson
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. The name of the author may not be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Text;
using System.IO;
using OpenSSL.Core;
using OpenSSL.X509;
using OpenSsl.Protocols;

namespace OpenSSL.SSL
{
    internal class SslStreamServer : SslStreamBase
    {
        public SslStreamServer(
            Stream stream, 
            bool ownStream,
            X509Certificate serverCertificate,
            bool clientCertificateRequired,
            X509Chain caCerts,
            SslProtocols enabledSslProtocols,
            SslStrength sslStrength,
            bool checkCertificateRevocation,
            RemoteCertificateValidationHandler remote_callback)
            : base(stream, ownStream)
        {
            checkCertificateRevocationStatus = checkCertificateRevocation;
            remoteCertificateSelectionCallback = remote_callback;

            // Initialize the SslContext object
            InitializeServerContext(serverCertificate, clientCertificateRequired, caCerts, enabledSslProtocols, sslStrength, checkCertificateRevocation);
            
            // Initalize the Ssl object
            ssl = new Ssl(sslContext);
            // Initialze the read/write bio
            read_bio = BIO.MemoryBuffer(false);
            write_bio = BIO.MemoryBuffer(false);
            // Set the read/write bio's into the the Ssl object
            ssl.SetBIO(read_bio, write_bio);
            read_bio.SetClose(BIO.CloseOption.Close);
            write_bio.SetClose(BIO.CloseOption.Close);
            // Set the Ssl object into server mode
            ssl.SetAcceptState();
        }

        internal protected override bool ProcessHandshake()
        {
            bool bRet = false;
            int nRet = 0;
            
            if (handShakeState == HandshakeState.InProcess)
            {
                nRet = ssl.Accept();
            }
            else if (handShakeState == HandshakeState.RenegotiateInProcess)
            {
                nRet = ssl.DoHandshake();
            }
            else if (handShakeState == HandshakeState.Renegotiate)
            {
                nRet = ssl.DoHandshake();
                ssl.State = Ssl.SSL_ST_ACCEPT;
                handShakeState = HandshakeState.RenegotiateInProcess;
            }
            SslError lastError = ssl.GetError(nRet);
            if (lastError == SslError.SSL_ERROR_WANT_READ || lastError == SslError.SSL_ERROR_WANT_WRITE || lastError == SslError.SSL_ERROR_NONE)
            {
                if (nRet == 1) // success
                {
                    bRet = true;
                }
            }
            else
            {
                // Check to see if we have alert data in the write_bio that needs to be sent
                if (write_bio.BytesPending > 0)
                {
                    // We encountered an error, but need to send the alert
                    // set the handshakeException so that it will be processed
                    // and thrown after the alert is sent
                    handshakeException = new OpenSslException();
                }
                else
                {
                    // No alert to send, throw the exception
                    throw new OpenSslException();
                }
            }
            return bRet;
        }

        private void InitializeServerContext(
            X509Certificate serverCertificate,
            bool clientCertificateRequired,
            X509Chain caCerts,
            SslProtocols enabledSslProtocols,
            SslStrength sslStrength,
            bool checkCertificateRevocation)
        {
            if (serverCertificate == null)
            {
                throw new ArgumentNullException("serverCertificate", "Server certificate cannot be null");
            }
            if (!serverCertificate.HasPrivateKey)
            {
                throw new ArgumentException("Server certificate must have a private key", "serverCertificate");
            }

            // Initialize the context
            sslContext = new SslContext(SslMethod.TLSv1_server_method, ConnectionEnd.Server, true, new[] { Protocols.Http2, Protocols.Http1 });
            
            // Remove support for protocols not specified in the enabledSslProtocols
            if ((enabledSslProtocols & SslProtocols.Ssl2) != SslProtocols.Ssl2)
            {
                sslContext.Options |= SslOptions.SSL_OP_NO_SSLv2;
            }
            if ((enabledSslProtocols & SslProtocols.Ssl3) != SslProtocols.Ssl3 &&
                ((enabledSslProtocols & SslProtocols.Default) != SslProtocols.Default))
            {
                // no SSLv3 support
                sslContext.Options |= SslOptions.SSL_OP_NO_SSLv3;
            }
            if ((enabledSslProtocols & SslProtocols.Tls) != SslProtocols.Tls &&
                (enabledSslProtocols & SslProtocols.Default) != SslProtocols.Default)
            {
                sslContext.Options |= SslOptions.SSL_OP_NO_TLSv1;
            }

            // Set the context mode
            sslContext.Mode = SslMode.SSL_MODE_AUTO_RETRY;
            // Set the workaround options
            sslContext.Options = SslOptions.SSL_OP_ALL;
            // Set the client certificate verification callback if we are requiring client certs
            if (clientCertificateRequired)
            {
                sslContext.SetVerify(VerifyMode.SSL_VERIFY_PEER | VerifyMode.SSL_VERIFY_FAIL_IF_NO_PEER_CERT, remoteCertificateSelectionCallback);
            }
            else
            {
                sslContext.SetVerify(VerifyMode.SSL_VERIFY_NONE, null);
            }

            // Set the client certificate max verification depth
            sslContext.SetVerifyDepth(10);
            // Set the certificate store and ca list
            if (caCerts != null)
            {
                // Don't take ownership of the X509Store IntPtr.  When we
                // SetCertificateStore, the context takes ownership of the store pointer.
                var cert_store = new X509Store(caCerts, false);
                sslContext.SetCertificateStore(cert_store);
                Stack<X509Name> name_stack = new Core.Stack<X509Name>();
                foreach (X509Certificate cert in caCerts)
                {
                    X509Name subject = cert.Subject;
                    name_stack.Add(subject);
                }
                // Assign the stack to the context
                sslContext.CAList = name_stack;
            }
            // Set the cipher string
            sslContext.SetCipherList(GetCipherString(false, enabledSslProtocols, sslStrength));
            // Set the certificate
            sslContext.UseCertificate(serverCertificate);
            // Set the private key
            sslContext.UsePrivateKey(serverCertificate.PrivateKey);
            // Set the session id context
            sslContext.SetSessionIdContext(Encoding.ASCII.GetBytes("AppDomainHost: UnitTests12345678"/*AppDomain.CurrentDomain.FriendlyName*/));
        }
    }
}
