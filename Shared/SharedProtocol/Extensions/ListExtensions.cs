using System;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol.Compression;

namespace SharedProtocol.Extensions
{
    public static class ListExtensions
    {
        public static int GetIndex(this List<KeyValuePair<string, string>> list, Func<KeyValuePair<string, string>, bool> predicate)
        {
            var predicatePositive = list.Where(predicate);

            if (predicatePositive.Count() == 0)
            {
                return -1;
            }

            return list.IndexOf(predicatePositive.First());
        }

        public static int[] GetIndexes(this List<KeyValuePair<string, string>> list, Func<KeyValuePair<string, string>, bool> predicate)
        {
            var predicatePositive = list.Where(predicate);
            var result = new List<int>(16);

            foreach (var entry in predicatePositive)
            {
                var index = list.IndexOf(entry);
                if (index != -1)
                {
                    result.Add(index);
                }
            }

            return result.ToArray();
        }

        public static string GetValue(this List<Tuple<string, string, IAdditionalHeaderInfo> > list, string key)
        {
            foreach (var header in list)
            {
                if (header.Item1 == key)
                {
                    return header.Item2;
                }
            }

            throw new KeyNotFoundException(key + "was not found");
        }
    }
}
