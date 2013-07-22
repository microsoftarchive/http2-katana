using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace SharedProtocol.ExtendedMath
{
    /// <summary>
    /// Extended math class. It's partial for future extending.
    /// </summary>
    public static partial class MathEx
    {
        public static T Min<T>(IComparer<T> comparer, params T[] items)
        {
            if (items == null || comparer == null || items.Length <= 1)
            {
                throw new ArgumentException("Argument has null or empty value");
            }

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
                throw new ArgumentException("Argument is null or empty");
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
