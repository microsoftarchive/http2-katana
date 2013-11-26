using Microsoft.Http2.Protocol;
using Microsoft.Http2.Push.Exceptions;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin;

namespace Microsoft.Http2.Push
{
    using PushFunc = Action<IDictionary<string, string[]>>;

    public class PushMiddleware : OwinMiddleware
    {
        private readonly Dictionary<string, string[]> _references;

        public PushMiddleware(OwinMiddleware next)
            : base(next)
        {
            _references = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "/index.html", new []
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

        private bool TryGetPushPromise(IOwinContext context, out PushFunc pushPromise)
        {
            pushPromise = context.Get<PushFunc>(CommonOwinKeys.ServerPushFunc);
            return pushPromise != null;
        }

        // TODO: The spec does not specify how to derive the push promise request headers.
        // Fow now we are just going to copy the original request and change the path.
        private void Push(IOwinRequest request, PushFunc pushPromise, string pushReference)
        {
            // Copy the headers
            var headers = new HeaderDictionary(
                new Dictionary<string, string[]>(request.Headers, StringComparer.OrdinalIgnoreCase));

            // Populate special HTTP2 headers
            headers[CommonHeaders.Method] = request.Method; // TODO: Not all methods are allowed for push.  Don't push, or change to GET?
            headers[CommonHeaders.Scheme] = request.Scheme;
            headers.Remove("Host");
            headers[CommonHeaders.Host] = request.Headers["Host"];

            headers.Remove(CommonHeaders.ContentLength); // Push promises cannot emulate requests with bodies.

            // TODO: What about cache headers? If-Match, If-None-Match, If-Modified-Since, If-Unmodified-Since.
            // If-Match & If-None-Match are multi-value so the client could send e-tags for the primary resource and referenced resources.
            // If-Modified-Since and If-Unmodified-Since are single value, so it may not make sense to apply them for secondary resources.

            // Change the request path to the pushed resource
            headers[CommonHeaders.Path] = pushReference;

            pushPromise(headers);
        }
    }
}