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

namespace OpenSSL.Crypto
{
	/// <summary>
	/// Encapsulates the native openssl Diffie-Hellman functions (DH_*)
	/// </summary>
	public class DH : Base
	{
		/// <summary>
		/// Constant generator value of 2.
		/// </summary>
		public const int Generator2 = 2;

		/// <summary>
		/// Constant generator value of 5.
		/// </summary>
		public const int Generator5 = 5;
		
		private const int FlagCacheMont_P = 0x01;
		private const int FlagNoExpConstTime = 0x02;

		/// <summary>
		/// Flags for the return value of DH_check().
		/// </summary>
		[Flags]
		public enum CheckCode
		{
			/// <summary>
			/// 
			/// </summary>
			CheckP_NotPrime = 1,

			/// <summary>
			/// 
			/// </summary>
			CheckP_NotSafePrime = 2,

			/// <summary>
			/// 
			/// </summary>
			UnableToCheckGenerator = 4,

			/// <summary>
			/// 
			/// </summary>
			NotSuitableGenerator = 8,
		}

		private BigNumber.GeneratorThunk thunk = null;

		#region dh_st

		[StructLayout(LayoutKind.Sequential)]
		struct dh_st
		{
			public int pad;
			public int version;
			public IntPtr p;
			public IntPtr g;
			public int length;
			public IntPtr pub_key;
			public IntPtr priv_key;

			public int flags;
			public IntPtr method_mont_p;
			public IntPtr q;
			public IntPtr j;
			public IntPtr seed;
			public int seedlen;
			public IntPtr counter;

			public int references;
			#region CRYPTO_EX_DATA ex_data;
			public IntPtr ex_data_sk;
			public int ex_data_dummy;
			#endregion
			public IntPtr meth;
			public IntPtr engine;
		}
		#endregion

		#region Initialization
		internal DH(IntPtr ptr, bool owner) : base(ptr, owner) { }
		/// <summary>
		/// Calls DH_generate_parameters()
		/// </summary>
		/// <param name="primeLen"></param>
		/// <param name="generator"></param>
		public DH(int primeLen, int generator)
			: base(Native.ExpectNonNull(Native.DH_generate_parameters(primeLen, generator, IntPtr.Zero, IntPtr.Zero)), true)
		{
		}

		/// <summary>
		/// Calls DH_generate_parameters_ex()
		/// </summary>
		/// <param name="primeLen"></param>
		/// <param name="generator"></param>
		/// <param name="callback"></param>
		/// <param name="arg"></param>
		public DH(int primeLen, int generator, BigNumber.GeneratorHandler callback, object arg)
			: base(Native.ExpectNonNull(Native.DH_new()), true)
		{
			this.thunk = new BigNumber.GeneratorThunk(callback, arg);
			Native.ExpectSuccess(Native.DH_generate_parameters_ex(
				this.ptr,
				primeLen,
 				generator,
				this.thunk.CallbackStruct)
			);
		}

		/// <summary>
		/// Calls DH_new().
		/// </summary>
		public DH() 
			: base(Native.ExpectNonNull(Native.DH_new()), true) 
		{
			dh_st raw = this.Raw;
			raw.p = Native.BN_dup(BigNumber.One.Handle);
			raw.g = Native.BN_dup(BigNumber.One.Handle);
			this.Raw = raw;
		}

		/// <summary>
		/// Calls DH_new().
		/// </summary>
		/// <param name="p"></param>
		/// <param name="g"></param>
        public DH(BigNumber p, BigNumber g)
            : base(Native.ExpectNonNull(Native.DH_new()), true)
        {
            dh_st raw = this.Raw;
            raw.p = Native.BN_dup(p.Handle);
            raw.g = Native.BN_dup(g.Handle);
            this.Raw = raw;
        }

		/// <summary>
		/// Calls DH_new().
		/// </summary>
		/// <param name="p"></param>
		/// <param name="g"></param>
		/// <param name="pub_key"></param>
		/// <param name="priv_key"></param>
		public DH(BigNumber p, BigNumber g, BigNumber pub_key, BigNumber priv_key)
			: base(Native.ExpectNonNull(Native.DH_new()), true)
		{
			dh_st raw = this.Raw;
			raw.p = Native.BN_dup(p.Handle);
			raw.g = Native.BN_dup(g.Handle);
			raw.pub_key = Native.BN_dup(pub_key.Handle);
			raw.priv_key = Native.BN_dup(priv_key.Handle);
			this.Raw = raw;
		}

		/// <summary>
		/// Factory method that calls FromParametersPEM() to deserialize
		/// a DH object from a PEM-formatted string.
		/// </summary>
		/// <param name="pem"></param>
		/// <returns></returns>
		public static DH FromParameters(string pem)
		{
			return FromParametersPEM(new BIO(pem));
		}

		/// <summary>
		/// Factory method that calls PEM_read_bio_DHparams() to deserialize 
		/// a DH object from a PEM-formatted string using the BIO interface.
		/// </summary>
		/// <param name="bio"></param>
		/// <returns></returns>
		public static DH FromParametersPEM(BIO bio)
		{
			IntPtr ptr = Native.ExpectNonNull(Native.PEM_read_bio_DHparams(
				bio.Handle, IntPtr.Zero, null, IntPtr.Zero));
			return new DH(ptr, true);
		}

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr DH_new_delegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr d2i_DHparams_delegate(out IntPtr a, IntPtr pp, int length);

        private static IntPtr Managed_DH_new()
        {
            return Native.DH_new();
        }

        private static IntPtr Managed_d2i_DHparams(out IntPtr a, IntPtr pp, int length)
        {
            return Native.d2i_DHparams(out a, pp, length);
        }
        /// <summary>
		/// Factory method that calls XXX() to deserialize
		/// a DH object from a DER-formatted buffer using the BIO interface.
		/// </summary>
		/// <param name="bio"></param>
		/// <returns></returns>
		public static DH FromParametersDER(BIO bio)
		{
            DH_new_delegate dh_new = new DH_new_delegate(Managed_DH_new);
            d2i_DHparams_delegate d2i_DHparams = new d2i_DHparams_delegate(Managed_d2i_DHparams);
            IntPtr dh_new_ptr = Marshal.GetFunctionPointerForDelegate(dh_new);
            IntPtr d2i_DHparams_ptr = Marshal.GetFunctionPointerForDelegate(d2i_DHparams);
            IntPtr ptr = Native.ExpectNonNull(Native.ASN1_d2i_bio(dh_new_ptr, d2i_DHparams_ptr, bio.Handle, IntPtr.Zero));
            DH dh = new DH(ptr, true);
            return dh;
		}
		#endregion

		#region Methods
		/// <summary>
		/// Calls DH_generate_key().
		/// </summary>
		public void GenerateKeys()
		{
			Native.ExpectSuccess(Native.DH_generate_key(this.ptr));
		}

		/// <summary>
		/// Calls DH_compute_key().
		/// </summary>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public byte[] ComputeKey(BigNumber pubkey)
		{
			int len = Native.DH_size(this.ptr);
			byte[] key = new byte[len];
			Native.DH_compute_key(key, pubkey.Handle, this.ptr);
			return key;
		}

		/// <summary>
		/// Calls PEM_write_bio_DHparams().
		/// </summary>
		/// <param name="bio"></param>
		public void WriteParametersPEM(BIO bio)
		{
			Native.ExpectSuccess(Native.PEM_write_bio_DHparams(bio.Handle, this.ptr));
		}

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int i2d_DHparams_delegate(IntPtr a, IntPtr pp);

        private int Managed_i2d_DHparams(IntPtr a, IntPtr pp)
        {
            return Native.i2d_DHparams(a, pp);
        }

		/// <summary>
		/// Calls ASN1_i2d_bio() with the i2d = i2d_DHparams().
		/// </summary>
		/// <param name="bio"></param>
		public void WriteParametersDER(BIO bio)
		{
            i2d_DHparams_delegate i2d_DHparams = new i2d_DHparams_delegate(Managed_i2d_DHparams);
            IntPtr i2d_DHparams_ptr = Marshal.GetFunctionPointerForDelegate(i2d_DHparams);
            Native.ExpectSuccess(Native.ASN1_i2d_bio(i2d_DHparams_ptr, bio.Handle, this.ptr));
            //!!
            /*
            IntPtr hModule = Native.LoadLibrary(Native.DLLNAME);
			IntPtr i2d = Native.GetProcAddress(hModule, "i2d_DHparams");
			Native.FreeLibrary(hModule);
			
			Native.ExpectSuccess(Native.ASN1_i2d_bio(i2d, bio.Handle, this.ptr));
            */
		}

		/// <summary>
		/// Calls DHparams_print().
		/// </summary>
		/// <param name="bio"></param>
		public override void Print(BIO bio)
		{
			Native.ExpectSuccess(Native.DHparams_print(bio.Handle, this.ptr));
		}

		/// <summary>
		/// Calls DH_check().
		/// </summary>
		/// <returns></returns>
		public CheckCode Check()
		{
			int codes = 0;
			Native.ExpectSuccess(Native.DH_check(this.ptr, out codes));
			return (CheckCode)codes;
		}
		#endregion

		#region Properties
		private dh_st Raw
		{
			get { return (dh_st)Marshal.PtrToStructure(this.ptr, typeof(dh_st)); }
            set { Marshal.StructureToPtr(value, this.ptr, false); }
		}

		/// <summary>
		/// Accessor for the p value.
		/// </summary>
		public BigNumber P
		{
			get { return new BigNumber(this.Raw.p, false); }
			set 
			{
				dh_st raw = this.Raw;
				raw.p = Native.BN_dup(value.Handle);
				this.Raw = raw;
			}
		}

		/// <summary>
		/// Accessor for the g value.
		/// </summary>
		public BigNumber G
		{
			get { return new BigNumber(this.Raw.g, false); }
			set 
			{
				dh_st raw = this.Raw;
				raw.g = Native.BN_dup(value.Handle);
				this.Raw = raw;
			}
		}

		/// <summary>
		/// Accessor for the pub_key value.
		/// </summary>
		public BigNumber PublicKey
		{
			get { return new BigNumber(this.Raw.pub_key, false); }
            set
            {
                dh_st raw = this.Raw;
                raw.pub_key = Native.BN_dup(value.Handle);
                this.Raw = raw;
            }
        }

		/// <summary>
		/// Accessor for the priv_key value.
		/// </summary>
		public BigNumber PrivateKey
		{
			get { return new BigNumber(this.Raw.priv_key, false); } 
			set
            {
                dh_st raw = this.Raw;
                raw.priv_key = Native.BN_dup(value.Handle);
                this.Raw = raw;
            }
		}

		/// <summary>
		/// Creates a BIO.MemoryBuffer(), calls WriteParametersPEM() into this buffer, 
		/// then returns the buffer as a string.
		/// </summary>
		public string PEM
		{
			get
			{
				using (BIO bio = BIO.MemoryBuffer())
				{
					this.WriteParametersPEM(bio);
					return bio.ReadString();
				}
			}
		}

		/// <summary>
		/// Creates a BIO.MemoryBuffer(), calls WriteParametersDER() into this buffer, 
		/// then returns the buffer.
		/// </summary>
		public byte[] DER
		{
			get
			{
				using (BIO bio = BIO.MemoryBuffer())
				{
					this.WriteParametersDER(bio);
					return bio.ReadBytes((int)bio.NumberWritten).Array;
				}
			}
		}

		/// <summary>
		/// Sets or clears the FlagNoExpConstTime bit in the flags field.
		/// </summary>
		public bool NoExpConstantTime
		{
			get { return (this.Raw.flags & FlagNoExpConstTime) != 0; }
			set
			{
				dh_st raw = this.Raw;
				if (value)
					raw.flags |= FlagNoExpConstTime;
				else
					raw.flags &= ~FlagNoExpConstTime;
				this.Raw = raw;
			}
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Calls DH_free().
		/// </summary>
		protected override void OnDispose() {
			Native.DH_free(this.ptr);
		}

		#endregion
	}
}