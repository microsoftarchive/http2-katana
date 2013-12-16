// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Push
{
    internal class GraphHelper
    {
        private class ReferenceCycleDetector
        {
            private enum NodeColor : byte
            {
                White = 0,
                Red = 1,
                Black = 2,
            }

            private Dictionary<string, NodeColor> _used = new Dictionary<string, NodeColor>(64);

            public bool HasCycle(Dictionary<string, string[]> sparsedMatrix)
            {
                _used.Clear();
                foreach (var key in sparsedMatrix.Keys)
                {
                    _used.Add(key, NodeColor.White); //all are white
                }

                foreach (var node in sparsedMatrix.Keys)
                {
                    if (Dfs(node, sparsedMatrix))
                        return true;
                }

                return false;
            }

            private bool Dfs(string key, IDictionary<string, string[]> sparsedMatrix)
            {
                _used[key] = NodeColor.Red; //red
                foreach (var child in sparsedMatrix[key])
                {
                    if (_used[child] == NodeColor.White) //white
                    {
                        if (Dfs(child, sparsedMatrix))
                            return true;
                    }
                    else if (_used[child] == NodeColor.Red) //red
                    {
                        return true;
                    }
                }

                _used[key] = NodeColor.Black; //black
                return false;
            }
        }

        private class TreeBuilder
        {
            private Dictionary<string, bool> _used = new Dictionary<string, bool>(64);
            private Dictionary<string, string[]> _tree = new Dictionary<string, string[]>();

            public Dictionary<string, string[]> BuildTree(ReferenceTable table, string root)
            {
                if (!table.ContainsKey(root))
                    throw new Exception("No such key. Should invoke next.");

                _used.Clear();
                _tree.Clear();

                foreach (var key in table.Keys)
                {
                    _used.Add(key, false);
                }

                Dfs(table, root);

                return _tree;
            }

            private void Dfs(ReferenceTable table, string root)
            {
                _used[root] = true;
                var nonused = new List<string>(8);

                foreach (var child in table[root])
                {
                    if (!_used[child])
                    {
                        nonused.Add(child);
                        Dfs(table, child);
                    }
                }
                _tree.Add(root, nonused.ToArray());
            }
        }

        public Dictionary<string, string[]> BuildTree(ReferenceTable table, string root)
        {
            return new TreeBuilder().BuildTree(table, root);
        }

        public bool HasCycle(Dictionary<string, string[]> sparsedMatrix)
        {
            return new ReferenceCycleDetector().HasCycle(sparsedMatrix);
        }
    }
}
