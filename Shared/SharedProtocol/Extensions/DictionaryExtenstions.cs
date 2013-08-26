using System;
using System.Collections.Generic;

namespace SharedProtocol.Extensions
{
    public static class DictionaryExtenstions
    {
        //Develop template
        public static void AddRange(this IDictionary<string, object> dest, IDictionary<string, object> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Source is null");
            }

            foreach (var item in source)
            {
                dest.Add(item.Key, item.Value);
            }
        }

        public static void AddRange(this IDictionary<string, string> dest, IDictionary<string, string> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Source is null");
            }

            foreach (var item in source)
            {
                dest.Add(item.Key, item.Value);
            }
        }
    }
}
