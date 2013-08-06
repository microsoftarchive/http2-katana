using System;
using System.Collections.Generic;
using SharedProtocol.Compression;
using System.Linq;

namespace SharedProtocol.Extensions
{
    public static class ListExtensions
    {
        public static string GetValue(this List<KeyValuePair<string, string>> list, string key)
        {
            var headerFound = list.Find(header => header.Key == key);

            if (!headerFound.Equals(default(KeyValuePair<string, string>)))
            {
                return headerFound.Value;
            }
            
            return null;
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
