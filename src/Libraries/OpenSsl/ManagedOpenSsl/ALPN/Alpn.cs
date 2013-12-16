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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenSSL.Exceptions;
using OpenSSL.SSL;

namespace OpenSSL.ALPN
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ssl"></param>
    /// <param name="selProto"></param>
    /// <param name="selProtoLen"></param>
    /// <param name="inProtos"></param>
    /// <param name="inProtosLen"></param>
    /// <param name="arg"></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AlpnCallback(IntPtr ssl,
                                     [MarshalAs(UnmanagedType.LPStr)] out string selProto,
                                     [MarshalAs(UnmanagedType.U1)] out byte selProtoLen,
                                     IntPtr inProtos, int inProtosLen, IntPtr arg);

    internal class AlpnExtension
    {
        internal AlpnExtension(IntPtr ctxHandle, IEnumerable<string> knownProtos)
        {
            if (knownProtos == null)
                throw new ArgumentNullException("knownProtos");

            SetKnownProtocols(ctxHandle, knownProtos);
        }

        [DllImport("ssleay32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SSL_CTX_set_alpn_protos(IntPtr /*SSL_CTX * */ ctx,
                                                         [MarshalAs(UnmanagedType.LPArray)] byte[]
                                                             /*const unsigned char* */ protos,
                                                         UInt32 protos_len);

        [DllImport("ssleay32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SSL_get0_alpn_selected(IntPtr /*SSL* */ ssl,
                                                         ref IntPtr /*const unsigned char** */ data,
                                                         ref IntPtr /*unsigned* */ len);

        //void SSL_CTX_set_alpn_select_cb(SSL_CTX* ctx,
        //int (*cb) (SSL *ssl,
        //     const unsigned char **out,
        //     unsigned char *outlen,
        //     const unsigned char *in,
        //     unsigned int inlen,
        //      void *arg),
        // void *arg)
        [DllImport("ssleay32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SSL_CTX_set_alpn_select_cb(IntPtr /*SSL_CTX* */ ctx,
                                                             IntPtr /* int (*cb) */ alpnCb,
                                                             IntPtr /*void* */ arg);

        private byte[] _knownProtocols;

        private bool CompareProtos(byte[] protos1, int offset1, byte[] protos2, int offset2, int count)
        {
            if (offset1 + count > protos1.Length
                || offset2 + count > protos2.Length)
            {
                return false;
            }

            for (int i = 0; i < count; i ++)
            {
                if (protos1[i + offset1] != protos2[i + offset2])
                    return false;
            }

            return true;
        }

        private void SetKnownProtocols(IntPtr ctx, IEnumerable<string> protos)
        {
            using (var protoStream = new MemoryStream())
            {
                int offset = 0;
                foreach (var proto in protos)
                {
                    byte protoLen = (byte) proto.Length;
                    protoStream.WriteByte(protoLen);

                    var protoBf = Encoding.UTF8.GetBytes(proto);
                    protoStream.Write(protoBf, 0, protoLen);

                    offset += protoLen + sizeof(byte);
                }

                _knownProtocols = new byte[offset];
                Buffer.BlockCopy(protoStream.GetBuffer(), 0, _knownProtocols, 0, offset);
            }

            if (SSL_CTX_set_alpn_protos(ctx, _knownProtocols, (UInt32)_knownProtocols.Length) != 0)
                throw new AlpnException("cant set alpn protos");
        }

        public int AlpnCb(IntPtr ssl, 
                                 [MarshalAs(UnmanagedType.LPStr)] out string selProto, 
                                 [MarshalAs(UnmanagedType.U1)] out byte selProtoLen,
                                 IntPtr inProtos, int inProtosLen, IntPtr arg)
        {
            var inProtosBytes = new byte[inProtosLen];

            for (int i = 0; i < inProtosLen; i++)
            {
                inProtosBytes[i] = Marshal.ReadByte(inProtos, i);
            }

            int matchIndex = -1;
            byte matchLen = 0;
            for (int i = 0; i < _knownProtocols.Length; )
            {
                bool gotMatch = false;
                for (int j = 0; j < inProtosLen; )
                {
                    if (_knownProtocols[i] == inProtosBytes[j] &&
                        CompareProtos(_knownProtocols, i + 1, inProtosBytes, j + 1, _knownProtocols[i]))
                    {
                        /* We found a match */
                        matchIndex = i;
                        matchLen = _knownProtocols[i];
                        gotMatch = true;
                        break;
                    }

                    j += inProtosBytes[j];
                    j++;
                }

                if (gotMatch)
                    break;

                i += _knownProtocols[i];
                i++;
            }

            if (matchIndex == -1)
            {
                selProto = null;
                selProtoLen = 0;
                return (int) Errors.SSL_TLSEXT_ERR_NOACK;
            }

            selProto = Encoding.UTF8.GetString(_knownProtocols, matchIndex + 1, matchLen);

            selProtoLen = matchLen;
            return (int) Errors.SSL_TLSEXT_ERR_OK; //ok OPENSSL_NPN_NEGOTIATED
        }
    }
}
