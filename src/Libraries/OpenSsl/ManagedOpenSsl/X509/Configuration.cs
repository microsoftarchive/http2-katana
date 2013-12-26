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

namespace OpenSSL.X509
{
	#region X509v3Context

	/// <summary>
	/// Wraps X509V3_CTX
	/// </summary>
	class X509V3Context : Base
	{
		#region X509V3_CTX
		[StructLayout(LayoutKind.Sequential)]
		struct X509V3_CTX
		{
			public int flags;
			public IntPtr issuer_cert;
			public IntPtr subject_cert;
			public IntPtr subject_req;
			public IntPtr crl;
			public IntPtr db_meth;
			public IntPtr db;
		}
		#endregion

		#region Initialization

		/// <summary>
		/// Calls OPENSSL_malloc()
		/// </summary>
		private X509V3Context()
			: base(Native.OPENSSL_malloc(Marshal.SizeOf(typeof(X509V3_CTX))), true)
		{ }

		/// <summary>
		/// Calls X509V3_set_ctx()
		/// </summary>
		/// <param name="issuer"></param>
		/// <param name="subject"></param>
		/// <param name="request"></param>
		public X509V3Context(X509Certificate issuer, X509Certificate subject, X509Request request)
			: this()
		{
			Native.X509V3_set_ctx(
				this.ptr,
				issuer != null ? issuer.Handle : IntPtr.Zero,
				subject != null ? subject.Handle : IntPtr.Zero,
				request != null ? request.Handle : IntPtr.Zero,
				IntPtr.Zero,
				0);
		}

		#endregion

		#region Methods

		/// <summary>
		/// X509V3_set_ctx_nodb - sets the db pointer to NULL
		/// </summary>
		public void SetNoDB()
		{
			int db_offset = (int)Marshal.OffsetOf(typeof(X509V3_CTX), "db");
			IntPtr db_param = new IntPtr((int)this.ptr + db_offset);
			Marshal.WriteIntPtr(db_param, IntPtr.Zero);
		}

		/// <summary>
		/// Calls X509V3_set_nconf()
		/// </summary>
		/// <param name="cfg"></param>
		public void SetConfiguration(Configuration cfg)
		{
			Native.X509V3_set_nconf(this.ptr, cfg.Handle);
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Calls OPENSSL_free()
		/// </summary>
		protected override void OnDispose()
		{
			Native.OPENSSL_free(this.ptr);
		}

		#endregion
	}
	#endregion

	/// <summary>
	/// Wraps the NCONF_* functions
	/// </summary>
	public class Configuration : Base
	{
		#region Initialization

		/// <summary>
		/// Calls NCONF_new()
		/// </summary>
		private Configuration()
			: base(Native.NCONF_new(IntPtr.Zero), true)
		{ }

		/// <summary>
		/// Calls NCONF_load()
		/// </summary>
		/// <param name="filename"></param>
		public Configuration(string filename)
			: this()
		{
			this.Load(filename);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Calls NCONF_load()
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			int eline = 0;
			Native.ExpectSuccess(Native.NCONF_load(this.ptr, filename, ref eline));
		}

		/// <summary>
		/// Creates a X509v3Context(), calls X509V3_set_ctx() on it, then calls
		/// X509V3_EXT_add_nconf()
		/// </summary>
		/// <param name="section"></param>
		/// <param name="issuer"></param>
		/// <param name="subject"></param>
		/// <param name="request"></param>
		public void ApplyExtensions(
			string section,
			X509Certificate issuer,
			X509Certificate subject,
			X509Request request)
		{
			using (X509V3Context ctx = new X509V3Context(issuer, subject, request))
			{
				ctx.SetConfiguration(this);
				Native.ExpectSuccess(Native.X509V3_EXT_add_nconf(
					this.ptr,
					ctx.Handle,
					Encoding.ASCII.GetBytes(section),
					subject.Handle));
			}
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Calls NCONF_free()
		/// </summary>
		protected override void OnDispose()
		{
			Native.NCONF_free(this.ptr);
		}

		#endregion
	}
}