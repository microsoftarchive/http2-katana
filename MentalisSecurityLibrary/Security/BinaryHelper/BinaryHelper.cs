//-----------------------------------------------------------------------
//<copyright file="BinaryHelper.cs" company="Microsoft Open Technologies, Inc.">
//Copyright © 2002-2007, The Mentalis.org Team
//Portions Copyright © Microsoft Open Technologies, Inc.
//All rights reserved.
//http://www.mentalis.org/ 
//Redistribution and use in source and binary forms, with or without modification, 
//are permitted provided that the following conditions are met:
//- Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
//- Neither the name of the Mentalis.org Team, 
//nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
//INCLUDING, BUT NOT LIMITED TO, 
//THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
//PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
//OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
//EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Org.Mentalis.Security.BinaryHelper
{
	    /// <summary>
    /// Binary operations helper class.
    /// </summary>
    internal static class BinaryHelper
    {
        /// <summary>
        /// Converts array of bytes to int32
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <returns>The Int32 number.</returns>
        public static Int32 Int32FromBytes(params byte[] bytes)
        {
            Int32 result = 0;

            for (int i = bytes.Length - 1; i >= 0; --i)
            {
                result |= bytes[i] << (8 * i);
            }

            return result;
        }

        /// <summary>
        /// Converts array of bytes to int32
        /// </summary>
        /// <param name="bytes">ArraySegment array</param>
        /// <returns>The Int32 number.</returns>
        public static Int32 Int32FromBytes(ArraySegment<byte> bytes)
        {
            return Int32FromBytes(bytes, 0);
        }

        /// <summary>
        /// Converts array of bytes to int32
        /// </summary>
        /// <param name="bytes">ArraySegment array</param>
        /// <param name="ignoreFirstBitsNum">Number of bits to ignore</param>
        /// <returns>The Int32 number.</returns>
        public static Int32 Int32FromBytes(ArraySegment<byte> bytes, int ignoreFirstBitsNum)
        {
            Int32 result = 0;
            for (int i = 0; i < bytes.Count; ++i)
            {
                byte b = bytes.Array[i + bytes.Offset];
                if (i == 0 && ignoreFirstBitsNum > 0)
                {
                    b &= (byte)(0xFF >> ignoreFirstBitsNum);
                }

                result = result << 8;
                result |= b;
            }

            return result;
        }

        /// <summary>
        /// Converts int32 to byte array
        /// </summary>
        /// <param name="value">Int32 value</param>
        /// <param name="bytes">ArraySegment array to fill</param>
        public static void Int32ToBytes(Int32 value, ArraySegment<byte> bytes)
        {
            for (int i = 0; i < bytes.Count; ++i)
            {
                bytes.Array[bytes.Count - 1 - i + bytes.Offset] = (byte)((value) >> (i * 8));
            }
        }

        /// <summary>
        /// Creates array of bytes, then converts int32 to the array
        /// </summary>
        /// <param name="value">Int32 value</param>
        /// <param name="bytesNum">Number of bytes </param>
        /// <returns>Array of bytes of size bytesNum.</returns>
        public static byte[] Int32ToBytes(Int32 value, int bytesNum)
        {
            var bytes = new byte[bytesNum];
            for (int i = 0; i < bytesNum; ++i)
            {
                bytes[bytesNum - 1 - i] = (byte)((value) >> (i * 8));
            }

            return bytes;
        }

        /// <summary>
        /// Creates array of bytes, then converts int32 to the array
        /// </summary>
        /// <param name="value">Int32 value</param>
        /// <param name="bytesNum">Number of bytes </param>
        /// <returns>Array of bytes of size bytesNum.</returns>
        public static byte[] Int32ToBytes(Int64 value, int bytesNum)
        {
            var bytes = new byte[bytesNum];
            for (int i = 0; i < bytesNum; ++i)
            {
                bytes[bytesNum - 1 - i] = (byte)((value) >> (i * 8));
            }

            return bytes;
        }
        /// <summary>
        /// Converts int64 to the array of bytes size 4
        /// </summary>
        /// <param name="value">Int64 value</param>
        /// <returns>Array of bytes of size 4.</returns>
        public static byte[] Int64ToBytes(Int64 value)
        {
            return Int32ToBytes(value, 4);
        }
        /// <summary>
        /// Converts int32 to the array of bytes size 4
        /// </summary>
        /// <param name="value">Int32 value</param>
        /// <returns>Array of bytes of size 4.</returns>
        public static byte[] Int32ToBytes(Int32 value)
        {
            return Int32ToBytes(value, 4);
        }

        /// <summary>
        /// Converts int16 to the array of bytes size 2
        /// </summary>
        /// <param name="value">Int16 value</param>
        /// <returns>Array of bytes of size 2.</returns>
        public static byte[] Int16ToBytes(Int16 value)
        {
            return Int32ToBytes(value, 2);
        }

        /// <summary>
        /// Converts array of bytes to Int16
        /// </summary>
        /// <param name="msByte">Most significant byte</param>
        /// <param name="lsByte">Least significant byte</param>
        /// <returns>Int16 integer.</returns>
        public static Int16 Int16FromBytes(byte msByte, byte lsByte)
        {
            return Int16FromBytes(msByte, lsByte, 0);
        }

        /// <summary>
        /// Converts array of bytes to Int16, ignoring higher bits
        /// </summary>
        /// <param name="msByte">Most significant byte</param>
        /// <param name="lsByte">Least significant byte</param>
        /// <param name="ignoreFirstBitsNum">Number of higher bits to ignore</param>
        /// <returns>Int16 integer.</returns>
        public static Int16 Int16FromBytes(byte msByte, byte lsByte, int ignoreFirstBitsNum)
        {
            if (ignoreFirstBitsNum > 0)
            {
                msByte &= (byte)(0xFF >> ignoreFirstBitsNum);
            }
            return (Int16)((msByte << 8) | lsByte);
        }
    }

}
