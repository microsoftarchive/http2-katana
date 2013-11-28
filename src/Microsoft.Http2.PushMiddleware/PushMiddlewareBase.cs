using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace Microsoft.Http2.Push
{
    using PushFunc = Action<IDictionary<string, string[]>>;
    public abstract class PushMiddlewareBase : OwinMiddleware
    {
        protected readonly Dictionary<string, string[]> _references;

        public PushMiddlewareBase(OwinMiddleware next)
            : base(next)
        {
            _references = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "/index.html", new[]
                            {
                                "/simpleTest.txt",
                            }
                    },
                    {
                        "/simpleTest.txt", new string[0] //simpleTest does not reference any files.
                    },
                };

            if (ReferenceCycleDetector.HasCycle(_references))
            {
                //TODO Handle this situation. 
            }
        }


        public override async Task Invoke(IOwinContext context)
        {
            string[] pushReferences;
            PushFunc pushPromise;
            if (_references.TryGetValue(context.Request.Path.Value, out pushReferences)
                && TryGetPushPromise(context, out pushPromise))
            {
                foreach (string pushReference in pushReferences)
                {
                    Push(context.Request, pushPromise, pushReference);
                }
            }

            await Next.Invoke(context);
        }


        protected bool TryGetPushPromise(IOwinContext context, out PushFunc pushPromise)
        {
            pushPromise = context.Get<PushFunc>(CommonOwinKeys.ServerPushFunc);
            return pushPromise != null;
        }

        protected abstract void Push(IOwinRequest request, PushFunc pushPromise, string pushReference);

    }
}