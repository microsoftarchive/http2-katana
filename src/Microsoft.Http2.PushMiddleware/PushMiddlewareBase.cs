using System.Threading.Tasks;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using Owin;

namespace Microsoft.Http2.Push
{
   using PushFunc = Action<IDictionary<string, string[]>>;

    public abstract class PushMiddlewareBase : OwinMiddleware
    {
        private readonly ReferenceTable _references;

        public PushMiddlewareBase(OwinMiddleware next)
            :base(next)
        {
            _references = new ReferenceTable(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "/index.html", new[]
                            {
                                "/simpleTest.txt",
                            }
                    },
                    {
                        "/simpleTest.txt", new[]
                            {
                                "/index.html",
                            }
                    },
                });
        }
        
        public override async Task Invoke(IOwinContext context)
        {
            bool gotError = false;
            var refTree = context.Get<Dictionary<string, string[]>>(CommonOwinKeys.RefTable);
            //original request from client
            if (refTree == null)
            {
                string root = context.Request.Path.Value;
                if (!_references.ContainsKey(root))
                {
                    gotError = true; // Cant build tree because no such name in the ref table. Should invoke next
                }
                else
                {
                    var refCpy = new ReferenceTable(_references);
                    var tree = new TreeBuilder().BuildTree(refCpy, root);
                    refTree = tree;
                    context.Set(CommonOwinKeys.RefTable, tree);
                }
            }
            if (!gotError)
            {
                PushFunc pushPromise;
                string[] pushReferences;
                if (refTree.TryGetValue(context.Request.Path.Value, out pushReferences)
                    && TryGetPushPromise(context, out pushPromise))
                {
                    foreach (var pushReference in pushReferences)
                    {
                        Push(context.Request, pushPromise, pushReference);
                    }
                }
            }

            await Next.Invoke(context);
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
     
        // TODO: The spec does not specify how to derive the push promise request headers.
        // Fow now we are just going to copy the original request and change the path.
        protected bool TryGetPushPromise(IOwinContext context, out PushFunc pushPromise)
        {
            pushPromise = context.Get<PushFunc>(CommonOwinKeys.ServerPushFunc);
            return pushPromise != null;
        }

        protected abstract void Push(IOwinRequest request, PushFunc pushPromise, string pushReference);
   }
}