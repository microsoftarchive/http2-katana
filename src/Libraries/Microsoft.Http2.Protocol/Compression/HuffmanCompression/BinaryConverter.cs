// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    internal static class BinaryConverter
    {
        public static bool[] ToBits(byte[] bytes)
        {
            var result = new bool[bytes.Length * 8];
            for (int i = 0; i < bytes.Length; i++)
            {
                for (byte j = 0; j < 8; j++)
                {
                    result[i*8 + j] = GetBit(bytes[i], (byte)(7 - j));
                }
            }

            return result;
        }

        public static byte[] ToBytes(bool[] bools)
        {
            var result = new byte[bools.Length / 8 + 1];
            int offset = 0;
            byte count = 8;
            int resIndex = 0;

            while (count != 0)
            {
                result[resIndex++] = GetByte(bools, offset, count);
                offset += count;
                int roffset = bools.Length - offset;
                count = roffset >= 8 ? (byte)8 : (byte) roffset;
            }

            return result;
        }

        public static byte[] ToBytes(List<bool> bools)
        {
            var result = new byte[bools.Count / 8];
            int offset = 0;
            byte count = 8;
            int resIndex = 0;

            while (count != 0)
            {
                result[resIndex++] = GetByte(bools, offset, count);
                offset += count;
                int roffset = bools.Count - offset;
                count = roffset >= 8 ? (byte)8 : (byte)roffset;
            }

            return result;
        }

        private static byte GetByte(List<bool> bits, int offset, byte count)
        {
            if (count == 0)
                throw new ArgumentException("count is 0");
            if (count > 8)
                throw new ArgumentException("byte is 8 bits");

            byte result = 0;
            int endIndex = offset + count;
            byte bitIndex = 7;
            for (int i = offset; i < endIndex; i++, bitIndex--)
            {
                if (bits[i])
                    result |= (byte)(1 << bitIndex);
            }

            return result;
        }

        private static byte GetByte(bool[] bits, int offset, byte count)
        {
           if (count == 0)
               throw new ArgumentException("count is 0");
           if (count > 8)
                throw new ArgumentException("byte is 8 bits");

            byte result = 0;
            int endIndex = offset + count;
            byte bitIndex = 7;
            for (int i = offset; i < endIndex; i++, bitIndex--)
            {
                if (bits[i])
                    result |= (byte) (1 << bitIndex);
            }

            return result;
        }

        private static bool GetBit(byte b, byte pos)
        {
            if (pos > 7)
                throw new ArgumentOutOfRangeException("pos > 7");

            byte mask = (byte)(1 << pos);

            byte masked = (byte)(b & mask);

            return masked != 0;
        }
    }
}