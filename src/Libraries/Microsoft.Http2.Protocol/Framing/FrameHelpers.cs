using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace SharedProtocol.Framing
{
    // Helpers for reading binary fields of various sizes
    internal static class FrameHelpers
    {
        public static byte SetBit(byte input, bool value, byte offset)
        {
            Contract.Assert(offset <= 7);

            if (value == GetBit(input, offset))
            {
                return input;
            }

            return (byte)(input ^ (1 << offset));
        }

        public static bool GetBit(byte input, byte offset)
        {
            Contract.Assert(offset <= 7);

            return (input >> offset) % 2 == 1;
        }

        public static bool GetHighBitAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            return ((0x80 & buffer[offset]) == 0x80);
        }

        public static void SetHighBitAt(byte[] buffer, int offset, bool value)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            if (value)
            {
                buffer[offset] |= 0x80;
            }
            else
            {
                buffer[offset] &= 0x7F;
            }
        }

        public static int GetHigh3BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            return ((0xE0 & buffer[offset]) >> 5);
        }

        public static void SetHigh3BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            Contract.Assert(value >= 0 && value <= 7);
            byte lower5Bits = (byte)(buffer[offset] & 0x1F);
            byte upper3Bits = (byte)(value << 5);
            buffer[offset] = (byte)(upper3Bits | lower5Bits);
        }

        public static int Get5BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            return (0x1F & buffer[offset]);
        }

        public static void Set5BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0x1F);
            byte lower5Bits = (byte)(value & 0x1F);
            byte upper3Bits = (byte)(buffer[offset] & 0xE0);
            buffer[offset] = (byte)(upper3Bits | lower5Bits);
        }

        public static int Get8BitsAt(byte[] buffer, int offset, byte value)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            return (0x1F & buffer[offset]);
        }

        public static void Set8BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0x1F);
            byte lower5Bits = (byte)(value & 0x1F);
            byte upper3Bits = (byte)(buffer[offset] & 0xE0);
            buffer[offset] = (byte)(upper3Bits | lower5Bits);
        }

        public static int Get15BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset + 1 < buffer.Length);
            int highByte = (buffer[offset] & 0x7F);
            return (highByte << 8) | buffer[offset + 1];
        }

        public static void Set15BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset + 1 < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0x7FFF);
            buffer[offset] |= (byte)((value >> 8) & 0x7F);
            buffer[offset + 1] = (byte)value;
        }

        public static int Get16BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset + 1 < buffer.Length);
            return (buffer[offset] << 8) | buffer[offset + 1];
        }

        public static void Set16BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset + 1 < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0xFFFF);
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)value;
        }

        public static int Get24BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset + 2 < buffer.Length);
            return (buffer[offset] << 16) | (buffer[offset + 1] << 8) | buffer[offset + 2];
        }

        public static void Set24BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset + 2 < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0xFFFFFF);
            buffer[offset] = (byte)(value >> 16);
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)value;
        }

        public static int Get31BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset + 3 < buffer.Length);
            int highByte = (buffer[offset] & 0x7F);
            return (highByte << 24)
                | buffer[offset + 1] << 16
                | buffer[offset + 2] << 8
                | buffer[offset + 3];
        }

        public static void Set31BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset + 3 < buffer.Length);
            Contract.Assert(value >= 0 && value <= 0x7FFFFF);
            buffer[offset] |= (byte)((value >> 24) & 0x7F);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        public static int Get32BitsAt(byte[] buffer, int offset)
        {
            Contract.Assert(offset >= 0 && offset + 3 < buffer.Length);
            return (buffer[offset] << 24)
                | buffer[offset + 1] << 16
                | buffer[offset + 2] << 8
                | buffer[offset + 3];
        }

        public static void Set32BitsAt(byte[] buffer, int offset, int value)
        {
            Contract.Assert(offset >= 0 && offset + 3 < buffer.Length);
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        // TODO: The spec reallly needs to change the header encoding to UTF8
        public static void SetAsciiAt(byte[] buffer, int offset, string value)
        {
            Contract.Assert(offset >= 0 && offset + value.Length - 1 < buffer.Length);
            Encoding.ASCII.GetBytes(value, 0, value.Length, buffer, offset);
        }

        public static string GetAsciiAt(ArraySegment<byte> segment)
        {
            return Encoding.ASCII.GetString(segment.Array, segment.Offset, segment.Count);
        }

        public static string GetAsciiAt(byte[] buffer, int offset, int length)
        {
            Contract.Assert(offset >= 0 && offset + length - 1 < buffer.Length);
            return Encoding.ASCII.GetString(buffer, offset, length);
        }

        // +------------------------------------+
        // | Number of Name/Value pairs (int32) |
        // +------------------------------------+
        // |     Length of name (int32)         |
        // +------------------------------------+
        // |           Name (string)            |
        // +------------------------------------+
        // |     Length of value  (int32)       |
        // +------------------------------------+
        // |          Value   (string)          |
        // +------------------------------------+
        // |           (repeats)                |
        public static byte[] SerializeHeaderBlock(Dictionary<string, string> pairs)
        {
            int encodedLength = 4 // 32 bit count of name value pairs
                + 8 * pairs.Count; // A 32 bit size per header and value;
            foreach (var key in pairs.Keys)
            {
                encodedLength += key.Length + pairs[key].Length;
            }

            byte[] buffer = new byte[encodedLength];
            Set32BitsAt(buffer, 0, pairs.Count);
            int offset = 4;
            foreach (var key in pairs.Keys)
            {
                Set32BitsAt(buffer, offset, key.Length);
                offset += 4;
                SetAsciiAt(buffer, offset, key);
                offset += key.Length;
                Set32BitsAt(buffer, offset, pairs[key].Length);
                offset += 4;
                SetAsciiAt(buffer, offset, pairs[key]);
                offset += pairs[key].Length;
            }
            return buffer;
        }

        // +------------------------------------+
        // | Number of Name/Value pairs (int32) |
        // +------------------------------------+
        // |     Length of name (int32)         |
        // +------------------------------------+
        // |           Name (string)            |
        // +------------------------------------+
        // |     Length of value  (int32)       |
        // +------------------------------------+
        // |          Value   (string)          |
        // +------------------------------------+
        // |           (repeats)                |
        public static Dictionary<string, string> DeserializeHeaderBlock(byte[] rawHeaders)
        {
            var headers = new Dictionary<string, string>();

            int offset = 0;
            int headerCount = Get32BitsAt(rawHeaders, offset);
            offset += 4;
            for (int i = 0; i < headerCount; i++)
            {
                int keyLength = Get32BitsAt(rawHeaders, offset);
                Contract.Assert(keyLength > 0);
                offset += 4;
                string key = GetAsciiAt(rawHeaders, offset, keyLength);
                offset += keyLength;
                int valueLength = Get32BitsAt(rawHeaders, offset);
                offset += 4;
                string value = GetAsciiAt(rawHeaders, offset, valueLength);
                offset += valueLength;

                headers.Add(key, value);
            }
            return headers;
        }
    }
}
