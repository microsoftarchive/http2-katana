// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Http2.Protocol.Exceptions;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    using Map = Dictionary<bool[], byte>;

    internal partial class HuffmanCodesTable
    {
        private bool _isRequest;
        private const bool T = true;
        private const bool F = false;

        public HuffmanCodesTable(bool isRequest)
        {
            _isRequest = isRequest;
        }

        public int Size 
        {
            get
            {
                Map bitsMap = _isRequest ? _reqSymbolBitsMap : _respSymbolBitsMap;
                return bitsMap.Keys.Sum(value => value.Length);
            }
        }

        public Map HuffmanTable
        {
            get
            {
                return _isRequest ? _reqSymbolBitsMap : _respSymbolBitsMap;
            }
            set
            {
                Debug.Assert(value != null);
                if (_isRequest)
                    _reqSymbolBitsMap = value;
                else
                    _respSymbolBitsMap = value;
            }
        }

        public byte GetByte(bool[] bits)
        {
            Map bitsMap = _isRequest ? _reqSymbolBitsMap : _respSymbolBitsMap;
            foreach (var tableBits in bitsMap.Keys)
            {
                if (tableBits.Length != bits.Length)
                    continue;

                bool match = true;
                for (byte i = 0; i < bits.Length; i++)
                {
                    if (bits[i] != tableBits[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return bitsMap[tableBits];
                }
            }

            throw new CompressionError(new Exception("symbol does not present in the alphabeth"));
        }

        public byte GetByte(List<bool> bits)
        {
            Map bitsMap = _isRequest ? _reqSymbolBitsMap : _respSymbolBitsMap;
            foreach (var tableBits in bitsMap.Keys)
            {
                if (tableBits.Length != bits.Count)
                    continue;

                bool match = true;
                for (byte i = 0; i < bits.Count; i++)
                {
                    if (bits[i] != tableBits[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return bitsMap[tableBits];
                }
            }

            throw new CompressionError(new Exception("symbol is not present in the alphabeth"));
        }

        public bool[] GetBits(byte c)
        {
            var bitsMap = _isRequest ? _reqSymbolBitsMap : _respSymbolBitsMap;
            var val = bitsMap.FirstOrDefault(pair => pair.Value == c).Key;

            if (val == null)
                throw new CompressionError(new Exception("symbol does not present in the alphabeth"));

            return val;
        }
    }
}
