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
using System.Diagnostics;

namespace OpenSSL.Core
{
	/// <summary>
	/// Wraps the BN_* set of functions.
	/// </summary>
	public class BigNumber : Base, IComparable<BigNumber>
	{
		#region Predefined Values
		/// <summary>
		/// Creates a BigNumber object by calling BN_value_one()
		/// </summary>
		public static BigNumber One = new BigNumber(Native.BN_value_one(), false);

		/// <summary>
		/// Calls BN_options()
		/// </summary>
		public static string Options
		{
			get { return Native.BN_options(); }
		}
		#endregion

		#region Initialization
		internal BigNumber(IntPtr ptr, bool owner) : base(ptr, owner) { }
		/// <summary>
		/// Calls BN_new()
		/// </summary>
		public BigNumber()
			: base(Native.ExpectNonNull(Native.BN_new()), true)
		{
		}

		/// <summary>
		/// Calls BN_dup() on the BigNumber passed in.
		/// </summary>
		/// <param name="rhs"></param>
		public BigNumber(BigNumber rhs)
			: base(Native.BN_dup(rhs.ptr), true)
		{
		}

		/// <summary>
		/// Creates a BigNumber by calling BN_set_word()
		/// </summary>
		/// <param name="value"></param>
		public BigNumber(uint value)
			: this()
		{
			Native.ExpectSuccess(Native.BN_set_word(this.ptr, value));
		}
		#endregion

		#region Conversion
		/// <summary>
		/// Calls BN_dec2bn()
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static BigNumber FromDecimalString(string str)
		{
			byte[] buf = Encoding.ASCII.GetBytes(str);
			IntPtr ptr;
            int ret = Native.BN_dec2bn(out ptr, buf);
            if (ret <= 0)
                throw new OpenSslException();
            return new BigNumber(ptr, true);
		}

		/// <summary>
		/// Calls BN_hex2bn()
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static BigNumber FromHexString(string str)
		{
			byte[] buf = Encoding.ASCII.GetBytes(str);
			IntPtr ptr;
            int ret = Native.BN_hex2bn(out ptr, buf);
            if (ret <= 0)
                throw new OpenSslException();
			return new BigNumber(ptr, true);
		}

		/// <summary>
		/// Calls BN_bin2bn()
		/// </summary>
		/// <param name="buf"></param>
		/// <returns></returns>
		public static BigNumber FromArray(byte[] buf)
		{
			IntPtr ptr = Native.BN_bin2bn(buf, buf.Length, IntPtr.Zero);
			return new BigNumber(Native.ExpectNonNull(ptr), true);
		}

		/// <summary>
		/// Calls BN_bn2dec()
		/// </summary>
		/// <returns></returns>
		public string ToDecimalString()
		{
			return Native.PtrToStringAnsi(Native.BN_bn2dec(this.ptr), true);
		}

		/// <summary>
		/// Calls BN_bn2hex()
		/// </summary>
		/// <returns></returns>
		public string ToHexString()
		{
			return Native.PtrToStringAnsi(Native.BN_bn2hex(this.ptr), true);
		}

		/// <summary>
		/// Calls BN_get_word()
		/// </summary>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static implicit operator uint(BigNumber rhs)
		{
			return Native.BN_get_word(rhs.ptr);
		}

		/// <summary>
		/// Creates a new BigNumber object from a uint.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static implicit operator BigNumber(uint value)
		{
			return new BigNumber(value);
		}

		/// <summary>
		/// Calls BN_bn2bin()
		/// </summary>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static implicit operator byte[](BigNumber rhs)
		{
			byte[] bytes = new byte[rhs.Bytes];
			Native.ExpectSuccess(Native.BN_bn2bin(rhs.ptr, bytes));
			return bytes;
		}
		
		/// <summary>
		/// Calls BN_bn2bin()
		/// </summary>
		/// <param name="bytes"></param>
		public void ToBytes(byte[] bytes)
		{
			Native.ExpectSuccess(Native.BN_bn2bin(this.ptr, bytes));
		}

		#endregion

		#region Properties
		/// <summary>
		/// Returns BN_num_bits()
		/// </summary>
		public int Bits
		{
			get { return Native.BN_num_bits(this.ptr); }
		}

		/// <summary>
		/// Converts the result of Bits into the number of bytes.
		/// </summary>
		public int Bytes
		{
			get { return (this.Bits + 7) / 8; }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Calls BN_clear()
		/// </summary>
		public void Clear()
		{
			Native.BN_clear(this.ptr);
		}

		/// <summary>
		/// Calls BN_rand_range()
		/// </summary>
		/// <param name="range"></param>
		/// <returns></returns>
		public static BigNumber NextRange(BigNumber range)
		{
			BigNumber bn = new BigNumber();
			Native.ExpectSuccess(Native.BN_rand_range(bn.Handle, range.Handle));
			return bn;
		}

		/// <summary>
		/// Calls BN_pseudo_rand()
		/// </summary>
		/// <param name="bits"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		/// <returns></returns>
		public static BigNumber PseudoNext(int bits, int top, int bottom)
		{
			BigNumber bn = new BigNumber();
			Native.ExpectSuccess(Native.BN_pseudo_rand(bn.Handle, bits, top, bottom));
			return bn;
		}

		/// <summary>
		/// Calls BN_pseudo_rand_range()
		/// </summary>
		/// <param name="range"></param>
		/// <returns></returns>
		public static BigNumber PseudoNextRange(BigNumber range)
		{
			BigNumber bn = new BigNumber();
			Native.ExpectSuccess(Native.BN_pseudo_rand_range(bn.Handle, range.Handle));
			return bn;
		}
		
		
		#endregion

		#region Operators
		/// <summary>
		/// Calls BN_add()
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static BigNumber operator + (BigNumber lhs, BigNumber rhs)
		{
			BigNumber ret = new BigNumber();
			Native.ExpectSuccess(Native.BN_add(ret.Handle, lhs.Handle, rhs.Handle));
			return ret;
		}

		/// <summary>
		/// Calls BN_sub()
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static BigNumber operator -(BigNumber lhs, BigNumber rhs)
		{
			BigNumber ret = new BigNumber();
			Native.ExpectSuccess(Native.BN_sub(ret.Handle, lhs.Handle, rhs.Handle));
			return ret;
		}

		/// <summary>
		/// Determines if lhs is by-value equal to rhs
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
        public static bool operator ==(BigNumber lhs, BigNumber rhs)
        {
			if (object.ReferenceEquals(lhs, rhs))
				return true;
			if ((object)lhs == null || (object)rhs == null)
				return false;
			return lhs.Equals(rhs);
        }

		/// <summary>
		/// Determines if lhs is by-value different than rhs
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
        public static bool operator !=(BigNumber lhs, BigNumber rhs)
        {
			return !(lhs == rhs);
        }
		#endregion

		#region Overrides
		/// <summary>
		/// Calls BN_cmp()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			BigNumber rhs = obj as BigNumber;
			if ((object)rhs == null)
				return false;
			return Native.BN_cmp(this.ptr, rhs.ptr) == 0;
		}

		/// <summary>
		/// Creates a hash code by converting this object to a decimal string and 
		/// returns the hash code of that string.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return ToDecimalString().GetHashCode();
		}

		/// <summary>
		/// Calls BN_print()
		/// </summary>
		/// <param name="bio"></param>
		public override void Print(BIO bio)
		{
			Native.ExpectSuccess(Native.BN_print(bio.Handle, this.ptr));
		}
		#endregion

		#region IDisposable Members

		/// <summary>
		/// Calls BN_free()
		/// </summary>
		protected override void OnDispose() {
			Native.BN_free(this.ptr);
		}

		#endregion

		#region IComparable<BigNumber> Members

		/// <summary>
		/// Calls BN_cmp()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(BigNumber other)
		{
			return Native.BN_cmp(this.ptr, other.ptr);
		}

		#endregion

		#region Callbacks

		/// <summary>
		/// Generator callback. Used mostly for status indications for long-
		/// running generator functions.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="n"></param>
		/// <param name="arg"></param>
		/// <returns></returns>
		public delegate int GeneratorHandler(int p, int n, object arg);

		internal class GeneratorThunk
		{
			private Native.bn_gencb_st gencb = new Native.bn_gencb_st();
			private GeneratorHandler OnGenerator;
			private object arg;

			public Native.bn_gencb_st CallbackStruct
			{
				get { return this.gencb; }
			}

			public GeneratorThunk(GeneratorHandler client, object arg) 
			{
				this.OnGenerator = client;
				this.arg = arg;

				this.gencb.ver = 2;
				this.gencb.arg = IntPtr.Zero;
				this.gencb.cb = this.OnGeneratorThunk;
			}

			internal int OnGeneratorThunk(int p, int n, IntPtr arg)
			{
                if (OnGenerator != null)
                {
                    try
                    {
                        return OnGenerator(p, n, this.arg);
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                }
                else
                {
                    // return 1 to allow generation to succeed with
                    // no user callback
                    return 1;
                }
			}
		}

		#endregion
		
		#region Context
		public class Context : Base
		{
			public Context()
				: base(Native.ExpectNonNull(Native.BN_CTX_new()), true) {
			}
			
			public BigNumber BigNumber {
				get { return new BigNumber(Native.ExpectNonNull(Native.BN_CTX_get(this.ptr)), false); }
			}
			
			public void Start() {
				Native.BN_CTX_start(this.ptr);
			}
			
			public void End() {
				Native.BN_CTX_end(this.ptr);
			}
			
			protected override void OnDispose() {
				Native.BN_CTX_free(this.ptr);
			}
		}
		#endregion
	}
}