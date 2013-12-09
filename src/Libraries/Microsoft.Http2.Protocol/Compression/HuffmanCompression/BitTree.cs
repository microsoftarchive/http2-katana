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

                    while (temp != null)
                    {
                        temp = !bits[i] ? temp.Left : temp.Right;

                        if (temp == null) 
                            continue;

                        symbolBits.Add(temp.Value);
                        i++;
                    }
                    var symbol = _table.GetByte(symbolBits);

                    if (symbol == HuffmanCodesTable.Eos)
                    {
                        result = new byte[stream.Position];
                        Buffer.BlockCopy(stream.GetBuffer(), 0, result, 0, result.Length);
                        break;
                    }

                    stream.WriteByte(symbol);
                }
            }
            return result;
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
