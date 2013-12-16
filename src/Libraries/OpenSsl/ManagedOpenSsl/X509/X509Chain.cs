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
	/// <summary>
	/// Contains a chain X509_INFO objects.
	/// </summary>
	public class X509Chain : Core.Stack<X509Certificate>
	{
		#region Initialization
		/// <summary>
		/// Default null constructor
		/// </summary>
		public X509Chain() { }

		/// <summary>
		/// Creates a chain from a BIO. Expects the stream to contain
		/// a collection of X509_INFO objects in PEM format by calling
		/// PEM_X509_INFO_read_bio()
		/// </summary>
		/// <param name="bio"></param>
		public X509Chain(BIO bio)
		{
			IntPtr sk = Native.ExpectNonNull(Native.PEM_X509_INFO_read_bio(bio.Handle, IntPtr.Zero, null, IntPtr.Zero));
			using (Core.Stack<X509CertificateInfo> stack = new Core.Stack<X509CertificateInfo>(sk, true))
			{
				while (stack.Count > 0)
				{
					using (X509CertificateInfo xi = stack.Shift())
					{
						X509Certificate cert = xi.Certificate;
						if (cert != null)
						{
							this.Add(cert);
						}
					}
				}
			}
		}

		/// <summary>
		/// Creates a new chain from the specified PEM-formatted string
		/// </summary>
		/// <param name="pem"></param>
		public X509Chain(string pem)
			: this(new BIO(pem))
		{
		}
		#endregion

		#region Methods
		/// <summary>
		/// Returns X509_find_by_issuer_and_serial()
		/// </summary>
		/// <param name="issuer"></param>
		/// <param name="serial"></param>
		/// <returns></returns>
		public X509Certificate FindByIssuerAndSerial(X509Name issuer, int serial)
		{
			using (Asn1Integer asnInt = new Asn1Integer(serial))
			{
				IntPtr ptr = Native.X509_find_by_issuer_and_serial(this.ptr, issuer.Handle, asnInt.Handle);
				if (ptr == IntPtr.Zero)
					return null;
				X509Certificate cert = new X509Certificate(ptr, true);
				// Increase the reference count for the native pointer
				cert.AddRef();
				return cert;
			}
		}

		/// <summary>
		/// Returns X509_find_by_subject()
		/// </summary>
		/// <param name="subject"></param>
		/// <returns></returns>
		public X509Certificate FindBySubject(X509Name subject)
		{
			IntPtr ptr = Native.X509_find_by_subject(this.ptr, subject.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			X509Certificate cert = new X509Certificate(ptr, true);
			// Increase the reference count for the native pointer
			cert.AddRef();
			return cert;
		}
		#endregion
	}

	/// <summary>
	/// A List for X509Certificate types.
	/// </summary>
	public class X509List : List<X509Certificate>
	{
		#region Initialization
		/// <summary>
		/// Creates an empty X509List
		/// </summary>
		public X509List() { }

		/// <summary>
		/// Calls PEM_x509_INFO_read_bio()
		/// </summary>
		/// <param name="bio"></param>
		public X509List(BIO bio)
		{
			IntPtr sk = Native.ExpectNonNull(
				Native.PEM_X509_INFO_read_bio(bio.Handle, IntPtr.Zero, null, IntPtr.Zero));
			using (Core.Stack<X509CertificateInfo> stack = new Core.Stack<X509CertificateInfo>(sk, true))
			{
				while (stack.Count > 0)
				{
					using (X509CertificateInfo xi = stack.Shift())
					{
						if (xi.Certificate != null)
							this.Add(xi.Certificate);
					}
				}
			}
		}

		/// <summary>
		/// Populates this list from a PEM-formatted string
		/// </summary>
		/// <param name="pem"></param>
		public X509List(string pem)
			: this(new BIO(pem))
		{
		}

		/// <summary>
		/// Populates this list from a DER buffer.
		/// </summary>
		/// <param name="der"></param>
		public X509List(byte[] der)
		{
			BIO bio = new BIO(der);
			while (bio.NumberRead < der.Length)
			{
				X509Certificate x509 = X509Certificate.FromDER(bio);
				this.Add(x509);
			}
		}
		#endregion
	}
}
