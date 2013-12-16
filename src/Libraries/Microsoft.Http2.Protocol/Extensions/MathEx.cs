// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.Http2.Protocol.Extensions
{
    /// <summary>
    /// Extended math class. It's partial for future extending.
    /// </summary>
    public static partial class MathEx
    {
        public static T Min<T>(IComparer<T> comparer, params T[] items)
        {
            if (items == null || items.Length <= 1)
                throw new ArgumentNullException("items");

            if (comparer == null)
                throw new ArgumentNullException("comparer");

            T result = items[0];
            for (int i = 0; i < items.Length - 1; i++)
            {
                result = comparer.Compare(result, items[i + 1]) > 0 ? items[i + 1] : result;
            }

            return result;
        }

        public static T Min<T>(params T[] items) where T : IComparable<T>
        {
            if (items == null || items.Length <= 1)
            {
                throw new ArgumentNullException("items");
            }

            T result = items[0];
            for (int i = 0; i < items.Length - 1; i++)
            {
                result = result.CompareTo(items[i + 1]) > 0 ? items[i + 1] : result;
            }

            return result;
        }

        public static byte[] ComputeMD5ChecksumOf(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(String.Format("Specified file not found {0}",path));
            }

            var fileData = File.ReadAllBytes(path);
            return new MD5CryptoServiceProvider().ComputeHash(fileData);
        }
    }
}
