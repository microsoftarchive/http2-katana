using System.Collections.Generic;
using System.Linq;

namespace SharedProtocol.Extensions
{
    public static class KeyValuePairExtensions
    {
        public static int GetHashBasedOnKey(this KeyValuePair<string,string> kv)
        {
            var key = kv.Key;
            int result = 0;
            key.Count(c =>
                {
                    result += c;
                    return true;
                });

            return result;
        }
    }
}
