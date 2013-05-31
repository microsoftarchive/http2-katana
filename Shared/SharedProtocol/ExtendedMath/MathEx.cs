using System;
using System.Collections.Generic;

namespace SharedProtocol.ExtendedMath
{
    public static partial class MathEx
    {
        public static Int64 POSITIVE_INFINITY = (Int64) Math.Pow(2, 64) - 1;

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
    }
}
