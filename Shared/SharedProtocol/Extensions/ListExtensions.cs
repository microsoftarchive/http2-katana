using System;
using System.Collections.Generic;
using SharedProtocol.Compression;

namespace SharedProtocol.Extensions
{
    public static class ListExtensions
    {
        public static string GetValue(this List<Tuple<string, string, IAdditionalHeaderInfo> > list, string key)
        {
            var headerFound = list.Find(header => header.Item1 == key);

            if (!headerFound.Equals(default(Tuple<string, string, IAdditionalHeaderInfo>)))
            {
                return headerFound.Item2;
            }
            throw new KeyNotFoundException(key + "was not found");
        }
    }
}
