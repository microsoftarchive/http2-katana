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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenSSL.Core;
using OpenSSL.Exceptions;
using OpenSSL.SSL;

namespace OpenSSL.Extensions
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

            if (Native.SSL_CTX_set_alpn_protos(ctx, _knownProtocols, (UInt32)_knownProtocols.Length) != 0)
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
