using System;
using System.Collections.Generic;
using SharedProtocol.Compression;
using System.Linq;

namespace SharedProtocol.Extensions
{
    public static class ListExtensions
    {
        public static string GetValue(this List<Tuple<string, string, IAdditionalHeaderInfo> > list, string key)
        {
            var headerFound = list.Find(header => header.Item1 == key);

            if (headerFound != null && !headerFound.Equals(default(Tuple<string, string, IAdditionalHeaderInfo>)))
            {
                return headerFound.Item2;
            }
            throw new KeyNotFoundException(key + "was not found");
        }

        public static int GetSize(this List<KeyValuePair<string, string>> list)
        {
            int result = 0;
            list.Count(header =>
            {
                result += header.Key.Length + header.Value.Length;
                return true;
            });
            return result;
        }
    }
}
