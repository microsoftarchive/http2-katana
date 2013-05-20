//-----------------------------------------------------------------------
// <copyright file="CompressionProcessor.cs" company="Microsoft Open Technologies, Inc.">
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
	using System;
	using zlib;
	using System.IO;
    using System.Text;

    // TODO: Split into a compressor and a decompressor
	public class CompressionProcessor : IDisposable
	{
		private MemoryStream _memStreamCompression;
        private MemoryStream _memStreamDecompression;
		private ZOutputStreamExt _compressOutZStream;
		private ZOutputStreamExt _decompressOutZStream;

		/// <summary>
		/// Compression data.
		/// </summary>
		public CompressionProcessor()
		{
            _memStreamCompression = new MemoryStream();
            _memStreamDecompression = new MemoryStream();
            _compressOutZStream = new ZOutputStreamExt(_memStreamCompression, CompressionDictionary.Dictionary, zlibConst.Z_DEFAULT_COMPRESSION);
            _decompressOutZStream = new ZOutputStreamExt(_memStreamDecompression, CompressionDictionary.Dictionary);
		}

		/// <summary>
		/// Copy stream buffer for input stream to output stream.
		/// </summary>
		/// <param name="input">Input stream.</param>
		/// <param name="output">Output stream.</param>
		private static void CopyStream(Stream input, Stream output)
		{
			byte[] buffer = new byte[input.Length];
			int len;

            while ((len = input.Read(buffer, 0, (int)input.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }

			output.Flush();
		}

        /// <summary>
        /// Compresses the specified input data.
        /// </summary>
        /// <param name="inData">The input data to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        public byte[] Compress(byte[] inData)
        {
            byte[] outData;
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, _compressOutZStream);
                outData = _memStreamCompression.ToArray();
                ClearStream(_memStreamCompression, (int)_memStreamCompression.Length);
            }
            return outData;
        }

        /// <summary>
        /// Clear stream buffer.
        /// </summary>
        /// <param name="input">Stream.</param>
        /// <param name="len">Length stream buffer.</param>
        private static void ClearStream(Stream input, int len)
        {
            byte[] buffer = new byte[len];
            input.Position = 0;
            input.Write(buffer, 0, len);
            input.SetLength(0);
        }

        /// <summary>
        /// Decompresses the specified input data.
        /// </summary>
        /// <param name="inData">The input data to decompress.</param>
        /// <param name="outData">The decompressed output data.</param>
        public byte[] Decompress(byte[] inData)
        {
            return Decompress(new ArraySegment<byte>(inData));
        }

        public byte[] Decompress(ArraySegment<byte> inData)
        {
            byte[] outData;
            using (Stream inMemoryStream = new MemoryStream(inData.Array, inData.Offset, inData.Count))
            {
                try
                {
                    CopyStream(inMemoryStream, _decompressOutZStream);
                }
                catch (ZStreamException)
                {
                    _decompressOutZStream.SetDictionary(CompressionDictionary.Dictionary);
                    _decompressOutZStream.finish();
                }

                outData = _memStreamDecompression.ToArray();
                ClearStream(_memStreamDecompression, (int)_memStreamDecompression.Length);
            }
            return outData;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
		public void Dispose()
		{
			// _decompressOutZStream.Dispose(); // TODO: Throws NullRef internally
            // _compressOutZStream.Dispose(); // TODO: Throws NullRef internally
            _memStreamCompression.Dispose();
            _memStreamDecompression.Dispose();
		}
	}
}
