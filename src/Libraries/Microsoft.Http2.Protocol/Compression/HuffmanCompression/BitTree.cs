// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    internal class BitTree
    {
        private Node _root;
        private HuffmanCodesTable _table;

        public BitTree(HuffmanCodesTable table)
        {
            _table = table;
            _root = new Node(false, null);
            BuildTree(table);
        }

        private void BuildTree(HuffmanCodesTable table)
        {
            foreach (var bits in table.HuffmanTable.Keys)
            {
                Add(bits);
            }
            
            Add(HuffmanCodesTable.Eos); //Add eos into codes table
        }

        private void Add(bool[] bits)
        {
            if (bits == null) 
                throw new ArgumentNullException("bits is null");

            Node temp = _root;

            for(int i = 0 ; i < bits.Length ; i++)
            {
                bool bit = bits[i];
                if (!bit)
                {
                    if (temp.Left == null)
                        temp.Left = new Node(false, temp);

                    temp = temp.Left;
                }
                else
                {
                    if (temp.Right == null)
                        temp.Right = new Node(true, temp);

                    temp = temp.Right;
                }
            }
        }

        public byte[] GetBytes(bool[] bits)
        {
            if (bits == null) 
                throw new ArgumentNullException("bits is null");

            byte[] result = null;
            using (var stream = new MemoryStream())
            {
                int i = 0;

                while (i < bits.Length)
                {
                    Node temp = _root;
                    var symbolBits = new List<bool>();

                    bool isEos = true;

                    int j = 0;
                    while (temp != null)
                    {
                        
                        temp = !bits[i] ? temp.Left : temp.Right;

                        if (temp == null) 
                            continue;

                        symbolBits.Add(temp.Value);
                        isEos &= temp.Value == HuffmanCodesTable.Eos[j];

                        if (isEos && ++j == HuffmanCodesTable.Eos.Length)
                        {
                            result = new byte[stream.Position];
                            Buffer.BlockCopy(stream.GetBuffer(), 0, result, 0, result.Length);
                            return result;
                        }

                        i++;
                    }

                    var symbol = _table.GetByte(symbolBits);
                    stream.WriteByte(symbol);
                }
            }

            throw new Exception("End of message is not recognized!");
        }

        private class Node
        {
            public bool Value { get; private set; }

            public Node Left { get; set; }
            public Node Right { get; set; }
            public Node Parent { get; private set; }

            public Node(bool value, Node parent, Node left = null, Node right = null)
            {
                Value = value;
                Left = left;
                Right = right;
                Parent = parent;
            }
        }
    }
}
