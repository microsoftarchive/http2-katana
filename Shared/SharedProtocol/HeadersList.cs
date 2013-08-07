using System.Collections.Generic;
using System.Linq;

namespace SharedProtocol
{
    public class HeadersList : List<KeyValuePair<string, string>>
    {
        public HeadersList() { }

        public HeadersList(IEnumerable<KeyValuePair<string, string>> list) : base(list) { }

        public HeadersList(int capacity) : base(capacity) { }

        public string GetValue(string key)
        {
            var headerFound = this.Find(header => header.Key == key);

            if (!headerFound.Equals(default(KeyValuePair<string, string>)))
            {
                return headerFound.Value;
            }

            return null;
        }

        public int GetSize()
        {
            int result = 0;
            this.Count(header =>
            {
                result += header.Key.Length + header.Value.Length;
                return true;
            });
            return result;
        }
    }
}
