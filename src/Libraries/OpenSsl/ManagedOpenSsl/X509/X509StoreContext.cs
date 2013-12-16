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

// Copyright (c) 2009 Frank Laub
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

namespace OpenSSL.X509
{
	/// <summary>
	/// Wraps the X509_STORE_CTX object
	/// </summary>
	public class X509StoreContext : Base
	{
		#region X509_STORE_CONTEXT
		[StructLayout(LayoutKind.Sequential)]
		struct X509_STORE_CONTEXT
		{
			public IntPtr ctx;
			public int current_method;
			public IntPtr cert;
			public IntPtr untrusted;
			public int purpose;
			public int trust;
#if PocketPC
            public uint check_time;
#else
			public long check_time;
#endif
			public uint flags;
			public IntPtr other_ctx;
			public IntPtr verify;
			public IntPtr verify_cb;
			public IntPtr get_issuer;
			public IntPtr check_issued;
			public IntPtr check_revocation;
			public IntPtr get_crl;
			public IntPtr check_crl;
			public IntPtr cert_crl;
			public IntPtr cleanup;
			public int depth;
			public int valid;
			public int last_untrusted;
			public IntPtr chain;
			public int error_depth;
			public int error;
			public IntPtr current_cert;
			public IntPtr current_issuer;
			public IntPtr current_crl;
			#region CRYPTO_EX_DATA ex_data;
			public IntPtr ex_data_sk;
			public int ex_data_dummy;
			#endregion
		}
		#endregion

		#region Initialization
		/// <summary>
		/// Calls X509_STORE_CTX_new()
		/// </summary>
		public X509StoreContext()
			: base(Native.ExpectNonNull(Native.X509_STORE_CTX_new()), true)
		{
		}

		internal X509StoreContext(IntPtr ptr, bool isOwner)
			: base(ptr, isOwner)
		{
		}
		#endregion

		#region Properties

		/// <summary>
		/// Returns X509_STORE_CTX_get_current_cert()
		/// </summary>
		public X509Certificate CurrentCert
		{
			get
			{
				IntPtr cert = Native.X509_STORE_CTX_get_current_cert(this.ptr);
				return new X509Certificate(cert, false);
			}
		}

		/// <summary>
		/// Returns X509_STORE_CTX_get_error_depth()
		/// </summary>
		public int ErrorDepth
		{
			get { return Native.X509_STORE_CTX_get_error_depth(this.ptr); }
		}

		/// <summary>
		/// Getter returns X509_STORE_CTX_get_error(), setter calls X509_STORE_CTX_set_error()
		/// </summary>
		public int Error
		{
			get { return Native.X509_STORE_CTX_get_error(this.ptr); }
			set { Native.X509_STORE_CTX_set_error(this.ptr, value); }
		}

		/// <summary>
		/// Returns an X509Store based on this context
		/// </summary>
		public X509Store Store
		{
			get { return new X509Store(this.Raw.ctx, false); }
		}

		/// <summary>
		/// Returns X509_verify_cert_error_string()
		/// </summary>
		public string ErrorString
		{
			get { return Native.PtrToStringAnsi(Native.X509_verify_cert_error_string(this.Raw.error), false); }
		}

		private X509_STORE_CONTEXT Raw
		{
			get { return (X509_STORE_CONTEXT)Marshal.PtrToStructure(this.ptr, typeof(X509_STORE_CONTEXT)); }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Calls X509_STORE_CTX_init()
		/// </summary>
		/// <param name="store"></param>
		/// <param name="cert"></param>
		/// <param name="uchain"></param>
		public void Init(X509Store store, X509Certificate cert, X509Chain uchain)
		{
			Native.ExpectSuccess(Native.X509_STORE_CTX_init(
				this.ptr,
				store.Handle,
				cert != null ? cert.Handle : IntPtr.Zero,
				uchain.Handle));
		}

		/// <summary>
		/// Returns X509_verify_cert()
		/// </summary>
		/// <returns></returns>
		public bool Verify()
		{
			int ret = Native.X509_verify_cert(this.ptr);
			if (ret < 0)
				throw new OpenSslException();
			return ret == 1;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Calls X509_STORE_CTX_free()
		/// </summary>
		protected override void OnDispose()
		{
			Native.X509_STORE_CTX_free(this.ptr);
		}

		#endregion
	}
}
