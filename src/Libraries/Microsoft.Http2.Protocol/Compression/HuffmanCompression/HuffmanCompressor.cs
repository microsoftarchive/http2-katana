using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    internal class HuffmanCompressionProcessor
    {
        private BitTree _tree;
        private HuffmanCodesTable _table;

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

            //add finish symbol
            huffmanEncodedMessage.AddRange(_table.GetBits(HuffmanCodesTable.Eos));

            return BinaryConverter.ToBytes(huffmanEncodedMessage);
        }

        public byte[] Decompress(byte[] compressed)
        {
            var bits = BinaryConverter.ToBits(compressed);
            return _tree.GetBytes(bits);
        }
    }
}
