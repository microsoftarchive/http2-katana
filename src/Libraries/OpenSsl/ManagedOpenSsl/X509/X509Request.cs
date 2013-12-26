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

// Copyright (c) 2006-2007 Frank Laub
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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenSSL.Core;
using OpenSSL.Crypto;

namespace OpenSSL.X509
{
	/// <summary>
	/// Wraps a X509_REQ object.
	/// </summary>
	public class X509Request : Base
	{
		#region Initialization
		/// <summary>
		/// Calls X509_REQ_new()
		/// </summary>
		public X509Request() 
			: base(Native.ExpectNonNull(Native.X509_REQ_new()), true)
		{ }
		
		internal X509Request(IntPtr ptr, bool owner) 
			: base(ptr, owner) 
		{ }

		/// <summary>
		/// Calls X509_REQ_new() and then initializes version, subject, and key.
		/// </summary>
		/// <param name="version"></param>
		/// <param name="subject"></param>
		/// <param name="key"></param>
		public X509Request(int version, X509Name subject, CryptoKey key)
			: this()
		{
			this.Version = version;
			this.Subject = subject;
			this.PublicKey = key;
		}

		/// <summary>
		/// Calls PEM_read_bio_X509_REQ()
		/// </summary>
		/// <param name="bio"></param>
		public X509Request(BIO bio)
			: base(Native.ExpectNonNull(Native.PEM_read_bio_X509_REQ(bio.Handle, IntPtr.Zero, null, IntPtr.Zero)), true)
		{ }

		/// <summary>
		/// Creates a X509_REQ from a PEM formatted string.
		/// </summary>
		/// <param name="pem"></param>
		public X509Request(string pem)
			: this(new BIO(pem))
		{ }
		#endregion

		#region X509_REQ_INFO
		[StructLayout(LayoutKind.Sequential)]
		private struct X509_REQ_INFO
		{
			#region ASN1_ENCODING enc;
			public IntPtr enc_enc;
			public int enc_len;
			public int enc_modified;
			#endregion
			public IntPtr version;
			public IntPtr subject;
			public IntPtr pubkey;
			public IntPtr attributes;
		}
		#endregion

		#region X509_REQ
		[StructLayout(LayoutKind.Sequential)]
		private struct X509_REQ
		{
			public IntPtr req_info;
			public IntPtr sig_alg;
			public IntPtr signature;
			public int references;
		}
		#endregion

		#region Properties
		private X509_REQ Raw
		{
			get { return (X509_REQ)Marshal.PtrToStructure(this.ptr, typeof(X509_REQ)); }
		}

		private X509_REQ_INFO RawInfo
		{
			get { return (X509_REQ_INFO)Marshal.PtrToStructure(this.Raw.req_info, typeof(X509_REQ_INFO)); }
		}
		
		/// <summary>
		/// Accessor to the version field. The settor calls X509_REQ_set_version().
		/// </summary>
		public int Version
		{
			get { return Native.ASN1_INTEGER_get(this.RawInfo.version); }
			set { Native.ExpectSuccess(Native.X509_REQ_set_version(this.ptr, value)); }
		}

		/// <summary>
		/// Accessor to the pubkey field. Uses X509_REQ_get_pubkey() and X509_REQ_set_pubkey()
		/// </summary>
		public CryptoKey PublicKey
		{
			get { return new CryptoKey(Native.ExpectNonNull(Native.X509_REQ_get_pubkey(this.ptr)), true); }
			set { Native.ExpectSuccess(Native.X509_REQ_set_pubkey(this.ptr, value.Handle)); }
		}

		/// <summary>
		/// Accessor to the subject field. Setter calls X509_REQ_set_subject_name().
		/// </summary>
		public X509Name Subject
		{
			get { return new X509Name(Native.X509_NAME_dup(this.RawInfo.subject), true); }
			set { Native.ExpectSuccess(Native.X509_REQ_set_subject_name(this.ptr, value.Handle)); }
		}

		/// <summary>
		/// Returns the PEM formatted string for this object.
		/// </summary>
		public string PEM
		{
			get
			{
				using (BIO bio = BIO.MemoryBuffer())
				{
					this.Write(bio);
					return bio.ReadString();
				}
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Sign this X509Request using the supplied key and digest.
		/// </summary>
		/// <param name="pkey"></param>
		/// <param name="digest"></param>
		public void Sign(CryptoKey pkey, MessageDigest digest)
		{
			if (Native.X509_REQ_sign(this.ptr, pkey.Handle, digest.Handle) == 0)
				throw new OpenSslException();
		}

		/// <summary>
		/// Verify this X509Request against the supplied key.
		/// </summary>
		/// <param name="pkey"></param>
		/// <returns></returns>
		public bool Verify(CryptoKey pkey)
		{
			int ret = Native.X509_REQ_verify(this.ptr, pkey.Handle);
			if (ret < 0)
				throw new OpenSslException();
			return ret == 1;
		}

		//public ArraySegment<byte> Digest(IntPtr type, byte[] digest)
		//{
		//    uint len = (uint)digest.Length;
		//    Native.ExpectSuccess(Native.X509_REQ_digest(this.ptr, type, digest, ref len));
		//    return new ArraySegment<byte>(digest, 0, (int)len);
		//}

		/// <summary>
		/// Calls X509_REQ_print()
		/// </summary>
		/// <param name="bio"></param>
		public override void Print(BIO bio)
		{
			Native.ExpectSuccess(Native.X509_REQ_print(bio.Handle, this.ptr));
		}

		/// <summary>
		/// Calls PEM_write_bio_X509_REQ()
		/// </summary>
		/// <param name="bio"></param>
		public void Write(BIO bio)
		{
			Native.ExpectSuccess(Native.PEM_write_bio_X509_REQ(bio.Handle, this.ptr));
		}

		/// <summary>
		/// Converts this request into a certificate using X509_REQ_to_X509().
		/// </summary>
		/// <param name="days"></param>
		/// <param name="pkey"></param>
		/// <returns></returns>
		public X509Certificate CreateCertificate(int days, CryptoKey pkey)
		{
			return new X509Certificate(Native.ExpectNonNull(Native.X509_REQ_to_X509(this.ptr, days, pkey.Handle)), true);
		}
		#endregion

		#region Overrides Members

		/// <summary>
		/// Calls X509_REQ_free()
		/// </summary>
		protected override void OnDispose() {
			Native.X509_REQ_free(this.ptr);
		}

		#endregion
	}
}
