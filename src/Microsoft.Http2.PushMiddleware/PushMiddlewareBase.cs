using Microsoft.Owin;
using System;
using System.Collections.Generic;
using Owin;

namespace Microsoft.Http2.Push
{
    using PushFunc = Action<IDictionary<string, string[]>>;

    public abstract class PushMiddlewareBase : OwinMiddleware
    {
        protected PushMiddlewareBase(OwinMiddleware next)
            :base(next)
        {
        }

        protected bool TryGetPushPromise(IOwinContext context, out PushFunc pushPromise)
        {
            pushPromise = context.Get<PushFunc>(CommonOwinKeys.ServerPushFunc);
            return pushPromise != null;
        }

        protected abstract string[] GetPushResources(IOwinContext context);

        protected abstract void Push(IOwinRequest request, PushFunc pushPromise, string pushReference);
   }
}