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

// Copyright (c) 2006-2008 Frank Laub
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
using System.IO;
using System.Threading;
using OpenSSL.Crypto;

namespace OpenSSL.X509
{
    /// <summary>
	/// Used for generating sequence numbers by the CertificateAuthority
	/// </summary>
	public interface ISequenceNumber
	{
		/// <summary>
		/// Returns the next available sequence number
		/// </summary>
		/// <returns></returns>
		int Next();
	}

	/// <summary>
	/// Implements the ISequenceNumber interface. 
	/// The sequence number is read from a file, incremented, 
	/// then written back to the file
	/// </summary>
	public class FileSerialNumber : ISequenceNumber
	{
		private string path;
		/// <summary>
		/// Constructs a FileSerialNumber. The path specifies where 
		/// the serial number should be read and written to.
		/// </summary>
		/// <param name="path"></param>
		public FileSerialNumber(string path)
		{
			this.path = path;
		}

		#region ISequenceNumber Members
		/// <summary>
		/// Implements the Next() method of the ISequenceNumber interface.
		/// The sequence number is read from a file, incremented, 
		/// then written back to the file
		/// </summary>
		/// <returns></returns>
		public int Next()
		{
			FileInfo serialFile = new FileInfo(this.path);
			string name = serialFile.FullName.Replace('\\', '/');
			using (Mutex mutex = new Mutex(true, name))
			{
				mutex.WaitOne();
				int serial = 1;
				if (serialFile.Exists)
				{
					using (StreamReader sr = new StreamReader(serialFile.FullName))
					{
						string text = sr.ReadToEnd();
						serial = Convert.ToInt32(text);
						++serial;
					}
				}

				using(StreamWriter sr = new StreamWriter(serialFile.FullName))
				{
					sr.Write(serial.ToString());
				}
			
				return serial;
			}
		}
		#endregion
	}

	/// <summary>
	/// Simple implementation of the ISequenceNumber interface.
	/// </summary>
	public class SimpleSerialNumber : ISequenceNumber
	{
		private int seq;

		/// <summary>
		/// Construct a SimpleSerialNumber with the initial sequence number set to 0.
		/// </summary>
		public SimpleSerialNumber() { this.seq = 0; }

		/// <summary>
		/// Construct a SimpleSerialNumber with the initial sequence number
		/// set to the value specified by the seed parameter.
		/// </summary>
		/// <param name="seed"></param>
		public SimpleSerialNumber(int seed) { this.seq = seed; }

		#region ISequenceNumber Members

		/// <summary>
		/// Returns the next available sequence number.
		/// This implementation simply increments the current 
		/// sequence number and returns it.
		/// </summary>
		/// <returns></returns>
		public int Next()
		{
			return ++seq;
		}

		#endregion
	}

	/// <summary>
	/// High-level interface which does the job of a CA (Certificate Authority)
	/// Duties include processing incoming X509 requests and responding
	/// with signed X509 certificates, signed by this CA's private key.
	/// </summary>
	public class X509CertificateAuthority : IDisposable
	{
		#region Self-Signed Factory Methods

		/// <summary>
		/// Factory method which creates a X509CertifiateAuthority where
		/// the internal certificate is self-signed
		/// </summary>
		/// <param name="cfg"></param>
		/// <param name="seq"></param>
		/// <param name="subject"></param>
		/// <param name="start"></param>
		/// <param name="validity"></param>
		/// <returns></returns>
		public static X509CertificateAuthority SelfSigned(
			Configuration cfg,
			ISequenceNumber seq,
			X509Name subject,
			DateTime start,
			TimeSpan validity)
		{
			CryptoKey key;
			using (DSA dsa = new DSA(true))
			{
				key = new CryptoKey(dsa);
				// Dispose the DSA key, the CryptoKey assignment increments the reference count
			}
			X509Certificate cert = new X509Certificate(
				seq.Next(),
				subject,
				subject,
                key,
				start,
				start + validity);

			if(cfg != null)
				cfg.ApplyExtensions("v3_ca", cert, cert, null);

			cert.Sign(key, MessageDigest.DSS1);

			return new X509CertificateAuthority(cert, key, seq, cfg);
		}

        /// <summary>
        /// Factory method that creates a X509CertificateAuthority instance with
        /// an internal self signed certificate
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="seq"></param>
        /// <param name="key"></param>
        /// <param name="digest"></param>
        /// <param name="subject"></param>
        /// <param name="start"></param>
        /// <param name="validity"></param>
        /// <returns></returns>
        public static X509CertificateAuthority SelfSigned(
            Configuration cfg,
            ISequenceNumber seq,
            CryptoKey key,
            MessageDigest digest,
            X509Name subject,
            DateTime start,
            TimeSpan validity)
        {
            X509Certificate cert = new X509Certificate(
                seq.Next(),
                subject,
                subject,
                key,
                start,
                start + validity);

            if (cfg != null)
                cfg.ApplyExtensions("v3_ca", cert, cert, null);

            cert.Sign(key, digest);

            return new X509CertificateAuthority(cert, key, seq, cfg);
        }

        /// <summary>
        /// Factory method that creates a X509CertificateAuthority instance with
        /// an internal self signed certificate. This method allows creation without
        /// the need for the Configuration file, X509V3Extensions may be added
        /// with the X509V3ExtensionList parameter
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="key"></param>
        /// <param name="digest"></param>
        /// <param name="subject"></param>
        /// <param name="start"></param>
        /// <param name="validity"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static X509CertificateAuthority SelfSigned(
            ISequenceNumber seq,
            CryptoKey key,
            MessageDigest digest,
            X509Name subject,
            DateTime start,
            TimeSpan validity,
            X509V3ExtensionList extensions)
        {
            X509Certificate cert = new X509Certificate(
                seq.Next(),
                subject,
                subject,
                key,
                start,
                start + validity);

            if (null != extensions)
            {
                foreach (X509V3ExtensionValue extValue in extensions)
                {
                    X509Extension ext = new X509Extension(cert, cert, extValue.Name, extValue.IsCritical, extValue.Value);
                    cert.AddExtension(ext);
                }
            }

            cert.Sign(key, digest);

            return new X509CertificateAuthority(cert, key, seq, null);
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Constructs a X509CertifcateAuthority with the specified parameters.
		/// </summary>
		/// <param name="caCert"></param>
		/// <param name="caKey"></param>
		/// <param name="serial"></param>
		/// <param name="cfg"></param>
		public X509CertificateAuthority(X509Certificate caCert, CryptoKey caKey, ISequenceNumber serial, Configuration cfg)
		{
			if (!caCert.CheckPrivateKey(caKey))
				throw new Exception("The specified CA Private Key does match the specified CA Certificate");
			this.caCert = caCert;
			this.caKey = caKey;
			this.serial = serial;
			this.cfg = cfg;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Accessor to the CA's X509 Certificate
		/// </summary>
		public X509Certificate Certificate
		{
			get { return this.caCert; }
		}

		/// <summary>
		/// Accessor to the CA's key used for signing.
		/// </summary>
		public CryptoKey Key
		{
			get { return this.caKey; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Process and X509Request. This includes creating a new X509Certificate
		/// and signing this certificate with this CA's private key.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		/// <returns></returns>
		public X509Certificate ProcessRequest(X509Request request, DateTime startTime, DateTime endTime)
		{
            return ProcessRequest(request, startTime, endTime, MessageDigest.DSS1);
		}

        /// <summary>
        /// Process and X509Request. This includes creating a new X509Certificate
        /// and signing this certificate with this CA's private key.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="digest"></param>
        /// <returns></returns>
        public X509Certificate ProcessRequest(X509Request request, DateTime startTime, DateTime endTime, MessageDigest digest)
        {
            //using (CryptoKey pkey = request.PublicKey)
            //{
            //    if (!request.Verify(pkey))
            //        throw new Exception("Request signature validation failed");
            //}

            X509Certificate cert = new X509Certificate(
                serial.Next(),
                request.Subject,
                this.caCert.Subject,
                request.PublicKey,
                startTime,
                endTime);

            if (this.cfg != null)
                this.cfg.ApplyExtensions("v3_ca", this.caCert, cert, request);

            cert.Sign(this.caKey, digest);

            return cert;
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Dispose the key, certificate, and the configuration
		/// </summary>
		public void Dispose()
		{
			if (this.caKey != null)
			{
				this.caKey.Dispose();
				this.caKey = null;
			}

			if (this.caCert != null)
			{
				this.caCert.Dispose();
				this.caCert = null;
			}

			if (this.cfg != null)
			{
				this.cfg.Dispose();
				this.cfg = null;
			}
		}

		#endregion

		#region Fields
		private X509Certificate caCert;
		private CryptoKey caKey;
		private ISequenceNumber serial;
		private Configuration cfg;
		#endregion
	}
}
