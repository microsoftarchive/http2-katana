using System.Collections.Generic;

namespace Microsoft.Http2.Push
{
    internal static class ReferenceCycleDetector
    {
        private enum NodeColor : byte
        {
            White = 0,
            Red = 1,
            Black = 2,
        }

        private static Dictionary<string, NodeColor> used;

        public static bool HasCycle(Dictionary<string, string[]> sparsedMatrix)
        {
            used = new Dictionary<string, NodeColor>(64);
            foreach (var key in sparsedMatrix.Keys)
            {
                used.Add(key, NodeColor.White); //all are white
            }

            foreach (var node in sparsedMatrix.Keys)
            {
                if (Dfs(node, sparsedMatrix))
                    return true;
            }

            return false;
        }

        private static bool Dfs(string key, IDictionary<string, string[]> sparsedMatrix)
        {
            used[key] = NodeColor.Red; //red
            foreach (var child in sparsedMatrix[key])
            {
                if (used[child] == NodeColor.White) //white
                {
                    if (Dfs(child, sparsedMatrix))
                        return true;
                }
                else if (used[child] == NodeColor.Red) //red
                {
                    return true;
                }
            }

            used[key] = NodeColor.Black; //black
            return false;
        }
    }
}
