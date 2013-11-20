using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Http2.Push;

namespace Owin
{
    public static class PushExtensions
    {
        public static IAppBuilder UsePush(this IAppBuilder builder)
        {
            return builder.Use(typeof(PushMiddleware));
        }

        public static bool HasRecursion(this Dictionary<String, IList<String>> graph)
        {
            return graph.Any(node => IsRecursionByThisHead(node, graph));
        }

        private static bool IsRecursionByThisHead(KeyValuePair<String, IList<String>> current,
                                            Dictionary<String, IList<String>> wholeHierarchy)
        {
            bool gotRecursion = false;
            foreach (var vertex in current.Value)
                if (wholeHierarchy.Keys.Contains(vertex))
                    if (wholeHierarchy[vertex].Contains(current.Key))
                        gotRecursion = true;

            return gotRecursion;
        }

    }
}
