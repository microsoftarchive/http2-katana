//-----------------------------------------------------------------------
// <copyright file="ZOutputStreamExt.cs" company="Microsoft Open Technologies, Inc.">
//
// The copyright in this software is being made available under the BSD License, included below. 
// This software may be subject to other third party and contributor rights, including patent rights, 
// and no such rights are granted under this license.
//
// Copyright (c) 2012, Microsoft Open Technologies, Inc. 
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer.
// - Redistributions in binary form must reproduce the above copyright notice, 
//   this list of conditions and the following disclaimer in the documentation 
//   and/or other materials provided with the distribution.
// - Neither the name of Microsoft Open Technologies, Inc. nor the names of its contributors 
//   may be used to endorse or promote products derived from this software 
//   without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// </copyright>
//-----------------------------------------------------------------------

namespace SharedProtocol.Compression
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using zlib;

	/// <summary>
	/// ZStream with dictionary support.
	/// </summary>
	class ZOutputStreamExt : ZOutputStream
	{
        public byte[] _dictionary = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZOutputStreamExt"/> class.
		/// </summary>
		/// <param name="outStream">The out stream.</param>
        /// <param name="isCompressor">Is stream compressor or decompressor.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="level">Compression level.</param>
        public ZOutputStreamExt(Stream outStream, byte[] dictionary, int level)
            : base(outStream, level)
        {
            _dictionary = dictionary;
            // TODO: Setting the seed dictionary always causes decompression to fail
            // z.deflateSetDictionary(_dictionary, _dictionary.Length);
            FlushMode = zlibConst.Z_SYNC_FLUSH;
        }

        // Decompression, no level flag
        public ZOutputStreamExt(Stream outStream, byte[] dictionary)
            : base(outStream)
        {
            _dictionary = dictionary;
            z.inflateSetDictionary(_dictionary, _dictionary.Length);
        }

		/// <summary>
		/// Set dictionary compress.
		/// </summary>
		/// <param name="dictionary">Dictionary compression.</param>
		/// <returns></returns>
		internal int SetDictionary(byte[] dictionary)
		{
			int error;
			if (compress)
			{
			    _dictionary = dictionary;
                z.deflateInit(zlibConst.Z_DEFAULT_COMPRESSION);
				error = z.deflateSetDictionary(dictionary, dictionary.Length);
			}
			else
			{
                _dictionary = dictionary;
				error = z.inflateSetDictionary(dictionary, dictionary.Length);
			}

			return error;
		}
	}
}
