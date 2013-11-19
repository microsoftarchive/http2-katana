using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Http2.PushMiddleware
{
    using PushPromiseFunc = Func<IDictionary<string, string[]>, Task>;

    public class StaticPushMiddleware : OwinMiddleware
    {
        private readonly IDictionary<string, IList<string>> _references;

        public StaticPushMiddleware(OwinMiddleware next)
            : base(next)
        {
            // TODO: Validate that this table never has cycles.  Push recursion would be bad!
            _references = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "/index.html", new List<string>() 
                    {
                        "/images/image1.jpg",
                        "/scripts/sript.js",
                    }
                }
            };
        }

        public override async Task Invoke(IOwinContext context)
        {
            IList<string> pushReferences;
            PushPromiseFunc pushPromise;
            if (_references.TryGetValue(context.Request.Path.Value, out pushReferences)
                && TryGetPushPromise(context, out pushPromise))
            {
                foreach (string pushReference in pushReferences)
                {
                    await Push(context.Request, pushPromise, pushReference);
                }
            }

            await base.Next.Invoke(context);
        }

        private bool TryGetPushPromise(IOwinContext context, out PushPromiseFunc pushPromise)
        {
            pushPromise = context.Get<PushPromiseFunc>("server.PushPromise");
            return pushPromise != null;
        }

        // TODO: The spec does not specify how to derive the push promise request headers.
        // Fow now we are just going to copy the original request and change the path.
        private Task Push(IOwinRequest request, PushPromiseFunc pushPromise, string pushReference)
        {
            // Copy the headers
            HeaderDictionary headers = new HeaderDictionary(
                new Dictionary<string, string[]>(request.Headers, StringComparer.OrdinalIgnoreCase));

            // Populate special HTTP2 headers
            headers[":method"] = request.Method; // TODO: Not all methods are allowed for push.  Don't push, or change to GET?
            headers[":scheme"] = request.Scheme;
            headers.Remove("Host");
            headers[":host"] = request.Headers["Host"];

            headers.Remove("Content-Length"); // Push promises cannot emulate requests with bodies.

            // TODO: What about cache headers? If-Match, If-None-Match, If-Modified-Since, If-Unmodified-Since.
            // If-Match & If-None-Match are multi-value so the client could send e-tags for the primary resource and referenced resources.
            // If-Modified-Since and If-Unmodified-Since are single value, so it may not make sense to apply them for secondary resources.

            // Change the request path to the pushed resource
            headers[":path"] = pushReference;

            return pushPromise(headers);
        }
    }
}