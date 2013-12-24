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
        private BitTree _requestTree;
        private HuffmanCodesTable _requestTable;

        private BitTree _responseTree;
        private HuffmanCodesTable _responseTable;

        public HuffmanCompressionProcessor()
        {
            _requestTable = new HuffmanCodesTable(isRequest: true);
            _requestTree = new BitTree(_requestTable, true);

            _responseTable = new HuffmanCodesTable(isRequest: false);
            _responseTree = new BitTree(_responseTable, false);
        }

        public byte[] Compress(byte[] data, bool isRequest)
        {
            var huffmanEncodedMessage = new List<bool>();

            var table = isRequest ? _requestTable : _responseTable;

            foreach (var bt in data)
            {
                huffmanEncodedMessage.AddRange(table.GetBits(bt));
            }

            //add finish symbol
            huffmanEncodedMessage.AddRange(isRequest ? HuffmanCodesTable.ReqEos : HuffmanCodesTable.RespEos);

            return BinaryConverter.ToBytes(huffmanEncodedMessage);
        }

        public byte[] Decompress(byte[] compressed, bool isRequest)
        {
            var bits = BinaryConverter.ToBits(compressed);

            var tree = isRequest ? _requestTree : _responseTree;

            return tree.GetBytes(bits);
        }
    }
}
