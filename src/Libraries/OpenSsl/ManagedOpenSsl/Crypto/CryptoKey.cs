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

// Copyright (c) 2006-2012 Frank Laub
// All rights reserved.
//
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

namespace OpenSSL.Crypto
{
	/// <summary>
	/// Wraps the native OpenSSL EVP_PKEY object
	/// </summary>
	public class CryptoKey : BaseCopyableRef<CryptoKey>
	{
		/// <summary>
		/// Set of types that this CryptoKey can be.
		/// </summary>
		public enum KeyType
		{
			/// <summary>
			/// EVP_PKEY_RSA 
			/// </summary>
			RSA = 6,
			/// <summary>
			/// EVP_PKEY_DSA
			/// </summary>
			DSA = 116,
			/// <summary>
			/// EVP_PKEY_DH
			/// </summary>
			DH = 28,
			/// <summary>
			/// EVP_PKEY_EC
			/// </summary>
			EC = 408
		}

		const int EVP_PKEY_RSA = 6;
		const int EVP_PKEY_DSA = 116;
		const int EVP_PKEY_DH = 28;
		const int EVP_PKEY_EC = 408;

		[StructLayout(LayoutKind.Sequential)]
		struct EVP_PKEY
		{
			public int type;
			public int save_type;
			public int references;
			public IntPtr ptr;
			public int save_parameters;
			public IntPtr attributes;
		}

		#region Initialization
		internal CryptoKey(IntPtr ptr, bool owner) 
			: base(ptr, owner) 
		{ }

		/// <summary>
		/// Calls EVP_PKEY_new()
		/// </summary>
		public CryptoKey() 
			: base(Native.ExpectNonNull(Native.EVP_PKEY_new()), true) 
		{ }

		/// <summary>
		/// Calls PEM_read_bio_PUBKEY()
		/// </summary>
		/// <param name="pem"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static CryptoKey FromPublicKey(string pem, string password)
		{
			using (BIO bio = new BIO(pem))
			{
				return FromPublicKey(bio, password);
			}
		}

		/// <summary>
		/// Calls PEM_read_bio_PUBKEY()
		/// </summary>
		/// <param name="bio"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static CryptoKey FromPublicKey(BIO bio, string password)
		{
			PasswordCallback callback = new PasswordCallback(password);
			return FromPublicKey(bio, callback.OnPassword, null);
		}

		/// <summary>
		/// Calls PEM_read_bio_PUBKEY()
		/// </summary>
		/// <param name="bio"></param>
		/// <param name="handler"></param>
		/// <param name="arg"></param>
		/// <returns></returns>
		public static CryptoKey FromPublicKey(BIO bio, PasswordHandler handler, object arg)
		{
			PasswordThunk thunk = new PasswordThunk(handler, arg);
			IntPtr ptr = Native.ExpectNonNull(Native.PEM_read_bio_PUBKEY(
				bio.Handle,
				IntPtr.Zero,
				thunk.Callback,
				IntPtr.Zero
			));

			return new CryptoKey(ptr, true);
		}

		/// <summary>
		/// Calls PEM_read_bio_PrivateKey()
		/// </summary>
		/// <param name="pem"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static CryptoKey FromPrivateKey(string pem, string password)
		{
			using (BIO bio = new BIO(pem))
			{
				return FromPrivateKey(bio, password);
			}
		}

		/// <summary>
		/// Calls PEM_read_bio_PrivateKey()
		/// </summary>
		/// <param name="bio"></param>
		/// <param name="passwd"></param>
		/// <returns></returns>
		public static CryptoKey FromPrivateKey(BIO bio, string passwd)
		{
			PasswordCallback callback = new PasswordCallback(passwd);
			return FromPrivateKey(bio, callback.OnPassword, null);
		}

		/// <summary>
		/// Calls PEM_read_bio_PrivateKey()
		/// </summary>
		/// <param name="bio"></param>
		/// <param name="handler"></param>
		/// <param name="arg"></param>
		/// <returns></returns>
		public static CryptoKey FromPrivateKey(BIO bio, PasswordHandler handler, object arg)
		{
			PasswordThunk thunk = new PasswordThunk(handler, arg);
			IntPtr ptr = Native.ExpectNonNull(Native.PEM_read_bio_PrivateKey(
				bio.Handle,
				IntPtr.Zero,
				thunk.Callback,
				IntPtr.Zero
			));

			return new CryptoKey(ptr, true);
		}

		/// <summary>
		/// Calls EVP_PKEY_set1_DSA()
		/// </summary>
		/// <param name="dsa"></param>
		public CryptoKey(DSA dsa)
			: this()
		{
			Native.ExpectSuccess(Native.EVP_PKEY_set1_DSA(this.ptr, dsa.Handle));
		}

		/// <summary>
		/// Calls EVP_PKEY_set1_RSA()
		/// </summary>
		/// <param name="rsa"></param>
		public CryptoKey(RSA rsa)
			: this()
		{
			Native.ExpectSuccess(Native.EVP_PKEY_set1_RSA(this.ptr, rsa.Handle));
		}

		/// <summary>
		/// Calls EVP_PKEY_set1_DH()
		/// </summary>
		/// <param name="dh"></param>
		public CryptoKey(DH dh)
			: this()
		{
			Native.ExpectSuccess(Native.EVP_PKEY_set1_DH(this.ptr, dh.Handle));
		}
		#endregion

		#region Properties
		private EVP_PKEY Raw
		{
			get { return (EVP_PKEY)Marshal.PtrToStructure(this.ptr, typeof(EVP_PKEY)); }
		}

		/// <summary>
		/// Returns EVP_PKEY_type()
		/// </summary>
		public KeyType Type
		{
			get
			{
				int ret = Native.EVP_PKEY_type(this.Raw.type);
				switch (ret)
				{
					case EVP_PKEY_EC:
						return KeyType.EC;
					case EVP_PKEY_DH:
						return KeyType.DH;
					case EVP_PKEY_DSA:
						return KeyType.DSA;
					case EVP_PKEY_RSA:
						return KeyType.RSA;
					default:
						throw new NotSupportedException();
				}
			}
		}

		/// <summary>
		/// Returns EVP_PKEY_bits()
		/// </summary>
		public int Bits
		{
			get { return Native.EVP_PKEY_bits(this.ptr); }
		}

		/// <summary>
		/// Returns EVP_PKEY_size()
		/// </summary>
		public int Size
		{
			get { return Native.EVP_PKEY_size(this.ptr); }
		}
		#endregion

		#region Methods

		/// <summary>
		/// Calls EVP_PKEY_assign()
		/// </summary>
		/// <param name="type"></param>
		/// <param name="key"></param>
		public void Assign(int type, byte[] key)
		{
			Native.ExpectSuccess(Native.EVP_PKEY_assign(this.ptr, type, key));
		}

		/// <summary>
		/// Returns EVP_PKEY_get1_DSA()
		/// </summary>
		/// <returns></returns>
		public DSA GetDSA()
		{
			if (this.Type != KeyType.DSA)
				throw new InvalidOperationException();
			return new DSA(Native.ExpectNonNull(Native.EVP_PKEY_get1_DSA(this.ptr)), true);
		}

		/// <summary>
		/// Returns EVP_PKEY_get1_DH()
		/// </summary>
		/// <returns></returns>
		public DH GetDH()
		{
			if (this.Type != KeyType.DH)
				throw new InvalidOperationException();
			return new DH(Native.ExpectNonNull(Native.EVP_PKEY_get1_DH(this.ptr)), false);
		}

		/// <summary>
		/// Returns EVP_PKEY_get1_RSA()
		/// </summary>
		/// <returns></returns>
		public RSA GetRSA()
		{
			if (this.Type != KeyType.RSA)
				throw new InvalidOperationException();
			return new RSA(Native.ExpectNonNull(Native.EVP_PKEY_get1_RSA(this.ptr)), false);
		}

		/// <summary>
		/// Calls PEM_write_bio_PKCS8PrivateKey
		/// </summary>
		/// <param name="bp"></param>
		/// <param name="cipher"></param>
		/// <param name="password"></param>
		public void WritePrivateKey(BIO bp, Cipher cipher, string password)
		{
			PasswordCallback callback = new PasswordCallback(password);
			WritePrivateKey(bp, cipher, callback.OnPassword, null);
		}

		/// <summary>
		/// Calls PEM_write_bio_PKCS8PrivateKey
		/// </summary>
		/// <param name="bp"></param>
		/// <param name="cipher"></param>
		/// <param name="handler"></param>
		/// <param name="arg"></param>
		public void WritePrivateKey(BIO bp, Cipher cipher, PasswordHandler handler, object arg)
		{
			PasswordThunk thunk = new PasswordThunk(handler, null);
			Native.ExpectSuccess(Native.PEM_write_bio_PKCS8PrivateKey(bp.Handle, this.ptr, cipher.Handle, IntPtr.Zero, 0, thunk.Callback, IntPtr.Zero));
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Calls EVP_PKEY_free()
		/// </summary>
		protected override void OnDispose()
		{
			Native.EVP_PKEY_free(this.ptr);
		}

		/// <summary>
		/// Returns CompareTo(obj)
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			CryptoKey rhs = obj as CryptoKey;
			if (rhs == null)
				return false;
			return Native.EVP_PKEY_cmp(this.ptr, rhs.Handle) == 1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		internal override CryptoLockTypes LockType
		{
			get { return CryptoLockTypes.CRYPTO_LOCK_X509_PKEY; }
		}

		internal override Type RawReferenceType
		{
			get { return typeof(EVP_PKEY); }
		}

		/// <summary>
		/// Calls appropriate Print() based on the type.
		/// </summary>
		/// <param name="bio"></param>
		public override void Print(BIO bio)
		{
			switch (this.Type)
			{
				case KeyType.RSA:
					GetRSA().Print(bio);
					break;
				case KeyType.DSA:
					GetDSA().Print(bio);
					break;
				case KeyType.EC:
					break;
				case KeyType.DH:
					GetDH().Print(bio);
					break;
			}
		}

		#endregion
	}
}
