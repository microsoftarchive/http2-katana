using System.Collections.Generic;
using System.Linq;

namespace Http2.TestClient
{
    public static class ArgsHelper
    {
        public static IDictionary<string, object> GetEnvironment(string[] args)
        {
            var argsList = new List<string>(args);
            var environment = new Dictionary<string, object>
                {
                    {"useHandshake", !argsList.Contains("-no-handshake")},
                    {"usePriorities", !argsList.Contains("-no-priorities")},
                    {"useFlowControl", !argsList.Contains("-no-flowcontrol")},
                };
            return environment;
        }

        public static string TryGetUri(string[] args)
        {
            return args.FirstOrDefault(a => a.Contains("http"));
        }
    }
}
