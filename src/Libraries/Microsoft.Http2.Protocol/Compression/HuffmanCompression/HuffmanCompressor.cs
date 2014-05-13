// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    internal class HuffmanCompressionProcessor
    {
        private BitTree _tree;
        private HuffmanCodesTable _table;

        private BitTree _responseTree;

        public HuffmanCompressionProcessor()
        {
            _table = new HuffmanCodesTable();
            _tree = new BitTree(_table);
        }

        public byte[] Compress(byte[] data)
        {
            var huffmanEncodedMessage = new List<bool>();

            foreach (var bt in data)
            {
                huffmanEncodedMessage.AddRange(_table.GetBits(bt));
            }

            // Adds most significant bytes of EOS
            int temp = 8 - huffmanEncodedMessage.Count % 8;
            int numberOfBitsInPadding = temp == 8 ? 0 : temp;

            for (int i = 0; i < numberOfBitsInPadding; i++)
            {
                huffmanEncodedMessage.Add(HuffmanCodesTable.Eos[i]);
            }
            
            return BinaryConverter.ToBytes(huffmanEncodedMessage);
        }

        public byte[] Decompress(byte[] compressed)
        {
            var bits = BinaryConverter.ToBits(compressed);
            return _tree.GetBytes(bits);
        }
    }
}
