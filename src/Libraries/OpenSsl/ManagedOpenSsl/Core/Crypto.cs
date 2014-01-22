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

namespace OpenSSL.Core
{
	/// <summary>
	/// V_CRYPTO_MDEBUG_*
	/// </summary>
	[Flags]
	public enum DebugOptions
	{
		/// <summary>
		/// V_CRYPTO_MDEBUG_TIME 
		/// </summary>
		Time = 0x01,

		/// <summary>
		/// V_CRYPTO_MDEBUG_THREAD
		/// </summary>
		Thread = 0x02,

		/// <summary>
		/// V_CRYPTO_MDEBUG_ALL 
		/// </summary>
		All = Time | Thread,
	}

	/// <summary>
	/// CRYPTO_MEM_CHECK_*
	/// </summary>
	public enum MemoryCheck
	{
		/// <summary>
		/// CRYPTO_MEM_CHECK_OFF 
		/// for applications
		/// </summary>
		Off = 0x00,

		/// <summary>
		/// CRYPTO_MEM_CHECK_ON 
		/// for applications
		/// </summary>
		On = 0x01,

		/// <summary>
		/// CRYPTO_MEM_CHECK_ENABLE
		/// for library-internal use
		/// </summary>
		Enable = 0x02,

		/// <summary>
		/// CRYPTO_MEM_CHECK_DISABLE
		/// for library-internal use
		/// </summary>
		Disable = 0x03,
	}

	/// <summary>
	/// Exposes the CRYPTO_* functions
	/// </summary>
	public class CryptoUtil
	{
		/// <summary>
		/// Returns MD2_options()
		/// </summary>
		public static string MD2_Options
		{
			get { return Native.MD2_options(); }
		}

		/// <summary>
		/// Returns RC4_options()
		/// </summary>
		public static string RC4_Options
		{
			get { return Native.RC4_options(); }
		}

		/// <summary>
		/// Returns DES_options()
		/// </summary>
		public static string DES_Options
		{
			get { return Native.DES_options(); }
		}

		/// <summary>
		/// Returns idea_options()
		/// </summary>
		public static string Idea_Options
		{
			get { return Native.idea_options(); }
		}

		/// <summary>
		/// Returns BF_options()
		/// </summary>
		public static string Blowfish_Options
		{
			get { return Native.BF_options(); }
		}

		/// <summary>
		/// Calls CRYPTO_malloc_debug_init()
		/// </summary>
		public static void MallocDebugInit()
		{
			Native.CRYPTO_malloc_debug_init();
		}

		/// <summary>
		/// Calls CRYPTO_dbg_set_options()
		/// </summary>
		/// <param name="options"></param>
		public static void SetDebugOptions(DebugOptions options)
		{
			Native.CRYPTO_dbg_set_options((int)options);
		}

		/// <summary>
		/// Calls CRYPTO_mem_ctrl()
		/// </summary>
		/// <param name="options"></param>
		public static void SetMemoryCheck(MemoryCheck options)
		{
			Native.CRYPTO_mem_ctrl((int)options);
		}

		/// <summary>
		/// Calls CRYPTO_cleanup_all_ex_data()
		/// </summary>
		public static void Cleanup()
		{
			Native.CRYPTO_cleanup_all_ex_data();
		}

		/// <summary>
		/// Calls ERR_remove_state()
		/// </summary>
		/// <param name="value"></param>
		public static void RemoveState(uint value)
		{
			Native.ERR_remove_state(value);
		}

		/// <summary>
		/// CRYPTO_MEM_LEAK_CB
		/// </summary>
		/// <param name="order"></param>
		/// <param name="file"></param>
		/// <param name="line"></param>
		/// <param name="num_bytes"></param>
		/// <param name="addr"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MemoryLeakHandler(uint order, IntPtr file, int line, int num_bytes, IntPtr addr);

		/// <summary>
		/// Calls CRYPTO_mem_leaks_cb()
		/// </summary>
		/// <param name="callback"></param>
		public static void CheckMemoryLeaks(MemoryLeakHandler callback)
		{
			Native.CRYPTO_mem_leaks_cb(callback);
		}

        public static void ErrFreeStrings()
        {
            Native.ERR_free_strings();
        }
	}
}
